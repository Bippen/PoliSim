# UI v3.3 - symbols first, words on demand

Ruled by Elias 2026-09-29 (`COMPLETED.md` §648's record carries the ruling's text); written here before anything is built on it, per the
standing rule that a design a later session depends on lives in the repo (§643). Design's answer to the first three items is boards 19a
(the symbol language), 19b (the slip, the desk's two-level tooltip) and 19c (the dense view) in `PoliSim v2 Screens.dc.html`, read
2026-09-29 (`#d24-note`); Design calls the programme D24.

## 1. The rule

**Symbols and graphics at rest, words and numbers on demand.** A screen at rest shows marks, instruments and the few words that name what
is on it; every word or number it no longer prints is reachable **within two tooltip levels** (19b: level 1 the slip, level 2 a marked
term's definition), or on the dense view (19c). **A check asserts the reachability** - a removed word that no slip and no dense line can
reach fails the bar.

## 2. The metric

**At-rest text draws per screen**, counted from the dry film's label table (the `DryGuiPass` record): the draws inside the clip of each
rail screen's own unscrolled frame. The tool is `Tools/text_baseline.pl rank <labels.tsv>`; the baseline is generated into
`docs/generated/UI_V33_BASELINE.tsv` from the dry film of the tree that closed item 1 of the ruling (§648) and is never hand-edited.

**Target: at least 50 % fewer at-rest text draws on the main screens** than the baseline, each screen measured against its own baseline row.
A screen's retrofit closes when its dry film's count meets the target and the reachability check holds.

## 3. The order of the retrofits

**By the measured at-rest text, most first, with the Desk after** (ruled 2026-09-29): **People, then Stats, Energy and Budget; the Desk
after them.** The remaining screens follow the baseline's order. **Each retrofit keeps its screen's grammar** - the instruments, the
columns and the order stay; what changes is that words become marks at rest and words on demand. **People cuts its at-rest text through
the symbol language and keeps its provenance behind †.**

Also on the retrofit list (built before the slip existed): **the formation sheet** (§647). **The party-creation flow is built icon-first**
(never retrofitted).

## 4. Foundations

1. **The slip - a two-level pinnable tooltip** (19b): level 1 opens after 250 ms at rest on a symbol, instrument or marked name, below-right
   of its anchor; level 2 from a marked term in level 1; a click on the head pins (three pinned chains a page); a slip never commits
   anything. Its first line is always the glyph's own word, so every slip is its own legend entry.
2. **An icon registry with honest fallbacks until assets land.** A symbol the registry cannot load draws its WORD in its place - never a
   blank, never a stand-in glyph - and the registry's coverage check names every symbol drawn as a fallback. The D24 vocabulary (19a,
   sixteen glyphs) is installed from Design's delivery **as one zip, never written from a pulled image**.
3. **The generated icon inventory sent to Design** (D24 item 4): `PoliSim.EditorTools.D24Inventory.Emit`, category level, from the
   catalogues and the icon loader - nothing typed.

## 5. What is kept

- **Honesty qualifiers stay visible at rest** - DERIVED, SOURCED, DATED, PROVISIONAL, ABSENT, BILLED - as words until Design's glyphs land,
  then as the 19a honesty glyphs. An absence is never quieter than the figure it replaces.
- **Colour never carries meaning alone.** Every ink that means something is backed by a shape or a sign (19a rule 6; the swing rows of §648
  are the pattern: the sign is the shape, the ink backs it).
- **A dense view shows every number and source** (19c): the global †, lines never columns, every glyph's word beside it.

## 6. Out of scope

Unity's `productName` and `companyName`; the simulation. A retrofit that would change what the model computes is not a retrofit.
