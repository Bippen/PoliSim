# Claude Design asset request — PoliSim

**Status: §1E CLOSED (all five) — verified 2026-08-11 by per-item enumeration against disk, see §1F.2.
The earlier "independently verified by zero diffs" claim was withdrawn: identity is not compliance.
§1F ANSWERED, DELIVERED AND IMPORTED (2026-08-11); batch correctly not started.
⚠ OPEN: eight SVG sources unfiled — `DeliveredAssetCheck` is RED on them by design.**
**Date:** 2026-08-10.

➡ **START AT [§1E](#1e-pass-3-follow-ups--five-import-blockers-2026-08-10).** Pass 3 answered all nine
items of the §1D revision request — seven accepted as raised, two amended with reasoning better than the
request had. What remains is **five delivery-side items that stop delivered assets from importing**:
a prefix that contradicts §3.1's tint rule, a second namespace inside `Chrome/`, two sprite sheets that
need an import recipe §3 does not carry, SVG-only delivery where every previous pass shipped PNGs, and
D1's own agreed draft carrier which has no PNG at all. None disputes a design decision.

**§1D is retained below as the record** of what was raised and how it was answered — read it for the
reasoning behind D4's hue cap and D7's resort ladder, both of which are now implemented.

⚠ *(RECONCILED 2026-08-12: the three repo-side mirrors — `REVISION_REQUEST_PASS3.md`,
`FOLLOWUPS_PASS3.md`, `PARTY_EMBLEM_QUESTION.md` — are RETIRED and deleted. Each mirrored a section of
this document whose request has been answered, delivered and imported (§1D answered by pass 3; §1E
closed per this document's own status line; §1F answered and imported per §1F.2), and each carried a
stale "OPEN" status header of its own — precisely the two-documents-one-fact drift the consolidation
rule exists to remove. The Design-project uploads are outside this repo and unaffected. The original
note follows.)* **§1D is mirrored at `uploads/REVISION_REQUEST_PASS3.md` in the Design project, and at
`REVISION_REQUEST_PASS3.md` in this repo, so it arrives as a new file rather than an in-place overwrite
of a document a week old.** **This document is the source of truth**; the mirror is generated from §1D by
the command recorded at the top of the mirror, so regenerating it is how they are kept from drifting.
Edit §1D here, then regenerate — never edit the mirror.

**What is settled.** Passes 1 and 2 received, verified and imported 2026-08-03: 41 sprites + 7 SVG
sources + palette + `DIRECTION.md` + `CANVAS_SPEC.md`, 52/52 resolving through `Resources.Load`. Pass 2
answered every item in §1B. **The delivered specification is reproduced in §1C** — read that before
implementing anything. The chrome is wired but **not yet confirmed in a live Editor**, and nothing may be
layered on it until it is.

**What is ours, not theirs:** the draft-amber sign-off (§1B.5), and wiring any of it.

**Supersedes:** `CLAUDE_DESIGN_ASSET_REQUEST_5E.md`, `_UI_CHROME.md`, `_UI_CHROME_ADDENDUM.md` and
`_MACRO.md` — all four fully delivered, imported and verified in production. Their contents are recorded
in `COMPLETED.md` §8 and the originals remain in git history. **This is the single standing asset request
document**; new requests append here rather than starting a new file.

Fifth request in this project. **The technical conventions in §3–§4 are unchanged and still binding.**

⚠ **This header was itself the failure it exists to catch.** Between 2026-08-03 and 2026-08-10 it read
*"CHROME COMPLETE, NOTHING OUTSTANDING WITH DESIGN"* while §1D sat below it carrying four blockers, and
Elias could not find the revision request because the document's most-read line denied there was one.
A status line is a cached value with no expiry — §6's rule, landing on the document that records §6.

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
CrimeJustice, Sectors, Infrastructure, SovereignWealth, Global, Neutral — hex values in §3.0). They
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

### What already exists — 136 sprites. DO NOT REDRAW.

*84 → 96 → 125 → 136 across three days. **§6 says to re-derive this from the filesystem rather than trust
it, and it has been right every single time.** A count in prose is a cached value with no expiry.*

| Category | Count | Status |
|---|---|---|
| Stat icons (`icon_stat_*`, `icon_trend_*`, `badge_*`, `icon_release_marker`) | 43 | wired |
| Area + nav icons (`icon_area_*` ×10, `icon_nav_*` ×4) | 14 | wired |
| Portraits (9 cabinet + 7 Fed chair) | 16 | wired |
| **v2.0 chrome, passes 1 + 2** | **41** | **imported, 52/52 resolving, not yet wired to any control** |
| Chrome, pre-v2.0 | 11 | 3 wired; **all 11 now superseded** by the v2.0 passes |
| Background texture (`menu_pattern_tile`) | 1 | wired |
| Country flags (`flag_country_*`) | 6 | wired |
| Party emblems (`emblem_party_*`) | 4 | wired |

*Pre-v2.0 chrome is 11 rather than 12 because pass 1's `ui_slider_track` took over that filename. Pass 2's
scrollbars supersede the last four that had no v2.0 equivalent, so the pre-v2.0 set is now entirely dead
weight — retained until the v2.0 chrome is actually wired, then removable in one pass.*

Reskinning any of these in the new idiom is in scope. **Inventing replacements for art that already
exists is not.** `ui_button_disabled` is a special case: IMGUI has no disabled style state, so it becomes
usable only once its screen is on Canvas.

---

## 1B. CHROME PASS 2 — ✅ ANSWERED IN FULL, 2026-08-03

*Retained as written rather than deleted, because the request and its answer read as a pair. Every item
below was delivered: scrollbars with the arrow-button call made explicitly, the chip judgment (which came
back as "not a pill" — see §1C.5), the Pagella stamp re-cut, and the missing `DIRECTION.md`. §1B.3's
question is answered too: the chrome pack was complete in itself, and `CANVAS_SPEC.md` covers §3.3.*

### The original request follows

Pass 1 arrived complete against its own manifest: 30 PNGs, every one matching its stated dimensions,
all alpha-correct, all 41 sprites in the folder resolving through `Resources.Load`. The 9-slice insets,
the baked-shadow reasoning and the tint rule are all usable as delivered. **This is a short follow-up,
not a rework.**

Two components are **unaccounted for** — not delivered, and not listed under "Not in this pack (by
design)" either, which is why they read as gaps rather than decisions.

### 1B.1 Scrollbars — the significant one

⚠ **Every data screen in the game scrolls. There are 16 scroll views**: the left column, all six tabs,
both of the Budget screen's columns, and every Policy/Laws sub-screen. Without sprites they fall back to
Unity's built-in grey scrollbars sitting against baked paper — **on the Budget screen, three of them at
once, on the densest surface in the game.** It is the most visible way the illusion can break.

Pass 1's own `ui_slider_track` / `ui_slider_knob` are the right family to derive from, but a scrollbar is
a different control and IMGUI styles it separately.

| file | role | notes |
|---|---|---|
| `ui_scrollbar_track_v` | vertical channel | 9-slice, stretches on Y only |
| `ui_scrollbar_thumb_v` | vertical thumb | 9-slice, stretches on Y only |
| `ui_scrollbar_track_h` | horizontal channel | 9-slice, stretches on X only |
| `ui_scrollbar_thumb_h` | horizontal thumb | 9-slice, stretches on X only |

- **Hover and pressed variants for the two thumbs** if the idiom wants them (`_hover`, `_pressed`). Tracks
  need one state only.
- ⚠ **IMGUI also draws scrollbar UP and DOWN BUTTONS from the skin.** Unity's defaults will show through
  as grey arrows even once the track and thumb are replaced. Either supply them, or say explicitly that
  they should be styled to nothing — **please make that call rather than leaving it, because "nothing" is
  a legitimate answer here and silence is not.**
- The channel reads best as recessed *into* the paper or desk rather than laid on top of it; the thumb is
  the natural place for brass. That is a suggestion, not a spec.

*The old chrome pack shipped four scrollbar sprites which pass 1 supersedes. Do not derive from them —
they are the dark-dashboard idiom, and they were never wired.*

### 1B.2 Badge / chip / pill

`PoliSimWidgets.Badge` and `PoliSimTheme.Pill`. Five call sites, but far higher visual frequency than that
suggests: **the signed delta pill rides on every stat tile**, and stat tiles fill the dashboard and the
whole Statistics tab. `DecisionCard`'s urgency chip is the same shape.

| file | role | colour |
|---|---|---|
| `ui_chip` | small 9-sliceable pill, text sits inside it | **WoA** — tinted per use |

⚠ **This one is genuinely optional, and we would rather have your judgment than a sprite.** It works
procedurally today (Unity 6's `GUI.DrawTexture` takes real corner radii, which is how `PoliSimTheme.Pill`
draws it), so the question is whether a procedurally-rounded pill can sit on aged paper without looking
like screen UI that wandered in. **If the answer is that it should not be a pill at all in this idiom —
that a delta belongs as inked text, a rule, or a small stamp instead — say that and skip the sprite.**

Two load-bearing constraints on whatever replaces it, both non-negotiable in FUNCTION however it looks:

- **Direction-aware green/red.** A delta is coloured by whether the change is *good*, not by whether the
  number rose. Falling unemployment is green. So the mark must be tintable, and legible in both.
- **Amber means drafted-not-enacted**, and only that.

### 1B.3 One question, because it changes what we build next

The manifest is headed "UI chrome" and "Pass 1 of 1". Brief §3.3 also asked for the **Canvas path** —
sprite sheets, prefab-shaped component specs with their states, transition timings — for the eight
narrative screens (country selector, election results, Fed chair selection, cabinet decision, foreign
policy meeting, bill vote, budget signing, interrupt banner). What arrived on that side is
`ui_scrim_takeover` and its 180 ms timing.

**Is "Pass 1 of 1" saying the chrome pack is complete in itself, or that no further pass is planned?** If
the former, this is simply the next request and nothing is wrong. If the latter, the Canvas screens have
chrome to reuse but no specification, and that work is blocked.

### 1B.4 Three corrections and a missing document

- ✅ **`menu_pattern_tile` is imported and wired.** The manifest's closing line says it is "still pending
  import on your side (roadmap)". It is not — it resolves at 256×256 with wrap Repeat and draws the
  country-selector background. The roadmap line you read was stale. **Recorded because it is the same
  cached-status failure this document keeps catching, arriving from the other direction this time.**
- ⚠ **The stamp SVGs specify Georgia**, which is a licensed Microsoft font. The delivered PNGs are raster
  so nothing ships and there is no problem today — but the sources cannot be regenerated by anyone without
  that font. **Please re-cut them in TeX Gyre Pagella**, which the game already ships (§1, typography) and
  which is a Palatino clone rather than a Georgia one, so expect to re-space.
- ⚠ **`ui_slider_track` collides by name with the old chrome pack's sprite.** Ours won and supersedes it,
  which is correct — flagged only so the collision is not a surprise if you regenerate.
- ❓ **The manifest cites `[B1]`, `[B5]`, `[B8]`, `[W1]` and `[W2]`** as though a companion document
  exists. It is not in the zip. Those tags look like the "where Suzerain's approach would NOT work here"
  analysis §1 asked for — **which is the part we most wanted.** Please resend it, in the pack.

### 1B.5 Held, awaiting sign-off — not a request

`polisim_palette.json` is imported but **no value is wired yet**. Its flagged decision is a real find and
Elias's to make: draft amber and the Political area hue are literally the same hex today
(`PoliSimTheme.Draft = Caution = #E0B341`, `SystemArea.Political = #E0B341`), so two load-bearing
behaviours have been sharing a colour. The pack separates them. Nothing further is needed from Design on
this; it is recorded here so the hold is visible rather than looking like an oversight.

---

## 1C. DELIVERED SPECIFICATION — pass 2, 2026-08-03

**Pass 2 answered every item in §1B.** It shipped 11 new sprites, re-cut the three stamps in Pagella, and
brought back the two documents that were missing: `DIRECTION.md` (the `[Wn]`/`[Bn]` companion) and
`CANVAS_SPEC.md` (the §3.3 Canvas path).

⚠ **Both are reproduced below rather than referenced, because `AssetPackArchive/` is gitignored.** The
zips never enter the repo, so a document that lives only inside one is lost to anyone who clones. This is
the delivered-vs-reachable lesson applied to prose instead of sprites.

### 1C.1 The thesis, and where the idiom was deliberately refused

> *"A ledger, not a decree. Suzerain frames ONE document at a time and lets ornament spend freely around
> it. PoliSim shows dozens of live figures at once and redraws them at 3× speed. The idiom is adopted at
> the PERIMETER and refused at the ROW."*

| tag | refused | why |
|---|---|---|
| W1 | ornament per row | plate-per-document costs 40–60px/item; Budget has ~40 rows. Ornament concentrates on panel frames (≤6/screen); rows get hairline `ui_pixel` rules |
| W2 | texture under live digits | grain behind a redrawing numeral shimmers. Numeral plates ≤2% grain; `ui_grain_tile` **never** behind live digits |
| W3 | scene transitions everywhere | tab flips stay instant — a clerk flipping folders. Motion is rationed to the Canvas moments |
| W4 | a mood palette | Suzerain runs 2–3 tones; PoliSim's eleven hues are **data infrastructure**. Aged, not reduced — distinguishable at 12px swatch |
| W5 | the prose register | their paper carries letters; ours carries figures. Bulletins, ledgers, forms, tally sheets |
| W6 | period as age | the game plays in 2026. The 1950s is the institution's graphic language, not artifact age. "Ministry fresh print", stock `#F0E7D8`, weathering reserved for archival material |

**Also not carried across:** scene illustrations · per-row paper skeuomorphs (stacked-sheet offsets, wax
seals on data cards) · handwriting as a data carrier · page-turn transitions · heavy vignettes over dense
panels · documents half-overlapping data panels (impossible by measurement).

**Carried across:** the desk as ground · paper as the surface of record · folder tabs as navigation ·
ornate framing reserved for portraits and consequential documents · stamps and printed badges as state ·
the full-screen document as the shape of every consequential moment.

### 1C.2 Where each load-bearing behaviour lands

| tag | behaviour | how the chrome satisfies it |
|---|---|---|
| B1 | amber draft cue | `ui_stamp_draft` + `ui_hatch_draft` tinted draftAmber — **may change form, may not disappear** |
| B2 | direction-aware green/red | good `#3E8A5F` / bad `#9C4238`; deltas are inked text coloured by goodness, not sign |
| B3 | MoneyUnit | register rule: unit always named beside the figure; no art renders currency without one |
| B4 | shrink, never truncate | plates are 9-slice with near-flat centres — **no fixed-size text plate anywhere in the pack** |
| B5 | stable control layout | `ui_btn_disabled`, `ui_slider_knob_disabled`, N/A chip — disabled is rendered, never omitted |
| B6 | published vs live | published = printed bulletin (solid frame + ref period + date + badge chip); live = desk reading (dashed rule, unbadged) |
| B7 | six consolidated tabs | folder tabs + `ui_tab_spine` tinted per area |
| B8 | always-visible interrupt | `ui_banner_hold` is desk-mounted (dark set) so it survives Budget full-screen **and** the Canvas scrim |
| B9 | legend ↔ chart colour | legend swatches print in the chart's own ink; emblems sit BESIDE swatches, never instead |
| B10 | lining figures | art carries no numerals of its own |
| B11 | `U+2212` / `U+00B1` | true minus everywhere in art copy |

### 1C.3 The hand-off envelope — every consequential moment

Runs **from the IMGUI side**, per the render-order spike.

| t (ms) | action | side |
|---|---|---|
| 0 | input locks; sim clock holds | IMGUI |
| 0–180 | `ui_scrim_takeover` fades 0→100%, opacity only, ease-out quad | IMGUI |
| 180–240 | hold at 85%; IMGUI suppressed, Canvas enabled behind the wash | swap |
| 240–500 | document entrance: rise 24px, settle −0.6°→0°, ease-out cubic | Canvas |
| 580–700 | stamp/seal thunk: scale 1.15→1.0, 120ms | Canvas |
| 700+ | controls fade in last | Canvas |

Exit reversed and faster: controls lock → document drops 16px + fades 200ms → Canvas disabled → IMGUI
redraws under scrim → scrim lifts 240ms. **Round trip ≤1.2s.** `ui_banner_hold` survives the whole
sequence.

### 1C.4 Per-screen Canvas specs

- **Country selector** — `CountryFolder` ×6. Folder card, `ui_tab_spine` tinted per country's mapped area
  hue, flags (full-colour exemption), `menu_pattern_tile` ground. States: normal · hover (lift −8px, 60ms)
  · pressed (scale 0.985) · selected (folder opens, brief slides in, 320ms) · disabled (`#B9A886`, no lift).
- **Election results** — bulletin lands → seats fill by declaration wave 1200ms in party-ink order
  (procedural dots, rule 10) → swing figures count up 600ms → verdict stamp thunks last.
- **Bill vote / budget signing** — division bar fills 500ms (ayes from left, noes from right, meeting at
  the threshold tick) → CARRIED/REJECTED stamp. Signing: pen scratch 400ms → `ui_seal_official` drops
  1.3→1.0 over 140ms + 6px settle shake.
- ⚠ **Fed chair · cabinet decision · foreign policy meeting — THE DUAL-SITING ANSWER.** Standalone
  (`drawOwnFrame: true`) gets its own plate + `ui_frame_ornate` + title band, oval portraits at hero size,
  full envelope entrance. Embedded (`false`) gets **interior furniture only** — no plate, no frame, no
  title band, no outer shadow, rect portraits at roster size, and **no entrance animation**, since it is
  already on screen inside Decisions. **Asset consequence: frame, title band and plate are separate
  sprites from interior furniture, so the embedded path simply skips them.** This is the constraint §1
  flagged, resolved.
- **Interrupt banner** — dark set, desk-mounted, amber lamp dot pulsing 60↔100% on a 1.2s sine, always
  naming the resolving screen.
- **Scrim** — one radial-vignette sprite stretched full-screen; **opacity is the only animated property**,
  and it is never a dim layer under a partial popup, because those do not exist.

### 1C.5 Two implementation instructions that came back with the art

1. ⚠ **Scrollbar arrow buttons are styled to NOTHING** — "a ledger has no arrow furniture". Point
   `upButton`/`downButton`/`leftButton`/`rightButton` at `ui_scrollbar_button_none` **and** set
   `fixedWidth = fixedHeight = 0` with zero margins. **Both are required — the sprite alone still leaves
   IMGUI reserving the space.**
2. ⚠ **THE SIGNED DELTA IS NOT A PILL IN THIS IDIOM.** On paper a delta is inked text, not a lozenge.
   **Retire `PoliSimTheme.Pill` at the stat-tile delta call sites**; keep `ui_chip` / `ui_chip_outline` for
   `PoliSimWidgets.Badge` sites — published/revised/urgency — where a printed chip is period-correct. This
   is the judgment §1B.2 asked for instead of a sprite, and it is a **code** change, not an art one.

---

## 1D. REVISION REQUEST — the eight screen boards, 2026-08-10

**The boards are good and most of this document is now answered by them.** The surface ladder, the ink
set, the rule-weight vocabulary and the letterspaced-caps section head give the idiom a spine that
survives at data density. Two things in particular are better than what was asked for:

- **The in-row slider.** Standing value as a hard `2px` tick, draft value as the knob, and the span
  between them hatched in `#BE8A00` — **in both directions**, so a cut reads as clearly as a rise. That
  is behaviour 1 rendered as *distance* rather than as decoration, and it is the strongest single idea
  in the pack.
- **The dual-siting answer.** Unambiguous, and it resolves the constraint §1 flagged: plate, frame and
  title band ship as separate sprites from the interior furniture, so the embedded path skips three draw
  calls rather than needing a second design.

Both measured architectural constraints hold. No board interleaves the two renderers, and 1e states the
render-order finding in its own words. **§1D.4 proposes a wording fix to one sentence that contradicts
this**, and it is a text fix rather than a design change.

What follows is **nine items that are Design's calls, not ours to resolve unilaterally.** Four block
implementation. Each is stated with its evidence rather than as a preference, and two arrive with a
proposed answer rather than only a question.

### 1D.1 — The four blockers

#### D1 ⛔ The draft marker glyph does not exist in any font the game ships

The boards make `✎` (U+270F) the primary carrier of **behaviour 1**. It appears on all four IMGUI boards:
the `✎ 3 DRAFTS OPEN` header, the `STANDING ✎ DRAFT` column header, every drafted row (`22,0% ✎ 24,5%`),
both ledger subtotals, the bill rail's three figure rows, and the `✎ DRAFT — NOT ENACTED` stamp.

Measured against the three fonts actually in `Assets/Resources/Art/UI/Fonts/`, by reading their `cmap`
tables directly:

| glyph | Pagella Regular | Pagella Bold | Courier Prime |
|---|---|---|---|
| `U+270F` ✎ pencil | **absent** | **absent** | **absent** |
| `U+270E` ✎ pencil (lower) | **absent** | **absent** | **absent** |
| `U+26A0` ⚠ warning sign | **absent** | **absent** | **absent** |
| `U+25C4` ◄ pointer | **absent** | **absent** | **absent** |
| `U+25B2` ▲ / `U+25BC` ▼ | present | present | **absent** |
| `U+2212` − / `U+00B1` ± | present | present | present |

This is **behaviour 11's failure mode landing on top of behaviour 1** — *"a font or glyph set lacking it
renders a blank box on a readout the player is meant to trust."* Shipped as drawn, every draft marker in
the game renders as `□`, on the one cue that may change form but may not become nothing.

Behaviour 11 itself still holds: `U+2212` and `U+00B1` are present everywhere and the boards use them
correctly. The regression is only in glyphs introduced after that audit.

✅ **PROPOSED RESOLUTION — please confirm or override.** `icon_pencil_draft.svg` already shipped in pass 1
and is still unwired. **Make the draft carrier that sprite rather than a text glyph**: white-on-alpha,
tinted `#BE8A00`, drawn inline before the draft figure. This costs nothing new, removes the font
dependency permanently, and is consistent with the pack's own tint rule. If a typographic mark is wanted
instead, it must be one Pagella actually carries — but the sprite looks like the better answer.

Two riders either way: **▲/▼ are safe in Pagella but must never be set in Courier Prime**, and `⚠` needs
replacing wherever it appears in shipped UI copy (it is fine in board annotations).

#### D2 ⛔ The division bar depicts a quantity the simulation does not compute

The boards' central legislative visual is a seat headcount: `PASSES · 186 – 164`, a bar filled 53.1% aye
against nay meeting at a threshold tick, and `aye 186 · 176 to pass · margin 10`. It carries 1b's bill
rail, 1c's division records (`212 – 138`) and 1g's `DIVISION No. 215`.

`ParliamentSystem.GetSeatWeightedAlignment` documents the opposite, in its own comment:

> *"Worth understanding before displaying it: this is NOT a headcount, and there is no seats-based
> majority threshold anywhere in this model. Each party contributes its seat share…"*

What the model does produce, and what `DrawBillLiveEstimate` renders across all five screens today, is a
direction label, a WOULD PASS / WOULD FAIL verdict, and a **diverging lean bar** of that alignment. That
renderer's comment records the choice as deliberate:

> *"Deliberately not `PoliSimWidgets.SupportBar` — this model has no seats-based majority for it to draw…
> the Parliament card already shipped that exact bug once."*

So the boards ask for a re-run of a bug this codebase already found, fixed and wrote down. **The same
applies to 1b's per-row `VOTES` column** (`−9`, `+6`, `−12`, `+4`, `N/A`) — per-instrument legislative
support does not exist at any granularity; bills are scored whole.

**Two ways out, and the choice is Design's:** give the diverging alignment a period-correct treatment —
it is a real quantity, it is what the vote turns on, and a ledger has perfectly good ways to draw a lean
— or tell us the headcount is worth building in the simulation. The first is cheap and honest. The
second is a simulation change, not a UI one, and would need Elias's sign-off separately.

Note that **seat counts themselves are real** (`Country.ParliamentSeats`), so 1c's government/opposition
bar and all of 1h's election figures are fine as drawn. It is specifically the *per-bill division* and
the *per-instrument vote* that have nothing behind them.

#### D3 ⛔ The density board tested half the density

1b is captioned *"the density stress test: 19 live line items"* and draws 11 tax rows and 8 spending
rows at `44px`. The actual data model:

| | board 1b | actual |
|---|---|---|
| `TaxType` | 11 | **13** |
| `SpendingCategory` | 8 | **29** |
| `WelfareProgramType` | — | 6 |
| `InfrastructureType` | — | 4 |

W1's own argument used the right figure — *"Budget has ~40 rows"* — and then the board drew nineteen.
29 spending rows at 44px is **1276px of content in a column roughly 800px tall.**

Three things follow, all needing a decision:

1. **Row height comes down, or the column scrolls.** Both are legitimate; they produce different
   designs.
2. **No board draws a scrollbar anywhere** — despite §1B.1 establishing 16 scroll views and pass 2
   delivering six sprites for them. The thumb's width, its inset from the paper edge, and whether the
   channel is recessed into paper or desk are unspecified on every screen that scrolls.
3. **1b shows revenue and appropriations side by side while the sub-tab row highlights `Tax`.** The
   implementation shows one category at a time (`DrawBudgetProcessTab` → `BudgetProcessCategory`: Tax,
   Spending, Welfare, Infrastructure, SWF). Either Budget is meant to abandon its sub-tabs for a
   permanent two-column ledger — which the row arithmetic above makes impossible — or the board is a
   composite. Please say which.

#### D4 ⛔ Four data visualisations were never aged

`UiPalette.GetCategoricalColor` is still `Color.HSVToRGB(hue, 0.65f, 0.9f)` walking a golden angle —
saturated screen colour, untouched by the v2.0 pass. It draws:

| call site | series length |
|---|---|
| `HemicycleRenderer` seats **and legend** | 4 parties |
| sector employment pie | 8 sectors |
| **spending pie** | **29 categories** |
| tax revenue pie | 13 types |

The boards draw hemicycle seats in aged inks (`#9C4238`, `#62579F`, `#A8842E`, `#35619E`), so the design
**assumes an aged categorical set exists.** None was delivered — `polisim_palette.json` covers eleven
area hues and four semantic colours and is silent on categorical series.

This is Elias's eleven-hue ruling one level down, and by its own stated reasoning:

> *"colour is load-bearing wherever it also keys a data visualisation… A seal, emblem or typographic mark
> cannot substitute there, because the mark is not what the chart is drawn in."*

The eleven were kept as a floor for exactly this. These four charts are the same case — and **29 mutually
distinguishable aged hues is a materially harder problem than eleven**, which is why it needs Design
rather than a runtime desaturation we invent. Left as-is, the Statistics tab renders a bright HSV
rainbow on aged paper: a more visible break than the grey scrollbars §1B.1 worried about.

If 29 distinguishable aged hues is not achievable — a defensible answer — then say so, and the spending
pie needs a different chart form rather than a worse palette.

### 1D.2 — Five smaller items

#### D5 ⚠ Party inks are the area inks, on the same screens

| party | ink | already means |
|---|---|---|
| National Labor Front | `#9C4238` | CrimeJustice — **and semantic `bad`** |
| Reform Union | `#62579F` | Sectors |
| Agrarian League | `#A8842E` | **Political** |
| Centrist Coalition | `#35619E` | Fiscal |

On the Politics tab the tab's own ink is `#A8842E` and the Agrarian League swatch is `#A8842E`. On 1c the
`majority of 1` warning prints `#9C4238`, the same ink as the largest party's seats two rows above.

This is the defect §1B.5 just resolved for draft amber and Political, arriving from another direction:
two load-bearing meanings sharing one hex. Behaviour 9 requires a legend swatch to match *its own arc*;
it does not require the arc to match an unrelated area accent.

Related and cosmetic, but it has to be re-keyed: **the board's party names are invented.**
`PartyArchetype` is `ProgressiveAlliance · ConservativeUnion · CentristCoalition · NationalistFront`, and
`emblem_party_*` sprites exist for those four. Only Centrist Coalition matches.

#### D6 ⚠ A third hue tint is used but not delivered

Inactive tab swatches use a knocked-back tint that is in neither the `ink` nor the `lifted` table and not
in `polisim_palette.json`. Six of the eleven appear on the boards — Fiscal `#3D6494`, Political `#96762A`,
Labor `#A2653E`, CrimeJustice `#8E4A40`, Sectors `#5B5187`, Global `#4E7291` — and five do not, with no
stated derivation to compute them from. **Either the five missing values, or the rule that produces
them.**

#### D7 ⚠ Behaviour 4 is satisfied in the sprites and broken in the layout

§1C.2 is right that no *sprite* is a fixed text plate. At the **layout** level, 1b fixes the instrument-
name column at `168px` and clips overflow, and the stat-tile labels are set not to wrap.

Program names are longer than that at these sizes, and cabinet-minister names are generated at runtime.
*"A clipped number is a plausible wrong number"* applies to a clipped label too: `Veterans Benefits
Mandatory` clipped to `Veterans Benefits` is a different programme. **Please confirm every fixed-measure
text cell shrinks to fit rather than clipping**, and note where that changes the ledger's column widths.

#### D8 ⚠ Behaviour 6 is stated backwards between the two documents

§1C.2 reads: *"published = printed bulletin (solid frame + ref period + date + badge chip); live = desk
reading (dashed rule, unbadged)."*

Board 1a draws the opposite — the **dashed**-bordered block is the one carrying the `PRELIMINARY` badge
and the publication date, while the live desk readings sit on solid plates under a `DOMESTIC BULLETIN —
DESK READINGS, LIVE` caption.

**The board's version is arguably the better one** — a dashed rule reads as *provisional*, which is what
preliminary means. But two documents now state opposite rules for the same behaviour, and this is
precisely the behaviour where getting it backwards stays invisible until a player trusts a wrong figure.
One of them has to be struck. Please say which.

#### D9 ⚠ Eight sprite names in the captions have no file behind them

`ui_event_card` · `ui_status_ok` · `ui_stamp_holds` · `ui_stamp_verdict` · `emblem_state_seal` ·
`canvas_folder_country` · `canvas_btn_brass` · `canvas_btn_paper`

The first four have plausible substitutes in the delivered pack, and we will use them unless told
otherwise: the event card as a tinted `ui_panel_paper` with a drawn left rule, and the stamps as tinted
`ui_stamp_carried` / `ui_stamp_rejected`. **The last four do not** — the Canvas path has no button or
folder art at all, which re-opens §1B.3 after `CANVAS_SPEC.md` appeared to close it. 1f and 1g both
depend on them.

### 1D.3 — The locale decision nobody has taken

Every board sets decimals with a comma (`$29,3T`, `4,38%`, `−$0,51T`) and dates in Swedish
(`12 maj 2026`, `14 november 2027`), while all UI copy is English (`Send to the floor`, `Open dossier`).

⚠ **This is not an art-direction choice and it should not be settled in art.** `UiFormat` pins money to
`InvariantCulture` on purpose, and its doc comment names this exact string as the reason:

> *"Money renders in InvariantCulture, and deliberately so. This machine's locale is sv-SE, so the first
> version of this class produced `"$29,0T"` — a Swedish decimal comma against a US dollar sign… a
> locale-dependent formatter cannot have a fixed-string regression test, which this function above all
> others needs. Note the project's own history here — the "9,3" incident was a comma-decimal figure
> clipped in a narrow rect — so the separator is not a cosmetic detail."*

Board 1f prints USA GDP as **`$29,0T`** — character for character the string that comment names as the
bug. Elias's machine is sv-SE, so this is the development environment leaking into the boards rather
than a decision anyone made.

**No change is needed from Design here**; it is flagged so the boards are not read as settling it. The
separator belongs to `UiFormat` and behaviour 3, and the date format belongs beside it. If a Swedish
locale is genuinely wanted as a product decision, that is Elias's call and a much larger piece of work
than the boards imply.

### 1D.4 — One sentence to change, in this document rather than in the art

§1C.3 says `ui_banner_hold` *"survives the whole sequence"*, and 1e's caption repeats it. Read against
1e panel 3's own `IMGUI LAYER SUPPRESSED`, that sentence describes IMGUI drawing over a live Canvas
screen — element-granularity interleaving, and the exact thing the render-order spike ruled out.

✅ **PROPOSED RESOLUTION — a text fix, not a design change.** Board 1g already does it correctly in the
art: the banner is drawn *by the Canvas screen*, pinned to the bottom edge (`rgba(20,16,11,0.9)` on
`1px #3A2F1E`, padding `10/24`). So the rule should read:

> **Every Canvas takeover redraws the hold banner itself.** The IMGUI banner does not persist across the
> hand-off — it cannot, because IMGUI is suppressed from t=180ms. Time-hold state is never invisible
> because both sides draw it, not because one side survives.

That preserves behaviour 8 exactly and keeps screen granularity intact. **One knock-on: 1h omits the
banner entirely** — the one board that should carry it and does not. Please add it, or say why election
night is the exception.

### 1D.5 — What we are building meanwhile

Unblocked by all nine, and starting only once Elias has reviewed the existing chrome wiring live: the
surface ladder and ink set (already wired), the tab strip's three-state treatment minus D6's five missing
tints, the sub-tab and plate treatment, both status-line states, the dossier card and the generic stamp
treatment, the dual-siting build rule, and the envelope timings.

---

## 1E. PASS 3 FOLLOW-UPS — five import blockers, 2026-08-10

**Pass 3 closed all nine. This is not a fourth revision round** — the design decisions are settled and
none of what follows disputes one. Two of the amended answers were better than what was asked for: D4
refusing to invent 29 distinguishable aged hues and changing the chart form instead, and D7 rejecting
uniform auto-shrink because a column printing at four different sizes reads as an error rather than a
fit. Both are now implemented on our side.

These are **five things that stop delivered assets from being importable**, all in the delivery rather
than the design.

### E1 — `emblem_state_seal` violates §3.1's prefix rule

The sprite is right and white-on-alpha is the correct choice for a seal. The **name** is the problem.

§3.1 makes the prefix load-bearing: *"Country flags and party emblems are authored in their own real
colours… Any new art in those two categories stays full-colour; everything else stays white-on-alpha.
Getting this backwards in either direction produces art that cannot be used."* So `emblem_*` currently
*means* "full-colour exemption, never tint" — and the pass-3 manifest marks this one WoA, tinted
`inkText` on documents and brass on desk. That is the opposite rule under the same prefix, and it makes
the exemption impossible to check by name.

✅ **Requested: ship it as `ui_seal_state`.** Your own manifest note gives the answer — it calls the
sprite *"radial-tick family of `ui_seal_official`"*, which is exactly where it belongs and where it
inherits the right tint rule.

### E2 — `canvas_*` opens a second namespace inside `Chrome/`

All 52 sprites in `Assets/Resources/Art/UI/Chrome/` are `ui_*`. `canvas_folder_country`,
`canvas_btn_brass` and `canvas_btn_paper` introduce a parallel family in the same folder.

It is defensible — they are the Canvas path and they behave differently — but our coverage check and
everything else keyed to the convention now has to know about two families, and nothing in the pack
says why.

✅ **Requested: conform to `ui_*`** (`ui_folder_country`, `ui_btn_brass_canvas` / `ui_btn_paper_canvas`,
or whatever reads best to you) — or, if the split is deliberate, say so in the manifest so it is a
recorded decision rather than something we discover by sorting a directory.

### E3 — the two button strips need an import spec we do not have

`canvas_btn_brass` and `canvas_btn_paper` are `256×384` = **three cells of `256×128`** (normal / hover /
pressed), with 9-slice `24/24/24/32` *per cell*. Every other sprite in four passes has been a single
sprite with one border set.

§3's import instruction — copy the `.meta` from `icon_stat_gdp.png.meta` — produces a **single-sprite**
texture, and `Resources.Load<Sprite>` returns **null** on a multi-sprite texture. So following the
brief's own instruction on these two produces art that cannot load.

✅ **Requested, either is fine:** split each strip into three separate sprites
(`ui_btn_brass_canvas_normal` / `_hover` / `_pressed`), which matches how every other state variant in
the pack already ships — or supply the Sprite Mode Multiple spec (grid size, offsets, per-cell pivots
and borders) so §3 can carry a second import recipe.

**The first is strongly preferred.** `ui_scrollbar_thumb_v` / `_hover` / `_pressed` already established
separate-sprite state variants in pass 2, and matching that costs nothing.

### E4 — pass 3 is SVG-only, so none of it exists in `Resources/` yet

Passes 1 and 2 shipped PNGs. Pass 3 shipped four SVG sources with *"rasterize @2× at import"*.

We can rasterize, but it puts the authoritative pixels on our side of the line for the first time —
every previous pass has been byte-for-byte what you authored, and a re-rasterization by us is a
different image from yours in ways neither of us would see until they are side by side.

✅ **Requested: PNG delivery at @2×, as in passes 1 and 2**, with the SVGs retained as sources. If
rasterizing on our side is the intent going forward, say so explicitly and we will record it — the
concern is the silent change of who owns the pixels, not the work.

### E5 — `icon_pencil_draft` has no PNG either, and it is D1's agreed carrier

Found while implementing the Budget ledger row, 2026-08-10.

D1's resolution — accepted by both sides — is that the draft marker is **the `icon_pencil_draft`
sprite, never a font glyph**, because no shipped font carries `U+270F`. But that sprite has only ever
existed as `svg/icon_pencil_draft.svg`. Pass 1's manifest lists it under "SVG sources", not among the 30
PNGs, and there is no `icon_pencil_draft.png` anywhere in `Assets/Resources/`.

So the agreed fix for D1 is currently not importable, by the same E4 problem one file wider.

⚠ **This is a FIDELITY gap, not a broken behaviour, and the distinction matters for how you prioritise
it.** Behaviour 1 is satisfied today without the pencil: the drafted figure prints in draft amber
`#BE8A00`, and the span between the standing tick and the draft knob is hatched with `ui_hatch_draft`
tinted the same. If even the hatch sprite is missing, the row falls back to a flat amber wash at the
hatch's own weight — **the cue may change form, but at no point does it become nothing.** What is
missing is the pencil's identity, not the amber's meaning.

✅ **Requested: `icon_pencil_draft.png` at @2×, white-on-alpha**, alongside E4's four. Same delivery
question, same answer needed.

### DEVIATIONS — declared, not requests

**A different category from E1–E5.** Those are things we cannot build. These are places the build has
deliberately departed from the boards, declared so the divergence is visible and yours to accept or
reject. **The build should never diverge silently**, which is the only reason this section exists — none
of it is blocked on you, and none of it needs a reply unless you disagree.

**V1 — the "(current seat composition)" qualifier moved from the row to the screen header.**

| | |
|---|---|
| board | each tax row carries the full sentence *"If introduced now: WOULD PASS (current seat composition)"* |
| build | the row carries `WOULD PASS` / `WOULD FAIL` / `PENDING`; the qualifier appears once, in the screen's header |

**Why:** the board drew **eight** rows. `TaxType` has **thirteen**, so the inline version prints the
identical parenthetical twelve times on one screen — a line each, carrying nothing after the first. The
verdict varies per row; the qualifier is a property of the screen.

This is D3's arithmetic again — a board tested against a row count the game does not have — landing on
copy rather than on layout. Genuinely your call whether the qualifier belongs on the row; we have taken
the reading that it does not, and will put it back on request.

⚠ **This per-row verdict is NOT the per-instrument `VOTES` column D2 deleted.** That column scored each
tax instrument's own legislative support, which does not exist. This scores the standalone
Implement/Remove bill for that one program — a real whole-bill direction the model does compute. Same
row, different quantity, and they look alike enough to be worth keeping straight.

**V2 — Mandatory vs Discretionary spending has no treatment in the boards, so the build kept its own.**

`SpendingCategory` splits into **Mandatory** (6 lines — Social Security, Medicare, Medicaid, Income
Security, Veterans Benefits, Federal Retirement) and **Discretionary** (23). The distinction is real and
mechanical: mandatory programmes take a narrower draft range and cost more approval per unit changed,
because entitlement reform is politically expensive.

**Board 1b does not express it anywhere** — no grouping, no marker, no column. So there was nothing to
adopt, and the build kept what it already had: **two section headers, each introducing its own group.**

Declared rather than requested, for two reasons. It is not a row property — it is a property of a
*group*, and a heading is what a group heading looks like — so a row-level treatment would be the wrong
shape even if one existed. And inventing a visual language for a distinction the boards never addressed
is inventing, not implementing, which is the line this section exists to keep visible.

✅ **If you want it expressed differently, that is a real design question and worth answering** — the two
groups differ by orders of magnitude ($1.53T against $9B), which is exactly the kind of thing a period
ledger has conventions for. But it needs a decision, not a guess from us.

### OPEN QUESTIONS — raised rather than decided in code

Two things the first Spending capture surfaced. Neither is a defect and neither is blocking; both are
choices we would rather you made than have us settle silently in an implementation.

**Q1 — `SHARE` loses discriminating power on the discretionary tail.**

The board's trailing column for a spending row is SHARE, as % of GDP. It works on Mandatory, where the
lines are large. On Discretionary it reads:

`0.4% · 0.4% · 0.3% · 0.3% · 0.2% · 0.2% · 0.1%`

Seven rows, three distinct values, and the tail below that rounds to `0.0%`. The column is still
*correct* — those really are the shares — it has simply stopped distinguishing anything, on the group
where there are 23 rows to distinguish. The money column beside it (`$105B`, `$130B`, `$80.0B`) carries
the size perfectly well.

Three ways we can see, and it is your call which:
1. **Switch basis within the group** — share of the *group's* total rather than of GDP, so Discretionary
   lines are compared against each other and spread across the full range.
2. **Drop the column for Discretionary**, keep it for Mandatory. Different groups, different useful
   facts.
3. **Leave it.** Consistency across the two groups may be worth more than resolution within one, and a
   run of near-identical small numbers does itself say "these are all small".

**Q2 — the row pitch is a spec number nobody has confirmed.**

Rows currently sit about **57px** apart at 1600×900, so Spending's 29 categories run to roughly
**1650px** and scroll. Scrolling handles it and nothing breaks.

But per this project's suspect-number rule, that pitch was **derived from the font metric rather than
chosen** — it is whatever two lines of body type plus padding come to, not a decision anyone took.
Board 1b quotes `36px` at 1920×1080, which is a different number at a different size, so the two cannot
be compared directly.

**The question is whether the tail should be denser than "two lines of type" implies.** A ledger that
wants 29 rows visible at once is a different instrument from one that wants 8 legible ones, and that is
a design position rather than an arithmetic result. ✅ **"The pitch should be N at 1080p, deriving from
the font as it does now" is a perfectly good answer** — we only need it to be an answer rather than a
default.

### What this blocks, precisely

Nothing in the IMGUI path. All four items are Canvas-side, and the Canvas path was already gated behind
the IMGUI wiring being confirmed live. **Our coverage check now fails on these four by design** — it
gained a second direction this pass (does everything *specified* exist, not just: does everything
*present* load), and these are the first four entries it has ever reported missing. That failure is the
check working, not a regression.

---

## 1F. PARTY EMBLEMS — ONE QUESTION, BEFORE ANY ART (2026-08-11)

**Status: ANSWERED 2026-08-11. Decision taken, four marks delivered, batch correctly not started.
Awaiting IMPORT on our side — see §1F.1.**

This was a question, not a request for assets. It gates roughly forty of them, it has a long lead time,
and answering it wrong after the batch is drawn is expensive. §1B.3 set the precedent for asking exactly
one thing when the answer changes what gets built next; this was that shape.

⚠ **This section originally read "§1E's five import blockers are unaffected and still open." THAT WAS
WRONG WHEN WRITTEN** — all five closed 2026-08-10, and Design said so on receipt. It was copied from this
document's own status header, which was stale. **That is the second time this header has misled a reader,
and the warning about the first time is four lines above it.** Rule 12's form exactly: a status
describing the outside world is a cached value and needs an expiry, and the fix is to re-derive from the
filesystem rather than read a document. Left in place as the record rather than quietly deleted.

### What changed on our side

PoliSim is adding real politics to all six playable countries. On 2026-08-11 Elias reversed
working-discipline rule 9 **for parties only**: the game now carries **real political parties with real
names, real vote shares and real seat counts** — Socialdemokraterna, CDU/CSU, Rassemblement National,
Fratelli d'Italia, Prawo i Sprawiedliwość, the Republican and Democratic parties, and so on.

**People did not move and will not.** Every minister, party leader, legislator, head of state and Fed
Chair remains original and fictional. A party is an institution; a politician is a person.

### The question

**How should a real party's identity be represented visually, given that we will not reproduce a
trademarked logo?**

Party *names* are text and we are comfortable using them. Party *logos* are marks owned by organisations,
and reproducing them in a commercial game on Steam is a different proposition entirely. But a hemicycle
legend with six identical grey dots is unreadable, and colour alone stops working the moment two parties
in one chamber share a family colour — which happens in four of our six countries.

**Our provisional answer, offered so you have something to disagree with:** an original abstract mark per
party, in the house style, in that party's real colour. Recognisable by hue and silhouette, owned by us,
and defensible. We hold this loosely — you have made a better call than our brief twice already (D4's hue
cap and D7's resort ladder), and this is more your domain than ours.

### What we would like back

1. **A decision, with your reasoning** — our approach, a better one, or a reason the whole framing is
   wrong.
2. **A proof of concept on three parties, not forty.** Our suggestion is one two-party system and one
   crowded one: the US Republicans and Democrats, plus Sweden's eight-party Riksdag where the problem is
   hardest. If three marks work at both sizes below, the batch is derisked; if they do not, we have spent
   three drawings finding out.
3. **Nothing else yet.** The screens these live on — election night, the campaign screen, the coalition
   board — are not built. Art delivered before them repeats the `menu_pattern_tile.png` outcome, where a
   delivered asset sat unimported for weeks while three documents called it a gap.

### The two sizes that have to work

An emblem is legible at both or it is not usable:

| Where | Size | Notes |
|---|---|---|
| Hemicycle / results legend | ~14-18px square | Beside a party's short code. The demanding case — six to eight of these stack vertically and must be told apart at a glance |
| Results and coalition screens | ~48-64px square | Room for real silhouette |

### Technical conventions — §3 and §4 apply unchanged

Same prefix rules, the §3.1 tint rule, PNG delivery alongside any SVG source, and the `Zone.Identifier`
origin check on receipt. Two of §1E's five blockers are convention violations rather than design
disagreements, so it is worth re-reading §3 before the proof of concept rather than after it.

### 1F.1 THE ANSWER, and what it obliges us to build

**Decision (Design, 2026-08-11): the framing holds, reframed as BALLOT STAMPS** — the mark a game's
election authority assigns a party, one ink, silhouette-first. Real electoral commissions do this so
parties survive one-colour printing.

**This is better than what we asked for, and the reason is worth keeping.** Our version was an
original abstract mark — a workaround for a trademark problem, with no answer to "why do forty marks look
related?" A ballot stamp is diegetic: within the game's fiction the marks share a language because one
authority issues them. It is period-true, it is definitionally not the trademark, and it explains the
family resemblance rather than excusing it.

**Rules that came with it**, each of which constrains our seed data as much as their drawing:

| Rule | What it obliges |
|---|---|
| Silhouette classes unique per chamber; collision pairs must differ in class | A party's class is a property of the party, per chamber it sits in |
| Solid ink, one counter ≥2px at 16px | Legibility floor at legend size |
| Never the subject of the party's own registered mark (no rose for S) | The trademark distance is structural, not stylistic |
| National iconography stays in state chrome (no stars in the US set) | A party mark is not a flag |
| Ink-safe colours required — **SD's yellow flagged** | **Our `DisplayColor` seed values are now a legibility constraint, not just branding.** Sweden's set must be checked before it is seeded |

**The convention call, which is the part that reaches code.** Marks ship **white-on-alpha** in a new
`mark_party_*` family and are **tinted at draw time from the party's seed-data colour**. `emblem_*` keeps
its already-coloured, never-tint meaning and retires with the archetypes.

This is the right call and it lands exactly on the roadmap's Open Question 3 — *"seed data lives in one
file with retrieval dates, so a refresh is a data edit and never a code change."* A rebrand, or Sweden's
13 September election changing a whole party set, is now a `DisplayColor` edit. No redelivery.

**Built on our side, 2026-08-11:** `IconLibrary.GetPartyMark`, `PoliticalParty.MarkName`, and the
Parliament screen drawing each chamber row's mark tinted from the same `DisplayColor` the label is inked
in — so mark and text cannot disagree about a party's colour. US marks wired to `mark_party_us_rep` /
`mark_party_us_dem`.

**Argued overshoot, accepted: four drawings rather than three.** Our brief asked for one two-party system
and one crowded one, but named only S from Sweden — which cannot exhibit the red-red collision the
crowded case exists to test. V is the minimum that makes it testable. The brief was wrong and the
delivery was right.

### 1F.2 IMPORTED 2026-08-11 from `PoliSim v2 Design Progress3.zip`

**This section read "NOT YET IMPORTED" for about an hour**, which was true when written and stopped being
true when Elias delivered the pack. Kept as the record: the gap between delivered and imported is this
project's most-repeated failure (`icon_stat_interestrate` registered "awaiting delivery" on the day it
arrived; `menu_pattern_tile.png` delivered then unimported for weeks while three documents called it a
gap), and it was worth one hour of visible open status rather than none.

**Inspection before extraction**, per the origin-verification discipline established for the first pack:
77 files, 54 PNG / 19 SVG / 3 MD / 1 JSON, **no executables or scripts, no path-escape entries, no
compression anomalies**. Extracted to scratch outside the repo and inspected there before anything was
copied in.

⚠ **Mark-of-the-web could NOT be checked.** Windows alternate data streams are not visible across the
Linux mount this session reaches the repo through, so `Zone.Identifier` was neither present nor absent —
it was unobservable. That check remains outstanding and can only be done Windows-side.

**The WoA claim was verified rather than trusted**, because the tint path depends on it and a coloured
PNG run through a tint would double-apply colour: all four marks are 128×128 RGBA with **exactly one
unique RGB value among visible pixels, and it is white**. Ink coverage 19-36%.

| Mark | Silhouette class | Note from MANIFEST |
|---|---|---|
| `mark_party_us_rep` | crest | |
| `mark_party_us_dem` | torch | |
| `mark_party_se_s` | banner | not a rose — that is the subject of their registered mark |
| `mark_party_se_v` | star | the fourth drawing; the S/V red-red collision is untestable with three |

**Imported**: four PNGs to `Assets/Resources/Art/UI/Emblems/`, four SVG sources to `Emblems/Source/`,
with `.meta` files copied from the existing `emblem_party_*` importer settings and given fresh GUIDs — so
the marks import with settings already proven at legend size rather than Unity defaults.

**No regression from the re-delivery.** Of the pack's other 65 assets, **57 are byte-identical to what
is already on disk and 0 differ**. A full-pack re-delivery is exactly where a silent regression hides —
a re-export at different settings, a sprite subtly re-rendered — and byte-identity across 57 files
rules that out. That is the whole of what the diff establishes.

⚠ **IT WAS ALSO READ AS "§1E CONFIRMED CLOSED", AND IT IS NOT EVIDENCE OF THAT.** The inference ran:
*had any blocker still been open, its corrected file would have differed.* That requires an open
blocker to imply a changed file, which fails three ways. A designer who re-exported their working set
and appended four marks produces zero diffs whether or not they read the blocker list — identity is
exactly as consistent with "fixed nothing" as with "nothing needed fixing". A blocker satisfied by a
file that was never delivered shows up as ABSENCE, which a diff over shared files cannot see. And E1
and E2 are about NAMING, which is not a byte-level property at all: a file can be byte-identical and
still be at the wrong name.

**§1E is closed, verified 2026-08-11 by enumeration against disk — the method that actually answers
it.** Each blocker checked as an item rather than inferred from an aggregate:

| Blocker | Asked for | On disk | Verdict |
|---|---|---|---|
| E1 | rename `emblem_state_seal` → `ui_seal_state` | `Chrome/ui_seal_state.png` present; no `emblem_state_seal` anywhere | ✅ |
| E2 | `canvas_*` → `ui_*` | no `canvas_*` remains; `ui_folder_country`, `ui_btn_brass_canvas`, `ui_btn_paper_canvas` | ✅ |
| E3 | split the two 3-cell strips into single sprites | six present: `ui_btn_{brass,paper}_canvas` + `_hover` + `_pressed` | ✅ (the preferred option) |
| E4 | pass 3 as PNG @2× | all four present in `Chrome/` | ✅ |
| E5 | `icon_pencil_draft.png` | present, and wired as B1's carrier | ✅ |

⚠ **E5 was closed by the pass-3 import, NOT by this pack.** `icon_pencil_draft` being among the 57
byte-identical files means the pack re-shipped an already-satisfied file. Byte-identity there is a
no-regression result, never a correction — the same conflation as above, one file down.

### The eight unplaced SVG sources — an OPEN item, and not for the reason first recorded

`ui_btn_{brass,paper}_canvas_{normal,hover,pressed}`, `ui_folder_country`, `ui_seal_state` — all SVG
*sources*. No runtime asset is missing; every corresponding PNG is imported and resolving.

⚠ **The stated reason for leaving them unplaced is stale.** It read: *"rather than filed into a guessed
folder, since inventing a second namespace inside `Chrome/` was itself one of §1E's blockers."* But E2
IS that blocker, and it resolved — the delivery came back as `ui_*` and no `canvas_*` remains on disk.
There is no open question about where these belong: `Chrome/Source/` already holds 19 SVG sources under
exactly this convention. They are not blocked. They are unfiled.

✅ **FILED 2026-08-11.** Extracted from `AssetPackArchive/PoliSim v2 Design Progress3.zip` into
`Chrome/Source/`, which now holds 27 SVG sources. `DeliveredAssetCheck` went **exit 1 → exit 0, "0
missing from 0 root zip(s), 0 missing from archived packs"**.

⚠ **Filed rather than annotated, deliberately.** The alternative was to document the red as expected. A
check that is *supposed* to exit 1 is a check people stop reading — the annotation buys a week before
the red goes invisible, and then the next real gap arrives in a channel nobody watches. Filing them cost
one extraction; maintaining the exception would have cost attention indefinitely.

### The marks DO resolve — verified, but not by the check that was prescribed

⚠ **§1F said: run `DeliveredAssetCheck` and `StatIconCoverageCheck` to settle whether the marks
resolve. `StatIconCoverageCheck` cannot answer that question.** It enumerates `StatNodeId` plus
`menu_pattern_tile` — eighteen stats and a background — and never touches `Emblems/`. It passed **19 of
19** with the marks present; it would have passed 19 of 19 with them absent or corrupt. Same defect as
the diff argument in this same section: a procedure whose scope does not contain the claim, read as
evidence for it. **A passing check that cannot fail for the stated reason is worse than no check,
because it retires the question.**

✅ **`PartyMarkCoverageCheck` written to ask it properly**, behind a self-test on a known-good emblem so
a broken probe cannot masquerade as coverage.

⚠ **ITS FIRST VERSION HAD THE SAME DEFECT, ONE LEVEL DOWN — and reporting "4 of 4 resolve at 128×128,
the hand-written metas are sound" was the overclaim.** A handle coming back proves the GUID, the path
and that the meta parses. It proves nothing about whether **block compression** took effect, and
compression mangling white-on-alpha at icon size is the documented damage vector these settings exist to
prevent. A compressed mark resolves at 128×128 and reports green.

Extended to assert `texture.format`, which is runtime ground truth and needs no `isReadable` — just as
well, since these metas carry `isReadable: 0` and pixels cannot be sampled at all. **It failed
immediately: all four imported as DXT5.**

⚠ **THE CAUSE: the metas were copied from the wrong art category.** §1F recorded them as *"copied from
the existing `emblem_party_*` importer settings … already proven at legend size"*, and that is true about
provenance — the files are byte-identical to that reference apart from GUID. But `emblem_party_*` is
**full-colour** art (§3.1: authored in real colours, never tinted) and `mark_party_*` is
**white-on-alpha, tinted at draw time** — the naming split exists to mark exactly that difference. The
emblem family carries `textureCompression: 1`; `Chrome/`, the other white-on-alpha family, carries `0`
after §3's correction. **"Already proven" was proven for a different category**, which is the
cached-claim shape again with the cache one art family over.

✅ Corrected to `textureCompression: 0` / `nPOTScale: 0` on all four and re-verified: **4 of 4 resolve at
128×128, RGBA32.** The check's own bar was corrected too — it first compared each mark against the
reference emblem's format, which passed 4 of 4 while every mark was DXT5. **A check whose bar is another
artifact inherits that artifact's defects.**

### Scale this gates, for planning only

Roughly forty parties across six countries once every seed set lands — three seeded today (USA), the rest
following per country. **Sweden's set changes on 13 September 2026**, when it holds a general election, so
Swedish emblems drawn before then may need revisiting. That is an argument for the proof of concept now
and the batch later, not for waiting.

## 2. Explicitly OUT of scope — please do not produce these

- **Anything the seven data renderers already draw**: axes, gridlines, tick marks, plot lines, threshold
  lines, bars, area fills, legends, sparklines, map shapes, policy-web nodes and edges, hemicycle seats,
  pie wedges, compass dots. All procedural, per working-discipline item 10 — these render real tracked
  simulation data rather than a picture, and that is exactly what rule 10 protects. **Frames, plates and
  paper AROUND them are in scope; the data marks inside are not.**
- **Any sprite already delivered.** **136** are in production — see the table in §1. Check
  `Assets/Resources/Art/UI/` before producing anything. *(This bullet said 84, then 96, both within two
  days; re-derive from the filesystem rather than trusting the number.)*
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
| Wrap Mode | **Clamp** (`wrapU/V/W: 1`) | Correct for every sprite drawn once. **Tiling art needs `Repeat` — see §3.0a** |

The delivered `.meta` should match `Assets/Resources/Art/UI/Stats/icon_stat_gdp.png.meta` exactly apart
from its `guid`.

### ⚠ 3.0a — COPY THE META **FROM WITHIN THE SAME RENDERING CLASS**, never from the nearest filename

**"Copy that file and change only the guid" is reliable only inside one rendering class, and the
filenames actively work against getting that right.** This qualifier was added 2026-08-11 after the rule,
followed exactly as written, produced a defect for the third time.

There are three classes, and the only thing that matters is what happens to the pixels between the file
and the screen:

| Class | Members | Compression | Why |
|---|---|---|---|
| **White-on-alpha, tinted** | `Chrome/`, `Icons/`, `Stats/`, **`mark_party_*`** | **None (`0`)** | The alpha edge *is* the drawing, and it is re-tinted at draw time. Block compression quantises it into visible fringing at icon size. |
| **Full-colour, untinted** | `Flags/`, `Portraits/`, **`emblem_party_*`** | Currently `1` — undecided | §3.1's named exemption. Drawn as authored, so alpha-edge damage has no tint to amplify it. |
| **Tiling** | `Textures/menu_pattern_tile` | **None (`0`)** | Repeats across a surface, so block edges repeat too and read as a grid. |

⚠ **`Emblems/` STRADDLES TWO CLASSES, and that is where the last defect came from.** `emblem_party_*` is
full-colour and never tinted; `mark_party_*` is white-on-alpha and tinted at draw time. They are
**filename-adjacent and treatment-opposite**. Four `mark_party_*` metas were copied from
`emblem_party_*` — the nearest neighbour by name, the wrong one by treatment — and all four imported as
DXT5. `Chrome/` was the far neighbour by name and the correct one by class.

**So the test is never "which file is next to it".** It is: *does this art get tinted at draw time?* If
yes, copy from `Chrome/`. If no, copy from `Flags/`. If it tiles, see the row below.

**Two instances of this same rule, both previously recorded as one-off exceptions:**

- **Chrome needs `isReadable: 1`** (2026-08-03, after it cost an entire UI). The icon template carries
  `isReadable: 0`, correct for icons, which are tinted via `GUI.color` and never read back.
  `UiPalette.GetTintedChrome` instead calls `Texture2D.GetPixels`, which **throws** on a non-readable
  texture. Pass-1 metas were copied from the icon template exactly as instructed, and the first wired
  build rendered as an empty desk.
- **`menu_pattern_tile` needs Wrap Mode `Repeat`, not `Clamp`.** It is drawn with
  `DrawTextureWithTexCoords` across the whole menu. Clamp does not fail — it stretches the edge pixel
  across the screen, which reads as a design choice rather than a broken import.

Neither is an exception. Both are the same rule: **the template encodes its own class's treatment, and
copying it across a class boundary carries the wrong treatment with it.**

### 3.0b — the two settings rulings, 2026-08-11

**MIPMAPS — OFF, and this is an EXISTING rule now checked, not a new one.** The settings table above has
said *"Mipmaps **Off** (`enableMipMap: 0`) — UI sprites never minify"* since it was written. 44 files
across `Emblems/`, `Flags/`, `Icons/` and `Portraits/` carried them anyway. A mip chain on art IMGUI
draws at 1:1 is memory spent making it blurrier. All 44 corrected (per-file before/after verification),
and `ImporterSettingsCheck` promoted it from warning to **error**.

⚠ Recorded here as pre-existing precisely so the check is not cited as the authority for it. **A check
must never be the source of a rule it enforces** — that is circular, and the failure it creates is a
rule nobody can argue with because nobody can find where it was decided.

**FULL-COLOUR COMPRESSION — ACCEPTABLE, ruled after a visual check.** The 26 `Flags/`, `Portraits/` and
`emblem_party_*` sprites import block-compressed. **Flags are the worst case for block compression** —
large flat colour fields meeting at sharp edges is exactly what DXT quantises worst — and compared
against an uncompressed source at display size they show no visible damage. Portraits, which are
continuous-tone and have no hard colour boundaries, are covered *a fortiori*.

So compression stays for this class, and **the warning was dropped rather than kept as a passing note.**
A permanent 26-line amber is a thing people learn to skim, and a check whose output is mostly noise stops
being read at all — the same argument that made filing the eight SVG sources better than annotating an
expected failure. `ImporterSettingsCheck` now runs **149 sprites, 0 errors, 0 warnings**.

✅ **`ImporterSettingsCheck` now enforces this.** It enumerates every `*.png` under
`Assets/Resources/Art/UI/` (149 files), classifies each by treatment rather than by folder, and asserts
against stated values — reading the **imported texture**, not the `.meta` text, because the meta is the
claim and the texture is the fact. It found a third instance on its first run: all 14 `icon_area_*` /
`icon_nav_*` sprites were DXT5 while `Stats/`, the same class one folder over, was already correct.

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

- *"84 sprite files on disk"* — then 96, now 125 after chrome pass 1. Wrong twice inside two days.
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

## §1F — CARRIED, NOT RESOLVED: Design's rasterization diff (opened 2026-08-10)

Pass 3 shipped the six per-state button PNGs with an explicit caveat: they are cut from the strip
cells byte-identically in geometry, but Design asked us to diff them against **our own**
rasterization once before trusting the pipeline — correctly noting that a byte-identical cut is a
claim about their tooling, not a verification of ours.

**We cannot run it.** This machine has no SVG rasterizer: no ImageMagick (`convert` on PATH is the
Windows disk utility), no Inkscape, no `rsvg-convert`, no Node, and the `python3` on PATH is the
Microsoft Store stub. Unity cannot stand in either — `com.unity.vectorgraphics` is not installed.

**What was verified instead, and what that does and does not cover.** All six PNGs decode cleanly at
256×128 with 32bpp alpha, and all six MD5 hashes differ, so the cuts are genuinely distinct frames
rather than a duplicated cell — a failure mode a geometry-only check would have missed. That
verifies *their* delivery. It does not verify *our* pipeline, which is precisely the distinction
Design drew, so the caveat stands.

⚠ This closes when a rasterizer exists here — not when the sprites look right in a capture. They
already do, and that is exactly why this is easy to let lapse.

---

## 1G. ONE MARK — `mark_party_us_lib` (2026-08-11)

**One drawing, not a batch.** §1F's proof-of-concept ruling stands and this does not reopen it: the
screens that need forty marks still do not exist, and this is a gap in a set already on screen.

### Why this one

`PartyMarkCoverageCheck` enumerates the **party list** rather than the mark folder, and that change
immediately surfaced what a folder count had been hiding: the US seed carries **four parties and two
marks**. The Parliament rows draw a crest beside the Republicans and a torch beside the Democrats,
verified in capture; the other two rows draw text with an empty space where a mark would sit.

| Party | Mark | Status |
|---|---|---|
| Republican Party | `mark_party_us_rep` | ✅ crest, resolving, RGBA32 |
| Democratic Party | `mark_party_us_dem` | ✅ torch, resolving, RGBA32 |
| **Libertarian Party** | **`mark_party_us_lib`** | ⬅ **REQUESTED** |
| Other and independent | — | **deliberately none — see below** |

### The request

**`mark_party_us_lib.png`, @2×, 128×128, WHITE-ON-ALPHA.**

⚠ **White-on-alpha, NOT full colour — this is the trap §3.0a exists for.** `mark_party_*` is the
tinted class: the mark takes its colour from `PoliticalParty.DisplayColor` at draw time, so a party
rebrand is a seed-data edit rather than a redelivery. `emblem_party_*` is the *other* class,
full-colour and never tinted, and the two are filename-adjacent. Import settings per §3.0a's
white-on-alpha row — copy from `Chrome/`, never from `emblem_party_*`, which is how four marks came to
be block-compressed.

**Subject:** an original abstract mark, on the same terms as the first three. Legible as a silhouette at
**~14px** beside a legend row — that is the size it actually renders at, verified in
`marks_07a_politics_parliament.png`, and it is smaller than any brief so far has had to hold.

### ⚠ THE TRADEMARK CONSTRAINT — standing, applies to every party mark ever requested

**A party mark is ORIGINAL ART. It is never the party's own mark, and never a recognisable derivative of
one.** Real party *names* are text and we use them; real party *logos* are marks owned by organisations,
and reproducing one in a commercial game on Steam is an entirely different proposition.

**This has already decided three drawings, and the reasoning must be restated in every future request
rather than re-derived by whoever writes the next one:**

| Party | Delivered | NOT drawn, and why |
|---|---|---|
| Socialdemokraterna | a **banner** | **not a rose** — the rose is the subject of their registered mark |
| Democratic Party | a **torch** | not the donkey |
| Republican Party | a **crest** | not the elephant |

A mark must therefore be recognisable by **silhouette plus the party's real colour**, carry no element of
the registered mark, and be ours to own. Recorded as working-discipline **rule 9a**.

⚠ **For `mark_party_us_lib` specifically:** the Libertarian Party's associated imagery raises exactly the
same question, and this constraint is stated **before** the pack is produced rather than raised after it
arrives — which is the only point at which saying it is cheap.

### `Other and independent` — a deliberate NON-GAP, recorded so it is not requested later

It gets no mark, and that is a decision rather than an omission. **It is a residual bucket, not a party**
— it aggregates every vote not cast for the three named parties, has no organisation, no leader and no
identity. Giving it a drawn mark would assert an entity that does not exist, and the row rendering
without one is correct: `GetPartyMark` returns null and the row draws as text, which is precisely what a
residual should look like beside three parties that have marks.

**So `PartyMarkCoverageCheck` reporting "2 without one" is 1 request and 1 by design**, and it will keep
reporting 1 after this lands.
