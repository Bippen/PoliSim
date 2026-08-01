# Claude Design asset request — UI chrome ADDENDUM: vertical scrollbars

**Status:** ready to send.
**Date:** 2026-08-01.
**Context:** small follow-up to `CLAUDE_DESIGN_ASSET_REQUEST_UI_CHROME.md`. That pack was delivered
complete, verified and imported (`Assets/Resources/Art/UI/Chrome/`, commit `10c18a2`) — nothing in it is
wrong or being re-requested. This addendum covers **one gap the delivered pack's own README correctly
predicted**, and nothing else.

---

## 1. What's needed — 2 sprites

| File | Geometry | 9-slice border |
|---|---|---|
| `ui_scrollbar_track_vertical.png` / `.svg` | Capsule **24 wide × 48 tall**, centred, caps **top and bottom**, faint (alpha ~0.7), subtle recess. Vertical mirror of the delivered `ui_scrollbar_track`. | **0 left/right, 14 top/bottom** |
| `ui_scrollbar_thumb_vertical.png` / `.svg` | Capsule **24 wide × 48 tall**, centred, caps **top and bottom**, gentle left-to-right light gradient (the vertical analogue of the delivered thumb's top-light gradient). | **0 left/right, 14 top/bottom** |

Everything else matches the delivered pack exactly: 48×48 canvas, 8-bit RGBA, **pure white RGB with all
depth in the alpha channel**, transparent margins outside the capsule, SVG source at 24×24 using
`currentColor`.

---

## 2. Why — and why it can't be solved on our side

The delivered `ui_scrollbar_track` / `ui_scrollbar_thumb` are authored **horizontally** (caps left/right,
border 14 L/R + 0 T/B), exactly as the request's own spec table asked for. The pack README flagged the
consequence, and it is correct:

> *"IMGUI styles do not rotate textures, so a vertical scrollbar style pointed at these will stretch the
> caps."*

Verified against the real code: of **17 scroll views**, **16 are vertical-only**
(`GUILayout.BeginScrollView(..., GUILayout.Height(...))` with no width constraint). Exactly one — the
Budget Process three-column row — also scrolls horizontally, and the delivered horizontal sprites serve
it correctly. So the delivered orientation covers **1 of 17** cases.

Pointing a vertical scrollbar style at a horizontal capsule stretches the rounded caps along the bar's
length, so the ends smear into elongated blobs — worst on long scrollbars, which is most of them.

**Why not just rotate it in code:** IMGUI has no per-style texture rotation. It would require wrapping
each scrollbar draw in a `GUIUtility.RotateAroundPivot` / `Matrix4x4` transform around its own rect.
That is exactly the class of fragile hand-computed geometry that has already caused two real layout
regressions in this project (a header clipping mid-word, and a panel rendering catastrophically narrow),
and it would have to be right for all 17 scroll views at every window size. Two mirrored sprites are far
cheaper and cannot regress.

---

## 3. Explicitly NOT needed — please don't produce these

- **Scrollbar arrow buttons** (up/down/left/right). Unity's scrollbar style includes them, but modern
  flat scrollbars have none, and we will hide them in code by giving those sub-styles a zero fixed size.
  No art required — flagged here specifically so they aren't drawn on the assumption that a complete
  scrollbar needs them.
- **Horizontal scrollbar replacements.** The delivered ones are correct and already serve the one
  horizontal scroll view.
- **Vertical slider parts.** Every slider in the game is horizontal (30 `GUILayout.HorizontalSlider`
  calls, zero vertical), so the delivered slider track/fill/thumb need no vertical counterpart.
- **Anything else from the main pack.** Buttons, panel and slider parts are all delivered, verified
  (max RGB channel spread across the pack: 0 — perfectly neutral for tinting) and imported.

---

## 4. Filename manifest

**2 source assets → 4 files.**

```
ui_scrollbar_track_vertical.png / .svg
ui_scrollbar_thumb_vertical.png / .svg
```

Destination on delivery: `Assets/Resources/Art/UI/Chrome/` (PNGs) and
`Assets/Resources/Art/UI/Chrome/Source/` (SVGs) — straight in beside the existing chrome sprites, no new
folder.

---

## 5. Note back to Claude Design

The delivered pack's README caught a genuine contradiction in our own request document — a leftover
"Border set to 18 on all sides" line that survived a revision and disagreed with the per-element geometry
table below it. The pack was authored to the table, which was the correct call, and our document has
since been fixed. Flagging it as useful, not as a complaint: reading the spec closely enough to catch
that it disagreed with itself is exactly what we want.
