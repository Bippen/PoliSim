# Claude Design asset request — PoliSim

**Status: v2.0 VISUAL REDIRECTION — OPEN, and far larger than the four packs before it.**
**Date:** 2026-08-03.
**Supersedes:** `CLAUDE_DESIGN_ASSET_REQUEST_5E.md`, `_UI_CHROME.md`, `_UI_CHROME_ADDENDUM.md` and
`_MACRO.md` — all four fully delivered, imported and verified in production. Their contents are recorded
in `COMPLETED.md` §8 and the originals remain in git history. **This is the single standing asset request
document**; new requests append here rather than starting a new file.

Fifth request in this project. **The technical conventions in §3–§4 are unchanged and still binding**;
§1 below is the new brief.

---

## 1. THE v2.0 BRIEF — total visual redirection

### FIRST — research Suzerain yourself

**Before designing anything, look at Suzerain (Torpor Games) directly.** Find and study screenshots of its
interface rather than working from the description below. **The description is one reading of it, and
yours may well be better** — you will have looked at the source, and everything after this section is
second-hand by construction.

Specific screens worth finding, each chosen because it maps onto something PoliSim actually has:

| Look at | Because PoliSim needs |
|---|---|
| Cabinet meeting screens | the Cabinet roster and the decision modals |
| Political / party overview | the Parliament screen and its hemicycle |
| Economy and budget panels | the Budget screen — the densest in the game |
| The national map | the World Map on Statistics → International |
| Character portraits | 16 existing portraits, and how they are framed |
| Constitution and law screens | Policy/Laws, and the bill cards |
| The desk, folder and document framing generally | the whole chrome language |

**Study the idiom, not the assets.** What is wanted is the visual language — how paper, frames, furniture
and type are used to make a government feel physical. Reproducing Suzerain's own artwork, layouts or trade
dress is not, and would collide with the same standing rule that keeps every institution in this game
fictional (§2).

⚠ **Then tell us where Suzerain's approach would NOT work here, and why. This is not a courtesy
question.** Suzerain is a narrative game with numbers in it; **PoliSim is a numbers game that needs
narrative weight**, and that difference is where a faithful copy would fail. Concretely: the Budget screen
shows dozens of numeric line items at once, each with a slider, a standing-versus-draft pair, and a live
legislative-vote estimate beside it — on one screen, with no room bought back anywhere.

Wherever the ornamentation would cost more space than it earns, **we would rather adapt the style than
copy it into a worse fit**, and after looking at both you are better placed to see that than we are. Say
so in the delivery. A section listing what you deliberately did not carry across is more useful to us than
a set of frames that assume the fit is total.

### What is being asked for

*Our reading of the idiom — offered after yours rather than instead of it.*

PoliSim's UI is a dark-mode data dashboard. It becomes a **1950s-republic aesthetic built as physical
furniture**, in the visual idiom of Suzerain: desk surfaces, paper documents, folders, ornate frames,
painted portraits, textured backgrounds, and full-screen focus for consequential moments.

**This is a redirection, not a restyle.** Nothing in the current look is a constraint on the new one.

**Typography is already solved and needs nothing from Design.** The game ships TeX Gyre Pagella
(a metric-compatible Palatino clone, GUST FL) for display and body, and Courier Prime (SIL OFL) reserved
strictly for document artifacts. Both are open-licence and already imported. Do not propose faces.

### The one architectural fact that shapes every asset

The rebuild is a **hybrid at SCREEN granularity, never element granularity**:

| | Renders in | Gets |
|---|---|---|
| Narrative / consequential screens | **Canvas (UGUI)** | Transitions, TextMeshPro, masks, effects |
| Data-dense screens | **IMGUI, restyled** | 9-slice frames, textures, real type |

**A screen is either one or the other. Never both with interleaved layering.** This is not a stylistic
preference — it was measured. IMGUI composites as one flat rectangle that **cannot be masked, animated, or
partially occluded by Canvas elements**.

⚠ **TRANSITIONS RUN FROM THE IMGUI SIDE.** A render-order spike (2026-08-03) established that
ScreenSpaceCamera Canvas renders *below* IMGUI **and so does ScreenSpaceOverlay Canvas** — there is no
Canvas mode that draws above OnGUI. So a Canvas overlay **cannot** fade in over a data screen. The
sequence is: an IMGUI full-screen scrim fades over everything → the IMGUI layer is suppressed → the Canvas
screen takes over and plays its own entrance. **Every consequential moment is introduced this way**, which
means the *hand-off* itself is a design surface: what the scrim looks like, and how the document arrives
after it.

Practical consequence for art: **a document sliding halfway over a data panel is impossible.** A
full-screen takeover is not.

### The eleven-hue requirement — a FLOOR, with evidence

The game carries eleven system-area identity hues (Fiscal, Trade, Political, Welfare, Labor,
CrimeJustice, Sectors, Infrastructure, SovereignWealth, Global, Neutral — hex values in §1.6). They
propagate to tabs, cards, spines, tiles, countries, cabinet portfolios and icons.

**Elias's decision (2026-08-03): keep all eleven, aged and desaturated into a period palette. No
non-colour carrier.** The brief asks for **eleven distinguishable hues rendered as aged ink on faded
stock — printed colour on period paper, not saturated screen colour.**

⚠ **Eleven is a floor, not an opening position, and here is the evidence rather than the assertion.**
During this survey the four party emblems were wired into the hemicycle legend, drawn *instead of* the
legend's colour swatch. It looked better and it broke something real: **the swatch colour is what keys
each legend row to its own arc of seats in the chart above**, and the emblem palette has no relationship
to the chart's hues. The swatch had to be restored and the emblem placed beside it.

The generalisation: **colour is load-bearing wherever it also keys a data visualisation** — the hemicycle,
pie charts, the political compass, categorical breakdowns. A seal, emblem or typographic mark cannot
substitute there, because *the mark is not what the chart is drawn in*. Reducing the hue count does not
simplify the palette; it breaks a chart.

So: age them, desaturate them, print them on paper — but eleven must remain mutually distinguishable,
including when two of them sit adjacent as small swatches in a legend.

### The complete screen inventory

Nothing here is optional; an omission is a screen nobody designs.

**Full-screen states** (→ Canvas): country selector (6 countries) · election results.

**Persistent chrome** (→ IMGUI, every tab except Budget): left column — event/game-over banner, headline
stat tiles, policy controls, policy preview with 4 horizon buttons; pinned below — calendar, blocking-
reason status line, 4 speed buttons.

**Six consolidated tabs** — *six, not seven; Tax and Spending merged into Budget on 2026-08-01*:

| Tab | Sub-screens | Renderer |
|---|---|---|
| Statistics | Domestic · International (World Map, trade stats, activity log) | IMGUI |
| Decisions | aggregates every pending interrupt as cards | IMGUI, cards → Canvas when opened |
| Demographics | — | IMGUI |
| Budget | Tax · Spending · Welfare · Infrastructure · SWF — **full-screen mode, left column hidden** | IMGUI |
| Policy/Laws | LaborMarket · CrimeJustice · Sectors · PolicyWeb · Trade | IMGUI |
| Politics | Parliament · Compass · Cabinet · FederalReserve | IMGUI |

**Modals / consequential moments** (→ Canvas): Fed chair selection · cabinet decision · foreign policy
meeting · bill vote · the budget signing moment · pending-interrupt banner.

⚠ **DUAL-SITING — three of these render in TWO places.** Fed chair selection, cabinet decisions and
foreign policy meetings each appear *both* as their own screen *and* embedded inside the Decisions tab
(via a `drawOwnFrame` flag). **Each needs two treatments: a framed standalone, and an unframed embedded
variant that sits inside a host card.** Designing only the standalone leaves the Decisions tab broken.

### What must not regress — eleven load-bearing behaviours

Each fixed a real, documented defect. **The appearance may change completely; the FUNCTION may not.**
Full detail in `CLAUDE.md`; this is the checklist a design must not quietly violate.

1. **The amber draft cue** — one reserved colour meaning "drafted, not enacted". A player must never be
   unable to tell what they changed from what is law. May become a pencil mark or a margin annotation;
   may not become nothing.
2. **Direction-aware green/red** — keyed to whether a change is *good*, not whether the number went *up*.
   Falling unemployment is green.
3. **The MoneyUnit formatter** — a call site cannot render currency without naming a unit. Ended a bug
   that shipped three times on the same value.
4. **Shrink, never truncate** — a clipped number is a plausible wrong number. Text boxes must be able to
   shrink text, so no fixed-size text plate.
5. **Stable control layout** — every control renders every frame in the same order; "not applicable" is a
   disabled state, never an omitted element.
6. **The published/live distinction** — published figures carry reference period, publication date and
   preliminary/revised badges; live figures do not. *A paper aesthetic makes this easier: a published
   figure is a printed bulletin, a live one is a desk reading.*
7. **Per-area colour identity** — see the eleven-hue section above.
8. **The always-visible interrupt indicator** — when time is blocked, every screen must say so and name
   which screen resolves it. A player who cannot see why time stopped experiences a hang.
9. **Legend ↔ chart colour correspondence** *(new, 2026-08-03)* — any legend swatch must be drawn in the
   same colour as the chart element it explains. Proven by breaking it.
10. **Lining figures in any face used for data** *(new)* — Vollkorn was rejected because its old-style
    figures do not align down a column of stat tiles. Applies to any numeral shown in art.
11. **`U+2212` and `U+00B1` coverage** *(new)* — the true minus sign appears in every negative credit
    rating and delta; a font or glyph set lacking it renders a blank box on a readout the player is meant
    to trust.

### The component system — every repeating pattern

These become the design system's components: stat tile · threshold bar · legislative support bar ·
standing/draft pair · draft track · decision card · badge/chip · portrait · area card · tab button ·
sub-category button · slider row · bill card · live-estimate block (**one shared renderer across five
screens** — Labor, Crime & Justice, Sectors, Trade, and the annual budget bill) · speed button · sparkline.

### What already exists — 96 sprites. DO NOT REDRAW.

| Category | Count | Status |
|---|---|---|
| Stat icons (`icon_stat_*`, `icon_trend_*`, `badge_*`, `icon_release_marker`) | 43 | wired |
| Area + nav icons (`icon_area_*` ×10, `icon_nav_*` ×4) | 14 | wired |
| Portraits (9 cabinet + 7 Fed chair) | 16 | wired |
| Chrome (buttons, panel, sliders, scrollbars) | 12 | 3 wired; 9 superseded by v2.0 |
| Background texture (`menu_pattern_tile`) | 1 | wired |
| Country flags (`flag_country_*`) | 6 | wired |
| Party emblems (`emblem_party_*`) | 4 | wired |

Reskinning any of these in the new idiom is in scope. **Inventing replacements for art that already
exists is not.** `ui_button_disabled` is a special case: IMGUI has no disabled style state, so it becomes
usable only once its screen is on Canvas.

---

## 2. Explicitly OUT of scope — please do not produce these

- **Anything the seven data renderers already draw**: axes, gridlines, tick marks, plot lines, threshold
  lines, bars, area fills, legends, sparklines, map shapes, policy-web nodes and edges, hemicycle seats,
  pie wedges, compass dots. All procedural, per working-discipline item 10 — these render real tracked
  simulation data rather than a picture, and that is exactly what rule 10 protects. **Frames, plates and
  paper AROUND them are in scope; the data marks inside are not.**
- **Any sprite already delivered.** **96** are in production — see the table in §1. Check
  `Assets/Resources/Art/UI/` before producing anything. *(This bullet said 84 until 2026-08-03 and was
  wrong; re-derive from the filesystem rather than trusting the number.)*
- **Typefaces.** Already chosen, open-licensed and imported: TeX Gyre Pagella (display + body) and
  Courier Prime (document artifacts). Do not propose or supply fonts.
- **Cabinet portraits for Defense, Foreign Affairs and Education.** Genuinely needed eventually, and
  deliberately **not** requested — see §5.
- **`menu_pattern_tile.png`** — a menu/background texture. **Already delivered** (valid 256×256 PNG in
  "PoliSim GUI redesign.zip") and simply never imported. It is *wanted* — rule 10 approves background/menu
  textures and the country-selector screen has no background — but the work is an **import**, not a
  request. Tracked as live work in `POLISIM_MASTER_ROADMAP.md`. **Do not re-request it**; producing it
  again would duplicate art that exists.
- **Pre-coloured trend arrows or badges.** Colour is applied at runtime; see §3.
- **Real-world currency symbols, agency logos, or national statistics-office branding.** The game's
  institutions are fictional by standing rule 9, and this would also be someone else's trademark.

---

## 3. Format & technical spec

Unchanged from the four previous packs, plus two v2.0 additions (9-slice frames, and the Canvas path).

### 3.0 The eleven hues, as they stand today

These are the **current** saturated screen values, given so the aged/desaturated versions can be derived
from a known starting point rather than invented. `PoliSimTheme` holds the dark-surface tuning;
`UiPalette` holds the base. Both must move together.

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

Semantic colours are separate and also need period equivalents: **amber `#E0B341` = draft-not-enacted**
(reserved, see behaviour 1), good `#4EC98A`, bad `#C8534A`, caution `#E0B341`.

### 3.1 Tintable art — the default, and still binding

- **PNG:** 256×256, 8-bit RGBA, transparent background, **authored pure white** with all shape information
  in the alpha channel. Tinted at draw time via `UiPalette.DrawTintedIcon`. **No pre-coloured variants** —
  colours live in `UiPalette`/`PoliSimTheme` and must stay there.

⚠ **TWO CATEGORIES ARE EXEMPT, and this is not a lapse in the convention.** **Country flags** and **party
emblems** are authored in their own real colours — a flag is not tintable, and the emblem SVGs already
carry `#E0B23C` and `#FFFFFF`. Any new art in those two categories stays full-colour; everything else
stays white-on-alpha. Getting this backwards in either direction produces art that cannot be used:
a tinted flag is wrong, and a pre-coloured area icon defeats the eleven-hue system.

### 3.2 NEW — 9-slice frames (the IMGUI path)

Ornate frames, folders, plates and paper panels are drawn with `GUIStyle.border`, which is a working,
proven mechanism in this project (the chrome pack established it).

- Deliver the **exact border inset in px per edge** with each frame. Corners must not stretch; the centre
  must tile or stretch cleanly.
- Author at **2× the largest size it will render at** — every style in this UI rescales with
  `Screen.height`, so there is no single fixed render size.
- **Non-rectangular edges must be baked into the alpha.** Runtime masking in IMGUI is rectangles only:
  a torn paper edge, a deckle, an oval portrait vignette, a vignetted corner — all baked, none masked.
- **No effect can be applied at runtime.** Shadows, grain, glow, blur and paper texture bake into the
  sprite. Tint and opacity are the only runtime adjustments.

### 3.3 NEW — the Canvas path (narrative screens only)

For the screens listed as Canvas in §1: sprite sheets with 9-slice borders defined in the sheet metadata,
and component specs shaped as prefabs with their states (`normal` / `hover` / `pressed` / `disabled` /
`selected`) and transition timings. Effects *can* be runtime parameters here rather than baked.

Everything else in §3.1 still applies.
- **Renders small.** This icon draws at **22px** on the contextual stat row. Avoid thin strokes and
  interior detail that will disappear.
- **SVG source:** 24×24 geometry, `currentColor` fill, simple primitives, mirroring the existing packs.

### Unity import settings — the defaults are actively harmful here

Stated because these were worked out during the chrome import after Unity's defaults silently damaged
sprites:

| Setting | Value | Why |
|---|---|---|
| Texture Type | `Default` | |
| `nPOTScale` | **None** (`0`) | **Matters most.** The default resamples a non-power-of-two sprite to the nearest power of two, silently altering the artwork |
| `alphaIsTransparency` | **On** (`1`) | Shape lives in alpha |
| sRGB | On (`1`) | |
| Filter Mode | `Bilinear` (`1`) | |
| Compression | **None** (`textureCompression: 0`) | Block compression mangles white-on-alpha at icon sizes |
| Mipmaps | **Off** (`enableMipMap: 0`) | UI sprites never minify |
| Wrap Mode | **Clamp** (`wrapU/V/W: 1`) | |

The delivered `.meta` should match `Assets/Resources/Art/UI/Stats/icon_stat_gdp.png.meta` exactly apart
from its `guid`. Copying that file and changing only the guid is the reliable route.

---

## 4. Filename manifest and the naming rule

**Every filename derives from a real enum value in the code.** This is the rule the whole request format
is built on, and it is why a name can never be invented: the game resolves art at runtime by building the
string from the enum, so a filename that does not match an enum resolves to null and draws nothing.

| Pattern | Derived from | Example |
|---|---|---|
| `icon_area_<systemarea>` | `UiPalette.SystemArea`, lowercased | `icon_area_sovereignwealth` |
| `icon_stat_<statname>` | the displayable stat, lowercased | `icon_stat_laborforceparticipationrate` |
| `icon_nav_<tab>` | `ConsolidatedTab`, lowercased | `icon_nav_policylaws` |
| `portrait_cabinet_<portfolio>_<name_slug>` | `CabinetPortfolio` + `IconLibrary.Slug(name)` | `portrait_cabinet_interiorjustice_amara_oseibonsu` |
| `portrait_fedchair_<name_slug>` | `Slug(name)` | `portrait_fedchair_weilin_tanaka` |
| `emblem_party_<archetype>` | `PartyArchetype`, lowercased | `emblem_party_centristcoalition` |
| `flag_country_<countryid>` | `CountryId`, lowercased | `flag_country_poland` |
| `ui_<control>_<state>` | control + state | `ui_button_disabled` |

`Slug()` = lowercase, drop every non-letter, spaces → underscores. "Wei-Lin Tanaka" → `weilin_tanaka`.

⚠ **Enumerate the DISPLAY enum, not the storage struct.** The macro pack derived its stat list from
`EconomyState`'s 29 fields — the right instinct — and still missed `InterestRate`, which lives on
`CurrencyZone` because a rate belongs to a currency zone rather than to one country. It was structurally
invisible to that derivation while being a headline figure on two screens.

⚠ **Everything must live under `Assets/Resources/`.** `IconLibrary` uses `Resources.Load`, not
`AssetDatabase` (Editor-only, breaks in a player build). This is not a filing preference: the country
flags and party emblems sat outside `Resources/` for weeks, fully delivered and imported, and were
**unreachable by the game the entire time** — nothing referenced them, so nothing ever failed. An asset's
status has two parts, **delivered** and **reachable**, and only the first is visible from the inbox.

**1 source asset → 2 files** (`.png` + `.svg`), as in every previous pack.

---

## 5. Known future need — NOT yet requestable

**Cabinet portraits for the three unimplemented portfolios** (Defense, Foreign Affairs, Education): 3
candidates each, 9 portraits.

**Deliberately not requested, and this is a hard blocker rather than a scheduling choice.** Portrait
filenames derive from each minister's generated name — `portrait_cabinet_<portfolio>_<name_slug>`, resolved
at runtime from the candidate pools in `CabinetSystem`. Those three portfolios have no ministers authored
yet, so there are no names to derive filenames from, and **inventing them would break the
derive-filenames-from-real-values rule this whole request format is built on.**

Current portrait coverage is complete: 9 ministers (3 portfolios × 3 candidates) + 7 Fed chairs = 16
files, all present and name-matched.

Tracked in `MISSING_PREREQUISITES.md` §D1.

---

## 6. Verification note

**The v2.0 brief in §1 is derived from a full survey of the UI layer (2026-08-03), not from memory.**
Screen inventory read from `GameController`'s 80 draw methods and its tab enums; component list from
`PoliSimWidgets`; hue values from `PoliSimTheme`/`UiPalette`; sprite counts from the filesystem, then
cross-checked by `DeliveredAssetCheck` (191/191 entries across 7 zips, 0 missing) and
`StatIconCoverageCheck` (19/19 resolve at runtime). The render-order constraint and the eleven-hue
evidence are both measured results, recorded in `CLAUDE.md`.

⚠ **Three figures in the previous version of this document were STALE, and each was believed accurate
when written**, which is the point:

- *"84 sprite files on disk"* — now 96, and it was already wrong before this survey.
- *"7 consolidated tabs"* — six since the 2026-08-01 Tax+Spending merge; the same stale count survived in
  two code comments until 2026-08-03.
- *"Nothing outstanding"* — true of the request queue, and simultaneously false of the game: 10 delivered
  sprites were sitting outside `Resources/` where the code could not reach them.

**A count in prose is a cached value with no expiry** (working-discipline rule 12). Re-derive the sprite
inventory from the filesystem, and the screen inventory from the enums, before trusting any number here.

---

### Earlier verification note, retained (2026-08-02)

*The `icon_stat_interestrate` delivery record that used to sit in §1 was removed on 2026-08-03 — it was
stranded inside the v2.0 brief when §1 was rewritten, and a closed 2026-08-02 delivery reading as part of
an open request is exactly the kind of stale content this document keeps getting wrong. The delivery
itself is recorded in `COMPLETED.md` §8 and in git history; **its lesson — enumerate the display enum, not
the storage struct — was the part worth keeping and now lives in §4**, where a filename gets derived.*

Every literal icon name referenced anywhere in `Assets/Scripts` was extracted and cross-referenced against
the 84 sprite files then on disk; `icon_stat_interestrate` was the only miss. Area-icon and portrait
coverage were checked separately against `UiPalette.SystemArea` and against the real `CabinetSystem`/
`FederalReserveSystem` candidate pools respectively, and both were complete.
