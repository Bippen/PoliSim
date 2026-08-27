# Claude Design asset request — PoliSim

**Status — rewritten 2026-08-27 (the third consolidation) as ONE request, ready to send.** Everything
answered or dated has moved to `COMPLETED.md` (§24 the v2.0 collaboration, §33 the asset inventory);
git history holds every original section. What remains here is derived from the codebase on
2026-08-27 — the five Editor checks, the display enums and every sprite's call site — not from prose:
**§0** the state of the delivered set (so nothing is asked for twice), **§1–§3** the three live asks,
**§4** what is costed but not yet requestable, **§5** the standing conventions. Seventh request in this
project. **Date:** 2026-08-27.

➡ **Send order: §1 (the verdict Design is waiting for), then §2, then §3.** All three travel in one
package (`MISSING_PREREQUISITES.md` §S — sending is Elias's, the E2 convention). §4 is not sent; it is
here so the next ask does not arrive as a surprise.

✅ **SENT 2026-08-27** (on Elias's instruction, via `DesignSync` to Design's project): in place at
`uploads/CLAUDE_DESIGN_ASSET_REQUEST.md` and at the new dated path `send/design_request_2026-08-27/`
with `ATTACHMENTS.md`, `SHA256SUMS.txt` and the eight captures; both readbacks hash-identical to this
file at the send (`9a464915…24eec` CRLF / `bf7c2263…7cfb` LF). `MISSING_PREREQUISITES.md` §S is the
record; §2/§3's status lines below are kept as they were sent.

**Standing rule: a count in prose is a cached value with no expiry** (working-discipline rule 12).
Before trusting any number in this document, re-derive it: sprites with
`find Assets/Resources/Art/UI -name '*.png'`, chrome coverage with `ChromeV2CoverageCheck`, stat icons
with `StatIconCoverageCheck`, deliveries with `DeliveredAssetCheck`, importer state with
`ImporterSettingsCheck`, screen inventories from the enums (`StatNodeId`, `UiPalette.SystemArea`,
`ConsolidatedTab`, `CabinetPortfolio` × `CabinetSystem.CandidatePool`, `CountryId`, `PartyArchetype`).
This document has been the failure that rule exists to catch at least four times.

---

## 0. The delivered set at HEAD — derived 2026-08-27, so nothing below is asked for twice

**The five checks (logs `..\PoliSim-captures\logs\check_*_20260827_*.log`):**

| check | result | what it enumerates (rule 14) |
|---|---|---|
| `DeliveredAssetCheck` | **0 missing from 0 root zips, 0 missing from 13 archived packs** (20 superseded-by-ruling entries skipped via the manifest's `!` rows; one `ref`) | every archived zip's entries against `Assets/` |
| `StatIconCoverageCheck` | **19 of 19 resolve** | every `StatNodeId` icon + `menu_pattern_tile` — NOT chrome, emblems, marks or portraits |
| `ChromeV2CoverageCheck` | **50 of 50 resolve; 50 of 50 specified present; both directions clean** | `ChromeManifest.txt` against `Chrome/` |
| `ImporterSettingsCheck` | **140 sprites, 0 errors, 0 warnings** (112 white-on-alpha tinted, 27 full-colour, 1 tiling) | every `*.png` under `Assets/Resources/Art/UI/`, asserted against the imported texture |
| `PartyMarkCoverageCheck` | **PARTY SYSTEM NOT PRESENT — VERIFIED NOTHING** (honest; item 10's gate) | seeded parties' marks — none exist on `main` |

**On disk (140 PNGs):** Chrome 50 · Emblems 9 (4 `emblem_party_*` + 5 `mark_party_*`) · Flags 6 · Icons
14 (4 `icon_nav_*` + 10 `icon_area_*`) · Portraits 17 · Stats 43 · Textures 1.

**Coverage by display enum — complete, nothing to ask:** `StatNodeId` 18/18 · `ConsolidatedTab` 6 of 6
tabs draw an icon (4 `icon_nav_*`; Budget and Politics draw `icon_area_fiscal`/`_political` by design) ·
`PartyArchetype` emblems 4/4 · `CountryId` flags 6/6 · Fed chair pool 7/7 · the nine shipped
ministers 9/9 · `menu_pattern_tile` 1/1 · the chrome pack 50 = 50.

**Delivered and held — DO NOT RE-REQUEST, the wiring is ours:** 25 of the 43 `Stats/` sprites have no
call site (19 `icon_stat_*` for stats without a `StatNodeId`, plus `icon_trend_up/down/flat`,
`badge_preliminary/revised`, `icon_release_marker`); 8 of the 10 `icon_area_*` icons are drawn and
unplaced; 7 chrome names have no load call (`ui_frame_double`, `ui_btn_disabled`, `ui_stamp_draft`,
`ui_portrait_frame_oval`, `ui_btn_paper_canvas` + `_hover` + `_pressed`); the 5 `mark_party_*` await
seeded parties (item 10). These are on the roadmap as place-or-hold items and in `COMPLETED.md` §33 as
the record.

**The only coverage gap the enums show:** `CabinetSystem.CandidatePool` holds 18 ministers; 10 have art
(the nine shipped portfolios' ministers + the Defense PoC); **8 render the procedural placeholder** —
§1 below. (The sitting turn-0 Fed chair, Harriet Ellsworth, has no portrait and no call site asks for
one — a design question on the roadmap, not a gap.)

---

## 1. REQUEST — cabinet portraits: the batch, unblocked. THE VERDICT DESIGN IS WAITING FOR

> **`portrait_cabinet_defense_katarzyna_ekelund` (512×640 @2×) is on disk in `Portraits/`** (meta from
> the Portraits family, fresh GUID; SVG to the new `Portraits/Source/`). All three of Design's named
> gates are ANSWERED — this block is the deliverable back to Design:
>
> **1 — The pixel envelope (Design's missing fact):** all 16 pre-PoC portraits are **256×256 SQUARE,
> transparent-background flat busts** — textureType Default (not Sprite), spriteMode 0,
> alphaIsTransparency 0, compression ON, maxTextureSize 2048, drawn via
> `GUI.DrawTexture(ScaleMode.ScaleAndCrop)` into a **74:92 (≈0.804) roster rect** at ~3.2 line-heights.
> Consequences: 512×640 (0.80) matches the roster crop to **0.5%** — better than the existing squares,
> which lose ~20% of their width to the same crop; the resolution and size clear the importer as-is
> (2048 cap). **The envelope is ACCEPTED — no obstacle.**
>
> **2 — Oval vignette ownership: FRAME-OWNED, confirmed by what shipped.** At roster size the treatment
> is `ui_portrait_frame` — RECT brass over every portrait, art cropped underneath; the 16 bake NO
> vignette of any kind. The only oval vignette in the project lives in **`ui_portrait_frame_oval`'s
> alpha** (Design's own manifest row: "oval vignette in alpha"), delivered and unwired — the Canvas hero
> path. The PoC's baked "vignette" is an opaque painted background glow — rect-safe, verified by pixel
> inspection. **Bake the art, never the cutout.**
>
> **3 — The register side-by-side: ✅ PASSED 2026-08-26** (Elias's live Editor session): the painted
> plate belongs beside the existing register — Design's own named gate for the batch. **The batch is
> unblocked; this verdict travels in the send package.**

**The ask: the EIGHT remaining portraits, same envelope as the PoC** (rect roster framing + oval-vignette
hero treatment per §5's conventions; 1 source asset → 2 files, `.png` + `.svg`; destination
`Assets/Resources/Art/UI/Portraits/`; the game renders a procedural placeholder until each file lands, so
partial delivery is safe). The nine names are the shipped values in `CabinetSystem.CandidatePool`,
signed by Elias (R4-4 ruling R1, checked against real officeholders of the six countries on 2026-08-17 —
the collision search is `POLISIM_R4_4_PREREPORT.md` §4); every filename derives from them via the
standing rule (`portrait_cabinet_<portfolio>_<name_slug>`, `Slug()` per §5.4). All nine are ORIGINAL
FICTIONAL characters (working-discipline rule 9) — none may resemble any real person.

| filename | who they are (for the brush, not the label) | state |
|---|---|---|
| `portrait_cabinet_defense_katarzyna_ekelund` | Defense, Reformist. Wants procurement audited in the open — believes opaque contracting is where readiness actually dies. Sharp, forensic, unimpressed by braid. | ✅ delivered (the PoC) |
| `portrait_cabinet_defense_rafael_iwasaki` | Defense, Pragmatic. Capability-planning technocrat; buys what the threat assessment says, not what the parade needs. | outstanding |
| `portrait_cabinet_defense_gunnar_petrakis` | Defense, Traditionalist. Deterrence through visible strength; distrusts any reform that reads as weakness abroad. Weathered, formal. | outstanding |
| `portrait_cabinet_foreignaffairs_camille_adeyemi` | Foreign Affairs, Reformist. Institution-builder; thinks the multilateral table is where middle powers actually win. | outstanding |
| `portrait_cabinet_foreignaffairs_zofia_nakamura` | Foreign Affairs, Pragmatic. Interests-first dealmaker; judges every communiqué by what it moves, not what it says. | outstanding |
| `portrait_cabinet_foreignaffairs_aleksander_whitfield` | Foreign Affairs, Traditionalist. Alliances and protocol; believes predictability is a foreign policy, and a good one. | outstanding |
| `portrait_cabinet_education_yuki_dahlberg` | Education, Reformist. Curriculum modernizer; argues the system trains students for an economy that no longer exists. | outstanding |
| `portrait_cabinet_education_nadia_fitzgerald` | Education, Pragmatic. Evidence-based incrementalist; pilots before mandates, data before both. | outstanding |
| `portrait_cabinet_education_tobias_marchetti` | Education, Traditionalist. Standards and fundamentals; wary of every reform that trades rigor for relevance. | outstanding |

Coverage after delivery: 18 ministers + 7 Fed chairs = 25 portraits (17 on disk today). Import per §5.3's
treatment rules (`ImporterSettingsCheck` enforces); `DeliveredAssetCheck` gains 16 entries (8 × 2 files).

⚠ **Addendum, 2026-08-25 — flagged, not requested:** the roster this batch covers (3 portfolios × 3
philosophies) may grow as decision-density work proceeds — more portfolios means more candidates, and
therefore a larger batch later. Nothing to act on; named now so a future ask for "twelve" or "fifteen"
doesn't read as scope creep against this one.

Tracked in `MISSING_PREREQUISITES.md` §D1 (behind §S); the original request was **SENT 2026-08-17
(Elias)**; Design answered with the PoC.

---

## 2. REQUEST — the calendar panel board

**Status: OPEN — written 2026-08-26, not sent (the E2 convention: sending is Elias's).**

**Why this, why now.** The left-column calendar panel shipped 2026-08-24 code-derived, without a board —
the only v2.0-era surface besides the law browser that skipped Design, and playtest 2's verdict is that
it shows. The law browser's request taught the shape: state what exists precisely enough that Design
iterates rather than reinvents, and carry the data contract so the board is drawn against what the model
can actually mark.

### What exists (iterate, don't reinvent)

A weekday-aligned month grid — past days suffixed **" X"** in muted ink (a literal text suffix, the
utilitarian treatment §2.3 below asks about), today carried on a rounded card with the Political accent
wash, day numbers centered per cell; up to **four 5px dots** per day under the number, each tinted by its
marker's own SystemArea (fiscal hue for a release day, political for a division) — the cap is a hard
`min(count, 4)`. Below the grid, **"This Month"**: one ledger row per marker, measured date column
("12/31"-worst-case, the 2560 wrap lesson) then label. Above, the country-name-plus-year header preserved
from the old dashboard. The month page flips **instantly** on the boundary — grid, weekday alignment and
ledger regenerate with zero staleness (captured: `capfold_80a`/`80b`). Locale-honest throughout: the week
starts on the player culture's own `FirstDayOfWeek` and the month name is the culture's (the captures
show MÅN…SÖN and JANUARI). Chrome is entirely procedural (`RoundedCard`/`Rule`/`Pill`) — `ui_calendar_pad`
is the only calendar sprite that exists anywhere, so the board MAY spec sprites; nothing constrains it to
the procedural look.

### The data contract — FIXED, travels with this request

The board must not invent markers for events that aren't scheduled. The governing question, per source:
does a PENDING instance carry a computable date, and does a RESOLVED one retain one?

| Source | Future | Past | In the panel? |
|---|---|---|---|
| Fiscal year start | exact fixed (month, day), annually | exact | ✅ ONE merged marker — the same real date also opens the budget process and triggers the credit-rating review |
| Publication release days (the `PublishedStat` enum — twelve members at HEAD, six of them on a real release rule) | exact date arithmetic, no RNG | exact | ✅ every day of the shown month checked against every published stat |
| Pending bill countdowns (all 8 types) | exact: today + DaysRemaining | n/a (resolves into a division) | ✅ one marker per pending bill |
| Elections | exact (turn → epoch date) | no persisted log exists (the open ElectionRecord gap) | ✅ future only |
| Resolved divisions (24 retained) | n/a | exact stored dates | ✅ past only |
| Fired economic events | **unknowable** (probability roll) | exact date within the 6-turn fade window | ✅ past only, bounded |
| Cabinet decisions | **probability-only, no trace** | **no date stamp ever written** | ❌ excluded, both directions |
| Foreign-policy meetings | **probability-only, no trace** | same | ❌ excluded, both directions |

### Open design questions — the board's actual subject

1. **The X-mark.** The literal " X" suffix is utilitarian. Is there a period-true desk idiom — a
   crossed-off almanac feel — that says "spent day" without a text suffix?
2. **A marked day at grid size vs its detail in the ledger.** Dots say *that* and *whose area*; the
   ledger says *what*. Is that split right, and does four-dots-max read as "busy" or as noise?
3. **The month flip.** Currently instant. Worth a page-turn moment, or is instant the honest desk?
4. **Density.** The real worst case is captured, not hypothesized: USA October 1 lands the merged fiscal
   marker plus three annual publications on ONE day — four dots at the hard cap, one long merged ledger
   row (`capfold_83a`). What should a saturated day look like?
5. **One instrument or three stacked parts?** Header, grid, ledger — do they read as a single almanac
   page or as three components sharing a column?

### Constraints (real, from the shipped code)

- IMGUI; ledger grammar (`LedgerRow.Cell` measured columns); the **one-scroll rule** — the panel lives
  inside the left column's single scroll view.
- The left column is **43.2% of the window** (0.45 of the 0.96-margin area): **≈ 691 px at 1600×900,
  ≈ 1106 px at 2560×1440**. The pinned calendar pad and speed strip beneath are separate surfaces, out
  of this board's scope.
- Day-cell height is measured via `CalcHeight` against real content (a 2,004-violation guard lesson);
  the ledger date column is measured against the widest date, not a constant (the 2560 wrap lesson).
  Whatever the board draws, these stay measurements.
- **No new probability data** — the contract table above is the complete marker universe.

### ATTACHMENTS (§2) — verified on disk 2026-08-26, `..\PoliSim-captures\`

- `couple2s1600_02_statistics.png` (1600×900) / `couple2s2560_02_statistics.png` (2560×1440) — the
  panel today at both sizes: JANUARI 2029, X-marked past, today highlighted, dots on 5/12/30, the
  five-row ledger.
- `capfold_80a_calendar_month_end.png` / `capfold_80b_calendar_month_flip.png` (~1600×929) — the
  month-boundary pair: January 31, then February 1 regenerated.
- `capfold_83a_budget_pause_decisions.png` (~1600×929) — **the density case**: OKTOBER, four dots under
  day 1 at the cap, the merged "10/1 Fiscal year starts - budget process opens; credit rating reviewed"
  ledger row.

Attach these when this is actually sent — they're the evidence, not a description of it.

---

## 3. REQUEST — statistics graph weight and treatment

**Status: OPEN — written 2026-08-26, not sent (E2). ⚠ SPECIFICATION ONLY — NO SPRITE DELIVERABLES
EXPECTED. This is a visual-treatment ruling the way NO NEW SPRITES was a manifest rule: whatever is
specified, we implement as pixel rules in our own rasterizer; no art files are being requested.**

**The finding.** Playtest 2: the statistics graphs' series lines read as hairlines, at 2560 especially.
The current weight is deliberately left untouched as the evidence
(`couple2s2560_02a_statistics_domestic_deep` — four stacked graphs, the thinness plainly visible).

### The real raster machinery the ruling lands in (this shapes the answer)

- **Full graphs draw into a FIXED 300×90 texture** and display via `StretchToFill` into the layout rect:
  **≈ 810 px wide at 1600 (≈ 2.7× horizontal stretch), ≈ 1250 px at 2560 (≈ 4.2×)**; display height is
  `clamp(7.5% of screen height, 50..90)` → **67 px at 1600, 90 px at 2560 — vertically 1:1 at 2560**,
  which is why a 2-buffer-px line is 2 device px tall there: the hairline.
- **Sparklines build native-resolution buffers** (width/height from the rect) through the same
  Bresenham. A thickness rule must speak to both contexts — buffer px per context, or one
  resolution-relative rule we translate.
- The 300×90 buffer size is our constant, not a law — the ruling MAY say "raise the buffer to display
  resolution" and we implement that too.
- The maths is extracted and regression-tested (`BuildSparklinePixels`, pure, hammered at **336 width ×
  height × series-shape combinations**) — pixel-rule changes land under existing tests.

### The ask — three roles, one ruling

Line weight for the **primary history series** (currently 2 buffer px, solid), stated so all three roles
stay differentiated, not one number:

1. **History** — solid, the recorded data.
2. **Projection** — the one-turn estimate: currently lighter alpha AND dashed (every 3rd Bresenham step
   skipped), drawn on the most-recent page only. Must keep reading as "estimate, not committed" at
   whatever new weight.
3. **Threshold/reference** — NAIRU, the "comfortable" debt line: warm amber (`Caution`) with a
   right-aligned label riding the line. Must keep reading as "reference marker, not data."

### While ruling — do the existing behaviors' expressions hold at the new weight?

These exist and work; the question is whether their current visual expression should move with the
weight or stay:
- the **direction-aware green/red** delta convention (header % per graph);
- **release-point markers** and the **PRELIMINARY badge + lag dating** on published series;
- **preliminary-vs-revised as frame style** — dashed 1px frame while provisional, solid once revised (a
  second, separate channel from the projection dashing — both visible in
  `couple2s1600_02a_statistics_domestic_deep`).

### ATTACHMENTS (§3) — verified on disk 2026-08-26, `..\PoliSim-captures\`

- `couple2s2560_02a_statistics_domestic_deep.png` (2560×1440) — four stacked graphs: the hairline
  verdict's own evidence, plus the dashed next-year estimate, the amber "comfortable" threshold with its
  riding label, and the green/red deltas.
- `couple2s1600_02a_statistics_domestic_deep.png` (1600×900) — the As-published GDP graph: PRELIMINARY
  badge, release-point markers, dashed revision frame, the 1yr/5yr/All pager.
- `item5sweden_07d_politics_federalreserve.png` (1600×900) — the full-width neutral interest-rate graph,
  a third weight context (no green/red judgment by design).

Attach these when this is actually sent — they're the evidence, not a description of it.

---

## 4. Costed, NOT requestable yet — so the next ask is not a surprise (not sent)

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
  promoted to nodes (`MISSING_PREREQUISITES.md` §E4): joins the batch after §1 lands.
- **A portrait for the sitting turn-0 Fed chair** (Harriet Ellsworth) only if the roadmap's
  sitting-chair-row question resolves toward one.
- **The roster beyond nine** — §1's addendum.

Nothing else. The two built Canvas screens (1f the selector, 1g signing) needed nine pass-3 sprites and
consumed six of them; the three paper-canvas button states are delivered and unwired.

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
- **Cabinet portraits beyond §1's batch.** A larger roster later is a fresh request, not an extension.
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

⚠ **TWO CATEGORIES ARE EXEMPT, and this is not a lapse in the convention.** **Country flags** and
**party emblems** (`emblem_party_*`) are authored in their own real colours — a flag is not tintable.
Any new art in those two categories stays full-colour; everything else stays white-on-alpha, **including
`mark_party_*`**, which is tinted from seed data at draw time (a rebrand is a data edit, never a
redelivery). Getting this backwards in either direction produces art that cannot be used.

**Icon authoring.** Renders small — a stat icon draws at **22px** on the contextual stat row: avoid thin
strokes and interior detail that will disappear. SVG source 24×24 geometry, `currentColor` fill, simple
primitives, mirroring the existing packs.

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
| `alphaIsTransparency` | **On** (`1`) | Shape lives in alpha |
| sRGB | On (`1`) | |
| Filter Mode | `Bilinear` (`1`) | |
| Compression | **None** (`textureCompression: 0`) for white-on-alpha and tiling art; kept (`1`) for full-colour flags/portraits/emblems by ruling | Block compression mangles white-on-alpha at icon sizes; on full-colour art it showed no visible damage at display size |
| Mipmaps | **Off** (`enableMipMap: 0`) | UI sprites never minify — `ImporterSettingsCheck` errors on any |
| Wrap Mode | **Clamp** (`wrapU/V/W: 1`) | Correct for every sprite drawn once. **Tiling art needs `Repeat`** |
| `isReadable` | `1` for chrome (`UiPalette.GetTintedChrome` reads pixels back), `0` for icons | copied across the class boundary it cost an entire UI |

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
`Chrome/`; if no, from `Flags/`; if it tiles, the tile. `ImporterSettingsCheck` enumerates every PNG
under `Assets/Resources/Art/UI/`, classifies by treatment, and asserts against the **imported texture** —
the meta is the claim, the texture is the fact. The rulings behind these rows are `COMPLETED.md` §33.

### 5.4 Filename manifest and the naming rule

**Every filename derives from a real enum value in the code.** The game resolves art at runtime by
building the string from the enum, so a filename that does not match an enum resolves to null and draws
nothing.

| Pattern | Derived from | Example |
|---|---|---|
| `icon_area_<systemarea>` | `UiPalette.SystemArea`, lowercased | `icon_area_sovereignwealth` |
| `icon_stat_<statname>` | the displayable stat (`StatNodeId` via `GetIconName`), lowercased | `icon_stat_laborforceparticipationrate` |
| `icon_nav_<tab>` | `ConsolidatedTab`, lowercased — four of six; Budget and Politics reuse area icons by design | `icon_nav_policylaws` |
| `portrait_cabinet_<portfolio>_<name_slug>` | `CabinetPortfolio` + `IconLibrary.Slug(name)` | `portrait_cabinet_interiorjustice_amara_oseibonsu` |
| `portrait_fedchair_<name_slug>` | `Slug(name)` | `portrait_fedchair_priya_anand` |
| `emblem_party_<archetype>` | `PartyArchetype`, lowercased | `emblem_party_centristcoalition` |
| `mark_party_<country>_<party>` | the party seed's mark name | `mark_party_us_lib` |
| `flag_country_<countryid>` | `CountryId`, lowercased | `flag_country_poland` |
| `ui_<control>_<state>` | control + state, one sprite per state | `ui_btn_brass_canvas_hover` |

`Slug()` = lowercase, drop every non-letter, spaces → underscores. "Wei-Lin Tanaka" → `weilin_tanaka`.

⚠ **Enumerate the DISPLAY enum, not the storage struct.** The macro pack derived its stat list from
`EconomyState`'s fields — the right instinct — and still missed `InterestRate`, which lives on
`CurrencyZone`. It was structurally invisible to that derivation while being a headline figure on two
screens.

⚠ **Everything must live under `Assets/Resources/`.** `IconLibrary` uses `Resources.Load`, not
`AssetDatabase`. The flags and emblems once sat outside `Resources/` for weeks, fully delivered and
**unreachable by the game the entire time**. An asset's status has two parts, **delivered** and
**reachable**, and only the first is visible from the inbox.

**1 source asset → 2 files** (`.png` + `.svg`), as in every previous pack. No zip at the project root
means every delivery is imported and archived — a zip appearing there is the signal something is
unfinished, and `DeliveredAssetCheck` enforces it.
