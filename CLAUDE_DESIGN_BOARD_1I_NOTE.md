# Note to Design — Screen 1i (the law browser board) shipped

**A courtesy update, not a request. Nothing here needs an answer** — it closes
the loop on the one board you delivered against a rulings doc rather than a
brief, so you can see what it became. Written for sending when convenient (the
E2 convention: sending is Elias's).

## What shipped

**Screen 1i is built and in the game, against `LAW_BROWSER_BOARD_RULINGS.md`
as delivered — the three drawn answers intact, none re-litigated:**

- **Row weight — four cells, glyph as gutter.** Status glyph · name (14px
  bold, full weight) · category (dimmed mono, narrow) · magnitude · cost. The
  six-dial breakdown, the citation and the live estimate are detail-pane only,
  exactly as ruled.
- **Status by grouping, not a column.** IN FORCE first, then BEFORE THE HOUSE,
  then AVAILABLE, each with its own count. The row kept its four cells; what is
  in force is the first thing on the board.
- **Magnitude as the four-step stepped rule.** Filled steps in `#2b2620`, empty
  in `#cec0a2`, no new hue, length carrying the ordinal — MINOR/MODERATE/MAJOR/
  SWEEPING. No new sprite (the step is `ui_pixel` tinted, per the spec's own
  note); the pack shipped zero new art, as its manifest promised.

**The board answered its own capture-evidenced failure structurally.** §7's
populated capture showed the top two rows both un-enacted with the 8 in-force
laws scattered below the fold and no way to sort by status — the specific
problem the request said the board had to solve. The shipped board opens on
`IN FORCE — 8`: the enacted laws are the first thing on screen (see the
populated-browser capture below), not something to scroll for.

## §7 is closed on this side

The request itself (§7 of `CLAUDE_DESIGN_ASSET_REQUEST.md`) is marked
**overtaken by events** — it was written but never sent, because the board and
its rulings doc arrived first and the browser was rebuilt against them the same
day. Its four cited captures were superseded by that rebuild. Nothing there is
pending on you.

## Captures (the built board, both sizes)

From `../PoliSim-captures/`:

- `panewidth1600f_06f_policylaws_laws.png` · `panewidth2560final_06f_policylaws_laws.png`
  — the board at both sizes, the list+detail split with the detail pane's
  final width.
- `fiscal2560s_85g_bill_laws.png` — the **populated** state (8 in force, 2
  before the house, 50 total): the IN FORCE grouping doing its job, the exact
  failure §7 documented now solved. (`fiscal1600s4_85g_bill_laws.png` is the
  1600 pair.)

These are reference material, out of tree beside the other captures — no import
on your side, nothing to reconcile.

---

*One thing the board deliberately did NOT build, recorded so it doesn't read as
an omission: the "next sitting date" from the delivered board. Parliament here
has no shared sitting calendar — every bill resolves on its own independent
day-countdown, so there was no real concept to surface, and inventing one would
be a number dressed as researched. The rest of the board is as drawn.*
