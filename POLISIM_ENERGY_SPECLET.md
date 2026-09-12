# The energy spec-let and sourcing bill (stage 1 of `ENERGY_SYSTEM_ADAPTED.md`)

**Status: DOCUMENT ONLY. No energy code is built from this until Elias rules on it.** Every line of §6
is meant to be struck, amended or approved on its own; §7 is a recommendation and is strikeable whole.
The precedent is C-C12's `POLISIM_TAX_SPECLET.md` (E-27) and C-C13: the spec-let is ruled before any code.

**Status: RULED 2026-09-10 (Elias, E-34; `COMPLETED.md` §454) - S1–S15 as written - **S14 amended 2026-09-12 (DS-4, §474)**, the two forks S3 (the P90 cut) and S10 (appended members) taking the recommendation, §8 as the order of work.** Stage 2's sourcing landed the same night (`ENERGY_LAYER_SPINE.md`, §455) and **stage 2's build landed after it (§457): `EnergyLayer`, `EnergyLayerData`, `EnergyLayerCheck`, `EnergyLayerDump` → `ENERGY_LAYER_PREMISE.md` - readouts, the family's writer unchanged until stage 3.** **Stage 3 (EN-3) LANDED 2026-09-11 (§458–§460): `EnergyMarket` clears every block of every zone; the power figure's writer is the dispatch and the S4 deviation is closed; its own BASELINE family (`traj_en3`).** Stage 4 (EN-4) is the next energy code. The ruling's words: *"continue: the three false lines, the spec-let's fifteen rulings, and stage 2's sourcing."*

**What this document is not.** `ENERGY_SYSTEM_ADAPTED.md` is the concept adapted to this game; the
concept itself is an external research summary. **No figure from either is a datum.** Every number in
this document is either read off the repo (cited by file and line), read off a source reached today
(cited by series), or marked `[AUTHORED-DRAFT]` on its own line. Where a source could not be reached
from this machine the series is named exactly and marked **[PROVISIONAL]** - verified at fetch time,
never transcribed from memory.

---

## 1. What the model has today (read off the repo, 2026-09-10, HEAD 5430fcd)

There is **no energy layer**. What exists is five unrelated places that each hold a piece of one:

| place | what it holds | who writes it | who reads it |
|---|---|---|---|
| `EnvironmentSeeds.MixShares` (`Assets/Scripts/Data/EnvironmentFamily.cs:26`) | electricity mix 2023, seven shares (coal, gas, nuclear, hydro, wind, solar, other), Ember via OWID cross-checked vs Eurostat `nrg_bal_peh` and EIA Table 1.1 within a point (§342) | the seed, once; *"a static seed - nothing moves it"* | the Environment plate's distribution row (`GameController.Environment.cs:50`) |
| `EconomyState.PowerCo2PerCapita`, `TransportCo2PerCapita` (`EconomyState.cs:187-188`) | t CO₂ per person, EDGAR 2024 Power Industry / Transport over World Bank population 2023 | `EnvironmentFamily.AdvanceYear` (called `SimulationManager.cs:2610`), yearly: seed × carbon-tax elasticity × spending-line ratio, reverting | `TaxBases` driver `Emissions` (`TaxBases.cs:29-31, 79-81`): the carbon tax's base since §349 = (power + transport per head) × population; the Environment plate |
| `TaxType.CarbonTax` on `Country.TaxLines` | a rate 0–`CarbonTaxMax` = 100 (`TaxLine.cs:68`), implemented for Poland only at seed (`WorldFactory.cs:217`, 30), the other five 5 unimplemented | the player / the AI ministry | `EnvironmentFamily.AdvanceYear` at `CarbonTaxElasticityPerPoint = 0.004` per point `[AUTHORED-DRAFT]`; revenue = rate × base |
| `Sector[SectorType.Energy]` (`Sector.cs`, seeds `WorldFactory.cs:691-736`) | output share of GDP, employment share, a sector metric, five dials 0–100 (Subsidy, Regulation, Tax Credits, Research Grants, Nationalization/Deregulation at `Sector.cs:79`) | the player through the sector bill | `MacroSystem.ApplySectorEffects`; *descriptive* - *"do NOT feed back into the core national accounts identity"* (`Sector.cs` head) |
| `SpendingCategory.Energy` (seeds `WorldFactory.cs:1227, 1312, 1556, 1645, 1737`) | a budget line, indexed by `IndexSpendingLines` (P5-B2) | the player / the AI ministry | `EnvironmentFamily.PerHead` (the family's `EnergyPerHeadSeed` ratio, elasticity 0.15 `[AUTHORED-DRAFT]`) **and** `PolicyDecision.EnergySpendingChange` → `BusinessConfidence` (`MacroSystem.cs:2253, 2330, 2495`) |

Plus three events in the catalogue that are energy by name and carry their whole effect as typed
constants (`EventSystem.cs`): *Energy Price Shock* (:135, −1.5 % GDP, +1.0 inflation), *Cold Snap
Energy Squeeze* (:319, −0.4 %, +0.6), *Global Commodity Price Spike* (:93) and its mirror *Commodity
Prices Collapse* (:396). No event is state-triggered; the pool is a random draw at
`EventChancePerTurn = 0.40`.

**Nothing in the model knows an electricity price.** `EconomyState` has one scalar `Inflation`
(:16) and a `PriceLevel` (:19, P5-B6); there is no energy price, no CPI component, no retail tariff,
no wholesale price, no fuel price. There is no zone, no capacity, no load.

**Two stale sentences found while reading, not fixed (no energy code before the ruling; they are
recorded in §451 as a defect row):** the Environment plate's foot prints *"THE CARBON TAX'S BASE
STAYS ON OUTPUT - MOVING IT HERE IS SHEETED, NOT BUILT"* (`GameController.Environment.cs:56`) and
`SimulationManager.cs:2610`'s comment says the same; both have been false since §349 moved the base.
`ENVIRONMENT_FAMILY_SPINE.md:3` carries the same sentence. The plate's foot is player-visible.

## 2. The collision map — the whole risk

The energy layer would be **the third writer** of quantities the book already presents. The
single-book rider (no stored quantity may diverge from its presented value) makes each row below a
gate, not a note. **Stage 2 is not landed until every ⚠ row is closed one way: the field is derived
from the layer, or the layer does not touch it and says so on the screen.**

### 2.1 Against the environment family (P5-C5)

| field today | under the layer | the gate that proves the single book |
|---|---|---|
| ⚠ `MixShares` - static seed, seven shares | **DERIVED** every year: generation by technology from the dispatch, summed to the seven labels (`MixLabels`, `EnvironmentFamily.cs:81`); the seed becomes stage 2's *calibration target*, not a stored value | at seed year the derived mix reproduces the 2023 seed within the same tolerance §342 used (a point per share, six countries); `EnvironmentFamilyDiagnostic`'s Germany-coal assertion re-pointed at the derived figure |
| ⚠ `PowerCo2PerCapita` - EDGAR Power Industry ÷ population, moved by two authored elasticities | **DERIVED**: Σ (generation by technology × emission factor) ÷ population. ⚠ EDGAR's *Power Industry* is IPCC 1A1 - public electricity **and heat** and the other energy industries (refineries). The layer produces the electricity part only; the remainder is a **stated residual**, seeded once from the difference and carried without a mechanism, printed as such | at seed year electricity CO₂ + residual = the EDGAR seed exactly (the residual is defined that way); the residual's share printed per country so a reader sees what the layer does not move |
| ⚠ `TransportCo2PerCapita` | **NOT derived** by the layer in stages 2–5; keeps its infrastructure-line coupling. The layer adds one term: electrified transport moves demand *into* the power system and CO₂ *out of* this field - a cross-term that must net (the tonne leaves transport as it enters power at the grid's intensity) | Σ CO₂ before = Σ after when the electrification trend is held at zero; the trend itself is a sourced series (§4), else `[AUTHORED-DRAFT]` |
| ⚠ `CarbonTaxElasticityPerPoint = 0.004` (`:37`) | **RETIRED for power**: the carbon tax's rate enters variable cost in the merit order (fuel + carbon + variable O&M), and intensity falls because dispatch changes, not because a constant says so. **EN-4d (§467) took the national tax out of the merit order - ETS-covered plant is exempt of it by statute - so the carbon in the fleet's variable cost is the ETS price alone; the retirement stands, the lever the fleet answers is the ETS.** Stays for transport until that field is derived | the environment feedback pass's BASELINE (§349) re-explained per country: the same rate now moves the same field by a different mechanism |
| ⚠ `PowerEnergyLineElasticity = 0.15` (`:39`), `EnergyPerHeadSeed` (`:29`) | **RETIRED**: the Energy spending line stops being an elasticity on an intensity and becomes money in the incidence ledger (stage 4) - a support scheme, a network subsidy, a capacity payment, each a line that indexes (P5-B2) | the line's real per-head reading stays on the plate as a readout; nothing else reads it |
| `TaxBases` driver `Emissions` (`TaxBases.cs:79-81`) | **unchanged reader** - it reads the two fields; they change underneath it. Revenue follows its base (P5-B3) | the carbon tax's revenue at seed unchanged to the cent (same fields, same values at year 0) |
| ⚠ the EU ETS | five of the six countries' power sectors already pay a carbon price that is **not the player's carbon tax** (the ETS allowance price). Today the model has no such thing; Poland's *implemented* 30 (`WorldFactory.cs:217`) is the only trace. The layer needs the ETS price as a **sourced exogenous series** in variable cost, separate from `TaxType.CarbonTax`, or the merit order of every EU country is wrong from the first clearing. ⚠ `CarbonTaxMax = 100` has no unit stated in `TaxLine.cs`; read as €/t for this document, **to be confirmed** | at seed, the dispatch with the ETS price in reproduces the 2023 mix (the first gate); without it, it does not - that difference is the check that the series belongs |

### 2.2 Against `EconomyState` and the macro core

| quantity | the collision | disposition |
|---|---|---|
| ⚠ `Inflation` (one scalar) | a retail electricity price that moves and does not reach inflation contradicts the concept's own chain (wholesale → industrial cost → inflation) - but a pass-through is a **new Phillips input**, BASELINE on every country | **stage 4, its own family**, the pass-through weight sourced (electricity's weight in HICP, Eurostat `prc_hicp_inw` COICOP 04.5.1; the CPI relative importance for the USA, BLS) - never authored |
| `PriceLevel` (P5-B6) | fuel prices, CAPEX and O&M are nominal series in their vintage's prices; the book is in current prices | every price series enters with its vintage and is carried by `PriceLevel` forward - nominal with nominal, never across (B6) |
| `GDP`, `Population`, cohorts (F2) | demand's drivers | read-only inputs to the physical layer; the layer writes none of them |
| ⚠ `Sector[Energy].OutputShareOfGdp` (descriptive) | the layer produces a real system cost; a second, unrelated "energy output share" beside it is two books | **stage 4 derives it** (system cost ÷ nominal GDP) or the row is removed from the Sectors screen for Energy and says why - ruled in §6 |
| ⚠ the five Energy sector dials | the concept's four instruments (market liberalisation, retail intervention, investment planning, state ownership) vs the existing Subsidy / Regulation / Tax Credits / Research Grants / Nationalization-Deregulation rows | **mapped, not doubled**: Nationalization/Deregulation = state ownership; Subsidy = retail intervention's money side; Regulation = market liberalisation (0 light – 100 heavy inverted); Tax Credits = investment planning's incentive; Research Grants stays descriptive (no R&D system - refused in `ENERGY_SYSTEM_ADAPTED.md` §4). The Energy sector's five dials become the layer's instruments **with their existing save keys** (`SectorSubsidyOverrides`, `SectorDeregulationNationalizationOverrides` - append-only enums untouched) |
| ⚠ `PolicyDecision.EnergySpendingChange` → `BusinessConfidence` | once industrial electricity cost exists, this is a second channel for the same thing | **retired at stage 4** in favour of the industrial retail price → business confidence; until then untouched |
| `SpendingCategory.Energy`, `ClimateAndEnvironment` and the indexation (`IndexSpendingLines`, `SimulationManager.cs:3860-3890`) | support schemes are spending lines and index like any other (P5-B2) | new lines are **new `SpendingCategory` members appended** (serialized enum) or sub-lines of `Energy`; ruled in §6 |
| the AI finance ministry (§387–§388) | it moves the player's levers under the EU rule / US caps; support schemes are new levers | the ministry's rule set reads the new lines like any other; **the energy ministry (stage 7) does not touch the budget except through them** |
| ⚠ the event catalogue and FT-9 | *Energy Price Shock* and *Cold Snap* carry typed GDP and inflation shocks. Under the layer a fuel-price shock is a **fuel price series moving for a duration** and the GDP and inflation effects are *derived* through dispatch and the pass-through. FT-9's `ShockDurationDays = DaysPerTurn` is `[CONVENTION, TAGGED §407: to be struck when the catalogue carries a duration per event]` - **the energy events would be the first to carry one**, and the tag is struck by them | stage 8, last, after the pass-through exists; the three events keep their typed constants until then; the harness runs events off (§404) so the BASELINE families of stages 2–7 are unaffected |
| `DaysPerTurn = 365`, the daily path | the year clears once per block per zone at the turn boundary; a shock inside the year enters the daily path over its duration (FT-9) | the clearing is **annual**; no daily energy quantity exists. Stated on the screen |
| the hemicycle / approval | the concept's last links (price → approval → parliament) exist already | the retail price's approval term goes through the existing approval model, one term, sourced or `[AUTHORED-DRAFT]`; nothing new in Politics |

### 2.3 Against Design

Boards 12a–12f in the Design project are an Energy track drawn from a redirect this repo does not
hold (§449; three boards beyond the 256 KiB fetch cap). **They are not read for this document.** When
Elias records the redirect they become stage 6's input, read the D11 way - verified figure for figure
against stages 2–5 before anything is drawn from them.

## 3. The form — blocks and zones

### 3.1 The load-block form

The year per zone is three blocks and a peak case. **Hours per block are derived per country from
the country's own hourly load series, never typed**:

- **Source per country:** ENTSO-E Transparency item **6.1.A Actual Total Load**, hourly (15/30/60-min
  resolution by area, aggregated to hours), per bidding zone - SE1..SE4 for Sweden, the single zone
  for DE-LU, FR, IT (the Italian zones summed), PL - calendar year 2023 **[PROVISIONAL: the platform
  returns 403 from this machine; the item code and its knowledge-base article were found by search;
  the CSV export needs a free account]**. USA: EIA **Form EIA-930 Hourly Electric Grid Monitor**,
  hourly demand for the Lower 48 (US48 aggregate), 2023 **[PROVISIONAL: the page's about text did not
  render; the series was found by search]**.
- **The derivation** (one procedure, six countries, published in the spine as a table with the
  series' digest): sort the 8,760 hours descending (the load-duration curve). *Base* = the annual
  minimum, 8,760 h. *Peak* = the hours above the P90 of load, at their mean. *Mid* = the rest, at
  their mean. The block energies sum to the year's consumption (a gate against Eurostat `nrg_cb_e`
  / EIA net consumption within a point). **The P90 cut is `[AUTHORED-DRAFT]`** and is the one
  constant in this form; it is §6 S3.
- **The winter-peak case** = the maximum hour of the year, per zone, from the same series; the
  adequacy test compares it to dependable capacity (installed × a per-technology availability that is
  sourced - ENTSO-E's ERAA de-rating factors **[PROVISIONAL]** - or `[AUTHORED-DRAFT]` by line). A
  1-in-N cold year needs several years of load; **not in stage 2**, named.

### 3.2 Zones - Sweden four, the other five one each

**Sweden.** *"Sverige består av fyra elområden"* - Elområde Luleå SE1, Sundsvall SE2, Stockholm SE3,
Malmö SE4; *"Sverige delades in i fyra elområden 2011"* (Energimarknadsinspektionen, page
*Elområden*, `https://ei.se/konsument/el/elmarknaden/elomraden`, **reached and read 2026-09-10**;
the division took effect 1 November 2011 under Svenska kraftnät, following the Commission's
competition case - Ei report **R2012:06** is the fuller account, named). Nord Pool's *Bidding areas*
page names the same four as day-ahead areas **[PROVISIONAL: 403 from this machine]**. ACER's
bidding-zone review page (reached earlier this session) carries the definition and Sweden inside
the Nordic review area - the verified backstop if Nord Pool stays unreachable.

The zones need three sourced things each: hourly load (3.1), installed capacity by technology per
zone (Svenska kraftnät / ENTSO-E 14.1.A per bidding zone **[PROVISIONAL]**), and the **transfer
capacities** between SE1–SE2, SE2–SE3, SE3–SE4 (ENTSO-E 11.1.A *Forecasted Transfer Capacities* or
Svk's published NTC values **[PROVISIONAL]**). Without the third the four zones clear as one and the
whole point - SE4's price above SE1's - is absent; **stage 2 does not land Sweden's four without it.**

**Evidence on the record, not a build item (2026-09-10, `COMPLETED.md` §452).** Design has drawn an
*Energy map — interactive prototype* (`Energy Map Prototype.dc.html` at the root of the Design project,
read whole): Sweden's outline with Gotland and Öland, the trunks in one ink at three weights
(400 ⁄ 275 ⁄ 220 kV), the four elområden as bands (*"AREAS REAL · BOUNDARIES DRAWN AS PARALLELS,
APPROXIMATE"*), and the three snitt as dashed Caution cuts labelled SNITT 1 · SNITT 2 · SNITT 4; five
overlays (GRID · BOTTLENECKS · SUPPLY · DEMAND · WEATHER); every hover tagged illustrative
(*"TRANSMISSION NOT IN THE GAME · ILLUSTRATIVE"*, *"NO LOAD BY REGION IN THE GAME · ILLUSTRATIVE"*,
*"SITE PUBLIC · CAPACITY AGGREGATED, ILLUSTRATIVE"*). The board's own foot: *"The geometry never moves
between overlays — an overlay changes ink and lines only (12e's rule). Gutters stay lit under every
overlay; the active one is full ink. A price prints in Caution only where its cut is full. Sites and
cities are public; capacities, loads, flows and weather are illustrative until the columns each overlay
names exist. Per the spec-let, this map is deferred until D12 row 1's map lands — the prototype exists
to see the toggle, not to pre-empt the ruling."* **Its figures are illustrations** - the cut capacities
it draws (3 300 ⁄ 7 300 ⁄ 5 400) are not data here; the sourced figures are in
`ENERGY_LAYER_SPINE.md` §4. Two consequences, ruled: **(i) S15 is reinforced** - the three snitt ARE the
SE1–SE4 constraints, so the transfer capacities decide whether Sweden has zones at all, and the map is
unreadable without them; **(ii) the instrument-first ruling stands** - one country of six with a map is
a Sweden view, not the subsystem's home page, and the other five would have nothing to draw. **The
board's invariant is kept for whenever it is built: geometry never moves between overlays, and a price
prints in Caution only where its cut is full.**

**The other five run as one zone each, and the screen says so.** Germany-Luxembourg is one bidding
zone in fact; France one; Poland one. **Italy has seven zones and the USA three interconnections
with RTO markets inside them** - both are named future items in `ENERGY_SYSTEM_ADAPTED.md` §4 and
are *not approximated*: Italy clears as one zone with a printed note that its real market does not,
the USA likewise. Interconnection with neighbours outside the six (Norway, Denmark, Finland, the
Baltics, the Alps, Iberia, Canada) is **a net import series per country** from the balance
(Eurostat `nrg_cb_e` imports/exports; EIA for the USA), exogenous, not cleared.

## 4. The sourcing bill - every series named

| # | quantity | source and exact series | state |
|---|---|---|---|
| 1 | electricity mix 2023, six countries | Ember Yearly Electricity Data via OWID grapher (in-project, `ENVIRONMENT_FAMILY_SPINE.md` §8, cross-checked §342) | **VERIFIED in-project** - the calibration target of stage 2 |
| 2 | gross electricity production by fuel, 2023 | Eurostat `nrg_bal_peh` *Production of electricity and derived heat by type of fuel* (updated 2026-06-02 per the API) | **API answers** (`…/statistics/1.0/data/nrg_bal_peh?geo=SE&time=2023`); fetched for the mix cross-check, to be re-fetched whole per country |
| 3 | complete energy balance, 2023 | Eurostat `nrg_bal_c` | API answers; named in the spine (`:43`) |
| 4 | electricity supply, transformation, consumption, imports/exports | Eurostat `nrg_cb_e` | API answers; the block-energy gate and the net-import series |
| 5 | installed capacity by main fuel group and operator | Eurostat `nrg_inf_epc`; renewables detail `nrg_inf_epcrw` | API answers |
| 6 | electricity production indicators | Eurostat `nrg_ind_peh` | API answers |
| 7 | retail electricity price **components** (energy and supply, network, taxes, levies, VAT), households and non-households, annual | Eurostat `nrg_pc_204_c` and `nrg_pc_205_c` (the *_c* component tables; the bi-annual `nrg_pc_204`/`205` carry the bands and the `X_TAX`/`X_VAT`/`I_TAX` dimensions) | **API answers** - this is the free substitute for the IEA product (row 14) |
| 8 | hourly load per bidding zone, 2023 | ENTSO-E Transparency **6.1.A Actual Total Load** | [PROVISIONAL] 403; free account for the export |
| 9 | installed capacity per production type per bidding zone | ENTSO-E **14.1.A Installed Capacity per Production Type** | [PROVISIONAL] |
| 10 | transfer capacities SE1–SE4 | ENTSO-E **11.1.A Forecasted Transfer Capacities** (day-ahead NTC) or Svenska kraftnät's published capacities | [PROVISIONAL] |
| 11 | de-rating / availability by technology | ENTSO-E ERAA (European Resource Adequacy Assessment) de-rating factors, latest edition | [PROVISIONAL]; else `[AUTHORED-DRAFT]` per line |
| 12 | USA: capacity, generation, fuel, hourly demand | EIA **Form EIA-860** (generators, capacity by technology), **EIA-923** (generation and fuel consumption by plant, aggregated), **EIA-930** (hourly demand and net generation by source, US48; demand from 1 July 2015, generation by source from 1 July 2018); Electric Power Monthly **Table 1.1** already in-project (`table_1_01.xlsx`, 21 278 bytes, §342) | Table 1.1 VERIFIED; the three forms [PROVISIONAL] - eia.gov's data pages did not render to the fetcher today; bulk files are public |
| 13 | technology CAPEX, O&M, capacity factors | IRENA **Renewable Power Generation Costs in 2024** (the 2024-vintage edition published 2025) - LCOE, total installed cost, capacity factor, O&M by technology; for thermal and nuclear the IEA/NEA *Projected Costs of Generating Electricity 2020* or Lazard LCOE+ (named, not preferred) | [PROVISIONAL] 403 from irena.org; the edition's exact title confirmed at fetch |
| 14 | fuel prices and retail price composition | IEA **End-use Energy Prices** database | **PAID (€1,745 for twelve months) - verified today; cannot stand as a source.** Substitutes: row 7 for composition; fuel prices from World Bank *Pink Sheet* (monthly commodity prices - coal Australian, natural gas Europe TTF, crude Brent) and Eurostat `nrg_pc_203` (gas prices, non-household) - both free, [PROVISIONAL] |
| 15 | the ETS allowance price | EEX EUA auction results / the Commission's *ETS Report* series; or the ICAP allowance price explorer | [PROVISIONAL]; one annual average per year suffices for an annual clearing |
| 16 | electricity's weight in the price index | Eurostat `prc_hicp_inw` (HICP item weights, COICOP 04.5.1 electricity); BLS CPI relative importance (electricity) | [PROVISIONAL]; stage 4 only |
| 17 | electrification trend of transport | Eurostat `road_eqs_carpda` (passenger cars by motor energy) for the stock share; EIA AEO for the USA | [PROVISIONAL]; else the term is zero and stated |
| 18 | emission factors by fuel | IPCC 2006 Guidelines default factors (Vol. 2, Table 2.2) - the factors EDGAR itself applies | public; cited by table |

**The outcome of the sourcing, 2026-09-10 (`ENERGY_LAYER_SPINE.md`, §455), row by row:** 1 verified
again (the seeds equal Ember 2023 to the decimal) · 2–7 fetched by API for five, on disk with digests · 8
**substituted** - the hourly load came from energy-charts.info's republication of ENTSO-E (five
countries) and from eSett's open data (SE1–SE4, the settlement authority); ENTSO-E stays the source of
record and the account is optional · 9 fetched three ways (Eurostat, Ember, energy-charts), the thermal
divergence recorded; per-zone capacity BILLED with an apportionment fallback · 10 **verified from
Svenska kraftnät's own documents** (the capacity-map text and the 2024-09-30 report's "maximal
tilldelad NTC 2021–2023") · 11 BILLED (the ERAA page reached, the factors inside its PDFs) · 12 fetched
(EIA-930 both halves, EIA-860 2023, EPM 6.02 a/b, EPA 4.2.A) · 13 **BILLED** (irena.org 403 on page and
PDF) · 14 struck as paid; its substitutes fetched (Pink Sheet 2023, `nrg_pc_203`) · 15 **verified** (ICAP's
Allowance Price Explorer API: €85.51 secondary, €83.60 auctions, 2023) · 16 Eurostat fetched, BLS billed ·
17 Eurostat fetched, EIA AEO billed · 18 the IPCC chapter fetched and its Table 2.2 read.

**The gate on every row is §342's:** two independent sources within a point, or the row prints
BILLED with the series named and the reason. Nothing from the concept document appears in this
table because nothing in it is a source of record; its bibliography is where the names above came
from, and that is its whole standing.

## 5. Every existing ruling the design touches

| ruling | how it is touched | what this document proposes |
|---|---|---|
| **The carbon tax's base move (§349, landed)** | the base is the taxed CO₂ - the layer changes *how* the field moves, not what the base is | the base stays; the power half of the field becomes derived (2.1). `ENERGY_SYSTEM_ADAPTED.md` §3.4 calls the move "already sheeted" - **it is landed**, and the stale sentences (§1) are the same error on the screen |
| **P5-B3** revenue follows its base | unchanged | the revenue at seed unchanged to the cent |
| **P5-B2** spending lines persist and index | support schemes are lines | new lines appended to `SpendingCategory`, indexed by `IndexSpendingLines` with a driver in `SpendingDrivers` |
| **P5-B6** current prices | every energy price series nominal in its vintage | carried by `PriceLevel`; the retail decomposition printed nominal |
| **The Deregulation/Nationalization dial** (`Sector.cs:79`; ownership, not stringency; higher = output up, employment down) | becomes the layer's *state ownership* instrument for Energy | its existing effect on `Sector[Energy]` stays until stage 4 derives that row; then the dial's effect is the ownership term in the two ledgers and the descriptive effect is retired for Energy alone |
| **The sector dials' cost (P4-B3)** | Energy's dials would now cost through the ledgers | reconciled at stage 4 so the Sectors screen's cost line and the ledger agree - one figure |
| **The event catalogue, `EventBands`, FT-9 (§404) and the §407 tag** | energy events become state-triggered and duration-bearing | stage 8; the §407 tag struck by the first event carrying a duration; the three typed events replaced, not doubled |
| **The AI finance ministry (§387–§388)** | new lines are levers | the ministry's rules read them; nothing else changes |
| **The Riksbank page / monetary regime laws** | the pattern for stage 5's law category ("laws reach the energy parameters the way the ten monetary laws reach the Taylor constants") | a fifth law category - which overlaps the open "laws for the dials-only set" row (§378); **both are Elias's** |
| **Numeric inertness (§402)** | every energy constant a tagged line | as everywhere |
| **The single book** | the whole of §2 | the gates in §2 are the acceptance test of stage 2 |
| **Board 4a (§438) / no geographic map** | zones drawn as a small multiple, not on a map | as `ENERGY_SYSTEM_ADAPTED.md` §3.8; D12 row 1 unchanged |

## 6. The design, to be struck or approved line by line

- **S1. Zones as data; Sweden four, the rest one.** `Zone { Id, Country, hourly-load-derived blocks,
  fleet, transfer links }`. A country with one zone is the same code path as one with four.
- **S2. Fleet by technology, not by plant**, the seven labels of `MixLabels` split where the sources
  split them (gas into CCGT/OCGT if `nrg_inf_epc` does; hydro into reservoir/run-of-river if it does;
  otherwise not). Hydro carries a reservoir state (Sweden, Italy). No plant exists anywhere.
- **S3. Three blocks and a peak, the P90 cut `[AUTHORED-DRAFT]`.** ⚠ The one authored constant in
  the physical layer. Alternative: a fixed 500 h peak block (the capacity-planning convention) -
  equally authored. **Strike or bless.**
- **S4. ⚠ The environment family's power half becomes DERIVED at stage 2, with the EDGAR residual
  stated** (2.1). This is the single-book gate and it is BASELINE for the environment feedback family
  (§349) - the same fields move by a different mechanism. The alternative - the layer as readouts
  beside the family - is two books and is **not offered**.
- **S5. The ETS price is a sourced exogenous series distinct from `TaxType.CarbonTax`.** Without it
  the EU merit order is wrong at the first clearing. The player's carbon tax adds to it. **EN-4d (ruled 2026-09-11, built 2026-09-12, §467) read the statutes the other way: ETS-covered plant is exempt of the national carbon tax so the two prices do not stack - the fleet's carbon cost is the ETS alone, the tax reaches transport; applied at sector level for want of an installation register, the deviation stated.**
- **S6. Merit order over variable cost, one clearing per block per zone, transfer-constrained,
  with a scarcity term and negative prices permitted** (`ENERGY_SYSTEM_ADAPTED.md` §3.4). Stage 3.
  Congestion rent booked to the grid owner in the incidence ledger.
- **S7. The two ledgers land as one structure**: every policy writes a system-cost entry and an
  incidence vector (households, industry, taxpayers, generators, state). The retail price is the
  wholesale + network + policy + tax stack, each element traceable - Eurostat's `_c` components are
  the seed and the check. Stage 4.
- **S8. ⚠ The retail price reaches `Inflation` through a sourced HICP weight - its own family,
  BASELINE on six.** Stage 4, after S7, never with it (two families never land in one pass).
- **S9. The Energy sector's five dials ARE the four instruments** (2.2's mapping); no sixth control
  is added to the densest sub-screen in the game. Research Grants stays descriptive and says so.
- **S10. Support schemes are appended `SpendingCategory` members** - as many as the sources show
  the six countries actually run (renewables support, network subsidy, capacity payments, household
  price relief), each with a `SpendingDrivers` driver. Alternative: sub-lines under `Energy` - a
  new shape for the budget screens, larger. **Recommend appended members.**
- **S11. The energy ministry on the AI finance ministry's pattern**, stage 7, after the instruments
  exist - a mandate with weights, a budget, decisions explained through the attribution idiom;
  minister attributes inherited. Its failure modes measured, not authored.
- **S12. Events last (stage 8), state-triggered, duration-bearing**, replacing the three typed
  events and striking the §407 tag. Not before S8 exists, or an energy event has no way to reach
  inflation except by the typed constant it was meant to replace.
- **S13. No energy quantity is daily.** The clearing is annual at the turn boundary; a shock within
  the year enters the daily GDP path over its duration by FT-9 and nothing else. Stated on the screen.
- **S14. ~~The screen after stages 2–5, on Design's board, once the redirect is recorded~~ AMENDED 2026-09-12 (DS-4, `COMPLETED.md` §474): the page ships STRUCTURALLY FIRST in the v3 grammar, the board second, read the D11 way against the built page** (2.3). Elias: *"Structural ship first in the v3 grammar, the board second, read the D11 way against a built page. The D19 way worked: real parts make a better ask than a description does. I ruled S14 and I am amending it."* Its home: the Sectors page now, the rail cell asked in D20 and taken only if the page proves it needs one (DS-4b). As it stood, a diagnostic dump (`EnergyLayerDump`, the `BUDGET_PREMISE.md` pattern) was the only presentation until the redirect was recorded - readable, generated, never a figure typed; the dump stays the record's presentation beside the page (EN-6).
- **S15. Do not start stage 2 before the Sweden transfer capacities are in hand** (3.2). Four zones
  without the links between them is one zone drawn four times.

## 7. Sizing - the eight stages in sessions

| stage | what lands | sessions | depends on |
|---|---|---|---|
| 1 | this document, ruled | **done** (one) | the ruling |
| 2 | the physical layer: sourcing six countries (rows 2–6, 8–12, 18) then the fleet, the blocks, Sweden's four zones, the derived mix and power CO₂ with the EDGAR residual; `EnergyLayerDump`; its BASELINE against §404's dump; the §2.1 gates | **3** (two sourcing - the Eurostat rows are one session by API, ENTSO-E/EIA/Svk one with accounts - one build) | S1–S5, S15; the ENTSO-E account (Elias's, an errand) |
| 3 | the market layer: variable costs (rows 13–15), merit order, transfer constraints, scarcity, negative prices, congestion rent | **2–3** | stage 2 |
| 4 | the fiscal layer: the two ledgers, the retail stack seeded and checked against row 7, support lines appended and indexed, the Energy sector row derived, `EnergySpendingChange` retired; **then, its own pass**, the HICP pass-through (S8) | **2 + 1** | stage 3; S7 then S8 |
| 5 | policy and laws: the five dials mapped onto the instruments, the ONE law category reaching the energy parameters - **its own item, cited (§378 ruled 2026-09-12, §474: "the wider dials-only set follows P4-C3's one-category-per-session pattern and does not block stage 5")** | **2** | stage 4 (landed); EN-7 |
| 6 | the screen - **structural first (S14 as amended, DS-4), then Design's board read the D11 way against the built page** | **1–2** | stage 4 (landed); EN-6, then D20's answer |
| 7 | the ministry | **2** | stage 5 |
| 8 | events, state-triggered, duration-bearing; the §407 tag struck | **1** | stage 4's S8 |

**Stage 2 after 2026-09-10:** the sourcing done in one session rather than two (§455 - the
Eurostat, Ember, EIA, eSett, energy-charts, ICAP, Pink Sheet and Svenska kraftnät rows all answered
this machine; only IRENA and the ERAA factors did not), and **the build done the same night (§457)**
against the spine's five gates - stage 2 took one session where three were sized. Two things carried
into it by ruling: the thermal-capacity disagreement is resolved by naming a definition (the office of
record's), never by averaging three; the EDGAR residual is per country by method (main-activity
electricity derived; main-activity heat the known part; refineries and the factor gap the remainder,
printed with its sign; autoproducers in industry's book). **One deviation from S4, stated:** the power
figure's writer does not change hands at stage 2 - the derivation is asserted as an identity at the
seed and printed on the plate, and the family's elasticity keeps moving the figure until stage 3's
dispatch gives the carbon tax its mechanism; retiring the elasticity a stage early would have left
the tax with no effect on power.

**Stage 3 after 2026-09-11 (§458–§460):** done in one session where two to three were sized. Three
refinements of S6, each stated in the code and the record: (i) **a fleet is not one plant** - each fossil
category's dependable capacity is offered in 200 tranches spread ± 25 % `[AUTHORED-DRAFT]` around its
mean cost, because an annual merit order over one plant per category is winner-take-all and no
calibration can reach an interior share; (ii) **the trough decile's output is inflexible** - what a
category produced in the lowest-load decile runs in every block (heat-led CHP, minimum loads,
contracts) and the merit order runs above it, `[AUTHORED-DRAFT]` as a rule with the level the record's;
(iii) **where Eurostat's fuel grouping does not carry a category's own 2023 output** (France's and
Italy's coal plants sit in the multi-fuel groups - 19 and 36 MW under "coal" against 0.5 and 2.1 GW of
output) the dependable capacity is the output's own peak level, the definition kept and the shortfall
named. **The seed calibration** solves one adder per non-dominant fossil category (the dominant fuel's
adder is zero, so the price level is that fuel's own cost) until the seed dispatch reproduces 2023's
coal / gas / oil shares within a point - it reaches the thousandth for the five with a fleet. **Sweden's
price** is the water value proxied by Germany's and Poland's block prices weighted by SE4's capacity to
each (`CONVENTION`; Norway, Finland and Denmark are outside the six); its hydro runs at its 2023 block
levels and the reservoir dispatch is the open follow-up. **The S4 deviation is closed**: the writer
changed hands in this pass, the family's power elasticity retired (and the energy line's with it - a
field cannot have two writers), transport's couplings untouched. **The per-head figure is the 2023
system's per head** - the dispatch's CO₂ plus the residual over the SEED population, because the fleet
and the load are the 2023 system's until a stage grows them and a fixed total over a population that
halves would read as a doubling nobody built; the base, per head × population, follows the population
as the family's coupling always had it (the first baseline dump caught the alternative: Poland's figure
rose 28 % in a century of population decline with nothing burned).

**Stage 4 after 2026-09-11 (§461–§463):** the fiscal half done in one session where two were
sized; S8 (the HICP pass-through) is its own pass still, opened as EN-5. **S8 landed the same day as its own pass (§465):** the household price's change relative to the general price level times electricity's sourced index weight (Eurostat `prc_hicp_inw` CP0451; the BLS CPI-U relative importance for the USA) is a price-level term on the Phillips map that expectations look through - stage 4 complete, two passes, one day. What landed against row 4's
text, and where it deviates: (i) **the retail stack is decomposed per class** (households, non-households)
into wholesale + a FITTED supply margin + network + policy levies + environmental tax + VAT - the
wholesale from the dispatch, the rest Eurostat's 2023 components (`ENERGY_LAYER_SPINE.md` §6) carried by
the price level, VAT at the implied rate; it reproduces the components at the seed and is written to
the state each year and presented as written (the single-book rider). The margins are the layer's second
set of fitted parameters, two per country - Germany +0.117 / +0.047, France +0.057 / +0.132, Italy +0.156 / +0.077, Poland −0.031 / +0.018, Sweden −0.007 / −0.019, the USA +0.135 / +0.082 $ per kWh in the book's dollars, households / non-households - counted with the market's ten
adders by the check. (ii) **The two ledgers close to the unit** every year: system cost as far as the
model states it, incidence by payer and receiver. (iii) **Support schemes ride the book's existing energy
line**, not appended `SpendingCategory` members - no source on this machine splits a country's schemes
into lines with drivers - with the levy as the bill-financed half moving one for one against the line's
deviation from its indexed path (P5-B2 on the line, P5-B3 on the levy); the appended members wait for
their sources. (iv) **The congestion rent's rule** is Article 19(2)–(3) of Regulation 2019/943: credited
to next year's network component per kWh; armed by probe, since no snitt binds at the dated NTCs. (v)
**`EnergySpendingChange` is retired from BusinessConfidence** - the industrial bill's change as a share of
GDP replaces it at the proxy's own sensitivity - and stays on the approval formulas; the Energy sector's
row is NOT derived from the system cost (the sector table's seeds and the P4-B3 dials read it and no
source fixes the share) - a deviation stated. (vi) **The unit finding:** the carbon tax's points are the
country's currency per tonne and the market's costs are in euro; §460's Polish reference was read in the
wrong unit and stands corrected (coal 0.862 → 0.858, gas 0.111 → 0.115, the peak price 97.1 → 99.9 €/MWh for twenty zloty per tonne, €4.40); the book's own reading of a point - a share of
GDP on the emissions base - and the dispatch's were two readings of one line, opened ruling-first as EN-4c - **and ruled the same day (§464): the rate IS the country's currency per tonne of CO₂, the statutory figure at the seed (1 330 SEK, 30 EUR, 44.6 EUR; zero where no tax exists distinct from the ETS), revenue = rate × the taxed tonnes, the coverage bridge re-solved to hold the anchors. Which tonnes the statutes cover is EN-4d - ruled the same evening and built 2026-09-12 (§467): transport's, the ETS-covered fleet exempt of the tax at sector level with the deviation stated, the base and the bridge re-solved, the checks re-formed on an ETS probe; Sweden's statutory indexation of its rate billed as its own row (EN-4e) - **and ruled the same morning, carried (§471): one rule per country with its paragraph - Sweden indexed by the year's price ratio, Germany on the BEHG's schedule then carried by the level, France nominal - and the three real-rate readers with it.**

**The reservoir dispatch after 2026-09-11 (EN-3b, §466):** S2's storage is no longer abstracted - Sweden's zones clear at the exchange's own 2023 prices on the model's blocks with a measured coupling to the continent, the reservoirs are carried with Energiföretagen's capacities, and the shiftable hydro of Italy and Germany answers a policy's move of the block spread; the proxy's cost was measured at 87.3 against 43.9–70.0 €/MWh load-weighted. What stays open is named: the seed fill and the deficit slope have no source, and the market's own seed prices run low against the exchange (EN-3c).

**Total: fourteen to seventeen sessions**, none of which starts before this document is ruled, and
none of stages 3–8 before the stage before it has its own BASELINE explained per country. Each stage
is one commit family and one green simulation bar.

## 8. The recommendation - strikeable whole

Rule S1–S15 as written, with S3 (the P90 cut) and S10 (appended members) as the two places a
different answer costs nothing yet. Then **stage 2 only**, and inside it the sourcing first: the
Eurostat rows are reachable today by API, the ENTSO-E rows need an account that is Elias's to
register (`ERRANDS.md` E-34), and the Sweden links (S15) decide whether stage 2 lands four zones or
one. If the links cannot be sourced, Sweden runs as one zone with its four named - honestly absent -
and the four wait, exactly as Italy's seven and the USA's three do.

**Ruled as recommended, 2026-09-10 (§454).** The links were sourced the same night (S15's condition
met: Svenska kraftnät's own figures, `ENERGY_LAYER_SPINE.md` §4), so stage 2 lands Sweden's four zones,
not one.

**What this ruling does NOT foreclose:** the geographic map (D12 row 1), an R&D system (refused by
name with its trigger), Italy's and the USA's zone structures, cascading-failure modelling, the
1-in-N cold year - each named in `ENERGY_SYSTEM_ADAPTED.md` §4 with its disposition, none approximated.
