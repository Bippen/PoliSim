# PoliSim v2.0 — Screen Specification

*Extracted 2026-08-10 from Claude Design project `b3dec27b`, file `PoliSim v2 Screens.dc.html`
(treatment A, "ministry fresh print", eight boards at 1920×1080).*

⚠ **This is a specification, not source to port.** The delivered artifact is HTML/CSS; PoliSim renders
in IMGUI and Canvas. Every number below is design intent restated in terms this project can implement.
Nothing here was translated from markup — the markup was measured and discarded.

⚠ **Read `§C. CONFLICTS` before implementing anything.** Nine items conflict with the eleven
load-bearing behaviours, the delivered asset pack, or the simulation's actual data model. Four are
blocking.

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

### A.3 The eleven hues — three tints, only two delivered

Already wired in `PoliSimTheme` from `polisim_palette.json`; the mockups use them unchanged.

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

⚠ The **third column is new and only partly delivered** — see `§C.6`.

**Semantic:** draft `#BE8A00` (lifted `#D4A72C`) · good `#3E8A5F` (lifted `#6FB08A`) ·
bad `#9C4238` (lifted `#BC7168`) · caution = draft.

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

### A.9 The ledger row — the Budget screen's atom

Row height `44px`, separator `1px #D5C8AB`, grid `168px | 1fr | 114px | 62px | 44px`, gap `10`.
Column header band: `8.5 bold ls .13em inkFaint`, closed by `1px #B7A98C`, padding `7/0/5`.
Totals: `1.5px #8A7A5C` above, label `10 bold ls .14em inkFaint`, value `16 bold`.

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
subtotals, and on the bill rail. See `§C.1`: **the `✎` glyph does not exist in any shipped font.**

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

## C. CONFLICTS

Nine. Four block implementation. Each is stated with the evidence, not as a preference.

### C.1 ⛔ BLOCKING — B1's carrier glyph does not exist in any shipped font

The design makes `✎` (U+270F) the primary draft marker. It appears on all four IMGUI boards: the
`✎ 3 DRAFTS OPEN` header, the `STANDING ✎ DRAFT` column header, every drafted row (`22,0% ✎ 24,5%`),
both subtotals, the bill rail's three figure rows, and the `✎ DRAFT — NOT ENACTED` stamp.

Measured against the three fonts actually in `Assets/Resources/Art/UI/Fonts/`:

| glyph | Pagella Regular | Pagella Bold | Courier Prime |
|---|---|---|---|
| `U+270F` ✎ pencil | **absent** | **absent** | **absent** |
| `U+270E` ✎ pencil (lower) | **absent** | **absent** | **absent** |
| `U+26A0` ⚠ warning | **absent** | **absent** | **absent** |
| `U+25C4` ◄ | **absent** | **absent** | **absent** |
| `U+25B2` ▲ / `U+25BC` ▼ | present | present | **absent** |
| `U+2212` − / `U+00B1` ± | present | present | present |

This is precisely the B11 failure mode — *"a font or glyph set lacking it renders a blank box on a readout
the player is meant to trust"* — landing on **B1**, the behaviour that must never become nothing. Shipped
as designed, every draft marker in the game renders as `□`.

`U+2212` and `U+00B1` are confirmed present, so B11 as previously verified still holds. The regression is
in glyphs the design newly introduced after that audit.

**Resolution required from Design, not from code:** either the pencil becomes a tinted sprite
(`icon_pencil_draft.svg` already exists in the pack and is unwired), or the draft marker becomes a
typographic mark that Pagella actually carries. ▲/▼ are safe in Pagella but must never be set in Courier
Prime.

### C.2 ⛔ BLOCKING — the division bar shows a quantity the simulation does not compute

The design's central legislative visual is a seat-count division: `PASSES · 186 – 164`, a bar filled
53.1% aye against nay meeting at a threshold tick, and `aye 186 · 176 to pass · margin 10`. It appears on
1b (bill rail), 1c (division records `212 – 138`) and 1g (`DIVISION No. 215 · 186 – 164`).

`ParliamentSystem.GetSeatWeightedAlignment` documents the opposite in its own comment:

> *"this is NOT a headcount, and there is no seats-based majority threshold anywhere in this model. Each
> party contributes its seat share…"*

`DrawBillLiveEstimate` renders what the model does produce: a direction label, a WOULD PASS / WOULD FAIL
verdict, and a diverging lean bar. Its comment records that this is deliberate — *"Deliberately not
PoliSimWidgets.SupportBar — this model has no seats-based majority for it to draw… the Parliament card
already shipped that exact bug once."*

So the design asks for a re-run of a bug the codebase already fixed and documented. Either the
simulation gains a real division model (a substantial change, not a UI one), or the division bar becomes
a period-styled rendering of the *diverging alignment* that actually exists. The second is cheap and
honest; it needs a design decision because the bar's whole composition changes.

**The same applies to the per-row `VOTES` column** (`−9`, `+6`, `−12`, `+4`, `N/A`) on 1b's ledger.
Per-instrument legislative support does not exist at any granularity — bills are scored whole.

### C.3 ⛔ BLOCKING — the Budget board tests 19 rows; the game has 42

1b is captioned *"the density stress test: 19 live line items"* and draws 11 tax rows and 8 spending rows
at `44px` each. The actual data model:

| | mockup | actual |
|---|---|---|
| `TaxType` | 11 | **13** |
| `SpendingCategory` | 8 | **29** |
| `WelfareProgramType` | — | 6 |
| `InfrastructureType` | — | 4 |

29 spending rows at 44px is `1276px` of content in a column roughly `800px` tall. The board's own
argument in W1 used the right number — *"Budget has ~40 rows"* — and then the board drew half of them.
The density case is untested at the density that exists.

Two knock-ons:
- **Row height must come down, or the column scrolls.** No board draws a scrollbar anywhere, despite
  §1B.1 establishing 16 scroll views and pass 2 delivering six scrollbar sprites for them. The
  scrollbar's width, inset and relationship to the paper edge are unspecified on every screen.
- **1b shows revenue and appropriations side by side while the sub-tab row highlights `Tax`.** The
  implementation has one category visible at a time (`DrawBudgetProcessTab` → `BudgetProcessCategory`).
  Either the design intends Budget to abandon its sub-tabs for a two-column always-on ledger — which the
  row arithmetic makes impossible — or the board is a composite. It needs saying which.

### C.4 ⛔ BLOCKING — four data visualisations were never aged

`UiPalette.GetCategoricalColor` is still `Color.HSVToRGB(hue, 0.65f, 0.9f)` on a golden-angle sequence —
saturated screen colour, untouched by the v2.0 pass. It drives four charts:

| call site | slices |
|---|---|
| `HemicycleRenderer` seats + legend | 4 parties |
| sector employment pie | 8 sectors |
| **spending pie** | **29 categories** |
| tax revenue pie | 13 types |

The mockups draw hemicycle seats in aged inks (`#9C4238`, `#62579F`, `#A8842E`, `#35619E`), so the design
**assumes an aged categorical set exists.** None was delivered. `polisim_palette.json` covers eleven area
hues and four semantic colours; it says nothing about categorical series.

This is the eleven-hue argument again, one level down: colour here keys a data visualisation, so it is
load-bearing by the generalisation recorded in §1 — and 29 mutually distinguishable aged hues is a
materially harder problem than eleven. **Left as-is, the Statistics tab renders a bright HSV rainbow on
aged paper** — the most visible way the illusion can break, and worse than the grey scrollbars §1B.1
worried about.

### C.5 ⚠ Party inks collide with area inks on the same screen

The four party inks are the four area inks, exactly:

| party | ink | collides with |
|---|---|---|
| National Labor Front | `#9C4238` | CrimeJustice — **and semantic `bad`** |
| Reform Union | `#62579F` | Sectors |
| Agrarian League | `#A8842E` | **Political** |
| Centrist Coalition | `#35619E` | Fiscal |

On the Politics tab the tab's own ink is `#A8842E` and the Agrarian League swatch is `#A8842E`. On 1c the
`majority of 1` warning prints in `#9C4238` — the same ink as the largest party's seats, two rows above.

This is the same defect §1B.5 just resolved for draft amber and Political, arriving from a different
direction: two load-bearing meanings sharing one hex. B9 requires the legend swatch to match its arc; it
does not require the arc to match an unrelated area accent.

Related: **the design's party names are invented.** `PartyArchetype` is `ProgressiveAlliance ·
ConservativeUnion · CentristCoalition · NationalistFront`; the boards use *National Labor Front · Reform
Union · Agrarian League · Centrist Coalition*. Only one matches, and `emblem_party_*` sprites exist for
the real four. Cosmetic in the mockup, but the ink→party mapping has to be re-keyed to the real set.

### C.6 ⚠ A third hue tint is used but not delivered

Inactive tab swatches use a knocked-back tint that is in neither the `ink` nor `lifted` table and is not
in `polisim_palette.json`. Six of eleven appear (`§A.3`); five do not, and there is no stated derivation
to compute the rest from. Either the five missing values or the rule that produces them is needed.

### C.7 ⚠ B4 — the design's ledger truncates

`§1C.2` records B4 as satisfied at the sprite level: *"no fixed-size text plate anywhere in the pack."*
True, and not the whole story. At the **layout** level 1b fixes the instrument-name column at `168px` and
clips overflow, and stat-tile labels are set to not wrap. Program and instrument names are longer than
168px at several of these sizes, and cabinet-minister names are generated.

*"A clipped number is a plausible wrong number"* applies to a clipped label too — `Veterans Benefits
Mandatory` clipped to `Veterans Benefits` is a different programme. Every fixed-measure text cell in the
spec must route through the shrinking measured-label path, never a clip.

### C.8 ⚠ B6 — the spec and the mockup disagree on which treatment is which

`§1C.2` B6: *"published = printed bulletin (solid frame + ref period + date + badge chip); live = desk
reading (dashed rule, unbadged)."*

1a draws the opposite: the **dashed**-border block (`1px dashed #B7A98C`) is the one carrying the
`PRELIMINARY` badge and the publication date, while the live desk readings sit on solid plates under a
`DOMESTIC BULLETIN — DESK READINGS, LIVE` caption.

The mockup's version is arguably the better one — a dashed rule reads as *provisional*, which is what
preliminary means. But the two documents state opposite rules for the same behaviour, and B6 is exactly
the behaviour where getting it backwards is invisible until a player trusts a wrong figure. One of them
has to be struck.

### C.9 ⚠ Named-but-undelivered sprites, and one date/decimal locale question

Eight sprite names appear in board captions with no file behind them anywhere in `Assets/Resources/`:

`ui_event_card` · `ui_status_ok` · `ui_stamp_holds` · `ui_stamp_verdict` · `emblem_state_seal` ·
`canvas_folder_country` · `canvas_btn_brass` · `canvas_btn_paper`

The first four have plausible substitutes in the delivered pack (the event card is a tinted
`ui_panel_paper` with a drawn left rule; the stamps can reuse `ui_stamp_carried` / `ui_stamp_rejected`
tinted). The last four do not — **the Canvas path has no button or folder art at all**, which is the
§1B.3 gap re-opening after `CANVAS_SPEC.md` appeared to close it.

Separately: every board sets decimals with a comma (`$29,3T`, `4,38%`) and dates in Swedish
(`12 maj 2026`, `14 november 2027`) while all UI copy is English (`Send to the floor`). That is a
locale decision nobody has taken. It interacts with B3 — the MoneyUnit formatter owns how a figure
prints, so the separator belongs there, not in art direction.

---

## D. THE TWO ARCHITECTURAL CONSTRAINTS — both respected

**1. Hybrid at screen granularity. ✅ Respected, with one clause needing restatement.**

Every board is wholly one side: 1a–1d IMGUI, 1f–1h Canvas. No board interleaves an IMGUI element into a
Canvas screen or the reverse. 1e makes the swap explicit and gives it a 60ms hold at 85% scrim precisely
so the changeover is invisible.

The one clause to restate: `§1C.3` says `ui_banner_hold` *"survives the whole sequence"*, and 1e's caption
repeats it. Read literally against *"IMGUI LAYER SUPPRESSED"* at t=180ms, that would mean IMGUI keeps
drawing over an active Canvas screen — element-granularity interleaving, and the thing the spike ruled
out. **1g resolves it correctly in the art:** the banner is drawn *by the Canvas screen*, pinned to the
bottom edge (`rgba(20,16,11,.9)` on `1px #3A2F1E`, padding `10/24`). So the rule is
**"every Canvas takeover draws its own copy of the hold banner"**, not "the IMGUI banner persists."
It should be written that way, because the current phrasing is an invitation to break the architecture.

1h omits the banner entirely — the one board that should carry it and does not.

**2. Transitions run from the IMGUI side. ✅ Respected explicitly and correctly.**

1e panel 2 states the measured constraint in the design's own words: *"ui_scrim_takeover fades in, drawn
BY IMGUI — no Canvas mode draws above OnGUI."* The scrim is one 256×256 radial-vignette sprite stretched
full-screen, animated by opacity alone — inside IMGUI's two legal runtime knobs (tint and opacity), so
the fade needs no baked frames. The design also draws the correct conclusion downstream: *"a document
sliding halfway over the ledger is impossible by measurement, and nothing in this system pretends
otherwise."*

---

## E. WHAT IS ACTUALLY BUILDABLE TODAY

Unblocked by every conflict above, because none of it touches the four blockers:

- the surface ladder, ink set and rule weights (`§A.1`–`A.2`) — already wired, needs verification only
- the tab strip's three-state treatment (`§A.7`), minus the five missing swatch tints
- the sub-tab row and plate treatment (`§A.8`)
- the two status-line states (`§A.6`) — B8's carrier, fully specified
- the dossier card and the generic stamp treatment (`§A.11`)
- the dual-siting build rule (`§A.12`) — unambiguous, and the constraint §1 flagged is resolved
- the hand-off envelope timings (`§A.13`)

**Gated on Elias reviewing the current chrome wiring in a live Editor.** That instruction stands and this
document does not supersede it — every item above changes the same screens the wiring already touched.
