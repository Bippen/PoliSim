# PoliSim UI v3.0 — Direction (founding document, 2026-08-28)

**Elias's brief, verbatim in substance:** fold-in for the side tabs; a full main screen with
oversight; the main page loses unnecessary text and goes graphics-and-simplicity.

**The thesis, ruled (V3-R1):** v3.0 is **the desk with fewer words, not a different desk.** Two
altitudes, one idiom: the landing surface becomes an instrument stage — full-bleed, graphical,
nearly wordless — while the deep screens (Budget, laws, statistics ledgers) remain the documents
they are. Same paper, inks, fonts, sprites, stamps; the entire v2 chrome, the 96-sprite pack, the
capture corpus and every guard carry over. *The struck alternative — a new visual idiom — would
orphan the asset pipeline and Design's eleven boards sixteen days before the election build; it is
one line to un-strike if the instrument stage disappoints on film.*

## The three pillars

### 1. The fold (V3-R2)

The mechanism already exists half-built: **Budget full-screen hides the left column outright**
(spec §A.5's declared deviation). v3 promotes that from one screen's special case to a first-class,
player-controlled shell state, everywhere:

- **OPEN** — today's frame exactly: chrome column at `LeftColumnWidthFraction`, tab tongues,
  content column. Nothing moves; v2 screens are already correct in this state.
- **FOLDED** — chrome column and tab tongues collapse to **one icon rail** (~56 px class): the
  area icons carry navigation (they exist, coverage 14/14, promoted in the omnibus — this is the
  rail they were waiting for), a collapsed calendar chip (month + day numeral, the pad's own
  materials), the status dot (HELD glow / RUNNING green — B8's carrier survives folding), and the
  fold toggle. Everything else yields to the stage.
- **State rules:** the flip is **instant** (the calendar's own ruling — this desk does not tween);
  the state persists per save; per-screen defaults — the oversight screen defaults FOLDED, ledger
  screens default OPEN; every screen must be *legal* in every state a player can reach (guards run
  in each; locking a state — R-A1's Budget precedent — is a legitimate way to make it unreachable,
  recorded per screen: **R-SP2, ratified 2026-08-28**, amending "in both states"), but only
  defaults are canonical on film (V3-R4).
- Minimum width stays 1280×720; the fold is what makes the floor generous instead of tight.

### 2. The Desk — the oversight screen (V3-R3)

A new landing surface, **Screen 0, "The Desk"**: the full-bleed stage the fold exists for, composed
from instruments the project has already earned — the world map, the compass, the approval face
with its nine-term attribution, the sparkline strip at 1l's weights, the calendar sheet, the
event/alert stamps, the stepped rules. **It is the legibility program's graphical culmination:**
everything on it is derived, attributed, and drawn — nothing is authored prose.

**The text budget, absolute:** captions at mono 9.5 and instrument labels only; no sentences, no
paragraphs, no restatements. Numbers appear as instruments (a dial, a bar, a rule, a sparkline)
with the numeral as the instrument's label, never as a text row. The playtest-3 taxonomy extends:
class (b) restatements may return **only as instruments**; class (c) never returns.

**Design draws it before we build it.** The board-is-the-spec convention holds: The Desk and the
rail are boards first (the Phase A request), built second. Design draws against our census and
instrument inventory, the way 1i was drawn against a real capture — at **1280×720 first**, because
the floor is where graphics-first pays or fails.

**Ratified standing 2026-08-28 (R-PC1, Phase C) — three of the build's calls (`COMPLETED.md` §41):**

- **R-B2, the ways home.** The boards name no way back to the Desk; the rail's calendar chip (the
  sheet collapsed, so its click opens the sheet's home) and the open document's own rail icon
  clicked again return to Screen 0. The Desk's rail shows no spine (board 1m, D2).
- **R-B3, the lock.** Screen 0 locks FOLDED — its three columns take the whole window and the
  chrome column's contents (the sheet, the cluster, the hold banner) live on the stage; OPEN is
  unreachable there (R-SP2's form), the toggle rendered on its disabled face.
- **R-B4, the eight.** The effects card draws C22's eight figures — the preview's own. Board 1m's
  two further rows, **debt-to-GDP and currency, are refused because the model does not estimate
  them**; the refusal is the model's honesty, not a layout choice. *Recorded line:* if those two
  estimates are ever wanted they are a **simulation feature with its own measurement pass**
  (candidate slot: after item 10), never a UI patch that prints a number the preview does not
  compute.

Design's side already holds the same three: the live screens file's board 1m gained a `BUILT
2026-08-28 (631a9d4, v3desk_*)` block the same day — *"pointer, not an edit. Standing corrections
from the build, accepted: the effects card draws C22's eight rows (this board's debt-to-GDP /
currency rows were not estimates the game holds — a board error, theirs the right fix, R-B4); ways
home = calendar chip / active icon again (R-B2, the board named none); Screen 0 locks FOLDED
(R-B3)."* Read back through the Design MCP at the Phase C kickoff; the file is otherwise unchanged
since the Phase B read.

### 3. The cut (the main page's text)

Phase A takes a film-based census of the current landing screen's every text element, classified
by the playtest-3 taxonomy — (a) load-bearing / (b) restating / (c) decoration — with counts.
Pure (c) dies immediately (that cut needs no board). (b) waits for the board to return as
instruments or not at all. (a) is the board's required content list. The census, not taste, is
what Design designs against.

## What v3.0 is NOT

No new fonts, inks, or hue-budget changes; no Canvas rewrite (the shell is IMGUI like the frame it
folds); no simulation change of any kind; no per-element redesign of deep screens (screen
granularity stands); no new sprites assumed — the rail and the stage are composed from delivered
art and primitives until a board proves a gap, and any gap becomes a request, never an inline
invention.

## Sequence, against 13 September

- **Phase A (now, one session):** census · shell + rail built and guarded in both states · the
  instrument inventory with measured minimum sizes · the Design request written as the request
  doc's next ask. *The shell builds before the board because it is structure, not aesthetics — it
  gets re-skinned, not re-architected, when the board lands.*
- **Send (Elias, one gesture):** the request doc now carries §E5 + the v3 ask — hold the pending
  request-doc send until Phase A lands so one send carries both; the courtesy note can go any
  time.
- **Phase B (on Design's boards):** The Desk built against the board; the (b)-class returns
  resolved; capture family `v3desk_*`. **Built 2026-08-28, the day boards 1m and 1n landed on the
  live screens file** — Screen 0 (`GameController.Desk.cs`) and the rail's re-skin, against the
  boards as read into `POLISIM_V2_SCREEN_SPEC.md` §A.17; the build's own reversible calls are
  R-B1…R-B14 (`COMPLETED.md` §41).
- **Phase C:** per-screen fold defaults tuned on film; §P's density verdict re-read on the folded
  stage.
- **Item 10 lands inside v3:** election night is born on the v3 shell — the Desk folded, the map
  as the stage. **Fallback, stated:** if Design's board has not landed by the gate, election night
  builds in the OPEN state (pure v2, fully supported) and moves to the stage later; the shell
  ships either way, so nothing converts twice.

## Validation continuity (V3-R4)

Every existing capture state stays canonical in its screen's default fold state; the shell adds
one folded/unfolded pair per column-layout class (standard, Budget full-screen, Canvas), not ×2
across the corpus. Guards, edge checks, and the containment assert run in both states in the
harness even where film shows one. The label-clipping class stays closed — a folded-state clip is
a new instance of the old class and is treated as such.

**Amended 2026-08-28 (R-SP2, the stage-prep micro-pass — ratified):** "legal in both states" reads
*legal in every state a player can reach*. Locking a state (the Budget ledger's FOLDED lock, R-A1)
is a legitimate way to make a state unreachable; each lock is recorded per screen, and the harness
sweeps and films only the states that remain reachable.
