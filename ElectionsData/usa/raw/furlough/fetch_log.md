# fetch_log - ElectionsData/usa/raw/furlough/ (PS-6 US-22c, Elias's ruling J7 (iv): furloughs reach unemployment as BLS counts them), 2026-10-20

Command: `curl -s -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36" -o <file> "https://web.archive.org/web/20261018000000id_/<url>"` (the Internet Archive's raw replay, the closest capture); bytes stored as received. Digests in `SHA256SUMS.txt`.

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-19T23:58:53Z | 403 | 1321 | [NOT KEPT] "Access Denied" | https://www.bls.gov/news.release/archives/empsit_11082013.htm (direct) |
| 2026-10-19T23:58:53Z | 403 | 1321 | [NOT KEPT] "Access Denied" | https://www.bls.gov/news.release/archives/empsit_02012019.htm (direct) |
| 2026-10-19T23:58:58Z | 200 | 569104 | wayback_empsit_11082013.htm | https://www.bls.gov/news.release/archives/empsit_11082013.htm (capture 20260819193950) |
| 2026-10-19T23:59:04Z | 200 | 1081192 | wayback_empsit_02012019.htm | https://www.bls.gov/news.release/archives/empsit_02012019.htm (capture 20260819193702) |

Notes
- **bls.gov answered 403** ("Access Denied") to this machine directly; both releases are read from the Internet Archive's raw replay of bls.gov's own responses.
- **BLS's treatment, in its words** (the box note of each release, October 2013 and January 2019): "In the household survey, individuals are classified as employed, unemployed, or not in the labor force based on their answers to a series of questions about their activities during the survey reference week. Workers who indicate[d] that they were not working during the entire survey reference week and expected to be recalled to their jobs should be classified [in the household survey] as unemployed on temporary layoff." Each release's household text: "Among the unemployed, the number who reported being on temporary layoff increased by 448,000 [October 2013] / 175,000 [January 2019]. This figure includes furloughed federal employees who were classified as unemployed on temporary layoff under the definitions used in the household survey." The reference week: "In the household survey, the reference period is generally the calendar week that includes the 12th of the month." The establishment survey counts the furloughed as employed (paid for the pay period); the game reads the household survey's rate.
- **Not taken:** each box note adds that some furloughed workers were recorded as "employed but absent from work", a misclassification BLS leaves as recorded. The game counts every furloughed worker as BLS's definition classifies them, not as the survey's responses happened to record them (DECLARED, `Assets/Scripts/Simulation/UsLapse.cs`).
- **The headcount** is CBO's, already on disk: CBO 61823 (`../lapse/house_arrington_61823_quantitative_effects_of_a_shutdown.pdf`, quoted in `ElectionsData/usa/debt_limit.md`), "650,000 employees will be furloughed, on average".
