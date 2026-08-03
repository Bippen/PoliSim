# Fonts — licences and attribution

Every font here is redistributable in a commercial game. **The previous test fonts were not**: Palatino
Linotype, Courier New, Consolas and Georgia are licensed Microsoft system fonts, usable *on* Windows but
not shippable inside a build. That blocker is what this folder exists to resolve.

`PoliSimTheme` loads these by name via `Resources.Load<Font>` and is null-safe — a missing file leaves
Unity's built-in face rather than rendering nothing.

## Licence terms, and what they require of us

| Font | Role | Licence | Redistribution in a commercial game |
|---|---|---|---|
| Gentium Book Plus (Regular, Bold) | display + body candidate | SIL OFL 1.1 | Yes |
| TeX Gyre Pagella (Regular, Bold) | display + body candidate | GUST Font License (LPPL 1.3c) | Yes |
| Literata (Regular) | display + body candidate | SIL OFL 1.1 | Yes |
| Vollkorn (Regular) | display + body candidate | SIL OFL 1.1 | Yes |
| Courier Prime (Regular) | document artifacts | SIL OFL 1.1 | Yes |

**SIL OFL 1.1 obligations, all of which this folder satisfies:**

1. **The licence text must travel with the font.** `Licenses/OFL-*.txt` is the verbatim licence for each,
   downloaded from the upstream source alongside the binary. **These files must be included in the shipped
   build**, not merely in the repo — see "Shipping" below, which is the one thing still open.
2. **The fonts may not be sold on their own.** Bundling inside a game is explicitly permitted.
3. **Reserved Font Names.** If a font is *modified*, the modified version must be renamed. We do not modify
   them, so this does not bite — but subsetting or re-hinting a font later WOULD count as modification.
4. No requirement to open-source the game, and no attribution-in-credits requirement. Crediting them is
   courteous, not obligatory.

**GUST Font License (TeX Gyre Pagella)** is LPPL 1.3c plus a request — not a legal requirement — that
derived works be renamed. Same practical position as OFL: redistribute freely, do not pass off a modified
version under the original name.

## Shipping — OPEN, and it is a real obligation rather than paperwork

Unity does **not** automatically place `Licenses/*.txt` into a player build. A `.txt` under `Resources/`
is not loaded as a `TextAsset` unless something references it, and files outside `Resources/` are not
copied at all. Before the first public build, one of these has to happen:

- copy `Licenses/` into `StreamingAssets/` (simplest — the folder is copied verbatim into the build), or
- surface the licence text in an in-game credits/legal screen, or
- ship it beside the executable as part of the installer payload.

**Recorded here rather than assumed**, because the obligation attaches to distribution, not to the repo,
and a repo-only licence file satisfies nothing.

## What is actually wired

**TeX Gyre Pagella** (display + body) and **Courier Prime** (document artifacts). Set in
`PoliSimTheme.DisplayFontDefault` / `BodyFontDefault` / `DocumentFontDefault`; swapping is a one-constant
change, and the other three families are kept here so a swap needs no re-download.

Chosen after capturing all seven screens under each candidate. Two findings decided it, and neither is
visible in a type specimen:

- **Gentium Book Plus clipped "Sovereign Wealth Fund"** in the Budget category rail — a taller line box
  against a `fixedHeight` button, same string, same style. Pagella fits it.
- **Vollkorn sets old-style (text) figures.** `$29.0T` and `37,00%` render with x-height numerals that do
  not align down a column. In a game whose every screen is a table of numbers, that is disqualifying —
  and it only shows up on a real stat tile.

Literata was competitive but is a variable font (see below).

## Candidate notes

- **Gentium Book Plus** — SIL's own humanist serif, drawn for extended reading with large counters. The
  closest match to the stated direction ("stays readable at 13–15px"), and the only candidate here shipped
  as a true Regular + Bold pair.
- **TeX Gyre Pagella** — a metric-compatible Palatino clone, so it is the literal answer to
  "Palatino-class". OTF/CFF outlines rather than TrueType.
- **Literata** and **Vollkorn** — both are **variable fonts**, and Unity's IMGUI dynamic-font path renders
  a variable font at its default instance only. They are here as comparison candidates; picking either for
  production means fetching a static instance instead.
- **Courier Prime** — a typewriter face redrawn for screen and print, specifically to fix Courier New's
  thinness. Reserved for document artifacts, never body text.

## Character coverage — verified, not assumed

The game needs Basic Latin plus exactly two characters beyond it, both in user-facing strings:

- `U+2212` MINUS SIGN — every negative credit rating (`AA−`, `A−`, `BBB−`, `BB−`, `B−`) and every negative
  delta pill in `PoliSimWidgets`. **Not the ASCII hyphen, and many fonts omit it.**
- `U+00B1` PLUS-MINUS SIGN — the margin-of-error text on every policy-preview panel.

All six country names and all 18 generated minister/Fed-chair names are pure ASCII, despite reading as
continental — `Wei-Lin Tanaka`, `Amara Osei-Bonsu`, `Elena Voskresenskaya` and the rest carry no
diacritics. Every other non-ASCII character in the codebase (`—`, `–`, `×`, `⚠`) appears only in comments.

**All five fonts were checked against this set and all five pass**, ASCII 0x20–0x7E plus both required
symbols. Re-run that check before adding or swapping any font — a missing `U+2212` would show as a blank
box on the credit rating readout, which is a display the player is meant to trust.
