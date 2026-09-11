# The energy system — concept adapted to PoliSim (2026-09-11)

**Provenance.** The source is an external concept document (ChatGPT, uploaded 2026-09-11), read in
full. It is **reference material, not a spec** — the same standing as the metric list at Playtest 5.
Its research is sound and its sources are real; what follows is the version that fits this game's
architecture. Every departure is stated with its reason. **Nothing here is built until the spec-let
is ruled** (the C-C12 / C-C13 / P5-C1 precedent).

**Installed at root 2026-09-10 (`COMPLETED.md` §451) as REFERENCE-AND-PLAN.** Its standing: the plan
of record for the energy track's eight stages, and a reference for the concept it adapts. ⚠ **The
source docx is external reference material, not a spec, and it is NOT on disk** - no `.docx`
matching it exists under the repo, `PoliSim-captures/`, `AssetPackArchive/` or the Design project's
uploads as of the install; it is recorded by description only (a ChatGPT concept document,
uploaded 2026-09-11 by its own dating - the day after the install date, which is the document's own
statement and is kept as written). Should it land, it goes under `PoliSim-captures/sources/` with
its digest, and this line changes to say so. **Stage 1 is `POLISIM_ENERGY_SPECLET.md`** - the
collision map, the sourcing bill with every series named, the sizing, and the strikeable design -
**RULED 2026-09-10 as recommended (`COMPLETED.md` §454)**; stage 2's sourcing landed the same night as
`ENERGY_LAYER_SPINE.md` (§455) and its build after it (§457: the physical layer as readouts,
`ENERGY_LAYER_PREMISE.md` its presentation); stage 3 (EN-3) built 2026-09-11 (§458–§460: the clearing,
Sweden's zones against the NTCs, the power figure written by the dispatch); stage 4, the fiscal layer,
is next (EN-4). Design's energy-map prototype is recorded against
the spec-let's §3 as evidence, not a build item (§452). **No figure here is a datum**; every number in this document is an
illustration and the spec-let says so of each.

---

## 1. What the concept assumes that this game does not have

Five collisions, each of which would have been discovered expensively:

| The concept assumes | This game holds | Consequence |
|---|---|---|
| A daily gameplay tick with hidden 15-minute or hourly slices | `DaysPerTurn = 365` — **one turn is one year**; the day loop exists but a *turn* is a year | 35,040 intraday slices × six countries per turn is not a cost this model can carry, and the player never sees an hour. **Refused.** |
| One playable country | Six countries, all simulated, five run by AI governments | Anything built is either country-general or honestly absent for five, and says which |
| A geographic map with plants, lines and animated flows | No geographic map exists — the world map is a six-node graph; board 4a's cartogram is *electoral* (area = k·mandat), not geography | **Map-first is refused for now.** D12 row 1 (a stylized six-country map) is an open Design row; the energy screen is instrument-first until a real map exists |
| A technology R&D tree | No research system of any kind | **Deferred by name**, with its trigger: the day a research mechanic exists |
| Plant-level and substation-level assets | No spatial substrate below the country, except Sweden's 29 electoral valkretsar | Fleet is modelled **by technology, not by plant**; zones only where they are real and sourced |

**And one thing this game holds that the concept does not know about:** the environment family
(P5-C5) already carries electricity mix by source, power CO₂ per head and transport CO₂ per head,
seeded from EDGAR and Ember and cross-checked. **The energy system must become the mechanism
underneath those readings, never a second set of numbers.** The single-book rider applies: no
stored quantity may diverge from its presented value.

## 2. What is kept, and why it is the good half

- **The layered architecture** — physical → market → network → fiscal → political. It maps almost
  one-to-one onto how this project already separates concerns, and it is why one event can
  propagate: low wind → gas dispatch → import bill → wholesale price → industrial cost → inflation
  → approval → parliament. This game already has the last four links.
- **The two ledgers: "system cost" and "who pays".** The single best idea in the document, and it
  is this project's own instinct — a subsidy does not lower a cost, it moves its incidence. It
  matches the effectiveness card's flow-and-level pair and the attribution ledger's discipline.
- **Merit-order clearing with an area price.** The one mechanic that makes cheap generation and
  cheap delivered electricity different things, which is the concept's own first principle.
- **Retail price = wholesale + network + policy + taxes.** This is where energy becomes political
  and it lands directly on the existing tax and budget architecture.
- **Installed ≠ dependable capacity**, and the reserve margin that follows from it.
- **State-triggered events rather than random cards** — the existing catalogue's own idiom.
- **Delegation with failure modes** — an energy ministry that can be slow, captured or
  single-KPI-obsessed. This game already has ministers with loyalty, knowledge, efficiency and
  popularity, and an AI finance ministry running sourced fiscal rules. Delegation is that pattern.
- **A drillable "why did the price move?" decomposition.** The attribution ledger's shape, applied
  to a new quantity.

## 3. The adapted design

### 3.1 Time and resolution — the central change

**No intraday.** The year resolves as a **load-duration approximation**: three blocks — base, mid
and peak — with hours-per-block sourced per country, plus a **winter-peak** case for the adequacy
test. This is the standard annual capacity-expansion form, it preserves scarcity, merit order and
reserve margin, and it costs the turn boundary a bounded amount of work. FT-9's rule applies to any
shock: it enters the daily path over its duration so the year nets to its print.

### 3.2 Zones — Sweden first, exactly as elections did

Sweden's four *elområden* (SE1–SE4) are real, sourced (Energimarknadsinspektionen; Nord Pool's
bidding-area documentation) and are the whole reason area prices are politics in Sweden. **Sweden
gets four zones; the other five run as one zone each** until their zone structures are sourced —
Italy's multiple zones and the USA's three interconnections are named as future items, not
approximated. The architecture takes zones as data; the seed decides how many.

### 3.3 The physical layer, per country

Fleet **by technology**, not by plant: installed capacity × availability × resource factor, seeded
from **Ember** (already verified in-project at P5-C5) and **Eurostat/EIA energy balances**.
Hydro carries a reservoir state because Sweden and Italy are hydro systems and a reservoir is the
one storage that must not be abstracted away. Demand from the substrate this game already has —
population and cohorts (F2), GDP and the sector shares on Statistics — plus a sourced electrification
trend and a weather draw on its own named stream.

### 3.4 The market layer

One annual clearing per block per zone: merit order over variable cost (fuel + carbon + variable
O&M), constrained by inter-zone transfer capacity, producing an **area price** per zone and a
congestion rent where a limit binds. A **scarcity term** when dependable capacity approaches
residual peak. Negative prices permitted rather than special-cased. Fuel and carbon prices are new
sourced series; the carbon price connects to the existing carbon tax, which currently reaches the
CO₂ intensities as a readout (P5-C5) and would now reach them through dispatch — **that is the
carbon tax's base move, already sheeted.**

### 3.5 The fiscal layer — the two ledgers

Every energy policy writes to both: **system cost** (what the electricity actually costs to
produce and deliver) and **incidence** (who pays — households, industry, taxpayers, generators,
the state). Retail price decomposes into wholesale, network, policy and tax, each traceable. This
lands on the existing budget lines and tax instruments; **a support scheme is a spending line and
its cost indexes like any other** (P5-B2), and revenue follows its base (P5-B3).

### 3.6 Policy, as a matrix and not one slider

The existing Deregulation/Nationalization dial stays but is **not** the whole control. Four
independent instruments, each a dial or a law family: market liberalisation, retail intervention,
investment planning, and state ownership — so "private generation + regulated retail" and
"state-owned grid + liberal wholesale" are both reachable. Neither pole is correct; each buys
something and costs something, measured. Laws reach the energy parameters the way the monetary
regime's ten laws reach the Taylor constants.

### 3.7 Delegation — the energy ministry

The AI finance ministry's pattern: a mandate with weights (affordability, reliability,
decarbonisation, independence, fiscal discipline, industrial competitiveness), a budget, and
decisions it takes within them, **explained in plain language** through the attribution idiom. It
inherits the minister attributes: efficiency scales delivery, knowledge narrows disclosure, loyalty
drives resignation, popularity feeds approval. Its failure modes are the mechanism, not sabotage.

### 3.8 What the player sees — instrument-first, not map-first

Until a geographic map exists, the Energy screen is the Riksbank page's shape at a larger scale:
the system's state as instruments, the price with its decomposition, the fleet as a distribution,
the zones as a small multiple, the forecast as the graph idiom at 1l's weights, and the ministry's
brief as the political half. **Every figure derived; absences drawn as absences.** When D12's
stylized map lands, the zones can move onto it.

## 4. Refused, deferred, and named

| Item | Disposition |
|---|---|
| Intraday / 15-minute simulation | **Refused** — one turn is one year |
| Plant-level fleet, substations, nodal flow | **Refused** — technology-level fleet; hidden nodes are not needed for the questions this game asks |
| Animated flow map | **Refused for now** — no geographic map exists; revisit when D12's lands |
| R&D technology tree | **Deferred by name**, trigger: a research mechanic exists |
| Three separate markets (day-ahead / intraday / balancing) | **Collapsed to one clearing plus a scarcity term** |
| Italy's zones, USA's interconnections and RTO structure | **Named future items** — sourced before built, never approximated |
| Blackout cascade modelling | **Staged**: scarcity → controlled shedding is in; cascading failure is deferred |
| Ownership as company-level markers | **Mapped** onto the existing nationalization dial and the sovereign wealth fund |

## 5. Staging

Each stage is its own BASELINE family where it touches the model, and **two families never land in
one pass.**

1. **The spec-let, ruled.** This document, sourced, sized, with the collision map against
   `EconomyState` and the environment family — **before any code.**
2. **The physical layer**: fleet, demand, availability, the three blocks, six countries, Sweden's
   four zones. Readouts only; the environment family's existing figures become derived from it,
   proving the single book.
3. **The market layer**: merit order, area prices, congestion, scarcity, negative prices.
4. **The fiscal layer**: the two ledgers, the retail decomposition, support schemes as spending
   lines.
5. **Policy and laws**: the four instruments, and a law category reaching the energy parameters.
6. **The screen**, on Design's board.
7. **The ministry**, on the AI finance ministry's pattern.
8. **Events**, state-triggered, through the existing catalogue.

## 6. The sourcing bill

Ember (in-project, verified) · Eurostat energy balances (`nrg_bal_*`, already fetched for the
electricity mix) · EIA for the USA · ENTSO-E for transfer capacities and load · Energimarknadsinspektionen
and Nord Pool for Sweden's zones · IRENA 2024 for technology CAPEX and O&M · IEA for fuel prices
and price-composition trends. **Every figure sourced with vintage and basis, or `[AUTHORED-DRAFT]`
with its line. No figure from the concept document is a datum** — it is a research summary, and its
numbers are illustrations.
