# PoliSim UI Chrome — control-chrome sprite pack

Delivered 2026-08-01 by Claude Design. Answers `CLAUDE_DESIGN_ASSET_REQUEST_UI_CHROME.md` in full,
with all four section-8 decisions applied (subtle gradients; 6px button / 9px panel corners;
slider fill included; panel sprite produced).

**Addendum (2026-08-01, same day):** 2 vertical scrollbar sprites added per
`CLAUDE_DESIGN_ASSET_REQUEST_UI_CHROME_ADDENDUM.md` — 16 of the 17 scroll views are vertical and
IMGUI has no per-style texture rotation. Mirrored 90 degrees from the delivered horizontal pair:
caps top/bottom, recess/gradient rotated to read lit-from-top-left. Same alpha values.

## Authoring convention
RGB is pure white everywhere; ALL depth (gradients, bevels, pressed inset, recesses, edge
highlights) lives in the ALPHA channel. Tint at draw time with a GUI.color multiply, exactly like
`UiPalette.DrawTintedIcon` — final pixel = tint x alpha over background. No pre-colored variants
exist by design (request section 2): colors stay in `UiPalette`/`PoliSimTheme`.

## Manifest (12 sprites, PNG 48x48 + SVG 24x24 source)
| File | Geometry | 9-slice border (GUIStyle.border) |
|---|---|---|
| ui_button_normal / _hover / _pressed / _disabled | full-bleed rounded rect, r=6 | 10 all sides |
| ui_panel | full-bleed rounded rect, r=9, flat fill (alpha 0.8) + 1px inner border (alpha 1.0) | 13 all sides |
| ui_slider_track | capsule 48x24 centered (r=12), recessed | 14 L/R, 0 T/B |
| ui_slider_fill | capsule 48x24 centered (r=12), solid | 14 L/R, 0 T/B |
| ui_slider_thumb | circle d=38 centered (~10% margin), ring edge + slight raise | NOT sliced |
| ui_scrollbar_track | capsule 48x24 centered, faint (alpha 0.7), subtle recess | 14 L/R, 0 T/B |
| ui_scrollbar_thumb | capsule 48x24 centered, gentle top-light gradient | 14 L/R, 0 T/B |
| ui_scrollbar_track_vertical | capsule 24x48 centered, caps top/bottom, recess on left edge | 14 T/B, 0 L/R |
| ui_scrollbar_thumb_vertical | capsule 24x48 centered, left-to-right gradient | 14 T/B, 0 L/R |

The capsules sit centered on the canvas with transparent margins on the non-sliced axis — drawn at
a rect of thickness T, the capsule renders T/2 thick. Radius (12) is under the 14px slice border,
so caps are never cut mid-curve.

## Project-side notes
1. **Border values above, not "18".** Request section 9 says "Border set to 18 on all sides" in one
   place, but its own per-element table says 10/13/14 — the table matches decision 2 (6px/9px radii)
   and is what these files are authored to. The "18" line reads as a leftover from the earlier
   16px-radius draft. Use 10 (buttons), 13 (panel), 14 on the sliced axis (capsules).
2. **Vertical scrollbars: RESOLVED by the addendum sprites** — point vertical scrollbar styles at
   the `_vertical` pair; no rotated draws needed. (Per PoliSim-side confirmation these load as plain
   Texture2D and slice via `GUIStyle.border` set in code — `spriteBorder` is never read by IMGUI.)
3. **Slider fill is new behavior**, per decision 3: stock Unity draws no fill. Track and fill share
   IDENTICAL geometry — draw both at the exact same rect and clip the fill style rect to the value
   fraction. They align pixel-for-pixel.
4. **Disabled buttons**: the sprite is flat by design; Unity GUI additionally auto-dims disabled
   controls (~50% alpha), so avoid also pre-darkening the disabled tint or it will double up.
5. **Unity import** (per PoliSim-side import fixes, confirmed 2026-08-01): nPOTScale None (do NOT
   let 48x48 resample to 64x64 — it destroys the slice geometry), Alpha Is Transparency ON, no
   block compression, no mipmaps, Clamp wrapping, Bilinear.

## SVG sources (`Source/`)
24x24 geometry, `currentColor` fill, 1-3 primitives each (rounded-rect / circle / linear gradient
with stop-opacity). Reference-only — Unity consumes the PNGs. The pressed inner shadow is expressed
as a multi-stop gradient in SVG (plain SVG cannot subtract alpha); the PNGs are authoritative.
