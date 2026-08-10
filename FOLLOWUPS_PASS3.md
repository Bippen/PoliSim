# PoliSim — Pass 3 follow-ups

**Status: OPEN — five import blockers, plus one declared deviation.**
**Date:** 2026-08-10.
**Not a revision round.** Pass 3 answered all nine §1D items and none of this disputes a design
decision. Five delivery-side problems stop delivered assets from importing; one deviation is declared
for visibility and needs no reply unless you disagree with it.

⚠ **GENERATED FILE — do not edit.** The body below is §1E of `CLAUDE_DESIGN_ASSET_REQUEST.md`, the
source of truth. Regenerate after any edit to §1E:

```sh
awk '/^## 1E\./{f=1} /^## 2\./{f=0} f' CLAUDE_DESIGN_ASSET_REQUEST.md
```

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

### What this blocks, precisely

Nothing in the IMGUI path. All four items are Canvas-side, and the Canvas path was already gated behind
the IMGUI wiring being confirmed live. **Our coverage check now fails on these four by design** — it
gained a second direction this pass (does everything *specified* exist, not just: does everything
*present* load), and these are the first four entries it has ever reported missing. That failure is the
check working, not a regression.

---

