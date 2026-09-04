# The education family's data spine — P5-C3, the part that does not wait on Design (2026-09-05)

**What this is.** P5-C3 (education) is the second society-stat family in the catalog's order (`SOCIETY_STATS_CATALOG.md` § 2). Its build - seeded for six, coupled, displayed as instruments, family explained - waits, like health's, on the grammar D15 item 3 asks Design to draw (E-17 not pasted at this writing). This is the family's data spine: the seeds sourced for six where a series exists, absent and stated where it does not, and the coupling proposed. **Nothing is built from it.**

**Fetched 2026-09-05** through the Eurostat JSON-stat API (`Invoke-RestMethod`, decoded by index) and the OECD SDMX API (CSV, parsed header-aware - the label columns carry commas). Every value is the latest observation the flow returned, sex total; the family's pass re-fetches and records digests.

---

## 1. Attainment — six of six (OECD), five cross-checked against Eurostat

**OECD Education at a Glance**, dataflow **DSD_EAG_LSO_EA / DF_LSO_NEAC_DISTR_EA** (adults' educational attainment distribution), 25–64, sex total, unit PT_POP_SEX_AGE (% of the age group). Eurostat **edat_lfse_03** (population by educational attainment, 25–64, unit PC) beside it for the five.

| country | year (OECD) | below upper secondary (ISCED 0–2) | upper secondary or post-secondary (ISCED 3–4) | tertiary (ISCED 5–8) | Eurostat 2025: at least upper secondary (ED3-8) · tertiary (ED5-8) |
|---|---|---|---|---|---|
| Sweden | 2025 | 14.0 | 35.2 | 50.8 | 89.0 · 50.8 |
| Germany | 2025 | 14.1 | 50.5 | 35.5 | 85.8 · 35.4 |
| France | 2024 | 16.1 | 40.6 | 43.4 | 84.7 · 44.8 |
| Italy | 2025 | 33.0 | 44.7 | 22.3 | 67.0 · 22.3 |
| Poland | 2025 | 5.1 | 54.9 | 40.0 | 94.9 · 40.0 |
| USA | 2025 | 7.7 | 40.1 | 52.2 | - (Eurostat does not cover the USA) |

The two sources agree within a point wherever both exist (Sweden 50.8 / 50.8; Italy 22.3 / 22.3; Poland 40.0 / 40.0). **The seeds the family takes:** *general attainment* = at least upper secondary, 25–64 (100 − ISCED 0–2); *tertiary attainment* = ISCED 5–8, 25–64. One source for six: the OECD flow; Eurostat is the cross-check, not a second book.

## 2. Early leavers — five of six, the USA absent and stated

**Eurostat edat_lfse_14** (early leavers from education and training, 18–24, % of the age group, all labour statuses). The USA is not in Eurostat, and the NCES status dropout rate (16–24, not enrolled and without a credential) is a different definition on a different age band with no API - **absent and stated**, not estimated.

| country | 2022 | 2023 | 2024 | 2025 |
|---|---|---|---|---|
| Sweden | 8.8 | 7.4 | 7.2 | 6.7 |
| Germany | 12.5 | 13.0 | 13.5 | 13.1 |
| France | 7.6 | 7.6 | 7.9 | 7.2 |
| Italy | 11.5 | 10.5 | 9.8 | 8.2 |
| Poland | 4.7 | 3.7 | 4.1 | 4.0 |
| USA | absent | absent | absent | absent |

**The seed the family takes:** *dropout* = early leavers 18–24 for the five; the USA's instrument prints ABSENT with the reason, and the youth-unemployment pull (`EconomyState.YouthUnemployment`) reaches attainment for it directly.

## 3. Students per teacher — six of six

**OECD Education at a Glance**, dataflow **DSD_EAG_UOE_NON_FIN_PERS / DF_UOE_NF_PERS_STR** (ratio of students to teaching staff), all institutions, full-time equivalents, measure STU_PERS, unit ST_TCHR (students per teacher).

| country | year | primary (ISCED 1) | lower secondary (ISCED 2) | upper secondary (ISCED 3) |
|---|---|---|---|---|
| Sweden | 2024 | 12.4 | 11.2 | 13.1 |
| Germany | 2024 | 15.2 | 12.9 | 11.9 |
| France | 2023 | 18.1 | 14.7 | 11.4 |
| Italy | 2024 | 10.5 | 10.4 | 10.6 |
| Poland | 2024 | 13.0 | 9.5 | 11.9 |
| USA | 2024 | 13.7 | 14.3 | 15.2 |

**The seed the family takes:** *students per teacher* = the primary and lower-secondary figures, shown as two; the most direct spending readout the family has - the education line over the 0–19 cohort buys teachers, and this is what they teach.

## 4. Academic score and graduation rate — FETCH TO DO, the source named, no figure here

- **Academic score = PISA 2022 mean scores** (mathematics, reading, science; the mean of the three as the headline). PISA is NOT on the OECD SDMX API (no dataflow under any agency names it; checked 2026-09-05); the source is the OECD PISA 2022 Results, Volume I, Annex B1 tables (the country means with standard errors), downloaded as the OECD publishes them, one vintage. Nothing is recalled here: the pass fetches the file, records its digest, and seeds from it.
- **Graduation rate = upper-secondary graduation rate** (Education at a Glance indicator B3, first-time graduates as % of the population at the typical age). The SDMX flows hold graduates as COUNTS (DF_UOE_NF_STUD_TOTALS), not the rate; the rate is in the EAG tables. Same rule: fetched on the pass, or derived from counts over the cohort with the derivation stated.

## 5. The coupling proposed — every line `[AUTHORED-DRAFT]`, none built

The family reads: the education spending line per pupil (`SpendingCategory.Education` and the US education lines over the 0–19 cohort, `SpendingDriver.Youth0To19` - P5-B2's driver, so a smaller cohort with the same line is more money per pupil), the portfolio's effectiveness when P5-C7 lands (spending per pupil against its seed standing in), and `EconomyState.YouthUnemployment` as the pull out of school.

| metric | moves with | direction | the proposed line, to be measured on the pass |
|---|---|---|---|
| students per teacher | spending per pupil | down with spending (more teachers per pupil) | the immediate readout: ratio_target = ratio_seed × (spend-per-pupil_seed ÷ spend-per-pupil)^s, fast reversion |
| academic score (PISA) | students per teacher, with a lag of years; effectiveness | up as the ratio falls and effectiveness rises | a slow drift toward a target set by the ratio and effectiveness; the elasticity a draft, the lag the length of schooling (a cohort's worth of years), CHECKED on the pass against the cross-section of six |
| early leavers | youth unemployment (the pull), spending per pupil (the push) | up with youth unemployment, down with spending | reversion toward a target in both terms |
| graduation rate | the complement of early leaving, with the ratio | up as leavers fall | derived where the rate is not seeded; seeded where it is |
| attainment (25–64) | the stock: this year's graduates and leavers entering the 25–64 cohort while the old leave it | slow, one cohort a year | a stock-flow line over F2's substrate - the one metric here that cannot move fast, and the instrument says so |

**Feedback to the model, proposed and NOT built:** attainment → `Country.ProductivityTrendGrowthRate` (a small, lagged term on the productivity trend P5-B7 seeded), the one channel through which education reaches output - measured against the trajectory suite when it lands, never before.

## 6. Display rows, drafted for the board (the order of the six figures: Sweden, Germany, France, Italy, Poland, USA)

| row | figure | unit | source line (caption mono) | band | arrows to |
|---|---|---|---|---|---|
| Students per teacher · primary | 12.4 / 15.2 / 18.1 / 10.5 / 13.0 / 13.7 | students per teacher | OECD EAG · UOE_NF_PERS_STR · ISCED 1 · 2023–24 | 8 to 20 | the education line per pupil |
| Students per teacher · lower secondary | 11.2 / 12.9 / 14.7 / 10.4 / 9.5 / 14.3 | students per teacher | OECD EAG · ISCED 2 | 8 to 20 | the education line per pupil |
| Academic score | to fetch | PISA mean | OECD PISA 2022 · Vol. I · Annex B1 | 350 to 550 | students per teacher; effectiveness |
| Early leavers | 6.7 / 13.1 / 7.2 / 8.2 / 4.0 / absent | % of 18–24 | Eurostat edat_lfse_14 · 2025 | 0 to 20 | youth unemployment; the education line |
| Graduation rate | to fetch | % at typical age | OECD EAG · B3 | 60 to 100 | early leavers |
| At least upper secondary · 25–64 | 86.0 / 85.9 / 83.9 / 67.0 / 94.9 / 92.3 | % of 25–64 | OECD EAG · LSO_NEAC_DISTR_EA · 2024–25 | 50 to 100 | the stock-flow line |
| Tertiary · 25–64 | 50.8 / 35.5 / 43.4 / 22.3 / 40.0 / 52.2 | % of 25–64 | OECD EAG · LSO_NEAC_DISTR_EA | 15 to 60 | the stock-flow line |

"To fetch" and "absent" print as words in the caption face, never as figures. Nothing here is built; the board corrects it, then the pass builds what the board says.
