# Claude Design asset request — PoliSim

**Status — ONE LIVE ASK (2026-08-28): §1, the eighth request — two boards for UI v3.0, Phase A.** The
seventh request (2026-08-27) was answered the same day and migrated per this document's charter
("appended to, then emptied on delivery"): `COMPLETED.md` §24, the rulings to `POLISIM_V2_SCREEN_SPEC.md`
§A.16. **§1 is the next ask** — boards, not sprites: *"Screen 0, The Desk, folded"* and *"the rail"*, drawn
at 1280×720 first against three annexes we supply (the census of the landing screen's text, the inventory
of every instrument with its measured minimum size, the captures). **§E5** (two strip-cut findings) is
still open and travels in the same send. What else is here: **§0** the delivered set as it stands, **§4**
what is costed but not yet requestable, and **§5** the standing conventions.
**Date:** 2026-08-28.

**Standing rule: a count in prose is a cached value with no expiry** (working-discipline rule 12).
Before trusting any number in this document, re-derive it: sprites with
`find Assets/Resources/Art/UI -name '*.png'`, chrome coverage with `ChromeV2CoverageCheck`, stat icons
with `StatIconCoverageCheck`, portraits with `PortraitCoverageCheck`, deliveries with
`DeliveredAssetCheck`, importer state with `ImporterSettingsCheck`, screen inventories from the enums
(`StatNodeId`, `UiPalette.SystemArea`, `ConsolidatedTab`, `CabinetPortfolio` ×
`CabinetSystem.CandidatePool`, `CountryId`, `PartyArchetype`). This document has been the failure that
rule exists to catch at least four times.

---

## 1. The eighth request — two boards for UI v3.0, Phase A (2026-08-28): "Screen 0, The Desk, folded" and "the rail"

**What this asks for, in one sentence:** two boards, drawn at **1280×720 first** (then 1600×900 if you wish) — *"Screen 0, The Desk, folded"* and *"the rail"* — designed against three annexes we supply below: the census of every text element on today's landing screen with its class, the inventory of every instrument the game already draws with its **measured** minimum legible size, and the current landing-screen captures. No sprites are requested by this ask; a gap a board proves becomes a follow-up ask, costed, never an inline invention.

**Why (the direction, one paragraph).** UI v3.0 is *the desk with fewer words, not a different desk* (`POLISIM_UI_V3_DIRECTION.md`, V3-R1). Two altitudes, one idiom: the landing surface becomes an instrument stage — full-bleed, graphical, nearly wordless — while the deep screens (Budget, laws, the statistics ledgers) stay the documents they are. Same paper, inks, fonts, sprites and stamps; your eleven boards, the 96-sprite pack and every capture carry over. The shell that makes room for the stage is **built** (Phase A: the fold — `ShellFoldState` OPEN/FOLDED, the chrome column and the tab tongues collapsing to one icon rail; the mechanism is structure and gets re-skinned, not re-architected, when your board lands). What is **not** built is the stage itself: The Desk is a board first (this ask), built second (Phase B).

### 1.1 Board one — "Screen 0, The Desk, folded"

The landing surface in the FOLDED shell: the rail at the left (board two), the stage taking the rest — at 1280×720 that is **1229 × 691 px of desk inside the 2 % margin, of which the stage is ≈ 1149 × 691 after the rail (39 px cell + sheet padding) and the 24 px column gap**; at 1920×1080 the stage is ≈ 1751 × 1037 (rail cell 55 px). Composed from the instruments in Annex B — the world map, the compass, the approval attribution, the sparkline strip, the calendar sheet, the stamps, the stepped rule — and nothing authored: **everything on The Desk is derived, attributed and drawn.**

**Hard constraints (V3-R3, binding):**

- **The text budget is absolute:** captions at mono 9.5 (Courier Prime, the document face) and instrument labels only; no sentences, no paragraphs, no restatements. A number appears as an instrument (a dial, a bar, a rule, a sparkline) with the numeral as the instrument's label, never as a text row.
- **The census is the content list:** every class-(a) element in Annex A is required content (as an instrument or a label, not as the prose it is today); class (b) may return **only as an instrument**; class (c) never returns (it is already cut).
- **No new hues, no new fonts, no Canvas** — the eleven area inks, the semantic three, the aged paper set (`PoliSimTheme.cs`, `POLISIM_V2_SCREEN_SPEC.md` §A.3), Pagella and Courier Prime as chosen. The stage is IMGUI like the frame it folds.
- **Delivered sprites plus primitives:** the 148 sprites on disk (§0) and rule 10's procedural marks — axes, lines, dots, bars, rules, the stepped rule, the glow. A gap the board proves (a dial face, a compass rose, a stamp we do not have) becomes a follow-up ask with its cost stated; do not draw one in as if it existed.
- **Instant flip:** the fold does not tween; the board shows one state (folded) and the rail's toggle is the way back.
- **The floor first:** 1280×720 is where graphics-first pays or fails; the measured minimums in Annex B are at that size and at 1080p — an instrument placed below its minimum is a board defect, not a build problem.

**The three deviation conventions you already know, restated because they bind here:** neutral valence (no instrument may look good or bad by its shape alone — `GetDeltaColor` keys to *good*, not to *up*, and the inks carry it); no invented data (every figure on the board must be one the inventory says the game holds; a placeholder numeral is fine, a placeholder *stat* is not); IMGUI adaptations declared (a treatment IMGUI cannot draw — a runtime blur, a non-rectangular mask, a tween — is named on the board as "adapt", never assumed).

### 1.2 Board two — "the rail"

The FOLDED chrome. Built in Phase A as an icon rail on the paper sheet the column stands on; the board re-skins it. **Required contents, exactly (V3-R2):** the six navigation icons (the tongues' own: `icon_nav_statistics`, `icon_nav_decisions`, `icon_nav_demographics`, `icon_area_fiscal`, `icon_nav_policylaws`, `icon_area_political`; the active one in its area ink behind a spine, the others in the tab-swatch tint — §A.3's third column), the calendar chip (the pad's own materials: month and day numeral), the status dot carrying B8's two states faithfully (HELD amber **with the glow**, `0 0 6px rgba(212,167,44,.7)`; RUNNING green, no glow), and the fold toggle. **Nothing else.** The rail's measure is derived from the icons' own 24-unit grid — a cell is the grid plus 10 units of air each side (55 px at 1080p, 39 px at 720p, 64 px at 1440p) — so the board may move the air, not the derivation. The built rail is on film in Annex C (`v3a_1280_02a_statistics_domestic`, `v3a_1920_…`) so you draw against the thing that exists.

### 1.3 The annexes

- **Annex A — the census** (below): every text element on the landing screen and the OPEN chrome column, with content, px size at 720/1080, role and class, counted.
- **Annex B — the instrument inventory** (below): renderer, what it takes, data and honesty class, whether it stands alone, and the minimum legible size **measured on film** with the break stated.
- **Annex C — the captures** (in the send package, `captures/`): the landing screen in both shell states at 1280×720 and 1920×1080 (`v3a_<size>_02a_statistics_domestic` folded — the default — and `…_open`), the rail as built, the ladder films behind Annex B (`v3a_ladder_<size>_ladder_<kind>`), and for reference the OPEN chrome column's own text (`v3a_<size>_02a_statistics_domestic_open`, `_rows`, `_deep`).

**What comes back and where it lands:** two boards on the live screens file (1m, 1n, in your numbering) or as PNG at 1280×720; any gap costed in a line under each. The day they land: Phase B builds The Desk against board one (`v3desk_*` capture family), the rail is re-skinned against board two, every (b) in Annex A is resolved as an instrument or dropped, and this section migrates to `COMPLETED.md`.

### Annex A — the census: every text element on the landing screen (Statistics › Domestic) and the OPEN chrome column, from the film (`clear_p1c_1920_02*`, `clear_p1c_1280_02_statistics`; code at HEAD `23cbb84`)

Classes (the direction's taxonomy): **(a)** load-bearing · **(b)** restating an instrument that exists or could · **(c)** decoration. Classification honesty: unsure between (a) and (b) is (a). Sizes are the rendered font in px at **1280×720 / 1920×1080** (the two ends of the film; 1600×900 and 2560×1440 lie between and above — every style is `Screen.height`-derived and clamped, `GameController.RescaleStylesToScreen`, widget type at `clamp(h/1080, 0.6, 1.5)` × the theme constants). "×n" = the element repeats.

| # | where | element (content) | px 720 / 1080 | role | class |
|---|---|---|---|---|---|
| C1 | chrome · top banner (only while an event is live) | `BREAKING: {event name}` | 20 / 30 | event headline | (a) |
| C2 | | the event's description sentence | 16 / 24 | narrative | (a) — the event's only text |
| C3 | | `Effects: GDP ±x.x%, Inflation ±x.x pts, Approval ±x.x` | 16 / 24 | three deltas as a sentence | (b) — an event stamp with three instruments; the map's event dots already carry the event |
| C4 | chrome · (only at game over) | `GAME OVER` | 20 / 30 | state | (a) |
| C5 | | the game-over reason | 16 / 24 | | (a) |
| C6 | chrome · calendar sheet | `{Country} - Year {N}` | 23 / 35 | country name + elapsed turn | (a) — the name has no other home in the chrome; "Year N" is the turn count, not the pad's calendar year |
| C7 | | `JANUARI 2029` (MMMM yyyy, OS locale) | 23 / 35 | the month page's own label | (a) |
| C8 | | weekday abbreviations ×7 | 13 / 19 | instrument labels | (a) |
| C9 | | day numerals 1–31 (spent days struck; today tinted; up to four area dots) | 15 / 23 | instrument | (a) |
| C10 | | `This Month` | 23 / 35 | section header | (b) — the page above names the month and the rows date themselves (the playtest-3 "Derived" precedent) |
| C11 | | `{m}/{d}` ×n | 16 / 24 | row date | (a) |
| C12 | | marker label in area ink (`Unemployment published`, `Budget bill due`, …) ×n | 16 / 24 | what lands that day | (a) — cross-references the grid dot (that + whose area), says what; not a restatement |
| C13 | | `Nothing scheduled this month.` (only when empty) | 16 / 24 | empty state | (b) — the empty ledger under its rule could carry it |
| C14 | chrome · policy preview | `This Year's Policy` | 23 / 35 | panel header | (b) — `Estimated Effects` and the rows name the subject |
| C15 | | ~~`Show tab guide` / `Hide tab guide` button + its paragraph naming the pre-consolidation ten tabs~~ | (skin) / 16–24 | help text | **(c) — CUT `23cbb84`** |
| C16 | | `Estimated Effects` | 23 / 35 | the list's label | (a) |
| C17 | | horizon buttons `1 Day` `1 Week` `1 Month` `Full Turn` | 18 / 26 | controls | (a) |
| C18 | | `Over the next {horizon}` | 16 / 24 | | (b) — restates the selected horizon button |
| C19 | | `(±5-10% margin of error)` | 16 / 24 | the estimate's error band | (a) — no instrument carries it (a band on the figures could; until then (a)) |
| C20 | | `- a linear/compounding-scaled display estimate from the full 365-day projection, not a simulated sub-year value.` | 16 / 24 | methodology disclosure | (a) — honesty text; unsure → (a) |
| C21 | | ~~`Projection only, not a guarantee.`~~ | | hedge | **(c) — CUT `23cbb84`** |
| C22 | | eight effect rows `GDP Growth: +x%` … `Net Budget Impact: $x` (good/bad ink) | 16 / 24 | the estimate's figures | (a) — the figures; their sentence form is pre-v2 (on the stage they are instruments) |
| C23 | chrome · pinned strip · calendar pad | `JAN.` (MMM, OS locale) | 11 / 16 | | (a) |
| C24 | | `30` | 33 / 50 | | (a) |
| C25 | | `2029` (Courier) | 12 / 17 | | (a) |
| C26 | | `Time running` (RUNNING plate) | 16 / 24 | state | (b) — the green lamp beside it carries the state; the rail keeps the lamp |
| C27 | | `TIME PAUSED: {reasons} to continue.` (HELD plate) | 20 / 30 | the resolving screens named | (a) — B8's load-bearing half |
| C28 | | `Pause` `1x` `2x` `3x` `Saves` | 23 / 35 | controls | (a) |
| S1 | content · tab strip | tongue labels `Statistics` `Decisions` `Demographics` `Budget` `Policy/Laws` `Politics` (over their icons) | 13 / 19 | navigation | (a) OPEN — folded, the icons carry it and the labels are (b) |
| S2 | content · sheet | `Statistics` | 23 / 35 | header | (b) — the pulled-forward tongue says it |
| S3 | sub-tabs | `Domestic` / `International` | 18 / 26 | navigation | (a) |
| S4 | caption | `DOMESTIC BULLETIN — DESK READINGS, LIVE` | 14 / 21 | B6's screen-level live/published carrier | (a) — its first half restates the sub-tab; its second half is the only statement that these are live readings |
| S5 | | `Domestic` | 23 / 35 | header | (b) — the selected sub-tab says it |
| S6 | tiles ×10 | labels `GDP` `UNEMPLOYMENT` `INFLATION` `APPROVAL RATING` `CURRENCY STRENGTH` `POVERTY RATE` `GOVERNMENT DEBT` `DEBT-TO-GDP` `CREDIT RATING` `BUDGET BALANCE` | 7 / 10 | instrument labels | (a) |
| S7 | | values (`$29.8T` `4.37` `2.20` `47.7` `101.4` `18.3` `$38.8T` `130.1` `AAA` `-$5.46T`) | 28 / 42 (shrink to fit, floor 11) | figures | (a) |
| S8 | | unit `%` ×5 | 9 / 13 | units | (a) |
| S9 | | GDP delta `+0.00%`; `OUTLOOK +` / `OUTLOOK -` on Credit Rating when not Stable | 9 / 13 bold | | (a) |
| S10 | derived ledger | row names `GDP per capita` `Tax burden` `Government spending` `Deficit`/`Surplus` `Primary deficit`/`Primary surplus` | 16 / 24 | | (a) |
| S11 | | figures (`$85.8k` `19.3%` …) | 16 / 24 | | (a) |
| S12 | | trailing `of GDP` ×3, `of GDP, excl. interest`; the empty states `no population` / `not yet computed` + `advance a year` | 16 / 24 | units; empty states | (a) the units; (b) the empty-state phrases |
| S13 | | `Sector shares of GDP` | 16 / 24 | group label | (b) — each row's trailing `of GDP` says it and the rows are named sectors |
| S14 | | eight sector rows: names, figures, `of GDP` ×8 | 16 / 24 | | (a) — a unit column read down, not eight restatements |
| S15 | | `Sector shares of GDP: not tracked for this country.` (conditional) | 16 / 24 | empty state | (b) |
| S16 | six graphs | titles `GDP` `Unemployment` `Inflation` `Approval Rating` `Poverty Rate` `Debt-to-GDP` | 16 / 24 | instrument labels | (a) |
| S17 | | the `(dashed = next-year estimate)` suffix ×3 | 16 / 24 | a legend key | (b) — a key could be an instrument (1l drew the line weights; the key is what the suffix restates) |
| S18 | | change label `+2,5%` per graph | 16 / 24 bold | | (a) — prints in the OS culture (`+2,5%`) where the tiles print invariant (`+0.00%`); an existing inconsistency, logged, not v3's |
| S19 | | `< Older` / `Newer >` | 10 / 16 | controls (disabled on one page) | (a) |
| S20 | | range label (blank on one page; `Last 50 years`; `N-M years ago`) | 10 / 16 | | (a) |
| S21 | | axis labels min / mid / max ×3 per graph | 10 / 16 | | (a) |
| S22 | | threshold labels `NAIRU`, `comfortable` | 10 / 16 | | (a) |
| S23 | | `No data yet - advance a year.` (conditional) | 16 / 24 | empty state | (b) |
| S24 | Society box | `Society` | 23 / 35 | header | (b) — the rows name themselves; their area inks carry the grouping |
| S25 | | rows `Youth unemployment` `Life expectancy` `Income inequality (Gini)` `Real wages` `Productivity` `Housing overburden` (EU five only) `Homeownership` `House prices` | 16 / 24 | | (a) |
| S26 | | figures | 16 / 24 | | (a) |
| S27 | | trailing `of youth labor force` `years at birth` `0-100 scale` `index, 100 = start of term` ×2 `$ per hour (PPP), against your own past` `spend >40% of income on housing` `of households` / `of households (primary metric)` | 16 / 24 | units and their definitions | (a) — the two caveats (`against your own past`, `(primary metric)`) are rulings made visible; unsure → (a) |
| S28 | As published | `As published` | 23 / 35 | header | (b) — every title beneath carries "as published" |
| S29 | | `What the public sees: lagged, and revised as later estimates arrive.` | 16 / 24 | | (b) — restates B6's two channels, the badge chip (published) and the dashed frame (preliminary) |
| S30 | | ~~`Compare against the live figures above.`~~ | | instruction | **(c) — CUT `23cbb84`** |
| S31 | three published graphs | `GDP as published` `Unemployment as published` `Inflation as published` + change label, page row, axis labels, date axis, `latest: {value} ({lag})`, the badge chip `PRELIMINARY` / `FINAL` | 16 / 24; 10 / 16 | | (a) |
| S32 | | range buttons `1yr` `5yr` `All` | 10 / 16 | controls | (a) |
| S33 | bulletin | `PRELIMINARY`/`FINAL` chip · `Poverty rate as published: 18.3` · `for Jan 2028 - Dec 2028, released 1 Mar 2029` | 16 / 24 | B6's channel 1 | (a) |
| S34 | | `{label}: not yet published - the first release is still ahead.` / the graph's `Not yet published - the first release is still ahead.` (conditional) | 16 / 24 | empty states | (b) |

**Counts (element kinds, ×n collapsed):** (a) **44** · (b) **18** (C3 C10 C13 C14 C18 C26 S2 S5 S12-part S13 S15 S17 S23 S24 S28 S29 S34, plus S1's labels once folded) · (c) **3 kinds, 4 text elements** — cut at `23cbb84`. Nothing on this screen was cut that a board might have wanted back: every (b) stands and waits for the board.

### Annex B — the instrument inventory: every self-contained figure the code already draws, with its minimum legible size measured on film

The ladder films are `v3a_ladder_1920_ladder_<kind>` (1920×1080: body type 23 px) and `v3a_ladder_1280_ladder_<kind>` (1280×720: body type 16 px), each rung captioned with its size in Courier under it; the sizes below are absolute pixels and hold at both (the type-bearing instruments were re-read on the 720p film). Read this table with the direction's rule: **candidates only, no new instruments.** "Honesty class" is the data's provenance vocabulary the code already uses — LIVE (`Country.State`, the desk reading), PUBLISHED (`Country.Published`, lagged and revisable — B6's badge and dashed frame), DERIVED (`DerivedStats`, arithmetic on live values), LEDGER (Class A attribution terms, recorded at the boundary and audited), SEED (`WorldFactory` constants, tagged `[VERIFIED]`/`[PROVISIONAL]`), CHROME (delivered art, no data). "Stands alone" = draws correctly outside its screen with only the data named.

| # | instrument (the direction's name) | renderer, entry point | takes | data · honesty class | stands alone? | minimum legible size — MEASURED, and the break |
|---|---|---|---|---|---|---|
| I1 | **the world map** | `MapRenderer.Draw(Rect, countries, playerId, eventMarkers, turn, fadeTurns, labelStyle, out clicked…)` (`MapRenderer.cs:105`) | a Rect (host 260 px tall, full column width) | six GDP-sized nodes at fixed illustrative positions (`CountryMapPositions` — **not geography**: no polygons, no coastlines), trade-volume lines, fading event dots · LIVE (`Country.State.GDP`, `TradePartners`, the event markers) | yes — one call, its own textures | **with names: 360×216 (type 12–17)** — at 240×144 the names collide (they do not declutter, and "United States" / "France" touch at every size, a pre-existing placement defect); **nodes and lines alone: 120×72** (six nodes distinct), merging at 90×54 |
| I2 | **the compass** | `PoliticalCompassRenderer.Draw(Rect, countries, playerId, labelStyle)` (`:118`) | a Rect (host square, `clamp(0.4·h, 260, 520)`) | one dot per country on two 0–100 axes, `GetFiscalSizeAxisValue` / `GetRegulationWelfareAxisValue` · DERIVED from LIVE (+ the seed portfolios, `[PROVISIONAL]` until the database session) | **yes, since R-SP4 (2026-08-28)** — the renderer declares an honest footprint (`Footprint`: the plot square plus the caption band at the width the captions need, wrapped never shrunk) and containment-asserts the plot and both captions inside the rect it is given; the first filing of this row found the captions loose (GUILayout labels after the plot) | **with names: 240×240 at 8-px type** (crowded), 360 at 12 px comfortable; **dots alone: 90×90**, merging at 64 |
| I3 | **the approval face with its nine-term attribution** | **does not exist as a face or a dial.** What exists: (a) the Approval headline **tile** (`PoliSimWidgets.StatTile`, `:387`); (b) the Approval **graph** (`GraphRenderer.Draw`, `:118`); (c) the attribution **ledger panel** (`StatTracePanel.Draw(country, gapStance, style, style, width, hostHeight)`, `StatTracePanel.cs:149` — GUILayout, no Rect; `MeasureHeight` first) with **13 term rows** (12 Class-A terms + ClampLoss; the nine the direction names are the nine non-misery terms) and up to four dated events, every row `fill = -1` (no gauge: "there is no proportion here") | (a) a Rect + scale; (b) a width; (c) a width + host height | (a) LIVE; (b) LIVE history (`StatHistory.ApprovalRating.Quarterly`); (c) LEDGER (`Country.ApprovalLedgerLastPeriod`, Class A terms + Class B events, the boundary identity audited) | (a) yes; (b) yes (its own texture cache per instance); (c) yes given the country — its section selection is static (`RequestSelection`, committed in `MeasureHeight`) | **tile: label legible to scale 0.8 (264×98, label 8 px), the hero figure alone to scale 0.3 (99×37, 13 px)**; **graph: 240 px wide** at 9-px furniture (title, change, axis), the line alone to 120; **ledger: 360 px wide at 13-px type (the header line, the terms, the indented misery sub-rows all read); at 240 / 9 px the sub-row labels ("· unemployment above NAIRU") overflow their name column at the 8-px floor - the guard's own record on both films**. **A face or dial would be a NEW instrument** — Phase B's, drawn on the board first |
| I4 | **the sparkline strip at 1l's weights** | `GraphRenderer.DrawSparkline(Rect, history, color, maxPoints = 40)` (`:914`); the strip is `PolicyScreenStatsRenderer.Draw(area, country, labelStyle, availableWidth, maxStats = 4)` (`:149`, GUILayout) — one chip per stat: icon 22/16·type, value, trend arrow, sparkline 72×20 per 16 px type | a Rect (the bare line); a width (the strip) | LIVE (`Country.State`, never published — the class doc's ruling); history for the line · **1l's weight law applies to it — R-G4, `thickness = max(2, round(rectHeight/34))` device px, so 2 px at the chip's 20–35 px rects (the graph's 3 px at a 90 px rect is the same law at a taller rect)**; no projection segment (a sparkline has no estimate to dash). *(Corrected 2026-08-28, R-SP3: this row's first filing said no 1l weight reached the sparkline — it does, through the one renderer the strip and the graphs share, `GraphRenderer.BuildSparklinePixels`; the eye pair at 1280 and 2560 is on film.)* | yes (the line); the strip needs `PolicyScreenStats.GetStatsForArea` | **line: the shape reads to 36×10**, comfortable 54×15 (the chip's own 72×20); a dash at 24×7. **strip: 280 px wide at 9-px type** (four chips stacked), 380 at 12 px in two columns; 200 / 8 px is the floor and reads as a footnote |
| I5 | **the calendar sheet** | `GameController.DrawCalendarMonthGrid(monthStart, today, markers)` + `DrawCalendarMonthLedger` (`:2961`, `:3141`; GUILayout) — the month page (weekday header, day cells, struck spent days, up to four area dots per day) and the dated ledger | a width (the column) | markers from `BuildCalendarMonthMarkers` — release days (`ReleaseCalendar`), pending bills' days, the election cycle, divisions, events · LIVE schedule facts | no — a GameController method reading the simulation's calendars; extractable | **420 px wide at the sheet's own type** (16 px at 720p, 23 at 1080p): at 320 the weekday row clips its seventh column; at 240 the month header wraps and the ledger's names break mid-word; below that the numerals overlap |
| I5b | the calendar **pad** / the rail's **chip** | `DrawCalendarPad()` (`:4390`, size from body type: 64/12.5 × label) and `DrawRailCalendarChip(cell, …)` (v3a — the pad's sprite at cell size, month + day) | the pad: none (its own size); the chip: a cell width | `SimulationManager.CurrentDate` · LIVE | the chip yes (a cell width); the pad no | **chip: a 32-px cell** (32×36: month 9 px, day 13 px); at 24 the day numeral has no line box left (the guard's own record, both films); the rail's 39 px at 720p and 55 at 1080p sit above the floor |
| I6 | **the event / alert stamps** | the division verdict stamps: `ui_stamp_carried` / `ui_stamp_rejected` (170×50 @1×, rotation baked) via `UiPalette.DrawTintedIcon` (`GameController.cs` ~`:8605`); the urgency chip: `PoliSimWidgets.Stamp(Rect, text, style, ink, borderInk, borderWidth, rotation)` (`:272`, procedural, −2°); the badge chip `PoliSimWidgets.Badge` (`ui_chip`); the HELD lamp `DrawHeldLamp` (v3a) | a Rect | CHROME (the sprites) · the chip's text is (a) content; the lamp is B8's state | yes | **sprite stamp: 68×20** (the word reads), comfortable 85×25, 51×15 the break; **procedural chip: 9-px type** (83×20), 8 the floor, 7 breaks; **lamp: 8 px with its glow readable**, 6 the dot alone, 4 a speck |
| I7 | **the stepped rule** | `GameController.DrawMagnitudeSteps(Rect, tier, stepWidth, gap)` (`:7538`) — always four steps, filled to the tier, one ink | a Rect | a law's magnitude tier (1–4) · SEED/content | yes | **32×8** (four steps and the fill count read), comfortable 48×12; 24×6 marginal; 16×4 dots |
| I8 | the hemicycle | `HemicycleRenderer.Draw(title, seats, labelStyle)` (`:38`; GUILayout, **fixed `AreaWidth 340 × AreaHeight 190`**, five rows, 10 px dots) + a legend of `LedgerRow.DrawReadOnly` rows | none (its constants) | `Country.ParliamentSeats` · LIVE (seat drift) — re-keys under item 10 | yes at its one size | **does not shrink** — no size parameter; the legend rows need ≥ ~430 px of row width at 23-px type (a 440 px frame clipped "Nationalist Front" by 9 px); a Phase B change if the board wants it smaller |
| I9 | the pie | `PieChartRenderer.Draw(title, slices, labelStyle, valueFormat, moneyUnit)` (`:58`; **fixed `Diameter 120`**, solid — no donut) | none (its constant) | demographics shares · LIVE | yes at its one size | **does not shrink**; and the eight-ink cap makes it a ledger past eight categories (`RankedBarLedgerRenderer`) |
| I10 | the line graph (1l's weights live here) | `GraphRenderer.Draw(title, history, projected, labelStyle, higherIsBetter, moneyUnit, threshold…)` / `DrawPublished(...)` (`:118` / `:214`; GUILayout; texture 300×90, display height `clamp(0.075·h, 50, 90)`) — title + signed change, page row, axis min/mid/max, threshold label; the published form adds the date axis, release markers, the badge, the dashed frame | a width | LIVE history (`StatHistory.*.Quarterly`, 250 entries) or PUBLISHED series | yes (one instance per chart — it caches its texture) | **240 px wide at 9-px furniture**; 320 at 12 px comfortable; the plot line alone reads at 120 |
| I11 | the policy web | `PolicyWebRenderer.Draw(Rect, labelStyle, country, pinnedPolicy, pinnedStat, out…)` (`:731`) — the ring, ~73 nodes sized by degree, edges on hover/pin, solid = DERIVED (a ledger term) / dashed = DECLARED | a Rect (host `clamp(…, 0.5·h, 0.92·h)` square) | the edge set per country · LEDGER (Derived) and DECLARED | yes | **with labels: 240×240 at 9-px type** (crowded), 360 at 13 px comfortable; **the ring alone reads at 180**, a blob at 120 |
| I12 | the flag | `IconLibrary.GetFlag(CountryId)` — full-colour art, never tinted (`:163`) | a Rect (3:2) | CHROME | yes | **24×16** recognisable (stripes and canton), 30×20 comfortable; 18×12 breaks |
| I13 | the area / nav icons | `IconLibrary.GetAreaIcon(area)` / `Get("icon_nav_*")` through `UiPalette.DrawTintedIcon` — white-on-alpha, tinted (`:38`, `:323`) | a Rect (square) | CHROME | yes | **12 px** readable, 16 comfortable, 10 recognisable, 8 a blob (§5.2's 22 px guidance stands) |
| I14 | the read-only ledger row (the gauge lane) | `LedgerRow.DrawReadOnly(Rect, name, fill, figureText, trailingText, barInk, nameStyle, figureStyle)` (`:399`) — name, track + fill, figure, unit; `fill < 0` = no gauge | a Rect (its height from `LedgerRow.Height(style)`) | any proportion · LIVE or DERIVED | yes | **10-px type** (row 33 px tall) reads; 8 px is the floor and reads as a footnote — a document form, not a stage instrument; listed because the direction's "a bar" is this lane |
| I15 | the bars | `UiPalette.DrawDivergingBar(Rect, value, displayRange)` (`:602`, fills outward from centre, green right / red left); `PoliSimWidgets.ThresholdBar(Rect, fraction, thresholdFraction, fill)` (`:537`) | a Rect | a signed alignment; a share with a threshold · LIVE/DERIVED | yes | **40×14** (the fill and the centre line / tick read), comfortable 64×14; 24×14 breaks |
| I16 | the stat tile | `PoliSimWidgets.StatTile(Rect, label, value, suffix, delta, deltaIsGood, subLabel, area, scale, barFraction, thresholdFraction)` (`:387`; height from `StatTileHeight(scale, hasDelta, hasBar)`) — the printed plate, label at 10·scale, hero figure at 42·scale (shrinks to 11), delta at 13·scale | a Rect + a scale | LIVE | yes | **scale 0.8 (264×98)** with its label; the hero figure alone to scale 0.3 (99×37) |
| — | **not instruments, listed so the board does not ask:** the portraits (`DrawPersonPortrait`, a person, not a reading), the country selector and the signing document (Canvas screens, their own class), the ranked bar ledger (a table), the trace panel's chips (I4's strip) | | | | | |

**How the sizes were measured.** `GameController.DrawInstrumentLadder` (harness-only; no player path reaches it) draws one kind per capture on a paper sheet at a descending run of sizes, each rung by the instrument's own renderer on the live game's data, with a Courier caption of the size; instruments that carry type take a label style scaled with the rung and floored at the guard's 8 px, so a 64 px map is not measured with 24 px names. The break is read by eye on the film at both sizes and stated per row; where the code's own guards recorded a rung (the chip's day numeral at the 24-px cell, the sheet's at its narrowest rungs), the log line is the break's second witness (`shot_v3a_ladder_*.log`, reported not gated).

---

## 0. The delivered set at HEAD — derived 2026-08-27 (after Progress5), so nothing is asked for twice

**The checks (logs `..\PoliSim-captures\logs\p5_*_20260827_*.log`):**

| check | result | what it enumerates (rule 14) |
|---|---|---|
| `DeliveredAssetCheck` | **0 missing from 0 root zips, 0 missing from 14 archived packs** (Progress5.zip 16 of 16; the superseded pre-v2.0 chrome skipped via the manifest's `!` rows) | every archived zip's entries against `Assets/` |
| `StatIconCoverageCheck` | **19 of 19 resolve** | every `StatNodeId` icon + `menu_pattern_tile` |
| `PortraitCoverageCheck` | **25 of 25 resolve** (18 ministers across six portfolios + 7 Fed chairs; the sitting chair reported, not counted) | every `CabinetSystem.CandidatePool` and `FederalReserveSystem.CandidatePool` member through `IconLibrary`'s accessors |
| `ChromeV2CoverageCheck` | **50 of 50 both directions** | `ChromeManifest.txt` against `Chrome/` |
| `ImporterSettingsCheck` | **148 sprites, 0 errors, 0 warnings** (112 white-on-alpha tinted, 35 full-colour, 1 tiling) | every `*.png` under `Assets/Resources/Art/UI/`, asserted against the imported texture |
| `PartyMarkCoverageCheck` | **PARTY SYSTEM NOT PRESENT — VERIFIED NOTHING** (honest; item 10's gate) | seeded parties' marks — none exist on `main` |

**On disk (148 PNGs):** Chrome 50 · Emblems 9 (4 `emblem_party_*` + 5 `mark_party_*`) · Flags 6 · Icons
14 (4 `icon_nav_*` + 10 `icon_area_*`) · **Portraits 25** · Stats 43 · Textures 1.

**Coverage by display enum — complete, nothing to ask:** `StatNodeId` 18/18 · `ConsolidatedTab` 6 of 6
tabs draw an icon · `PartyArchetype` emblems 4/4 · `CountryId` flags 6/6 · **`CabinetSystem.CandidatePool`
18/18** · Fed chair pool 7/7 · `menu_pattern_tile` 1/1 · the chrome pack 50 = 50.

**Delivered and held — DO NOT RE-REQUEST, the wiring is ours:** 25 of the 43 `Stats/` sprites have no
call site (19 `icon_stat_*` for stats without a `StatNodeId`, plus `icon_trend_up/down/flat`,
`badge_preliminary/revised`, `icon_release_marker`); 8 of the 10 `icon_area_*` icons are drawn and
unplaced; 7 chrome names have no load call (`ui_frame_double`, `ui_btn_disabled`, `ui_stamp_draft`,
`ui_portrait_frame_oval`, `ui_btn_paper_canvas` + `_hover` + `_pressed`); the 5 `mark_party_*` await
seeded parties (item 10). These are on the roadmap as place-or-hold items and in `COMPLETED.md` §33 as
the record.

**The one name the enums show without art, by design:** the sitting turn-0 Fed chair (Harriet Ellsworth,
`WorldFactory.cs`, deliberately outside the candidate pool) — a roadmap question, not a gap; §4.

---

## 4. Costed, NOT requestable yet — the next ask starts here (not sent)

Derived from the screens that exist and the one that does not (`POLISIM_V2_SCREEN_SPEC.md` §A.14;
`MISSING_PREREQUISITES.md` §D/§E):

- **Election night, Canvas screen 1h (item-10-gated, 13 Sept 2026).** Against the delivered set: paper,
  the ornate frame, the masthead, the `SEATS DECLARED` chip (`ui_chip`) and the party-ink swing figures
  (procedural, rule 10) all exist. **Two things do not:** one identity mark per SEATED party for the
  legend row — count unknown until the party seeds land (five `mark_party_*` on disk, rule 9a's
  original-art-never-the-registered-mark rule applies to every one) — and the verdict stamp, which is
  §A.11's generic procedural treatment unless Design asks for baked art at the 1h board. The R5 hex
  exchange (an ink-safe LP `DisplayColor`, Sweden's set) travels with the same gate.
- **Two `StatNodeId` icons** — youth unemployment and life expectancy — when those Society rows are
  promoted to nodes (`MISSING_PREREQUISITES.md` §E4); the promotion is ours and comes first.
- **A portrait for the sitting turn-0 Fed chair** (Harriet Ellsworth) only if the roadmap's
  sitting-chair-row question resolves toward one — the same envelope as the batch (512×640 @2×, the
  Portraits class, `portrait_fedchair_harriet_ellsworth`).
- **The roster beyond eighteen** — flagged 2026-08-25, not requested: more portfolios means more
  candidates and a larger batch later; a future ask for "twelve" or "fifteen" is a fresh request, not
  scope creep against the one just delivered.

Nothing else. The two built Canvas screens (1f the selector, 1g signing) needed nine pass-3 sprites and
consumed six of them; the three paper-canvas button states are delivered and unwired. Boards 1k and 1l
asked for no art.

---

## 5. Conventions — standing, unchanged; read before producing anything

### 5.1 Explicitly OUT of scope — please do not produce these

- **Anything the seven data renderers already draw**: axes, gridlines, tick marks, plot lines, threshold
  lines, bars, area fills, legends, sparklines, map shapes, policy-web nodes and edges, hemicycle seats,
  pie wedges, compass dots. All procedural, per working-discipline rule 10 — these render real tracked
  simulation data rather than a picture, and that is exactly what rule 10 protects. **Frames, plates and
  paper AROUND them are in scope; the data marks inside are not.**
- **Any sprite already delivered** — §0 lists the held stock. Check `Assets/Resources/Art/UI/` before
  producing anything; re-derive the count from the filesystem (rule 12).
- **Typefaces.** Already chosen, open-licensed and imported: TeX Gyre Pagella (display + body) and
  Courier Prime (document artifacts). Do not propose or supply fonts.
- **Cabinet portraits beyond the eighteen.** A larger roster later is a fresh request (§4).
- **`menu_pattern_tile.png`** — delivered, imported and wired (2026-08-02, `DrawCountrySelector` and the
  Canvas ground). **Do not re-request it.**
- **Pre-coloured trend arrows or badges.** Colour is applied at runtime; see §5.2.
- **Real-world agency logos, national statistics-office branding, or any real party's registered mark.**
  Rule 9 (split 2026-08-11) lets real INSTITUTIONS be named — parties, chambers, formulas — but people
  stay fictional, and rule 9a makes every party mark ORIGINAL ART, recognisable by silhouette and real
  colour, never the organisation's own mark (the S banner-not-rose and the Democrats' torch-not-donkey
  precedents). Someone else's trademark is not ours to draw.

### 5.2 Format & technical spec

**Tintable art — the default, and still binding.** PNG 256×256, 8-bit RGBA, transparent background,
**authored pure white** with all shape information in the alpha channel; tinted at draw time via
`UiPalette.DrawTintedIcon`; **no pre-coloured variants** — colours live in `UiPalette`/`PoliSimTheme` and
must stay there. The inks are the v2.0 aged set in `PoliSimTheme.cs` and `POLISIM_V2_SCREEN_SPEC.md`
§A.3; re-read them from the code, never from a table in this document.

⚠ **THREE CATEGORIES ARE EXEMPT, and this is not a lapse in the convention.** **Country flags**,
**party emblems** (`emblem_party_*`) and **portraits** are authored in their own real colours — a flag is
not tintable, and a portrait is a painted bust (full-colour, opaque background, 512×640 @2× since the
batch of nine; the sixteen older ones are 256×256 squares). Any new art in those categories stays
full-colour; everything else stays white-on-alpha, **including `mark_party_*`**, which is tinted from
seed data at draw time (a rebrand is a data edit, never a redelivery). Getting this backwards in either
direction produces art that cannot be used.

**Icon authoring.** Renders small — a stat icon draws at **22px** on the contextual stat row: avoid thin
strokes and interior detail that will disappear. SVG source 24×24 geometry, `currentColor` fill, simple
primitives, mirroring the existing packs.

**Portrait authoring.** 1 source → 2 files (`.png` @2× + `.svg`); the roster frame is `ui_portrait_frame`
(RECT brass over the art) and the hero oval lives in `ui_portrait_frame_oval`'s alpha — **bake the art,
never the cutout**; no baked vignette. The register is Design's planar gouache (the PoC passed Elias's
register side-by-side 2026-08-26).

**9-slice frames (the IMGUI path).** Ornate frames, folders, plates and paper panels are drawn with
`GUIStyle.border`, a proven mechanism here. Deliver the **exact border inset in px per edge** with each
frame; corners must not stretch and the centre must tile or stretch cleanly. Author at **2× the largest
size it will render at** — every style rescales with `Screen.height`, so there is no single fixed render
size. **Non-rectangular edges must be baked into the alpha** — runtime masking in IMGUI is rectangles
only: a torn edge, a deckle, an oval portrait vignette, all baked, none masked. **No effect can be
applied at runtime** — shadows, grain, glow, blur and paper texture bake into the sprite; tint and
opacity are the only runtime adjustments.

**The Canvas path (narrative screens only).** Per-state single sprites (`normal` / `hover` / `pressed` /
`disabled` / `selected` — the scrollbar precedent; never a multi-cell strip, which `Resources.Load<Sprite>`
returns null for), 9-slice borders stated per sprite, transition timings per component; effects *can*
be runtime parameters here rather than baked. Everything in the tintable rule still applies. The
`canvas_*` namespace is retired — one `ui_*` namespace inside `Chrome/`.

### 5.3 Unity import settings — the defaults are actively harmful here

| Setting | Value | Why |
|---|---|---|
| Texture Type | `Default` | |
| `nPOTScale` | **None** (`0`) | **Matters most.** The default resamples a non-power-of-two sprite to the nearest power of two, silently altering the artwork |
| `alphaIsTransparency` | **On** (`1`) for white-on-alpha; `0` on the opaque portraits | Shape lives in alpha |
| sRGB | On (`1`) | |
| Filter Mode | `Bilinear` (`1`) | |
| Compression | **None** (`textureCompression: 0`) for white-on-alpha and tiling art; kept (`1`) for full-colour flags/portraits/emblems by ruling | Block compression mangles white-on-alpha at icon sizes; on full-colour art it showed no visible damage at display size |
| Mipmaps | **Off** (`enableMipMap: 0`) | UI sprites never minify — `ImporterSettingsCheck` errors on any |
| Wrap Mode | **Clamp** (`wrapU/V/W: 1`) | Correct for every sprite drawn once. **Tiling art needs `Repeat`** |
| `isReadable` | `1` for chrome (`UiPalette.GetTintedChrome` reads pixels back), `0` for icons and portraits | copied across the class boundary it cost an entire UI |

**COPY THE META FROM WITHIN THE SAME RENDERING CLASS, never from the nearest filename.** Three classes,
and the only thing that matters is what happens to the pixels between the file and the screen:

| Class | Members | Compression |
|---|---|---|
| **White-on-alpha, tinted** | `Chrome/`, `Icons/`, `Stats/`, **`mark_party_*`** | None |
| **Full-colour, untinted** | `Flags/`, `Portraits/`, **`emblem_party_*`** | kept, by ruling |
| **Tiling** | `Textures/menu_pattern_tile` | None, wrap Repeat |

`Emblems/` STRADDLES TWO CLASSES — `emblem_party_*` full-colour, `mark_party_*` white-on-alpha —
filename-adjacent and treatment-opposite; four marks once imported DXT5 from the wrong neighbour. **The
test is never "which file is next to it": does this art get tinted at draw time?** If yes, copy from
`Chrome/`; if no, from `Flags/` or `Portraits/`; if it tiles, the tile. A new portrait's meta is the
PoC's (`portrait_cabinet_defense_katarzyna_ekelund.png.meta`) with a fresh guid — the eight of
Progress5 differ from it in that line alone. `ImporterSettingsCheck` enumerates every PNG under
`Assets/Resources/Art/UI/`, classifies by treatment, and asserts against the **imported texture** — the
meta is the claim, the texture is the fact. The rulings behind these rows are `COMPLETED.md` §33.

### 5.4 Filename manifest and the naming rule

**Every filename derives from a real enum value in the code.** The game resolves art at runtime by
building the string from the enum, so a filename that does not match an enum resolves to null and draws
nothing.

| Pattern | Derived from | Example |
|---|---|---|
| `icon_area_<systemarea>` | `UiPalette.SystemArea`, lowercased | `icon_area_sovereignwealth` |
| `icon_stat_<statname>` | the displayable stat (`StatNodeId` via `GetIconName`), lowercased | `icon_stat_laborforceparticipationrate` |
| `icon_nav_<tab>` | `ConsolidatedTab`, lowercased — four of six; Budget and Politics reuse area icons by design | `icon_nav_policylaws` |
| `portrait_cabinet_<portfolio>_<name_slug>` | `CabinetPortfolio` + `IconLibrary.Slug(name)` | `portrait_cabinet_foreignaffairs_zofia_nakamura` |
| `portrait_fedchair_<name_slug>` | `Slug(name)` | `portrait_fedchair_priya_anand` |
| `emblem_party_<archetype>` | `PartyArchetype`, lowercased | `emblem_party_centristcoalition` |
| `mark_party_<country>_<party>` | the party seed's mark name | `mark_party_us_lib` |
| `flag_country_<countryid>` | `CountryId`, lowercased | `flag_country_poland` |
| `ui_<control>_<state>` | control + state, one sprite per state | `ui_btn_brass_canvas_hover` |

`Slug()` = lowercase, drop every non-letter, spaces → underscores. "Wei-Lin Tanaka" → `weilin_tanaka`;
"Amara Osei-Bonsu" → `amara_oseibonsu`. **The check for a portrait batch is programmatic:** derive the
expected stems from `CabinetSystem.CandidatePool` by this rule and `diff` them against the delivered
stems — 0 missing, 0 unexpected, spelling exact — because a rename that looks like an omission has
happened before (`icon_crime` → `icon_area_crimejustice`).

⚠ **Enumerate the DISPLAY enum, not the storage struct.** The macro pack derived its stat list from
`EconomyState`'s fields — the right instinct — and still missed `InterestRate`, which lives on
`CurrencyZone`. It was structurally invisible to that derivation while being a headline figure on two
screens.

⚠ **Everything must live under `Assets/Resources/`.** `IconLibrary` uses `Resources.Load`, not
`AssetDatabase`. The flags and emblems once sat outside `Resources/` for weeks, fully delivered and
**unreachable by the game the entire time**. An asset's status has two parts, **delivered** and
**reachable**, and only the first is visible from the inbox — which is why every import ends with a
load through the game's own accessor, never with a directory listing.

**1 source asset → 2 files** (`.png` + `.svg`), as in every previous pack. No zip at the project root
means every delivery is imported and archived — a zip appearing there is the signal something is
unfinished, and `DeliveredAssetCheck` enforces it.

## §E5 — two pipeline findings from the rasterization diff (2026-08-28)

Our half of the strip-cut diff closed (external rasterizer, the six canvas buttons 6/6 within
tolerance). The 90-pair sweep found two source/shipped mismatches — both asks, neither urgent:

1. **`ui_hatch_draft.png` — 60% pixel mismatch against its SVG source:** the source applies the
   rotation to the stripes, the shipped PNG applies it to the tiling. **Our presumption: the
   shipped PNG is canonical** — it is what two playtests reviewed on screen — so the ask is
   *confirm, and re-export the SVG source to match the shipped render*, not a choice of which to
   adopt. If the SVG was the intent, say so and we re-import instead.
2. **`ui_slider_track.png` — a 256-wide strip whose only source on file is a 24×24 pill:** point
   us at the real source for the strip, or confirm the pill is the intended source and the strip
   a derived export — in which case the derivation (stretch region, caps) goes in the pipeline
   note so the diff can model it.

Everything else in the 90-pair sweep sat inside budget; nine `Stats/` icons near the 2% line are
ours to inspect (A5), not Design's.
