# The health family's data spine — P5-C2, the part that does not wait on Design (2026-09-05)

**What this is.** P5-C2 (health) is the first society-stat family and the one D15 item 3 asks Design to draw the grammar on. The grammar has not landed (E-17, the D15 paste, is Elias's), so the BUILD - seeded, coupled, displayed as instruments, family explained - waits; the sheet says to land the family's DATA SPINE meanwhile: the seeds sourced for six and the coupling proposed. This document is that spine. **Nothing is built from it**: no field, no seed constant, no coupling line - those land in one pass with the grammar, as a BASELINE family with the trajectory suite before and after.

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

**The seed the family takes:** *coverage* = TPRIBASI, the population covered for a core set of services by public or primary private insurance - the one column defined for all six. *Public share of coverage* = COVGCMED / TPRIBASI (the USA 43 %, the five ≥ 100 %) - the derived readout Elias's "Medicare/Medicaid" rows map to (`SOCIETY_STATS_CATALOG.md` § 1). **Retiree coverage** is derived: coverage applied to the 65+ cohort of F2's substrate, with the USA's Medicare rule (the 65+ are publicly covered) as the one authored exception, marked `[AUTHORED-DRAFT]`.

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

Dataflow **DSD_HEALTH_PROC / DF_WAITING** (Waiting times), measure WAIT_MEAN (mean days), waiting-time type WTSP (from specialist assessment to treatment), procedures as coded. **Sweden, Italy and Poland report; Germany, France and the USA publish no comparable series** - the flow holds no rows for them, and the catalog's row was corrected to say so (`SOCIETY_STATS_CATALOG.md` § 1).

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

The order of the six figures in each row is Sweden, Germany, France, Italy, Poland, USA. "Absent" prints as the word, in the caption face, never as a zero or a dash the eye could read as a figure. Nothing here is built; the board corrects it, then the pass builds what the board says.
