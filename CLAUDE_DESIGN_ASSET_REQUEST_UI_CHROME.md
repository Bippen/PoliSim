# Claude Design asset request — UI chrome (buttons, sliders, scrollbars, panels)

**Status:** ready to send, pending Elias's answers to section 8.
**Date:** 2026-08-01.
**Context:** follow-up to `CLAUDE_DESIGN_ASSET_REQUEST_5E.md` (icons, portraits, emblems, flags — all
delivered and now in production use). That request covered *imagery*. This one covers *control chrome*:
the backgrounds Unity draws behind buttons, sliders, scrollbars and panels.

---

## 1. Why this request exists — the audit

Master Sequence step 5e restyled the game's **content** (cards, stat tiles, icons, portraits, lean bars).
It never touched the **controls**. The result is a visible split: modern rounded dark cards containing
controls that still render in Unity's stock 2019-era grey chrome.

This was audited against the actual code, not from screenshots. Every result below is a real call-site
count.

### Tier 1 — literally stock Unity chrome (no override at all)

These clone `GUI.skin.*` and change only font size or fixed height, so Unity's default grey
gradient/bevel textures draw underneath:

| Control | Where | Count | Notes |
|---|---|---|---|
| **Sliders** | `_sliderStyle` / `_sliderThumbStyle` (`GUI.skin.horizontalSlider` / `horizontalSliderThumb`) | **30 sliders** | **The single biggest offender.** Every policy dial in the game — tax rates, welfare generosity, spending lines, all Labor/Crime/Sector/SWF/Trade dials. |
| **Panel / box frames** | `_boxStyle` (`GUI.skin.box`) | **32 usages** | The frame around essentially every tab and column. Its flat grey is what makes whole screens read as "old Unity". |
| **Scroll views** | `GUILayout.BeginScrollView` default skin | **17 scroll views** | Default scrollbars, visible on nearly every tab. |
| **"Show / Hide tab guide" button** | `GameController.cs:1593` | 1 | Passes **no style argument at all** — pure `GUI.skin.button`. |
| **Graph paging buttons** | `GraphRenderer.cs:190`, `:202` (`"< Older"`, `"Newer >"`) | 2 | `_pageButtonStyle` is a bare `GUI.skin.button` clone. |

### Tier 2 — recoloured, but flat and hard-cornered

~21 further button call sites route through `UiPalette.BuildButtonStyle`, which swaps in solid-colour
textures per state. These are **not** stock grey — they are correctly colour-coded and have real
hover/pressed feedback — but they are flat rectangles with square corners, so they still clash with the
rounded cards introduced in Phase C. Six kinds exist: `Primary`, `Neutral`, `Implement`, `Remove`,
`Tab`, `TabSelected`.

**Both tiers are in scope for this request.** Fixing only the three literal stock buttons would leave 30
stock sliders and 32 stock panel frames sitting beside them.

---

## 2. What we are asking for, and the one big format decision

**Author every control background WHITE on transparent, as a 9-slice, and let the game tint it at
runtime.** This is not a new idea — it is the convention the original pack's own README established for
icons: *"Authored white so a single texture serves every state — tint with `PoliSimTheme.Accent(...)`
instead of shipping a coloured copy per hue."* `UiPalette.DrawTintedIcon` already does exactly this in
production, and `UiPalette.BuildButtonStyle` already resolves a colour per button kind and state.

The consequence is a **much smaller ask**. Six button kinds × four states would be 24 sprites; authored
white and tinted, it is **four**. Please do not ship pre-coloured variants — the colours live in
`UiPalette`/`PoliSimTheme` and must stay there, or every future palette change becomes an art change.

---

## 3. Button backgrounds — 4 sprites

9-sliced so one texture serves every button size in the game (they range from ~60px wide sub-tab buttons
to full-width action buttons).

| State | Filename | Intent |
|---|---|---|
| Normal | `ui_button_normal.png` / `.svg` | Rounded rect, subtle top-to-bottom gradient, 1px lighter inner top edge. Reads as slightly raised. |
| Hover | `ui_button_hover.png` / `.svg` | Same shape, brighter overall, slightly stronger edge highlight. |
| Pressed | `ui_button_pressed.png` / `.svg` | Same shape, inverted gradient / subtle inner shadow at top. Reads as pushed in. |
| Disabled | `ui_button_disabled.png` / `.svg` | Same shape, flat, no gradient or edge highlight. Used heavily — every gated control (`GUI.enabled = false`) during game-over or while a bill is pending. |

**Why these need to be art and not procedural:** the project already generates rounded rectangles
procedurally (`UiPalette.GetRoundedTexture`, used by the Phase C cards), so a *flat* rounded button needs
no art at all. What procedural fills cannot express is the gradient, inner bevel and pressed-inset that
make a control read as physically pressable. That depth is the entire reason to request these.

---

## 4. Slider — 3 sprites (highest impact)

30 sliders make this the most-seen control in the game, and it is currently 100% stock Unity.

| Part | Filename | Intent |
|---|---|---|
| Track | `ui_slider_track.png` / `.svg` | Thin rounded capsule, recessed (subtle inner shadow). 9-sliced horizontally so it stretches to any width. |
| Fill | `ui_slider_fill.png` / `.svg` | Same capsule shape, solid, drawn left-of-thumb to show the current value. White for tinting — this is where a policy area's own accent colour will show. |
| Thumb | `ui_slider_thumb.png` / `.svg` | Circular grip with a clear edge and slight raise. **Not** 9-sliced — fixed shape, drawn at a fixed size. |

**Note on the fill:** Unity's built-in `HorizontalSlider` does not draw a fill at all — only a track and
a thumb. Adding one is a small code change on our side, and it is worth it: with 30 dials that each have
a *standing* value and a *draft* value, a filled bar communicates "how far along this range am I" far
better than a lone thumb. Please supply it even though it has no current equivalent.

---

## 5. Scrollbars — 2 sprites

| Part | Filename | Intent |
|---|---|---|
| Track | `ui_scrollbar_track.png` / `.svg` | Very subtle recessed capsule — should nearly disappear against a dark panel. |
| Thumb | `ui_scrollbar_thumb.png` / `.svg` | Rounded capsule, clearly visible but not attention-grabbing. 9-sliced along its length. |

---

## 6. Panel frame — 1 sprite

| Part | Filename | Intent |
|---|---|---|
| Panel | `ui_panel.png` / `.svg` | 9-sliced rounded rect with a soft 1px border, no fill gradient. Replaces `GUI.skin.box` in all 32 usages. |

**Note:** the Phase C content cards are already procedurally generated and look correct
(`UiPalette.BuildCardStyle`). This panel sprite is for the **outer tab/column frames**, which are a
different, larger element. If it turns out visually indistinguishable from the procedural card in
practice, we will drop it and reuse the procedural one — flagged here so it isn't over-invested in.

---

## 7. Explicitly OUT of scope — please do not produce these

Carried over from the previous request's approach, and sharpened by two lessons since (see
`POLISIM_MASTER_ROADMAP.md`, 5e retrospective — two widgets from the original pack were rejected because
they encoded mechanics and layout this project does not have):

- **Anything already procedural and working.** All data visualisations stay procedural per working
  discipline item 10: graphs, the world map, the political compass, the parliament hemicycle, pie charts,
  the policy web, the Phase C cards, and the new diverging lean bars. No art needed, none wanted.
- **Pre-coloured button variants** — see section 2. White + runtime tint only.
- **Full window/HUD frames, decorative borders, ornamental corners.** The design language is flat and
  restrained; this is a data-dense simulation, not a fantasy RPG.
- **Checkbox / radio / dropdown / text-field chrome.** The codebase currently uses **zero** toggles,
  dropdowns or text fields (verified: `GUILayout.Toggle` count is 0). Requesting them would be
  speculative.
- **Icons of any kind.** Fully covered by the previous request and already in production.
- **Per-state slider thumb variants** (hover/pressed). One thumb is enough for the first pass; we can
  tint it for state exactly as with everything else.

---

## 8. Open questions for Elias

1. **Gradient depth.** How much dimensionality do you want on buttons — nearly flat with just a 1px edge
   highlight (most consistent with the current flat dark cards), or a more pronounced raised/pressed
   gradient (more tactile, slightly more "game UI")? *Recommendation: subtle.* The existing design
   language is flat, and strong gradients would fight the Phase C cards.
2. **Corner radius.** The Phase C cards use a 9px radius at 1080p. Should buttons match exactly, or be
   slightly tighter (~6px) so controls read as distinct from containers? *Recommendation: slightly
   tighter* — it visually separates "thing you click" from "thing that holds content".
3. **The slider fill (section 4).** Confirm you want a fill added at all — it is a genuine, if small,
   behavioural change to every dial in the game, not just a reskin. *Recommendation: yes*, given 30
   dials with standing-vs-draft values.
4. **The panel sprite (section 6).** Worth producing, or should we just reuse the existing procedural
   card for outer frames too? *Recommendation: produce it*, but treat it as the most droppable item here.

---

## 9. Format & technical spec

Matches `CLAUDE_DESIGN_ASSET_REQUEST_5E.md`'s established conventions, so these drop in beside the
existing assets with no format drift.

- **PNG:** 8-bit RGBA, transparent background, **authored white** (single-colour, tinted at draw time).
- **9-sliced elements** (`ui_button_*`, `ui_slider_track`, `ui_slider_fill`, `ui_scrollbar_*`,
  `ui_panel`): **64×64**, corner radius **16px**, intended 9-slice border **18px** on all four sides
  (leaving a 28×28 stretchable centre). Keep all detail — gradients, bevels, edges — inside that 18px
  border, since the centre is stretched arbitrarily and any detail there will smear.
- **Non-9-sliced elements** (`ui_slider_thumb`): **48×48**, artwork centred, ~10% transparent margin so
  the shape is not clipped when drawn at odd sizes.
- **SVG source:** same geometry, `currentColor` fill, simple primitives (rounded-rect, circle, linear
  gradient). Keep it simple enough to be redrawn procedurally later if we ever choose to — the same
  design intent the original pack was built on.
- **Unity import settings** (for reference, not part of the art brief): Texture Type
  `Sprite (2D and UI)`, Alpha Is Transparency **on**, sRGB **on**, Filter Mode `Bilinear`, Compression
  `None`. 9-sliced sprites additionally need their Border set to 18 on all sides in the Sprite Editor —
  a project-side import step, not something the art must encode.

---

## 10. Filename manifest

**10 source assets → 20 files** (PNG + SVG each).

```
# Buttons (9-slice, 64x64)
ui_button_normal.png / .svg
ui_button_hover.png / .svg
ui_button_pressed.png / .svg
ui_button_disabled.png / .svg

# Slider
ui_slider_track.png / .svg      (9-slice, 64x64)
ui_slider_fill.png / .svg       (9-slice, 64x64)
ui_slider_thumb.png / .svg      (fixed shape, 48x48)

# Scrollbar (9-slice, 64x64)
ui_scrollbar_track.png / .svg
ui_scrollbar_thumb.png / .svg

# Panel (9-slice, 64x64)
ui_panel.png / .svg
```

Destination on delivery: `Assets/Resources/Art/UI/Chrome/` (PNGs) and
`Assets/Resources/Art/UI/Chrome/Source/` (SVGs), mirroring the existing `Icons/` + `Icons/Source/`
layout. Note the `Resources/` root — required so `IconLibrary`'s `Resources.Load` can reach them at
runtime in a real player build, the same reason the icons and portraits were moved there.
