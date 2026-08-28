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
| §S — the send package | ✅ **SENT 2026-08-27** (the request, hash-verified, to Design's project); the courtesy note (rewritten 1i–1l-aware 2026-08-28) alone still waits on **Elias — send** | the E2 convention: sending is Elias's — this send was on his instruction |
| §A — the ruling queue Q6–Q10; F2; **the omnibus's A4–A6** (the ledger row type, the raster budget, the anchored form) | **Elias — a decision** | each at its own named trigger; A4–A6 are the 2026-08-28 report's `RULINGS NEEDED` |
| §B — three seed quality debts | **Elias — database access** (an OECD/Eurostat re-sourcing session) | none blocks anything |
| §D — item 10, the political game, and everything riding it (**+ the political-model fact Phase 3 measured**) | **Sweden's vote, 13 Sept 2026, then Elias's pricing decision** | the one remaining spine item |
| ~~§D1 — cabinet portraits, eight outstanding~~ | ✅ **DELIVERED AND IMPORTED 2026-08-27** (Progress5; `PortraitCoverageCheck` 25 of 25) | tombstone below; the look is §V's |
| §E2 — mark accounting + the R5 hexes | **item 10** | 13 Sept 2026 |
| ~~§E3 — rasterization diff, our half~~ | ✅ **CLOSED 2026-08-28** (the omnibus, `a15c0c1`: resvg as `StripCutDiffCheck`'s external rasterizer, the six buttons 6/6) | tombstone below; its two findings are §E5 |
| §E4 — the icon promotion for R4-1's two Society rows | **Claude Design — the next batch**, behind §D1 | two `StatNodeId` members first |
| §E5 — two strip-cut findings (the hatch tile's tiling, the slider track's strip) | **Claude Design — re-cut or explain** (Elias to say which side is the truth) | filed as `CLAUDE_DESIGN_ASSET_REQUEST.md` §E5 (2026-08-28), goes with the next send |
| §F — the session-sourced seed spread (OECD PMR 2023-24, SOCX 2021) | **Elias — CONFIRM** the mapping and six caveats | every figure `[PROVISIONAL]`; the trajectories byte-identical either way |
| §V — built, not seen (the omnibus review package, every surface with its capture named) | **Elias — a visual review** | rule 3's third layer |
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

**Still unsent — Elias's:** the courtesy note (`CLAUDE_DESIGN_BOARD_1I_NOTE.md`, corrected
2026-08-27, rewritten 1i–1l-aware 2026-08-28). It was not part of the instruction that sent the
request; it is a note, not an ask, and carries nothing Design is waiting for. §D1 is no longer gated
behind this entry — the verdict is in Design's project.

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

**The omnibus pass's `RULINGS NEEDED` (2026-08-28; the report carries the same three, each a one-line
answer, each written back here when given):**

- **A4 — the ledger row's one-line type.** R-K4 set the law browser's row pitch to the Budget's ledger
  pitch and measured it (1600: 80px / 4.5 laws per viewport → 66px / 5.3; 2560: 85 / 6.1 → 68 / 7.6; 1280:
  66 / 2.5 → 55 / 2.5, the §A.8 caption block taking the floor's gain). The 1j board's ~27px is a ONE-LINE
  row; the ledger row is two lines by construction (the wrap-first name ladder). A one-line row type is a
  different row — build it, or accept the two-line pitch as the browser's? (`476c66c`,
  `omni_final_*_06f_policylaws_laws*`.)
- **A5 — the raster check's budget.** `StripCutDiffCheck`'s 2% mismatch budget was set before any
  comparable output existed; with resvg, nine of the 42 Stats icons sit at 2.06–3.21% on a family that
  runs 0.5–3.2% in a continuum (antialiasing at a 10.7× upscale of 24-unit strokes). Raise the budget to
  4% (all nine pass, the largest at 3.21%), or keep 2% and hand-inspect the nine? Left as set (§E5).
- **A6 — the anchored form, confirmed with real seeds in.** The seed-spread mechanism (`6df94de`) measures
  every welfare and regulation effect from the country's seeded position, so the sourced seeds (`915c800`)
  moved the compass and the dials' starting positions and NOT the no-policy trajectories (6/6 identical).
  That is the honest form Elias ruled for on 2026-08-27 with the figures still unsourced; now that they
  are in, confirm it stands — the live-deviation alternative (effects measured from zero, every baseline
  moving) is one revert away and is recorded in `CLAUDE.md` "Playtest 3, the rulings".

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

Not Design's: the nine Stats icons 0.06–1.2 pp over the check's 2% budget are an antialiasing margin on a
budget set blind (RULINGS NEEDED, §A). The three stamps differ only by font rendering (`TEXT`, named).

## 🟡 E4. The `StatNodeId`/icon promotion for R4-1's two Society rows (youth unemployment, life expectancy)

Both rows exist as plain derived rows with no `StatNodeId` and no icon (`DrawDerivedStatRow` in
`GameController.cs`). Promotion is two enum members and two icon files; **the icon ask joins the next
Design asset batch** (`CLAUDE_DESIGN_ASSET_REQUEST.md` §4 names it as costed, not requested; D1's
batch landed 2026-08-27, so nothing queues ahead of it now — the promotion itself is the first step and
is ours). Waiting on Design once the members exist, so not startable as a request today.

---

# F. Waiting on Elias — CONFIRM the session-sourced seed spread (sourced 2026-08-28 under R-K9; every figure `[PROVISIONAL]`)

**The ruling (playtest 3, finding 1):** option (i), a per-country seed spread for sector regulation and
implemented welfare programs, from real data per the standing rule — "do NOT invent a spread to make
the plot look good; if the figures need sourcing, say so and Elias will source them." R-K9 of the omnibus
kickoff (2026-08-28) had the session source them from the OECD primary datasets and tag every value
`[PROVISIONAL - session-sourced 2026-08-28, Elias to confirm]`; they landed at `915c800` in the slots the
mechanism (`6df94de`) had held open. **What waits on Elias is the confirmation** — of the mapping (which
was §F's own proposal, followed as written) and of the six caveats below — not the sourcing. The
trajectories stayed byte-identical (the anchored form; `traj_post_phase4` ≡ `traj_pre_seedspread` 6/6), so
a struck figure costs one literal, nothing downstream.

**Regulation — OECD Product Market Regulation, 2023-24 vintage on the 2023 methodology (0–6, lower = less
regulated).** Economy-wide from the OECD's own workbook `PMR-Indicator_Econwide_2023-24-and-2018_02.02.2026.xlsx`
(oecd.org, retrieved 2026-08-28, SHA-256 D0EBCFC7…; sheet `PMR_Econwide_2023-24`, its published "OECD
average" row 1.3464), cross-checked against the SDMX API (`OECD.ECO.GCRD,DSD_PMR@DF_PMR,1.3` — identical to
seven decimals for all six; the published average and the 38-member simple mean differ by 0.0004, so the
mean is the convention the sector series use, where no published row exists). Mapping: level = 50 × PMR /
average, clamped 10–90.

| country | PMR 2023 | level | ENERGY (mean 1.3134) | ECOMM (1.3056) | RETAIL_TRADE (1.0409) |
|---|---|---|---|---|---|
| USA | 1.5786 | 58.6 | 0.9855 → 37.5 | 1.4606 → 55.9 | 1.5714 → 75.5 |
| Sweden | 0.8063 | 29.9 | 1.0959 → 41.7 | 1.5459 → 59.2 | 0.5714 → 27.4 |
| Germany | 1.2080 | 44.9 | 0.4543 → 17.3 | 1.3928 → 53.3 | 0.8929 → 42.9 |
| France | 1.2297 | 45.7 | 0.8027 → 30.6 | 1.3188 → 50.5 | 3.0000 → 90 (144.1 clamped) |
| Italy | 1.2310 | 45.7 | 0.7207 → 27.4 | 0.7426 → 28.4 | 1.9286 → 90 (92.6 clamped) |
| Poland | 1.0664 | 39.6 | 1.3779 → 52.5 | 0.9784 → 37.5 | 1.0612 → 51.0 |

**Welfare — OECD SOCX public social expenditure by policy area, % of GDP, 2021** (the latest year all six
report the programme breakdown — the USA runs to 2023, France to 2022; dataflow
`OECD.ELS.SPD,DSD_SOCX_AGG@DF_SOCX_AGG,1.0`, expenditure source Public, retrieved 2026-08-28). The FACT half
as proposed: universal statutory health coverage — the five, not the USA; means-tested, housing and
childcare — all six; UBI and NIT — none. The FIGURE half: generosity = clamp(spend / CostShareOfGdp × 100,
0, 100) with the budget's own cost shares (healthcare 10, means-tested 6, housing 1.5, childcare 1).

| country | Health TP41 → healthcare | Family in-kind TP51/K → childcare | Housing TP82 → housing | Other social policy TP91 → means-tested |
|---|---|---|---|---|
| USA | 9.496 — not implemented (stays in the Healthcare budget line) | 0.568 → 56.8 | 0.236 → 15.7 | 0.900 → 15.0 |
| Sweden | 6.954 → 69.5 | 2.049 → 100 (204.9) | 0.378 → 25.2 | 0.529 → 8.8 |
| Germany | 9.994 → 99.9 | 1.436 → 100 (143.6) | 0.528 → 35.2 | 0.156 → 2.6 |
| France | 9.654 → 96.5 | 1.353 → 100 (135.3) | 0.632 → 42.1 | 1.216 → 20.3 |
| Italy | 6.880 → 68.8 | 0.588 → 58.8 | 0.041 → 2.7 | 1.559 → 26.0 |
| Poland | 4.613 → 46.1 | 0.808 → 80.8 | 0.024 → 1.6 | 0.127 → 2.1 |

**The six caveats, for the confirmation (each a one-literal change if struck):**
1. "The cash social-assistance component of income support" was read as TP91's cash half, which TP91's
   total already contains — means-tested = TP91 total, nothing counted twice.
2. Germany's minimum income (Bürgergeld) is booked under Unemployment (TP71) in SOCX, not TP91 — its 2.6
   understates the real scheme; the aggregate dataflow cannot separate it. Poland's 2.1 is the same class.
3. Childcare follows "family, services/in-kind" (TP51/K), which includes home help and other in-kind
   services; ECEC alone (TP521) gives USA 31.5, SWE 100 (149), DEU 81.0, FRA 100 (124), ITA 49.4, POL 67.8.
   Three countries clamp at 100 under either reading — the model's 1%-of-GDP full-generosity cost sits
   below real spending.
4. 2021 is a pandemic-affected year (USA health 9.496 vs 8.956 in 2023); "latest common year" followed.
5. Poland's housing line is TP822 "other benefits in kind" (no TP821 entry); the fact half's ✓ kept.
6. France's retail indicator jumped 1.99 → 3.00 between 2018 and 2023 and clamps at 90, as does Italy's 1.93.

**What landed:** one line per country for regulation, one tuple per implemented program for welfare, the
`[PLACEHOLDER]` tags retired; the Compass Y axis spreads (raw 37.5 Poland … 57.3 France, 19.8 units over
461 of 600 px — `CompassAxisDiagnostic`, `compass_post_phase4`); the Sectors and Welfare tabs open at the
real positions (§V); the no-policy trajectories byte-identical.

**Still flagged, not this section's to decide:** the same uniform-50 finding holds for the other four
sector dials and the labor/crime dials the seed file lists as uniform placeholders; the Compass Y formula
averages generosity over IMPLEMENTED programs, so a country with one generous program outranks a broad
welfare state.

# V. Waiting on Elias — a visual review (built, not seen; rule 3's third layer)

**The review package of the omnibus pass (2026-08-28).** Everything visual the pass shipped, each with
its capture named — the closing matrix `omni_final_<size>_<screen>.png` at 1280×720, 1600×900, 1920×1080
and 2560×1440 (USA) plus the per-country sets named per row, all under
`G:\UNITY\Projects\PoliSim-captures\`. "Pinned on film" is containment evidence, not a sighting; each item
closes to `COMPLETED.md` with the session named. The three findings of playtest 3 are closed by their
rulings; their surfaces are re-listed here as built.

| surface | built | the capture | what to look for |
|---|---|---|---|
| **The Compass — the Y axis** (Politics › Compass) | `915c800` | `omni_final_*_07b_politics_compass` | six countries on both axes (raw Y 37.5 Poland … 57.3 France); the sourced spread is PROVISIONAL (§F) |
| **The Economic Sectors tab at the sourced positions** (Policy/Laws › Economic Sectors) | `915c800` | `omni_final_*_06c_policylaws_sectors` (+`_rows`, `_deep`) | Regulation opens at the country's PMR level (USA 59; Energy 38, Telecoms 56, Retail 76), not 50 |
| **The Welfare tab at the sourced portfolio** (Budget › Welfare) | `915c800` | `omni_final_*_05c_budget_welfare`; Sweden `omni_p4_swe1600_05c_budget_welfare` | the USA opens with means-tested / housing / childcare implemented and no universal healthcare; Sweden with four |
| **The Policy Web's causal graph** (Policy/Laws › Policy Web) | `a267fd6` | `omni_final_*_06d_policylaws_policyweb` | ⚠ the derived / declared idiom and the stat chords draw on a CLICKED node — no capture state pins one; the rest is verified by code |
| **The four budget decompositions** (Budget › Spending, per country) | `6307dce` / `ad7b240` / `d33e1ae` / `e04f238` | `omni_p6_de1600_05b_budget_spending_rows`, `omni_p6_it1600_…`, `omni_p6_pl1600_…`, `omni_p6_fr1600_…` (+`_deep`) | the real areas at the game's G level; the largest line the remainder; the % of GDP column carries the method's distortion (Germany's Defense 5.1%) |
| **The RUNNING status plate and the disabled speed face** (§A.6, B5) | `f92e14f` → `adcb52e` | `omni_final_*_01b_running_strip`, `_90_interrupt_held` | the plate under the label, the lamp, the muted 1×/2×/3× faces while held |
| **The screen captions and the sub-tab icons** (§A.8, R-K6) | `c188b28`, `da6a684` | every `omni_final_*_0[2-7]*` | the caption right-aligned above the rule; icons on Statistics and Budget rows, none on the Policy/Laws row at 1600 (by width) |
| **The inactive tab-swatch tints; the urgency chip as a stamp** (§A.3, §A.11) | `adcb52e` | `omni_final_*_0[2-7]_*`; `_03_decisions` | the folder tongues' icon tint; HOLDS TIME / CAN WAIT rotated −2° |
| **The row family on the Fed tab, International, the Trade bill card** | `f145ba2` | `omni_final_*_07d_politics_federalreserve*`, `_02b_statistics_international*`, `_06e_policylaws_trade*` | rows, not sentence labels; the Trade card's three cost rows |
| **Board 1k — the calendar as one almanac sheet** | `3fa3eb2` | `omni_final_*_0[2-7]` (the left panel) | the diagonal strike, the ledger dot, the 4-dot underline, one sheet |
| **Board 1l — the graph weights** | `2b698b0` | `omni_final_*_02a_statistics_domestic_deep` | history 3px over the projection's 2px dashes |
| **§A.13's two envelope rows** (the Signing screen's document entrance and the button fade) | `14740e2` | ⚠ no capture state opens the ceremony — verified by compile and the seam's flag | the 260ms rise, the 460ms button delay |
| **The law browser at fifty** (the ledger pitch, the name ladder) | `476c66c` | `omni_final_*_06f_policylaws_laws*` | pitch 66px / 5.3 laws per viewport at 1600 (RULINGS NEEDED on the one-line row type); the detail pane's content sized to its viewport in `a331e82` (no horizontal scrollbar, the status whole at 1280) |
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
