# The society-stats catalog — P5-C1, self-ruled (2026-09-05)

**What this is.** Elias's metric list (`PLAYTEST5_ANNEX_E16.md` §2 - a foreign game's wiki, US-centric; the *breadth* wanted, not the *content*) mapped row by row to this game's six countries under the annex's §3 rules: **KEEP** where a sourced series exists for all six, **MAP** where the US term has a general form, **N/A** with the reason where the six-country frame cannot hold it. Every kept row names its series, the dial or law that reaches it (sourced, or `[AUTHORED-DRAFT]` with the proposed line), its display home and its trajectory-family note. Families are ordered by gameplay impact as §3 ruled: health → education → infrastructure → environment → immigration and poverty depth → effectiveness. **No metric is built from this document** (the C1 row); each family is its own BASELINE pass (P5-C2 to C7), on the one society-stat grammar D15 item 3 asks Design to draw on health.

**What already exists, so it is not asked for twice.** The state carries `EconomyState.PovertyRate`, `EconomyState.Gini`, `EconomyState.LifeExpectancy`, `EconomyState.BirthRate`, `EconomyState.DeathRate`, `EconomyState.CrimeIndex`, `EconomyState.OrganizedCrimeIndex`, `EconomyState.PrisonPopulationRate`, `EconomyState.CorruptionIndex`, `EconomyState.YouthUnemployment`, `EconomyState.Unemployment`, `EconomyState.LaborForceParticipationRate`, `EconomyState.Homeownership`, `EconomyState.HousingOverburden`, `EconomyState.HousePriceIndex`, `EconomyState.Productivity`, `EconomyState.RealWageIndex`, `EconomyState.Population` and the cohort substrate (F2); the Budget carries every revenue and spending line; the law browser carries every law's status; the elections track carries turnout and the voter groups. A row that lands on one of these is marked EXISTS and points at it.

**Sources named by family (the series a KEEP row cites).** Health: OECD Health Statistics (health insurance coverage - population covered for a core set of services, public and primary private; waiting times for elective procedures - mean and median days, the indicators behind the OECD's *Waiting Times for Health Services* report; Health Care Quality Indicators - avoidable hospital admissions, 30-day mortality after AMI and stroke). Education: OECD PISA 2022 (mean scores, mathematics, reading, science); OECD Education at a Glance (upper-secondary graduation rates, indicator B3; student-teacher ratios, D2); Eurostat edat_lfse_03 (attainment 25–64 by level) and edat_lfse_14 (early leavers 18–24), with the US Census CPS educational-attainment tables for the USA. Infrastructure: WEF Global Competitiveness Report 2019, quality of road infrastructure (1–7; the series ended with that edition and is dated as such); IRF World Road Statistics (road network length); TomTom Traffic Index (congestion level per city, aggregated to the country's largest cities - a commercial index with a published method, kept with that caveat). Environment: EDGAR (JRC / IEA) fossil CO₂ per capita, by sector (power industry; transport); Ember Yearly Electricity Data (electricity generation by source and CO₂ intensity of electricity); Eurostat nrg_bal_c and the US EIA for energy sources by type. Immigration and poverty depth: Eurostat migr_eipre (third-country nationals found to be illegally present, per year) and the US DHS Office of Homeland Security Statistics unauthorized-population estimates; Eurostat ilc_li11 (relative median at-risk-of-poverty gap) and the OECD Income Distribution Database poverty gap; Eurostat lfsi_sup_a (underemployed part-time workers) and the OECD labour underutilisation series; OECD Affordable Housing Database HC3.1 (homelessness - definitions differ by country and the row says so). Crime: Eurostat crim_just_job (police officers per 100 000) and the FBI UCR / Bureau of Justice Statistics for the USA; World Prison Brief (the prison population the existing stat was seeded from). Every KEEP row is a FETCH TO DO on its family's pass; nothing in this catalog is a figure.

---

## 1. Health — first, the family D15's grammar is drawn on (P5-C2)

| the list's row | ruling | this game's metric | series (per country, six) | reaches it | display home | family note |
|---|---|---|---|---|---|---|
| Health coverage | **KEEP** | population covered for a core set of services, public and primary private, % | OECD Health Statistics, health insurance coverage | the health spending line (`SpendingCategory.HealthcareAndSocialCare`, the US Medicare and Medicaid lines) through the coupling table `[AUTHORED-DRAFT]`: coverage rises with real health spending per head, ceilinged at 100 | the People page (the society block) and the Health ministry's card | C2 BASELINE; the coverage ceiling is a source fact (Sweden, France, Germany, Italy near 100 public; the USA below, split) |
| Retiree health coverage | **MAP** | coverage of the 65+ cohort (the cohort substrate × coverage; the USA's Medicare population) | derived from the row above and F2's 65+ cohort | the pension and health lines | beside coverage | C2; no separate seed - a derivation, stated |
| Private health care quality | **MAP** | health care quality, one figure: the OECD HCQI composite this game keys (avoidable admissions, 30-day mortality) - not split by payer, the six-country frame does not hold a payer split | OECD Health Care Quality Indicators | health spending per head and the minister's efficiency `[AUTHORED-DRAFT]` | the People page | C2 |
| Medicare quality / Medicaid quality | **N/A** | US programmes; their content is the two rows above (public coverage, quality) | - | - | - | the terms do not travel; the general form is kept |
| (the list's Health tab) life expectancy, birth/death ratio | **EXISTS** | `EconomyState.LifeExpectancy`, `EconomyState.BirthRate`, `EconomyState.DeathRate` | seeded already | already coupled (F2) | People | - |
| wait times (Elias's §1: "healthcare quality wait times") | **KEEP for three, absent and stated for three** | waiting time for elective procedures, mean days from specialist assessment to treatment (cataract, knee replacement, hip replacement - the OECD's procedures) | OECD Health Statistics, waiting times (DF_WAITING): Sweden, Italy and Poland report it; Germany, France and the USA publish no comparable series (fetched 2026-09-05 - the flow holds no rows for the three), so their figure is ABSENT AND STATED, not estimated | the health line's effectiveness (allocated ÷ requested × efficiency, P5-C7) `[AUTHORED-DRAFT]`: waits fall as effectiveness rises above 1, rise below | People; the Health ministry's card | C2; the first metric the effectiveness mechanic will be felt on |

## 2. Education (P5-C3)

| the list's row | ruling | this game's metric | series | reaches it | display home | family note |
|---|---|---|---|---|---|---|
| Academic score | **KEEP** | PISA mean score (mathematics, reading, science; the three shown, the mean of the three as the headline) | OECD PISA 2022 | the education line and the 0–19 cohort `[AUTHORED-DRAFT]`: spending per pupil (the line over the 0–19 cohort) with a lag | People; the Education ministry's card | C3 BASELINE |
| Graduation rate | **KEEP** | upper-secondary graduation rate, % | OECD Education at a Glance B3 | as above | People | C3 |
| Dropout rate | **KEEP** | early leavers from education and training, 18–24, % | Eurostat edat_lfse_14; US CPS status dropout rate | as above; youth unemployment (`EconomyState.YouthUnemployment`) as the pull `[AUTHORED-DRAFT]` | People | C3 |
| Dropout causes | **N/A** | no comparable six-country series on causes | - | - | - | a narrative, not a metric |
| Student/teacher ratio | **KEEP** | students per teacher, primary and secondary | OECD Education at a Glance D2 | the education line over the 0–19 cohort - the most direct spending metric there is `[AUTHORED-DRAFT]` | the Education ministry's card | C3 |
| Higher-education attainment | **KEEP** | tertiary attainment, 25–64, % | Eurostat edat_lfse_03; US CPS | as above with a long lag | People | C3 |
| General educational attainment | **KEEP** | at least upper secondary, 25–64, % | Eurostat edat_lfse_03; US CPS | as above | People | C3 (the same fetch as the row above) |

## 3. Infrastructure (P5-C4)

| the list's row | ruling | this game's metric | series | reaches it | display home | family note |
|---|---|---|---|---|---|---|
| Roads in poor condition | **MAP** | quality of road infrastructure, 1–7 (the WEF survey figure; dated 2019 and the row says so - no later six-country series exists) | WEF GCR 2019 | the infrastructure line and the existing `Country.InfrastructureSpendingGrowthAdjustment` channel `[AUTHORED-DRAFT]`: condition decays at a rate and spending arrests it | People; the Transport ministry's card | C4 BASELINE |
| Road congestion | **KEEP** | congestion level, % extra travel time, the country's largest cities aggregated | TomTom Traffic Index (commercial, published method - kept with the caveat) | the infrastructure line against population growth `[AUTHORED-DRAFT]` | People | C4 |
| Road length | **KEEP** | road network km per 1 000 km² | IRF World Road Statistics | slow; the infrastructure line | the Transport card | C4, low impact - shown, barely moved |
| Energy sources by type | **KEEP** | electricity generation by source, % | Ember; Eurostat nrg_bal_c; US EIA | the energy line and the carbon tax | the Environment block | belongs to C5 (below); listed here because the list put it under Infrastructure |

## 4. Environment (P5-C5)

| the list's row | ruling | this game's metric | series | reaches it | display home | family note |
|---|---|---|---|---|---|---|
| Electricity CO₂ per person (lbs) | **MAP** | CO₂ from the power sector per capita, tonnes (metric; the list's pounds do not travel) | EDGAR, power industry sector, over population | the carbon tax (`TaxType.CarbonTax`) and the energy line `[AUTHORED-DRAFT]`: the tax lowers the generation mix's intensity with a lag | People (the Environment block) | C5 BASELINE; the carbon tax's base (P5-B3 left it on output) moves to this metric when C5 lands |
| Car CO₂ per person (lbs) | **MAP** | transport CO₂ per capita, tonnes | EDGAR, transport sector, over population | the carbon tax; the infrastructure line (public transport) `[AUTHORED-DRAFT]` | People | C5 |
| (Other: environment) | **KEEP** | total fossil CO₂ per capita, tonnes - the headline the two rows above split | EDGAR | as above | People | C5 |

## 5. Immigration and poverty depth (P5-C6)

| the list's row | ruling | this game's metric | series | reaches it | display home | family note |
|---|---|---|---|---|---|---|
| Illegal immigrant count | **MAP** | irregular migration: third-country nationals found illegally present per year (EU five); the unauthorized population estimate (USA) - two definitions, the row says which is which; **absent and stated** for a year a country has no estimate | Eurostat migr_eipre; US DHS OHSS | the Immigration Policy dial (`LaborDial.ImmigrationPolicy`) and the Border Enforcement dial `[AUTHORED-DRAFT]` | People (the migration block, beside `EconomyState.NetMigrationRate`) | C6 BASELINE |
| Poverty rate | **EXISTS** | `EconomyState.PovertyRate` | seeded | coupled (welfare, minimum wage, housing) | People | - |
| Poverty effect | **MAP** | the poverty GAP: relative median at-risk-of-poverty gap, % (how far below the line the poor sit - the "effect" the list means) | Eurostat ilc_li11; OECD IDD poverty gap | the transfer lines and welfare generosity `[AUTHORED-DRAFT]`: generosity closes the gap before it moves the rate | People | C6 |
| Unemployment and underemployment | **EXISTS / KEEP** | `EconomyState.Unemployment` exists; underemployment: underemployed part-time workers, % of employment | Eurostat lfsi_sup_a; OECD labour underutilisation | the labour dials | People | C6 (underemployment only) |
| Homelessness | **KEEP with caveat** | homeless persons per 10 000 - national definitions differ (the OECD says so per country) and the figure is shown with its definition | OECD Affordable Housing Database HC3.1 | housing spending and `EconomyState.HousingOverburden` `[AUTHORED-DRAFT]` | People (housing block) | C6 |
| Per-capita income | **EXISTS (derived)** | GDP over population, in the country's unit | derived | - | People | a readout, not a stat |
| Employment breakdown | **EXISTS** | employment by sector (the People page's second pie, §309/§313) | SCB and the five offices, seeded | the sector dials | People | - |

## 6. Department effectiveness (P5-C7, the mechanic)

| the list's row | ruling | this game's metric | series | reaches it | display home | family note |
|---|---|---|---|---|---|---|
| Effectiveness = funding received ÷ department's budget request (eight departments) | **KEEP as a mechanic** | per portfolio: allocated ÷ requested × the minister's efficiency attribute, where the request is P5-B2's driver-indexed line (`SimulationManager.IndexSpendingLines`) and the allocation the player's figure (P5-B5) | none to source - a ratio of two figures on the book | it IS the coupling: the ministry's outcomes (the families above) read effectiveness for that portfolio | the minister's card and the Budget row (D15 item 4 asks where) | C7, after the first family exists to read it |

## 7. Rows ruled N/A, with the reason

| the list's row | reason |
|---|---|
| Counties; party demographics per county; ancestry demographics | the six-country frame has no sub-national layer and models no ancestry; the electorate is the F-series' voter groups |
| Voter registration | Sweden, Germany, France, Italy and Poland register automatically; there is nothing to move - turnout exists in the elections track |
| Ideology, turnout | EXIST in the elections track (the voter groups, the campaign, election night) |
| Democratic / Republican / Independent approval of the military | partisan opinion of one country's parties, not a metric; approval exists as `EconomyState.ApprovalRating` |
| Military (no simulated metric beyond approval) | the defence line exists on the book; no metric is asked for by the list itself |
| Low-, medium-, upper-income tax rate, rated separately; flat tax, deductions, exemptions | behind F4 (the cohort income dimension) where they already wait - §3's rule; the rating curve note is a design remark on a foreign game |
| City income tax; property and school property tax | sub-national instruments; property tax exists as a line (`TaxType.PropertyTax`, unimplemented in the five), the school levy has no general form |
| Police officers, arrest rates, crimes committed, jailed and for what, prisoner locations | crime exists as `EconomyState.CrimeIndex`, `EconomyState.OrganizedCrimeIndex`, `EconomyState.PrisonPopulationRate` (World Prison Brief); police per 100 000 (Eurostat crim_just_job) would be a KEEP if a crime family is ever sheeted - it is not in §3's order; arrests, offences and prisoner locations have no six-country series |
| Guns | one estimate (Small Arms Survey 2017 civilian holdings), not a series; nothing moves it |
| Debt (total), debt per person | EXIST: `EconomyState.GovernmentDebt`, over population as a readout |
| Laws - status of every law | EXISTS: the law browser (P4-C3), twenty laws today and growing |
| Budget - revenues, expenditures, mandatory vs discretionary | EXISTS: the Budget screen |
| Other (remainder) | environment is family 4; nothing else is named |

---

**The order and the passes.** C2 health (six KEEP/MAP rows, the grammar's first family) → C3 education (six) → C4 infrastructure (three, one dated) → C5 environment (three, and the carbon tax's base moves to it) → C6 immigration and poverty depth (four) → C7 effectiveness (the mechanic that the families read). Each is its own BASELINE family: `TrajectoryBaselineDump` before and after, the diffs read per country, the seeds `[VERIFIED]` from the series named here, the couplings `[AUTHORED-DRAFT]` with the line stated, the display on D15 item 3's grammar. **No metric is built from this document.**
