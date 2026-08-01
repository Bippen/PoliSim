# PoliSim Macro Data & Release Calendar — icon pack

Delivered 2026-08-01 by Claude Design. Answers `CLAUDE_DESIGN_ASSET_REQUEST_MACRO.md` in full.
Section 9 open question: **the full 42 were produced**, per the request's own recommendation.
Third pack in this project, after the 5E imagery pack and the UI-chrome pack; conventions match both.

## Authoring convention
256x256 PNG, 8-bit RGBA, pure white RGB with all shape information in the ALPHA channel — tint at
draw time via `UiPalette.DrawTintedIcon`. No pre-coloured variants exist, including the trend arrows
and revision badges (request sections 3 and 5): direction is not goodness, and the game decides hue
per stat. SVG sources are 24x24, `currentColor`, 1-8 simple primitives each (line / polyline /
circle / rect / arc), matching the earlier packs' redraw-procedurally-later intent.

Every mark is drawn on the same 24-unit grid at a 1.95-unit stroke, round caps and joins, so the set
reads as one family at any size.

## Contents — 42 sprites, 84 files
- **27 existing-stat icons** (`icon_stat_*`), names as verified against `EconomyState`.
- **9 new-stat icons** (`icon_stat_*`), filenames PROVISIONAL per request section 2b — rename freely
  on import; the subjects are settled, only the identifiers may move.
- **3 trend arrows** (`icon_trend_up` / `_down` / `_flat`) — `down` is a true mirror of `up`; `flat`
  is the same arrow held level, so all three read as one family.
- **1 release marker** (`icon_release_marker`) — staff + solid flag + base dot. Asymmetric by design
  so it cannot be mistaken for a data point; verified legible at 13px.
- **2 revision badges** (`badge_preliminary`, `badge_revised`).

## Requested families, honoured
| Pair | How they read as siblings |
|---|---|
| `gdp` / `potentialgdp` | Identical bar trio; solid vs. dashed outline (actual vs. capacity). |
| `unemployment` / `youthunemployment` | Same person + dashed shoulders; the youth mark is smaller. |
| `consumerconfidence` / `businessconfidence` | Identical pulse line above a person vs. an office block. |
| `inflation` / `inflationexpectations` | Identical tag + up arrow; the expectations arrow is dashed (forward-looking). |
| `crimeindex` / `creditrating` | Same shield silhouette, different interior (crack vs. grade bands). |

## Badges — distinguishable without colour, per request section 5
The silhouettes differ, not just the fill: **preliminary is a dashed-outline DIAMOND**, **revised is a
solid RING**. Either survives greyscale, monochrome tinting and a 12px render. The ring was chosen
over a plain filled dot specifically so a revised badge cannot be misread as an ordinary data point
on a plot line.

## Notes worth reading before wiring
1. **Nine marks were redrawn after a legibility pass**, not first-draft output: `netmigrationrate`
   and `housingcost` (a centred arrow crossing/abutting another shape read as a plus sign and a
   chimney — both arrows were moved off-axis), `corruptionindex`, `organizedcrimeindex`, `birthrate`,
   both confidence marks, both unemployment marks. Called out because the first versions looked fine
   in the abstract and only failed when actually rendered.
2. **`population` vs. `laborforceparticipationrate`** are necessarily close — the request specifies
   person groups for both. They are separated deliberately: population is three solid figures at
   mixed sizes; participation is three EQUAL figures on a baseline with the third DASHED (the
   non-participating share). If they still collide in situ, participation is the one to redraw.
3. **`birthrate` is a cradle, not a person**, so it does not collide with `populationgrowthrate`
   (person + up arrow). `deathrate` is an urn — restrained, per the brief.
4. **No real currency symbols, agency logos or national marks** appear anywhere in the pack
   (`currencystrength` is two concentric circles — a generic coin), per request section 6 and
   standing rule 9.
5. **Unity import** (from request section 7, unchanged): Texture Type Default, alphaIsTransparency ON,
   sRGB ON, Bilinear, compression None, mipmaps OFF, wrap Clamp, **nPOTScale None**.

Destination: `Assets/Resources/Art/UI/Stats/` (PNG) and `Assets/Resources/Art/UI/Stats/Source/` (SVG).
