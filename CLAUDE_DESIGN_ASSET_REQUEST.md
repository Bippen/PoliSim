# Claude Design asset request — PoliSim

**Status — NO LIVE ASK (2026-08-28, night). The ninth request — UI v3.1, "one frame, denser,
instruments" — was answered in full the evening it was sent** (boards 1n-r2, 1m-r2 and 2a, the D4 density
token table and the D6 contrast pass, all on the live screens file) **and built the same night as UI v3.1
Phase B** (`COMPLETED.md` §45; the boards and tables as read are the spec's §A.18). Its text stays below as
§1 with its six annexes, marked answered — the annexes are the measurements Design built against, and
three of them carry a correction or a re-measure filed back for Design's next look, none an ask: **Annex C's
paper-box padding was quoted from the box rect, not from the paper** (the sprite's own drop shadow sat inside
the rect — measured 14/14/10/26 px; it now hangs outside, so the padding tokens mean what the table took
them to mean); **Annex F re-measured after D6** (Caution 4.09 / 3.90 against the pass's 4.5; TextPrimary on
brass 4.03, not the table's 5.5); **§E5's hatch pair after the third cut** (7.42 % — rasterizer edge coverage,
not a cut error; the bar question is Elias's). The eighth request (the v3.0 boards) was answered the day it
was sent and migrated to `COMPLETED.md` §41. What else is here: **§0** the delivered set as it stands, **§4**
what is costed but not yet requestable (redrawn rail glyphs joined it — refused by 1n-r2 as a costed
follow-up), **§5** the standing conventions. **Date:** 2026-08-28.

**Standing rule: a count in prose is a cached value with no expiry** (working-discipline rule 12).
Before trusting any number in this document, re-derive it: sprites with
`find Assets/Resources/Art/UI -name '*.png'`, chrome coverage with `ChromeV2CoverageCheck`, stat icons
with `StatIconCoverageCheck`, portraits with `PortraitCoverageCheck`, deliveries with
`DeliveredAssetCheck`, importer state with `ImporterSettingsCheck`, screen inventories from the enums
(`StatNodeId`, `UiPalette.SystemArea`, `ConsolidatedTab`, `CabinetPortfolio` ×
`CabinetSystem.CandidatePool`, `CountryId`, `PartyArchetype`). This document has been the failure that
rule exists to catch at least four times.

---

## 1. The ninth request — UI v3.1: one frame, denser, instruments (2026-08-28) — ANSWERED IN FULL THE SAME EVENING; BUILT AS v3.1 PHASE B THE SAME NIGHT

*The request as Elias delivered it with the v3.1 Phase A kickoff (archived verbatim at `../PoliSim-captures/inbox/DESIGN_REQUEST_V3_1.md`); only the heading levels are changed to sit inside this document. The six annexes it names follow, engineering-measured (R-E3). **Answered 2026-08-28:** 1n-r2, 1m-r2, 2a, D4, D6 on the live screens file — read into `POLISIM_V2_SCREEN_SPEC.md` §A.18 verbatim and built (`COMPLETED.md` §45). Kept here, not migrated, because the annexes are the measured base the answer was computed against and the next request starts from their corrections (the status line above).*


**From Elias's first live sitting on the v3.0 build (two findings-screenshots attached as Annex D).
Six asks, one theme: the Desk's frame won — now everything gets it, tighter, with the data drawn
instead of listed.** Boards at 1280×720 first, as before. All standing conventions (§5) hold: PNG
canonical, deviations declared not silent, no new hues without a costed case, derived data only —
we will refuse rows the model doesn't hold, as with 1m, and you've seen that refusal is respected
on both sides.

### D1 — One frame everywhere (ruling, for your awareness; no board needed)

The OPEN state (chrome column + tab tongues, the "half screen") retires. Every screen now lives in
the Desk's frame: the rail, one full-bleed sheet. Budget's form becomes the game's form. The
chrome column's duties are covered (audit in Annex A): oversight lives on the Desk, interrupts on
the rail's banner, time and status in the rail. You are not asked to draw this — you are told so
that 1n-r2 and every future board assumes it.

### D2 — The rail, revision 2 (board 1n-r2)

- **A home cell, obvious.** Topmost, visually first-class — the one cell that reads "back to the
  Desk" without being learned. Engineering ships a structural interim (existing glyph) the same
  day; your board replaces its face. The calendar-chip-as-home stays as the second, learned way.
- **Icon legibility at the real cells.** Measured cells: 39 / 46 / 55 / 64 px at 1280 / 1600 /
  1920 / 2560 (Annex B has each icon at each size). Elias's finding: not readable, not intuitive
  enough. Your call to make, stated as a question: captions under the icons (mono, tiny) · larger
  cells · redrawn glyphs at small-size-first · some mix. The rail may grow wider if legibility
  earns it; say the number.

### D3 — The Desk, revision 2 (board 1m-r2: density)

Elias's finding, verbatim in substance: too much dead paper, text too small because spacing eats
the room. Revise 1m with: tighter margins and inter-plate gaps, larger base type, the bottom
strip integrated into the sheet rather than floating, and — explicitly — **the Year-0 / empty
states designed** (the approval ledger with no last period, the effects card at all-zero, the
sparklines with no history). Part of the perceived emptiness is empty states nothing was ever
drawn for; density alone won't fix what absence causes. Annex C carries the measured current
paddings so your cuts are numbers against numbers.

### D4 — Global density tokens

The same tightening, game-wide: screen padding, panel padding, plate gaps, row pitches where not
already ruled (the law browser's one-line pitch stands). Deliver as a revised token table against
Annex C's current values — one number per token, we apply mechanically. Type may grow where the
reclaimed room allows; say where.

### D5 — Statistics as instruments (new board: "2a — Statistics drawn")

Elias's ask: the fitting form for each dataset — bar charts, pie/share forms where parts truly
sum to a whole (sector shares of GDP), graphs where time matters — and declutter. Annex E is the
content census of today's Statistics screens: every dataset, its shape (share / series / level /
distribution), and its current form. Constraints, standing: derived data only; the categorical
cap (eight series) holds; honesty classes stay visible (PRELIMINARY, revision frames, the dashed
next-year convention); no new hues — the palette you set is the palette. Sub-tab structure is
yours to keep or collapse; screen granularity is ours and stays.

### D6 — Contrast pass

Elias's finding: readability suffers at small sizes. Annex F measures today's ink pairs
(foreground/background, px size, where used). Revise the faint-ink assignments where they fall
below comfortable reading at their real sizes — new *values* for existing tokens, not new tokens.

**Annexes (engineering-supplied, in this doc):** A — chrome-column duty audit (what the column
carried, where each duty now lives). B — the rail's icons at real cell sizes. C — measured
paddings and dead-space figures per screen at 1280 and 2560. D — Elias's two sitting screenshots.
E — Statistics content census. F — ink-pair contrast table. Captures: the current `v3c_*` /
`v3desk_*` sets.

### Annex A — the chrome column's duty audit (R-E1's gate; from the code at HEAD `999e47e`, every method the OPEN branch alone called)

The OPEN frame drew, and only the OPEN frame drew: `DrawTopBanner` (the event banner, the game-over banner), `DrawCalendarPanel` (the country/year header, the 1k month page, the dated ledger), `DrawPolicyControls` → `DrawPolicyPreview` (the horizon buttons, the eight estimates, the margin and methodology lines), `DrawCalendarAndSpeedControls` (the calendar pad, the RUNNING plate / HELD banner, the OPEN fold toggle, the speed row, Saves), and `DrawConsolidatedTabs` / `DrawActiveFolderTongue` (the six tongues with their labels, the pulled-forward active folder). The census (Annex A of the eighth request) numbered their text C1–C28 and S1. Each duty, and where it lives with one frame:

| # | duty the OPEN chrome uniquely carried (census rows) | home with ONE FRAME | on every screen? | verdict |
|---|---|---|---|---|
| 1 | the live event — name, description, its three effects (C1–C3, `DrawTopBanner`) | the Desk's event card (C1/C2 as captions, C3 as three bars) and the map's event dots | no — the Desk, one rail cell away (the home cell) | covered (oversight → the Desk) |
| 2 | game over and its reason (C4–C5, `DrawTopBanner`) | the Desk's §A.11 stamp; **and the rail's folded banner on every screen** — behaviour #8 (a player can always see why the clock is stopped) applied to the game-over hold, added in this pass (R-E1's own "must satisfy #8 on every screen") | yes, from this pass | covered (interrupts → the banner) |
| 3 | the country's name and the turn count (C6, the calendar panel header) | the Desk's masthead (`{COUNTRY} · YEAR {N}`); the country's identity on every screen through the home cell's flag (R-E2's glyph) | the name yes (the flag), the year the Desk's | covered (oversight → the Desk; identity → the rail) |
| 4 | the month page and the dated ledger (C7–C13) | the Desk's calendar column (the same 1k sheet) | no — the Desk; the rail's chip says the day | covered (oversight → the Desk) |
| 5 | the policy preview — horizon, eight estimates, margin, methodology (C14–C22) | the Desk's effects card (the same cached `PreviewTurn`, the same horizon) | no — the Desk; the one draft the preview still reads (the interest-rate change) is set on the Federal Reserve document, read on the Desk | covered (the preview → the effects card), one interaction cost recorded |
| 6 | the calendar pad — month, day, year (C23–C25) | the rail's chip (month, day); the year on the Desk's month header and masthead | month and day yes; the year the Desk's | covered (time → the rail) |
| 7 | the RUNNING plate (C26) | the rail's lamp (green, no glow) | yes | covered (status → the rail) |
| 8 | the HELD banner — the reasons, screens named (C27) | the rail's folded banner above the sheet, on every screen (Budget's own hold omitted on Budget, which states it) | yes | covered (interrupts → the banner) |
| 9 | the speed controls — Pause · 1× · 2× · 3× (C28) | the Desk's masthead cluster (all four); **the rail's bottom cell — the fold toggle's cell, freed by ONE FRAME — becomes the player's PAUSE / RUN chip (R-E1a, the audit's one addition)**; disabled while an interrupt or game over holds the clock (B5) | pause/run yes; the speed choice the Desk's | covered (speed → the rail and the Desk) |
| 10 | Saves (C28's fifth button) | the Desk's masthead | no — the Desk, one cell away; not time-critical | covered (oversight → the Desk) |
| 11 | navigation — the six tongues and their labels (S1) | the rail's six icons (labels retired with the tongues — Design's D2 answers legibility) | yes | covered (navigation → the rail) |
| 12 | the active document shown (the pulled-forward folder) | the rail's active cell — the 12 % wash and the spine (board 1n) | yes | covered |
| 13 | the fold toggle itself (OPEN ↔ FOLDED) | retires — there is one frame; its cell is row 9's | — | retired by ruling |

**Finding: no orphan.** Every duty has a named home; two homes are added in this pass rather than assumed — the game-over line on the folded banner (row 2, behaviour #8) and the rail's pause/run chip (row 9, R-E1a). Two interaction costs are recorded, not hidden: the interest-rate draft's estimate is read on the Desk (row 5) and Saves is the Desk's (row 10). **The retirement proceeds.**

### Annex B — the rail's icons at the real cells (rendered on film, cropped from the `v31_*` matrix at each size; the crops are in the paste's `annex_b/`)

The cell is derived, not chosen: the icons' 24-unit grid plus 10 units of air each side, the unit being the tongue icon's pixel size / 24 — `cell = round(icon × 44 / 24)`; the icon is `round(tab type × 1.15)`, the tab type `clamp(0.024 × h, 18, 30)`.

| window | client height | tab type | icon px | cell px | the HOME flag (v3.1) | crop |
|---|---|---|---|---|---|---|
| 1280×720 | 699 | 18 | 21 | 39 | 24×16 | `annex_b_rail_1280.png` |
| 1600×900 | 929 | 22 | 25 | 46 | 29×19 | `annex_b_rail_1600.png` |
| 1920×1080 | 1059 | 25 | 29 | 53 — measured 55 on film (the sheet's own rounding) | 33×22 | `annex_b_rail_1920.png` |
| 2560×1440 | 1419 | 30 | 35 | 64 | 40×27 | `annex_b_rail_2560.png` |

The seven cells, top to bottom, and the sprite each draws (white-on-alpha, tinted; the flag full-colour):

| cell | sprite | ink when active / inactive | what it is meant to read as |
|---|---|---|---|
| HOME (v3.1, first, a rule beneath) | `flag_country_<countryid>` (full colour) | brass wash + spine on the Desk / plain | back to the Desk; the country |
| 1 | `icon_nav_statistics` | Global ink `#5C87A8` / tint `#4E7291` | Statistics |
| 2 | `icon_nav_decisions` | CrimeJustice `#9C4238` / `#8E4A40` | Decisions |
| 3 | `icon_nav_demographics` | Labor `#B5622F` / `#A2653E` | Demographics |
| 4 | `icon_area_fiscal` | Fiscal `#35619E` / `#3D6494` | Budget (the area icon by design) |
| 5 | `icon_nav_policylaws` | Sectors `#62579F` / `#5B5187` | Policy / Laws |
| 6 | `icon_area_political` | Political `#A8842E` / `#96762A` | Politics (the area icon by design) |
| chip | `ui_calendar_pad` at cell width | — | the day (and the second way home) |
| lamp | procedural dot, 8 px at the 720 type ratio | Good green / DraftOnDesk amber with the glow | RUNNING / HELD |
| PAUSE · RUN (v3.1) | the desk chip (StockOff plate, brass when paused) | — | the player's own hold |

Your call, as D2 states it: captions under the icons (mono, tiny) · larger cells · redrawn glyphs at small-size-first · some mix. The rail may grow wider if legibility earns it — say the number; the derivation above is what the code will follow.

### Annex C — the measured paddings, gaps and pitches (from the code's own tokens at HEAD; the px columns are the values at the two film sizes, 1280×699 and 2560×1419 client), and the dead-space share per screen (from the film)

**The frame.** `s` = the widget scale `clamp(h / 1080, 0.6, 1.5)` → **0.65 at 1280, 1.31 at 2560**; every style's type comes from `Screen.height` (the type table below).

| token (where it lives) | rule | px at 1280 | px at 2560 | note |
|---|---|---|---|---|
| screen margin (`ScreenMarginFraction`) | 2 % of the window, each side | 26 × 14 | 51 × 28 | the desk ground showing around the frame |
| rail → sheet gap (`ColumnSpacingFraction`) | 2 % of the area width | 25 | 50 | |
| section spacing (`SectionSpacingFraction`) | 3 % of the area height | 20 | 41 | vertical breathing between stacked panels (OPEN-era; still used inside sheets) |
| the sheet (`_boxStyle` = the skin's box) | padding + margin, fixed px | 28 per nesting level | 28 | **not scaled** — measured (the LAWSPROBE, 1600): a box inside a box's padding costs 28 px per level |
| area card (`StyleBoxAsPaper`) | padding 14 / 14 / 12 / 14; 9-slice border 22 / 22 / 22 / 28 | as stated, fixed | as stated | the paper sprite's own inset |
| dossier card (`_dossierCardStyle`) | padding 18 + 8 (spine) / 18 / 32 / 20 | fixed | fixed | Decisions and the bill panels |
| hold banner (`_holdBannerStyle`) | padding 10 / 6; lamp gap 6 | fixed | fixed | |
| stat tile (`PoliSimWidgets.StatTile`) | padX 17·s, padY 16·s, label block 20·s, value gap 9·s, delta 18·s (+8·s before a bar) | 11 / 10 / 13 / 6 / 12 | 22 / 21 / 26 / 12 / 24 | height from `StatTileHeight`: ≈ 98·s |
| tile grid gap (`DrawHeadlineStatTiles`) | 8·s | 5 | 11 | three columns |
| ledger row (`LedgerRow.Height`) | max(2·line, 12·s + line) + 6·s, line = max(lineHeight, size + 4) | ≈ 44 at 16 px type | ≈ 78 at 28 px | the two-line lane; the one-line pitch is line + 6·s (≈ 24 / 40) |
| law-browser row pitch (R-C1) | one-line + the 10 px gap | ≈ 37 | ≈ 55 | **stands, by the request's own rule** |
| line graph (`GraphRenderer`) | display height clamp(0.075·h, 50, 90); buffer 300×90 | 52 | 90 | the width stretches to the column |
| calendar sheet (1k) | cell gap 3·s; the dot row 10; dot 5; strike inset 2; section rule 1.5 with 6 above and below | 2 / 10 / 5 | 4 / 10 / 5 | the dot row and dot are fixed px by ruling |
| the Desk (board 1m, scaled by the sheet's inner ratio to 1118×660) | masthead 26; column gaps 16; plate gaps 16 (12 above the event card); the strip's rule 10 above, 9 below; chip pad 7 × 5; chip gap 8 | the board's own at 1280 (ux = uy ≈ 1.0) | ×2.05 / ×2.03 | **every Desk value is the board's — D3's revision replaces them as a set** |
| the rail | cell = icon × 44 / 24 (the icons' grid + 10 units of air each side); cell gap 4 units | cell 39, icon 21 | cell 64, icon 35 | 1600: 46 / 25 · 1920: 55 / 30 |
| radii (`PoliSimTheme`) | panel 19 · card 16 · inset 14 · chip 12 · control 11 | fixed | fixed | |
| bars (`PoliSimTheme`) | sm 6 · md 8 · lg 12 | fixed | fixed | |

**Type today (px, from `RescaleStylesToScreen`'s clamps on `Screen.height`, then the derived styles):**

| style | rule | 1280 (h 699) | 2560 (h 1419) | used for |
|---|---|---|---|---|
| header | clamp(0.032·h, 22, 42) | 22 | 42 | section headers, the calendar month |
| body / label | clamp(0.022·h, 16, 28) | 16 | 28 | ledger names, figures, paragraphs, map names on documents |
| tab | clamp(0.024·h, 18, 30) | 18 | 30 | (the tongues, retired) — the rail's icon size derives from it (×1.15) |
| banner | clamp(0.028·h, 20, 36) | 20 | 36 | the HELD / GAME OVER banner |
| mono meta (Courier) | max(9, body × 9/12.5) | 12 | 20 | dates, captions on documents |
| calendar weekday / day numeral | body × 0.8 / × 0.95 | 13 / 15 | 22 / 27 | the 1k sheet |
| stat tile label / hero / small | 10·s / 42·s / 13·s | 7 / 27 / 8 | 13 / 55 / 17 | the ten plates |
| the Desk's captions (`DeskPx`) | board px × h / 720, floor 8 | 8–9 (the 6.5 and 7 px captions floor at 8) | 16–19 | every Desk caption |
| the Desk's body / numerals | 11–13 / 13.5–30 × h / 720 | 11–13 / 13–29 | 22–26 / 27–59 | the ledger names, the chips' numerals, the hero |

**Dead-space share per screen** (the film, `v31_*` at 1280 and 2560, the code as pushed). *Method, so the numbers can be reproduced or disputed:* the content column (right of the rail and its gap, top margin to bottom margin — the held banner inside it counts as content) is cut into blocks of 16 px at 1280 and 32 px at 2560 (the same fraction of the frame); the paper reference is the column's modal colour (16-level bins, sampled every 4 px — the plain paper dominates every screen); a block is EMPTY when no sampled pixel in it differs from that reference by more than 28 on any channel (the paper sprite's grain stays under it); dead space = empty blocks / all blocks. `deadspace.ps1` in the pass's scratch; its output is the table below, verbatim. **How to read the rows:** `01c_desk` (43.5 % / 45.5 %) is an **empty state, not spacing** — turn 0: the ledger has no period, the effects card is all-zero, the sparklines have no history (the held Desk at turn 3, `01d_desk_held`, reads 35.8 % / 37.7 % with the same layout); the Policy Web (55.5 % / 63.7 %), the Compass (55.6 % / 62.3 %) and Parliament (50.4 % / 57 %) are **an instrument's own negative space** — a ring, a square plot, a fixed-size hemicycle — not padding; Demographics (58.6 % / 66.7 %: a fixed 120 px pie beside short ledgers) and Statistics › Domestic (44.2 % / 46 %: ten plates with their gaps, the graphs below the fold) are the **spacing-and-form** cases D3/D4/D5 are for; Decisions (17.6 %) and Budget (28.8 %) are the densest. The 2560 figures run higher than the 1280 ones on every screen — the frame grows faster than its content, which is the sitting's finding in numbers.

**The dead-space table (measured on the `v31_*` film, 2026-08-28):**

| screen | window | dead space (share of the sheet) | empty blocks | paper reference |
|---|---|---|---|---|
| 01c_desk | 1280 | 43,5 % | 1285 of 2952 blocks of 16 px | paper #E8E8D8 |
| 01d_desk_held | 1280 | 35,8 % | 1057 of 2952 blocks of 16 px | paper #E8E8D8 |
| 02a_statistics_domestic | 1280 | 44,2 % | 1306 of 2952 blocks of 16 px | paper #E8E8C8 |
| 02b_statistics_international | 1280 | 33,8 % | 997 of 2952 blocks of 16 px | paper #F8E8D8 |
| 03_decisions | 1280 | 17,6 % | 521 of 2952 blocks of 16 px | paper #E8E8D8 |
| 04_demographics | 1280 | 58,6 % | 1729 of 2952 blocks of 16 px | paper #E8E8D8 |
| 05b_budget_spending | 1280 | 28,8 % | 849 of 2952 blocks of 16 px | paper #E8E8D8 |
| 06a_policylaws_labormarket | 1280 | 36 % | 1063 of 2952 blocks of 16 px | paper #E8E8D8 |
| 06d_policylaws_policyweb | 1280 | 55,5 % | 1639 of 2952 blocks of 16 px | paper #F8E8D8 |
| 06f_policylaws_laws | 1280 | 35,1 % | 1036 of 2952 blocks of 16 px | paper #E8E8D8 |
| 07a_politics_parliament | 1280 | 50,4 % | 1489 of 2952 blocks of 16 px | paper #E8E8D8 |
| 07b_politics_compass | 1280 | 55,6 % | 1642 of 2952 blocks of 16 px | paper #E8E8D8 |
| 07c_politics_cabinet | 1280 | 31,8 % | 939 of 2952 blocks of 16 px | paper #E8E8D8 |
| 07d_politics_federalreserve | 1280 | 42,5 % | 1254 of 2952 blocks of 16 px | paper #E8E8D8 |
| 01c_desk | 2560 | 45,5 % | 1396 of 3066 blocks of 32 px | paper #E8E8D8 |
| 01d_desk_held | 2560 | 37,7 % | 1157 of 3066 blocks of 32 px | paper #E8E8D8 |
| 02a_statistics_domestic | 2560 | 46 % | 1411 of 3066 blocks of 32 px | paper #E8E8C8 |
| 02b_statistics_international | 2560 | 45,6 % | 1397 of 3066 blocks of 32 px | paper #F8E8D8 |
| 03_decisions | 2560 | 41,1 % | 1259 of 3066 blocks of 32 px | paper #E8D8C8 |
| 04_demographics | 2560 | 66,7 % | 2045 of 3066 blocks of 32 px | paper #E8E8D8 |
| 05b_budget_spending | 2560 | 31,2 % | 956 of 3066 blocks of 32 px | paper #E8E8D8 |
| 06a_policylaws_labormarket | 2560 | 34,4 % | 1055 of 3066 blocks of 32 px | paper #E8E8D8 |
| 06d_policylaws_policyweb | 2560 | 63,7 % | 1952 of 3066 blocks of 32 px | paper #F8E8D8 |
| 06f_policylaws_laws | 2560 | 36,1 % | 1107 of 3066 blocks of 32 px | paper #E8E8D8 |
| 07a_politics_parliament | 2560 | 57 % | 1747 of 3066 blocks of 32 px | paper #E8E8D8 |
| 07b_politics_compass | 2560 | 62,3 % | 1909 of 3066 blocks of 32 px | paper #E8E8D8 |
| 07c_politics_cabinet | 2560 | 27,5 % | 843 of 3066 blocks of 32 px | paper #E8E8D8 |
| 07d_politics_federalreserve | 2560 | 32,9 % | 1010 of 3066 blocks of 32 px | paper #E8E8D8 |

**Re-measured after D4 and the five boards were built (the `v31b_*` film, 2026-08-28, the same method and script — the re-measure is the fact, as D4 asks):** 1280 → `01c_desk` 43.9 % (was 43.5; the Year-0 empty states are now drawn furniture, and the board's own expectation of ≈ 30 % is not what this method reads on the built Desk) · `01d_desk_held` 39.7 (35.8) · `02a_statistics_domestic` **42.1 (44.2)** · `02b_statistics_international` 39.3 (33.8) · `03_decisions` **27.2 (17.6)** · `04_demographics` **67.6 (58.6)** · `05b_budget_spending` 28.6 (28.8) · `06a_policylaws_labormarket` 37.7 (36.0) · `06d_policylaws_policyweb` 57.7 (55.5) · `06f_policylaws_laws` 42.5 (35.1) · `07a_politics_parliament` 52.3 (50.4) · `07b_politics_compass` 57.9 (55.6) · `07c_politics_cabinet` 38.0 (31.8) · `07d_politics_federalreserve` 44.2 (42.5); 2560 → the Desk 44.5 (45.5) · held 43.5 (37.7) · Domestic 42.9 (46.0) · International 50.8 (45.6) · Decisions 57.7 (41.1) · Demographics 68.7 (66.7) · Budget 35.8 (31.2) · Labor 38.3 (34.4) · Policy Web 66.9 (63.7) · Laws 41.7 (36.1) · Parliament 59.7 (57.0) · Compass 69.7 (62.3) · Cabinet 30.4 (27.5) · Federal Reserve 40.4 (32.9). **What the numbers say:** the spacing cut COMPACTS content, so on a screen whose content is a fixed quantity — a short list, a fixed-size instrument, a ledger that ends above the fold — the share of empty paper RISES (Decisions +9.6 pts, Demographics +9.0, Laws +7.4 at 1280; the 2560 rows more so); the one screen re-composed for the reclaim, Statistics › Domestic (board 2a), is the one that fell (−2.1 / −3.1). D4's expectation ("Demographics and Domestic drop ~10–14 pts from spacing alone") held for neither: the reclaim has nowhere to go until the content grows into it — 6 % more body type (16 → 17) does not — or the screen is re-composed the way 2a re-composed Domestic. The paper-box correction (the shadow outside the rect, Annex C's base) is in these numbers too. Filed for Design's next look; the candidates are Demographics, Decisions and the short Politics screens, in that order.

### Annex D — Elias's two sitting screenshots (2026-08-28, the first live sitting on the v3.0 build)

**The images are Elias's and travel with the paste** — they were not on disk, in the inbox or among the Design uploads when this request was assembled, so this annex carries the findings in his words and the two frames he judged, and the screenshots go in beside it when he pastes (the E2 convention: sending is his). Place them under `send/design_request_2026-08-28d/annex_d/` as `sitting_1_desk_density.png` and `sitting_2_rail_icons.png`.

- **Screenshot 1 — the Desk (Screen 0, 1280×720 or the sitting's window):** *"too much dead paper, text too small because spacing eats the room."* The frame he judged is the `v3desk_*` / `v31_*` `01c_desk` / `01d_desk_held` family; the measured paddings and the dead-space shares are Annex C; the Year-0 empty states are flagged there as "empty-state, not spacing" — D3's board designs them.
- **Screenshot 2 — a document with the rail (the icons at the real cells):** *"not readable, not intuitive enough"* — the six tinted icons at 39 / 46 / 55 / 64 px cells; and *"readability suffers at small sizes"* — the faint inks at their real sizes. Annex B renders each icon at each cell; Annex F measures the ink pairs; the structural HOME cell (the flag, first position, a rule beneath) shipped in v3.1 Phase A as the interim your 1n-r2 re-skins.

### Annex E — the Statistics content census: every dataset on the two Statistics screens, its shape and its current form (from the code at HEAD, `DrawDomesticStatisticsContent` / `DrawInternationalStatisticsContent` and the methods they call)

Shapes: **level** (one number now) · **share** (a part of a stated whole) · **series** (a value over time) · **distribution** (parts of one whole, summing to it) · **relation** (things and their links). Honesty class as Annex B of the eighth request used it (LIVE / PUBLISHED / DERIVED / LEDGER). "Current form" is what draws today.

| # | dataset | shape | honesty | current form | notes for the board |
|---|---|---|---|---|---|
| E1 | GDP, unemployment, inflation, approval rating, currency strength (independent-currency countries only), poverty rate, government debt, debt-to-GDP, credit rating, budget balance | level ×10 | LIVE | ten `StatTile` plates in a 3-column grid (label, hero numeral, unit; GDP's turn delta and the rating's outlook as the only pills) | the same ten are the Desk's chip strip (S6); a tile is a plate, not an instrument — the board may keep, shrink or replace them |
| E2 | GDP per capita | level (currency per person, no denominator) | DERIVED | read-only ledger row, no gauge | |
| E3 | tax burden · government spending · deficit/surplus · primary deficit/surplus | share of GDP ×4 | DERIVED (from the last fiscal report; "advance a year" before the first) | ledger rows with a gauge and the trailing unit `of GDP` | four shares of one whole — but not parts of each other; a bar ledger, not a pie |
| E4 | sector shares of GDP — eight sectors | **distribution** (parts of one whole, summing to it) | LIVE | eight ledger rows, each `of GDP` | **the one true pie/share form on the screen** (D5's example); eight = the categorical cap exactly |
| E5 | GDP over time (+ the dashed next-year estimate) | series | LIVE history (quarterly) + the preview's projection | `GraphRenderer` line graph: title with signed change, 300×90 buffer, axis min/mid/max, page row | R-G1..R-G5 weights |
| E6 | unemployment over time (+ NAIRU threshold, + the dashed estimate) | series + a reference level | LIVE + the country's NAIRU | line graph with the amber threshold and its riding label | |
| E7 | inflation over time | series | LIVE | line graph | |
| E8 | approval rating over time (+ the dashed estimate) | series | LIVE | line graph | |
| E9 | poverty rate over time | series | LIVE | line graph | |
| E10 | debt-to-GDP over time (+ the "comfortable" threshold) | series + a reference level | LIVE + the country's comfortable ratio | line graph with the amber threshold | |
| E11 | youth unemployment | share (of the youth labour force) | LIVE | ledger row with gauge | |
| E12 | life expectancy | level (years) | LIVE | ledger row, no gauge (§A.9b) | |
| E13 | income inequality (Gini) | level on a 0–100 scale | LIVE | ledger row with gauge | |
| E14 | real wages · house prices | index (100 = start of term) ×2 | LIVE | ledger rows, no gauge (unbounded index) | the honest comparison is the country's own past — a series form would fit if a history were kept (`RealWageIndex`/`HousePriceIndex` are in `StatHistory`) |
| E15 | productivity | level ($ per hour, PPP) | LIVE | ledger row, no gauge | history kept (`Productivity`) |
| E16 | housing overburden (EU five only) · homeownership | share ×2 (of households) | LIVE | ledger rows with gauge; the USA's overburden row is ABSENT by ruling, not zero | |
| E17 | GDP as published · unemployment as published · inflation as published | series, PUBLISHED (lagged, revisable; monthly cadence) | PUBLISHED | `GraphRenderer.DrawPublished`: the date axis, release markers, the PRELIMINARY/FINAL badge, the dashed revision frame, the 1yr/5yr/All pager, `latest: {value} ({lag})` | the honesty channels (B6) must survive any redesign |
| E18 | poverty rate as published | level, PUBLISHED (annual cadence) | PUBLISHED | a bulletin (`PublishedFigure`): badge · figure · the reference period and release date | annual = a bulletin, not a graph (eleven points beside a daily series read as broken) |
| E19 | the sentence "What the public sees: lagged, and revised as later estimates arrive." | — (a (b) restatement of the badge and the frame) | — | one label | waits for the board to return as an instrument or not at all |
| E20 | the world map — six countries as GDP-sized nodes at fixed illustrative positions, trade-volume lines, fading event dots | relation | LIVE (`Country.State.GDP`, `TradePartners`, the event markers) | `MapRenderer` plate, names on §A.9a's ladder (R-SP5); hover readout; click pins a detail panel below | not geography — no polygons, no coastlines |
| E21 | the pinned country/event detail under the map | level (the clicked country's headline readings) / the event's text | LIVE | a text panel | |
| E22 | tariff pass-through to prices | level (pp of inflation, last period) | LEDGER (the closing fiscal period's applied term) | one label | |
| E23 | trade balance over time (+ the current level in the title) | series | LIVE history | line graph (`_tradeBalanceGraph`) | |
| E24 | "Recent activity" — the last eleven turn-log lines | series of events (text) | LIVE (`_turnLog`) | eleven labels | a (b)-class list; the Desk's calendar ledger and event card carry the dated facts |

**Counts:** 24 dataset rows; shapes — level 9 · share 6 · distribution 1 · series 10 (E5–E10, E17 ×3, E23) · relation 1; forms today — tiles 1 grid · ledger rows 13 · line graphs 10 · bulletin 1 · map 1 · text 3. **Where the form and the shape disagree (the board's subject):** E4 is a distribution drawn as eight rows; E3 four shares of GDP drawn as gauges that read as unrelated; E14/E15 indices and a level with histories kept but drawn as single numbers; E1's ten levels drawn as ten plates. Standing constraints: derived data only; the eight-series cap; the honesty channels visible; no new hues.

### Annex F — the ink pairs as they render today: foreground, background, px at the two film sizes, where used, and the measured WCAG contrast ratio (computed from the hex values in `PoliSimTheme.cs` / `UiPalette.cs`; 4.5 : 1 is the conventional body-text floor, 3 : 1 the large-text floor — stated as reference lines, not as this project's rule)

| ink (token) | on | ratio | px at 1280 / 2560 | where it is read at that size |
|---|---|---|---|---|
| TextPrimary `#2B2620` | Card `#F0E7D8` | **12.2** | 16 / 28 body; 22 / 42 headers; 11–13 / 22–26 Desk labels | body, ledger names, the Desk's row labels |
| TextPrimary | Tile `#EDE2CB` | **11.7** | 27 / 55 hero; 13 / 27 chip numerals | tile and chip numerals |
| TextSecondary `#5D564A` | Card | **5.9** | 8–9 / 16–19 Desk captions; 12 / 20 mono meta; 13 / 22 weekday | every Desk caption, dates, the bulletin caption |
| TextSecondary | Tile | **5.6** | 8 / 16 chip labels | the strip's chip labels |
| TextMuted `#665E4F` (D6 2026-08-28; was `#7A7263` at 3.9) | Card | **5.22** | 8 / 16 methodology caption; 13 / 22 "+N more" | the Desk's C20 line, the ledger's overflow row |
| TextMuted `#665E4F` (was 3.7) | Tile | **4.98** | 7 / 13 | **the stat tile's label** (`FontLabel` 10 × scale → 11 × scale under D4) |
| Neutral `#5F6672` (D6; was `#6D7480` at 3.8) | Card | **4.72** | 10–12 / 20–24 | zero deltas, neutral figures (text uses; the Neutral AREA ink stays `#6D7480`) |
| Good `#2E7048` (D6; was `#3E8A5F` at 3.4) | Card | **4.86** | 10–12 / 20–24 | positive deltas (numerals, mono) |
| Bad `#9C4238` | Card | **5.3** | 10–12 / 20–24 | negative deltas — untouched by D6 |
| Caution `#8F6900` (D6, text uses; was `#BE8A00` at 2.5 — the FILL amber keeps `#BE8A00` as `Draft`) | Card | **4.09** | 9 / 19 BREAKING chip; 10 / 16 threshold labels | the event chip, NAIRU / "comfortable" labels riding the graph line — **D6 aimed at ≥ 4.5; measured 4.09** |
| Caution `#8F6900` (was 2.4) | Tile | **3.90** | 9 / 19 | the BREAKING chip on the event card's plate — **measured 3.90** |
| TextOnDesk `#F0E7D8` | Desk `#241B10` | **13.8** | 20 / 36 | the HELD / GAME OVER banner |
| DraftOnDesk `#D4A72C` | Desk | 7.6 | (the lamp, 8 px dot) | not text |
| TextPrimary | StockOff `#B9A886` | **6.4** | 8 / 16 | the chips' captions (PAUSE · 1× · SAVES · 1D … and the rail's PAUSE/RUN) |
| InkOnStock `#45392A` | StockOff | 4.8 | 18 / 30 | (the tongues, retired) |
| TextPrimary `#2B2620` (D6's assignment flip; was light `#F4ECDC` at 3.2) | Brass `#9C8148` | **4.03** | 8 / 16 | the SELECTED chip's caption (1× · FULL TURN · RUN) — **D6's table said 5.5; measured 4.03** (brass unchanged) |
| TextOnPlate `#3D372E` | Tile | 9.2 | 16 / 28 | plate text |
| Hairline `#B7A98C` | Card | 1.9 | (rules) | not text |
| HairlineStrong `#8A7A5C` | Card | 3.4 | (rules, the compass grid) | not text |
| area ink Fiscal `#35619E` | Card | 5.1 | 16 / 28 ledger labels; 22 / 42 headers | calendar rows, section headers |
| Trade `#23867B` | Card | 3.6 | same | |
| Political `#8A6B21` (D6; was `#A8842E` at 2.9) | Card | **4.07** | same | calendar rows (division/election markers), the Politics header |
| Welfare `#A84E7B` | Card | 4.2 | same | |
| Labor `#B5622F` | Card | 3.6 | same | |
| CrimeJustice `#9C4238` | Card | 5.3 | same | |
| Sectors `#62579F` | Card | 5.1 | same | |
| Infrastructure `#3E7480` | Card | 4.3 | same | |
| SovereignWealth `#85643A` | Card | 4.4 | same | |
| Global `#47708E` (D6; was `#5C87A8` at 3.1) | Card | **4.31** | 16 / 28; 22 / 42 | the Statistics headers, "Domestic" / "International", the calendar's event markers |
| tab tint Fiscal `#3D6494` | the rail's paper | 5.0 | (icons 21 / 35) | inactive rail icons — not text |
| tab tint Political `#96762A` | | 3.5 | | |
| tab tint Labor `#A2653E` | | 3.8 | | |
| tab tint CrimeJustice `#8E4A40` | | 5.4 | | |
| tab tint Sectors `#5B5187` | | 5.8 | | |
| tab tint Global `#4E7291` | | 4.1 | | |

**The pairs below 4.5 : 1 that carry TEXT at 16 px or less at 1280, as first measured** (the sitting's "readability suffers at small sizes"): TextMuted on Tile at 7 px (3.7) — the stat tile label; TextMuted on Card at 8 px (3.9) — the Desk's methodology caption; Caution on Card/Tile at 9–10 px (2.5 / 2.4) — the BREAKING chip and the threshold labels; Good on Card at 10–12 px (3.4) — the positive deltas; the light-on-brass selected chip at 8 px (3.2); Global at 16 px (3.1) and Political at 16 px (2.9) as ledger inks. Rows are (fg, bg, px, where) as R-E3 asks; the judgment was D6's.

**Re-measured 2026-08-28 after D6 was applied (`PoliSimTheme.cs` / `UiPalette.cs`; the same sRGB-luminance arithmetic on the hex values; my measurement is the fact, as D6 asks):** TextMuted 5.22 / 4.98 (target ≥ 5.0: met on Card, 0.02 short on Tile), Good 4.86 (≥ 4.8 ✓), Neutral 4.72 (≥ 4.5 ✓), Global 4.31 and Political 4.07 (≥ 4.0 ✓ both). **Two rows stay under 4.5 with text on them:** Caution at 9–10 px — 4.09 on Card, 3.90 on Tile (D6 aimed at ≥ 4.5; `#8F6900` at L −0.07 does not reach it — a further −0.03 or so would); and the selected chip's caption at 8 px — TextPrimary on brass measures **4.03**, not the 5.5 D6's table gives (up from 3.17; the flip is kept — it is the better of the two assignments and the brass is ruled unchanged). Neither is an ask; both are filed here for Design's next look, with the numbers.

---

## Annex G — the Policy Web, measured (2026-08-28 night; R-W3: the annexes are measurements, not prose)

The base for board 2b. Everything below is measured — the occupancy by a row-run scan for the
web's flat plate colour on the films (the same script both eras), the counts by
`PolicyWebCensus` (batch, from the same public API the screen draws from), the type from the
code's own formulas at the four capture heights.

**G.1 — Occupancy, before → after the interim full-sheet build (R-W1).** The web's plate on
the `06d` frame at rest:

| size | before: plate px / % of window / % of sheet | after: plate px / % of window / % of sheet |
|---|---|---|
| 1280×720 | 1120×328 · 41.1 % · 43.6 % | 1120×448 · **56.1 %** · **59.6 %** |
| 1600×900 | 1421×483 · 46.2 % · 48.9 % | 1423×591 · **59.8 %** · **63.3 %** |
| 1920×1080 | 1722×569 · 48.2 % · 51.0 % | 1722×733 · **62.1 %** · **65.6 %** |
| 2560×1440 | 2331×1162 · 74.6 % · 78.7 % | 2331×1162 · 74.6 % · 78.7 % (the old ceiling already filled this frame) |

Before the build, the visible plate at the three smaller sizes was SHORTER than the diagram's
own half-screen floor — the ring was clipped by the fold at rest. After it, the plate is the
scroll viewport exactly, at every size.

**G.2 — The ring's arithmetic (why the wide plate is not a wide ring).** `PolicyWebRenderer.
Draw`: radius = min(width, height)/2 − labelMargin − MaxNodeDiameter/2, where labelMargin =
the widest wedge header as rendered ("Sovereign Wealth" / "Crime & Justice") + 2·5 px pad.
Height-bound at every capture size — a circle cannot use the plate's horizontal room, and the
header margin is reserved on ALL sides though the wide headers only need it left and right.
That geometry is the composition question the board owns.

**G.3 — Labels (the ladder at the real sizes).** Always-on: the 9 wedge headers at
max(9, body−1) px = **16 / 21 / 25 / 29** at 1280/1600/1920/2560. On hover/click only: ONE
node label at the body size = **17 / 22 / 26 / 30** (both from `RescaleStylesToScreen`'s
clamp(0.024·h, 17, 30); the node label clamps into the plate rect rather than reserving
radius). At rest no node is labelled and no edge draws — the resting frame is dots, headers
and dividers.

**G.4 — The census (`PolicyWebCensus`, batch-measured).** 73 nodes = 55 policy + 18 stat;
wedges: Labor 6 · Crime & Justice 6 · Fiscal 28 · Welfare 6 · Sectors 5 · Sovereign Wealth 2 ·
Trade 1 · Political 1 · Stats 18. Policy→stat edges: 121 full set = **73 derived + 48
declared**; per country: USA 120 (72 derived — the policy rate's issuance edge does not exist
under `BaseDebtInterestRateOverride`), the other five 121. Stat→stat (the causal graph): 7,
all derived. One node draws no edge, stated by name: Tariffs (Tax Line) — an enum-member-only
node. Edge ink today: DERIVED solid at full ink, DECLARED dashed at reduced ink; effect colour
from the target stat's own good/bad framing; thickness spans 1.1–3.4 px on RelativeStrength
(uniform 1.0 where no cross-comparable ratio exists).

**G.5 — The clicked-node readout, verbatim contents (the pane board 2b may re-compose; the
CONTENTS are the model's, R-W2).** Policy node: the name in its area ink · the description ·
"Current effects:" (computed lines from the live dials) · "Moves:" — one line per edge,
"<stat> ▲/▼ — ledger: <term>" or "— declared". Stat node: the name · "Affected by (levers):"
(same line form) · "In the books (stat → stat):" — "moved by <stat> ▲/▼ — ledger: <term>" /
"feeds <stat> …" · the 50-year neutral-ink history graph where one of the 13 tracked stats,
else the sentence "No trend history tracked for this stat yet."

**G.6 — The films (the deterministic family — byte-stable run-to-run outside three named
clock frames).** Rest: `pweb_1280_06d_policylaws_policyweb.png`, `pweb_2560_06d_…`. Clicked:
`pweb_1280_06k_…_node_policy(.png/_rows)` (Income Tax pinned), `pweb_1280_06l_…_node_stat`
(Approval pinned), and the 2560 twins — all under `..\PoliSim-captures\`.

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
- **The rail's six navigation glyphs redrawn small-size-first** — refused by board 1n-r2 (2026-08-28) as a
  costed follow-up: the captions under the cells answered the sitting's legibility finding; a redraw of
  the delivered `icon_nav_*` / `icon_area_*` set at 22 / 26 / 31 / 36 px is a fresh ask if the captioned
  rail is still not enough at the next sitting. Same envelope as the delivered set (24-unit grid, SVG).

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
   **Answered 2026-08-28 (the live screens file, "§E5 answered"): presumption confirmed, the source
   re-exported** with explicit 45° stripes — stated as a 16 px horizontal period and a 6 px
   horizontal duty (perpendicular width 4.243) — and the verification left to our rasterizer, with
   the offer: *"if it still mismatches, the residual is duty or phase — say which and I re-cut once."*
   **Imported and diffed the same day (resvg, `stripcut_b1_20260828_150949.log`): still outside
   budget — structure 33.4 % against 1 %, edge 0.40, down from 48.5 %.** The residual, measured on
   the shipped 32×32 PNG rather than guessed: its stripes run along `x + y` with a **16 px period**
   (the alpha profile along `x + y` repeats every 16), centred within half a pixel of the multiples
   of 16 — **the phase is fine** — and the ink is ≈8 px wide along x (5.7 perpendicular; coverage
   50.4 %). The re-export's five lines sit at `x + y = −32, 0, 32, 64, 96` — **a 32 px period, twice
   the PNG's** — at 6 / 4.243. So the residual is the period first and the duty second: the stated
   intent (16 px) was right, the file is off by a factor of two. **The one re-cut asked:** lines at
   `x + y = 16k` for every k that touches the 32×32 tile (k = −1…4), perpendicular stroke ≈ 5.7
   (≈ 8 px along x), phase as it is. `StripCutDiffCheck` keeps the pair deferred by name with this
   measurement as the pointer (R-D3); the deferral lifts the day the re-cut sits in budget.
   **Re-cut #3 landed 2026-08-28** (the screens file's `assets/polisim_ui_v2/svg/ui_hatch_draft.svg`:
   nine `<rect>`s in a `rotate(45 16 16)` group, 5.657 wide on an 11.314 pitch — 16 px along x, 8 px
   duty, centred on x + y = 16k; cut to the measurement above). **Imported and diffed the same day
   (`stripcut_e5close_20260828_191435.log`): structure 7.42 % against 1.00 %, edge 0.02, mismatch
   7.81 % — down from 33.4 %; period, phase and duty now agree.** The residual, classified pixel by
   pixel: 64 of the 76 mismatched px straddle the check's alpha-128 ink threshold (the shipped PNG's
   edge pixels sit at alpha 160, resvg's at 96–152 — the two rasterizers cover a 45° edge on a 32 px
   tile differently), 12 are solid-vs-void (1.17 % of the canvas). Not a cut error, and not Design's
   to cut a fourth time on our say-so: a bar question, put to Elias in the v3.1 Phase B report as a
   RULING NEEDED (name the pair "diagonal-tile, viewed not counted" — the three text stamps' treatment
   — with the measurement on record; or a fourth cut at ≈ 8.1 px duty; or a classifier refinement,
   which still reads 1.17 %). The pair stays in `StripCutDiffCheck.DeferredPairs` by name with this
   measurement as its pointer until ruled. **No further ask to Design under this item.**
2. **`ui_slider_track.png` — a 256-wide strip whose only source on file is a 24×24 pill:** point
   us at the real source for the strip, or confirm the pill is the intended source and the strip
   a derived export — in which case the derivation (stretch region, caps) goes in the pipeline
   note so the diff can model it.
   **Answered 2026-08-28 — CLOSED:** the strip is Design's own authored raster, regenerated with
   the v2 chrome pack (plain surface, 9-slice 10/10/4/12, ticks tiled by code), no SVG source
   exists, and the pill was the old pack's leftover under a colliding name. Done as asked: the pill
   removed from `Source/`, the strip listed source-less in the check's model
   (`StripCutDiffCheck.SourcelessByDesign`, printed by name on every run; a source re-appearing
   under the name is a FAIL). No SVG authored from the strip is requested.

Everything else in the 90-pair sweep sat inside budget; nine `Stats/` icons near the 2% line are
ours to inspect (A5), not Design's.
