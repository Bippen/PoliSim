# PoliSim — Claude Design asset request (Master Sequence step 5e)

Status: **compiled, not yet sent to Claude Design pending Elias's review of this list.** No `GameController.cs`
rendering changes have been made and none will be until this request is reviewed and the resulting assets
are back — see `POLISIM_MASTER_ROADMAP.md`'s working-discipline item 10 and Part B's 5e note.

## Context

Master Sequence step 5e was originally scoped as a tab/IA reorganization only, with an aesthetic
restyling pass held separately as step 5f. Elias has revised that: 5e now combines both — the tab/IA
consolidation into 7 tabs, and a full sprite-based visual overhaul (icons + character portraits +
background textures), sourced together via Claude Design. This reverses working-discipline item 10
("all visuals stay procedurally-drawn, no imported sprite art"), which held through step 5d — see the
Roadmap's own updated item 10 for the exact terms of that reversal.

**A prior Claude Design pack already exists in this project**
(`PoliSim GUI redesign.zip`, security-reviewed and origin-verified as a claude.ai download — see the
Roadmap's 5f-prep note) and covers 8 of the areas below already. This request reuses those 8 rather than
re-commissioning them, and only asks for what that pack didn't cover: 3 more area icons, all 9 Cabinet
minister portraits, and 4 party emblems.

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
| `Neutral` | `#8C8C8C` | Generic fallback hue, no dedicated tab | **NEW, but probably not needed — see Q1** | `icon_area_neutral.png` / `.svg` (only if Q1 says yes) |

**Net new icons requested: 2 (Infrastructure, Global), possibly 3 pending Q1.**

## 2. Cabinet minister portraits

One portrait per candidate in `CabinetSystem.CandidatePool` (`Assets/Scripts/Simulation/CabinetSystem.cs`).
Only 3 of the 6 confirmed-scope portfolios are implemented in code today (`CabinetPortfolio.cs`), so this
is the complete current set — **not** requesting ahead for portfolios that don't exist yet (see Q2).

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

## 3. Party emblems

One emblem per `PartyArchetype` (`Assets/Scripts/Data/PartyArchetype.cs`). The hex values below are
**not** deliberately chosen brand colors — they're what `UiPalette.GetCategoricalColor(index)` currently
computes procedurally (a golden-angle HSV rotation) for each party's position in
`HemicycleRenderer.LeftToRightOrder`, included for reference only. See Q5 on whether to honor them.

| `PartyArchetype` | FiscalStance | Current procedural color (approx, not fixed) | Filename |
|---|---|---|---|
| `ProgressiveAlliance` | +0.7 | `#E65050` | `emblem_party_progressivealliance.svg` + `.png` |
| `CentristCoalition` | 0.0 | `#50E67C` | `emblem_party_centristcoalition.svg` + `.png` |
| `NationalistFront` | -0.3 | `#A750E6` | `emblem_party_nationalistfront.svg` + `.png` |
| `ConservativeUnion` | -0.7 | `#E6D350` | `emblem_party_conservativeunion.svg` + `.png` |

**4 emblems requested** (SVG + 256px PNG export each, matching the icon format below). All four party
names are original and fictional (rule 9) — never real political parties.

## 4. Explicitly OUT of scope for this request

- **Per-line-item icons** (individual `TaxType`s, `SpendingCategory`s, `WelfareProgramType`s) — that's
  40+ items across three enums and was never asked for. Icons stay at the `SystemArea`/portfolio/party
  level only, per Elias's own scoping.
- **Background/menu textures** — the existing pack's `menu_pattern_tile.png` (256px seamless tile)
  already covers this; not requesting anything new here since nothing about the 7-tab consolidation
  changes what a background texture needs to be.
- **Country flags** — not requested by default; see Q3 below, this is a real open question, not a
  silent no.
- **Fed Chair candidate portraits** — a separate 7-candidate pool (`FederalReserveSystem.CandidatePool`),
  structurally identical to Cabinet ministers but not mentioned in the original request scope; see Q4.

---

## 5. Format & technical spec

**Icons and emblems** (system-area icons, party emblems) — matches the existing pack's own established
convention exactly, so the new files drop in next to the old ones with zero format drift:
- PNG: 256×256, 8-bit RGBA, transparent background, **authored white** (single-color silhouette — tinted
  at draw time via `PoliSimTheme.Accent(area)`, not shipped as a pre-colored copy per hue).
- SVG: 24×24 source geometry, `currentColor` fill, 2–4 primitives (circle/rounded-rect/triangle/
  ellipse/arc) — simple enough to redraw procedurally later if ever needed, per the existing pack's own
  design intent.
- Unity import settings (for reference, not part of the art brief): Texture Type `Sprite (2D and UI)`,
  Alpha Is Transparency **on**, sRGB **on**, Filter Mode `Bilinear`, Max Size 256, Compression `None`.

**Portraits** (Cabinet ministers) — no prior art precedent exists for these specifically; the current
`PoliSimWidgets.Portrait` is a purely procedural placeholder (a hue ring + geometric head/shoulders
silhouette, no art asset at all), so this is genuinely new territory, not a reuse case:
- PNG: 256×256, 8-bit RGBA, transparent background outside the character.
- Head-and-shoulders bust, centered, composed for a **circular crop** — the existing procedural widget
  clips the portrait into a circle with a colored ring around it, so keep the subject within that circular
  safe area (roughly the inner 90% of the canvas), not full-bleed to the square corners.
- Stylized/illustrated, not photorealistic — consistent with the game's existing abstract, geometric
  aesthetic (Political Compass, hemicycle dots, procedural graphs) and appropriate for a cast that's
  explicitly fictional (rule 9).
- One consistent art style across all 9, since they'll appear side-by-side in candidate-picker UI.

---

## 6. Full filename manifest

```
# System-area icons — 2 new (Infrastructure, Global), 1 conditional (Neutral, see Q1)
icon_area_infrastructure.png
icon_area_infrastructure.svg
icon_area_global.png
icon_area_global.svg
icon_area_neutral.png        # only if Q1 = yes
icon_area_neutral.svg        # only if Q1 = yes

# System-area icons — 8 existing, LOCAL RENAME ONLY, no new art (see Q6)
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

# Party emblems — 4 new
emblem_party_progressivealliance.svg
emblem_party_progressivealliance.png
emblem_party_centristcoalition.svg
emblem_party_centristcoalition.png
emblem_party_nationalistfront.svg
emblem_party_nationalistfront.png
emblem_party_conservativeunion.svg
emblem_party_conservativeunion.png

# Conditional — only if Q3 says yes (6 real national flags, not fictional)
flag_country_usa.png / .svg
flag_country_sweden.png / .svg
flag_country_germany.png / .svg
flag_country_france.png / .svg
flag_country_italy.png / .svg
flag_country_poland.png / .svg
```

**Total confirmed new art: 15 files as source assets (2 icons + 9 portraits + 4 emblems), each with its
PNG/SVG pair where applicable — 25 actual files counting both formats.** Plus 8 local renames (zero new
art). Country flags (12 more files) and the Neutral icon (2 more files) are pending Q3/Q1.

---

## 7. Questions for Elias

1. **Neutral area icon** — `SystemArea.Neutral` is a generic fallback hue with no dedicated tab or card
   of its own (unlike the other 10). Recommend skipping it; confirm or override.
2. **Cabinet portrait scope vs. future portfolios** — only 3 of 6 confirmed-scope `CabinetPortfolio`
   values exist in code today (see `CabinetPortfolio.cs`'s own doc comment on why). Recommend requesting
   portraits only for the 9 candidates that exist now, and commissioning more when/if the other 3
   portfolios actually get built (avoids speculative asset debt for content that doesn't exist yet, and
   for character names that would need to be written first anyway). Confirm or override.
3. **Country flags** — six real countries (USA, Sweden, Germany, France, Italy, Poland), each already
   has a distinct `UiPalette` hue via `GetCountryColor`/`GetCountryArea` (borrowed from an existing
   `SystemArea`, e.g. USA → Political gold, Sweden → Trade teal). Real flags are fine content-wise (rule
   9's "fictional only" applies to invented entities — parties, ministers — not to these six real,
   already-named countries), but that's 6 more commissioned assets not explicitly asked for. Do you want
   real national flags, or is the existing per-country hue sufficient for now? Not assuming either way.
4. **Fed Chair candidates** — a separate 7-candidate pool (`FederalReserveSystem.CandidatePool`),
   structurally identical to Cabinet ministers (named fictional characters with a philosophy and
   description) but not mentioned in the original ask. In scope for portraits this pass, or deferred?
5. **Party emblem colors** — the four colors listed in section 3 are an arbitrary procedural rotation
   (golden-angle HSV), not deliberately chosen political branding, and don't track real-world convention
   (e.g. Progressive Alliance computes to red, not a color usually associated with progressive movements).
   Should Claude Design match these exactly, or take creative discretion within the game's existing dark
   UI palette? Recommend creative discretion, since the colors were never a deliberate design choice to
   begin with.
6. **Rename the 8 existing icons now?** Recommend yes — a local, zero-cost rename (`icon_fiscal.png` →
   `icon_area_fiscal.png` etc.), no new art needed, so all 11 system-area icon files share one consistent
   naming convention rather than 8 under the old pack's short names and 3 under the new one. Confirm or
   override.

---

**Next step once this is reviewed**: send sections 1–3 (with Q1/Q3/Q4/Q5 answers folded in) to Claude
Design as the actual art brief. No `GameController.cs` changes until the assets are back.
