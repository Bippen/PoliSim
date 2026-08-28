# PoliSim v2.0 — Screen Specification

*Extracted 2026-08-10 from Claude Design project `b3dec27b`, file `PoliSim v2 Screens.dc.html`
(treatment A, "ministry fresh print" — the eight boards of pass 3 at 1920×1080; Screens 1i and 1j, the
law browser, followed on 2026-08-25/26 and are specified in `LAW_BROWSER_BOARD_RULINGS.md`). **Revised
to pass 3, 2026-08-10; consolidated 2026-08-27.***

⚠ **This is a specification, not source to port.** The delivered artifact is HTML/CSS; PoliSim renders
in IMGUI and Canvas. Every number below is design intent restated in terms this project can implement.
Nothing here was translated from markup — the markup was measured and discarded.

**Status at 2026-08-27 (the third consolidation).** This file is the v2.0 visual REFERENCE — the
conventions the code cites by section (`LedgerRow.cs` §A.9, `GameController.cs` §A.9/§A.11/§A.12) and
the spec of the one screen not yet built, **1h election night (item-10-gated, `MISSING_PREREQUISITES.md`
§D)**. Its history — the nine pass-3 dispositions, the seven re-review findings (every one closed) and
the "what is buildable" list (every item built) — is `COMPLETED.md` §24 ("The screen spec's finished
sections"); those sections are removed here rather than left reading as open work. Where the build
deviated from a number below, the deviation is stamped in place; where a clause is still unbuilt, it is
marked **UNBUILT — roadmap item 4** and lives on `POLISIM_MASTER_ROADMAP.md`. Every standing rule in
§A governs any new screen regardless.

---

## ⚠ EVERY NUMBER IN THIS DOCUMENT IS SUSPECT UNTIL DERIVED OR CONFIRMED

**Read this before copying any measurement below.** Two instances in one day, in the same file, the
second written a method away from the warning about the first:

| # | number | copied as | actually was |
|---|---|---|---|
| 9 | ledger row height `36px` | a row height | a height at 1080p — two 13px lines overflow it at 1440p |
| 10 | column widths `250 / 150 / 88` | fixed widths, font-scaled | widths against board 1b's ~1100px panel — they overflow this screen's ~745px column |

**A number on a design board is a measurement taken against that board's conditions — its resolution,
its font size, AND its container width.** Every one of those varies here. Deriving against one axis
while copying another still ships the bug, which is exactly how #10 happened immediately after #9 was
written down.

**So: paddings, insets, tick sizes, the `14px` scrollbar track, the `6px` inset, the `40px` minimum
thumb, the `11px` shrink floor, the `44/26/22px` panel paddings, the `2px` standing tick, the `15px`
track height — none of them is a constant until someone decides it is.**

✅ **"This one is genuinely fixed" is a real answer, not a dodge.** A hairline rule should not scale; a
1px separator at 1440p is still 1px. The requirement is that it be a *decision taken*, recorded where
the number is used — not a number that survived because nobody questioned it. The tell in both
instances so far: the number arrived with no unit of comparison attached, so it read as a constant
purely because the board had nothing else in view to measure it against.

---

## A. THE EXTRACTED SPEC

### A.0 What the boards cover — the eight of pass 3 (2026-08-10); 1i/1j followed (the law browser, 2026-08-25/26)

| board | screen | side | shows |
|---|---|---|---|
| 1a | Persistent chrome + Statistics : Domestic | IMGUI | left column, tab strip, time-**held** state |
| 1b | Budget, full-screen (left column hidden) | IMGUI | the density case: ledger + bill rail |
| 1c | Politics : Parliament | IMGUI | time-**running** state, legend↔chart ink |
| 1d | Decisions | IMGUI | dossier cards + the dual-siting rule, both variants |
| 1e | The hand-off envelope | swap | 5-panel storyboard + timeline |
| 1f | Country selector | Canvas | six folder cards, hover state |
| 1g | Bill vote → budget signing | Canvas | the consequential document |
| 1h | Election night returns | Canvas | the returns bulletin — **NOT BUILT, item-10-gated** |
| 1i / 1j | Law browser; law browser at 50 | IMGUI | `LAW_BROWSER_BOARD_RULINGS.md`; both built |

Mockups run mid-game (Turn 9, May 2026) so drafts, deltas and a time-hold are all visible at once.
Chart marks inside plates are the procedural renderers' output, shown for context — **not deliverables**
(standing rule 10).

### A.1 Surfaces — the exact ladder

The design commits to a strict five-step surface ladder. Depth is carried by *value*, never by hue.

| step | role | value | notes |
|---|---|---|---|
| 0 | desk (deep) | `#14100B` | Canvas takeover ground only |
| 0 | desk | `linear 155° #2E2318 → #241B10 @50% → #1C150C` | every IMGUI screen's ground |
| 1 | closed folder stock | `#B9A886` (hover `#C4B28E`) | inactive tab tongues only |
| 2 | paper (panel) | `linear 178° #F1E8D9 → #ECE2CF @62% → #E7DCC6` | every outer panel |
| 3 | plate (inner panel) | `#F4ECDC` | panels *inside* a paper panel |
| 3 | tile | `#EDE2CB` | stat tiles, dossier bodies |
| 4 | plate (control) | `#E7DCC4` | inactive buttons, sub-tabs |

Desk grain: `repeating-linear 93° rgba(255,235,200,0.018) 0→2px, transparent 2→9px`.
Desk vignette: `radial 130% 100% at 50% 20%, transparent 50% → rgba(0,0,0,0.4) 100%`.
Canvas ground uses a coarser 45°/16px grain and a tighter `at 50% 45%` vignette to `rgba(0,0,0,0.55)`.

**Both are baked into `ui_grain_tile` / `ui_scrim_takeover`; neither is a runtime effect** (§3.2).

### A.2 Inks and rules

| token | value | use |
|---|---|---|
| `inkText` | `#2B2620` | all primary type; the 2px heading rule |
| `inkFaint` | `#5D564A` | labels, secondary body, caption type |
| `rule` | `#B7A98C` | light separators, header underlines |
| `ruleHeavy` | `#8A7A5C` | section rules (1.5px), plate borders |
| `ruleRow` | `#D5C8AB` | ledger row separators (1px) |
| `borderPaper` | `#CBBC9D` | paper panel edge |
| `borderPlate` | `#C9BA9B` | plate / tile edge |
| `textOnDesk` | `#E8DDC4` — **ships `#F0E7D8`** (`PoliSimTheme.TextOnDesk`, a declared deviation, 2026-08-27) | type on the dark banner |
| `mutedInk` | `#9A917D` | disabled row ink and disabled button text |
| `deskCaption` | `#8D7D5F` | mono annotations on desk ground |

Brass: `#B3985E → #9C8148` gradient, border `#6F5A30`, text `#F4ECDC`.

⚠ *2026-08-27: three of the ten tokens have no constant anywhere in the repo — `ruleRow` (`#D5C8AB`,
the row-separator weight) among them; re-derive the list against `PoliSimTheme.cs` before building.*
**UNBUILT — roadmap item 4.**

**Rule weights are semantic and consistent across all eight boards:**
`2px #2B2620` = screen/section heading rule · `1.5px #8A7A5C` = panel section rule ·
`1px #B7A98C` = column-header rule · `1px #D5C8AB` = row separator ·
`3px double #8A7A5C` inset 8–14px = the ornate frame (`ui_frame_double`).

### A.3 The eleven hues — three tints, all delivered; two wired

Columns 1 and 2 are wired (`UiPalette`'s area inks, `PoliSimTheme.AreaAccentsOnDesk`); the boards use
them unchanged. **The third column — the inactive tab-swatch tint — is delivered and NOT wired** (the
tab swatch draws the area ink at HEAD; `polisim_palette.json` is Design's file, not one in this repo).
**UNBUILT — roadmap item 4** (2026-08-27).

| area | ink (on paper) | lifted (on desk) | tab swatch (on stock) |
|---|---|---|---|
| Neutral | `#6D7480` | `#9AA1AD` | — |
| Fiscal | `#35619E` | `#7C9CC9` | `#3D6494` |
| Trade | `#23867B` | `#5FA89E` | — |
| Political | `#A8842E` | `#C9A855` | `#96762A` |
| Welfare | `#A84E7B` | `#C27E9F` | — |
| Labor | `#B5622F` | `#C98A5E` | `#A2653E` |
| CrimeJustice | `#9C4238` | `#BC7168` | `#8E4A40` |
| Sectors | `#62579F` | `#9288C2` | `#5B5187` |
| Infrastructure | `#3E7480` | `#7BA3AE` | — |
| SovereignWealth | `#85643A` | `#B0925F` | — |
| Global | `#5C87A8` | `#8FAEC7` | `#4E7291` |

The third column is the **inactive tab swatch tint**, delivered in full by pass 3 as `tabTint.*`. Its
derivation, for anything that needs to generate one: **ink at oklch chroma ×0.78, lightness ×0.97**,
snapped to hex. The table above carries the snapped values, so the rule is documentation rather than
something to compute at runtime.

**Semantic:** draft `#BE8A00` (lifted `#D4A72C`) · good `#3E8A5F` (lifted `#6FB08A`) ·
bad `#9C4238` (lifted `#BC7168`) · caution = draft.

### A.3a Party inks — their own set, not the area inks

Pass 3, D5. Cut deliberately in hue space the eleven areas do not occupy (wine / petrol / drab khaki /
sage), and keyed to the **real** `PartyArchetype` members. **A party may never print in an area accent
or in semantic good/bad.** Hemicycle arcs, legend swatches and swing figures all key this set, and the
legend swatch is the arc's own ink (B9).

| `PartyArchetype` | ink | lifted |
|---|---|---|
| ProgressiveAlliance | `#7E3557` | `#A2607F` |
| ConservativeUnion | `#2F4E63` | `#6B87A0` |
| CentristCoalition | `#77714A` | `#9E9873` |
| NationalistFront | `#4E5A45` | `#7E8A73` |

Verified against the wired tables: none of the four collides with any of the eleven area inks, either
tint, or `good` / `bad` / `draftAmber`.

### A.3b Categorical series — eight, and a hard cap

Pass 3, D4. Replaces `UiPalette.GetCategoricalColor`'s golden-angle HSV walk. Assign **in series order**:

`#9C5233` · `#8A7A2C` · `#5C7434` · `#2F7458` · `#2E6E7E` · `#46608F` · `#745394` · `#95517A`

⚠ **Eight is a hard cap, not a palette that repeats.** A series longer than eight **must not be
hue-keyed at all** — it changes chart form to a **ranked single-ink bar ledger in the owning area's
ink**. Verified against the actual series lengths:

| series | length | treatment |
|---|---|---|
| `SectorType` (employment pie) | **8** | at the cap — stays hue-keyed |
| `TaxType` (revenue pie) | 13 | converts to ranked bar ledger, Fiscal ink |
| `SpendingCategory` (spending pie) | 29 at pass 3 — **46 at HEAD**, same treatment | converts to ranked bar ledger, Fiscal ink |
| `PartyArchetype` (hemicycle) | 4 | keys `parties.*`, never this set |

The ranked bar ledger satisfies B9 trivially — one ink, and every row carries its own inline label, so
there is no legend to disagree with the chart. The implementation consequence (the palette must FAIL
past eight, never wrap) shipped as a throw in `UiPalette.GetCategoricalColor` (`a7bd40d`).

### A.4 Typography — role assignments

TeX Gyre Pagella throughout; Courier Prime is reserved and its reservation is *narrow*.

| role | size | weight / tracking |
|---|---|---|
| wordmark (selector) | 54 | bold, ls `.04em` |
| screen title (Canvas doc) | 30–32 | bold |
| panel title | 16–17 | bold, ls `.03–.04em` |
| **section head** | 10–12 | bold, ls `.13–.20em`, usually `inkFaint`, always CAPS |
| body | 12.5–15 | regular |
| ledger name | 13 | bold |
| ledger value | 12.5–13 | regular, right-aligned |
| ledger total | 16 | bold |
| tile label | 8.5–9.5 | bold, ls `.12–.13em`, CAPS, `inkFaint` |
| tile value | 19 (left col) / 24 (main) | bold, lh 1.15 |
| tile delta | 10–11 | bold, good/bad ink |
| hero figure | 26–32 | bold |

**The letterspaced CAPS section head is the single strongest carrier of the idiom.** It appears on every
panel on every board. It is what makes a data panel read as a government form rather than a dashboard.

**Courier Prime is permitted only for:** document identifiers (`H.R. 2027-1`, `Division No. 215`), dates
and timestamps, reference-period notes, margin/footnote annotations, and the `annualized` / `live` status
words. **Never for a figure the player reads as data** — those are Pagella, for B10 (lining figures).

### A.5 Screen frame and the two column layouts

Screen padding `20px / 24px / 18px`. Column gap `22px`.

- **Standard tabs:** grid `430px | 1fr` — chrome left, content right.
- **Budget full-screen:** grid `1fr | 430px` — ledger left, bill rail right, **left column hidden**.
  The 430px measure is held constant so the eye does not have to re-acquire it.

⚠ **Declared deviation (2026-08-27):** the build ships a FRACTION, not a fixed measure —
`GameController.LeftColumnWidthFraction = 0.45f` of the content area (≈ 691 px at 1600×900, ≈ 1106 px
at 2560×1440), and the Budget full-screen hides the column outright. The "held constant" intent is
served by the fraction being one constant; nothing re-derives 430px.

⚠ **UI v3.0 (2026-08-28, `POLISIM_UI_V3_DIRECTION.md` V3-R2): the two column layouts are the two
states of ONE shell fold.** OPEN is the standard grid above, exactly; FOLDED collapses the chrome column
and the tab tongues to one icon rail (`GameController.DrawFoldedRail`: the six nav/area icons as
navigation — active in area ink behind a spine, inactive in the tab-swatch tint — then, pinned at the
bottom, the calendar chip from the pad's own materials, the status dot carrying B8's two states with the
HELD glow, and the fold toggle; nothing else) and the content column takes the rest. Budget full-screen is
no longer its own mechanism: it is the Budget ledger's FOLDED default, so "left column hidden" now reads
"the rail" — a cell of the icons' own 24-unit grid plus 10 units of air each side (`icon × 44/24`: 55 px
at 1080p, 39 px at 720p, 64 px at 1440p, plus the sheet's padding). Per-screen defaults: the landing
screen (Statistics › Domestic, carrying the oversight default until Screen 0 exists) and Budget FOLDED;
every other screen OPEN. The flip is instant; the player's choice persists per screen and per save
(`UiDraftState.ShellFoldOverrides`). Every screen is legal in every reachable state (R-SP2, 2026-08-28 —
the guards run in each; a locked state is unreachable and recorded per screen: the Budget ledger, R-A1),
the folded frame's interrupt banner carrying the HELD reasons above the sheet; only defaults are canonical
on film (V3-R4).

### A.6 The left column (persistent chrome)

⚠ **As built (2026-08-27):** items 1 and 3 moved by a recorded decision — the **Calendar Panel**
(`a13dd7b`, 2026-08-24) replaced the country header + headline tile grid in this slot, the tiles
relocated via `DrawHeadlineTiles`; items 4 and 6 are live in this slot. The order below is the pass-3
board, kept as the reference the Calendar Panel board request (`CLAUDE_DESIGN_ASSET_REQUEST.md` §2)
iterates against.

Vertical flex, gap `12px`, in fixed order — item 5 is a `flex:1` spacer that pins 6 to the bottom.

1. **Country header** — paper panel, padding `14/18`, flex gap 14. Flag `52×34`. Name `21 bold ls .02em`;
   beneath it `11.5 inkFaint` "Turn 9 · the President's desk". Right rail, mono `10 #8A7A5C`, three lines:
   `REPUBLIC / STANDARD / FORM 1-A`.
2. **Event / game-over banner** — paper panel, **border-left `6px` in the event's area ink**, padding
   `10/14`. Kicker `10 bold ls .16em` in that same ink (`EVENT — LABOR`); mono `10 #8A7A5C` right
   (`3 turns remain`). Title `14.5 bold`. Body `11.5 inkFaint`.
3. **Stat tiles** — 3 columns, gap `8`. Tile: `#EDE2CB`, `1px #C9BA9B`, **top `3px` in area ink**,
   shadow `0 3px 7px rgba(0,0,0,.3)`, padding `7/10/8`.
4. **Policy preview** — paper panel, padding `14/16`. Head row: `THIS TURN'S POLICY` (`11 bold ls .18em`)
   / `✎ 3 DRAFTS OPEN` (`10 bold ls .08em` draft amber), rule `1.5px #8A7A5C`. Then `ESTIMATED EFFECTS`
   (`10 bold ls .14em`), the **four horizon buttons in a 2×2 grid**, gap 6 — active brass, rest plate.
   Effect rows `12.5` with `1px #D5C8AB` separators, value in good/bad ink with a ▲/▼ prefix.
   Footnote, mono `9.5 #8A7A5C`: the ±5–10% margin-of-error text.
5. spacer.
6. **Calendar + status + speed** — paper panel, padding `12/16`, flex gap 16.
   - Calendar pad, `64px` wide, `#F4ECDC` on `1px #C9BA9B`, shadow `0 2px 5px`: month `8.5 bold ls .2em`
     in `#9C4238` over a `1px #D5C8AB` rule; day `26 bold`; mono `9 inkFaint` `2026 · T9`.
   - Status line — **two states, and this is B8's carrier:**
     - **HELD:** `#241B10` on `1px #0E0A06`, padding `6/10`. Dot `8px #D4A72C`, glow
       `0 0 6px rgba(212,167,44,.7)`. Text `10.5 #E8DDC4`, **the resolving screen named in `#D4A72C`**.
     - **RUNNING:** `#EDE2CB` on `1px #C9BA9B`. Dot `8px #3E8A5F`, no glow. Text `10.5 #3D372E`.
       **UNBUILT — roadmap item 4** (only the HELD half is dressed, `DrawHoldBannerLabel`; the running
       branch is a bare label).
   - Four speed buttons, gap 6, equal flex. Active brass; inactive plate; **when time is held all four
     non-Pause buttons render `#DDD2B8` / `1px #C9BA9B` / text `#9A917D` — rendered, never omitted (B5).**
     **UNBUILT — roadmap item 4** (`DrawSpeedButton` keys on `selected` only; `ui_btn_disabled` has no
     loader).

### A.7 The tab strip

Gap `6`, padding `0 14px`, aligned to bottom, tongues overlapping the panel by `−1px`.

| | active | inactive |
|---|---|---|
| background | `linear 180° #F2EADB → #EFE5D4` | `#B9A886` |
| border | `1px #CBBC9D`, none at bottom | `1px #8A7A5C`, none at bottom |
| top edge | **`3px` area ink** | **`3px` area lifted** |
| radius | `5 5 0 0` | `5 5 0 0` |
| padding | `10 / 22 / 13` | `8 / 18 / 9` |
| type | `15 bold` inkText | `14` `#45392A` |
| swatch | `11×11` area ink | `11×11` area *tab-swatch* tint |
| depth | `z 2`, shadow `0 −3px 10px rgba(0,0,0,.25)` | none |

The active tongue is physically larger, not merely lighter — it is a folder pulled forward. Count badge:
`#9C4238` fill, `#F4ECDC` text, `10 bold`, radius `9`, padding `0 6`.

### A.8 The content panel and sub-tabs

Content panel: paper gradient, `1px #CBBC9D`, shadow `0 16px 34px rgba(0,0,0,.55)`, padding `22/28/24`.

Sub-tab row sits directly under, closed by a `2px #2B2620` rule with `10px` of clearance:
- **active:** `#E7DCC4`, `1px #8A7A5C`, **bottom `3px` area ink**, `13.5 bold`, padding `5/18`
- **inactive:** transparent, **bottom `1px #B7A98C`**, `13.5 inkFaint`, padding `5/18`
- **right-aligned screen caption**, `11 bold ls .18em inkFaint` — e.g.
  `DOMESTIC BULLETIN — DESK READINGS, LIVE`, `THE NATIONAL ASSEMBLY — 350 SEATS`.
  This caption is B6's live/published carrier at screen level. **UNBUILT — roadmap item 4** (nothing
  draws it at HEAD, 2026-08-27; §A.8a's "live desk reading" state is defined as sitting under it).

Inner plates: `#F4ECDC`, `1px #C9BA9B`, shadow `0 3px 8px rgba(0,0,0,.2)`, padding `14/18`.
Plate head: `11 bold ls .16em` over a `1.5px #8A7A5C` rule with `6px` clearance; optional mono `10
#8A7A5C` right-aligned status word.

### A.8a Published vs live — B6, corrected

⚠ **The §1C.2 sentence is STRUCK.** It read *"published = printed bulletin (solid frame + ref period +
date + badge chip); live = desk reading (dashed rule, unbadged)"*, which conflated two independent
things and disagreed with every board. `DIRECTION.md`'s B6 row now carries the corrected rule; this is
that rule, so the two documents agree rather than contradict.

**Two orthogonal channels, and that is the whole point:**

| channel | keys | values |
|---|---|---|
| **badge chip + reference period + publication date** | **published-ness** | present = published · absent = live |
| **frame style** | **revision status** | dashed = preliminary/provisional · solid = final |

So a *preliminary published* figure is **badged, dated, and dashed** — which is exactly what board 1a
draws, and what the old sentence made impossible to express. A **live desk reading** is unbadged,
undated, and sits on a solid plate under the screen-level `— DESK READINGS, LIVE` caption.

The correction matters because the two states the old rule collapsed are the two a player most needs
told apart: *"this number is provisional"* and *"this number is not a publication at all."*

### A.9 The ledger row — the Budget screen's atom

**Redrawn in pass 3** against the screen as actually built — one `BudgetProcessCategory` at a time, all
29 spending programs (46 at HEAD; the count is pass 3's), both density levers taken. **Built `9705205`;
the measures live in `LedgerRow.cs` as measurements, and the row height is DERIVED from the font metric
— the 36px below is the value at 1080p (`COMPLETED.md` §24).**

Row height **`36px`** (was 44), separator `1px #D5C8AB`, grid **`250px | 1fr | 150px | 88px`**, gap `10`,
right padding `26px` to clear the scrollbar. The per-row `VOTES` column is **deleted** — no
per-instrument legislative support exists — and its width is what pays for the 250px name column.
Column header band: `8.5 bold ls .13em inkFaint`, closed by `1px #B7A98C`, padding `7/26/5/0`.
Totals: `1.5px #8A7A5C` above, label `10 bold ls .14em inkFaint`, value `16 bold`.

29 rows × 36px = 1044px (pass 3's arithmetic) against a viewport of roughly 22 rows, so **it scrolls,
and that is intended**.

#### A.9b The read-only row, and the figure with no denominator — ADDED 2026-08-11

Every read-only row this spec described was a **proportion**: a condition index out of 100, a share of
GDP, a seat share. `LedgerRow.DrawReadOnly` was written assuming one, and the Statistics conversion found
figures that are not — **GDP per capita is currency per person, with no ceiling to be a fraction of** —
and any figure not yet computed has no value at all.

⚠ **An empty track is NOT neutral. It reads as a gauge sitting at zero**, which is a confident wrong
number: worse than drawing nothing, and exactly the failure class this project keeps finding.

✅ **A NEGATIVE FILL MEANS "NO GAUGE" — draw nothing in the track lane.** It covers both cases, because
in both the honest statement is *there is no proportion here*, never *the proportion is zero*.

**This EXTENDS an existing convention rather than adding a second one.** `LedgerRow.Draw`'s `barFraction`
has meant "no bar" at negative since the Budget conversion. One idiom, two methods, one sign rule.

#### A.9c Where Parliament's row spec actually lives — CORRECTED 2026-08-11

⚠ **§A.10 is BUTTONS.** It has been cited as Parliament's reference and does not resolve there.
Parliament's legend rows are governed by **board 1c** and by the **D2 disposition** in the pass-3 table
below (*"verdict + seat-weighted lean bar; per-row `VOTES` deleted"*).

**Trailing column ruled 2026-08-11: the seat PERCENTAGE, with the seat count as the figure** — the same
split the Statistics sector rows take. D2 having deleted the per-row `VOTES` column is precisely what
leaves that column free for it.

⚠ **The legend's colour is load-bearing and survives the conversion by moving, not by staying.** It keys
each row to its own arc in the chart above; an emblem drawn *instead* of the swatch broke that once
already. `DrawReadOnly` draws its gauge in `barInk`, so the party hue now colours a bar that is also
proportional to the seat share — one mark carrying two readings where a solid swatch carried one.

**Scroll view treatment — every scroll view, not just this one** (applied globally on `GUI.skin`,
`GameController.StyleScrollbars`, so the per-view count is beside the point):
`ui_scrollbar_track_v` recessed *into* the paper (baked inner shadow), **`14px` wide, inset `6px` from
the paper's right edge**, full ledger height. `ui_scrollbar_thumb_v` brass, proportional, **minimum
length `40px`**, inset `1px` within the track. Arrow buttons per pass 2: `ui_scrollbar_button_none`
**and** `fixedWidth = fixedHeight = 0` with zero margins — both, or IMGUI still reserves the space.

**The in-row slider is the design's best single idea and it is B1's primary carrier:**

- Track `15px` tall, ticks as `repeating-linear 90° #B7A98C 0→1px, transparent 1→10%` (every 10%),
  closed by a `1.5px #8A7A5C` baseline.
- **Standing tick:** `2px × 21px` bar in `#2B2620`, at the enacted value, offset `−3px` above the track.
- **Draft band:** the span between standing tick and knob, filled with `ui_hatch_draft` tinted `#BE8A00`
  — 45° hatch, `rgba(190,138,0,.8)` 0→3px / `rgba(190,138,0,.28)` 3→6px. **Drawn in both directions**, so
  a cut and a rise are equally legible.
- **Knob:** `14×23`, radius 2, brass gradient, `1px #6F5A30`, shadow `0 1px 3px rgba(0,0,0,.35)`.
- **Locked row:** knob `#C9BDA3 → #B3A789` on `1px #9A917D`; the entire row's ink drops to `#9A917D`.
  The row is still drawn, still measured, still occupies its full row height (B5) — *44px was the
  pre-pass-3 value; the height is derived, see §A.9's opening note.*

Draft values appear inline as `standing ✎ draft`, the draft half in `#BE8A00` bold — on rows, on
subtotals, and on the bill rail.

⚠ **The pencil is GEOMETRY, never a font glyph.** Pass 3 settled this: no shipped font carries `U+270F`,
so the mark is `icon_pencil_draft` tinted `#BE8A00`, drawn inline immediately before the draft figure,
`11px` at body size (`12px` on totals, `9px` in column headers), rotated `−30°` as authored. The same
geometry is baked into `ui_stamp_draft` and the `✎ DRAFT` chip. Two riders that travel with it:

- **`▲` / `▼` are Pagella-only.** Present there, absent from Courier Prime — never set a delta arrow in
  the document face.
- **`⚠` becomes a printed `N.B.`** in shipped copy. It is absent from all three fonts.

### A.9a Names never clip — the resort ladder

Pass 3, D7. Agreed that nothing may clip, but **not by uniform auto-shrink**: a ledger column where
every row prints at a different size reads as an error rather than as a fit. Order of resort:

1. **Widen the fixed column** — 250px, paid for by the deleted `VOTES` column.
2. **Wrap generated names to two lines** — minister names, anything authored at runtime.
3. **Curated abbreviation table for enum names** — `Veterans Benefits Mandatory` → `Veterans Benefits —
   Mand.`. A table, so the abbreviation is chosen once and is stable between frames.
4. **Best-fit shrink, floor `11px`** — last resort only.

**The numeric variant — extended 2026-08-10 per `§C.2`.** Design's ladder is a *name* ladder, and two of
its four steps cannot apply to a figure: a number must not wrap (a money value broken across two lines
is unreadable, and worse, is briefly readable as a different number), and a number has no abbreviation
table (`MoneyUnit` tiering has already done that job by the time the string exists). So for every cell
that holds a figure the ladder is two steps, not four:

1. **Widen the fixed column.**
2. **Best-fit shrink, floor `11px`.** Never a wrap, never a table, and never a clip.

**This applies wherever a number appears, not only in ledger rows** — B4 is a class-level rule and its
original defect was numeric. The cells it governs:

| site | measure | holds |
|---|---|---|
| ledger `STANDING ✎ DRAFT` | `150px` | two money figures + the pencil sprite |
| ledger `SHARE` | `88px` | `6.5% GDP` |
| ledger totals | right-aligned, panel-width | `$4.73T ✎ $4.74T` |
| stat tile value / delta | ~`133px` tile (3-up in 430px) | hero figure + signed delta |
| legend seats / delta (1c) | `70px` / `56px` / `52px` | seat counts and swings |
| legend seats / delta (1h) | `60px` / `56px` | as above |
| country card figure strip (1f) | 3-up in card width | population · GDP · debt-to-GDP |
| calendar pad day | `64px` | day number |

None of these is demonstrably broken today — `MoneyUnit` tiering bounds a money figure to about six
characters, so `150px` is comfortable. The point is that it is comfortable **by assumption** rather than
by construction, and that assumption is exactly the one B4 exists because someone already made.

### A.10 Buttons

| | fill | border | text |
|---|---|---|---|
| brass (primary) | `linear 180° #B3985E → #9C8148` | `1px #6F5A30` | `#F4ECDC` bold |
| paper (secondary) | `#E7DCC4` | `1px #8A7A5C` | `#45392A` |
| disabled | `#DDD2B8` | `1px #C9BA9B` | `#9A917D` |

Radius `2px` throughout. Primary carries `0 2–3px 6–9px rgba(0,0,0,.3–.4)`; secondary carries none.
Primary is given `flex 1.4–1.6` against secondary's `1` — the hierarchy is width as well as value.

⚠ *2026-08-27: brass and paper are built and called (`UiPalette.BuildButtonStyle`); the DISABLED face
is not — `ui_btn_disabled.png` is on disk with zero references, and `GUI.enabled` dimming over
brass/paper stands in for it (the 2026-08-12 "served by current treatment, revivable" ruling). The one
place the spec names it as required — the held-state speed buttons, B5 — is* **UNBUILT — roadmap
item 4.**

### A.11 The dossier card (Decisions)

Body `linear 180° #EDE2C6 → #E6D9BC`, `1px #B7A98C`, **border-left `6px` area ink**, shadow
`0 4px 10px rgba(0,0,0,.28)`, padding `12–14 / 18 / 14–16`.

**Tab shoulder** — absolute, `top −1px left 16px`, `#DCCBA6` on `1px #B7A98C` with no top edge,
`8.5 bold ls .14em #6B6250`, padding `2/8`, reading `DOSSIER · <AREA>`.

Urgency chip: `9.5 bold ls .08em`, `1.5px` border in its own ink, padding `1/7`, **rotated `−2°`**.
`HOLDS TIME` in `#9C4238`; `CAN WAIT` in `#8A7A5C` on `1.5px #B7A98C`.

**Generic stamp treatment** (used for `HOLDS TIME`, `CARRIED`, `REJECTED`, `DRAFT — NOT ENACTED`,
`GOVERNMENT DEFEATED`): a `2–3px` rectangular border in the ink, same-ink text, `bold ls .14em`,
padding `3/9`, rotated `−2°` to `−6°`. Rotation magnitude scales with consequence.

⚠ *2026-08-27: the card body, the area spine and the DOSSIER shoulder are built (`fc16304`); the urgency
chip ships as a plain `DrawColoredLabel` — no `1.5px` border, no rotation — and the generic stamp is
unbuilt (`CARRIED`/`REJECTED` are the `ui_stamp_*` sprites on the Division Records panel).* **UNBUILT —
roadmap item 4.**

### A.12 Dual-siting — the answer, restated as a build rule

Both variants appear on 1d, drawn side by side, and the rule is unambiguous:

| | standalone (`drawOwnFrame = true`) | embedded (`drawOwnFrame = false`) |
|---|---|---|
| plate | own `#F4ECDC` plate | **none** — host dossier is the surface |
| frame | `ui_frame_ornate`, inset `6px` | **none** |
| title band | `OFFICE OF THE PRESIDENT` + subject, centred, over a `1.5px #8A7A5C` rule | **none** |
| outer shadow | `0 4px 10px rgba(0,0,0,.25)` | **none** |
| portraits | oval, hero size (`58×72`+) | rect, roster size (`44×54`) |
| entrance | full 1e envelope | **none** — it is already on screen |

**Asset consequence:** plate, frame and title band are separate sprites from the interior furniture, so
the embedded path simply skips three draw calls. Applies identically to Fed chair, cabinet decisions and
foreign policy meetings.

⚠ *2026-08-27: the EMBEDDED column is built and called (`drawOwnFrame:false` at both Decisions sites).
The STANDALONE column is superseded, not pending — `ui_frame_ornate` is Canvas-path by ruling
(2026-08-12) and the framed IMGUI modal dies with the Canvas rebuild; its `drawOwnFrame:true` branch is a
plain box today, by design.*

### A.13 The hand-off envelope

Runs **from the IMGUI side**, per the render-order spike. One envelope for every consequential moment.

| t (ms) | action | side |
|---|---|---|
| 0 | input locks; sim clock holds | IMGUI |
| 0–180 | `ui_scrim_takeover` fades 0→100%, **opacity only**, ease-out quad | IMGUI |
| 180–240 | hold at 85%; IMGUI suppressed, Canvas enabled behind the wash | swap |
| 240–500 | document rises 24px, settles `−0.6° → 0°`, ease-out cubic; shadow deepens | Canvas |
| 580–700 | stamp/seal thunk: scale `1.15 → 1.0` over 120ms | Canvas |
| 700+ | controls fade in last | Canvas |

Exit, reversed and faster: controls lock → document drops 16px + fades 200ms → Canvas disabled → IMGUI
redraws beneath the scrim → scrim lifts 240ms. **Round trip ≤ 1.2s.**

The 60ms hold at step 3 is load-bearing: **it is what hides the swap.** Without it the layer change is
visible, and the screen-granularity architecture becomes apparent to the player.

⚠ *2026-08-27: the IMGUI half — the scrim, opacity-only, the hold, the swap — is built and called on four
paths (`14cbad6`, the takeover seam; one declared deviation, the scrim covers 100% rather than holding at
85%). Two of the six rows have no implementation; re-derive which against `GameController`'s seam
before building.* **UNBUILT — roadmap item 4.**

### A.14 Canvas screens

*Status 2026-08-27: **1f BUILT** (`14cbad6`, `CountrySelectorScreen.cs`; on screen in every live session)
and **1g BUILT** (`5f64554`, `SigningScreen.cs`; its seal/button branch defect was playtest 1's own
finding) — both with declared deviations recorded in CLAUDE.md; **1h NOT BUILT**, item-10-gated
(`MISSING_PREREQUISITES.md` §D — the paragraph below is the spec that build will start from).*

**Country selector (1f).** Padding `56/120`. Title block: kicker `15 bold ls .5em #9C8148`, wordmark
`54 #E8DDC4 ls .04em`, then a centred rule pair (`140×1px #6B5F4A`) flanking `14 ls .24em #B7A98C`.
Grid `3×2`, gap `26/30`, margin-top `40`.

Folder card: `linear 180° #E9DDC0 → #E0D1AF`, `1px #B7A98C`, **top `5px` country ink**, shadow
`0 8px 18px rgba(0,0,0,.5)`, padding `20/24`. Tab shoulder `top −15px left 20px`, radius `4 4 0 0`,
padding `3/14`, `10 bold ls .16em #45392A`. Flag `86×56` (full-colour exemption). Name `24 bold`;
under it `HUE: <AREA>` at `12 bold ls .1em` **in that country's ink**. A three-up figure strip
(population / GDP / debt-to-GDP) over a `1px #C9BA9B` rule, labels `9 bold ls .12em`, values `16 bold`.

States: normal · **hover** (lift `−8px`, 60ms, shadow → `0 16px 32px rgba(0,0,0,.65)`, body lightens to
`#EDE2C6 → #E4D6B6`, button promotes to brass) · pressed (scale `0.985`, 40ms) · selected (folder opens,
brief slides in, 320ms) · disabled (`#B9A886`, no lift).

Country → hue: **USA Political · Sweden Trade · Germany Welfare · France Labor · Italy Sectors ·
Poland SovereignWealth.**

**Signing (1g).** Document `820px` wide, centred, paper `linear 178° #F2EADB → #EDE3D0 @60% → #E8DEC9`,
`1px #CBBC9D`, shadow `0 34px 70px rgba(0,0,0,.8), 0 6px 16px rgba(0,0,0,.5)`, padding `46/58/40`,
ornate inset `14px`. Seal `56×56` centred above a `12 ls .34em` institution line, title `30 bold`, mono
`12` provenance line, closed by the `2px #2B2620` rule. Two-column figure grid, gap `0 40px`. Division
block on a `#F4ECDC` plate with a `1.5px #8A7A5C` border. Signature rule `1.5px #2B2620`, `44px` tall,
under a mono `11` presentation clause and over `11 bold ls .16em` office line. Seal landing zone
`104×104`. Beats: SIGN → pen scratch 400ms → `ui_seal_official` drops `1.3 → 1.0` over 140ms with a 6px
settle shake → document slides up, scrim lifts.

**Election night (1h).** Same envelope; document `1240px`, padding `38/52/34`, ornate inset `13px`.
Masthead splits: institution + title left, timestamp + a `348 OF 350 SEATS DECLARED` chip
(`#5D564A` fill, `#F4ECDC` text, `11 bold ls .1em`) right. Body grid `1.25fr | 1fr`, gap `36`.
Beats: bulletin lands → seats fill by declaration wave 1200ms in party-ink order → swing figures count
up 600ms → verdict stamp thunks last.

### A.15 Component inventory as the design realises it

`stat tile` · `ledger row (+ in-row slider)` · `standing/draft inline pair` · `draft hatch band` ·
`dossier card (+ tab shoulder)` · `stamp` · `urgency chip` · `plate` · `section head + rule` ·
`tab tongue` · `sub-tab` · `brass/paper/disabled button` · `status line (held / running)` ·
`calendar pad` · `division bar` · `legend row (swatch + emblem + name + figure + delta)` ·
`figure strip` · `folder card (Canvas)` · `document masthead (Canvas)`.

*2026-08-27: seventeen of the nineteen resolve to a build site; the `screen caption` (§A.8) does not,
and one more row has no site — re-derive which before relying on this list. The two are roadmap item 4.*

### A.16 Boards 1k and 1l — the calendar sheet and the graph-weight ruling (answered 2026-08-27; NOT BUILT)

*Design's answers to the seventh request's §2 and §3, read from the live `PoliSim v2 Screens.dc.html`
(`data-screen-label="1k Calendar panel board"`, `"1l Graph weight ruling"`). Both are pixel rules for the
existing procedural code — no sprite was asked for or delivered. Roadmap live items 8–9; not started by
Elias's ruling (§V first). Every number below is a board value under the banner at the top of this file.*

**1k — the left-column calendar panel as ONE almanac sheet** (drawn against the request's FIXED data
contract, at 691px = 43.2% of 1600, reproducing `couple2s1600_02` and the `capfold_83a` density case):

1. **The X-mark retires; the strike replaces it.** A spent day is the numeral crossed with ONE diagonal
   ink stroke — the almanac cross-off. Pixel rule: 1.5px at 1600 / 2px at 2560, ink at 55%, angle ≈ −24°,
   inset 2px into the numeral box. The literal " X" suffix goes (it was doing an almanac's job with a
   typewriter's tool; the strike frees four characters of cell width at every size).
2. **The dots-vs-ledger split STANDS.** Grid dots say *that* and *whose area*; the ledger says *what*.
   Four dots reads as busy, not noise, once **the ledger row carries the same 5px dot** — the two
   instruments cross-reference instead of restating each other.
3. **The flip stays INSTANT.** The month is regenerated, not turned; zero staleness IS the honest desk.
4. **The saturated day earns the heavy-day rule.** At the 4-dot cap the cell gains a **2px ink underline
   beneath the dot row** — the almanac's red-letter mark in this desk's own ink. The cap stays hard; the
   merged sentence lives in the ledger.
5. **One instrument.** Header, grid and ledger are ONE paper sheet — one card, sections separated by
   rules, one scroll. Three stacked cards would make the column read as a pile; a sheet reads as a place.

Measurements stay measurements (day-cell height via `CalcHeight`; the date column against the widest
date; the column 43.2% of the window); chrome stays procedural (`RoundedCard`/`Rule`/`Pill`);
`ui_calendar_pad` untouched; locale honesty untouched (MÅN…SÖN, JANUARI, `FirstDayOfWeek`).

**1l — the graph-weight ruling, SPECIFICATION ONLY** (lands in `BuildSparklinePixels` under the existing
336-combination tests). The finding restated: the WEIGHT ORDER was flat — history, projection and
threshold all landed within a device pixel of each other at 2560, so the recorded data could not
outrank its own reference marker. A series line is mostly horizontal, so its read weight is its VERTICAL
thickness, which the 300×90 buffer's stretch never multiplies at 2560 (×1.0 V) — the rule therefore
speaks in buffer px once, with no per-resolution branch (≈ ×0.74 V at 1600, antialiased by the bilinear
stretch: 3 buffer px ≈ 2.2 device px there, 3 at 2560).

- **R-G1 History: 3 buffer px** (from 2), solid, full ink.
- **R-G2 Projection: stays 2 buffer px**, lighter alpha, most-recent page only; **dash cadence re-cut
  from "skip every 3rd step" to 3 on / 2 off** so the gaps stay visible beside a heavier history. It
  must read "estimate", never "second series".
- **R-G3 Threshold: stays 1 buffer px**, warm amber, riding label — a reference IS a hairline by design;
  differentiation comes from the weight order 3 / 2 / 1.
- **R-G4 Sparklines** (native-resolution buffers, same Bresenham): thickness = `max(2, round(rectHeight
  / 34))` device px — 2 at small rects, 3 at a 90px 2560 rect.
- **R-G5 The 300×90 buffer may stand.** Raising it to display resolution is invited, not required; if
  raised, restate the rule in device px (3 / 2 / 1) and nothing else changes.

**What moves with the weight and what must not:** release-point markers scale to weight + 2 buffer px
(a 5px square at the new history weight) so they stay proud of the line; the green/red direction deltas
(header ink), the PRELIMINARY badge + lag dating (text channel) and the **1px dashed revision FRAME do
NOT move** — the frame is chrome, a separate channel from the projection dashing, and its fine cadence is
exactly what keeps the two dash meanings apart. The full-width neutral-rate graph (`item5sweden_07d`)
takes the same 3/2/1 order with its no-judgment ink untouched. **Acceptance, in engineering's own
harness:** one constant and one cadence change under the 336-combination set; the eye's check is four
stacked graphs at 2560 where history plainly outranks the amber reference — the inverse of
`couple2s2560_02a`.

---


### A.17 Screen 0 — The Desk, and the rail re-skinned (boards 1m and 1n, read 2026-08-28; built the same day, UI v3.0 Phase B)

**The boards are the spec.** Board 1m ("Screen 0 — The Desk, folded", v3.0 board one, drawn 2026-08-28 against Annex A's census and Annex B's measured minimums at 1280×720) and board 1n ("the rail", v3.0 board two, a re-skin of the built rail — "the derivation untouched, only the air moved"), both on the live screens file. What they rule, restated here so the code can cite a line:

**1m — the split (D1).** The chrome census folds with the chrome, so every chrome (a) lands on the stage or the rail: C6 the masthead · C7–C12 the calendar sheet · C16–C22 the effects card · C23–C25 the rail chip · C27 the HELD banner (above the masthead) · C28 the speed cluster (on the masthead, D5). The content rows (S-) keep their document — Statistics stands one rail cell away, unchanged — and the stage restates only its ten headlines, as the chip strip. *Strike the split and the stage is the old screen again: the census's 44 (a) do not fit 1149×691 at their minimums.*

**1m — the placement at 1280×720** (the sheet's inner area 1118×660; every size scales these by the inner area's ratio): the masthead 26 tall — the flag and `{COUNTRY} · YEAR {N}` left, `DESK READINGS · LIVE` and the cluster (PAUSE 1× 2× 3× SAVES) right; three columns 420 / 240 / 425 with 16 px gaps — the map plate 420×290 over the approval ledger 420×222; the compass 240×240 over the effects card 240×272; the calendar sheet 425×380 over the event card's reservation 425×136; a rule; the chip strip 10 × ~104 × 56. Instruments at or above Annex B's minimums: map 420×290 (min 360×216 with names), compass 240 (its floor), ledger 420 at 13 px (min 360/13), sheet 425 (min 420), chip line 42×16 (≥ 36×10), diverging bars 64×9 (height traded, declared), rail chip 39 (min 32).

**1m — the seven declared deviations**, accepted as built: D1 the split · D2 no active spine on Screen 0 (the Desk sits above the six documents) · D3 the compass's axis captions inside its rect (the renderer's own footprint since R-SP4 does exactly this) · D4 C20's methodology sentence as a mono caption · D5 the speed cluster and Saves on the masthead · D6 approval as a hero numeral over the nine-term ledger — no face exists, none invented · D7 trend lines and chip deltas in neutral ink.

**1m — the (b) resolutions on this surface:** C3 → the event card's three bars · C10/C13 dropped (the 1k precedent) · C18 → the horizon control carries it · C26 → the lamp · S1's labels → the rail's icons. Every other (b) stays with its document.

**1m — the conditionals:** the event card draws only while an event is live (the BREAKING chip, the name, the description as its only text, the three bars; the empty state is the reservation, undrawn); HELD rides above the masthead with the lamp amber and the speed faces disabled (B5, rendered never omitted); GAME OVER is §A.11's stamp over the dimmed stage with the reason as one caption.

**1m — the text budget audit, the board's own line:** every string ≤ a mono 9.5 caption or an instrument's label/numeral · zero sentences · marks inside plates are the renderers' output · glyphs are the delivered sprites · the tab→ink mapping is re-read from §A.3/the code, never from a board.

**1n — the rail.** The derivation untouched: cell = the icons' 24-unit grid + 10 units of air each side (39 / 46 / 55 / 64 px at the four sizes). The air: the nav block top-anchored under the sheet's cap, the utility block (chip · lamp · toggle) bottom-anchored, one breathing gap between — never equal distribution. Active: a 3 px spine at the cell's left edge, full cell height, in the area ink; the icon in the same ink; a 12 % area-ink wash behind the cell. Inactive: the tab-swatch tint (§A.3's third column), no wash, no plate. Hover: the stock hover face, tint unchanged. Contents exactly V3-R2's four — six icons, the chip (the pad's mono month over its numeral day, a hairline rule between), the lamp (RUNNING green, no glow; HELD `#D4A72C` with `0 0 6px rgba(212,167,44,.7)`), the toggle on the stockOff plate ("›" folded, "‹" on the open strip). The flip is instant. On Screen 0 no spine draws (1m, D2). Deviations none; gaps none.

**Gaps costed by the boards: none asked.** Two optional notes carried, not asks: an approval dial face would be a NEW instrument (one further board first; procedural arc and needle, no sprite); the chip sparklines' weight is R-G4's already.

**Built (Phase B, 2026-08-28):** `GameController.Desk.cs` (Screen 0), the rail's active convention and the chip's rule in `GameController.DrawRailNavCell` / `DrawRailCalendarChip`, the ledger accessor `StatTracePanel.BuildApprovalDeskTerms`, the harness's `01c_desk` · `01d_desk_held` · `01e_desk_event` · `01f_desk_gameover`. The build's own reversible calls are R-B1…R-B14 (`COMPLETED.md` §41).

## C. PASS 3 — DISPOSITIONS AND THE RE-REVIEW FINDINGS — MIGRATED (2026-08-27)

*The nine §1D dispositions (D1–D9 + the locale and banner items) and the seven re-review findings
(C.1 the division-record blocker, C.2 the numeric ladder, C.3 the 36px/type-rescale collision, C.4 the
categorical cap as a throw, C.5 `emblem_state_seal` → `ui_seal_state`, C.6 the `canvas_*` namespace, C.7
SVG-only delivery and the manifest the coverage check gained) were ALL closed by the build and are
recorded in `COMPLETED.md` §24 ("The screen spec's finished sections"). Git history holds the original
text (the spec at `d29406f`).*

---

## D. THE TWO ARCHITECTURAL CONSTRAINTS — both respected

**1. Hybrid at screen granularity. ✅ Respected — and pass 3 closed the one clause that weakened it.**

Every board is wholly one side: 1a–1d IMGUI, 1f–1h Canvas. No board interleaves an IMGUI element into a
Canvas screen or the reverse. 1e makes the swap explicit and gives it a 60ms hold at 85% scrim precisely
so the changeover is invisible.

The clause raised as §1D.4 — `§1C.3`'s *"survives the whole sequence"*, which read as IMGUI drawing over
a live Canvas screen — **was accepted verbatim and is now struck.** Pass 3's replacement rule ("every
Canvas takeover redraws the hold banner itself; both sides draw it") **was never implemented, and the
build settled it differently (corrected 2026-08-27):** takeovers stop the clock by construction
(`COMPLETED.md` §27 — a Canvas screen is a held state), so no hold banner is owed while one is up; the
IMGUI layer is suppressed except for the scrim, which fades over the active Canvas precisely because
IMGUI is topmost. Any future Canvas screen that does NOT hold time must revisit this; 1h holds it.

The boards carry the banner on **1a, 1b, 1d, 1g and 1h** as drawn; `CANVAS_SPEC.md` §0/§3 were
corrected in pass 3 to match. The previous phrasing was the one sentence in the whole pack that,
implemented literally, would have broken screen granularity.

**2. Transitions run from the IMGUI side. ✅ Respected explicitly and correctly.**

1e panel 2 states the measured constraint in the design's own words: *"ui_scrim_takeover fades in, drawn
BY IMGUI — no Canvas mode draws above OnGUI."* The scrim is one 256×256 radial-vignette sprite stretched
full-screen, animated by opacity alone — inside IMGUI's two legal runtime knobs (tint and opacity), so
the fade needs no baked frames. The design also draws the correct conclusion downstream: *"a document
sliding halfway over the ledger is impossible by measurement, and nothing in this system pretends
otherwise."*

---

## E. WHAT REMAINS (rewritten 2026-08-27; the 2026-08-10 "what is buildable" list was built item by item)

The chrome-wiring gate this section once carried is discharged: Elias has run three live Editor sessions
on the shipped v2 UI (playtest 1's scoping session 2026-08-18, playtest 2 on 2026-08-25, and the
2026-08-26 gates session — CLAUDE.md), which is exactly the setting the gate asked for. What this spec
still governs:

- **The one unbuilt screen — 1h ELECTION NIGHT (§A.14), item-10-gated** (`MISSING_PREREQUISITES.md`
  §D). Its §D.1 banner instance and party-ink swing usage go with it; it holds time, so §D.1's corrected
  rule applies as written.
- **The unbuilt clauses marked UNBUILT — roadmap item 4 above** (§A.2's three tokens, §A.3's tab-swatch
  tints, §A.6's RUNNING state and held-state buttons, §A.8's screen caption, §A.10's disabled face,
  §A.11's urgency chip and generic stamp, §A.13's two envelope rows, §A.15's two unresolved rows) —
  small, ungated, listed once on `POLISIM_MASTER_ROADMAP.md` and nowhere else.
- **Every standing rule in §A governs any new screen regardless** — the surface ladder, the inks, the
  eleven hues and the categorical cap, the typography roles, the resort ladder in both variants, the
  negative-fill sign rule, the dual-siting asset consequence, screen granularity and the IMGUI-side
  transition. The banner at the top of this file — every number is suspect until derived or confirmed
  — binds them all.
