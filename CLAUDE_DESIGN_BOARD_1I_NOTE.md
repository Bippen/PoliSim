# Note to Design — the law browser, the almanac sheet, the graph weights, the Desk and the rail: 1i–1n shipped

**A courtesy update, not a request. Nothing here needs an answer** — it closes the loop on six
screens end to end: Screen 1i built against your rulings doc, the §7.1 density finding you answered
with Screen 1j and 1j implemented the same day we read it, the two boards that answered §2 and §3
of our 2026-08-27 request — **1k Calendar panel board** and **1l Graph weight ruling** — built as drawn
in the omnibus pass of 2026-08-28, and the two v3.0 boards — **1m Screen 0, The Desk, folded** and
**1n the rail** — built the day they landed. Written for sending when convenient (the E2 convention:
sending is Elias's). *Rewritten 1i–1n-aware 2026-08-28 (v3.0 Phase C); the 1i–1l note (2026-08-28),
the 1j-aware note (2026-08-26, corrected 2026-08-27) and the original 1i-only note (2026-08-25) are in
git history.*

## Screen 1i — built as delivered (2026-08-25)

**Against `COMPLETED.md` §182 as delivered — the three drawn answers intact, none
re-litigated:** four cells with the glyph as gutter, the six-dial breakdown / citation / live
estimate detail-pane only, exactly as ruled; status by GROUPING, not a column — IN FORCE first,
so what is enacted is the first thing on the board (the §7 capture's own failure, solved
structurally); magnitude as the four-step stepped rule, no new hue, no new sprite.

## Screen 1j — your §7.1 answer, implemented same-day (2026-08-26)

We read **`1j Law browser at 50`** from your live `PoliSim v2 Screens.dc.html` and built it as
drawn: the category chips step down (the count moves into the summary line), the ORDER control
and a real search slot take their place, **AVAILABLE renders as four magnitude BANDS** with
three-cell rows (the stepped rule promoted from forty repeating cells to four band captions),
the category cell retired until a second category shipped — which it did the same afternoon
(Labor Market, 50 laws; the chip row and category cell returned exactly as 1j promised, and the
filter genuinely narrows for the first time), pending rows carry their real VOTE-IN
countdowns, and the detail pane gains the kicker, the band range, the two-column IF-ENACTED dial
grid, and per-party stance rows from real seat data.

**Three deviations, stated rather than silent:** neutral ink on the dial arrows (a standing
ruling here: a dial's sign carries no value judgment the model makes); no seat headcounts (the
model has none — your own pass-3 **D2** ruling struck them; stance sign + real seats show
instead, with the lean bar staying the decided quantity); no "next sitting" (no shared sitting
calendar exists — every bill resolves on its own independent day-countdown, so the date would be
a number dressed as researched; the countdowns are the real datum in that slot). Your sticky
band headers became plain caption rows — IMGUI has no sticky inside a scroller, the same stated
adaptation 1i's column header took.

**Since then, on our side only — and now converged on your grid (2026-08-28):** the rows are one-line,
built to board 1i's proportion (your 32 px pitch on a 14 px bold name, 2.29 name-fonts per row; ours
2.0–2.3 at each window size on our px basis), and the density was measured on film against the
two-line row we had: 3 → 5 laws in the viewport at 1280×720, 5 → 8 at 1600×900, 7 → 11 at 2560×1440.
The name keeps full weight and shrinks rather than truncates where our narrower list cannot hold a
long statute name at full size; the selected law's name in the detail pane wraps at word boundaries;
the pane's content is sized to its own viewport. None of it touches your three rulings.

## Screens 1k and 1l — your §2 and §3 answers, built as drawn (2026-08-28)

**1k, the calendar panel as one almanac sheet.** Your five answers, built: the " X" suffix retired
for one diagonal ink stroke through the spent day's numeral (1.5 px at 1600 / 2 px at 2560, 55 %
ink, −24°, one length for every numeral so the strikes read as a set); the dots-vs-ledger split
stands, with the ledger row repeating the grid's own dot; a day at the four-dot cap earns the 2 px
underline beneath its dot row; header, month page and ledger as one sheet with 1.5 px section
rules, not three cards; the flip stays instant. No sprite was requested and none was made —
`ui_calendar_pad` and the locale honesty are untouched.

**1l, the weight order.** Built exactly as ruled — R-G1 history 3 buffer px · R-G2 projection 2 px
at 3-on / 2-off dashes · R-G3 the threshold hairline 1 px amber, unchanged · R-G4 sparklines
`max(2, round(h/34))` · R-G5 the 300×90 buffer stands; release ticks at weight + 2; the direction
deltas, the PRELIMINARY badge and the 1 px revision frame do not move. Your finding — that history,
projection and threshold had landed within a device pixel of each other at 2560 — is what the
build answers: on four stacked graphs at 2560, history now plainly outranks the amber reference.

Your rulings doc stays at our repo root unchanged as delivered, with two dated lines: the
2026-08-26 pointer (1j overlays its AVAILABLE-row spec) and a 2026-08-28 line naming the current
capture sets and the open one-line-row call.


## Screens 1m and 1n — your v3.0 boards, built the day they landed (2026-08-28)

**Thank you for the same-day boards.** The ask went out in the morning with its three annexes and the
two boards were on the live screens file by the afternoon — drawn against the census and the measured
minimums rather than around them, with the split argued on the board itself. That let Screen 0 be
built, filmed at four sizes and pushed the same day.

**1m, Screen 0 — built as drawn.** The placement is the board's own: the sheet's 1118×660 inner area
at 1280×720 with the masthead, the three columns (420 / 240 / 425), the map over the approval ledger,
the compass over the effects card, the calendar sheet over the event card, the chip strip — scaled by
the sheet's ratio at the other sizes, so at 1280×720 the stage is your frame. All seven declared
deviations stand as you drew them (the split; no active spine on Screen 0; the compass's captions inside
its rect — our renderer's own footprint since the stage-prep pass does exactly that; C20 as a mono
caption; the speed cluster on the masthead; approval as a hero numeral over the nine-term ledger, no
dial invented; neutral ink on the lines). The (b) resolutions landed as you resolved them: the event's
three effects as bars on the card, "This Month" and the empty sentence dropped, the horizon control
carrying its own label, the lamp carrying the running state, the rail's icons carrying the labels.

**Three calls the build made, stated rather than silent — and now standing on both sides** (your board
carries them as "standing corrections from the build, accepted"; our direction doc ratified them the
same day):

- *The ways home.* The board draws no way back to the Desk from a document. Ours: the rail's calendar
  chip (the sheet collapsed, so its click opens the sheet's home) and the open document's own rail icon
  clicked a second time. Nothing was added to the rail; the two existing cells gained the behaviour.
- *Two rows refused.* The effects card draws eight figures, not ten: your debt-to-GDP and currency rows
  are not estimates the game computes — the preview projects GDP growth, inflation, unemployment,
  approval, poverty, participation, crime and the net budget, and nothing else. Printing the two would
  have been a number the model does not hold, so the refusal is the model's honesty, not a layout
  preference. If those estimates are ever wanted they are a simulation feature with its own measurement
  pass, never a UI patch — recorded as such.
- *The FOLDED lock.* Screen 0 has one legal state. The chrome column's contents (the calendar sheet, the
  speed cluster, the hold banner) live on the stage, so an OPEN Desk would show them twice and squeeze
  the stage into a space it cannot fit; the toggle draws on its disabled face there, the way the Budget
  ledger's does.

**Two things you could not see from the board, both ours:** the event card is filmed with an event from
the game's own pool (staged by the harness for one frame, then restored — nothing was written for the
film), and the game-over stamp with the reason string the game itself prints. And the chip strip's
sparklines run through the same renderer as the Statistics graphs, so your 1l weights already reach them
— the "one engineering constant if wanted" turned out to be in place.

**1n, the rail — built as the re-skin it was.** The derivation untouched (the icons' 24-unit grid plus
10 units of air: 39 / 46 / 55 / 64 px at the four sizes, as before); the air moved as drawn — the nav
block top-anchored under the sheet's cap, the utility block (chip · lamp · toggle) bottom-anchored with
one breathing gap between; the active cell's 3 px spine at the left edge, full cell height, in the area
ink, with the 12 % area-ink wash behind it; inactive cells in the tab-swatch tint; the hairline between
the chip's month and day; the lamp's two states carried as before; no spine on Screen 0. Nothing added,
nothing asked.

**Since then, on our side only:** the fold-default table is now ruled — a screen defaults FOLDED only
if its content is designed for the full-width stage, which today is exactly the Desk and the Budget
ledger (both locked); every document defaults OPEN, so Statistics › Domestic, which had carried the
landing duty as a stand-in before the Desk existed, opens with its column again.

## Captures — the current reference sets

From `../PoliSim-captures/` (USA, at 1280×720, 1600×900, 1920×1080 and 2560×1440). The earlier
`board1jc*` sets — the 1j build's first captures — are superseded and kept only as history:

- `v3desk_<size>_01c_desk.png` — Screen 0 as the game lands on it (RUNNING, turn 0: the lamp green,
  the cluster live, the ledger before its first period); `v3desk_<size>_01d_desk_held.png` — the
  warmed-up game (HELD above the masthead, the lamp amber, the speed faces disabled, the ten-row
  ledger, the strip's sparklines); `v3desk_<size>_01e_desk_event.png` — the event card filled;
  `v3desk_<size>_01f_desk_gameover.png` — the stamp over the dimmed stage. The 1280 frame is your
  board's own size.
- the rail on any `v3desk_<size>_0[2-7]*` capture — the active cell's spine and wash (1n) — and on
  `01c_desk`, without a spine.
- `omni_final_<size>_06f_policylaws_laws.png` (`_rows` / `_deep` scroll variants beside them) — the
  law browser at a hundred laws across two categories: 1i's grouping and stepped rule, 1j's bands,
  chips, order control and search slot, the detail pane.
- `omni_final_<size>_06g_laws_expected_effects.png` — the detail pane's "Expected effects" band
  (a post-1i addition: each law's long-run stat shifts derived from the declared coupling table the
  simulation itself reads — neutral, no authored valence, the model's coupling gaps left visible
  by ruling).
- the left panel of any `v3c_<size>_02_statistics.png` or `omni_final_<size>_0[2-7]*` capture — the
  almanac sheet (1k): the strikes, the ledger dot, the underline, the section rules; on Screen 0 the
  same sheet stands in the third column.
- `omni_final_2560_02a_statistics_domestic_deep.png` — four stacked graphs at 2560, the weight
  order (1l).

These are reference material, out of tree beside the other captures — no import on your side,
nothing to reconcile. (⚠ CORRECTED 2026-08-31 at C-F1: the rasterization diff's open pipeline item — the hatch tile's
source — is NOT open. §E5 CLOSED END-TO-END on 2026-08-28, both sides, after Elias's same-night ruling;
the sentence below said otherwise for three days and is struck. ~~The one open pipeline item from our half
of the rasterization diff — the hatch tile's source, one re-cut away — is a separate, small ask, filed in
the request doc as §E5 — not part of this note.~~)
