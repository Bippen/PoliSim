# Note to Design — the law browser, the almanac sheet and the graph weights: 1i–1l shipped

**A courtesy update, not a request. Nothing here needs an answer** — it closes the loop on four
screens end to end: Screen 1i built against your rulings doc, the §7.1 density finding you answered
with Screen 1j and 1j implemented the same day we read it, and the two boards that answered §2 and §3
of our 2026-08-27 request — **1k Calendar panel board** and **1l Graph weight ruling** — built as drawn
in the omnibus pass of 2026-08-28. Written for sending when convenient (the E2 convention: sending is
Elias's). *Rewritten 1i–1l-aware 2026-08-28; the 1j-aware note (2026-08-26, corrected 2026-08-27) and
the original 1i-only note (2026-08-25) are in git history.*

## Screen 1i — built as delivered (2026-08-25)

**Against `LAW_BROWSER_BOARD_RULINGS.md` as delivered — the three drawn answers intact, none
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

## Captures — the current reference sets

From `../PoliSim-captures/`, the omnibus closing matrix of 2026-08-28 (`omni_final_<size>_…`, USA,
at 1280×720, 1600×900, 1920×1080 and 2560×1440). The earlier `board1jc*` sets — the 1j build's first
captures — are superseded twice over and kept only as history:

- `omni_final_<size>_06f_policylaws_laws.png` (`_rows` / `_deep` scroll variants beside them) — the
  law browser at a hundred laws across two categories: 1i's grouping and stepped rule, 1j's bands,
  chips, order control and search slot, the detail pane.
- `omni_final_<size>_06g_laws_expected_effects.png` — the detail pane's "Expected effects" band
  (a post-1i addition: each law's long-run stat shifts derived from the declared coupling table the
  simulation itself reads — neutral, no authored valence, the model's coupling gaps left visible
  by ruling).
- the left panel of any `omni_final_<size>_0[2-7]*` capture — e.g. `omni_final_2560_02_statistics.png`
  — the almanac sheet (1k): the strikes, the ledger dot, the underline, the section rules.
- `omni_final_2560_02a_statistics_domestic_deep.png` — four stacked graphs at 2560, the weight
  order (1l).

These are reference material, out of tree beside the other captures — no import on your side,
nothing to reconcile. (The two pipeline findings from our half of the rasterization diff are a
separate, small ask, filed in the request doc as §E5 — not part of this note.)
