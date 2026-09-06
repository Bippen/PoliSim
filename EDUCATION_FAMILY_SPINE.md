# The education family's data spine — P5-C3, the part that does not wait on Design (2026-09-05)

> **BUILT 2026-09-06, `COMPLETED.md` §338, on Design's grammar (board 9c, inherited by shape).** The seeds below are the family's (`EducationFamily.Seed`), the §5 lines its couplings (`EducationFamily.TargetsFor`, every constant [AUTHORED-DRAFT]); PISA and the graduation rate are FETCHES with no score (ruled 2026-09-06) and their rows print the word; students per teacher is the card's key, not a page row (9c). The feedback proposed in §5 (attainment → the productivity trend) is NOT built.

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

**BUILT 2026-09-07 (overnight), `COMPLETED.md` §348 - the education feedback pass; the terms and their sourced gaps are §8 below.**

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

## 7. The fetches, 2026-09-06 (overnight) — PISA and the graduation rate under the cross-check gate: neither passed; the figures reached are recorded, the rows keep the word

**PISA 2022.** The OECD's own tables (Volume I, Annex B1: Tables I.B1.2.1–3, the country means with standard errors) sit behind the www host's bot check (HTTP 403 on the publication page); `webfs.oecd.org/pisa2022/` serves only the microdata (SAS/SPSS student, school and teacher files - the means could be computed from PV1–PV10 with W_FSTUWT, a computation this machine did not attempt without a decoder for .sav); NCES's international-comparison pages returned 404 at the paths tried. **One transcription reached:** Our World in Data's grapher series (sourced to the OECD PISA database), 2022 means: mathematics - Poland 489.0, Sweden 481.8, Germany 474.8, France 473.9, Italy 471.3, USA 464.9; reading - USA 503.9, Poland 488.7, Sweden 487.0, Italy 481.6, Germany 479.8, France 473.9; science - not on OWID. The World Bank (LO.PISA.MAT/REA/SCI) holds 2015 and 2018 only (2018 mathematics: Poland 515.6, Sweden 502.4, Germany 500.0, France 495.4, Italy 486.6, USA 478.2 - agreeing with OWID's 2018 to the tenth, which cross-checks the transcription channel, not the 2022 figures). **Gate: not passed** (one transcription of 2022, no science). **Billed:** OECD PISA 2022 Results (Volume I), Tables I.B1.2.1, I.B1.2.2, I.B1.2.3 - mean score in mathematics, reading, science, all students; or NCES `nces.ed.gov/surveys/pisa/pisa2022/` international tables. The row prints *to fetch* until either lands.

**Graduation rate.** No OECD dataflow under OECD.EDU.IMEP carries a graduation RATE (117 flows listed; the UOE flows hold enrolments, personnel, entrants and finance). Eurostat `educ_uoe_grad01` holds upper-secondary GRADUATES by age (2022, all ages, ISCED 3: Germany 674 026, France 905 768, Italy 556 900, Poland 386 679, Sweden 97 472) and `demo_pjan` the population aged 18 on 1 January 2023 (785 932 / 828 943 / 586 339 / 334 118 / 119 196); the gross ratio (all graduates ÷ one cohort) is 85.8 / 109.3 / 95.0 / 115.7 / 81.8 % - **above 100 for France and Poland**, because the count includes adults and second qualifications, so it is NOT the row's quantity (first-time graduates as % of the population at the typical age, EAG indicator B3) and cannot stand in for it. **Gate: not passed. Billed:** OECD Education at a Glance 2024, Table B3.1 (first-time upper secondary graduation rates), behind the www host; NCES ACGR for the USA is a different definition. The row prints *to fetch*.

## 8. The feedback pass, 2026-09-07 (overnight) — two terms, both DERIVED from the country's own sourced gaps, none authored

**The stock moves only where the leavers move** (§5: one cohort a year at the leavers' level against the seed's), so the USA's stock holds and its terms stay zero, stated on the diagnostic.

**Participation** (`EducationFamily.ParticipationTerm`, points on the 15+ rate, in `MacroSystem.ApplyLaborForceParticipationRate`'s target beside the health term): −(below-upper-secondary share now − seed) ÷ 100 × the country's own activity gap between ISCED 0–2 and ISCED 3–4 × the 25–64 share of the 15+ population from the pyramid. The gap, read at the source:

| country | activity rate 25–64, ISCED 0–2 | ISCED 3–4 | ISCED 5–8 | gap 0–2 → 3–4 (points) | source |
|---|---|---|---|---|---|
| Sweden | 78.2 | 88.7 | 93.7 | 10.5 | Eurostat `lfsa_argaed` 2024 |
| Germany | 69.9 | 85.7 | 90.7 | 15.8 | idem |
| France | 62.0 | 80.0 | 91.4 | 18.0 | idem |
| Italy | 60.5 | 78.2 | 87.5 | 17.7 | idem |
| Poland | 53.6 | 77.9 | 92.8 | 24.3 | idem |
| USA | 47.4 (less than a high-school diploma) | 56.9 (high-school graduates, no college) | 72.6 (bachelor's and higher) | 9.5 | BLS LNS11327659 / LNS11327660 / LNS11327662, 25+, 2024 monthly means (the OECD's 25–64 table sits behind the www bot check) |

Kept: `PoliSim-captures/sources/eurostat_lfsa_argaed_2024.txt`, `bls_lfpr_by_education_2024.json` (series titles read back at data.bls.gov).

**The productivity trend** (`EducationFamily.ProductivityTrendTerm`, points of trend productivity growth, in `MacroSystem.ApplySectorGrowthEffect`'s ledger under its existing all-sources ceiling, lagged one turn by construction): 100 × ln(W now ÷ W a year ago), W the attainment-weighted index of earnings relative to upper secondary = 100 (`EducationFamily.WageIndex`), clamped ±0.5 a year. The wage gap between attainment levels is read as the marginal-product gap - the Mincer reading, the stated approximation. The relative earnings, read at the source:

| country | below upper secondary | upper secondary or post-secondary non-tertiary | tertiary | source, year |
|---|---|---|---|---|
| Sweden | 77.9 | 100.5 | 124.3 | OECD EAG `DSD_EAG_LSO_EA@DF_LSO_EARN_REL_UPPER`, 25–64, all workers, 2024 |
| Germany | 79.2 | 105.2 | 155.9 | idem, 2024 (the base 100 is upper secondary alone in Germany's row) |
| France | 89.6 | 100 | 155.8 | **not in the OECD flow** - Eurostat SES 2022 `earn_ses22_16`, mean hourly earnings ISCED 0–2 / 3–4 / 5–8 = 14.60 / 16.30 / 25.40 EUR, enterprises of 10+, B–S excluding O; a different earnings concept, stated |
| Italy | 76.7 | 100.0 | 139.2 | OECD, 2023 |
| Poland | 88.2 | 100.2 | 150.9 | OECD, 2024 |
| USA | 72.5 | 100.0 | 174.4 | OECD, 2024 |

**The cross-check, stated rather than passed:** on the four countries both flows cover, Eurostat's 2022 hourly ratios (below ÷ upper: DE 0.68, IT 0.83, PL 0.85, SE 0.90; tertiary ÷ upper: 1.63, 1.50, 1.76, 1.25) agree with the OECD's in direction on every pair and differ by 4–12 points - annual earnings of all workers against hourly earnings of employees in enterprises of ten or more. The OECD reading is the seed where it exists (one concept for five); France's is Eurostat's, and the row says so. Kept: `oecd_lso_earn_rel_upper_six.csv` (the six filtered from the 114 MB flow), `eurostat_earn_ses22_16_by_attainment.txt`.

**Not built, stated:** PISA and the graduation rate still have no score (§7), so nothing here reads them; the term is the stock's, which is the leavers'.
