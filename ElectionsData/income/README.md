# Income by age — the income dimension's sources (S1 of the backlog plan, 2026-09-12)

Fetched 2026-09-12 for F4-1 (`POLISIM_BACKLOG_PLAN.md`, ruled `COMPLETED.md` §474): the series that give the cohort substrate an income dimension for six countries. Every file under `PoliSim-captures/sources/income/` is the authority's own, unedited; every figure in the CSVs beside this README is DERIVED from those files by `Tools/income_prep.pl` (`perl Tools/income_prep.pl PoliSim-captures/sources/income ElectionsData/income`), never typed. Nothing in the runtime reads these files yet - F4-1's generator will, and `GeneratedCatalogCheck` will hold its catalog to their digests then.

## The three files

| file | what | source | concept |
|---|---|---|---|
| `income_by_age_2024.csv` | mean and median income per age class, six countries | Eurostat `ilc_di03` (the five; survey year 2024 = income year 2023; sex T; EUR) · US Census CPS ASEC 2024 table PINC-01 (income year 2023; both sexes, all races) | ⚠ **two concepts, stated per row**: Eurostat's is EQUIVALISED NET household income per person (the OECD-modified scale); the CPS's is TOTAL MONEY INCOME per person 15+ with income, not equivalised and before tax. The per-band SHAPE (mean ÷ median) is what F4-1 reads from each; the levels are not compared across the two |
| `income_deciles_2024.csv` | the national decile cut-off points, the five | Eurostat `ilc_di01` (statinfo TC, EUR, 2024) | DS-2 (c): printed as a cross-check beside the per-band shape, **never fitted to** |
| `us_income_classes_by_age_2023.csv` | the CPS class table by age band: counts (thousands) per $2,500 class to $100,000 and over, with the median, the mean and the Gini per band | CPS ASEC 2024 PINC-01, the "Age" block | the USA's validation of the per-band shape, as SCB's `../sweden/valkrets_income_by_age_class_2024.csv` (HE0110, persons 16+ by class × age) is Sweden's |

Eurostat's age classes in `ilc_di03`: TOTAL, Y16-24, Y25-49, Y50-64, Y65-74, Y_GE65, Y_GE75 and the coarser cuts (Y16-64, Y_GE16, Y18-24, Y25-54, Y55-64, Y_LT65 …) - all of them are carried; F4-1 picks the finest partition of 16+ that every country reports. The CPS bands are ten-year with five-year sub-bands (15–24, 25–29 … 70–74, 75+).

## The form F4-1 derives (DS-2, ruled)

Per age band a log-normal whose σ is pinned by the band's mean over its median (σ² = 2 ln(mean ÷ median)) and whose scale is the median - DERIVED, no authored constant; the national deciles printed beside as the cross-check; the SCB and CPS class tables printed against the shape as its validation. `Gini` stays a calibrated gate with its writer unchanged (DS-2b).

## The files, by digest (sha256, first sixteen hex; under `PoliSim-captures/sources/income/`)

| file | sha256 (16) | size |
|---|---|---|
| `hinc02_10_1.xlsx` | `20f78cc9004228ea` | 13588 bytes |
| `ilc_di01_2024.json` | `c28831d9b5200d77` | 5705 bytes |
| `ilc_di03_2024.json` | `17828ee00be52d79` | 6995 bytes |
| `pinc01_1_1_1.xlsx` | `dd71833915ae4e3b` | 27143 bytes |

