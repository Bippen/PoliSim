# Claude Design asset request — Macro Data & Release Calendar (Master Sequence 9, Step D)

**Status:** ready to send.
**Date:** 2026-08-01.
**Context:** Step D of `POLISIM_MACRO_OVERHAUL_DIRECTIVE.md`. Deliberately compiled and sent FIRST, ahead
of Steps A–C, because sprite work has a long external turnaround while those steps are pure code that
does not depend on it. Third request in this project, after `CLAUDE_DESIGN_ASSET_REQUEST_5E.md` (icons,
portraits, emblems, flags) and `CLAUDE_DESIGN_ASSET_REQUEST_UI_CHROME.md` (control chrome) — both
delivered, verified and in production. Conventions below match those exactly.

---

## 1. What this is for

The game is gaining a real macroeconomic release calendar: each statistic is *published* on a realistic
schedule with a reporting lag, first as a preliminary figure and later as a revision. Graphs will show
when a figure was published versus the period it actually covers, and policy screens will show the
stats each policy genuinely affects.

**Almost all of that is procedural and needs no art.** `GraphRenderer` already draws axes, gridlines,
plot lines, threshold lines, bars and fills, and those stay code under working-discipline item 10's
data-visualization carve-out. This request covers only the four things that genuinely cannot be drawn
adequately as procedural geometry.

---

## 2. Stat icons — 29 sprites

One small icon per tracked statistic, used in policy-screen stat readouts, graph legends and the
dashboard.

**A note on how this list was derived, because it has one honest weakness.** The directive says to take
the list "from the real stat enum once Step C's stats exist" — but Step D now runs *first*, so that enum
does not exist yet. The list below is therefore compiled from two solid sources instead: the **29 fields
that actually exist on `EconomyState` today** (read directly from the code, so those names are real), and
the **seven new stats named in the directive**. Where a new stat's final identifier is not yet decided,
the filename is marked **PROVISIONAL** — the art is unaffected, only the filename may be renamed on
import. Please do not treat provisional names as uncertain *subjects*; the subject matter is settled.

### 2a. Existing tracked stats — names verified against `EconomyState`

| Subject | Filename | Suggested mark |
|---|---|---|
| GDP | `icon_stat_gdp.png` | Rising bar trio |
| Inflation | `icon_stat_inflation.png` | Price tag with an up-arrow |
| Unemployment | `icon_stat_unemployment.png` | Person outline with a broken/dashed edge |
| Approval rating | `icon_stat_approvalrating.png` | Thumbs-up or ballot check |
| Budget balance | `icon_stat_budget.png` | Balanced scale |
| Trade balance | `icon_stat_tradebalance.png` | Two opposing arrows (import/export) |
| Currency strength | `icon_stat_currencystrength.png` | Generic coin (no real currency symbol) |
| Consumption | `icon_stat_consumption.png` | Shopping basket |
| Investment | `icon_stat_investment.png` | Seedling / upward chart in a circle |
| Potential GDP | `icon_stat_potentialgdp.png` | Dashed-outline bar trio (the "capacity" counterpart to GDP) |
| Inflation expectations | `icon_stat_inflationexpectations.png` | Price tag with a dotted forward arrow |
| Consumer confidence | `icon_stat_consumerconfidence.png` | Person with an upward pulse line |
| Business confidence | `icon_stat_businessconfidence.png` | Office block with an upward pulse line |
| Government debt | `icon_stat_governmentdebt.png` | Bank building with a downward arrow |
| Debt-to-GDP ratio | `icon_stat_debttogdpratio.png` | Bank building over a bar (ratio framing) |
| Poverty rate | `icon_stat_povertyrate.png` | Empty bowl or open palm |
| Labor force participation | `icon_stat_laborforceparticipationrate.png` | Group of person outlines |
| Crime index | `icon_stat_crimeindex.png` | Shield with a crack |
| Prison population rate | `icon_stat_prisonpopulationrate.png` | Barred window |
| Organized crime index | `icon_stat_organizedcrimeindex.png` | Linked nodes / network |
| Corruption index | `icon_stat_corruptionindex.png` | Hand exchanging a coin |
| Population | `icon_stat_population.png` | Three person outlines |
| Birth rate | `icon_stat_birthrate.png` | Stylised infant / rising small figure |
| Death rate | `icon_stat_deathrate.png` | Downward figure or urn (restrained, non-morbid) |
| Net migration rate | `icon_stat_netmigrationrate.png` | Arrow crossing a dashed border |
| Dependency ratio | `icon_stat_dependencyratio.png` | Large figure supporting two small ones |
| Population growth rate | `icon_stat_populationgrowthrate.png` | Person outline with an upward arrow |

*(`NaturalBirthRate` and `NaturalNetMigrationRate` are internal baselines the player never sees directly
and deliberately get no icon.)*

### 2b. New stats from the directive — filenames PROVISIONAL

| Subject | Provisional filename | Suggested mark |
|---|---|---|
| Housing cost overburden (the primary housing metric per the directive) | `icon_stat_housingcost.png` | House with a downward pressure arrow on its roof |
| Homeownership rate | `icon_stat_homeownership.png` | House with a key |
| House price index | `icon_stat_housepriceindex.png` | House on a rising line |
| Inequality (Gini) | `icon_stat_inequality.png` | Two bars of very unequal height |
| Real wages | `icon_stat_realwages.png` | Pay envelope or banknote with a small arrow |
| Productivity (GDP per hour) | `icon_stat_productivity.png` | Gear with a clock hand |
| Youth unemployment | `icon_stat_youthunemployment.png` | Smaller person outline with the same broken edge as `unemployment` |
| Life expectancy | `icon_stat_lifeexpectancy.png` | Heart with a pulse line |
| Credit rating | `icon_stat_creditrating.png` | Shield bearing a star or grade band |

**27 + 9 = 36 stat icons.** Please keep visual families consistent — `unemployment` and
`youthunemployment` should read as siblings, as should `gdp`/`potentialgdp` and
`consumerconfidence`/`businessconfidence`, since they appear next to each other.

---

## 3. Trend arrows — 3 sprites

| Subject | Filename | Notes |
|---|---|---|
| Rising | `icon_trend_up.png` | |
| Falling | `icon_trend_down.png` | Should read as a clear mirror of `up`, not a separate design |
| Flat / unchanged | `icon_trend_flat.png` | |

Tinted at runtime, so **do not colour these green or red**. The game decides the colour per stat, because
direction and *goodness* are not the same thing: falling unemployment, inflation, poverty and crime are
all good, while falling GDP and approval are bad. A pre-coloured arrow would be wrong half the time.

---

## 4. Release marker — 1 sprite

| Subject | Filename | Notes |
|---|---|---|
| Publication point | `icon_release_marker.png` | A small pin/flag that sits ON a graph line marking the moment a figure was published |

Must read clearly at roughly 12–16px against a busy plot line, and be visually distinct from an ordinary
data point. Asymmetric (a pin or flag rather than a dot or diamond) so it cannot be mistaken for one.

---

## 5. Revision badges — 2 sprites

| Subject | Filename | Notes |
|---|---|---|
| Preliminary | `badge_preliminary.png` | Small shape suggesting provisionality — dashed or hollow outline |
| Revised | `badge_revised.png` | Same footprint, suggesting settled/confirmed — solid, or a small circular-arrow motif |

These carry real meaning rather than decoration: a preliminary figure may be revised away later, and the
player is expected to sometimes act on one that turns out to have been wrong. **The two must be
distinguishable at a glance and without relying on colour**, since both get tinted per context and some
players cannot rely on hue. Make the silhouettes differ, not just the fill.

---

## 6. Explicitly OUT of scope — please do not produce these

- **Anything `GraphRenderer` already draws**: axes, gridlines, tick marks, plot lines, threshold lines,
  bars, area fills, legends. All procedural, per working-discipline item 10.
- **Country flags, party emblems, portraits, system-area icons, tab icons** — delivered in the 5E pack.
- **Button, slider, scrollbar and panel chrome** — delivered in the UI-chrome pack.
- **Pre-coloured trend arrows or badges** — see sections 3 and 5.
- **Real-world currency symbols, agency logos, or national statistics-office branding.** The game's
  institutions are fictional by standing rule 9, and this would also be someone else's trademark.
- **Calendar/clock chrome for the release schedule.** Dates render as text; no art needed.

---

## 7. Format & technical spec

Identical to the two previous packs, including the `.meta` import settings worked out during the chrome
import — those defaults are wrong for UI sprites in ways that are easy to miss.

- **PNG:** 256×256, 8-bit RGBA, transparent background, **authored pure white** with all shape
  information in the alpha channel. Tinted at draw time via `UiPalette.DrawTintedIcon`. **No
  pre-coloured variants** — colours live in `UiPalette`/`PoliSimTheme` and must stay there.
- **Badges and the release marker:** same 256×256 white-on-transparent treatment. These render small
  (12–20px), so keep them legible at that size — avoid thin strokes and interior detail that will
  disappear.
- **SVG source:** 24×24 geometry, `currentColor` fill, simple primitives, mirroring the existing packs.
- **Unity import settings** (project-side, but stated because the defaults are actively harmful here):
  Texture Type `Default`, `alphaIsTransparency` **on**, sRGB **on**, Filter Mode `Bilinear`, compression
  **None**, mipmaps **off**, wrap mode **Clamp**, and `nPOTScale` **None**. That last one matters most:
  Unity's default would resample a non-power-of-two sprite to the nearest power of two, silently altering
  the artwork before anyone noticed.

---

## 8. Filename manifest

**42 source assets → 84 files** (PNG + SVG each).

```
# Stat icons - existing tracked stats (27), names verified against EconomyState
icon_stat_gdp / inflation / unemployment / approvalrating / budget / tradebalance
icon_stat_currencystrength / consumption / investment / potentialgdp
icon_stat_inflationexpectations / consumerconfidence / businessconfidence
icon_stat_governmentdebt / debttogdpratio / povertyrate / laborforceparticipationrate
icon_stat_crimeindex / prisonpopulationrate / organizedcrimeindex / corruptionindex
icon_stat_population / birthrate / deathrate / netmigrationrate / dependencyratio
icon_stat_populationgrowthrate

# Stat icons - new stats (9), filenames PROVISIONAL
icon_stat_housingcost / homeownership / housepriceindex / inequality / realwages
icon_stat_productivity / youthunemployment / lifeexpectancy / creditrating

# Trend arrows (3)
icon_trend_up / icon_trend_down / icon_trend_flat

# Release marker (1)
icon_release_marker

# Revision badges (2)
badge_preliminary / badge_revised
```

Destination on delivery: `Assets/Resources/Art/UI/Stats/` (PNGs) and
`Assets/Resources/Art/UI/Stats/Source/` (SVGs), mirroring the existing `Icons/` and `Chrome/` layout.
The `Resources/` root is required so `IconLibrary`'s `Resources.Load` reaches them in a real player
build — the same reason the icons, portraits and chrome all live there.

---

## 9. Open question for Elias

**Only one, and it is a scope question rather than an art one.** 36 stat icons is a large ask, and the
27 existing-stat icons have no confirmed consumer yet — Step B will put stats on policy screens, but
exactly which stats appear where is decided during Step B, not now. The nine NEW stats and the six
trend/marker/badge sprites are needed with certainty.

Options: send the full 42 now and have everything ready when Step B lands; or send **15** now (9 new
stats + 3 arrows + 1 marker + 2 badges) and request the 27 existing-stat icons once Step B has shown
which are actually used. *Recommendation: send the full 42.* The turnaround is the expensive part, the
per-icon marginal cost is low, and an unused icon costs a few KB whereas a missing one costs another
round trip mid-implementation.
