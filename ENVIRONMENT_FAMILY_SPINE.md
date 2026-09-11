# The environment family's data spine — P5-C5, from files, not APIs (2026-09-05)

> **BUILT 2026-09-06, `COMPLETED.md` §340, on 9c's grammar inherited by shape.** The per-capita figures below are the family's seeds (`EnvironmentFamily.Seed`), §4's lines its couplings (every constant [AUTHORED-DRAFT]; readouts, no feedback); the electricity mix a fetch row (landed §342); **the carbon tax's base MOVED to these metrics on its own pass, `COMPLETED.md` §349 (2026-09-07)** - `TaxBaseDriver.Emissions`, power + transport CO₂ per head × population; this line said "sheeted, not built" until 2026-09-10 (§452), three days after it stopped being true. **EN-3 (2026-09-11, §460): the POWER figure's writer is the energy market's dispatch** (`EnergyMarket.PowerCo2PerHead` - the fossil generation's CO₂ at each category's factor and main-activity share, plus the seed residual of heat plants, CHP heat and refineries, over the population); the family's power elasticity and the energy line's coupling to it are retired; §4's coupling lines hold for TRANSPORT only; the plate's mix row is this year's dispatch.

**What this is.** P5-C5 (environment) is the fourth society-stat family in the catalog's order (`SOCIETY_STATS_CATALOG.md` § 4). Its source is a file: the EDGAR 2024 greenhouse-gas booklet (JRC / IEA), with the World Bank's population for the per-capita division and Ember or Eurostat for the electricity mix. This spine seeds what the fetched file covers for six, states the rest absent (its one fetch row landed at §342), and proposes the coupling. **Nothing is built from it**; the build waits on the one-grammar rule (D15 item 3).

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
