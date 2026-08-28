# Claude Design asset request — PoliSim

**Status — NO LIVE BOARD ASK; ONE RE-CUT PENDING (2026-08-28, evening).** The eighth request (§1, the two
UI v3.0 boards) was **answered the same day it was sent** — boards 1m ("Screen 0 — The Desk, folded")
and 1n ("the rail") on the live screens file, no gap costed — and migrated per this document's charter
("appended to, then emptied on delivery"): the ask and its annexes verbatim to `COMPLETED.md` §41, the
boards' rulings to `POLISIM_V2_SCREEN_SPEC.md` §A.17, the build the same day (v3.0 Phase B). **§E5** is
half closed (the slider strip: source-less by Design's account, done as asked) and half a measured
re-cut away (the hatch source: the 2026-08-28 re-export carries a 32 px period where the shipped PNG's
is 16 — the figures are in §E5, the one re-cut is the live ask). What else is here: **§0** the delivered
set as it stands, **§4** what is costed but not yet requestable, and **§5** the standing conventions.
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


## 1. The eighth request — ANSWERED 2026-08-28 and migrated

Two boards for UI v3.0 — *"Screen 0, The Desk, folded"* and *"the rail"* — asked 2026-08-28 against three
annexes (the census of the landing screen's text, the instrument inventory with measured minimums, the
captures) and **answered the same day** as boards 1m and 1n on the live screens file, with §E5 answered
beside them. The ask as sent, with Annexes A and B verbatim, is `COMPLETED.md` §41; the boards as read are
`POLISIM_V2_SCREEN_SPEC.md` §A.17; the build is v3.0 Phase B (`GameController.Desk.cs`, the same day).
Nothing of it is live here.


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
