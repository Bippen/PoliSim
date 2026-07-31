# PoliSim — Claude Design asset request (Master Sequence step 5e)

Status: **all six open questions answered by Elias (2026-07-31) — ready to send to Claude Design.** No
`GameController.cs` rendering changes have been made and none will be until the resulting assets are
back — see `POLISIM_MASTER_ROADMAP.md`'s working-discipline item 10 and Part B's 5e note.

## Context

Master Sequence step 5e was originally scoped as a tab/IA reorganization only, with an aesthetic
restyling pass held separately as step 5f. Elias has revised that: 5e now combines both — the tab/IA
consolidation into 7 tabs, and a full sprite-based visual overhaul (icons + character portraits + real
country flags + background textures), sourced together via Claude Design. This reverses
working-discipline item 10 ("all visuals stay procedurally-drawn, no imported sprite art"), which held
through step 5d — see the Roadmap's own updated item 10 for the exact terms of that reversal.

**A prior Claude Design pack already exists in this project**
(`PoliSim GUI redesign.zip`, security-reviewed and origin-verified as a claude.ai download — see the
Roadmap's 5f-prep note) and covers 8 of the areas below already. This request reuses those 8 rather than
re-commissioning them, and asks for what that pack didn't cover: 3 more area icons, all 9 Cabinet
minister portraits, all 7 Fed Chair candidate portraits, 4 party emblems, and 6 real national flags.

**What stays procedural, unaffected by any of this**: `PoliSimTheme.cs`'s `RoundedBox`/`RoundedCard`/
`Pill`/`Rule`/`TopAccent`/`LeftSpine` primitives (pure rounded-rect/line geometry, no art asset), and
every existing data visualization — `GraphRenderer`, `MapRenderer`, `PolicyWebRenderer`,
`PoliticalCompassRenderer`, `HemicycleRenderer`. None of those draw a "picture"; they render real
tracked simulation data, and nothing about this request changes that.

---

## 1. System-area icons

One icon per `UiPalette.SystemArea` (`Assets/Scripts/UI/UiPalette.cs`). Colors below are the actual
`AreaColors` values read directly from that file (converted from the source `Color(r,g,b)` floats), not
approximated — tint icons to these at draw time exactly like the existing pack's own README already
specifies ("white on transparent — tint at draw time"), so these are reference-only, not a request to
author 11 separately-colored files.

| `SystemArea` | Hex | Used by (real tab/UI) | Status | Filename (new naming convention) |
|---|---|---|---|---|
| `Fiscal` | `#4A80D6` | Tax Policy, Budget Process | **Have it** — reuse `icon_fiscal.png`/`.svg` | `icon_area_fiscal.png` / `.svg` (rename only) |
| `Trade` | `#2EBFBF` | Trade tab | **Have it** — reuse `icon_trade.png`/`.svg` | `icon_area_trade.png` / `.svg` (rename only) |
| `Political` | `#D6A62E` | Federal Reserve, Elections, USA's country color | **Have it** — reuse `icon_political.png`/`.svg` | `icon_area_political.png` / `.svg` (rename only) |
| `Welfare` | `#D64A9E` | Welfare Policy | **Have it** — reuse `icon_welfare.png`/`.svg` | `icon_area_welfare.png` / `.svg` (rename only) |
| `Labor` | `#D67A2E` | Labor Market | **Have it** — reuse `icon_labor.png`/`.svg` | `icon_area_labor.png` / `.svg` (rename only) |
| `CrimeJustice` | `#A64A4A` | Crime & Justice | **Have it** — reuse `icon_crime.png`/`.svg` | `icon_area_crimejustice.png` / `.svg` (rename — old name was abbreviated) |
| `Sectors` | `#7A4AD6` | Economic Sectors | **Have it** — reuse `icon_sectors.png`/`.svg` | `icon_area_sectors.png` / `.svg` (rename only) |
| `SovereignWealth` | `#A68F2E` | Sovereign Wealth Fund | **Have it** — reuse `icon_sovereign.png`/`.svg` | `icon_area_sovereignwealth.png` / `.svg` (rename — old name was abbreviated) |
| `Infrastructure` | `#4A8FA6` | Infrastructure tab | **NEW — request** | `icon_area_infrastructure.png` / `.svg` |
| `Global` | `#6BADE0` | World Map tab | **NEW — request** | `icon_area_global.png` / `.svg` |
| `Neutral` | `#8C8C8C` | Generic fallback hue, no dedicated tab | **SKIPPED — confirmed by Elias (Q1)** | not requested |

**Net new icons: 2 (Infrastructure, Global).**

## 2. Cabinet minister portraits

One portrait per candidate in `CabinetSystem.CandidatePool` (`Assets/Scripts/Simulation/CabinetSystem.cs`).
Only 3 of the 6 confirmed-scope portfolios are implemented in code today (`CabinetPortfolio.cs`), so this
is the complete current set — **confirmed by Elias (Q2): only these 9, not requesting ahead for
portfolios that don't exist yet.**

| Portfolio | Candidate | Philosophy | Filename |
|---|---|---|---|
| `FinanceTreasury` | Elena Voskresenskaya | Reformist | `portrait_cabinet_financetreasury_elena_voskresenskaya.png` |
| `FinanceTreasury` | Marcus Ferreira | Pragmatic | `portrait_cabinet_financetreasury_marcus_ferreira.png` |
| `FinanceTreasury` | Harold Whitmore | Traditionalist | `portrait_cabinet_financetreasury_harold_whitmore.png` |
| `InteriorJustice` | Amara Osei-Bonsu | Reformist | `portrait_cabinet_interiorjustice_amara_oseibonsu.png` |
| `InteriorJustice` | Jonas Lindqvist | Pragmatic | `portrait_cabinet_interiorjustice_jonas_lindqvist.png` |
| `InteriorJustice` | Bruno Castellano | Traditionalist | `portrait_cabinet_interiorjustice_bruno_castellano.png` |
| `HealthSocialAffairs` | Ingrid Solberg | Reformist | `portrait_cabinet_healthsocialaffairs_ingrid_solberg.png` |
| `HealthSocialAffairs` | Wei-Lin Tanaka | Pragmatic | `portrait_cabinet_healthsocialaffairs_weilin_tanaka.png` |
| `HealthSocialAffairs` | Otto Baumgartner | Traditionalist | `portrait_cabinet_healthsocialaffairs_otto_baumgartner.png` |

**9 portraits requested.** Every name here is an original fictional character (per working-discipline
rule 9 — never a real person); their one-line personas are in `CabinetSystem.cs`'s own `CandidatePool` if
useful character-brief context for the artist (e.g. Elena Voskresenskaya: "Wants to modernize tax
administration wholesale — digital reporting, real-time enforcement...").

Filenames deliberately do NOT encode `CabinetMinisterPhilosophy` (Reformist/Pragmatic/Traditionalist) —
only portfolio + candidate name, since philosophy isn't part of this visual request (no styling by
philosophy has been asked for). Hyphenated real-world-style names (Osei-Bonsu, Wei-Lin) are flattened to
one lowercase word each in the filename to keep it plain ASCII with no punctuation.

## 3. Fed Chair candidate portraits

**Confirmed in scope by Elias (Q4)**: structurally identical to Cabinet ministers — one portrait per
candidate in `FederalReserveSystem.CandidatePool` (`Assets/Scripts/Simulation/FederalReserveSystem.cs`),
7 total, USA-only (Fed Chair is this codebase's one deliberate USA-only mechanic).

| Candidate | Philosophy | Filename |
|---|---|---|
| Marcus Thackeray | Hawkish | `portrait_fedchair_marcus_thackeray.png` |
| Ines Kowalski | Hawkish | `portrait_fedchair_ines_kowalski.png` |
| Theodore Voss | Moderate | `portrait_fedchair_theodore_voss.png` |
| Priya Anand | Moderate | `portrait_fedchair_priya_anand.png` |
| Roland Kade | Moderate | `portrait_fedchair_roland_kade.png` |
| Simone Delacroix | Dovish | `portrait_fedchair_simone_delacroix.png` |
| Nathaniel Osei | Dovish | `portrait_fedchair_nathaniel_osei.png` |

**7 portraits requested.** Same format spec and fictional-character rule as Cabinet portraits (section
6 below) — no `FedChairPhilosophy` in the filename, same reasoning as Cabinet's own philosophy omission.

## 4. Party emblems

One emblem per `PartyArchetype` (`Assets/Scripts/Data/PartyArchetype.cs`). **Elias confirmed (Q5): track
real-world political-color convention instead of the arbitrary procedural rotation
`UiPalette.GetCategoricalColor` currently produces** (which was never a deliberate choice — see the old
values below, kept for the record).

Real-world party colors vary by country, and `PartyArchetype` is an explicitly SHARED taxonomy across
all six countries (`PartyArchetypeData`'s own doc comment), not USA-specific — 5 of the 6 countries
(Sweden, Germany, France, Italy, Poland) follow the INTERNATIONAL convention (red = left/socialist,
blue = conservative/right, yellow = liberal/centrist), which is the opposite of the US-specific
convention (red = Republican/right, blue = Democrat/left). Given the taxonomy is shared and the country
roster is majority-non-US, this request uses the international convention — a deliberate, documented
tradeoff, not an oversight. `NationalistFront` has no single obvious real-world color (many
right-populist parties use blue, which would clash with `ConservativeUnion` here) — purple is proposed
instead, following UKIP's real-world precedent as a populist/protest party, and specifically avoiding
brown given its unrelated but troubling historical association with a real fascist movement.

| `PartyArchetype` | FiscalStance | Real-world convention | Suggested reference hex (Claude Design's discretion for final shade) | Filename |
|---|---|---|---|---|
| `ProgressiveAlliance` | +0.7 | Red (international left/socialist) | `#D6423C` | `emblem_party_progressivealliance.svg` + `.png` |
| `CentristCoalition` | 0.0 | Yellow/gold (international liberal/centrist, e.g. Germany's FDP) | `#E0B23C` | `emblem_party_centristcoalition.svg` + `.png` |
| `NationalistFront` | -0.3 | Purple (real-world populist/protest precedent, e.g. UK's UKIP — NOT brown) | `#8B3CD6` | `emblem_party_nationalistfront.svg` + `.png` |
| `ConservativeUnion` | -0.7 | Blue (international right/conservative) | `#3C6CD6` | `emblem_party_conservativeunion.svg` + `.png` |

<details>
<summary>Superseded — old procedural (arbitrary, not real-world) reference colors, kept for the record</summary>

Previously listed as reference (what `UiPalette.GetCategoricalColor(index)`'s golden-angle HSV rotation
happens to compute for each party's `HemicycleRenderer.LeftToRightOrder` position) — no longer the
target now that Q5 is resolved: ProgressiveAlliance `#E65050`, CentristCoalition `#50E67C`,
NationalistFront `#A750E6`, ConservativeUnion `#E6D350`.

</details>

**4 emblems requested** (SVG + 256px PNG export each, matching the icon format below). All four party
names are original and fictional (rule 9) — never real political parties; only their COLOR follows
real-world convention, not their name or platform.

## 5. Country flags

**Confirmed by Elias (Q3): real national flags**, one per `CountryId`
(`Assets/Scripts/Data/CountryId.cs`) — six real, already-named countries, so rule 9's "fictional only"
requirement (which applies to invented entities — parties, ministers — not real nations) doesn't apply
here. These supplement, not replace, each country's existing `UiPalette` hue
(`GetCountryColor`/`GetCountryArea`, shown below for reference).

| `CountryId` | Existing `UiPalette` hue (borrowed `SystemArea`) | Filename |
|---|---|---|
| `USA` | Political `#D6A62E` | `flag_country_usa.png` / `.svg` |
| `Sweden` | Trade `#2EBFBF` | `flag_country_sweden.png` / `.svg` |
| `Germany` | Welfare `#D64A9E` | `flag_country_germany.png` / `.svg` |
| `France` | Labor `#D67A2E` | `flag_country_france.png` / `.svg` |
| `Italy` | Sectors `#7A4AD6` | `flag_country_italy.png` / `.svg` |
| `Poland` | SovereignWealth `#A68F2E` | `flag_country_poland.png` / `.svg` |

**6 flags requested.** Unlike every other category in this request, these must be **accurate reproductions
of the real national flags** — real official colors and proportions, no creative reinterpretation. See
section 6 for the specific format guidance this implies (different from the icon/portrait spec).

---

## 6. Explicitly OUT of scope for this request

- **Per-line-item icons** (individual `TaxType`s, `SpendingCategory`s, `WelfareProgramType`s) — that's
  40+ items across three enums and was never asked for. Icons stay at the `SystemArea`/portfolio/party
  level only, per Elias's own scoping.
- **Background/menu textures** — the existing pack's `menu_pattern_tile.png` (256px seamless tile)
  already covers this; not requesting anything new here since nothing about the 7-tab consolidation
  changes what a background texture needs to be.

---

## 7. Format & technical spec

**Icons and emblems** (system-area icons, party emblems) — matches the existing pack's own established
convention exactly, so the new files drop in next to the old ones with zero format drift:
- PNG: 256×256, 8-bit RGBA, transparent background, **authored white** (single-color silhouette — tinted
  at draw time via `PoliSimTheme.Accent(area)`, not shipped as a pre-colored copy per hue). Party
  emblems are the one exception — author them IN their real-world-convention color directly (section 4),
  not white-for-tinting, since each party's color is now a fixed piece of its identity, not a
  runtime-swappable area hue.
- SVG: 24×24 source geometry, `currentColor` fill (icons) or fixed fill (emblems), 2–4 primitives
  (circle/rounded-rect/triangle/ellipse/arc) — simple enough to redraw procedurally later if ever needed,
  per the existing pack's own design intent.
- Unity import settings (for reference, not part of the art brief): Texture Type `Sprite (2D and UI)`,
  Alpha Is Transparency **on**, sRGB **on**, Filter Mode `Bilinear`, Max Size 256, Compression `None`.

**Portraits** (Cabinet ministers, Fed Chair candidates) — no prior art precedent exists for these
specifically; the current `PoliSimWidgets.Portrait` is a purely procedural placeholder (a hue ring +
geometric head/shoulders silhouette, no art asset at all), so this is genuinely new territory, not a
reuse case:
- PNG: 256×256, 8-bit RGBA, transparent background outside the character.
- Head-and-shoulders bust, centered, composed for a **circular crop** — the existing procedural widget
  clips the portrait into a circle with a colored ring around it, so keep the subject within that circular
  safe area (roughly the inner 90% of the canvas), not full-bleed to the square corners.
- Stylized/illustrated, not photorealistic — consistent with the game's existing abstract, geometric
  aesthetic (Political Compass, hemicycle dots, procedural graphs) and appropriate for a cast that's
  explicitly fictional (rule 9).
- One consistent art style across all 16 (9 Cabinet + 7 Fed Chair), since they can appear side-by-side in
  candidate-picker UI.

**Flags** (country flags) — a different brief from everything else in this request, since these depict
REAL national symbols and must be accurate, not creative:
- Real official colors and proportions for each flag — no stylization, no palette-matching to the game's
  own dark UI, no reinterpretation. Official aspect ratios differ per country (e.g. USA 10:19, most of
  the European flags here 2:3 or 3:5) — preserve each flag's own real proportions rather than stretching
  to a uniform shape.
- Deliver each on a 256×256 transparent-background canvas with the flag letterboxed/centered at its own
  correct aspect ratio (not stretched to fill the square) — keeps all 6 files uniform for layout purposes
  in Unity without distorting any individual flag.
- PNG only (no SVG requested) — flags are typically simple flat-color geometry, but exact vector
  reproduction of some of these (e.g. Poland's eagle emblem is NOT used on the plain civil flag, so this
  should be the plain two-band white-over-red flag) isn't worth the same procedural-redraw argument the
  icons/emblems have, so a clean high-res PNG is sufficient.
- Same Unity import settings as icons/portraits above.

---

## 8. Full filename manifest

```
# System-area icons — 2 new (Infrastructure, Global)
icon_area_infrastructure.png
icon_area_infrastructure.svg
icon_area_global.png
icon_area_global.svg

# System-area icons — 8 existing, LOCAL RENAME ONLY, no new art (confirmed, Q6)
icon_area_fiscal.png / .svg          (was icon_fiscal)
icon_area_trade.png / .svg           (was icon_trade)
icon_area_political.png / .svg       (was icon_political)
icon_area_welfare.png / .svg         (was icon_welfare)
icon_area_labor.png / .svg           (was icon_labor)
icon_area_crimejustice.png / .svg    (was icon_crime)
icon_area_sectors.png / .svg         (was icon_sectors)
icon_area_sovereignwealth.png / .svg (was icon_sovereign)

# Cabinet minister portraits — 9 new
portrait_cabinet_financetreasury_elena_voskresenskaya.png
portrait_cabinet_financetreasury_marcus_ferreira.png
portrait_cabinet_financetreasury_harold_whitmore.png
portrait_cabinet_interiorjustice_amara_oseibonsu.png
portrait_cabinet_interiorjustice_jonas_lindqvist.png
portrait_cabinet_interiorjustice_bruno_castellano.png
portrait_cabinet_healthsocialaffairs_ingrid_solberg.png
portrait_cabinet_healthsocialaffairs_weilin_tanaka.png
portrait_cabinet_healthsocialaffairs_otto_baumgartner.png

# Fed Chair candidate portraits — 7 new
portrait_fedchair_marcus_thackeray.png
portrait_fedchair_ines_kowalski.png
portrait_fedchair_theodore_voss.png
portrait_fedchair_priya_anand.png
portrait_fedchair_roland_kade.png
portrait_fedchair_simone_delacroix.png
portrait_fedchair_nathaniel_osei.png

# Party emblems — 4 new
emblem_party_progressivealliance.svg
emblem_party_progressivealliance.png
emblem_party_centristcoalition.svg
emblem_party_centristcoalition.png
emblem_party_nationalistfront.svg
emblem_party_nationalistfront.png
emblem_party_conservativeunion.svg
emblem_party_conservativeunion.png

# Country flags — 6 new
flag_country_usa.png
flag_country_sweden.png
flag_country_germany.png
flag_country_france.png
flag_country_italy.png
flag_country_poland.png
```

**Total new art: 28 source assets → 47 actual files** (2 icons × 2 formats, 9 Cabinet portraits × 1
format, 7 Fed Chair portraits × 1 format, 4 emblems × 2 formats, 6 flags × 1 format), plus 8 local
renames of already-existing art (zero new files).

---

## 9. Elias's answers (resolved 2026-07-31 — was "Questions for Elias")

1. **Neutral area icon** — recommendation confirmed. Skipped, not requested.
2. **Cabinet portrait scope vs. future portfolios** — recommendation confirmed. Only the 9 candidates
   that exist in code today; more requested later if/when the other 3 confirmed-scope portfolios are
   actually built.
3. **Country flags** — real national flags requested (section 5), not just the existing per-country hue.
4. **Fed Chair candidates** — confirmed in scope for portraits this pass (section 3), same as Cabinet
   ministers.
5. **Party emblem colors** — track real-world political-color convention (section 4: red/yellow/purple/
   blue), not the arbitrary procedural rotation.
6. **Rename the 8 existing icons now** — recommendation confirmed. Done in this document's own filename
   manifest (section 8); the actual file rename on disk happens once these are imported.

---

**Next step**: send sections 1–5 (the actual art brief — icons, Cabinet portraits, Fed Chair portraits,
emblems, flags) to Claude Design. No `GameController.cs` changes until the assets are back.
