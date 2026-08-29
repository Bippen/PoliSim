# Sweden — Riksdag turnout 2002–2022 [SOURCED] [PROVISIONAL]

Class: SOURCED (R-N4). Turnout = ballots cast (incl. blank/invalid) ÷ eligible voters, val.se's
"valdeltagande" basis (the same basis as `returns_2022.md`'s 84.21 %).

| election | turnout | note |
|---|---|---|
| 2002 | 80.11 % | the lowest of the series |
| 2006 | 81.99 % | |
| 2010 | 84.63 % | |
| 2014 | 85.81 % | matches `priors/previous_elections.md` (85.81) |
| 2018 | 87.18 % | matches `priors/previous_elections.md` (87.18); the highest |
| 2022 | 84.21 % | matches `returns_2022.md` (6,547,801 of 7,775,390) |

**Source:** Valmyndigheten, election results archive — https://www.val.se/valresultat/riksdag-region-och-kommun/2022/valresultat.html
and the historical archive at https://historik.val.se/ (per-election "Valdeltagande" rows).

**Caveat (R-K9):** the 2002–2010 figures were written 2026-08-29 from the recorder's knowledge of
the published series and are `[PROVISIONAL]` until read back from val.se; the 2014, 2018 and 2022
figures agree with the two sourced files already on disk. **Used for:** W-B11's "historically
plausible bounds" — the national turnout after any GOTV must stay within [80.11 %, 87.18 %]
widened by two points; the harness states which bound it tests.

**Billed:** per-valkrets eligible counts (val.se publishes them per valkrets in the same
workbook as the votes — `2018_R_per_valdistrikt.xlsx` has "Röstberättigade" per district); until
they are on disk, eligible per valkrets is DERIVED as valid votes ÷ the national turnout.
