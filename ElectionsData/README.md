# ElectionsData — the inert sourced catalogs (overnight 2026-08-28→29; E-1)

**INERT BY CONSTRUCTION (R-N2):** this folder sits OUTSIDE `Assets/` — Unity never imports it,
no gameplay path can reach it, and nothing in the live game reads it. It is the elections
track's data spine, wired to nothing until item 10's own pass wires it deliberately.

## The three data classes (R-N4, amending R-EL3 for the night)

- **SOURCED** — filled only under the full cross-check gate (R-K9 precedent): primary source
  URL, publisher, access date, vintage, basis, `[PROVISIONAL]` until a second session
  re-verifies. Official electoral-authority returns, statute-grade electoral rules, named
  expert-survey positions, named salience surveys.
- **AUTHORED-DRAFT** — game fiction only (action costs, staff bonuses, candidate attributes),
  marked `[AUTHORED-DRAFT]` with a one-line rationale, never dressed as researched. None ships
  as sourced. (None written tonight unless a chain constant demanded one — see the morning
  report's call log.)
- **DERIVED** — computed from the model's own seeds or from SOURCED values, formula stated.

## Standing constraints, cited not re-derived (R-EL5)

From `POLISIM_SEED_DATA_MACRO_OVERHAUL.md` Part 5 (the four allocator results and their
constraints — branch-side claims, to be reproduced in C# before being relied on):

1. **Vote COUNTS, never published percentages.** Germany 2025: a one-decimal share is enough to
   move a Bundestag seat (CDU exact only within 22.55–22.58 %). Every national table here
   carries absolute counts wherever the source publishes them; a shares-only table is marked
   `[SHARES-ONLY]` and billed.
2. **Poland and Italy allocate per constituency, or not at all.** National D'Hondt is a
   different, more proportional system than 41 × district D'Hondt (the recorded 70-seat error).
   No national-allocation figure for either country is presented as a modelled chamber.
3. **Sweden 2022 national modified Sainte-Laguë (first divisor 1.2) is the one exact anchor** —
   exact for the structural reason that the 39 adjustment seats exist to make the national
   result proportional. The property does not generalise (Sweden 2014's six-seat error).
4. Formatting: every file in this folder is written and parsed **InvariantCulture** (the B3
   decimal-comma defect recorded in the same doc — sv-SE machine).

## Layout

One folder per country (`sweden/ poland/ germany/ france/ italy/ usa/`), plus `positions/` and
`salience/`. Every data file opens with a source-register header block (source, accessed,
vintage, basis, class). `DATA_BILL.md` at this level records what the night could not fill,
per country, with candidate sources.
