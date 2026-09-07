# The infrastructure family's data spine — P5-C4, from files, not APIs (2026-09-05)

> **BUILT 2026-09-06, `COMPLETED.md` §339, on 9c's grammar inherited by shape.** Road quality and connectivity are the family's seeds (`InfrastructureFamily.Seed`, dated 2019), §4's lines its couplings (every constant [AUTHORED-DRAFT]); congestion a fetch row with no figure, road length absent and stated; the feedback proposed in §4 (quality → `InfrastructureSpendingGrowthAdjustment`) is NOT built.

**What this is.** P5-C4 (infrastructure) is the third society-stat family in the catalog's order (`SOCIETY_STATS_CATALOG.md` § 3). Its sources are files: the WEF Global Competitiveness Report 2019 dataset (the last edition of the road-quality survey), the IRF World Road Statistics (paid) and the TomTom Traffic Index (a web report). This spine seeds what the fetched file covers for six, states the rest absent, and proposes the coupling. **Nothing is built from it**; the build waits on the one-grammar rule (D15 item 3) like every family.

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

## 5. Display rows, drafted (Sweden, Germany, France, Italy, Poland, USA)

| row | figure | unit | source line (caption mono) | band | arrows to |
|---|---|---|---|---|---|
| Road quality | 83.9 / 83.4 / 85.3 / 71.3 / 71.6 / 87.2 | score 0–100 · 2019 | WEF GCR 2019 · ROADINF · survey 2018–19 | 50 to 100 | the infrastructure line |
| Road connectivity | 95.9 / 95.1 / 96.6 / 85.9 / 88.0 / 100 | index 0–100 · 2019 | WEF GCR 2019 · ROADQUALIDX | 70 to 100 | road quality; population |
| Railroad density | in the file | km per km² · 2016 | WEF GCR 2019 · railroad density | - | none |
| Road length | absent | km | IRF WRS (paid) | - | none |
| Congestion | to fetch | % extra travel time | TomTom Traffic Index | - | connectivity |

"Absent" and "to fetch" print as words in the caption face. Nothing here is built.

## 6. The fetch, 2026-09-06 (overnight) — congestion under the cross-check gate: one source reached, the second refused; the figures recorded, the row keeps the word

The TomTom Traffic Index ranking page (`tomtom.com/traffic-index/ranking/`, 856 KB) embeds its data as JSON: two lists, CITY CENTRE and METRO AREA, per city a congestion level `c` (% extra travel time), an average speed `v`, hours lost at rush hours `tLostRush` and a rank. The largest city per country, city-centre list: Warsaw 51.3 (rank 77), New York 48.8 (rank 102), Rome 45.2 (rank 143), Berlin 44.6 (rank 155), Stockholm 42.8 (rank 180), Paris 40.0 (rank 231); the metro-area list reads lower (Warsaw 41.9, New York 41.3, Rome 41.4, Berlin 40.2, Stockholm 30.9, Paris 35.0). The page carries no year field beside the records; the edition is the live one on the day fetched. **The second source refused:** INRIX's Global Traffic Scorecard returned HTTP 403. **Gate: not passed** (a single source, a web page rather than a file). **Billed:** TomTom Traffic Index 2024 (city-centre congestion level, % extra travel time), INRIX 2024 Global Traffic Scorecard (hours lost per driver). The row prints *to fetch*; connectivity stands in, stated.

## 7. RF-1, 2026-09-07 — why the score drifted to its cap, and the saturating rebuild

The probe (`InfrastructureDriftProbe`, `COMPLETED.md` §354) showed the drift was the lines' own real growth per head (the AI's budget rule grows the infrastructure lines with output; Sweden's follows prices alone while its population grows), not the seed's spending: at the seed's spending the rebuild equals the decay by construction. The readout now saturates: the rebuild is scaled by (100 − score) ÷ (100 − seed) - 1 at the seed, 0 at the ceiling, above 1 below the seed (`InfrastructureFamily.SaturationFactor`) - so the seed's spending holds the seed's score to the digit and a bounded survey score is no longer driven linearly past its meaning. Decay and rebuild elasticities (§4) unchanged. `InfrastructureReadoutDiagnostic` asserts the identity at the seed for six, both directions around it, and that a century at seed policy reaches neither ceiling nor floor for any of the six.
