# Claude Design asset request — PoliSim

**Status:** ready to send.
**Date:** 2026-08-02.
**Supersedes:** `CLAUDE_DESIGN_ASSET_REQUEST_5E.md`, `_UI_CHROME.md`, `_UI_CHROME_ADDENDUM.md` and
`_MACRO.md` — all four fully delivered, imported and verified in production. Their contents are recorded
in `COMPLETED.md` §8 and the originals remain in git history. **This is now the single standing asset
request document**; new requests append here rather than starting a new file.

**Conventions below are unchanged** from those four packs. Fourth request in this project.

---

## 1. Outstanding request — 1 sprite

| Subject | Filename | Suggested mark |
|---|---|---|
| Interest rate | `icon_stat_interestrate.png` | A percent sign paired with a policy-rate motif — e.g. `%` over a small stepped line, reading as "the rate that is *set*" rather than a generic percentage |

**Why it needs to be distinct from the inflation icon.** `icon_stat_inflation` is a price tag with an
up-arrow. Interest rate is a *policy lever the player pulls*, not an outcome they observe, and the two sit
adjacent on the Fiscal-area stat row. If they read as variants of one another the player will misread
which one they control.

### Why this one was missed, so the same derivation error does not recur

The macro pack derived its stat list from **"the 29 fields that actually exist on `EconomyState`"** — a
deliberately code-grounded method, and the right instinct. But **`InterestRate` is not an `EconomyState`
field.** It lives on `CurrencyZone.InterestRate`, because a rate belongs to a currency zone rather than to
one country's economy — the Eurozone five share one. So it was structurally invisible to that derivation
while being, simultaneously:

- one of the 18 stats reachable on a policy screen (`StatNodeId.InterestRate`),
- the target of its own policy node (`PolicyNodeId.InterestRateDecision`),
- an input to the Taylor Rule, and
- the headline figure on both the Fed and Eurozone screens.

**Lesson for any future stat-icon list: enumerate the display enum (`StatNodeId`), not the storage
struct.** Anything the UI can show needs an icon regardless of which type owns the field.

---

## 2. Explicitly OUT of scope — please do not produce these

- **Anything `GraphRenderer` already draws**: axes, gridlines, tick marks, plot lines, threshold lines,
  bars, area fills, legends, sparklines. All procedural, per working-discipline item 10.
- **Any icon already delivered.** 84 sprites are in production across four packs — 42 stat/trend/badge, 14
  nav and area icons, 16 portraits, 12 chrome. Check `Assets/Resources/Art/UI/` before producing anything.
- **Cabinet portraits for Defense, Foreign Affairs and Education.** Genuinely needed eventually, and
  deliberately **not** requested — see §5.
- **Pre-coloured trend arrows or badges.** Colour is applied at runtime; see §3.
- **Real-world currency symbols, agency logos, or national statistics-office branding.** The game's
  institutions are fictional by standing rule 9, and this would also be someone else's trademark.

---

## 3. Format & technical spec

Identical to all four previous packs.

- **PNG:** 256×256, 8-bit RGBA, transparent background, **authored pure white** with all shape information
  in the alpha channel. Tinted at draw time via `UiPalette.DrawTintedIcon`. **No pre-coloured variants** —
  colours live in `UiPalette`/`PoliSimTheme` and must stay there.
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

## 4. Filename manifest

**1 source asset → 2 files.**

```
icon_stat_interestrate.png
icon_stat_interestrate.svg
```

**Destination:** `Assets/Resources/Art/UI/Stats/` — under `Resources/` because `IconLibrary` uses
`Resources.Load` rather than `AssetDatabase`, which is Editor-only and would break in a player build.

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

**This request is the complete outstanding set as of 2026-08-02, verified rather than assumed.** Every
literal icon name referenced anywhere in `Assets/Scripts` was extracted and cross-referenced against the
84 sprite files on disk; `icon_stat_interestrate` was the only miss. Area-icon and portrait coverage were
checked separately against `UiPalette.SystemArea` and against the real `CabinetSystem`/
`FederalReserveSystem` candidate pools respectively, and both are complete.
