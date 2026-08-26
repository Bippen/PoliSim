# Claude Design asset request — PoliSim

**Status — rewritten 2026-08-26 (the consolidation pass).** The answered arc — §1 through §1G,
§6, §7 and §7.1: the whole v2.0 design collaboration from the brief through the boards, the
passes, the marks and the law browser — is migrated to `COMPLETED.md` §24, with git history
holding every original section in full. **This document now carries only the live asks and the
standing conventions**, per its own charter ("appended to, then emptied on delivery" —
the register's lifecycle row). Sixth request in this project.

**Date:** 2026-08-26.

➡ **START AT [§8](#8-request--the-calendar-panel-board-2026-08-26)**, then
[§9](#9-request--statistics-graph-weight-and-treatment-2026-08-26). **The §5 verdict travels in
the same send package** (`MISSING_PREREQUISITES.md` §S).

## The live asks

- **§5 — cabinet portraits, the batch of nine: ✅ GATE CLEARED 2026-08-26.** The Editor register
  side-by-side ran and PASSED — the painted plate belongs beside the existing register, Design's
  own named gate for the batch. Design may proceed per the approved §5 PoC; the verdict is the
  deliverable and rides the send package.
- **§8 — the calendar panel board.** OPEN — written, not sent (the E2 convention: sending is
  Elias's).
- **§9 — statistics graph weight and treatment.** OPEN — written, not sent; **specification
  only, no sprite deliverables.**

## Standing gates (nothing pending on Design)

- **The R5 hex exchange — gated by name on item 10 (13 Sept 2026, Sweden votes):** the LP
  ink-safe `DisplayColor` and Sweden's party set travel when the party seeds land on main. Design
  is waiting on a calendar, not on us (`MISSING_PREREQUISITES.md` §E2).
- **The rasterization diff — ours to close:** gated on a raster path whose OUTPUT is comparable;
  the compare machinery is finished and waiting (`MISSING_PREREQUISITES.md` §E3).
- **D1's delivery:** once the §5 verdict is sent, the batch of nine is Design's; import checks
  stand ready for the 18 files (`MISSING_PREREQUISITES.md` §D1).

**Standing rule, kept from the migrated §6:** a count in prose is a cached value with no expiry
(working-discipline rule 12) — re-derive sprite inventories from the filesystem and screen
inventories from the enums before trusting any number here. This header itself has been the
failure it exists to catch, twice.

---

## 2. Explicitly OUT of scope — please do not produce these

- **Anything the seven data renderers already draw**: axes, gridlines, tick marks, plot lines,
  threshold lines, bars, area fills, legends, sparklines, map shapes, policy-web nodes and edges,
  hemicycle seats, pie wedges, compass dots. All procedural, per working-discipline item 10 —
  these render real tracked simulation data rather than a picture, and that is exactly what rule
  10 protects. **Frames, plates and paper AROUND them are in scope; the data marks inside are
  not.**
- **Any sprite already delivered.** Check `Assets/Resources/Art/UI/` before producing anything —
  re-derive the count from the filesystem (rule 12); every prose count this document has carried
  went stale within days.
- **Typefaces.** Already chosen, open-licensed and imported: TeX Gyre Pagella (display + body)
  and Courier Prime (document artifacts). Do not propose or supply fonts.
- **Cabinet portraits beyond §5's nine.** The nine are the live ask; §5's addendum names the
  possibility of a larger roster later — that would be a fresh request, not an extension of this
  one.
- **`menu_pattern_tile.png`** — delivered, imported and wired (2026-08-02,
  `DrawCountrySelector`). **Do not re-request it**; producing it again would duplicate art that
  exists.
- **Pre-coloured trend arrows or badges.** Colour is applied at runtime; see §3.
- **Real-world currency symbols, agency logos, or national statistics-office branding.** The
  game's institutions are fictional by standing rule 9, and this would also be someone else's
  trademark.

---

## 3. Format & technical spec

Unchanged from the five previous packs, plus the two v2.0 additions (9-slice frames, and the
Canvas path).

### 3.0 The eleven hues, as they stand today

These are the **current** saturated screen values, given so the aged/desaturated versions can be
derived from a known starting point rather than invented. `PoliSimTheme` holds the dark-surface
tuning; `UiPalette` holds the base. Both must move together.

| Area | Current (`PoliSimTheme`) | Used for |
|---|---|---|
| Neutral | `#8F9AAB` | — |
| Fiscal | `#4D8DF6` | tax & spending; **Budget tab** |
| Trade | `#1FB2A6` | tariffs, partners; **Sweden** |
| Political | `#E0B341` | approval, elections, Fed; **USA**; **Politics tab** |
| Welfare | `#D9569F` | welfare programs; **Germany** |
| Labor | `#EE7A3A` | labor market; **France**; Demographics tab |
| CrimeJustice | `#C8534A` | crime & justice; Decisions tab |
| Sectors | `#7A6BF0` | economic sectors; **Italy**; Policy/Laws tab |
| Infrastructure | `#4A93A8` | infrastructure |
| SovereignWealth | `#B08D4A` | SWF; **Poland** |
| Global | `#6BAEE0` | world map; Statistics tab |

Semantic colours are separate and also need period equivalents: **amber `#E0B341` =
draft-not-enacted** (reserved, behaviour 1), good `#4EC98A`, bad `#C8534A`, caution `#E0B341`.

### 3.1 Tintable art — the default, and still binding

- **PNG:** 256×256, 8-bit RGBA, transparent background, **authored pure white** with all shape
  information in the alpha channel. Tinted at draw time via `UiPalette.DrawTintedIcon`. **No
  pre-coloured variants** — colours live in `UiPalette`/`PoliSimTheme` and must stay there.

⚠ **TWO CATEGORIES ARE EXEMPT, and this is not a lapse in the convention.** **Country flags** and
**party emblems** are authored in their own real colours — a flag is not tintable, and the emblem
SVGs already carry `#E0B23C` and `#FFFFFF`. Any new art in those two categories stays
full-colour; everything else stays white-on-alpha. Getting this backwards in either direction
produces art that cannot be used: a tinted flag is wrong, and a pre-coloured area icon defeats
the eleven-hue system.

### 3.2 9-slice frames (the IMGUI path)

Ornate frames, folders, plates and paper panels are drawn with `GUIStyle.border`, which is a
working, proven mechanism in this project (the chrome pack established it).

- Deliver the **exact border inset in px per edge** with each frame. Corners must not stretch;
  the centre must tile or stretch cleanly.
- Author at **2× the largest size it will render at** — every style in this UI rescales with
  `Screen.height`, so there is no single fixed render size.
- **Non-rectangular edges must be baked into the alpha.** Runtime masking in IMGUI is rectangles
  only: a torn paper edge, a deckle, an oval portrait vignette, a vignetted corner — all baked,
  none masked.
- **No effect can be applied at runtime.** Shadows, grain, glow, blur and paper texture bake into
  the sprite. Tint and opacity are the only runtime adjustments.

### 3.3 The Canvas path (narrative screens only)

For Canvas screens: sprite sheets with 9-slice borders defined in the sheet metadata, and
component specs shaped as prefabs with their states (`normal` / `hover` / `pressed` / `disabled`
/ `selected`) and transition timings. Effects *can* be runtime parameters here rather than baked.

Everything else in §3.1 still applies.
- **Renders small.** A stat icon draws at **22px** on the contextual stat row. Avoid thin strokes
  and interior detail that will disappear.
- **SVG source:** 24×24 geometry, `currentColor` fill, simple primitives, mirroring the existing
  packs.

### Unity import settings — the defaults are actively harmful here

Stated because these were worked out during the chrome import after Unity's defaults silently
damaged sprites:

| Setting | Value | Why |
|---|---|---|
| Texture Type | `Default` | |
| `nPOTScale` | **None** (`0`) | **Matters most.** The default resamples a non-power-of-two sprite to the nearest power of two, silently altering the artwork |
| `alphaIsTransparency` | **On** (`1`) | Shape lives in alpha |
| sRGB | On (`1`) | |
| Filter Mode | `Bilinear` (`1`) | |
| Compression | **None** (`textureCompression: 0`) | Block compression mangles white-on-alpha at icon sizes |
| Mipmaps | **Off** (`enableMipMap: 0`) | UI sprites never minify |
| Wrap Mode | **Clamp** (`wrapU/V/W: 1`) | Correct for every sprite drawn once. **Tiling art needs `Repeat` — see §3.0a** |

The delivered `.meta` should match `Assets/Resources/Art/UI/Stats/icon_stat_gdp.png.meta` exactly
apart from its `guid`.

### ⚠ 3.0a — COPY THE META **FROM WITHIN THE SAME RENDERING CLASS**, never from the nearest filename

**"Copy that file and change only the guid" is reliable only inside one rendering class, and the
filenames actively work against getting that right.** This qualifier was added 2026-08-11 after
the rule, followed exactly as written, produced a defect for the third time.

There are three classes, and the only thing that matters is what happens to the pixels between
the file and the screen:

| Class | Members | Compression | Why |
|---|---|---|---|
| **White-on-alpha, tinted** | `Chrome/`, `Icons/`, `Stats/`, **`mark_party_*`** | **None (`0`)** | The alpha edge *is* the drawing, and it is re-tinted at draw time. Block compression quantises it into visible fringing at icon size. |
| **Full-colour, untinted** | `Flags/`, `Portraits/`, **`emblem_party_*`** | `1` — ruled acceptable (§3.0b) | §3.1's named exemption. Drawn as authored, so alpha-edge damage has no tint to amplify it. |
| **Tiling** | `Textures/menu_pattern_tile` | **None (`0`)** | Repeats across a surface, so block edges repeat too and read as a grid. |

⚠ **`Emblems/` STRADDLES TWO CLASSES, and that is where the last defect came from.**
`emblem_party_*` is full-colour and never tinted; `mark_party_*` is white-on-alpha and tinted at
draw time. They are **filename-adjacent and treatment-opposite**. Four `mark_party_*` metas were
copied from `emblem_party_*` — the nearest neighbour by name, the wrong one by treatment — and
all four imported as DXT5. `Chrome/` was the far neighbour by name and the correct one by class.

**So the test is never "which file is next to it".** It is: *does this art get tinted at draw
time?* If yes, copy from `Chrome/`. If no, copy from `Flags/`. If it tiles, see the row above.

**Two instances of this same rule, both previously recorded as one-off exceptions:**

- **Chrome needs `isReadable: 1`** (2026-08-03, after it cost an entire UI). The icon template
  carries `isReadable: 0`, correct for icons, which are tinted via `GUI.color` and never read
  back. `UiPalette.GetTintedChrome` instead calls `Texture2D.GetPixels`, which **throws** on a
  non-readable texture. Pass-1 metas were copied from the icon template exactly as instructed,
  and the first wired build rendered as an empty desk.
- **`menu_pattern_tile` needs Wrap Mode `Repeat`, not `Clamp`.** It is drawn with
  `DrawTextureWithTexCoords` across the whole menu. Clamp does not fail — it stretches the edge
  pixel across the screen, which reads as a design choice rather than a broken import.

Neither is an exception. Both are the same rule: **the template encodes its own class's
treatment, and copying it across a class boundary carries the wrong treatment with it.**

### 3.0b — the two settings rulings, 2026-08-11

**MIPMAPS — OFF, and this is an EXISTING rule now checked, not a new one.** The settings table
above has said *"Mipmaps **Off** (`enableMipMap: 0`) — UI sprites never minify"* since it was
written. 44 files across `Emblems/`, `Flags/`, `Icons/` and `Portraits/` carried them anyway. All
44 corrected (per-file before/after verification), and `ImporterSettingsCheck` promoted it from
warning to **error**.

⚠ Recorded here as pre-existing precisely so the check is not cited as the authority for it. **A
check must never be the source of a rule it enforces** — that is circular, and the failure it
creates is a rule nobody can argue with because nobody can find where it was decided.

**FULL-COLOUR COMPRESSION — ACCEPTABLE, ruled after a visual check.** The 26 `Flags/`,
`Portraits/` and `emblem_party_*` sprites import block-compressed. **Flags are the worst case for
block compression** — large flat colour fields meeting at sharp edges is exactly what DXT
quantises worst — and compared against an uncompressed source at display size they show no
visible damage. Portraits, continuous-tone with no hard colour boundaries, are covered *a
fortiori*. So compression stays for this class, and the warning was dropped rather than kept as a
passing note — a permanent 26-line amber is a thing people learn to skim.
`ImporterSettingsCheck` enumerates every `*.png` under `Assets/Resources/Art/UI/`, classifies
each by treatment rather than by folder, and asserts against the **imported texture**, not the
`.meta` text — the meta is the claim and the texture is the fact.

---

## 4. Filename manifest and the naming rule

**Every filename derives from a real enum value in the code.** This is the rule the whole request
format is built on, and it is why a name can never be invented: the game resolves art at runtime
by building the string from the enum, so a filename that does not match an enum resolves to null
and draws nothing.

| Pattern | Derived from | Example |
|---|---|---|
| `icon_area_<systemarea>` | `UiPalette.SystemArea`, lowercased | `icon_area_sovereignwealth` |
| `icon_stat_<statname>` | the displayable stat, lowercased | `icon_stat_laborforceparticipationrate` |
| `icon_nav_<tab>` | `ConsolidatedTab`, lowercased | `icon_nav_policylaws` |
| `portrait_cabinet_<portfolio>_<name_slug>` | `CabinetPortfolio` + `IconLibrary.Slug(name)` | `portrait_cabinet_interiorjustice_amara_oseibonsu` |
| `portrait_fedchair_<name_slug>` | `Slug(name)` | `portrait_fedchair_weilin_tanaka` |
| `emblem_party_<archetype>` | `PartyArchetype`, lowercased | `emblem_party_centristcoalition` |
| `mark_party_<country>_<party>` | the party seed's mark name | `mark_party_us_lib` |
| `flag_country_<countryid>` | `CountryId`, lowercased | `flag_country_poland` |
| `ui_<control>_<state>` | control + state | `ui_button_disabled` |

`Slug()` = lowercase, drop every non-letter, spaces → underscores. "Wei-Lin Tanaka" →
`weilin_tanaka`.

⚠ **Enumerate the DISPLAY enum, not the storage struct.** The macro pack derived its stat list
from `EconomyState`'s 29 fields — the right instinct — and still missed `InterestRate`, which
lives on `CurrencyZone` because a rate belongs to a currency zone rather than to one country. It
was structurally invisible to that derivation while being a headline figure on two screens.

⚠ **Everything must live under `Assets/Resources/`.** `IconLibrary` uses `Resources.Load`, not
`AssetDatabase` (Editor-only, breaks in a player build). This is not a filing preference: the
country flags and party emblems sat outside `Resources/` for weeks, fully delivered and imported,
and were **unreachable by the game the entire time**. An asset's status has two parts,
**delivered** and **reachable**, and only the first is visible from the inbox.

**1 source asset → 2 files** (`.png` + `.svg`), as in every previous pack.

---

## 5. REQUEST — cabinet portraits. ✅ GATE CLEARED 2026-08-26 — THE BATCH OF NINE IS THE LIVE ASK

> **`portrait_cabinet_defense_katarzyna_ekelund` (512×640 @2×) is on disk in `Portraits/`** (meta
> from the Portraits family, fresh GUID; SVG to the new `Portraits/Source/`). All three of
> Design's named gates are ANSWERED — this block is the deliverable back to Design:
>
> **1 — The pixel envelope (Design's missing fact):** all 16 existing portraits are **256×256
> SQUARE, transparent-background flat busts** — textureType Default (not Sprite), spriteMode 0,
> alphaIsTransparency 0, compression ON, maxTextureSize 2048, drawn via
> `GUI.DrawTexture(ScaleMode.ScaleAndCrop)` into a **74:92 (≈0.804) roster rect** at ~3.2
> line-heights. Consequences: 512×640 (0.80) matches the roster crop to **0.5%** — better than
> the existing squares, which lose ~20% of their width to the same crop; the resolution and size
> clear the importer as-is (2048 cap). **The envelope is ACCEPTED — no obstacle.**
>
> **2 — Oval vignette ownership: FRAME-OWNED, confirmed by what shipped.** At roster size the
> treatment is `ui_portrait_frame` — RECT brass over every portrait, art cropped underneath; the
> 16 bake NO vignette of any kind. The only oval vignette in the project lives in
> **`ui_portrait_frame_oval`'s alpha** (Design's own manifest row: "oval vignette in alpha"),
> delivered and unwired — the Canvas hero path. The PoC's baked "vignette" is an opaque painted
> background glow — rect-safe, verified by pixel inspection. **Bake the art, never the cutout.**
>
> **3 — The register side-by-side: ✅ PASSED 2026-08-26** (Elias's live Editor session): the
> painted plate belongs beside the existing register — Design's own named gate for the batch.
> **The batch of nine is unblocked; this verdict travels in the send package.**
>
> ⚠ **Addendum, 2026-08-25 — flagged, not requested:** the roster this batch of nine covers
> (3 portfolios × 3 philosophies) may grow as decision-density work proceeds — more portfolios
> means more candidates, and therefore a batch larger than nine. Nothing to act on yet; named now
> so a future ask for "twelve" or "fifteen" doesn't read as scope creep against this one.

### The original request follows (2026-08-17)

**This section was "known future need — NOT yet requestable" until Round 4 batch R4-4 authored
the ministers.** The blocker was real and is now gone: the nine names below are the shipped
values in `CabinetSystem.CandidatePool`, signed by Elias (R4-4 ruling R1, checked against real
officeholders of the six countries on 2026-08-17 — the collision search is recorded in
`POLISIM_R4_4_PREREPORT.md` §4), and every filename below is derived from them via the standing
rule (`portrait_cabinet_<portfolio>_<name_slug>`, `Slug()` per §4). All nine are ORIGINAL
FICTIONAL characters (working-discipline rule 9) — none may resemble any real person.

**The ask: 9 portraits, same envelope as the 16 existing** (rect roster framing + oval-vignette
hero treatment per the conventions above; 1 source asset → 2 files, `.png` + `.svg`; destination
`Assets/Resources/Art/UI/Portraits/`; the game renders a procedural placeholder until each file
lands, so partial delivery is safe).

| filename | who they are (for the brush, not the label) |
|---|---|
| `portrait_cabinet_defense_katarzyna_ekelund` | Defense, Reformist. Wants procurement audited in the open — believes opaque contracting is where readiness actually dies. Sharp, forensic, unimpressed by braid. |
| `portrait_cabinet_defense_rafael_iwasaki` | Defense, Pragmatic. Capability-planning technocrat; buys what the threat assessment says, not what the parade needs. |
| `portrait_cabinet_defense_gunnar_petrakis` | Defense, Traditionalist. Deterrence through visible strength; distrusts any reform that reads as weakness abroad. Weathered, formal. |
| `portrait_cabinet_foreignaffairs_camille_adeyemi` | Foreign Affairs, Reformist. Institution-builder; thinks the multilateral table is where middle powers actually win. |
| `portrait_cabinet_foreignaffairs_zofia_nakamura` | Foreign Affairs, Pragmatic. Interests-first dealmaker; judges every communiqué by what it moves, not what it says. |
| `portrait_cabinet_foreignaffairs_aleksander_whitfield` | Foreign Affairs, Traditionalist. Alliances and protocol; believes predictability is a foreign policy, and a good one. |
| `portrait_cabinet_education_yuki_dahlberg` | Education, Reformist. Curriculum modernizer; argues the system trains students for an economy that no longer exists. |
| `portrait_cabinet_education_nadia_fitzgerald` | Education, Pragmatic. Evidence-based incrementalist; pilots before mandates, data before both. |
| `portrait_cabinet_education_tobias_marchetti` | Education, Traditionalist. Standards and fundamentals; wary of every reform that trades rigor for relevance. |

Coverage after delivery: 18 ministers + 7 Fed chairs = 25 portraits. Import per §3's treatment
rules (`ImporterSettingsCheck` will enforce); `DeliveredAssetCheck` gains 18 entries (9 × 2
files).

Tracked in `MISSING_PREREQUISITES.md` §D1; the request was **SENT 2026-08-17 (Elias)**.

---

## 8. REQUEST — the calendar panel board (2026-08-26)

**Status: OPEN — written, not sent (the E2 convention: sending is Elias's).**

**Why this, why now.** The left-column calendar panel shipped 2026-08-24 code-derived, without a
board — the only v2.0-era surface besides the law browser that skipped Design, and playtest 2's
verdict is that it shows. The law browser's §7 taught the shape: state what exists precisely
enough that Design iterates rather than reinvents, and carry the data contract so the board is
drawn against what the model can actually mark.

### What exists (iterate, don't reinvent)

A weekday-aligned month grid — past days suffixed **" X"** in muted ink (a literal text suffix,
the utilitarian treatment §8.3 below asks about), today carried on a rounded card with the
Political accent wash, day numbers centered per cell; up to **four 5px dots** per day under the
number, each tinted by its marker's own SystemArea (fiscal hue for a release day, political for a
division) — the cap is a hard `min(count, 4)`. Below the grid, **"This Month"**: one ledger row
per marker, measured date column ("12/31"-worst-case, the 2560 wrap lesson) then label. Above,
the country-name-plus-year header preserved from the old dashboard. The month page flips
**instantly** on the boundary — grid, weekday alignment and ledger regenerate with zero staleness
(captured: `capfold_80a`/`80b`). Locale-honest throughout: the week starts on the player
culture's own `FirstDayOfWeek` and the month name is the culture's (the captures show MÅN…SÖN and
JANUARI). Chrome is entirely procedural (`RoundedCard`/`Rule`/`Pill`) — `ui_calendar_pad` is the
only calendar sprite that exists anywhere, so the board MAY spec sprites; nothing constrains it
to the procedural look.

### The data contract — FIXED, travels with this request

The board must not invent markers for events that aren't scheduled. The governing question, per
source: does a PENDING instance carry a computable date, and does a RESOLVED one retain one?

| Source | Future | Past | In the panel? |
|---|---|---|---|
| Fiscal year start | exact fixed (month, day), annually | exact | ✅ ONE merged marker — the same real date also opens the budget process and triggers the credit-rating review |
| Publication release days (6 published stats) | exact date arithmetic, no RNG | exact | ✅ every day of the shown month checked against every stat |
| Pending bill countdowns (all 8 types) | exact: today + DaysRemaining | n/a (resolves into a division) | ✅ one marker per pending bill |
| Elections | exact (turn → epoch date) | no persisted log exists (the open ElectionRecord gap) | ✅ future only |
| Resolved divisions (24 retained) | n/a | exact stored dates | ✅ past only |
| Fired economic events | **unknowable** (probability roll) | exact date within the 6-turn fade window | ✅ past only, bounded |
| Cabinet decisions | **probability-only, no trace** | **no date stamp ever written** | ❌ excluded, both directions |
| Foreign-policy meetings | **probability-only, no trace** | same | ❌ excluded, both directions |

### Open design questions — the board's actual subject

1. **The X-mark.** The literal " X" suffix is utilitarian. Is there a period-true desk idiom — a
   crossed-off almanac feel — that says "spent day" without a text suffix?
2. **A marked day at grid size vs its detail in the ledger.** Dots say *that* and *whose area*;
   the ledger says *what*. Is that split right, and does four-dots-max read as "busy" or as
   noise?
3. **The month flip.** Currently instant. Worth a page-turn moment, or is instant the honest
   desk?
4. **Density.** The real worst case is captured, not hypothesized: USA October 1 lands the merged
   fiscal marker plus three annual publications on ONE day — four dots at the hard cap, one long
   merged ledger row (`capfold_83a`). What should a saturated day look like?
5. **One instrument or three stacked parts?** Header, grid, ledger — do they read as a single
   almanac page or as three components sharing a column?

### Constraints (real, from the shipped code)

- IMGUI; ledger grammar (`LedgerRow.Cell` measured columns); the **one-scroll rule** — the panel
  lives inside the left column's single scroll view.
- The left column is **43.2% of the window** (0.45 of the 0.96-margin area): **≈ 691 px at
  1600×900, ≈ 1106 px at 2560×1440**. The pinned calendar pad and speed strip beneath are
  separate surfaces, out of this board's scope.
- Day-cell height is measured via `CalcHeight` against real content (a 2,004-violation guard
  lesson); the ledger date column is measured against the widest date, not a constant (the 2560
  wrap lesson). Whatever the board draws, these stay measurements.
- **No new probability data** — the contract table above is the complete marker universe.

### ATTACHMENTS (§8) — verified on disk this pass, `..\PoliSim-captures\`

- `couple2s1600_02_statistics.png` (1600×900) / `couple2s2560_02_statistics.png` (2560×1440) —
  the panel today at both sizes: JANUARI 2029, X-marked past, today highlighted, dots on 5/12/30,
  the five-row ledger.
- `capfold_80a_calendar_month_end.png` / `capfold_80b_calendar_month_flip.png` (~1600×929) — the
  month-boundary pair: January 31, then February 1 regenerated.
- `capfold_83a_budget_pause_decisions.png` (~1600×929) — **the density case**: OKTOBER, four dots
  under day 1 at the cap, the merged "10/1 Fiscal year starts - budget process opens; credit
  rating reviewed" ledger row.

Attach these when this is actually sent — they're the evidence, not a description of it.

---

## 9. REQUEST — statistics graph weight and treatment (2026-08-26)

**Status: OPEN — written, not sent (E2). ⚠ SPECIFICATION ONLY — NO SPRITE DELIVERABLES EXPECTED.
This is a visual-treatment ruling the way NO NEW SPRITES was a manifest rule: whatever is
specified, we implement as pixel rules in our own rasterizer; no art files are being requested.**

**The finding.** Playtest 2: the statistics graphs' series lines read as hairlines, at 2560
especially. The current weight is deliberately left untouched as the evidence
(`couple2s2560_02a_statistics_domestic_deep` — four stacked graphs, the thinness plainly
visible).

### The real raster machinery the ruling lands in (this shapes the answer)

- **Full graphs draw into a FIXED 300×90 texture** and display via `StretchToFill` into the
  layout rect: **≈ 810 px wide at 1600 (≈ 2.7× horizontal stretch), ≈ 1250 px at 2560 (≈ 4.2×)**;
  display height is `clamp(7.5% of screen height, 50..90)` → **67 px at 1600, 90 px at 2560 —
  vertically 1:1 at 2560**, which is why a 2-buffer-px line is 2 device px tall there: the
  hairline.
- **Sparklines build native-resolution buffers** (width/height from the rect) through the same
  Bresenham. A thickness rule must speak to both contexts — buffer px per context, or one
  resolution-relative rule we translate.
- The 300×90 buffer size is our constant, not a law — the ruling MAY say "raise the buffer to
  display resolution" and we implement that too.
- The maths is extracted and regression-tested (`BuildSparklinePixels`, pure, hammered at **336
  width × height × series-shape combinations**) — pixel-rule changes land under existing tests.

### The ask — three roles, one ruling

Line weight for the **primary history series** (currently 2 buffer px, solid), stated so all
three roles stay differentiated, not one number:

1. **History** — solid, the recorded data.
2. **Projection** — the one-turn estimate: currently lighter alpha AND dashed (every 3rd
   Bresenham step skipped), drawn on the most-recent page only. Must keep reading as "estimate,
   not committed" at whatever new weight.
3. **Threshold/reference** — NAIRU, the "comfortable" debt line: warm amber (`Caution`) with a
   right-aligned label riding the line. Must keep reading as "reference marker, not data."

### While ruling — do the existing behaviors' expressions hold at the new weight?

These exist and work; the question is whether their current visual expression should move with
the weight or stay:
- the **direction-aware green/red** delta convention (header % per graph);
- **release-point markers** and the **PRELIMINARY badge + lag dating** on published series;
- **preliminary-vs-revised as frame style** — dashed 1px frame while provisional, solid once
  revised (a second, separate channel from the projection dashing — both visible in
  `couple2s1600_02a_statistics_domestic_deep`).

### ATTACHMENTS (§9) — verified on disk this pass, `..\PoliSim-captures\`

- `couple2s2560_02a_statistics_domestic_deep.png` (2560×1440) — four stacked graphs: the hairline
  verdict's own evidence, plus the dashed next-year estimate, the amber "comfortable" threshold
  with its riding label, and the green/red deltas.
- `couple2s1600_02a_statistics_domestic_deep.png` (1600×900) — the As-published GDP graph:
  PRELIMINARY badge, release-point markers, dashed revision frame, the 1yr/5yr/All pager.
- `item5sweden_07d_politics_federalreserve.png` (1600×900) — the full-width neutral interest-rate
  graph, a third weight context (no green/red judgment by design).

Attach these when this is actually sent — they're the evidence, not a description of it.
