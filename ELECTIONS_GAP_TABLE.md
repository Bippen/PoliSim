# The elections gap table — all 44 spec sections against what PoliSim holds (2026-08-29)

**Method (R-EL6, existing-systems-first):** every section of `ELECTIONS_CAMPAIGN_SPEC.md` is
classified **EXISTS** (the game already does this — where), **EXTENDS** (a real system is here and
grows to meet it — what), **NEW** (nothing corresponds), or **N/A** (R-EL7: inapplicable content
is dropped with a one-line reason, not parked and not built). Verified against the spec installed
at root the same day: 44 sections, §42 the causal chain, §44 the last.

**R-EL7 is live in this table.** Nine sections are N/A. Six of those are design principles or
illustrations rather than buildable units — dropping them is not a refusal to build, it is a
refusal to pretend a philosophy is a work item. The other three are genuine conflicts with
standing rulings, each named.

| § | Section | Class | Where / What / Why |
|---|---|---|---|
| 1 | Core Design Goal | **N/A** | A goal statement, not a unit. Its content becomes the acceptance test for §39 and §42. |
| 2 | Election Structure | **EXTENDS** | `ElectionSystem` holds a fixed cycle and `IsElectionTurn`; the spec's fields (dates, campaign duration, seats, system, districts, parties, candidates, voter groups, turnout) are the shape it grows into. Electoral-system rules already EXIST and are proven — see §28. Election *types*: national parliamentary is the built case; presidential is `ElectoralCollege`; referendum and party-leadership are NEW. |
| 3 | Campaign Calendar | **NEW** | No campaign phase exists. The game's day/turn loop (`AdvanceDay`/`AdvanceTurn`) is the clock it would hang from. |
| 4 | Political Parties | **EXTENDS** | `PartyArchetype` (four archetypes) + `Country.ParliamentSeats` exist and retire under D0. Multi-dimensional ideology is **half-built already**: CHES 2024 lrecon/galtan/eu_position are sourced for 34 real parties (`ElectionsData/positions/`), i.e. the data exists and the party object does not. |
| 5 | Voter Groups | **NEW** | No voter groups exist. ⚠ Per the overnight rule, groups are **DERIVED-first** from the demographic seeds the model already holds, and only what cannot be derived is sourced — no authored demographics, ever. |
| 6 | Voter Issue Priorities | **EXTENDS** | Salience is **sourced** (EB105 Spring 2026 per country, Gallup for the USA) and already drives the Phase-4 instrument's axis weights. Per-group issue weights are NEW. |
| 7 | Party-Voter Compatibility | **NEW → BUILT 2026-08-29** | `Elections/Compatibility.cs` — the five-term sum (policy match, ideological match, reputation, leader appeal, campaign effectiveness) normalised 0–100, with a synthetic-vector harness. |
| 8 | Voter Loyalty | **NEW → BUILT 2026-08-29** | `Elections/PreferenceModel.cs` — loyalty damping of compatibility toward a group's prior attachment. **This was the layer both Phase-4 deviation signatures implicated**, which is why it is the first chain unit built. |
| 9 | Campaign Resources | **NEW** | Money/time/staff/volunteers. Distinct from the government budget the game simulates — a campaign purse is not a fiscal line. |
| 10 | Campaign Offices | **NEW** | Regional organisation; depends on §24 existing first. |
| 11 | Campaign Strategy | **NEW** | The five strategies are modifiers over §12's action set. |
| 12 | Campaign Actions | **NEW** | The player-facing verbs. Bound by §42: an action may never be a flat vote delta. |
| 13 | Media System | **EXTENDS** | `PublicationSystem` is a real publication/lag/revision layer (preliminary → revised figures) — the substrate for coverage, but coverage itself is NEW. |
| 14 | Media Bias / Audience | **NEW** | Outlet-audience segmentation. |
| 15 | Debates | **NEW** | The decision-modal idiom exists (`DrawCabinetDecisionModal`, the Fed-chair picker) and is the UI pattern it would reuse — when wiring is allowed. |
| 16 | Candidate Attributes | **EXTENDS** | `CabinetMinister` (philosophy + `CompetenceBias`) and `FedChair` (philosophy) are the established attribute idiom, and 25 portraits are delivered. The spec's eleven-attribute candidate is a wider version of a thing that exists. |
| 17 | Scandals | **EXTENDS** | `EventSystem` already fires authored events with real effects through a seeded stream; a scandal is an event kind with a response set. |
| 18 | Political Events | **EXISTS** | `EventSystem` with its authored pool, effects and seeded draw. What it does NOT do is move **issue salience** — that hook is the EXTENDS half. |
| 19 | Government Performance | **EXISTS** | This is the entire macro simulation (GDP, unemployment, inflation, real wages, crime, debt, services) plus the approval ledger. **The spec's actual-vs-perceived split already exists too**, as `PublicationSystem`'s preliminary/revised figures — the game's most under-used asset for this system. |
| 20 | Polling System | **EXTENDS** | Same substrate as §19's perception layer; the poll object (sample, MoE, field date, breakdowns) is NEW. |
| 21 | Internal Polling | **NEW** | The paid-information economy. |
| 22 | Polling Momentum | **NEW** | Moving average + decay. |
| 23 | Tactical Voting | **NEW** | Requires §20 (what voters believe) and §28 (what the system rewards) to exist first. |
| 24 | Regional Politics | **EXTENDS** | ⚠ The game's "regions" are countries; sub-national regions do not exist as model objects. But the **data does**: 29 valkretsar, 41 okręgi with magnitudes, 16 Länder, 13 régions, 8+ regioni, 51 US jurisdictions — all sourced with results. Building §24 is modelling, not research. |
| 25 | Swing Regions | **NEW** | Pure derivation from §24 + §20; the spec's "do not hand the player the answer" rule makes it an intelligence product, not a readout. |
| 26 | Get-Out-The-Vote | **NEW → BUILT 2026-08-29** | `Elections/TurnoutModel.cs` — the spec's five-factor product (base × engagement × mobilisation × enthusiasm × salience), clamped, with the GOTV actions left as the NEW half. |
| 27 | Election-Day Simulation | **NEW → BUILT 2026-08-29** | `Elections/RegionalAggregation.cs` — per region, per group: population × eligible × turnout × preference, aggregated, plus `Final Vote = Expected + Election Noise` on its own named seeded stream. |
| 28 | Vote-to-Seat Conversion | **EXISTS — DONE AND PROVEN** | `SeatAllocation` (d'Hondt, Sainte-Laguë, modified Sainte-Laguë, per-district sum), `Rosatellum` (floored Hare ×2), `ElectoralCollege` (WTA + ME/NE district method). **Five of six real chambers reproduce EXACTLY from official counts.** The spec's PR / FPTP / mixed-member triad is covered; France's two-round SMD is the one uncovered family. |
| 29 | Coalition Formation | **NEW** | Sourced coalition structures exist for Italy and Poland; the negotiation model does not. |
| 30 | Election Results Screen | **NEW** | UI — blocked by R-N2 until wiring is ruled. Board 1h (election night) is already the Design-side placeholder. |
| 31 | Post-Election Analysis | **EXTENDS** | ⚠ **The game already has exactly this idiom**: the approval attribution ledger decomposes a number into named contributions the boundary audit proves. §31's "why you won" table is that ledger pointed at a vote share. |
| 32 | Campaign AI | **NEW** | Personalities over §12's action set. |
| 33 | AI Decision-Making | **NEW** | Expected-value scoring; needs §12 and §20. |
| 34 | Campaign Mistakes | **N/A** | A property the action set must have (recoverable, non-dominant), not a separate unit. Becomes an acceptance test for §12. |
| 35 | Diminishing Returns | **N/A** | A curve shape every spend path must obey, not a unit. Enforced inside §9/§12; noted as a rule so it cannot be forgotten. |
| 36 | Hidden Variables | **N/A** | An information-architecture principle governing §20/§21/§25 — implemented as *what the UI is not allowed to show*, which is a rule about other sections. |
| 37 | Campaign Staff Progression | **NEW** | Between-election progression. |
| 38 | Long-Term Political Capital | **NEW** | Cross-election persistence; the save layer would carry it. |
| 39 | Core Simulation Formula | **EXTENDS — PARTIALLY BUILT 2026-08-29** | Of the thirteen layers: compatibility ✔, ideological match ✔, loyalty/preference ✔, turnout ✔, regional ✔, noise ✔ (built today); government performance EXISTS (§19) and needs only a read; base support, candidate appeal, campaign effects, media, momentum and tactical voting remain NEW. |
| 40 | Unity Architecture | **N/A as written — CONFLICT WITH A STANDING RULING** | The spec asks for ScriptableObjects and a 13-manager MonoBehaviour tree. **R-EL1 rules the PoliSim idiom wins: catalogs in code, not ScriptableObjects**, and **R-N2 forbids wiring anything into the live game**. The spec's *modularity* is honoured — every unit built today is a separate pure static class in `PoliSim.Elections` — but its Unity-object prescriptions are dropped by ruling, not by preference. Revisit only if Elias strikes R-EL1. |
| 41 | Recommended Data Model | **EXTENDS** | The field lists are adopted as the SHAPE of the types (`ElectionTypes.cs` follows PartyData/VoterGroupData/RegionData/CandidateData closely) — as plain C# structs and static tables rather than ScriptableObjects, per R-EL1 as above. |
| 42 | Important Design Principle | **N/A as a unit — BINDING AS A RULE** | The causal chain (action → reach → salience → exposure → relevance → credibility → persuasion → preference → turnout → regional vote → electoral system → seats) is the acceptance criterion every §12 action must satisfy. Nothing to build; everything to obey. |
| 43 | Example Campaign Scenario | **N/A** | An illustration. ⚠ R-N4 standing: **not one of its numbers ships as data** (+4.1 %, 31.0 → 31.8 %, the party shares) — they are narrative, not seeds. |
| 44 | Design Philosophy | **N/A** | Philosophy. Its loop is the shape the finished system should have, and its question ("where can I actually gain votes?") is what §25 and §31 exist to make answerable. |

## Tally

**EXISTS 3** (§18, §19, §28 — one of them, the seat rung, already proven exact against five real
chambers) · **EXTENDS 10** (§2, §4, §6, §13, §16, §17, §20, §24, §31, §39, §41 — counting §39 and
§41 as extends) · **NEW 22** · **N/A 9** (§1, §34, §35, §36, §40, §42, §43, §44 — eight principle
or illustration sections — plus §40's ruling conflict counted among them).

**Built today, from NEW to done: four** — §7 compatibility, §8 loyalty, §26 turnout, §27
election-day aggregation with noise. Together they are the spine of §39.

**Day-2 update (2026-08-29).** §24's structural half is now built too (`RegionalVoteModel.cs`:
per-region electorate weights and per-region party availability, sourced for Germany) — the row
moves from EXTENDS-with-data-only to **EXTENDS, partially discharged**; what remains of §24 is
per-region voter-group composition and per-region priors. §8 and §27 were **measured against
reality** rather than merely built: both correct the deviations they were named for, but a uniform
loyalty constant regresses Italy, so R-EL13's wiring gate FAILED and nothing was wired. The next
unit the measurements name is **deriving loyalty per country (or per group, as §5/§8 specify)
rather than assuming one constant** — see `ELECTIONS_DAY2_REPORT_2026-08-29.md`.

## D0 reconciled — what this system replaces

`MISSING_PREREQUISITES.md` §D0 (item 10, "REALISTIC POLITICS AND ELECTIONS", gated on Sweden's
13 September 2026 vote) is the *same work* this spec describes, scoped before the spec existed.
The reconciliation is exact: **this spec IS item 10's political model**, and D0's collision map is
its migration plan. What retires when this system lands: `PartyArchetype`'s four archetypes (real
parties replace them — §4), the hardcoded `TotalSeats = 200` (real chamber sizes — §2/§28), and
`ElectionSystem`'s approval-threshold win condition (the vote model replaces it — §27/§39). What
survives untouched: seat drift, bill scoring, every renderer, and `PublicationSystem`, which is
promoted rather than replaced — it becomes §19's perception layer and §20's polling substrate.
What still gates: the 13 September re-seeding, and — separately and more importantly — **wiring,
which R-N2 forbids until Elias rules on it**. Nothing in this table changes that; every unit built
today is reachable from no gameplay path.
