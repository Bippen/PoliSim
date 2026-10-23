# US-35: the declared departures from D-US part two's boards 30a-30d (wave/us35, 2026-10-23)

The implementer's list, numbered for the record to cite. Each departure gives what the board draws, what the build does, and why.

1. **The role chip stays in the Desk's masthead, beside the year** (§685's place, board 21e), not in a v3.5 top bar. Its words stay in the chip's capitals. The precedence, the mark on its ink and the tag (DIVIDED, LEAVES / TAKES OFFICE) are built as 30c draws them.
2. **The lapse chip takes the LIVE caption's place** at the masthead's right, not "beside Paused": the build's Paused is the rail's chip.
3. **"Funded to", not "a CR to".** The model does not tell a continuing resolution's horizon from an appropriation's (both fund to a fiscal year's end).
4. **The restored state comes from a controller memo, not a saved field** (`LapseMemo`). It is keyed on the country object, so a load, a new game or a switch of country starts it afresh (the review's M4). A game loaded on the restoration's own day shows no restored chip.
5. **The dated rows** (THE PRESIDENT / CONGRESS / YOUR NOMINEE) **are kept under the view**, not retired into the provenance chip and the next-vote row. The nominee row carries an action (US-28's NEXT CHOICE). Retiring them is asked.
6. **The stations' third line (the board's sub) is each station's slip.** The ten days are drawn only on the desk: a vetoed record keeps no presentment day. On a vote on a veto, the pocket veto's line stays on the count's slip (§853), because the strip takes the room. These keep the plate's height at 16:9.
7. **The Docket's "LAW UNSIGNED IN N DAYS" is in the card's line, not a chip:** the Docket card has no chip slot. On the last day it reads LAW UNSIGNED TOMORROW (the review's M3).
8. **The lines card splits the override into a House row and a Senate row.** The right column is narrower than the board's. A 50-50 Senate reads "holds it with the VP" (the review's L2). An odd Senate prints no equal division (L3).
9. **The class rows' marks:** a class is GAME'S once the game has held its regular election. A seat the game elected to an unexpired term is brass-edged. Ohio's and Florida's 2026 specials (K14) are not in the catalog, so no game special is held for them.
10. **Party marks are drawn as light silhouettes on their ink.** Until US-34 installs the REP and DEM inks, `PoliSimTheme.Party` returns the Neutral for both. The bars' codes then tell the sides apart, and the class cells do not (the review's L4, which resolves when US-34 lands).

Also from the source, as the review judged them:
- No absence is modelled, so an override's base is the chamber seated.
- No Fed-chair nomination is held as pending, so the card says so and the count is BILLED.
- An overridden bill's button reads File.
