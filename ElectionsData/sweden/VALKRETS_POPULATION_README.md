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
substrate view, F3) needs to become a per-region view rather than a national one. It reaches the code
through the generated catalog `SwedishValkretsPopulation2024` (`ValkretsPopulationCatalogGenerator`;
`GeneratedCatalogCheck` re-derives the digests of both derived files and asserts every name joins the
returns catalog) and `CohortVoterGroups.ForValkrets`, whose per-valkrets groups sum to 1 in all 29
(`VoterGroupViewDiagnostic`). ⚠ Nothing on the GAME path reads the per-valkrets view yet: the campaign
has no age-group mechanic, and that consumer is the per-group design, which waits on per-group loyalty.
⚠ Two things it does not carry: education and income per municipality
(the other two marginals C-D1 billed, SCB UF0506 and HE0110 — still billed), and the ELIGIBLE
electorate. **Its 18+ count is RESIDENTS, not the electorate:** against Valmyndigheten's 2022 roll it
runs 1.03–1.09 in the counties and 1.11–1.16 in Stockholm (kommun and län), Uppsala, Göteborg and
Malmö — non-citizen adults plus two years' growth — so the campaign's mobilisable electorate per valkrets is the roll
(`SwedishValkretsReturns2022.Eligible`), and this file's job is the AGE STRUCTURE of each valkrets's
voters, not their number. An SCB citizenship cut is the fetch that would close the gap.
