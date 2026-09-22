# The society statistics - the catalog and its five family spines

> Merged 2026-09-22 (`COMPLETED.md` §579) from `docs/data/SOCIETY_STATS.md` and the five `*_FAMILY_SPINE.md` files, which are retired. The catalog is the head; each
> family is a section below it, opening with ONE status line. ⚠ **Every spine's body said *"Nothing is built from it"* while its own banner said BUILT** - written
> before each family landed and never corrected. Those sentences are replaced here by the section's status line; nothing else in any spine changed.

---

## Part I - the catalog

**What this is.** Elias's metric list (`COMPLETED.md` §369a (the retired `PLAYTEST5_ANNEX_E16.md`) §2 - a foreign game's wiki, US-centric; the *breadth* wanted, not the *content*) mapped row by row to this game's six countries under the annex's §3 rules: **KEEP** where a sourced series exists for all six, **MAP** where the US term has a general form, **N/A** with the reason where the six-country frame cannot hold it. Every kept row names its series, the dial or law that reaches it (sourced, or `[AUTHORED-DRAFT]` with the proposed line), its display home and its trajectory-family note. Families are ordered by gameplay impact as §3 ruled: health → education → infrastructure → environment → immigration and poverty depth → effectiveness. **No metric is built from this document** (the C1 row); each family is its own BASELINE pass (P5-C2 to C7), on the one society-stat grammar D15 item 3 asks Design to draw on health.

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


---

## Health

> **BUILT 2026-09-05, `COMPLETED.md` §333, on Design's grammar (board 9c). Its FEEDBACK pass was measured and landed §343 (deaths leave the pyramid); the treatable-mortality TREND family followed at §353.** The seeds below are the family's seeds (`HealthFamily.Seed`), the §6 lines its couplings (`HealthFamily.TargetsFor`), every constant [AUTHORED-DRAFT] as marked; the display is the 9c plate (`GameController.Health.cs`), not the drafted table of §6. The feedbacks proposed in §4 (quality → life expectancy, coverage → approval) are NOT built and wait for their own pass.

**What this is.** P5-C2 (health) is the first society-stat family and the one D15 item 3 asks Design to draw the grammar on. The grammar has not landed (E-17, the D15 paste, is Elias's), so the BUILD - seeded, coupled, displayed as instruments, family explained - waits; the sheet says to land the family's DATA SPINE meanwhile: the seeds sourced for six and the coupling proposed. This document is that spine. ****

**Fetched 2026-09-05 from the OECD SDMX API** (sdmx.oecd.org, agency OECD.ELS.HD; the dataflow ids are given without the agency prefix; every value is the latest observation for the country, sex total, as the flow returned it; the raw CSVs are the session's tool-results and the digests are not claimed - a family pass re-fetches and records the digest the day it seeds).

---

## 1. Coverage — six of six

Dataflow **DSD_HEALTH_PROT / DF_HEALTH_PROT** (Healthcare coverage), measure HIC, unit PT_POP (% of population), insurance type as coded.

| country | year | public + primary private (TPRIBASI) | government / compulsory (COVGCMED) | primary voluntary (PHINPMPI) | total voluntary (PHINTPHI) |
|---|---|---|---|---|---|
| Sweden | 2024 | 100 | 100 | - | 8.1 (duplicate cover only) |
| Germany | 2024 | 99.9 | 99.9 | - | 29.9 (complementary) |
| France | 2025 / 2023 | 99.9 | 99.9 | - | 58.4 (2023; complementary) |
| Italy | 2025 | 100 | 100 | - | - |
| Poland | 2025 | 92.1 | 92.1 | - | - |
| USA | 2024 | 91.8 | 39.2 | 52.6 | 61.7 |

**The seed the family takes:** *coverage* = TPRIBASI, the population covered for a core set of services by public or primary private insurance - the one column defined for all six. *Public share of coverage* = COVGCMED / TPRIBASI (the USA 43 %, the five ≥ 100 %) - the derived readout Elias's "Medicare/Medicaid" rows map to (`docs/data/SOCIETY_STATS.md` § 1). **Retiree coverage** is derived: coverage applied to the 65+ cohort of F2's substrate, with the USA's Medicare rule (the 65+ are publicly covered) as the one authored exception, marked `[AUTHORED-DRAFT]`.

## 2. Quality — six of six on one figure, five of six on the supporting two

**The one figure: treatable mortality**, dataflow **DSD_HEALTH_STAT / DF_AM** (Avoidable mortality), measure TRTM, unit DT_10P5HB (deaths per 100 000, age-standardised), sex total. It is the OECD/Eurostat definition of deaths that timely and effective health care could have averted - a quality-of-care outcome, and the only one the flow holds for all six (France reports no HCQI indicators to the OECD).

| country | year | treatable mortality per 100 000 | preventable (PREVM) | avoidable, total (AVM) |
|---|---|---|---|---|
| Sweden | 2024 | **45** | 78 | 123 |
| Germany | 2022 | **63** | 129 | 192 |
| France | 2023 | **46** | 103 | 149 |
| Italy | 2023 | **51** | 82 | 133 |
| Poland | 2024 | **106** | 166 | 272 |
| USA | 2023 | **92** | 193 | 285 |

**Supporting, five of six** (France absent from both flows): avoidable hospital admissions, dataflow **DSD_HCQO / DF_PC** (Primary care), age-sex standardised per 100 000 aged 15+, sex total, unit 10P5HB - asthma and COPD combined (ASCOCOMP), diabetes uncontrolled (ADMRDBUC), congestive heart failure (ADMRCHFL); and 30-day mortality after admission, dataflow **DSD_HCQO / DF_AC** (Acute care), per 100 admissions aged 45+, unlinked - AMI (MORTAMII), ischaemic stroke (MORTISTI).

| country | year | asthma + COPD | diabetes | CHF | AMI 30-day | stroke 30-day |
|---|---|---|---|---|---|---|
| Sweden | 2023 | 123.1 | 62.0 | 206.0 | 3.4 | 4.9 |
| Germany | 2023 | 251.5 | 180.5 | 381.5 | 7.9 | 7.0 |
| France | - | absent | absent | absent | absent | absent |
| Italy | 2023 | 31.9 | 31.2 | 163.2 | 4.7 | 6.9 |
| Poland | 2023 | 129.3 | 161.4 | 523.4 | 6.7 | 10.5 |
| USA | 2022 | 123.3 | 224.0 | 387.2 | 5.2 | 4.5 |

**The seed the family takes:** *quality* = treatable mortality (lower is better; the instrument prints deaths per 100 000 and never a score). The five-of-six rows are the family's SUPPORTING readouts, shown where present and absent-and-stated for France - not folded into a composite, because a composite of five and a hole is a second book.

## 3. Waiting times — three of six, absent and stated for three

Dataflow **DSD_HEALTH_PROC / DF_WAITING** (Waiting times), measure WAIT_MEAN (mean days), waiting-time type WTSP (from specialist assessment to treatment), procedures as coded. **Sweden, Italy and Poland report; Germany, France and the USA publish no comparable series** - the flow holds no rows for them, and the catalog's row was corrected to say so (`docs/data/SOCIETY_STATS.md` § 1).

| country | year | cataract surgery (CM131_138), mean days | knee replacement (CM8154), mean days | hip replacement (CM8151_8153) |
|---|---|---|---|---|
| Sweden | 2025 | 60.2 | 141.3 | not reported on this basis |
| Italy | 2025 | 69 | 90 | not reported on this basis |
| Poland | 2025 | 47 | 281.5 | not reported on this basis |
| Germany, France, USA | - | absent and stated | absent and stated | absent and stated |

**The seed the family takes:** *wait* = the mean of cataract and knee replacement waiting days (the two both reporters carry; hip is not on the specialist-to-treatment basis for any of the three). For the three countries without a series the instrument prints ABSENT with the reason, and the coupling does not run on them - a metric that is not seeded is not simulated.

## 4. The coupling proposed — every line `[AUTHORED-DRAFT]`, none claimed sourced

The family reads three things the model already has: the health spending line per head (`SpendingCategory.HealthcareAndSocialCare` and the US health lines, over the population), the age-cost index (`SpendingDriver.AgeCostIndex`, P5-B2 - the demand the money meets), and the portfolio's effectiveness when P5-C7 lands (allocated ÷ requested × the minister's efficiency; until then, spending per head against its seed stands in).

| metric | moves with | direction | the proposed line, to be measured on the family's pass |
|---|---|---|---|
| coverage (%) | real health spending per head relative to its seed, ceilinged at 100 | up with spending | a slow reversion toward a target that rises with spending per head; a country at 100 stays at 100 until spending per head falls below its seed; the USA's ceiling is its own (the split is structural) |
| quality (treatable mortality per 100 000) | spending per head against the age-cost index; the minister's efficiency | down with spending, up with an ageing cohort the money did not follow | a drift toward a target that falls with spending per age-cost unit; elasticity a stated draft, calibrated so that Poland's 106 against Sweden's 45 is roughly their spending-per-head gap on the OECD's own figures (a claim to CHECK on the pass, not to assume) |
| wait (days) | effectiveness (C7) - allocated ÷ requested × efficiency | down as effectiveness rises above 1, up below | the first metric the effectiveness mechanic is felt on; until C7, spending per head against its seed stands in |
| retiree coverage | derived | - | no coupling of its own |

**Feedbacks to the model, proposed and NOT built:** quality → `EconomyState.LifeExpectancy` (a small drift, stated) and coverage → `EconomyState.ApprovalRating` through the existing approval terms - both wait for the pass, both measured against the trajectory suite when they land.

**BUILT 2026-09-06 (overnight), `COMPLETED.md` §343 - the health feedback pass.** Three terms, all from the quality key's ratio to its seed (`HealthFamily.QualityLog`, zero at the seed, clamped to ±ln 4): life expectancy +1.0 year per unit (`LifeExpectancyPerLogQuality`, in `MacroSystem.ApplyLifeExpectancy`'s target), participation +0.5 points per unit (`ParticipationPerLogQuality`, in `ApplyLaborForceParticipationRate`'s target), and the crude death rate at seed + (treatable mortality − seed) ÷ 100 (`HealthFamily.DeathRateFor`, the identity - a treatable death is a death; written in `AdvanceYear`, the one channel that moves F2's held figure). Each `[AUTHORED-DRAFT]` in magnitude, directional in the literature, each stated at the constant. Coverage → approval stays proposed (the §D in §343 says why). The suite: §343 names the fields that opened, per country.

## 5. Display — waits for D15 item 3

The grammar is Design's (the ask is installed: the People page's society block or the ministry's card, a figure with its unit and source line, its band, its coupling arrows). This document holds the seeds and the couplings so the family lands on the drawn grammar in one pass; it draws nothing.

**Next on this family, in order:** E-17 (the paste) → Design's board → the pass: seeds from the tables above re-fetched and digested, the coupling lines measured, the instruments on the grammar, the trajectory suite before and after, the record.

---

## 6. The spine extended (2026-09-05, later) — E-17 not yet pasted, the board not landed, so the build waits and the spine grows

**The coupling for the three-country wait-time set, made explicit.** The wait instrument exists only where a series does (Sweden, Italy, Poland). For those three: *wait_t+1 = wait_t × (1 − k) + k × wait_target*, with *wait_target = wait_seed × (effectiveness_seed ÷ effectiveness_t)^e* - waits scale inversely with the portfolio's effectiveness (P5-C7: allocated ÷ requested × the minister's efficiency; until C7 lands, spending per head against its seed stands in for the ratio), *k* a reversion speed and *e* an elasticity, both `[AUTHORED-DRAFT]` with the line stated here and measured on the pass (the claim to CHECK: Poland's knee wait of 282 days against Sweden's 141 is roughly their health-spending-per-head gap on the OECD's own figures - if it is not, *e* is not 1). For Germany, France and the USA the instrument prints **ABSENT** with the reason, no wait is simulated, and the effectiveness channel reaches QUALITY directly for them (the next paragraph) so underfunding is still felt - a country without a series is not a country without a consequence.

**The quality key for all six, the target form.** *treatable mortality_target = tm_seed × (spend-per-age-cost-unit_seed ÷ spend-per-age-cost-unit_t)^q × (efficiency_seed ÷ efficiency_t)^m*, drifting toward the target at a reversion speed; lower is better; *q* and *m* `[AUTHORED-DRAFT]`. The seed is the OECD figure per country (Sweden 45, France 46, Italy 51, Germany 63, the USA 92, Poland 106 per 100 000); the spend-per-age-cost-unit is the health line over `SpendingDriver.AgeCostIndex`, so an ageing cohort the money did not follow raises treatable mortality on its own - the demographic pressure P5-B2 removed from the lines comes back where it belongs, on the outcome. The supporting five-of-six rows (avoidable admissions, 30-day mortality) are readouts and move with the key by their seed ratios - never a second coupling.

**Coverage, the target form.** *coverage_target = min(ceiling, coverage_seed × (spend-per-head_t ÷ spend-per-head_seed)^c)*, ceiling 100 for the five and the USA's own (its 91.8 is structural: the split between public and primary private is not a spending outcome); *c* `[AUTHORED-DRAFT]`. Retiree coverage is coverage on the 65+ cohort with the USA's Medicare rule.

**The display rows, drafted so the board has something to correct** (the People page's society block, the health family; the ministry's card carries the same three figures and nothing else). Each row: the figure in its unit · the source line in caption mono · the band · the coupling arrows (5c's renderer) to what reaches it.

| row | figure | unit | source line (caption mono) | band | arrows to |
|---|---|---|---|---|---|
| Coverage | 100 / 99.9 / 99.9 / 100 / 92.1 / 91.8 | % of population | OECD HEALTH_PROT · TPRIBASI · 2024–25 | the seed's figure to 100 | the health line (spending per head) |
| … of which public | 100 / 99.9 / 99.9 / 100 / 92.1 / 39.2 | % of population | OECD HEALTH_PROT · COVGCMED | - | none (a derived readout) |
| Retiree coverage | derived | % of the 65+ cohort | derived · F2 substrate × coverage | - | none |
| Quality · treatable mortality | 45 / 63 / 46 / 51 / 106 / 92 | deaths per 100 000, age-standardised, lower is better | OECD HEALTH_STAT · TRTM · 2022–24 | 40 to 120 | the health line over the age-cost index; the minister's efficiency; effectiveness (C7) |
| Waiting · cataract | 60 / absent / absent / 69 / 47 / absent | mean days, specialist to treatment | OECD HEALTH_PROC · WAIT_MEAN · CM131_138 · 2025 | 0 to 180 | effectiveness (C7) |
| Waiting · knee replacement | 141 / absent / absent / 90 / 282 / absent | mean days | OECD HEALTH_PROC · WAIT_MEAN · CM8154 · 2025 | 0 to 400 | effectiveness (C7) |
| Supporting · avoidable admissions | five of six | per 100 000 aged 15+ | OECD HCQO · DF_PC · 2022–23 | - | none (readouts, moved by the quality key) |
| Supporting · 30-day mortality | five of six | per 100 admissions aged 45+ | OECD HCQO · DF_AC · 2022–23 | - | none |

The order of the six figures in each row is Sweden, Germany, France, Italy, Poland, USA. "Absent" prints as the word, in the caption face, never as a zero or a dash the eye could read as a figure. The status line at the head of this section is what is built.

## 9. The trend, 2026-09-07 (ruled) — the OECD's observed rate of improvement, per country, one vintage, on the seed's anchor

**The ruling:** improvement in treatable mortality CONTINUES rather than saturating, as a SOURCED trend on the seed - the way B7's productivity trend works - never as a raised elasticity. Built in `COMPLETED.md` §353: `HealthSeeds.TreatableMortalityTrendPerYear` (below), `HealthSeeds.TrendIndex` (exp(rate × years since the seed), compounding at the yearly step), `HealthFamily.TrendedAnchor` = seed × index - the anchor the quality target multiplies, so the world's improvement and the player's spending effect are separate terms. The elasticities (§6) are untouched.

**The series** (OECD `DSD_HEALTH_STAT@DF_AM`, measure TRTM, unit DT_10P5HB - deaths per 100 000, age-standardised - sex total; the whole flow fetched 2026-09-07 and filtered locally, `PoliSim-captures/sources/oecd_df_am_all.csv` → `oecd_trtm_six.tsv`); the rate is ln(last ÷ first) ÷ years, one vintage per country:

| country | first year, value | last year, value | rate per year |
|---|---|---|---|
| Sweden | 2000, 92 | 2024, 45 | −2.980 % |
| Germany | 2000, 109 | 2022, 63 | −2.492 % |
| France | 2000, 78 | 2023, 46 | −2.296 % |
| Italy | 2003, 79 | 2023, 51 | −2.188 % |
| Poland | 2000, 169 | 2024, 106 | −1.944 % |
| USA | 2000, 124 | 2023, 92 | −1.298 % |

Since 2010 the same series runs −2.52 / −1.78 / −1.91 / −1.63 / −1.23 / −0.33 - slower everywhere and nearly flat in the USA; the ruling names one vintage since 2000 and that is the one seeded, with the 2010 reading beside it here for the day the vintage is revisited.

**What it does to the family's own figures:** at baseline the quality anchor falls at the country's rate for as long as the game runs, so treatable mortality reaches the family's floor (`MinTreatableMortality` = 10 per 100 000, a runaway guard) within the century for every country - the year it binds is REPORTED per country by `HealthTrendDiagnostic`, not hidden; the floor is a guard and not a statement about care, and the ruling is that improvement continues, so the guard's binding year is a figure for Elias, not a fix.

**Carried with it, as ruled:** the death-rate identity (seed + Δ treatable ÷ 100) and the pyramid's excess deaths (§343) read the trended figure, so at baseline the crude death rate falls and the pyramid holds more people than the publisher's projection. §353's §D states the double-count risk that follows (the publisher's own projection assumes continued mortality improvement) and takes the ruling as written.

**The deviation form, ruled 2026-09-07 (`COMPLETED.md` §361 (4), built in §364):** the quality log, the death rate and the pyramid's excess deaths read treatable mortality against the TRENDED anchor, not the seed - the trend is the difference from what the publisher's projection already carries, so it is not counted twice; on the trend itself every term is zero and only the readout moves. The floor guard stays. **The anchor carries the same floor** (`TrendedAnchor` = max(10, seed × index)): the first dump of the form showed that once treatable mortality sits on its guard while the anchor keeps falling below it, the "deviation" reads as a permanent deficit (life expectancy −1.4 years, participation −0.7 at the clamp) - a guard artefact, not care; with the anchor bounded by the same guard the deviation is zero when both sit on it (§364).


---

## Education

> **BUILT 2026-09-06, `COMPLETED.md` §338, on board 9c's grammar inherited by shape. Its FEEDBACK pass landed §348.** The seeds below are the family's (`EducationFamily.Seed`), the §5 lines its couplings (`EducationFamily.TargetsFor`, every constant [AUTHORED-DRAFT]); PISA and the graduation rate are FETCHES with no score (ruled 2026-09-06) and their rows print the word; students per teacher is the card's key, not a page row (9c). The feedback proposed in §5 (attainment → the productivity trend) is NOT built.

**What this is.** P5-C3 (education) is the second society-stat family in the catalog's order (`docs/data/SOCIETY_STATS.md` § 2). Its build - seeded for six, coupled, displayed as instruments, family explained - waits, like health's, on the grammar D15 item 3 asks Design to draw (E-17 not pasted at this writing). This is the family's data spine: the seeds sourced for six where a series exists, absent and stated where it does not, and the coupling proposed. ****

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
| Academic score | **billed** (§373: the tables are behind the www host and not on the SDMX API; PISA is not among the 1 000+ dataflows `sdmx.oecd.org` lists) | PISA mean | OECD PISA 2022 · Vol. I · Tables I.B1.2.1–3 | 350 to 550 | students per teacher; effectiveness |
| Early leavers | 6.7 / 13.1 / 7.2 / 8.2 / 4.0 / absent | % of 18–24 | Eurostat edat_lfse_14 · 2025 | 0 to 20 | youth unemployment; the education line |
| Graduation rate | **billed** (§373: no OECD dataflow carries the first-time rate; the UOE flows hold graduate counts by age, and Eurostat's gross ratio is not the rate) | % at typical age | OECD EAG 2024 · Table B3.1 | 60 to 100 | early leavers |
| At least upper secondary · 25–64 | 86.0 / 85.9 / 83.9 / 67.0 / 94.9 / 92.3 | % of 25–64 | OECD EAG · LSO_NEAC_DISTR_EA · 2024–25 | 50 to 100 | the stock-flow line |
| Tertiary · 25–64 | 50.8 / 35.5 / 43.4 / 22.3 / 40.0 / 52.2 | % of 25–64 | OECD EAG · LSO_NEAC_DISTR_EA | 15 to 60 | the stock-flow line |

"Billed" and "absent" print as words in the caption face, never as figures (the rows printed "to fetch" until §373, when they were closed as billed). The status line at the head of this section is what is built.

## 7. The fetches, 2026-09-06 (overnight) — PISA and the graduation rate under the cross-check gate: neither passed; the figures reached are recorded, the rows keep the word

**PISA 2022.** The OECD's own tables (Volume I, Annex B1: Tables I.B1.2.1–3, the country means with standard errors) sit behind the www host's bot check (HTTP 403 on the publication page); `webfs.oecd.org/pisa2022/` serves only the microdata (SAS/SPSS student, school and teacher files - the means could be computed from PV1–PV10 with W_FSTUWT, a computation this machine did not attempt without a decoder for .sav); NCES's international-comparison pages returned 404 at the paths tried. **One transcription reached:** Our World in Data's grapher series (sourced to the OECD PISA database), 2022 means: mathematics - Poland 489.0, Sweden 481.8, Germany 474.8, France 473.9, Italy 471.3, USA 464.9; reading - USA 503.9, Poland 488.7, Sweden 487.0, Italy 481.6, Germany 479.8, France 473.9; science - not on OWID. The World Bank (LO.PISA.MAT/REA/SCI) holds 2015 and 2018 only (2018 mathematics: Poland 515.6, Sweden 502.4, Germany 500.0, France 495.4, Italy 486.6, USA 478.2 - agreeing with OWID's 2018 to the tenth, which cross-checks the transcription channel, not the 2022 figures). **Gate: not passed** (one transcription of 2022, no science). **Billed:** OECD PISA 2022 Results (Volume I), Tables I.B1.2.1, I.B1.2.2, I.B1.2.3 - mean score in mathematics, reading, science, all students; or NCES `nces.ed.gov/surveys/pisa/pisa2022/` international tables. The row was closed as billed in §373 and prints *billed* until either lands.

**Graduation rate.** No OECD dataflow under OECD.EDU.IMEP carries a graduation RATE (117 flows listed; the UOE flows hold enrolments, personnel, entrants and finance). Eurostat `educ_uoe_grad01` holds upper-secondary GRADUATES by age (2022, all ages, ISCED 3: Germany 674 026, France 905 768, Italy 556 900, Poland 386 679, Sweden 97 472) and `demo_pjan` the population aged 18 on 1 January 2023 (785 932 / 828 943 / 586 339 / 334 118 / 119 196); the gross ratio (all graduates ÷ one cohort) is 85.8 / 109.3 / 95.0 / 115.7 / 81.8 % - **above 100 for France and Poland**, because the count includes adults and second qualifications, so it is NOT the row's quantity (first-time graduates as % of the population at the typical age, EAG indicator B3) and cannot stand in for it. **Gate: not passed. Billed:** OECD Education at a Glance 2024, Table B3.1 (first-time upper secondary graduation rates), behind the www host; NCES ACGR for the USA is a different definition. The row was closed as billed in §373 and prints *billed*.

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


---

## Infrastructure

> **BUILT 2026-09-06, `COMPLETED.md` §339. ⚠ Its feedback pass was MEASURED AND STOPPED (§350, reverted by ruling §362): the level form is sheeted and needs a level source (RF-2 re-formed on the real wage, §367, §384); RF-1's saturating road-quality readout landed §354.** Road quality and connectivity are the family's seeds (`InfrastructureFamily.Seed`, dated 2019), §4's lines its couplings (every constant [AUTHORED-DRAFT]); congestion a fetch row with no figure, road length absent and stated; the feedback proposed in §4 (quality → `InfrastructureSpendingGrowthAdjustment`) is NOT built.

**What this is.** P5-C4 (infrastructure) is the third society-stat family in the catalog's order (`docs/data/SOCIETY_STATS.md` § 3). Its sources are files: the WEF Global Competitiveness Report 2019 dataset (the last edition of the road-quality survey), the IRF World Road Statistics (paid) and the TomTom Traffic Index (a web report). This spine seeds what the fetched file covers for six, states the rest absent, and proposes the coupling. ****

**Fetched and verified by content, 2026-09-05:** `WEF_GCI_4.0_2019_Dataset.xlsx` from www3.weforum.org (3 190 529 bytes, zip signature 50 4B 03 04, sha256 `444f60e812ce54b8…`), kept in `PoliSim-captures/sources/`; parsed header-aware from the workbook's XML (the country columns keyed by ISO3 in the dataset's third row, the series by their Series Global ID). Also fetched, not yet parsed: the report itself as PDF (`WEF_TheGlobalCompetitivenessReport2019.pdf`, 8 970 608 bytes, sha256 `b916745d690dc60b…`) - the machine here has no PDF renderer, so the dataset file is the source.

---

## 1. Quality of roads — six of six, dated 2019

Series **ROADINF, "Quality of roads (1–7)"**, the Executive Opinion Survey question, 2019 edition (the survey's weighted average of 2018–2019, source date 28 November 2018). The dataset carries the normalised SCORE (0–100) and the RANK for 2019; the raw 1–7 VALUE cell is empty in this edition's row, so the score is the figure (the 2018 edition's rows are in the file too, listed for the trend).

| country | 2019 score (0–100) | 2019 rank (of 141) | 2018 score | 2017 backcast score |
|---|---|---|---|---|
| Sweden | 83.9 | 13 | 86.6 | 85.9 |
| Germany | 83.4 | 15 | 83.9 | 84.4 |
| France | 85.3 | 10 | 88.0 | 88.8 |
| Italy | 71.3 | 40 | 70.2 | 72.0 |
| Poland | 71.6 | 39 | 65.5 | 65.2 |
| USA | 87.2 | 5 | 90.5 | 89.4 |

**The seed the family takes:** *road quality* = the 2019 score, dated as such - the WEF discontinued the survey after this edition and no later six-country series exists; the instrument prints the year with the figure. It is the "roads in poor condition" row of Elias's list, mapped (the catalog's ruling).

## 2. Road connectivity — six of six

Series **ROADQUALIDX, "Road connectivity index"** (% of the best, 0–100; the WEF's own computation from Google Directions API travel speeds between a country's ten largest cities, weighted by population; the dataset's source cell names the World Bank WDI and national sources for the inputs).

| country | 2019 value | rank |
|---|---|---|
| Sweden | 95.9 | 8 |
| Germany | 95.1 | 11 |
| France | 96.6 | 6 |
| Italy | 85.9 | 38 |
| Poland | 88.0 | 32 |
| USA | 100.0 | 1 |

**The seed the family takes:** *connectivity* = the index; it is the nearest sourced six-country figure to the list's "road congestion" row and stands in for it, stated - the TomTom Traffic Index (congestion level, % extra travel time, per city) is a web report with no data file and is **FETCH TO DO** if the board asks for congestion by name.

## 3. Railroad density and road length — one in the file, one absent

The dataset also carries a **Railroad density** row (km of rail per square km) with a 2016 source date - kept as a readout if the board wants it. **Road network length** (the list's "road length") is the IRF World Road Statistics, a paid publication with no open file; **absent and stated**.

## 4. The coupling proposed — every line `[AUTHORED-DRAFT]`, none built

The family reads the infrastructure spending line (`SpendingCategory.InfrastructureAndDevelopment` and its US counterparts), the existing infrastructure condition channel (`Country.InfrastructureSpendingGrowthAdjustment`, the potential-growth adjustment infrastructure already earns), population growth, and the portfolio's effectiveness when P5-C7 lands.

| metric | moves with | direction | the proposed line |
|---|---|---|---|
| road quality (score) | infrastructure spending per head against its seed; a decay rate | up with spending, down with time | *quality_t+1 = quality_t − decay + build*, build = *k × (spend-per-head_t ÷ spend-per-head_seed)*, calibrated so the seed's spending holds the seed's score - the claim to CHECK on the pass |
| connectivity (index) | road quality, with a lag; population growth against the network | up with quality, down as population outgrows the network | a slow drift toward a target set by quality and the population ratio |
| railroad density | the infrastructure line, very slowly | up with spending | a stock that barely moves in a game's horizon; shown, barely moved |

**Feedback to the model, proposed and NOT built:** road quality → `Country.InfrastructureSpendingGrowthAdjustment` (the channel exists; the metric would become its visible face rather than a second channel) - measured against the trajectory suite when it lands.

**BUILT, MEASURED AND STOPPED 2026-09-07 (overnight), `COMPLETED.md` §350 - nothing committed.** Road quality against its seed as the growth channel's third term at the channel's own sensitivity (0.02 points of trend growth per point) passed its diagnostic and then the suite showed why it cannot stand: this family's quality READOUT drifts to its cap of 100 at baseline for four of six within a century (§339's rebuild elasticity against the lines' own real growth), so the term adds half a point of trend growth a year to those four for as long as the cap holds - potential +45 % (Poland) to +19 % (Germany) at 100 turns, Italy −8 %. A reused magnitude on a drifting readout is not an honest draft; the readout's drift is the thing to fix first, and a points-per-point elasticity of growth to road quality is not in the literature this side could read. The coupling stays a readout; the measurement is the record.

**LANDED 2026-09-07 (morning), `COMPLETED.md` §359, after RF-1's readout fix (§354):** the same term (the quality-gap accessor (removed in §362) × the channel's own 0.02 per point, the growth channel's quality term (removed in §362), ±0.5, inside the 0.75 ceiling), measured again on the saturating readout - potential Poland +26.8 %, the USA +8.6 %, Sweden +6.4 % at a century, Italy −5.5 %; landed as ruled, the magnitude flagged RECONSIDER (a growth term reused from a capped drag; the literature's effect is a level).

**REVERTED 2026-09-07 (midday), `COMPLETED.md` §362, as ruled.** The growth term is out; the saturating readout (§7) stays. Sheeted in its place: the LEVEL form - potential × (1 + λ × ln(quality ÷ seed)), a one-time factor that compounds nothing; λ to be read at Calderón & Servén 2004 before anything is typed. RF-2 (the AI's line scaling) opened on the readouts' fix track.

**Read 2026-09-07 (`COMPLETED.md` §366):** their quality index is a principal component of telephone waiting time, power losses and the paved-road share; their coefficient (0.68 points of GROWTH per standard deviation of the index, Table 3 col. 6) is a growth-rate effect on a 1960–2000 panel, and their road-quality indicator alone is not significant (Table 4). No honest mapping from the WEF 1–7 survey score to their index exists, and a growth coefficient cannot be typed as a level - **the level form stays sheeted with that reason.** It is unblocked by a source that reads a survey quality score in output LEVELS, or by a road-length stock series (§5's absent row) their stock index does read. **Attempted once 2026-09-07 (`COMPLETED.md` §374):** such a source exists - IMF WP/22/95, *Road Quality and Mean Speed Score*, a level regression of ln GDP per capita on the WEF score - and is unreachable from a session (403 on imf.org and the eLibrary). λ is BILLED to its table; the form is CLOSED AS SHEETED-WITH-REASON and returns only on a ruling with the coefficient in hand.

## 5. Display rows, drafted (Sweden, Germany, France, Italy, Poland, USA)

| row | figure | unit | source line (caption mono) | band | arrows to |
|---|---|---|---|---|---|
| Road quality | 83.9 / 83.4 / 85.3 / 71.3 / 71.6 / 87.2 | score 0–100 · 2019 | WEF GCR 2019 · ROADINF · survey 2018–19 | 50 to 100 | the infrastructure line |
| Road connectivity | 95.9 / 95.1 / 96.6 / 85.9 / 88.0 / 100 | index 0–100 · 2019 | WEF GCR 2019 · ROADQUALIDX | 70 to 100 | road quality; population |
| Railroad density | in the file | km per km² · 2016 | WEF GCR 2019 · railroad density | - | none |
| Road length | absent | km | IRF WRS (paid) | - | none |
| Congestion | **billed** (§373: TomTom's ranking page is one vendor; INRIX's scorecard is a rendered chart with no data in its page - the second source the gate needs is unreachable from a session) | % extra travel time | TomTom Traffic Index 2024 · INRIX Global Traffic Scorecard 2024 | - | connectivity |

"Absent" and "billed" print as words in the caption face (the congestion row printed "to fetch" until §373, when it was closed as billed). The status line at the head of this section is what is built.

## 6. The fetch, 2026-09-06 (overnight) — congestion under the cross-check gate: one source reached, the second refused; the figures recorded, the row keeps the word

The TomTom Traffic Index ranking page (`tomtom.com/traffic-index/ranking/`, 856 KB) embeds its data as JSON: two lists, CITY CENTRE and METRO AREA, per city a congestion level `c` (% extra travel time), an average speed `v`, hours lost at rush hours `tLostRush` and a rank. The largest city per country, city-centre list: Warsaw 51.3 (rank 77), New York 48.8 (rank 102), Rome 45.2 (rank 143), Berlin 44.6 (rank 155), Stockholm 42.8 (rank 180), Paris 40.0 (rank 231); the metro-area list reads lower (Warsaw 41.9, New York 41.3, Rome 41.4, Berlin 40.2, Stockholm 30.9, Paris 35.0). The page carries no year field beside the records; the edition is the live one on the day fetched. **The second source refused:** INRIX's Global Traffic Scorecard returned HTTP 403. **Gate: not passed** (a single source, a web page rather than a file). **Billed:** TomTom Traffic Index 2024 (city-centre congestion level, % extra travel time), INRIX 2024 Global Traffic Scorecard (hours lost per driver). The row was closed as billed in §373 and prints *billed*; connectivity stands in, stated.

## 7. RF-1, 2026-09-07 — why the score drifted to its cap, and the saturating rebuild

The probe (`InfrastructureDriftProbe`, `COMPLETED.md` §354) showed the drift was the lines' own real growth per head (the AI's budget rule grows the infrastructure lines with output; Sweden's follows prices alone while its population grows), not the seed's spending: at the seed's spending the rebuild equals the decay by construction. The readout now saturates: the rebuild is scaled by (100 − score) ÷ (100 − seed) - 1 at the seed, 0 at the ceiling, above 1 below the seed (`InfrastructureFamily.SaturationFactor`) - so the seed's spending holds the seed's score to the digit and a bounded survey score is no longer driven linearly past its meaning. Decay and rebuild elasticities (§4) unchanged. `InfrastructureReadoutDiagnostic` asserts the identity at the seed for six, both directions around it, and that a century at seed policy reaches neither ceiling nor floor for any of the six.


---

## Environment

> **BUILT 2026-09-06, `COMPLETED.md` §340. Its FEEDBACK pass landed §349 - the carbon base is the taxed CO2.** The per-capita figures below are the family's seeds (`EnvironmentFamily.Seed`), §4's lines its couplings (every constant [AUTHORED-DRAFT]; readouts, no feedback); the electricity mix a fetch row (landed §342); **the carbon tax's base MOVED to these metrics on its own pass, `COMPLETED.md` §349 (2026-09-07)** - `TaxBaseDriver.Emissions`, power + transport CO₂ per head × population; this line said "sheeted, not built" until 2026-09-10 (§452), three days after it stopped being true. **EN-3 (2026-09-11, §460): the POWER figure's writer is the energy market's dispatch** (`EnergyMarket.PowerCo2PerHead` - the fossil generation's CO₂ at each category's factor and main-activity share, plus the seed residual of heat plants, CHP heat and refineries, over the population); the family's power elasticity and the energy line's coupling to it are retired; §4's coupling lines hold for TRANSPORT only; the plate's mix row is this year's dispatch.

**What this is.** P5-C5 (environment) is the fourth society-stat family in the catalog's order (`docs/data/SOCIETY_STATS.md` § 4). Its source is a file: the EDGAR 2024 greenhouse-gas booklet (JRC / IEA), with the World Bank's population for the per-capita division and Ember or Eurostat for the electricity mix. This spine seeds what the fetched file covers for six, states the rest absent (its one fetch row landed at §342), and proposes the coupling. ****

**Fetched and verified by content, 2026-09-05:** `EDGAR_2024_GHG_booklet_2024.xlsx` from edgar.jrc.ec.europa.eu (3 960 014 bytes, zip signature 50 4B 03 04, sha256 `769803bb2d3c9535…`), kept in `PoliSim-captures/sources/`; parsed header-aware from the workbook's XML (sheets GHG_per_capita_by_country and GHG_by_sector_and_country; EDGAR names the countries "France and Monaco" and "Italy, San Marino and the Holy See"). Populations 2023 from the World Bank API (SP.POP.TOTL): Germany 83.29 M, France 68.37 M, Italy 58.98 M, Poland 36.69 M, Sweden 10.54 M, USA 336.76 M.

---

## 1. Greenhouse gases per capita — six of six

Sheet GHG_per_capita_by_country, t CO₂-eq per person per year (fossil CO₂, CH₄, N₂O and F-gases, GWP-100 AR5; LULUCF excluded).

| country | 2020 | 2021 | 2022 | 2023 |
|---|---|---|---|---|
| Sweden | 5.14 | 5.29 | 4.89 | **4.76** |
| Germany | 9.08 | 9.49 | 9.23 | **8.26** |
| France | 6.06 | 6.51 | 6.29 | **5.81** |
| Italy | 6.25 | 6.89 | 6.84 | **6.36** |
| Poland | 9.95 | 10.70 | 10.53 | **9.67** |
| USA | 17.11 | 17.97 | 17.99 | **17.61** |

**The seed the family takes:** *emissions per capita* = the 2023 figure; the headline of the family (the catalog's "total fossil CO₂ per capita" row, widened to all gases because that is what the booklet's per-capita sheet carries - stated).

## 2. CO₂ from the power industry and from transport — six of six, the list's two rows mapped

Sheet GHG_by_sector_and_country, substance CO₂, sectors "Power Industry" and "Transport", Mt CO₂ per year; per capita by the World Bank population above (metric tonnes; the list's pounds do not travel).

| country | power industry 2022 | power industry 2023 | per capita 2023 (t) | transport 2022 | transport 2023 | per capita 2023 (t) |
|---|---|---|---|---|---|---|
| Sweden | 5.93 | 5.90 | 0.56 | 13.61 | 13.31 | 1.26 |
| Germany | 229.64 | 177.56 | 2.13 | 147.71 | 139.72 | 1.68 |
| France | 38.29 | 23.94 | 0.35 | 123.37 | 122.21 | 1.79 |
| Italy | 100.68 | 84.10 | 1.43 | 104.01 | 102.73 | 1.74 |
| Poland | 143.70 | 117.78 | 3.21 | 67.72 | 67.91 | 1.85 |
| USA | 1 580.19 | 1 463.28 | 4.35 | 1 699.43 | 1 710.65 | 5.08 |

**The seeds the family takes:** *electricity CO₂ per person* = the power-industry figure over population (the list's "electricity CO₂ per person (lbs)" mapped to metric); *transport CO₂ per person* = the transport figure over population (the list's "car CO₂ per person"). Poland's power figure fell a fifth in one year (coal to gas and renewables) and Germany's a quarter; the instrument's band should hold that speed.

## 3. Electricity generation by source — FETCH TO DO, the source named

Ember's yearly electricity data (generation by source, CO₂ intensity of electricity) sits behind a page that refused the fetch here (HTTP 403 on the data page); Eurostat nrg_bal_c (energy balances) covers the five and the US EIA the sixth. No figure is written; the pass fetches the file and records its digest. The list's "energy sources by type" row waits on it.

## 4. The coupling proposed — every line `[AUTHORED-DRAFT]`, none built

The family reads the carbon tax (`TaxType.CarbonTax`, whose base P5-B3 left on output with the reason that this family's metric did not exist yet), the energy and infrastructure spending lines, and output.

| metric | moves with | direction | the proposed line |
|---|---|---|---|
| electricity CO₂ per person | the carbon tax rate (the mix's intensity falls with the tax, with a lag); the energy line | down with the tax and with energy spending | *intensity_target = intensity_seed × (1 − e × (tax_t − tax_seed) / 100)* floored above zero, reversion over years; the elasticity a draft, CHECKED on the pass against the six's own spread (Poland's 3.21 t against France's 0.35 is the mix, not the tax) |
| transport CO₂ per person | the carbon tax; the infrastructure line (rail and public transport) | down with both | the same form on the transport figure |
| emissions per capita (headline) | the two above and the rest at their seed ratios | derived | the sum of the sector figures per capita, the other sectors held at their seed share - a derived readout, no coupling of its own |

**What this family changes elsewhere, sheeted:** the carbon tax's BASE moves from output to the electricity and transport CO₂ metric when C5 lands (P5-B3's reason for leaving it on output), so a tax that works erodes its own base - as a Pigouvian tax does. **Feedback to the model, proposed and NOT built:** none in this pass; the emissions metrics are outcomes and read nothing back into output until a damage or trade channel is sourced.

**BUILT 2026-09-07 (overnight), `COMPLETED.md` §349 - the base moved.** `TaxBaseDriver.Emissions` = (power CO₂ per head + transport CO₂ per head) × population, the fifth tax-base driver; `TaxBases.Of(CarbonTax)` reads it and nothing else moved. The base at the seed is still the sourced share of GDP; what it FOLLOWS is the taxed tonnes, so a tax that works erodes its own base (`EnvironmentFeedbackDiagnostic`: the identity, the erosion under a raise, output's bases untouched). Absent intensities read a level of 0 and the base holds at the seed's real level; a save from before the pass grows its four references to five on first read.

**RULED AND BUILT 2026-09-11 (EN-4c, `COMPLETED.md` §464) - the tax's unit.** The carbon tax's rate is the COUNTRY'S CURRENCY PER TONNE OF CO₂ - the statutory 2023 figure at the seed (Sweden 1 330 SEK, Germany 30 EUR, France 44.6 EUR; Italy, Poland and the USA 0, no carbon tax distinct from the ETS) - and its revenue is rate × the taxed tonnes (this family's level, Mt) in the book's dollars; the share-of-GDP reading is retired. The transport coupling's elasticity is per BOOK DOLLAR per tonne above the seed's rate (`TransportElasticityPerDollarPerTonne` = 0.00042, the chain in its remark: 2.32 kg CO₂ per litre, a $1.70 litre, the −0.31 long-run fuel elasticity), so the table's *(tax_t − tax_seed) / 100* reads *e × (tax_t − tax_seed) in dollars per tonne*; the power half is the dispatch's (EN-3). The taxed tonnes are this family's two sectors; the statutes exempt ETS installations and tax heating fuels the family does not carry - EN-4d's.

**RULED 2026-09-11, BUILT 2026-09-12 (EN-4d, `COMPLETED.md` §467) - the tax's coverage.** The taxed tonnes are TRANSPORT's alone: `TaxBaseDriver.Emissions` = transport CO₂ per head × population. The power fleet is ETS-covered and exempt of the national carbon tax by statute - Sweden's lag (1994:1776) 6 a kap. 1 § (100 per cent), Germany's BEHG § 7 Abs. 5, France's composante carbone - because the ETS prices those tonnes and the two must not stack; no installation register exists here, so the exemption is applied at SECTOR level (the deviation, stated in `EnergyMarket`'s class doc). The power half of this family reads the dispatch, whose carbon price is the ETS; the tax's coupling is transport's alone, and the plate's 5c arrow while a tax draft is live paints on the transport row (`EnvironmentFamily.ProjectTransportCo2`). Sweden's statutory indexation of the rate (2 kap. 1 b §) is carried since EN-4e (§471, 2026-09-12): the rate moves as each country's statute moves it at the boundary (`CarbonRateStatute` - Sweden indexed, Germany on the BEHG's schedule, France nominal), and THIS COUPLING READS THE REAL RATE - the nominal figure over the price level against `CarbonTaxRateSeed` - so an indexed rate at no policy is no real change and a frozen one's erosion raises the transport intensity, shown rather than hidden (the B6 artefact the ruling names).

## 5. Display rows, drafted (Sweden, Germany, France, Italy, Poland, USA)

| row | figure | unit | source line (caption mono) | band | arrows to |
|---|---|---|---|---|---|
| Emissions per person | 4.76 / 8.26 / 5.81 / 6.36 / 9.67 / 17.61 | t CO₂-eq · 2023 | EDGAR 2024 · GHG per capita | 0 to 20 | none (derived) |
| Electricity CO₂ per person | 0.56 / 2.13 / 0.35 / 1.43 / 3.21 / 4.35 | t CO₂ · 2023 | EDGAR 2024 · Power Industry ÷ WB population | 0 to 5 | the carbon tax; the energy line |
| Transport CO₂ per person | 1.26 / 1.68 / 1.79 / 1.74 / 1.85 / 5.08 | t CO₂ · 2023 | EDGAR 2024 · Transport ÷ WB population | 0 to 6 | the carbon tax; the infrastructure line |
| Electricity by source | **landed** (§342, §8: Ember 2023 against Eurostat `nrg_bal_peh` and EIA Table 1.1, every pair within a point) | % of generation, seven parts | Ember (OWID) · Eurostat nrg_bal_peh · EIA 1.1 | - | a readout; the carbon tax on its own pass |

The mix row prints its seven figures since §342; an unseeded country would print "billed".

## 8. The fetch, 2026-09-06 (overnight) — the electricity mix under the cross-check gate: PASSED for six, landed as the plate's distribution row

**Source 1:** Ember's yearly electricity data as Our World in Data publishes it (`ourworldindata.org/grapher/share-electricity-{coal,gas,nuclear,hydro,wind,solar}.csv`, sourced to Ember), 2023 shares of generation. **Source 2 (the five):** Eurostat `nrg_bal_peh`, gross electricity production by fuel 2023 (GWh), shares over TOTAL; **source 2 (the USA):** the EIA's Electric Power Monthly Table 1.1 (`eia.gov/electricity/monthly/xls/table_1_01.xlsx`, 21 278 bytes, zip signature), 2023 annual totals over total generation at utility-scale facilities plus estimated small-scale solar.

| country | coal Ember / check | gas | nuclear | hydro | wind | solar |
|---|---|---|---|---|---|---|
| Sweden | 0.0 / 0.0 | 0.1 / 0.1 | 29.2 / 29.2 | 39.9 / 39.8 | 20.6 / 20.6 | 1.9 / 1.9 |
| Germany | 24.6 / 23.9 | 15.1 / 16.2 | 1.4 / 1.4 | 4.2 / 4.1 | 27.7 / 27.1 | 12.6 / 12.3 |
| France | 0.3 / 0.3 | 5.8 / 5.7 | 65.2 / 64.4 | 10.8 / 10.6 | 9.7 / 9.8 | 4.4 / 4.3 |
| Italy | 5.1 / 5.0 | 45.5 / 44.9 | 0 / 0 | 15.5 / 15.3 | 9.0 / 8.9 | 11.7 / 11.6 |
| Poland | 59.7 / 59.1 | 10.0 / 9.9 | 0 / 0 | 1.5 / 1.4 | 14.6 / 14.4 | 6.7 / 6.6 |
| USA | 15.9 / 16.1 | 42.5 / 43.2 | 18.2 / 18.5 | 5.6 / 5.9 | 9.9 / ~10 (EIA folds wind into "renewables excluding hydro and solar", 11.6) | 5.6 / 5.7 |

Every pair agrees within a point (Germany's gas 1.1 apart - Eurostat's gross production against Ember's net generation). **Gate: passed.** The family seeds the seven shares (the six fuels and the remainder) as a static readout (`EnvironmentSeeds.MixShares`) and the plate's *Electricity by source* row is the DISTRIBUTION form; nothing moves the mix - the carbon tax would, on its own pass. `EnvironmentFamilyDiagnostic` asserts the seven sum to 100 within half a point for six and Germany's coal against Eurostat's 23.9 within 1.5.


---

## Immigration and poverty depth

> **BUILT 2026-09-06, `COMPLETED.md` §341. ⚠ Its feedback pass was MEASURED AND STOPPED (§351).** The tables below are the family's seeds (`MigrationPovertyFamily.Seed`) with their two definitions carried as state; §6's lines its couplings (every constant [AUTHORED-DRAFT]; readouts, no feedback).

**What this is.** P5-C6 (immigration and poverty depth) is the fifth society-stat family in the catalog's order (`docs/data/SOCIETY_STATS.md` § 5). Its sources are Eurostat (three series, JSON-stat, decoded by index), the OECD (the Affordable Housing Database workbook HC3.1 and the Income Distribution Database dataflow) and the US Department of Homeland Security's Office of Homeland Security Statistics (a PDF report) with the Bureau of Labor Statistics for the US labour figure. This spine seeds what the fetched series cover for six, states the rest absent or differently defined, and proposes the coupling. ****

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

**The feedback pass of 2026-09-07 - STOPPED, `COMPLETED.md` §351.** The sheet asked for *flow → the cohort inflow term*; an apprehension count (`migr_eipre`, "found to be illegally present") is not an arrival count, and the only series that would bridge it - returns following an order to leave, `migr_eirtn` - gives a naive retained share of 0.91 / 0.88 / 0.96 / 0.47 for four of the five and **−3.6 for Sweden** (13 600 returned against 2 965 found in 2024, returns discharging earlier years' orders). No figure was typed in its place; the coupling stays a readout until an arrivals-and-stays series exists (billed in §351). **Attempted once more 2026-09-07 (`COMPLETED.md` §375) and CLOSED AS BILLED:** Pew Research's 2019 estimates are two stock ranges (2014, 2017) for four of the five, ending in 2017, a one-time study - not a series; the readout stands with its definition on the row.

## 7. Display rows, drafted (Sweden, Germany, France, Italy, Poland, USA)

| row | figure | unit | source line (caption mono) | band | arrows to |
|---|---|---|---|---|---|
| Irregular migration | 2.8 / 29.9 / 20.8 / 18.5 / 4.4 · USA 326 (stock) | per 10 000 · 2024 flow · USA 2022 stock | Eurostat migr_eipre · DHS OHSS 2024 | 0 to 40 (five) · a separate stock band for the USA | Immigration Policy; Border Enforcement |
| Poverty gap | 23.1 / 21.7 / 20.6 / 24.6 / 19.7 · USA 37.2 (OECD) | % of the 60 % line · 2025 · USA 2023 | Eurostat ilc_li11 · OECD IDD PG_INC_DISP | 10 to 40 | transfer generosity; minimum wage |
| Underemployment | 3.60 / 1.18 / 4.11 / 2.41 / 0.83 / 2.77 | % of employment · 2024 | Eurostat lfsi_sup_a ÷ lfsi_emp_a · BLS LNS12032194 ÷ LNS12000000 | 0 to 6 | the labour dials |
| Homelessness | 33 / 31 / 49 / 16 / 8 / 19 | per 10 000 · 2017 / 2022 / 2022 / 2021 / 2019 / 2023 | OECD AHD HC3.1.A1 | 0 to 60 | the housing line; overburden |

Two-definition rows print their definition in the caption face; a year older than five years prints as words. The status line at the head of this section is what is built.


---

