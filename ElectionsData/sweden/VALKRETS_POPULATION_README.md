# Population by Riksdag valkrets and age, 31 December 2024 — the per-valkrets marginals C-D1 billed

Built 2026-09-02 for F3's second clause. Two source files, both the publisher's own bytes, and two
derived files, both re-derivable by `Tools/build_valkrets_population.sh` (which is the record of the
method); `sha256sum` of the four is the check.

## Sources

- **`scb_BefolkningNy_2024_municipality_age_sex.json`** — SCB PxWeb API v1, table
  `START/BE/BE0101/BE0101A/BefolkningNy`, contents `BE0101N1` (Folkmängd), all 290 municipalities
  (4-digit region codes), single years of age 0–99 and 100+, both sexes, year 2024 (population at
  31 December 2024). 58 580 rows. Fetched by POST; the query is in the build script.
- **`riksdagen_vallag_2005_837.html`** — Vallagen (2005:837) as published on riksdagen.se, whose 4 kap.
  2 § (lydelse enligt Lag 2014:1384) lists the 29 Riksdag valkretsar: 22 are a county each, and seven
  are municipality lists — Stockholms kommun and the rest of Stockholms län; Malmö kommun and Skåne's
  three (västra 8, södra 11, norra och östra 13 municipalities); Göteborgs kommun and Västra Götaland's
  four (västra 11, norra 14, södra 8, östra 15). The statute's own lists, not a secondary table.

## Derived

- **`valkrets_municipalities_2024.csv`** — municipality code → valkrets (number and the statute's name).
  A county's municipalities map to the county's valkrets; the seven split valkretsar map by the
  statute's lists, matched to SCB's municipality names exactly or with the statute's genitive -s
  removed ("Lunds" → "Lund"). ⚠ Reconciled to the statute: each split valkrets holds exactly the
  number of municipalities the statute lists (8, 11, 13; 11, 14, 8, 15), Skåne's 33 and Västra
  Götaland's 49 are all accounted for, and all 290 municipalities map to exactly one valkrets.
- **`valkrets_population_by_age_2024.csv`** — the 29 valkretsar × 21 five-year bands (0–4 … 100+, the
  cohort substrate's own bands) + total, both sexes summed. ⚠ Reconciled to the publisher: the 29
  totals sum to **10 587 710**, which is SCB's own national figure for 2024 (Riket, `tot` age, men
  5 324 785 + women 5 262 925), fetched separately in the same session.

## What it is for, and what it is not yet

This is the age marginal per valkrets — the input the voter-group VIEW (`CohortVoterGroups`, C-D1's
substrate view, F3) needs to become a per-region view rather than a national one. Nothing on the game
path reads it yet; that consumer is F3's build, and this file lands as a discharged bill the way the
participation and tax sources did. ⚠ Two things it does not carry: education and income per
municipality (the other two marginals C-D1 billed, SCB UF0506 and HE0110 — still billed), and the
ELIGIBLE electorate per valkrets (citizenship and age 18+; `SwedishValkretsReturns2022.Eligible` has
the 2022 electorate from Valmyndigheten, which is the honest denominator until an SCB citizenship cut
is fetched).
