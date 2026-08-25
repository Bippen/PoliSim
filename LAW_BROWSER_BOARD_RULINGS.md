# Law browser board — Design rulings (§7)

Drawn 2026-08-25 as **Screen 1i** in `PoliSim v2 Screens.dc.html`
(`data-screen-label="1i Law browser board"`). The board is the spec; this file
is its written form so the build has a target that is not a chat message.

Drawn against the populated state of `run_85g_bill_laws.png` — 8 in force,
2 pending, 38 total, one populated category.

## The three open questions, answered

**Row weight.** Four cells, not five: status glyph · name · category ·
magnitude · cost (the glyph is a gutter, not a column). Name carries full
weight at 14px bold. Magnitude and cost are mono, secondary. Category is a
dimmed 9.5px mono token and earns its column only once a second category
populates. The six-dial delta breakdown, the real-world citation and the live
estimate are **detail-pane only**.

**Status.** Grouping, not a column. IN FORCE — 8 / BEFORE THE HOUSE — 2 /
AVAILABLE — 28, in force first. Sorting in force to the top makes a status
column redundant, which is why the row lost a cell, and it answers the
capture-evidenced failure structurally rather than by adding a control: what
is in force is the first thing on the board.

**Magnitude.** A four-step stepped rule in plain ink (`#2b2620` filled,
`#cec0a2` empty) beside its mono label. No new hue: status and category
already spend the colour budget, and the weight class is ordinal, so length
reads faster than a tag. MINOR 1 · MODERATE 2 · MAJOR 3 · SWEEPING 4.

## Board mechanics

- **Category chips carry counts.** `All · 38`, `Crime & Justice · 38`, then
  five hatched chips at `· 0`. This makes an inert filter read as empty
  content rather than a broken control. **It is a legibility fix, not the bug
  fix** — the underlying filter still needs fixing and its cause reporting.
- **Sticky column header inside the scroller** (`position:sticky;top:0`), so
  the header grid and the row grids resolve against the same scrollbar-reduced
  width. A header sibling above the scroller misaligns by the scrollbar width.
- **One scroll.** The list scrolls; the detail pane does not. ~27 rows per
  screen against the capture's 1.3 cards.
- **Bottom bar on 1c's convention** (`flex:1` spacer + bar): approval on hand,
  next sitting date, and the affordability line that makes the cost column a
  budget rather than a label.
- **Citation pane is new UI content** — the CONFIRMED / DIRECTIONAL /
  GENRE-IDIOM label and source exist only in code comments today. Flagged on
  the board as `NEW UI CONTENT — §7`.
- **Live estimate before a bill exists** — the Tax/Welfare per-row shape moved
  earlier, not the bill-gated `DrawBillLiveEstimate`.
- **No new sprites.** Desk chrome, ledger rows, existing stamps.

## The stepped rule — exact spec

The one element with no precedent in the chrome set, so it is pinned here in
full. All values are design px at @1× on the 1920×1080 board.

**No new sprite.** Each step is `ui_pixel` (4×4 white-on-alpha, "rules,
spines, keylines — tint anything") stretched to the step rectangle and tinted.
Empty steps are the same sprite at the empty ink. This is why the pack ships
zero new art.

**Inks.** Filled `#2b2620` (inkText) · empty `#cec0a2`. Never recoloured per
level: the scale is ordinal, and length carries it. A per-level hue would
re-spend the colour budget that status and category already hold.

**Levels.** MINOR 1/4 (±3–6) · MODERATE 2/4 (±7–14) · MAJOR 3/4 (±15–22) ·
SWEEPING 4/4 (±23–30). Filled steps count from the left.

| context | step | gap | run | label |
|---|---|---|---|---|
| ledger row | 7 × 12 px | 2 px | 34 px | Courier Prime 9 px, tracking 0.1em, `#5d564a`, fixed 56 px box, 7 px before the steps |
| detail pane | 11 × 16 px | 3 px | 53 px | serif 14 px bold, tracking 0.06em, `#2b2620`, preceded by a mono 9.5 px MAGNITUDE label at 12 px gaps |

The row's magnitude cell is 132 px wide (56 label + 7 gap + 34 run leaves
35 px of air before the approval column — deliberate, so the run never abuts
a right-aligned numeral).

**Ledger grid, for reference.** `26px minmax(0,1fr) 128px 132px 74px`, 12 px
gaps, 14 px side padding — declared identically on the sticky header and every
row. `minmax(0,1fr)` not `1fr`: a plain `1fr` cannot shrink below the name
cell's min-content and the ledger overflows horizontally. The scroller carries
`scrollbar-gutter:stable`, so header and rows resolve against the same width.

## Open on the engineering side

- **The citation record.** The board draws Portugal, Law 30/2000 / CONFIRMED
  in the slot for Drug Possession Decriminalisation. It is a real policy and
  correctly labelled, but it is a **placeholder for the slot** — verify
  against `LawCatalog`'s own comment for that law and use the recorded
  source. The slot is Design's; the record is not.
- Row content for the five unpopulated categories is untested — the board is
  drawn against one category and designed for ~300 rows across six.
