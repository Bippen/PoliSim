# Claude Design asset request — PoliSim

**Status — EMPTY OF LIVE ASKS (2026-08-27).** The seventh request went out on 2026-08-27 and was answered
the same day: §1 the eight cabinet portraits delivered as `PoliSim v2 Design Progress5.zip` (verified on
the six-step bar, imported, `PortraitCoverageCheck` 25 of 25); §2 the calendar panel board and §3 the
graph-weight ruling answered as screens **1k** and **1l** on Design's live `PoliSim v2 Screens.dc.html`.
Per this document's charter ("appended to, then emptied on delivery") the three asks migrated to
`COMPLETED.md` §24 ("The seventh request — sent, answered and imported in one day"), the rulings to
`POLISIM_V2_SCREEN_SPEC.md` §A.16, and the implementation items to the roadmap (live items 8–9, not
started by Elias's ruling). What remains here: **§0** the delivered set as it stands, **§4** what is costed
but not yet requestable — the next ask starts from there — and **§5** the standing conventions.
**Date:** 2026-08-27.

**Standing rule: a count in prose is a cached value with no expiry** (working-discipline rule 12).
Before trusting any number in this document, re-derive it: sprites with
`find Assets/Resources/Art/UI -name '*.png'`, chrome coverage with `ChromeV2CoverageCheck`, stat icons
with `StatIconCoverageCheck`, portraits with `PortraitCoverageCheck`, deliveries with
`DeliveredAssetCheck`, importer state with `ImporterSettingsCheck`, screen inventories from the enums
(`StatNodeId`, `UiPalette.SystemArea`, `ConsolidatedTab`, `CabinetPortfolio` ×
`CabinetSystem.CandidatePool`, `CountryId`, `PartyArchetype`). This document has been the failure that
rule exists to catch at least four times.

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
