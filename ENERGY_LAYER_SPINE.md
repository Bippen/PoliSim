# The energy layer's data spine — stage 2's sourcing, from files and APIs (2026-09-10)

> **SOURCED 2026-09-10, `COMPLETED.md` §455, under the ruling of `POLISIM_ENERGY_SPECLET.md` (S1–S15, §454). BUILT the same night, §457:** `Tools/energy_prep.pl` derives `EnergyData/` from the files below, `EnergyCatalogGenerator` emits `EnergyLayerData`, `EnergyLayer` reads it, `EnergyLayerCheck` holds the five gates of §11 on the bar, `EnergyLayerDump` writes `ENERGY_LAYER_PREMISE.md`. The build refined §9's method: the derived part is the MAIN-ACTIVITY producers' electricity (EDGAR's 1A1 is main activity; autoproducers are industry's book) - the reading in §9, which lumped all producers, is superseded by the dump's table. **Stage 3 (EN-3) BUILT 2026-09-11, §458–§460:** `Tools/energy_market_prep.pl` adds three files - the 2023 output level per zone, block and category on the clearing's hours (energy-charts by type for five, EIA-930 by source for the USA, eSett by type for SE1–SE4 on the national hours, with the exchange by neighbour from energy-charts `cbpf` apportioned by the interconnectors), the merit order's inputs per country and fossil category (§5's prices; efficiencies and emission factors DERIVED from the balance; EIA AEO2023 variable O&M; the ETS; lignite priced at EIA's US lignite delivered cost as a `[PROVISIONAL]` proxy for an untraded fuel), and Sweden's interconnectors by zone - and `EnergyMarket` clears on them; the power figure's writer is the dispatch. Every figure below is read off a file in `PoliSim-captures/sources/energy/` whose digest is in §12, or off a page fetched this night and saved beside it. **No figure from the concept document or from Design's prototype is in this spine.** Where two independent sources exist the gate of §342 is applied and its result printed - passed, or not passed with the reason. Where a series could not be reached the row says BILLED and names it.

**What this is.** The physical layer (stage 2) needs, per country: generation by technology, the fleet by technology, the hourly load folded into the spec-let's three blocks and a winter peak, Sweden's four zones with their links, and the prices the market layer (stage 3) will clear on. This spine carries each for six countries, with the cross-checks, in the order the build consumes them.

---

## 1. Generation by source, 2023 — six of six, and the family's seeds re-read

The environment family's `MixShares` (`EnvironmentFamily.cs:55-60`, Ember via OWID, §342) are the calibration target of stage 2 (spec-let §2.1). Re-read against **Eurostat `nrg_bal_peh`** (gross electricity production by fuel, GWh, updated 2026-06-02) folded into the seven labels - coal = solid fossil fuels `C0000X0350-0370`; gas = `G3000`; nuclear = `N900H`; hydro = `RA100` (pumped included); wind = `RA300`; solar = `RA410` + `RA420`; other = the rest (bioenergy, wastes, oil, manufactured gases, geothermal, tide) - and against **Ember's yearly release** (`release_generation_yearly_global.csv`, 16 086 064 bytes; Ember's "other" = bioenergy + other renewables + other fossil):

| country | source | coal | gas | nuclear | hydro | wind | solar | other | total 2023 |
|---|---|---|---|---|---|---|---|---|---|
| Germany | Eurostat GEP | 23.9 | 16.2 | 1.4 | 4.1 | 27.1 | 12.3 | 15.1 | 522 871 GWh |
| | Ember / **seed** | 24.6 | 15.1 | 1.4 | 4.2 | 27.7 | 12.6 | 14.3 | 506.72 TWh |
| France | Eurostat GEP | 0.3 | 5.7 | 64.4 | 10.6 | 9.8 | 4.3 | 4.8 | 524 901 GWh |
| | Ember / **seed** | 0.3 | 5.8 | 65.2 | 10.8 | 9.7 | 4.4 | 3.8 | 518.75 TWh |
| Italy | Eurostat GEP | 5.0 | 44.9 | 0.0 | 15.3 | 8.9 | 11.6 | 14.2 | 264 716 GWh |
| | Ember / **seed** | 5.1 | 45.5 | 0.0 | 15.5 | 9.0 | 11.7 | 13.2 | 261.55 TWh |
| Poland | Eurostat GEP | 59.1 | 9.9 | 0.0 | 1.4 | 14.4 | 6.6 | 8.6 | 167 362 GWh |
| | Ember / **seed** | 59.7 | 10.0 | 0.0 | 1.5 | 14.6 | 6.7 | 7.5 | 165.52 TWh |
| Sweden | Eurostat GEP | 0.0 | 0.1 | 29.2 | 39.8 | 20.6 | 1.9 | 8.4 | 166 093 GWh |
| | Ember / **seed** | 0.0 | 0.1 | 29.2 | 39.9 | 20.6 | 1.9 | 8.4 | 166.05 TWh |
| USA | EIA EPM Table 1.1 (2023 annual, utility + small-scale PV) | 15.9 | 42.4 | 18.2 | 5.8 | — | 5.6 | — | 4 256 676 GWh |
| | Ember / **seed** | 15.9 | 42.5 | 18.2 | 5.6 | 9.9 | 5.6 | 2.3 | 4 253.91 TWh |

**Gate:** every share within a point of its pair **except Germany's gas, 16.2 against 15.1 (1.1 points)** - Eurostat's `G3000` counts every gas-fired GWh including autoproducers' CHP, Ember's plant basis nets some of it into "other fossil" (Ember DE other fossil 4.1 against Eurostat's manufactured gases + oil + waste 2.9 + 1.0 + 1.2). Recorded, not hidden; the seeds stand as the target and the build reproduces them from Eurostat's fuel detail with the fold stated. The seven seeds equal Ember's 2023 shares to the decimal for six (the USA against EIA Table 1.1 within 0.2, as §342 found). EIA's Table 1.1 has no wind column of its own (wind sits in "renewable sources excluding hydroelectric and solar", 484 708 GWh with biomass and geothermal), which is why the USA row has two dashes.

**The producer split, which the fleet needs** (`nrg_bal_peh` GEP by producer, GWh): main-activity electricity-only plants carry 354 157 of Germany's 522 871, 490 920 of France's 524 901, 151 484 of Italy's 264 716, **33 992 of Poland's 167 362** (Poland's power is CHP: 108 456 main-activity CHP + 14 812 autoproducer CHP), 150 491 of Sweden's 166 093. Gross heat production beside it: DE 114 679, FR 49 059, IT 24 041, **PL 74 606**, SE 60 519 GWh. This is the heat half the spec-let's residual (§2.1) names, sourced.

## 2. The fleet, 2023 — installed capacity by technology, three sources side by side

Net maximum electrical capacity at year end, GW. **Eurostat `nrg_inf_epc` / `nrg_inf_epcrw`** (`CAP_NET_ELC`, all operators, updated 2026-08-21), **Ember** (the same release's *Capacity (GW)* column), **energy-charts.info `/installed_power`** (Fraunhofer ISE's republication of ENTSO-E and national data, `time_step=yearly`, read live 2026-09-10 - not saved as a file, the figures below are the read). The USA: **EIA Electric Power Annual Table 4.2.A** (`epa_04_02_a.xlsx`, existing net summer capacity, 2023 row, all sectors) against Ember.

| country | source | total | coal | gas | nuclear | hydro (of which pumped) | wind | solar | combustible other |
|---|---|---|---|---|---|---|---|---|---|
| Germany | Eurostat | 261.9 | 47.0 | 38.1 | 4.2 | 11.0 (5.3) | 69.4 | 75.5 AC · 84.0 DC | bio + waste 11.9, oil 3.4 |
| | Ember | 235.2 | 39.1 | 31.9 | 0.0 | 5.6 | 69.4 | 75.5 | bio 8.8, other fossil 3.8 |
| | energy-charts | — | 36.6 (lignite 18.4 + hard 18.3) | 35.7 | 0 | 5.4 + 10.0 pumped | 69.5 | 76.9 AC · 83.3 DC | bio 8.9, oil 4.3 |
| France | Eurostat | 152.8 | 0.0 | 10.9 (+2.6 solid/gas, +0.8 mixed) | 61.4 | 26.1 (1.7) | 23.1 | 19.7 AC · 23.4 DC | bio + waste 1.5, oil 3.8 |
| | Ember | 155.1 | 2.0 | 18.6 | 61.4 | 24.3 | 23.1 | 20.1 | bio 1.6, other fossil 3.2 |
| | energy-charts | — | 1.8 | 12.8 | 61.4 | 8.8 reservoir + 5.1 pumped (no run-of-river row) | 22.7 | 17.8 | bio 1.4, oil 3.0 |
| Italy | Eurostat | 130.1 | 0.0 (coal sits in the multi-fuel groups: solid/liquid 3.4, solid/gas 2.9, solid/liquid/gas 1.3) | 36.8 pure (+10.7 liquid/gas) | 0 | 22.9 (4.0) | 12.3 | 29.4 | bio + waste 3.2, oil 2.8, geothermal 0.8 |
| | Ember | 130.2 | 5.5 | 57.2 | 0 | 18.9 | 12.3 | 29.4 | bio 3.0, other fossil 3.5 |
| | energy-charts | — | 5.6 | 45.2 | 0 | 4.5 reservoir + 7.3 pumped | 11.7 | 5.9 (incomplete) | geothermal 0.9 |
| Poland | Eurostat | 59.9 | 27.0 | 4.3 | 0 | 2.4 (1.4) | 9.3 | 15.1 (14.0 AC) | bio + waste 1.3, oil 0.4 |
| | Ember | 59.5 | 28.6 | 3.2 | 0 | 1.0 | 9.3 | 15.9 | bio 1.0, other fossil 0.3 |
| | energy-charts | — | 26.4 (lignite 7.6 + hard 18.8) | 5.2 | 0 | 0.5 + 1.6 pumped | 9.6 | 14.6 | bio 0.7 |
| Sweden | Eurostat | 51.4 | 0.1 | 0.0 | 7.0 | 16.4 (0.0) | 16.2 | 4.0 AC · 4.8 DC | combustible fuels 7.7 |
| | Ember | 51.6 | 0.0 | 0.3 | 7.0 | 16.4 | 16.2 | 4.0 | bio 3.3, other fossil 4.0 |
| | energy-charts | — | — | — | 6.9 | 16.3 | 16.7 | 3.2 | others 6.6 |
| USA | EIA 4.2.A | 1 187.5 + 47.8 small PV | 178.4 | 507.5 (+1.9 other fossil gas) | 95.7 | 80.0 (+23.1 pumped) | in "other renewable" 254.0 | | petroleum 29.4, other 17.5 |
| | Ember | 1 254.5 | 199.8 | 543.2 | 95.7 | 84.3 | 147.5 | 139.9 | bio 9.9, other fossil 33.3 |

**Gate:** **nuclear, wind and solar (AC) agree within a point across every source that carries them**; Sweden's and Poland's totals agree within a point; hydro agrees where the sources class the same plants (Eurostat and Ember; energy-charts omits run-of-river for France and Italy). **Thermal capacity does NOT pass anywhere except Poland's coal (27.0 ⁄ 28.6 ⁄ 26.4):** Germany's coal reads 47.0 ⁄ 39.1 ⁄ 36.6 and its gas 38.1 ⁄ 31.9 ⁄ 35.7; France's gas 10.9 ⁄ 18.6 ⁄ 12.8; Italy's gas 36.8–47.5 ⁄ 57.2 ⁄ 45.2; the USA's coal 178.4 ⁄ 199.8 and gas 507.5 ⁄ 543.2. The reason is definitional, and it is printed rather than averaged: Eurostat's net maximum capacity keeps reserve and mothballed units and groups multi-fuel plants by fuel set (Italy's coal lives in "solid and liquid fuels"); Ember's is nameplate; energy-charts is ENTSO-E's operational list; EIA's is net summer against Ember's nameplate. Germany's nuclear 4.2 in Eurostat is the stock still INSTALLED at year end (its decommissioned column reads 0 - the plants stand) after the Atomgesetz ended their operation on 15 April 2023; the build keeps the record's figure and tags it CLOSED BY LAW - installed, unavailable - the one strict class where the sources disagree, resolved by naming what each counts (§457). **For the build:** the statistical office of record carries the fleet - Eurostat for five, EIA for the USA - with Ember and energy-charts as the cross-check, and thermal capacity is tagged DEFINITION-DIVERGENT on the dump with the three figures beside it. Availability × capacity, not capacity, is what the merit order needs, and the de-rating row (§10) is where the difference is absorbed.

**Per-zone capacity for Sweden is BILLED** - ENTSO-E 14.1.A per bidding zone needs the account (E-34); eSett's production per zone (§4) is the sourced fallback: the national fleet by technology apportioned to SE1–SE4 by each zone's 2023 production of that technology, stated as an apportionment on the dump.

## 3. The load, folded into the spec-let's blocks — six countries and Sweden's four zones

**Series.** Five EU countries: **energy-charts.info `/public_power`** for calendar 2023 (`country=de|fr|it|pl|se`, `start=2023-01-01&end=2024-01-01`), the `Load` series (ENTSO-E's actual total load, republished; 15-minute for Germany, hourly for the rest), files `energycharts_public_power_<cc>_2023.json`. The USA: **EIA-930** balance files for 2023 (`EIA930_BALANCE_2023_Jan_Jun.csv` 41 981 693 bytes, `_Jul_Dec.csv` 42 574 041 bytes), *Demand (MW) (Adjusted)* summed over every balancing authority per UTC hour (`eia930_us48_demand_2023.tsv`; the three first and three last hours, where fewer than 45 authorities had reported, dropped). Sweden's zones: **eSett Open Data `EXP15/Aggregate`** (settlement consumption per market balance area, hourly, `esett_EXP15_SE<n>_2023.json`) - the settlement authority since 2017; Svenska kraftnät's Mimer returns an empty hourly-metered column for 2023 for every area and is not usable (`mimer_consumption_2023_SN*.txt`, kept as the evidence).

**The fold** (`loadblocks.pl`, the spec-let's S3): the periods of the local calendar year sorted descending; *base* = the minimum, every hour; *peak* = the periods above the P90 value (the value exceeded by ten per cent of them), at their mean; *mid* = the rest, at their mean. The energies sum to the series to the hundredth (the `check` column, dropped here).

| zone | hours | energy TWh | min MW | mean | max | P90 | peak block: h · mean MW | mid block: h · mean MW | base TWh · mid above base · peak above base |
|---|---|---|---|---|---|---|---|---|---|
| Germany (15-min, 35 040 periods) | 8 760 | 458.38 | 27 933 | 52 327 | 73 828 | 64 868 | 876 · 67 595 | 7 884 · 50 630 | 244.69 · 178.95 · 34.74 |
| France (5 null hours) | 8 755 | 425.44 | 28 744 | 48 594 | 81 747 | 63 445 | 875 · 68 966 | 7 880 · 46 332 | 251.65 · 138.59 · 35.19 |
| Italy | 8 760 | 276.14 | 16 499 | 31 523 | 52 503 | 40 944 | 876 · 43 382 | 7 884 · 30 205 | 144.53 · 108.06 · 23.55 |
| Poland | 8 760 | 166.10 | 11 454 | 18 961 | 27 106 | 23 365 | 876 · 24 556 | 7 884 · 18 340 | 100.34 · 54.28 · 11.48 |
| Sweden | 8 760 | 130.62 | 8 259 | 14 911 | 24 332 | 19 291 | 876 · 20 530 | 7 884 · 14 286 | 72.35 · 47.52 · 10.75 |
| USA (UTC year) | 8 751 | 4 074.64 | 307 565 | 465 620 | 741 816 | 572 547 | 875 · 632 252 | 7 876 · 447 108 | 2 691.50 · 1 099.04 · 284.10 |
| SE1 | 8 760 | 11.16 | 870 | 1 274 | 1 779 | 1 557 | 876 · 1 616 | 7 884 · 1 237 | 7.62 · 2.89 · 0.65 |
| SE2 | 8 760 | 15.09 | 970 | 1 723 | 2 755 | 2 250 | 876 · 2 387 | 7 884 · 1 649 | 8.49 · 5.36 · 1.24 |
| SE3 | 8 760 | 79.58 | 5 076 | 9 084 | 15 200 | 11 853 | 876 · 12 630 | 7 884 · 8 690 | 44.46 · 28.50 · 6.62 |
| SE4 | 8 760 | 21.19 | 1 288 | 2 418 | 4 353 | 3 185 | 876 · 3 458 | 7 884 · 2 303 | 11.28 · 8.00 · 1.90 |

**Gate on the energies** (the block energies must land on a second source's consumption): Eurostat `nrg_cb_e` 2023, GWh - *available for final consumption* (AFC) and *inland demand* (ID) - DE 473 497 ⁄ 499 271, FR 408 692 ⁄ 445 822, IT 287 361 ⁄ 305 608, PL 147 582 ⁄ 157 922, SE 123 293 ⁄ 132 879; Ember *Demand* DE 515.94, FR 468.28, IT 312.80, PL 169.26, SE 137.56 TWh. ENTSO-E's load sits between AFC and inland demand for Germany, France, Italy and Sweden (it carries network losses, not plants' own use or pumping) - **passed as a bracket, the definition stated**. **Poland does not:** 166.1 TWh of load against 157.9 of inland demand (5 % over) - the Polish TSO's load definition to ENTSO-E is wider than Eurostat's balance; recorded, the build reads PL's blocks scaled to `nrg_cb_e`'s inland demand with the factor printed. The USA's 4 074.6 TWh against EIA Table 1.1's 4 183 270 GWh net generation (+73 406 small-scale) is the 4 % that losses and own use take - consistent. Sweden's four zones sum to 127.02 TWh of settlement consumption against 130.62 of load (−2.8 %, the network losses eSett does not settle) - consistent in the right direction.

**The winter peak.** The maximum hour of calendar 2023: Sweden 24 332 MW at 2023-12-06 17:00 CET (energy-charts); per zone SE1 1 779 MW (28 Dec 15:00), SE2 2 755 (6 Dec 16:00), SE3 15 200 (6 Dec 17:00), SE4 4 353 (28 Nov 08:00) - the zones' maxima are not coincident and sum to 24 087. **Svenska kraftnät's own peak-hour table** (`Topplasttimmen`, svk.se, from the yearly *Kraftbalansen* reports, "MWh/h, corrected to include …"): winter 2022/2023 **23 900 MWh/h on 2022-12-16 09–10**, winter 2023/2024 **25 200 on 2024-01-16 09–10**, 2024/2025 22 500 on 2025-01-13 08–09; the series runs back to 2002/2003 (26 400) with a maximum of 26 900 in 2003/2004. Calendar 2023's maximum sits between the two winters that bracket it - consistent. The 1-in-N cold year (spec-let §3.1) has its series here: twenty-three winters of peak hours, Svk's, on one page - not in stage 2, named.

## 4. Sweden's four zones and their links — sourced from the authorities

**The four, and since when.** Energimarknadsinspektionen, page *Elområden* (reached 2026-09-10): *"Sverige består av fyra elområden"* - Elområde Luleå SE 1, Sundsvall SE 2, Stockholm SE 3, Malmö SE 4; *"Sverige delades in i fyra elområden 2011."* Ei report **R2012:06** *Elområden i Sverige* (`ei_R2012_06_elomraden.pdf`, 2 249 652 bytes): *"Sedan 1 november 2011 är Sverige indelat i fyra elområden."* Nord Pool, *Bidding areas* (`nordpool_bidding_areas.html`): *"For each Nordic country, the local TSO decides which bidding areas the country is divided into … Sweden was divided into four bidding areas on in 2011"* [sic]. Svenska kraftnät, *Elområden* (`svk_elomraden.html`): *"Gränserna mellan elområdena går där det finns struktruella flaskhalsar"* [sic]; the division followed the Commission's finding that limiting exports to protect operational security breached competition rules. **⚠ The map is under review:** the same page records a government commission of 15 May 2025 to Svenska kraftnät to analyse changing the division, extended in May 2026 to a report on structural congestion - *"första steget i en formell översyn"* - due to the ministry by **29 January 2027**. The zones stand; the day they change is on the calendar.

**The links between them - the figures that decide whether Sweden has zones (S15).** Two Svenska kraftnät documents, both on disk:

| link | Svk *Information karta överföringskapacitet* (`svk_textforklaring_karta_overforingskapacitet.pdf`, undated; "maximal överföringskapacitet … då inga begränsningar finns som kan hota driftsäkerheten") | Svk *Mål för ökning av överföringskapaciteten mellan Sveriges elområden*, Svk 2023/2801, **2024-09-30**, Table 1 "Maximal tilldelad NTC 2021–2023" | the same report's targets 2030 · 2035 · 2040 · 2045 |
|---|---|---|---|
| SE1 → SE2 (snitt 1) | 3 300 MW | **3 300** | 3 300 · 3 300 · 3 700 · 4 000 |
| SE2 → SE1 | 3 300 | **3 300** | 3 300 · 5 500 · 6 500 · 7 500 |
| SE2 → SE3 (snitt 2) | 7 300 | **7 300** | 8 100 · 9 600 · 10 500 · 10 500 |
| SE3 → SE2 | 7 300 | **7 300** | 7 300 · 7 300 · 7 300 · 7 300 |
| SE3 → SE4 (snitt 4) | 6 200 | **5 600** | 6 200 · 6 200 · 6 200 · 6 200 |
| SE4 → SE3 | 2 500 | **2 800** | 2 800 · 3 300 · 3 500 · 3 600 |

The two agree on snitt 1 and 2 and differ on snitt 4 (6 200 ⁄ 2 500 against 5 600 ⁄ 2 800): the undated map states the network's maximum, the dated report the maximum the market was actually allocated in 2021–2023. **The seed is the dated report's column** (bold); the map's figure is kept as the ceiling. The report also says what the form is: NTC *"kommer inte längre att vara styrande … när flödesbaserad kapacitetsberäkningsmetod införs i Norden"* - flow-based capacity calculation went live in the Nordics in 2024, so the model's NTC form is the pre-flow-based one, and says so. Design's prototype drew 3 300 ⁄ 7 300 ⁄ 5 400 (§452) - the first two are the sourced figures, the third is not.

**The interconnectors, from the same map text (MW, out of Sweden ⁄ into Sweden):** SE1–Finland 1 500 ⁄ 1 100; SE1–Norway 600 ⁄ 700; SE2–NO4 300 ⁄ 250; SE2–NO3 1 000 ⁄ 600; SE3–NO1 2 095 ⁄ 2 145; SE3–Finland 1 200 ⁄ 1 200; SE3–DK1 715 ⁄ 715; SE4–DK2 1 300 ⁄ 1 700; SE4–Germany 615 ⁄ 600; SE4–Poland 600 ⁄ 600; SE4–Lithuania 700 ⁄ 700. The spec-let's rule stands: neighbours outside the six are a net-import series (`nrg_cb_e` IMP 7 330 ⁄ EXP 35 822 GWh for Sweden 2023), not cleared; these capacities bound it.

**What each zone produced in 2023** (eSett `EXP16/Aggregate`, settlement production, TWh): SE1 25.05 (hydro 19.15, wind 5.70, thermal 0.18); SE2 48.24 (hydro 33.25, wind 13.87, thermal 1.06, solar 0.07); SE3 73.03 (nuclear 46.69, hydro 11.80, wind 9.12, thermal 4.24, solar 1.13); SE4 9.23 (wind 5.64, hydro 1.64, thermal 1.29, solar 0.64). Sum 155.55 against Eurostat's net production 163 109 GWh (−4.6 %: eSett settles what is metered into the grid; small PV and industrial own-use are outside it) - the apportionment key of §2, its basis stated. Nuclear is SE3's alone; the north makes 73 TWh and uses 26; the south uses 21 and makes 9 - the whole case for the zones, in the numbers.

## 5. Fuel and carbon prices, 2023 — the variable-cost inputs of stage 3

| series | source | 2023 | state |
|---|---|---|---|
| Crude oil, Brent | World Bank *Pink Sheet* (`CMO-Historical-Data-Monthly.xlsx`, "Updated on January 03, 2025", sheet *Monthly Prices*, nominal US$) | **82.62 $/bbl** (twelve monthly figures averaged: 83.09 … 77.86) | fetched |
| Coal, Australian | same | **172.78 $/t** (317.99 in January to 126.82 in November) | fetched |
| Natural gas, Europe (TTF) | same | **13.11 $/mmbtu** (20.18 → 9.55 → 11.51) | fetched |
| Natural gas, US (Henry Hub) | same | **2.54 $/mmbtu** | fetched |
| Natural gas, industrial, delivered | Eurostat `nrg_pc_203`, band I3 (10 000–99 999 GJ), €/GJ GCV excluding VAT, 2023-S1 ⁄ S2 | DE 22.44 ⁄ 20.60 · FR 22.86 ⁄ 21.66 · IT 25.35 ⁄ 17.21 · PL 28.86 ⁄ 23.83 · SE 33.24 ⁄ 35.44 | fetched |
| EU ETS allowance (EUA) | **ICAP Allowance Price Explorer**, `api/systems` (`icap_allowance_price_explorer_systems.json`, 1 456 102 bytes), system 34 *European Union Emissions Trading System (from 2019)*; each date carries [US$, €, €] | **secondary market €85.51** (mean of 250 trading days) · **auctions €83.60** (mean of 223 auctions); for context 2022 €81.04, 2024 €66.43 | fetched - the spec-let's row 15 closed |
| IEA End-use Energy Prices | — | — | **PAID, struck** (spec-let §4 row 14); the rows above are its substitutes |

## 6. The retail stack, 2023 — Eurostat's price components, the seed and the check of stage 4's decomposition

€ per kWh, annual, **household band DC (2 500–4 999 kWh)** from `nrg_pc_204_c` and **non-household band IC (500–1 999 MWh)** from `nrg_pc_205_c`. The stack is energy and supply + network + taxes, fees, levies and charges; the last is itself VAT + renewable + capacity + environmental + nuclear + other:

| | energy & supply | network | taxes, fees, levies, charges | of which VAT | of which environmental | of which capacity | of which renewable | **total** |
|---|---|---|---|---|---|---|---|---|
| DE household | 0.1992 | 0.0937 | 0.1146 | 0.0650 | 0.0205 | 0.0101 | 0.0036 | **0.4075** |
| FR household | 0.1360 | 0.0659 | 0.0419 | 0.0346 | 0.0011 | 0.0062 | 0 | **0.2438** |
| IT household | 0.2359 | 0.0492 | 0.0720 | 0.0309 | 0.0160 | 0.0057 | 0.0170 | **0.3571** (an "other allowance" of 0.0368 beside it - the 2023 bonus sociale) |
| PL household | 0.0540 | 0.0542 | 0.1008 | 0.0391 | 0.0457 | 0.0094 | 0.0062 | **0.2090** |
| SE household | 0.0811 | 0.0762 | 0.0817 | 0.0478 | 0.0337 (energiskatten) | 0 | 0.0002 | **0.2390** |
| DE non-household | 0.1349 | 0.0548 | 0.0784 | 0.0428 | 0.0205 | 0.0098 | 0.0036 | **0.2681** |
| FR non-household | 0.2057 | 0.0252 | 0.0466 | 0.0442 | 0.0006 | 0.0018 | 0 | **0.2775** |
| IT non-household | 0.1623 | 0.0214 | 0.0891 | 0.0368 | 0.0110 | 0.0057 | 0.0313 | **0.2728** |
| PL non-household | 0.0998 | 0.0420 | 0.1246 | 0.0498 | 0.0538 | 0.0136 | 0.0059 | **0.2664** |
| SE non-household | 0.0700 | 0.0266 | 0.0250 | 0.0243 | 0.0005 | 0 | 0.0002 | **0.1216** |

The USA's retail stack is BILLED - EIA's Electric Power Monthly Table 5.6.A carries average retail price by sector, not its components; the components exist in EIA-861 (revenue by class) and are the build's fetch if wanted.

**Built 2026-09-11 (EN-4, `COMPLETED.md` §463):** these components are the seed of `EnergyLedger`'s stack - `EnergyData/retail_2023.csv` carries them per class with the 2023 consumption (`nrg_cb_e`/EIA), the policy levies as renewable + capacity + nuclear + other, and the USA's EIA averages with its components BILLED. The wholesale is the dispatch's and the supply margin is what Eurostat's energy-and-supply component leaves above it - FITTED, printed with its sign in `ENERGY_LAYER_PREMISE.md` §6: Germany +0.117 / +0.047, France +0.057 / +0.132, Italy +0.156 / +0.077, Poland −0.031 / +0.018, Sweden −0.007 / −0.019, the USA +0.135 / +0.082 $ per kWh in the book's dollars, households / non-households. Eurostat's stated totals differ from the sum of their published components by up to 0.0001 €/kWh (Italy's and Poland's rows) - the stack's total is its parts' sum, the stated total printed beside it.

## 7. Electricity's weight in the price index — stage 4's pass-through, sourced (S8)

Eurostat `prc_hicp_inw`, 2023, per mille of the HICP basket: **electricity (CP0451)** DE 29.63 · FR 28.21 · IT 33.15 · PL 23.87 · **SE 70.50**; all household energy (CP045) 55.03 · 60.40 · 64.44 · 85.38 · 82.96. Sweden's electricity weight is two and a half times Germany's - the pass-through will be, too. The USA's relative importance of electricity in the CPI: **BLS, December 2023, CPI-U, Electricity 2.428 % = 24.28 per mille** - paid 2026-09-11 through web.archive.org (bls.gov answers 403 to this client); ⚠ the table's two columns are CPI-U and CPI-W, not two years. **Built as EN-5 (§465):** `Tools/energy_index_prep.pl` → `EnergyData/price_index_weights_2023.csv` → `EnergyLayerData.RetailPriceIndexWeightPerMille`, read by `EnergyPassThrough`.

## 8. Electrification of transport — the cross-term of spec-let §2.1, sourced for five

Eurostat `road_eqs_carpda`, passenger cars by motor energy, 2023 stock: battery-electric DE 1 408 681 of 49 098 685 (**2.87 %**), FR 868 138 of 39 358 421 (**2.21 %**), IT 219 540 of 40 915 229 (**0.54 %**), PL 51 364 of 21 796 947 (**0.24 %**), SE 291 673 of 4 976 366 (**5.86 %**); plug-in hybrids 1.88 · 1.46 · 0.62 · 0.21 · 5.47 %. The USA (EIA AEO) is BILLED; the term is zero for the USA until it lands, stated.

## 9. The EDGAR residual — a first reading, the method the build will use

The spec-let's §2.1 says the layer derives the electricity part of `PowerCo2PerCapita` and carries EDGAR's remainder (heat, refineries) as a stated residual. A first reading of how large it is: the 2023 fuel inputs to electricity and heat generation from Eurostat's complete balance (`nrg_bal_c`, `TI_EHG_E` by fuel, TJ) × the IPCC 2006 default CO₂ factors for stationary combustion (Vol. 2, Ch. 2, Table 2.2, kg/TJ net calorific; `ipcc2006_v2_ch2_stationary_combustion.pdf`), against the family's seed × the World Bank population it was divided by:

| country | combustion CO₂ of all electricity + heat fuel inputs | the EDGAR seed as a total | ratio | fuel-input split: electricity-only · CHP · heat-only | refineries' own energy use (`NRG_PR_E`) |
|---|---|---|---|---|---|
| Germany | 193.2 Mt | 177.4 Mt (2.13 × 83.29 M) | 1.09 | 67 · 28 · 4 % | 242 PJ |
| France | 27.7 | 23.9 (0.35 × 68.37) | 1.16 | 92 · 5 · 2 | 89 |
| Italy | 73.1 | 84.3 (1.43 × 58.98) | 0.87 | 58 · 41 · 1 | 204 |
| Poland | 115.9 | 117.8 (3.21 × 36.69) | 0.98 | 12 · 80 · 7 | 82 |
| Sweden | 5.8 | 5.9 (0.56 × 10.54) | 0.99 | 76 · 16 · 8 | 49 |

**Read:** Sweden's and Poland's seeds are reproduced by the fuel inputs within two per cent; Germany and France overshoot (EDGAR's factors for lignite and for France's small plants differ from the defaults; EDGAR nets some autoproducer CHP into industry), Italy undershoots (EDGAR's *Power Industry* is IPCC 1A1 whole - refineries are 204 PJ of Italy's own use). The residual is therefore not one number: for Poland eighty per cent of the fuel goes through CHP, whose CO₂ the layer's electricity dispatch would only half move. **The build's method, fixed by this reading:** electricity-only plants' fuel × factor is the derived part; CHP's fuel is split by the electricity ⁄ heat output ratio of `nrg_ind_peh` (§1's producer split); heat-only plants and refineries are the residual, seeded from EDGAR's total minus the derived part at year 0 so the seed is reproduced exactly, and printed per country. The factors used here, from Table 2.2: anthracite 98 300 · coking coal 94 600 · other bituminous coal 94 600 · sub-bituminous 96 100 · lignite 101 000 · coke oven coke 107 000 · brown coal briquettes 97 500 · coke oven gas 44 400 · natural gas 56 100 · petroleum coke 97 500 · other petroleum products 73 300 · peat 106 000 · industrial wastes 143 000 · municipal wastes, non-biomass 91 700 · oil shale 107 000 (each read in the extracted text); blast furnace gas 260 000, refinery gas 57 600, LPG 63 100, gas/diesel oil 74 100 and residual fuel oil 77 400 are the same table's defaults and are re-read from the PDF at the build. Ember's per-source emissions in §1's file are lifecycle figures (non-zero for hydro, wind and nuclear) and are not this basis.

## 10. Costs and availability — the two rows the night could not reach

| row | what | state |
|---|---|---|
| technology CAPEX, O&M, capacity factors | IRENA *Renewable Power Generation Costs in 2024* | **BILLED** - irena.org answers 403 to this machine on the publication page and on the PDF; the alternatives named in the spec-let (IEA/NEA *Projected Costs 2020*, Lazard) not attempted tonight. An Elias-side download lands it under `sources/energy/` with its digest |
| de-rating (availability) by technology | ENTSO-E ERAA 2023 | page reached (`entsoe_eraa_2023_page.html`); the factors are inside the report PDFs, not read. **BILLED**; the build carries `[AUTHORED-DRAFT]` availabilities per technology line with the reason, replaced when the table is read |

## 11. What stage 2's build gets from this spine, and the gates it must pass

1. Six fleets by technology (§2, the office of record's column), Sweden's apportioned to four zones (§4's production key) - **gate:** the dump prints the three capacity figures side by side and the thermal divergence tag.
2. Six load-duration folds plus four zonal ones (§3) - **gate:** block energies equal the series; Poland scaled to inland demand with the factor printed.
3. Sweden's six directed links at the 2021–2023 NTC (§4) - **gate:** the four zones clear as one when the links are set infinite (a regression the check owns), and SE4's price exceeds SE1's at the seed when they are not.
4. The mix and the power intensity derived - **gate:** the 2023 seeds reproduced within §342's point (§1) and the EDGAR seed reproduced exactly at year 0 with the residual printed (§9).
5. Prices for stage 3 (§5) and the retail stack for stage 4 (§6), carried nominal in their vintage (P5-B6).

## 12. The files, by digest (first sixteen hex of sha256; all under `PoliSim-captures/sources/energy/`)

`eurostat_nrg_bal_peh_2023.json` 806dd3ddfd8c7b0f · `eurostat_nrg_cb_e_2023.json` 1ab2e59772e6dd3a · `eurostat_nrg_inf_epc_2023.json` 7332474dc632e0bc · `eurostat_nrg_inf_epcrw_2023.json` 3de213ae6e03aec2 · `eurostat_nrg_ind_peh_2023.json` 1a0c35e3eb7ec99f · `eurostat_nrg_pc_204_c_2023.json` e2874fda6f0a9073 · `eurostat_nrg_pc_205_c_2023.json` fe08550f15b719da · `eurostat_nrg_pc_203_2023.json` 249334e26c38dbe2 · `eurostat_prc_hicp_inw_2023.json` 4c4283ff0d0aa716 · `eurostat_road_eqs_carpda_2023.json` 9bcf968f4bc9f4c6 · `eurostat_nrg_bal_c_2023_TI_EHG.json` 47c6e70624e7d03e · `ember_release_generation_yearly_global.csv` b535c81592424ba9 · `ember_electricity_data_methodology.pdf` 4044e49f2aa7de3b · `energycharts_public_power_de_2023.json` 27160415c03736cd · `_fr_` 8e492ecfb3965eb9 · `_it_` f624025c46c5e2a3 · `_pl_` 4630474b0b1ee2d9 · `_se_` 516e228f50e5dba6 · `esett_EXP15_SE1..SE4_2023.json` 2dc7e99ab3a2adb0 · ca0cafffe281f222 · 995009d07acf06e8 · 9106d09ac1db167b · `esett_EXP16_SE1..SE4_2023.json` 15541f79c02a0141 · 585377f1ff5de08b · e5bd187de86f0694 · 0daebad4a25dd419 · `EIA930_BALANCE_2023_Jan_Jun.csv` a0e14e9c07ab4cc1 · `EIA930_BALANCE_2023_Jul_Dec.csv` c719fc1b513eec89 · `eia8602023.zip` 1447e23e608bea15 (21 237 983 bytes, unopened) · `eia_epa_04_02_a.xlsx` a6833d440bb24027 · `eia_epm_table_1_01.xlsx` ad2fbb8da38bda78 · `eia_epm_table_6_02_a.xlsx` ff6ab66bd05b6eab · `eia_epm_table_6_02_b.xlsx` 8e7c426e65095d2c · `wb_CMO-Historical-Data-Monthly.xlsx` bd89b83eeceadaec · `icap_allowance_price_explorer_systems.json` c5d0f5d465ff9445 · `ipcc2006_v2_ch2_stationary_combustion.pdf` a25d9f96d7672df3 · `svk_textforklaring_karta_overforingskapacitet.pdf` 5b5241379b9659b2 · `svk_mal_overforingskapacitet_slutrapport_2024.pdf` 79c9000cb924b450 · `svk_stamnatskarta_sv.pdf` 60c2005d870e15da · `ei_R2012_06_elomraden.pdf` 30bddba9312fe5ed · `nordpool_bidding_areas.html` 6d31698c6c1b3c3c · `svk_elomraden.html` b8fc388f0c3e5d35 · `svk_om-kraftsystemet_kraftsystemdata_topplasttimmen_.html` 53b35b30bfe7973c · `entsoe_eraa_2023_page.html` c5221b411209abe0. The scripts that read them (`jsonstat.pl`, `xlsx2tsv.pl`, `eia930_us48.pl`, `loadblocks.pl`, `pdftext.pl`) are the session's; they move to `Tools/` with the build.

## 13. The reservoir dispatch's sources (EN-3b, 2026-09-11, `COMPLETED.md` §466)

- **Sweden's zonal prices 2023, by the model's load blocks:** energy-charts.info `/price?bzn=SE1..SE4` (Fraunhofer ISE's republication of Nord Pool's day-ahead series, hourly; its licence note keeps the data to private and internal use - the seed carries three block means per zone, not the series). Folded on the P10 / P90 of the hourly national load (energy-charts `public_power`, Load): SE1 and SE2 19.94 / 37.04 / 83.41 (load-weighted 43.92), SE3 19.99 / 47.58 / 116.29 (57.68), SE4 36.58 / 61.21 / 122.45 (69.95) €/MWh. The same fold for the proxy's markets: DE-LU 72.83 / 93.48 / 131.06 (98.31), PL 80.24 / 111.02 / 148.09 (115.10).
- **The coupling to the continent:** the OLS slope of each zone's hourly price on the 615/600-weighted DE-LU/PL hourly price over 2023's 8 760 hours - SE1 0.3039, SE2 0.3039, SE3 0.6003, SE4 0.8170 - DERIVED (`Tools/energy_hydro_prep.pl`).
- **The reservoirs:** Energiföretagen, "Aktuellt magasinsläge Sverige" (weekly report, fetched 2026-09-11): 100 % = 33.7 TWh; per zone the report's fill and energy give the capacity - SE1 14 804, SE2 15 677, SE3 2 846, SE4 220 GWh (33.5 together). The 2023 year-end fill is not in the text the PDF yields - BILLED.
- **The fleets' storage character:** Eurostat `nrg_inf_epcrw` 2023 - RA100 hydro, RA110ROR run-of-river, RA130 pumped: Germany 10 951 / 4 269 / 5 345 MW → shiftable 0.2385; Italy 22 912 / 6 112 / 3 970 → 0.6774; Sweden 16 406 / 0 / 0 → 1 (regulated rivers); ⚠ France (26 058 / 0 / 1 728) and Poland (2 410 / 0 / 1 423) report run-of-river as 0 - a reporting hole for France - and the USA has no row: BILLED, no shift modelled.
- **Italy's reservoir capacity:** Terna's statistics pages are script-rendered and yielded no file from this machine - BILLED; Italy's shift runs on the turbines and the shiftable energy alone.

## 14. The carbon tax's coverage and indexation - the statutes (EN-4d and EN-4e, 2026-09-11/12, `COMPLETED.md` §467)

- **Sweden - the exemption:** lag (1994:1776) om skatt på energi, 6 a kap. 1 § (lagen.nu, fetched 2026-09-11; the riksdagen.se text reached the same paragraph): the table's rows *bränsle för framställning av skattepliktig elektrisk kraft* and *bränsle som förbrukas i en anläggning för vilken utsläppsrätter … måste överlåtas* - each **100 per cent** relief of the koldioxidskatt.
- **Sweden - the indexation:** the same law, 2 kap. 1 b § in its wording as amended by lag (2026:372): *"För kalenderåret 2028 och efterföljande kalenderår ska energiskatt och koldioxidskatt betalas med belopp som räknas om enligt andra och fjärde styckena. Regeringen fastställer varje år före november månads utgång de skattebelopp som enligt denna paragraf ska betalas för påföljande kalenderår."* The *jämförelsetal* is the general price level in June of the recalculation year over June of the year before, rounded to four decimals; the energy tax on the motor fuels (1 § första stycket 1, 2, 3 b and 7) adds 0.02 × the standing rates on top - the carbon tax is indexed by the price level alone. ⚠ The paragraph's earlier wordings (the one in force for 2023's 1 330 SEK) were not fetched - the current text is cited as the standing rule; the model carries a nominal-fixed rate (EN-4e).
- **Germany:** Brennstoffemissionshandelsgesetz (BEHG) § 7 Abs. 5 (gesetze-im-internet.de, fetched 2026-09-11): *Doppelbelastungen infolge des Einsatzes von Brennstoffen in einer dem EU-Emissionshandel unterliegenden Anlage* are to be avoided beforehand - the BEHG is the non-ETS instrument by construction, and its § 10 price (30 EUR for 2023) stands as the seed.
- **Germany - the schedule (EN-4e, fetched 2026-09-12):** BEHG § 10 Abs. 2 - the fixed price per certificate 25 EUR for 2021, 30 EUR for 2022 and 2023, 45 EUR for 2024, 55 EUR for 2025; *"Für das Jahr 2026 wird ein Preiskorridor mit einem Mindestpreis von 55 Euro pro Emissionszertifikat und einem Höchstpreis von 65 Euro pro Emissionszertifikat festgelegt"*; from 2027 an auction referenced to the EU ETS 2 by ordinance (Abs. 3) - no figure in the law. The model carries the fixed prices, the 2026 corridor at its floor (CONVENTION, the ceiling named), and thereafter the last legislated figure by the price level (`CarbonRateStatute`).
- **France - the tariffs' form (EN-4e, fetched 2026-09-12):** the composante carbone is a component inside the TICPE, TICGN and TICC tariffs, written as nominal amounts per unit (article 265 du code des douanes, since 2022 the code des impositions sur les biens et services - the article itself not reached, legifrance 403, BILLED). Its steps: 7 EUR/t 2014, 14.5 2015, 22 2016, 30.5 2017 (ecologie.gouv.fr, fiscalité carbone), 44.6 in 2018 by the loi de finances pour 2018, article 16, which also wrote 55 / 65.4 / 75.8 / 86.2 for 2019–2022 (the Sénat's PLF 2019 report a18-152-11, the tariffs *"exprimées en centimes d'euro par litre"*); the 2019 step was not enacted after the gilets jaunes and the component *"est depuis gelé à 44,6 €/t CO2"* (connaissancedesenergies.org, 6 September 2024). No indexation is written: the rate is nominal in law and the model holds it so.
- **Sweden - the rounding (EN-4e):** 2 kap. 1 b § tredje stycket, *"Jämförelsetalet ska avrundas till fyra decimaler"* - the model rounds the year's price ratio to four decimals before it multiplies the rate.
- **France:** the composante carbone of the TICPE, TICGN and TICC (ecologie.gouv.fr's fiscalité-carbone page, fetched 2026-09-11): installations under the quota regime stay at the taxes in force on 31 December 2013; the code article itself (legifrance) was not reached from this machine - BILLED, the ministry's statement cited.
- **The deviation the model makes:** every statute exempts per INSTALLATION (an installation that surrenders allowances); the model keeps no register of installations, so the exemption is applied at SECTOR level - the whole power sector exempt, the whole transport sector taxed; a heat plant outside the ETS, taxed in law, sits in the power figure's residual here and is exempted with the fleet. Stated in `EnergyMarket`'s class doc, `TaxBases.Emissions` and the seed comment in `WorldFactory`.

## 15. The electricity tax - the statutes of the five and the EU floor (EN-7b's sourcing, 2026-09-14, `COMPLETED.md` §499)

Fetched 2026-09-14 for EN-7b, energy stage 5's law category (§474): each country's statutory electricity tax in the seed year with its paragraph, the reforms that moved it, and the EU's floor. Every file under `PoliSim-captures/sources/energy_tax/` is the authority's own page, gazette, dataset or a consolidated-text publisher's edition, saved unedited; every quote was checked against its saved file by a second reader that did not reuse the fetcher's scripts. The figures below are EXTRACTED by pattern from the HTML and JSON editions in `Tools/energy_tax_prep.pl` (`perl Tools/energy_tax_prep.pl ../PoliSim-captures EnergyData` → `EnergyData/electricity_tax_2023.csv`) - a pattern that does not match stops the script; the PDFs (gazettes, the ADM tables) corroborate and are not parsed. Since EN-7b (§500) the runtime reads it: `EnergyCatalogGenerator` emits the rates and the floors into `EnergyLayerData` (`ElectricityTaxEurPerMwh`, `ElectricityTaxFloorEurPerMwh`, the digest guarded by `GeneratedCatalogCheck`), and the ledger moves the component by a statute's change within the coverage below, capped at 1.

**The statute against the seed** - the rate the class's Eurostat band pays, converted at the ECB 2023 reference rate (SEK 11.4787584313725, PLN 4.5419658823529 per euro), beside the seed's environmental-tax component (`retail_2023.csv`'s `tax_env`, Eurostat TAX_ENV + TAX_NUC) and the ratio of the two - the COVERAGE, what a change of the statute can move in the stack:

| country | class | the statute | 2023 rate | EUR/kWh | the seed's component (EUR/kWh) | coverage | note |
|---|---|---|---|---|---|---|---|
| DE | households | StromStG § 3 | 20.5 EUR/MWh | 0.020500 | 0.0205 | **1.000** | the standard rate since 1 January 2003 |
| DE | nonhousehold | StromStG § 3 | 20.5 EUR/MWh | 0.020500 | 0.0205 | **1.000** | the standard rate - manufacturing's relief of 5,13 EUR/MWh (§ 9b a.F.) is a refund on application, outside the band's figure |
| SE | households | LSE (1994:1776) 11 kap. 3 § + SFS 2022:1590 | 39.2 öre/kWh | 0.034150 | 0.0337 | **0.987** | the northern municipalities pay 9.6 ore less (11 kap. 9 §) - the band averages both |
| SE | nonhousehold | LSE (1994:1776) 11 kap. 9 § 1 st. 6 and 2 st. | 0.6 öre/kWh | 0.000523 | 0.0005 | **0.957** | the industrial rate - the service sector pays the full rate - the band is read as manufacturing (an inference) |
| FR | households | CIBS L312-37 at the floor, loi 2022-1726 art. 64 | 1 EUR/MWh | 0.001000 | 0.0011 | **1.100** | the price shield - the tariff it replaced 25.6875 EUR/MWh |
| FR | nonhousehold | CIBS L312-37 at the floor, loi 2022-1726 art. 64 | 0.5 EUR/MWh | 0.000500 | 0.0006 | **1.200** | the price shield |
| IT | households | D.Lgs. 504/1995 Allegato I + D.M. 30.12.2011 art. 1 | 0.0227 EUR/kWh | 0.022700 | 0.0160 | **0.705** | the residence exemption of art. 52 c.3 e) (150 kWh a month up to 3 kW) lowers the band's average - consistent with, not proven |
| IT | nonhousehold | D.Lgs. 504/1995 Allegato I (first 200 000 kWh a month) | 12.5 EUR/MWh | 0.012500 | 0.0110 | **0.880** | the first tier - the band's shortfall against it is not explained by the saved files |
| PL | households | ustawa o podatku akcyzowym art. 89 ust. 3 | 5 PLN/MWh | 0.001101 | 0.0457 | **41.514** | the excise is a small part of the band's environmental-tax figure - the rest is named by no saved document |
| PL | nonhousehold | ustawa o podatku akcyzowym art. 89 ust. 3 | 5 PLN/MWh | 0.001101 | 0.0538 | **48.872** | the excise is a small part of the band's environmental-tax figure - the rest is named by no saved document |

**The reading, per country.**
- **Germany** - the seed's component IS § 3's rate, both bands. Manufacturing's reliefs (§ 9b, and until 2023 § 10's Spitzenausgleich) are refunds on application; that Eurostat's IC band carries the gross rate is the reading the figures fit, not a sentence a saved file states.
- **Sweden** - households' component is the 2023 rate blended with the northern municipalities' deduction; non-households' is the industrial rate of 11 kap. 9 §, not the full rate the service sector pays (an inference: the band is read as manufacturing).
- **France** - the seed is the price shield: the accise held at the EU floors for 2023. A law that cuts France's electricity tax to the minimum does nothing in 2023's France; the restoration is the precedent (below).
- **Italy** - the household component is about seven tenths of the statute (the residence exemption of art. 52 c.3 e) is consistent with it; the contract mix is not in any saved file); the business component is under the first tier by a margin no saved file explains.
- **Poland - THE PREMISE FAILS.** The band's environmental-tax component is **41.514 times the excise for households and 48.872 times for non-households**. Eurostat's own metadata says the component "includes the excise duties" and names no other charge; its national-currency rows put the component near 204 zł/MWh (households) and 240 (non-households) against the excise's 5; ARE's survey tables give the effective excise as 5,0 zł/MWh for households, 4,3 medium voltage, 1,1 high voltage. What fills the rest is named by no fetched document. A law that scaled Poland's whole component as "the electricity tax" would scale charges nobody has named; a law on Poland's excise moves at most its own figure.
- **The EU floor** (Directive 2003/96/EC Annex I Table C, unchanged in the consolidated text of 10.01.2023): 0.5 EUR/MWh for business use, 1 for non-business; Art. 15(1)(h) allows households a total exemption, Art. 17 energy-intensive business down to 0 under agreements.
- **The USA** - no federal electricity excise (OECD Taxing Energy Use 2019, the US note, 2018 rates); states levy gross-receipts taxes (Florida's 2,5 %, a current page) - not a row: its retail components are BILLED.

**The precedents** (each act with its document id, each size read from its saved file; the laws' citations draw on these):
- **Sweden** - prop. 2016/17:142 (SFS 2017:399/400): households and the service sector +3,0 öre (2017) and +1,2 öre (2019), industry not raised · SFS 2020:1045: the industrial floor 0,5 → 0,6 öre · prop. 2022/23:1 (SFS 2022:1781): data centres lose the industrial rate from 1 July 2023 · prop. 2025/26:1 (SFS 2025:1357): the rate 43,9 → 36,0 öre from 2026. **Denmark** - LOV nr 1775 af 29/12/2025: the general elafgift to the EU minimum, 0,8 øre, for 2026-2027.
- **Germany** - the tax's introduction (BGBl. I 1999 S. 378, 20,00 DM/MWh; manufacturing at a fifth) · the schedule to 20,50 EUR from 2003 (BGBl. I 1999 S. 2432) · manufacturing's reduced rate to 60 % (BGBl. I 2002 S. 4602) · the 5,13 EUR relief (Haushaltsbegleitgesetz 2011, BGBl. I 2010 S. 1885) · manufacturing's relief to 20 EUR, net 0,50, for 2024-2025, and § 10 repealed (Haushaltsfinanzierungsgesetz 2024, BGBl. 2023 I Nr. 412) · the relief made permanent from 2026 (BGBl. 2025 I Nr. 340).
- **France** - the shield: to the EU floors from 1 February 2022 (loi 2021-1900 art. 29) and held for 2023 (loi 2022-1726 art. 64) · the partial restoration to 21 / 20,5 EUR/MWh from 1 February 2024 (loi 2023-1322 art. 92 and the arrêté of 25 January 2024) · the full restoration from 1 February 2025 (households 33,70) and the rewritten tariffs from 1 August 2025 (loi 2025-127).
- **Italy** - the municipal and provincial surcharges folded into the state excise from 2012 (the two D.M. of 30 December 2011) · the business tiers from 1 June 2012 (D.L. 16/2012 art. 3-bis, L. 44/2012).
- **Poland** - the excise 20 → 5 zł/MWh from 2019 (Dz.U. 2018 poz. 2538) · the 2022 shield: households exempt, others 4,60 (Dz.U. 2021 poz. 2349), extended to the year's end (Dz.U. 2022 poz. 2180) · 5 zł again from 2023, the relief lapsed.
- **The EU** - the harmonised minimum on electricity from 2004 (2003/96/EC); Latvia's and Malta's transitions up to it and the Czech Republic's and Ireland's temporary exemptions (2004/74/EC, Art. 18 and 18a).

**What the second readers flagged** (carried, not smoothed): Germany's "IC carries the gross rate" and Sweden's "IC is manufacturing" are inferences; France's bill article 7 is the law's article 20 by matching content, not by a sentence; France's 25,68 counterfactual likely leaves out the old local levy (the ministry's 32 "avant la crise"); Italy's household explanation is "consistent with", not proven; Poland's survey-column guess for the unnamed part is unsupported; Directive 2003/96/EC's business/non-business indent is the fourth of Art. 5.

**What is BILLED.** The code articles themselves for France (legifrance returns 403 - the Assemblée's adopted texts and the tax authority's pages stand in) and Italy (normattiva is a script shell - the Gazzetta Ufficiale and the customs agency's tables); Poland's ISAP (a bot wall - the Dziennik Ustaw PDFs); the Commission's "Excise duty tables Part II" 2023 edition (unreachable - its Taxes in Europe Database's JSON stands in); **the part of Poland's component that is not the excise** - no fetched document names it; the Statistical Office or ARE would have to be asked.

**The files, by digest** (sha256, first sixteen hex; under `PoliSim-captures/sources/energy_tax/`, 116 files):

| file | sha256 (16) | size |
|---|---|---|
| `de_3stromstaendg_bgbl2025_340.pdf` | `34f574832b5e3bfa` | 803422 bytes |
| `de_buzer_10_hfing2024_synopse.html` | `b91654f3fa2bd017` | 23641 bytes |
| `de_buzer_3_stromstg.html` | `2385c5f49d44243a` | 38288 bytes |
| `de_buzer_9b_hfing2024_synopse.html` | `59c2a68129318030` | 14539 bytes |
| `de_fortentwicklung_oekosteuer_bgbl2002_i_4602.pdf` | `e985998ef64fa675` | 23010 bytes |
| `de_fortfuehrung_oekosteuer_bgbl1999_i_2432.pdf` | `8fe92bd32d97b08b` | 27486 bytes |
| `de_hbeglg2011_bgbl2010_i_1885.pdf` | `d8bbb933a1d63e5a` | 114484 bytes |
| `de_hfing2024_bgbl2023_412.pdf` | `dda4da936b207f62` | 318888 bytes |
| `de_stroeg1999_bgbl1999_i_378.pdf` | `537cd84fc93facb1` | 23298 bytes |
| `de_stromstg_10.html` | `30dd4a7088c04aa6` | 3324 bytes |
| `de_stromstg_3.html` | `d37e1ee24f3ea292` | 3411 bytes |
| `de_stromstg_9.html` | `ab36737d9a7ed8de` | 12203 bytes |
| `de_stromstg_9b.html` | `7545d32f563bc8fa` | 5705 bytes |
| `de_stromstg_full.html` | `9172204a3447c445` | 79137 bytes |
| `dk_L24_2025_lovforslag.xml` | `b64c368d8efe2aff` | 122366 bytes |
| `dk_lov_2025_1775.xml` | `8256ceb23995a641` | 9401 bytes |
| `eu_2003_96_consol20230110.html` | `26130cbe139f8261` | 205852 bytes |
| `eu_2003_96_orig.html` | `137b47a0ecc454ab` | 189264 bytes |
| `eu_32011D0445.html` | `8f3f03eddab68055` | 11928 bytes |
| `eu_32015D0993.html` | `5c82b8fa58ae2ebb` | 13598 bytes |
| `eu_nrg_pc_204_sims.htm` | `4c3d691926435674` | 206039 bytes |
| `eu_tedb_de_20230101.docx` | `a2de92a258c5cad2` | 16377 bytes |
| `eu_tedb_de_20230701.docx` | `b5f687bdd4f5a653` | 16393 bytes |
| `eu_tedb_de_rate_20230101.json` | `1bea114ac6e70dbb` | 18524 bytes |
| `eu_tedb_de_rate_20230701.json` | `1bea114ac6e70dbb` | 18524 bytes |
| `eu_tedb_de_rate_20240101.json` | `134aef06856531b1` | 18524 bytes |
| `eu_tedb_de_rate_20240701.json` | `7d0aa635488fc2d1` | 18728 bytes |
| `eu_tedb_de_rate_20260101.json` | `28a57b0486d2e718` | 18563 bytes |
| `eu_tedb_fr_20230101.docx` | `4ab4e936480260ae` | 20001 bytes |
| `eu_tedb_fr_20230701.docx` | `00a00771cbb47b62` | 20001 bytes |
| `eu_tedb_fr_rate_20230101.json` | `61aa6befdeb728e7` | 15894 bytes |
| `eu_tedb_fr_rate_20230701.json` | `61aa6befdeb728e7` | 15894 bytes |
| `eu_tedb_fr_rate_20240101.json` | `c02d093f4028d305` | 15590 bytes |
| `eu_tedb_fr_rate_20240201.json` | `3375078db83c4a50` | 15593 bytes |
| `eu_tedb_fr_rate_20250201.json` | `71cf48624a9eb373` | 15581 bytes |
| `eu_tedb_fr_rate_20250801.json` | `aeee8bd7a768139b` | 15523 bytes |
| `eu_tedb_it_20230101.docx` | `b3ffea7def70687d` | 16687 bytes |
| `eu_tedb_it_20230701.docx` | `5bc17169dc5e9b35` | 16691 bytes |
| `eu_tedb_it_rate_20230101.json` | `04764f655ebd9110` | 18474 bytes |
| `eu_tedb_it_rate_20230701.json` | `04764f655ebd9110` | 18474 bytes |
| `eu_tedb_pl_20230101.docx` | `98e9a69a0081662a` | 18521 bytes |
| `eu_tedb_pl_20230701.docx` | `c121a9d684ede4d2` | 18522 bytes |
| `eu_tedb_pl_rate_20180701.json` | `a94cb8d4e29089b3` | 17257 bytes |
| `eu_tedb_pl_rate_20190101.json` | `963e02942eb22765` | 17250 bytes |
| `eu_tedb_pl_rate_20210701.json` | `395b28df135aa1a3` | 16884 bytes |
| `eu_tedb_pl_rate_20220101.json` | `3266b985dc7bd0af` | 16990 bytes |
| `eu_tedb_pl_rate_20230101.json` | `29b324e91dbc938c` | 16981 bytes |
| `eu_tedb_pl_rate_20230701.json` | `29b324e91dbc938c` | 16981 bytes |
| `eu_tedb_pl_rate_eur_20230101.json` | `77fe8f18895c16a0` | 17051 bytes |
| `eu_tedb_pl_rate_eur_20230701.json` | `77fe8f18895c16a0` | 17051 bytes |
| `eu_tedb_se_20230101.docx` | `19484896fde23822` | 17988 bytes |
| `eu_tedb_se_20230701.docx` | `2af34a4b69f74cad` | 18004 bytes |
| `eu_tedb_se_rate_20220701.json` | `7a2ccb2930463677` | 24039 bytes |
| `eu_tedb_se_rate_20230101.json` | `5d76789f5e71c444` | 23917 bytes |
| `eu_tedb_se_rate_20230701.json` | `0e5ed88eed59544f` | 24046 bytes |
| `eu_tedb_se_rate_20250701.json` | `9252ef5538230e99` | 24203 bytes |
| `eu_tedb_se_rate_20260101.json` | `3feccf21ce78639c` | 24203 bytes |
| `eu_tedb_se_rate_eur_20230101.json` | `52a3afd80c0b8d09` | 24103 bytes |
| `eu_tedb_se_rate_eur_20230701.json` | `9014d8fd8290ad0e` | 24238 bytes |
| `eu_tedb_search_20230101.json` | `5e6b867280219661` | 5032 bytes |
| `eu_tedb_search_20230701.json` | `e492f17359a6f8c6` | 5032 bytes |
| `eu_tedb_search_history.json` | `0b8f1ce3ddf30a20` | 91611 bytes |
| `fr_bofip_actu2023_53.html` | `0de22d4e10d68168` | 43290 bytes |
| `fr_bofip_res_eat147.html` | `9717aad73277ce8c` | 52812 bytes |
| `fr_bofip_res_eat240.html` | `6b054125f8844565` | 61882 bytes |
| `fr_dec2022-84_affpub.html` | `7bfd1193725494ad` | 2987 bytes |
| `fr_economie_fev2024.html` | `15f0a99f9f06f9f6` | 66922 bytes |
| `fr_impots_tarifs2025.html` | `c7ef39ab40666cb4` | 55968 bytes |
| `fr_impots_tarifs2025_jan.html` | `abf4ca7c88c14f48` | 51386 bytes |
| `fr_lf2022_art29_an_ta737.html` | `cf2f438c32ad9e0a` | 2522815 bytes |
| `fr_lf2023_art64_an_ta51.html` | `8f8ea9f5e23f8120` | 3290409 bytes |
| `fr_lf2024_art92_an_ta223.html` | `ef890f13842b2e8b` | 4902680 bytes |
| `fr_lf2025_art7_an_ta42.html` | `472f4b3332b9fb6c` | 3777289 bytes |
| `fr_plf2023_an_avis285.html` | `3245cf9c86513241` | 375718 bytes |
| `it_adm_aliquote_2023-01-01.pdf` | `10938dc52ce980f6` | 153847 bytes |
| `it_adm_aliquote_2026-01-01.pdf` | `e5e2abc38542b3a4` | 191352 bytes |
| `it_adm_aliquote_2026-04-08.pdf` | `9c51aabaaeb74db6` | 191995 bytes |
| `it_adm_tua_dlgs504.pdf` | `2ceb399e6e04e205` | 682170 bytes |
| `it_camera_dossier_dl16_2012.htm` | `354efb52ab021832` | 1957198 bytes |
| `it_edizionieuropee_tua.html` | `3dfd3a344aa21523` | 807062 bytes |
| `it_eurostat_nrg_pc_204_c_IT_DC_series.tsv` | `3db7a99f77079c25` | 4675 bytes |
| `it_eurostat_nrg_pc_205_c_IT_IC_series.tsv` | `2262a9f903e62c3a` | 4597 bytes |
| `it_gu_cc_ricorso_sardegna_012C0101.html` | `165a9f22f52bc1af` | 50017 bytes |
| `it_gu_dlgs43_2025_art1.html` | `319520bd5993f8cf` | 186428 bytes |
| `it_gu_dlgs43_2025_art11.html` | `dc8a137609a15e6c` | 1510 bytes |
| `it_gu_dlgs43_2025_art2.html` | `61e8c82e27cbf031` | 1654 bytes |
| `it_gu_dlgs43_2025_art8.html` | `341628169c6a733a` | 11875 bytes |
| `it_gu_dlgs43_2025_eli.html` | `ec1e601f5b43a592` | 17077 bytes |
| `it_gu_dm_2011-12-30_11A16869_art1.html` | `4a2dba9ccf9ccc8d` | 6305 bytes |
| `it_gu_dm_2011-12-30_11A16869_art2.html` | `ba1aff4aae1968d6` | 1004 bytes |
| `it_gu_dm_2011-12-30_11A16870_art1.html` | `ef2ba62318a28f48` | 4525 bytes |
| `it_gu_dm_2011-12-30_11A16870_art2.html` | `ba1aff4aae1968d6` | 1004 bytes |
| `it_gu_sg_2011-12-31_n304_index.html` | `ea7eb4a2517cab67` | 66095 bytes |
| `pl_are_statystyka_elektroenergetyki_2023.pdf` | `3df8b0334cd9c815` | 7146927 bytes |
| `pl_dzu_2018_1114_akcyza_tj.pdf` | `d767bb3081876423` | 3042884 bytes |
| `pl_dzu_2018_2538_akcyza_5zl.pdf` | `9b4f69c0d5b41278` | 368925 bytes |
| `pl_dzu_2021_2349_akcyza_tarcza.pdf` | `7b757c3d05f2072e` | 224478 bytes |
| `pl_dzu_2021_2350_page.html` | `79abbd383e1df403` | 9532 bytes |
| `pl_dzu_2022_2180_akcyza_extension.pdf` | `d743e97ec2e71352` | 600178 bytes |
| `pl_dzu_2023_1542_akcyza_tj.pdf` | `2236e8660b83ca37` | 3788145 bytes |
| `pl_eurostat_g11e_guidebook_2023.pdf` | `b8ad3eaaee2f85d8` | 198586 bytes |
| `pl_eurostat_nrg_pc_204_c_PL_DC_series.tsv` | `d8a9dca7205f1a12` | 2666 bytes |
| `pl_eurostat_nrg_pc_204_sims_pl.htm` | `00ba67737e003240` | 217954 bytes |
| `pl_eurostat_nrg_pc_205_c_PL_IC_series.tsv` | `0e0d3b970b112d58` | 2566 bytes |
| `se_lse_lagennu.html` | `e5b16344d2ddbc5d` | 1796646 bytes |
| `se_lse_lagennu_kons_2019_491.html` | `50b81ad104a69769` | 226158 bytes |
| `se_lse_lagennu_kons_2023_203.html` | `a5f510c8ceb6a084` | 332154 bytes |
| `se_lse_lagennu_kons_2025_99.html` | `b07ca7b9b093d99b` | 342695 bytes |
| `se_lse_riksdagen.html` | `1a1dc05e27e850b9` | 968230 bytes |
| `se_prop_2016_17_142.html` | `e07ecda1a1fad1cd` | 928565 bytes |
| `se_prop_2022_23_1_finansplan.html` | `1b31f8298b17eeba` | 5132080 bytes |
| `se_prop_2025_26_1_finansplan.html` | `47a5f484a8558aa9` | 10424294 bytes |
| `se_sfs_2022_1590_riksdagen.html` | `2cdc1cf6339bc4e6` | 263059 bytes |
| `se_skv_skattpael.html` | `b47127535fd134b6` | 465154 bytes |
| `us_fl_dor_grt_utility.html` | `64fc6a706232f54b` | 60471 bytes |
| `us_oecd_teu2019_note.pdf` | `3684f4697dd35c55` | 1412540 bytes |

