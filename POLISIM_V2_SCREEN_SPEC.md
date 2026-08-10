# PoliSim v2.0 — Screen Specification

*Extracted 2026-08-10 from Claude Design project `b3dec27b`, file `PoliSim v2 Screens.dc.html`
(treatment A, "ministry fresh print", eight boards at 1920×1080). **Revised to pass 3, 2026-08-10.***

⚠ **This is a specification, not source to port.** The delivered artifact is HTML/CSS; PoliSim renders
in IMGUI and Canvas. Every number below is design intent restated in terms this project can implement.
Nothing here was translated from markup — the markup was measured and discarded.

**Pass 3 status: all nine §1D items resolved.** Seven accepted as raised, two amended with reasoning
that improves on the request (D4's hue cap, D7's resort ladder). The locale flag and the §1D.4 banner
wording were both taken. `polisim_palette.json`, `MANIFEST.md`, `DIRECTION.md` and `CANVAS_SPEC.md` were
all updated in the same pass, and the boards were redrawn rather than annotated.

⚠ **Read `§C` before implementing anything.** The nine are closed; **seven new findings** came out of
re-reviewing pass 3 against the same standard. **One is blocking** — and it is a spec correction, not
something to discover mid-implementation.

---

## A. THE EXTRACTED SPEC

### A.0 What the eight boards cover

| board | screen | side | shows |
|---|---|---|---|
| 1a | Persistent chrome + Statistics : Domestic | IMGUI | left column, tab strip, time-**held** state |
| 1b | Budget, full-screen (left column hidden) | IMGUI | the density case: ledger + bill rail |
| 1c | Politics : Parliament | IMGUI | time-**running** state, legend↔chart ink |
| 1d | Decisions | IMGUI | dossier cards + the dual-siting rule, both variants |
| 1e | The hand-off envelope | swap | 5-panel storyboard + timeline |
| 1f | Country selector | Canvas | six folder cards, hover state |
| 1g | Bill vote → budget signing | Canvas | the consequential document |
| 1h | Election night returns | Canvas | the returns bulletin |

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
| `textOnDesk` | `#E8DDC4` | type on the dark banner |
| `mutedInk` | `#9A917D` | disabled row ink and disabled button text |
| `deskCaption` | `#8D7D5F` | mono annotations on desk ground |

Brass: `#B3985E → #9C8148` gradient, border `#6F5A30`, text `#F4ECDC`.

**Rule weights are semantic and consistent across all eight boards:**
`2px #2B2620` = screen/section heading rule · `1.5px #8A7A5C` = panel section rule ·
`1px #B7A98C` = column-header rule · `1px #D5C8AB` = row separator ·
`3px double #8A7A5C` inset 8–14px = the ornate frame (`ui_frame_double`).

### A.3 The eleven hues — three tints, all delivered

Already wired in `PoliSimTheme` from `polisim_palette.json`; the boards use them unchanged.

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
| `SpendingCategory` (spending pie) | 29 | converts to ranked bar ledger, Fiscal ink |
| `PartyArchetype` (hemicycle) | 4 | keys `parties.*`, never this set |

The ranked bar ledger satisfies B9 trivially — one ink, and every row carries its own inline label, so
there is no legend to disagree with the chart. See `§C.4` for the one implementation consequence.

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

### A.6 The left column (persistent chrome)

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
   - Four speed buttons, gap 6, equal flex. Active brass; inactive plate; **when time is held all four
     non-Pause buttons render `#DDD2B8` / `1px #C9BA9B` / text `#9A917D` — rendered, never omitted (B5).**

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
  This caption is B6's live/published carrier at screen level.

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
29 spending programs, both density levers taken.

Row height **`36px`** (was 44), separator `1px #D5C8AB`, grid **`250px | 1fr | 150px | 88px`**, gap `10`,
right padding `26px` to clear the scrollbar. The per-row `VOTES` column is **deleted** — no
per-instrument legislative support exists — and its width is what pays for the 250px name column.
Column header band: `8.5 bold ls .13em inkFaint`, closed by `1px #B7A98C`, padding `7/26/5/0`.
Totals: `1.5px #8A7A5C` above, label `10 bold ls .14em inkFaint`, value `16 bold`.

29 rows × 36px = 1044px against a viewport of roughly 22 rows, so **it scrolls, and that is intended**.

**Scroll view treatment — all 16 of them, not just this one:**
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
  The row is still drawn, still measured, still occupies its 44px (B5).

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

### A.14 Canvas screens

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

---

## C. PASS 3 — DISPOSITIONS, AND WHAT THE RE-REVIEW FOUND

### C.0 The nine, closed

Re-reviewed against the same standard as pass 2 — the eleven load-bearing behaviours and both measured
architectural constraints. Every disposition below was checked against the boards or the codebase rather
than taken on the changelog's word.

| item | disposition | verified how |
|---|---|---|
| D1 glyph | accepted — pencil is `icon_pencil_draft` geometry, never a font glyph | **0** occurrences of `U+270F` remain across all eight boards; riders taken (`▲▼` Pagella-only, `⚠` → printed `N.B.`) |
| D2 division bar | accepted, option 1 — verdict + seat-weighted lean bar; per-row `VOTES` deleted | boards redrawn; seat counts kept only where `Country.ParliamentSeats` is real. **But see `§C.1`** |
| D3 density | conceded — 1b was a composite; redrawn as the Spending sub-tab as built, 29 programs | rows 44→36px **and** it scrolls; scrollbar drawn and specified for all 16 views |
| D4 unaged charts | **amended** — categorical caps at 8; longer series change chart form | `SectorType` is exactly **8**, so the sector pie stays hue-keyed; tax (13) and spending (29) convert |
| D5 party inks | accepted — own hue set, re-keyed to the real `PartyArchetype` four | no collision with any area ink, either tint, or `good`/`bad`/`draftAmber` |
| D6 third tint | delivered — the derivation rule plus all eleven snapped values | `tabTint.*` complete in `polisim_palette.json` |
| D7 truncation | **amended** — never clip, but by a resort ladder, not uniform auto-shrink | ladder specified in `§A.9a`. **But see `§C.2`** |
| D8 published/live | boards' rule stands; §1C.2 struck | corrected rule recorded in `§A.8a`; `DIRECTION.md` B6 matches |
| D9 phantom sprites | accepted — four substitutions approved, four new assets delivered | **see `§C.5`–`§C.7`** |
| §1D.3 locale | agreed — not art's call | **0** comma-decimal money figures remain; dates English |
| §1D.4 banner | accepted verbatim | 1e reworded; banner now on 1a, 1b, 1d, 1g **and 1h** |

Two of these are better than what was asked for. **D4** refused to ship 29 distinguishable aged hues and
changed the chart form instead — the right answer, and the one that keeps B9 intact. **D7** rejected
uniform auto-shrink on the grounds that a column printing at four different sizes reads as an error
rather than as a fit, which is a real observation the request missed.

### C.1 ⛔ BLOCKING — division records have no backing data at all

Design's D2 caveat reads: *"division records keep the verdict stamp plus the alignment captured at the
vote."* Board 1c prints, per row: `No. 214` · title · `29 apr 2026 · alignment −0.12, seat-weighted` ·
`CARRIED` stamp.

**Nothing behind any of those five fields exists.** This is not "the alignment isn't persisted" — the
record itself does not exist, so the fallback of dropping to *date + verdict only* is not available
either.

Evidence:

- `ApplyBillResult` (`ParliamentSystem.cs:247`) is where every bill of every tier resolves. On FAIL it
  docks approval and returns. On PASS it mutates tax lines, welfare generosity, spending and SWF, applies
  the tax-hike approval penalty, and returns. **It writes no record.**
- A repo-wide search for a history store — `List<…Bill>`, `DivisionRecord`, `BillHistory`, `RecentVotes`,
  any activity or event log — returns **zero hits** in `Assets/Scripts/Data/` and
  `Assets/Scripts/Simulation/`.
- `DrawParliamentTab` (`GameController.cs:3682`) draws the hemicycle and `DrawPendingLegislation`.
  Pending only. Nothing UI-side retains a resolved bill either.

**The constructive half.** The alignment *is* computed at the exact moment of resolution —
`SimulationManager.cs:666` calls `WouldBillPass(country, bill)`, which calls `GetSeatWeightedAlignment`
internally — and then discards it. So Design's caveat is the cheapest possible unsatisfiable thing: the
quantity exists at the right instant, and `ApplyBillResult` is a **single choke point** all seven bill
tiers pass through. What is missing is a record, not a number.

**Two ways forward, and this is Elias's call, not ours:**

1. **Cut the panel** from 1c until a division-record store exists. 1c loses one of its four plates and
   the screen needs re-balancing.
2. **Add the store** — a small, well-bounded change: a per-country ring buffer of
   `(number, title, date, alignment, passed)` appended in `ApplyBillResult`. It is a simulation change,
   so it needs sign-off separately, and it is not covered by the current gate.

Recorded here rather than discovered during implementation, which is exactly what was asked for.

### C.2 ⚠ D7's ladder resolved the site, not the class

The ladder is specified for the **`PROGRAM` name column**, and it does resolve that column properly —
the 250px measure, the two-line wrap, the abbreviation table and the 11px floor between them mean a
generated or enum-derived name can no longer clip.

But B4 is a **class-level** rule, and its original defect was numeric. `UiFormat`'s own doc comment
names it: *"the '9,3' incident was a comma-decimal figure clipped in a narrow rect."*

Every other fixed-measure cell on the pass-3 boards still carries a fixed width with no stated resort:

| site | measure | content |
|---|---|---|
| ledger `STANDING ✎ DRAFT` | `150px`, no wrap | two money figures + the pencil sprite |
| ledger `SHARE` | `88px`, no wrap | `6.5% GDP` |
| stat tile label | ~`133px` (3-up in 430px), no wrap | `CURRENCY STRENGTH`, `GOVERNMENT DEBT` |
| legend seats / delta (1c) | `70px` / `56px` / `52px` | seat counts and swings |
| legend seats / delta (1h) | `60px` / `56px` | as above |

None of these is *demonstrably* broken — `MoneyUnit` tiering bounds a money figure to about six
characters, so 150px is comfortable today. The objection is that it is comfortable **by assumption**
rather than by construction, and the assumption is precisely the one B4 exists because someone made
before.

**What the spec needs:** the ladder stated as applying to *every* fixed-measure cell, with the numeric
variant of step 2 named — numbers cannot wrap and cannot be abbreviated by table, so for them the ladder
is *widen → shrink to the 11px floor*, and never a clip. This is a one-line generalisation of a rule
Design has already reasoned through correctly.

### C.3 ⚠ The 36px row and the two-line wrap collide as soon as type rescales

Pass 3 sets the ledger row at a fixed `36px` and permits generated names to wrap to two lines at `13px`
with `line-height 1.1`.

At 1080p that fits: `2 × 13 × 1.1 = 28.6px` inside 36px, about 7px spare. But **§3.2 of the asset
request states the governing constraint** — *"every style in this UI rescales with `Screen.height`, so
there is no single fixed render size."* At 1440p the same name sets at roughly `17.3px`, and two lines
become `38.1px` — **taller than the row that is supposed to contain it.**

This is the `PolicyScreenStatsRenderer.DrawChip` defect exactly, which has already produced three
separate instances in this project (`DrawChip`, `OverflowLineHeight`, tab-button `fixedHeight`). The
fix is the same one that worked there: **derive the row height from the font metric rather than fixing
it** —

```
RowHeight = max(2 × LineHeightFor(nameStyle) + pad, SliderTrackHeight + pad)
```

`36px` should be recorded as the value *at 1080p*, not as the row height.

### C.4 ⚠ `GetCategoricalColor` must fail past eight, not wrap

D4's cap is correct and the chart-form change is the right answer. The implementation trap is in how the
cap gets enforced.

`UiPalette.GetCategoricalColor(int index)` currently computes a golden-angle hue and therefore **always
returns a colour for any index**. Its call sites pass indices up to **28** (spending) and **12** (tax).
If the function simply becomes an eight-entry array indexed `index % 8`, those series keep working and
silently alias — category 0 and category 8 print in the same ink, on the same chart, and **B9 breaks
without anything failing.**

**The call sites must change chart form; the function must not quietly absorb the difference.** Whatever
replaces it should fail loudly past index 7 rather than wrap, so that a series which outgrows the cap is
caught at the call site instead of at a player's eye.

### C.5 ⚠ `emblem_state_seal` breaks what the `emblem_` prefix means

The sprite itself is right, and tintable is the correct choice for a seal. The **name** is the problem.

§3.1 makes the prefix load-bearing: *"Country flags and party emblems are authored in their own real
colours… Any new art in those two categories stays full-colour; everything else stays white-on-alpha.
Getting this backwards in either direction produces art that cannot be used."*

So `emblem_*` currently *means* "full-colour exemption, never tint". Pass 3's manifest marks
`emblem_state_seal` as **WoA** — tinted `inkText` on documents, brass on desk. That is the opposite
rule under the same prefix, and it makes the exemption uncheckable by name.

The manifest's own note gives the answer away: it calls the sprite *"radial-tick family of
`ui_seal_official`"*. **It should ship as `ui_seal_state`**, where it sits beside the sprite it is
derived from and inherits the correct tint rule. Left as `emblem_*`, `IconLibrary`'s emblem accessor
gains a member that must never be drawn the way every other member of that family is drawn.

### C.6 ⚠ `canvas_*` is a new namespace, and two of them are sprite sheets

Two separate consequences, neither an error, both needing a decision before import:

- **The prefix.** Every one of the 52 sprites in `Chrome/` is `ui_*`. `canvas_folder_country`,
  `canvas_btn_brass` and `canvas_btn_paper` open a second namespace inside the same folder. Defensible —
  they are the Canvas path and behave differently — but it should be a recorded decision rather than
  drift, because `ChromeV2CoverageCheck` and anything else keyed to the convention now sees two families.
- **The sheets.** `canvas_btn_brass` and `canvas_btn_paper` are `256×384` = **three cells of `256×128`**
  (normal / hover / pressed), with 9-slice `24/24/24/32` *per cell*. Every other delivered sprite is a
  single sprite with one border set. §3's import instruction — copy the `.meta` from
  `icon_stat_gdp.png.meta` — produces a **single-sprite** texture, and `Resources.Load<Sprite>` on a
  multi-sprite texture returns `null`. These two need Sprite Mode Multiple, a grid slice, and
  `Resources.LoadAll`. That is a real import difference and the first time the pack has needed one.

### C.7 ⚠ SVG-only delivery — nothing from pass 3 is loadable yet

Pass 3 shipped **SVG sources only**: `canvas_folder_country`, `canvas_btn_brass`, `canvas_btn_paper`,
`emblem_state_seal`. Every previous pass shipped PNGs. The manifest says to *"rasterize @2× at import"*,
so this is a task rather than an error — but it is a task nobody has scheduled, and until it is done the
four specified assets do not exist in `Assets/Resources/` at all.

⚠ **And `ChromeV2CoverageCheck` will not catch it.** That check enumerates `Chrome/` from disk and
verifies each file resolves through `Resources.Load` — it validates that *what is present is reachable*,
not that *what is specified is present*. Four missing assets produce a clean 52/52 pass.

That is the delivered-vs-reachable lesson arriving from the opposite direction, and it means the check
needs a manifest to compare against before it can be trusted on a pass that adds files.
## D. THE TWO ARCHITECTURAL CONSTRAINTS — both respected

**1. Hybrid at screen granularity. ✅ Respected — and pass 3 closed the one clause that weakened it.**

Every board is wholly one side: 1a–1d IMGUI, 1f–1h Canvas. No board interleaves an IMGUI element into a
Canvas screen or the reverse. 1e makes the swap explicit and gives it a 60ms hold at 85% scrim precisely
so the changeover is invisible.

The clause raised as §1D.4 — `§1C.3`'s *"survives the whole sequence"*, which read as IMGUI drawing over
a live Canvas screen — **was accepted verbatim and is now struck.** The governing rule:

> **Every Canvas takeover redraws the hold banner itself.** The IMGUI banner cannot persist past
> t=180ms, because IMGUI is suppressed. Time-hold state is never invisible because **both sides draw
> it**, not because one side survives.

Verified in the boards rather than in the changelog: the banner now appears on **1a, 1b, 1d, 1g and 1h**
— 1h was the board missing it, and it has been added. `CANVAS_SPEC.md` §0/§3 were corrected to match.

This is a strengthening, not a patch. The previous phrasing was the one sentence in the whole pack that,
implemented literally, would have broken screen granularity.

**2. Transitions run from the IMGUI side. ✅ Respected explicitly and correctly.**

1e panel 2 states the measured constraint in the design's own words: *"ui_scrim_takeover fades in, drawn
BY IMGUI — no Canvas mode draws above OnGUI."* The scrim is one 256×256 radial-vignette sprite stretched
full-screen, animated by opacity alone — inside IMGUI's two legal runtime knobs (tint and opacity), so
the fade needs no baked frames. The design also draws the correct conclusion downstream: *"a document
sliding halfway over the ledger is impossible by measurement, and nothing in this system pretends
otherwise."*

---

## E. WHAT IS ACTUALLY BUILDABLE

Everything the nine blockers held up is now specified. Unblocked, and touching none of `§C`'s findings:

- the surface ladder, ink set and rule weights (`§A.1`–`A.2`) — already wired, needs verification only
- the three hue tables (`§A.3`, `A.3a`, `A.3b`) — complete for the first time: all eleven tab tints, the
  four party inks, and the eight-hue categorical series
- the tab strip's three-state treatment (`§A.7`) — no longer missing five values
- the sub-tab row, plate treatment, and the corrected published/live rule (`§A.8`, `A.8a`)
- the two status-line states (`§A.6`) — B8's carrier, fully specified
- the ledger row and the scroll-view treatment (`§A.9`), **with `§C.3`'s row height derived rather than
  fixed at 36px**
- the resort ladder (`§A.9a`), **generalised per `§C.2` to numeric cells before it is relied on**
- the dossier card and the generic stamp treatment (`§A.11`)
- the dual-siting build rule (`§A.12`)
- the hand-off envelope timings (`§A.13`), including the corrected banner rule

**Needs a decision before it can be built:** the 1c division-records panel (`§C.1`) — cut, or add a
record store. **Needs an import pass:** rasterising pass 3's four SVGs, with the sheet-slicing and
naming questions in `§C.5`–`§C.7` settled first.

⚠ **Gated on Elias reviewing the current chrome wiring in a live Editor.** That instruction stands and
this document does not supersede it — every item above changes the same screens the wiring already
touched.
