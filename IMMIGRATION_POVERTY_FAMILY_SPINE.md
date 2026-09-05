# The immigration-and-poverty-depth family's data spine — P5-C6 (2026-09-05)

**What this is.** P5-C6 (immigration and poverty depth) is the fifth society-stat family in the catalog's order (`SOCIETY_STATS_CATALOG.md` § 5). Its sources are Eurostat (three series, JSON-stat, decoded by index), the OECD (the Affordable Housing Database workbook HC3.1 and the Income Distribution Database dataflow) and the US Department of Homeland Security's Office of Homeland Security Statistics (a PDF report) with the Bureau of Labor Statistics for the US labour figure. This spine seeds what the fetched series cover for six, states the rest absent or differently defined, and proposes the coupling. **Nothing is built from it**; the build waits on the one-grammar rule (D15 item 3) like every family.

**Fetched and verified, 2026-09-05** (all kept in `PoliSim-captures/sources/`):
- Eurostat `migr_eipre` (third-country nationals found to be illegally present, persons, TOTAL apprehension), `ilc_li11` (relative at-risk-of-poverty gap, median equivalised income, 60 % line), `lfsi_sup_a` (labour-market slack: underemployed part-time workers, 20–64, thousands and % of population) and `lfsi_emp_a` (employment 20–64, thousands) - JSON-stat 2.0 decoded by dimension index (`eurostat.ps1`), the decoded cells in `eurostat_*.txt`.
- OECD Affordable Housing Database `HC3-1-Population-experiencing-homelessness.xlsx` from webfs.oecd.org (197 990 bytes, zip signature 50 4B 03 04, sha256 `73a435fdf21f888f…`), tables HC3.1.A1 and HC3.1.A2 decoded from the workbook XML with the shared strings resolved (`oecd_hc3_1_a1_decoded.txt`, `oecd_hc3_1_a2_decoded.txt`). The file's name is the OECD's own; `HC3-1-Homeless-population.xlsx` does not exist (404).
- OECD Income Distribution Database, dataflow `OECD.WISE.INE,DSD_WISE_IDD@DF_IDD,1.0`, measure `PG_INC_DISP` (poverty gap), methodology METH2012, definition D_CUR, poverty lines PL_50 and PL_60 - the key order read from the data structure (REF_AREA.FREQ.MEASURE.STATISTICAL_OPERATION.UNIT_MEASURE.AGE.METHODOLOGY.DEFINITION.POVERTY_LINE), the CSV in `oecd_idd_povgap.csv`.
- DHS OHSS, *Estimates of the Unauthorized Immigrant Population Residing in the United States: January 2018–January 2022* (published 18 April 2024; `DHS_OHSS_unauthorized_2018-2022.pdf`, 660 215 bytes, `%PDF` signature, sha256 `ae61e88ab0b5af76…`), its text read by inflating the PDF's streams (`pdftext.pl`; the machine has no PDF renderer).
- BLS series LNS12032194 (employed part time for economic reasons, thousands) and LNS12000000 (employed, thousands), API v2 without a key, 2021–2025, `bls_underemployment.json`; the annual figures below are the means of the twelve monthly values (the API returned no M13 annual row without a key).
- Populations 2023 from the World Bank (SP.POP.TOTL, fetched for P5-C5): Germany 83.29 M, France 68.37 M, Italy 58.98 M, Poland 36.69 M, Sweden 10.54 M, USA 336.76 M - the per-head denominators, stated.

---

## 1. Irregular migration — six of six, TWO definitions, the row says which

The catalog's ruling: the EU five carry a FLOW (third-country nationals found to be illegally present in the year, Eurostat `migr_eipre`); the USA carries a STOCK (the unauthorized resident population estimate, DHS OHSS). They are not the same quantity and are never printed on one axis.

| country | 2021 | 2022 | 2023 | 2024 | 2025 | per 10 000 residents (2024 flow ÷ 2023 population) |
|---|---|---|---|---|---|---|
| Germany | 120 285 | 198 310 | 263 670 | 249 155 | 168 360 | 29.9 |
| France | 117 265 | 115 135 | 118 975 | 142 190 | 159 460 | 20.8 |
| Italy | 92 070 | 138 420 | 194 750 | 108 925 | 82 505 | 18.5 |
| Poland | 12 795 | 10 510 | 16 480 | 16 065 | 17 650 | 4.4 |
| Sweden | 2 635 | 2 455 | 2 510 | 2 965 | 2 495 | 2.8 |

**USA (stock):** 10.99 million unauthorized immigrants residing on 1 January 2022 (OHSS), against 10.5 million in January 2020 and 11.6 million in 2010; the report notes Pew's 10.94 million for 2022 beside it. Per 10 000 residents: 326 (÷ the 2023 population, stated - the report's own denominator is the 2022 ACS). No January 2021 estimate exists (the ACS was disrupted; the report says so).

**The seeds the family takes:** *irregular migration* = the 2024 flow per 10 000 for the five, the 2022 stock per 10 000 for the USA, each row captioned with its definition and year. **Absent and stated:** a stock estimate for the five (Eurostat publishes none; national estimates exist for Germany and Italy but not on one method) and a flow for the USA on the Eurostat definition (CBP encounters are a different quantity - border, not residence).

## 2. Poverty gap — six of six, two vintages that do not agree, stated

Eurostat `ilc_li11`, relative median at-risk-of-poverty gap, 60 % of median equivalised income, % (how far below the line the median poor person sits; the survey year, income of the year before):

| country | 2021 | 2022 | 2023 | 2024 | 2025 |
|---|---|---|---|---|---|
| Germany | 22.5 | 20.3 | 21.5 | 20.4 | 21.7 |
| France | 19.5 | 20.2 | 19.5 | 18.8 | 20.6 |
| Italy | 27.2 | 26.1 | 23.8 | 26.0 | 24.6 |
| Poland | 19.7 | 20.7 | 20.5 | 21.0 | 19.7 |
| Sweden | 20.7 | 21.5 | 24.0 | 23.4 | 23.1 |

OECD IDD `PG_INC_DISP`, poverty line 60 % of median, latest year: USA **37.2** (2023); for the same countries the OECD prints Germany 31.6, France 25.7, Italy 32.6, Poland 27.0 (2023), Sweden 22.7 (2024) - **ten points above Eurostat for Germany and Italy**, five for France and Poland, level for Sweden: the two bodies compute the gap differently (the OECD's methodology 2012 and income definition against EU-SILC's median gap), so the USA's 37.2 is read against the OECD column and never against the Eurostat one.

**The seed the family takes:** *poverty effect* = the Eurostat 2025 gap for the five and the OECD 2023 gap for the USA, each captioned with its source; the instrument prints the OECD figure for the five as a second reading if the board wants one axis. `EconomyState.PovertyRate` (the rate) exists and is coupled already; the gap is the new depth.

## 3. Underemployment — six of six, as a share of employment

Eurostat `lfsi_sup_a` UEMP_PT (underemployed part-time workers, 20–64) over `lfsi_emp_a` (employment 20–64), 2024; the USA from BLS LNS12032194 over LNS12000000 (16+; the means of the 2024 months):

| country | underemployed part-time 2024 (thousands) | employment 2024 (thousands) | % of employment | Eurostat's own % of population 20–64 |
|---|---|---|---|---|
| Germany | 469 | 39 683 | 1.18 | 1.0 |
| France | 1 144 | 27 847 | 4.11 | 3.1 |
| Italy | 555 | 23 028 | 2.41 | 1.6 |
| Poland | 138 | 16 679 | 0.83 | 0.6 |
| Sweden | 175 | 4 867 | 3.60 | 2.9 |
| USA | 4 467 | 161 348 | 2.77 | - (16+, BLS definition: part time for economic reasons) |

**The seed the family takes:** *underemployment* = the % of employment; the US row is captioned with its wider age band and the BLS definition. `EconomyState.Unemployment` is the existing companion.

## 4. Homelessness — six of six, each with its definition and year

OECD Affordable Housing Database, Table HC3.1.A1 (headline estimate, ETHOS Light categories; PIT = point-in-time count):

| country | headline estimate | % of population | per 10 000 | year | count | children | temporary accommodation for asylum seekers | source (the OECD's citation) |
|---|---|---|---|---|---|---|---|---|
| Sweden | 33 269 | 0.33 | 33 | 2017 | PIT | no | not included | National Board of Health and Welfare, national homelessness survey |
| Germany | 262 600 | 0.31 | 31 | 2022 | PIT | yes | not included | BMAS, Homeless Reporting Act report |
| France | 333 000 | 0.49 | 49 | 2022 | PIT | yes | included | DIHAL (2023) estimation |
| Italy | 96 197 | 0.16 | 16 | 2021 | flow | yes | not included | ISTAT permanent census |
| Poland | 30 330 | 0.08 | 8 | 2019 | PIT | yes | not included | Ministry of Family and Social Policy national count |
| USA | 653 104 | 0.19 | 19 | 2023 | PIT | yes | not included | HUD annual point-in-time count |

Table HC3.1.A2 gives the trend the OECD has: Germany 335 000 (≈2015) → 337 000 (≈2018) → 262 600; France 141 500 (≈2010) → 333 000; USA 640 466 → 564 708 → 552 830 → 653 104; Sweden 34 000 (≈2010) → 33 269 (2018 column); Poland 30 700 → 30 330; Italy the one census figure.

**The seed the family takes:** *homelessness* = the per-10 000 figure WITH its definition line (the catalog's caveat: national definitions differ; France counts asylum-seeker accommodation and Sweden excludes children, so the spread between them is partly the definition). Sweden's figure is eight years old and the instrument prints the year.

## 5. Per-capita income and the employment breakdown — exist

Per-capita income is `EconomyState.NominalGdp` over population (P5-B6: in current prices), a derived readout; the employment breakdown is the People page's second pie (§309/§313). Nothing to seed.

## 6. The coupling proposed — every line `[AUTHORED-DRAFT]`, none built

The family reads the Immigration Policy and Border Enforcement dials, the transfer lines and the welfare programmes' generosity, the housing line and `EconomyState.HousingOverburden`, and the labour dials.

| metric | moves with | direction | the proposed line |
|---|---|---|---|
| irregular migration (per 10 000) | the Immigration Policy dial (openness lowers irregular entry by widening legal channels) and the Border Enforcement dial (apprehensions rise, the resident stock falls); the cohort substrate's own inflow term for the stock | mixed, stated per dial | *flow_t = flow_seed × (1 − a × Δopenness) × (1 + b × Δenforcement)* for the five; the USA's stock follows the substrate's net inflow of the unauthorized share; a and b drafts, checked against the six's spread (Germany's 2023 peak was the Ukraine-war year - the seed's year matters) |
| poverty gap (%) | transfer generosity (`WelfareProgram.GenerosityLevel`) and the minimum wage | down with generosity - generosity closes the GAP before it moves the RATE (the catalog's line) | *gap_t = gap_seed × (1 − g × Δgenerosity)* with the rate's existing coupling untouched; g a draft |
| underemployment (% of employment) | the labour dials (overtime regulation raises it, retraining lowers it) and the cycle (the unemployment gap) | up with slack | *under_t = under_seed + h × (u_t − u*) + the dial terms*; h a draft |
| homelessness (per 10 000) | the housing line per head against its seed, `HousingOverburden`, the poverty gap | down with housing spending, up with overburden | *home_t = home_seed × (1 + k × Δoverburden − m × Δhousing-per-head)*, k and m drafts, the definition line carried unchanged |

**Feedback to the model, proposed and NOT built:** none in this pass - these are outcome readouts; the poverty RATE already reaches approval and consumption, and the gap adds depth, not a second channel.

## 7. Display rows, drafted (Sweden, Germany, France, Italy, Poland, USA)

| row | figure | unit | source line (caption mono) | band | arrows to |
|---|---|---|---|---|---|
| Irregular migration | 2.8 / 29.9 / 20.8 / 18.5 / 4.4 · USA 326 (stock) | per 10 000 · 2024 flow · USA 2022 stock | Eurostat migr_eipre · DHS OHSS 2024 | 0 to 40 (five) · a separate stock band for the USA | Immigration Policy; Border Enforcement |
| Poverty gap | 23.1 / 21.7 / 20.6 / 24.6 / 19.7 · USA 37.2 (OECD) | % of the 60 % line · 2025 · USA 2023 | Eurostat ilc_li11 · OECD IDD PG_INC_DISP | 10 to 40 | transfer generosity; minimum wage |
| Underemployment | 3.60 / 1.18 / 4.11 / 2.41 / 0.83 / 2.77 | % of employment · 2024 | Eurostat lfsi_sup_a ÷ lfsi_emp_a · BLS LNS12032194 ÷ LNS12000000 | 0 to 6 | the labour dials |
| Homelessness | 33 / 31 / 49 / 16 / 8 / 19 | per 10 000 · 2017 / 2022 / 2022 / 2021 / 2019 / 2023 | OECD AHD HC3.1.A1 | 0 to 60 | the housing line; overburden |

Two-definition rows print their definition in the caption face; a year older than five years prints as words. Nothing here is built.
