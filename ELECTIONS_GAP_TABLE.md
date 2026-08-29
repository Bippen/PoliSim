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
| 10 | Campaign Offices | **NEW â BUILT 2026-08-30 (W-B4)** | Regional organisation; depends on §24 existing first. **W-B4: `CampaignOffices.cs` â Â§10's five attributes and five provisions; organisation as local reach (a visit draws a quarter of a full office's), volunteers recruited to capacity, a daily operation into W-B11's ground game, maintenance paid or the office starves; concentration against spread measured (the crossover at 4 M kr). The AI campaign runs a staged office plan per personality; siting (W-B5, W-C2), staff (W-B5) and the screens (W-E1, W-E5) remain.** |
| 11 | Campaign Strategy | **NEW → BUILT W-B6 (2026-08-29)** | The five strategies are modifiers over §12's action set. `CampaignStrategy.cs`: the five as modifiers over the whole chain, each bullet a shape (loyal/swing, focus/other, opponent share); no dominant strategy in a 30-electorate sweep. |
| 12 | Campaign Actions | **NEW** | The player-facing verbs. Bound by §42: an action may never be a flat vote delta. |
| 13 | Media System | **EXTENDS → BUILT W-B9 (2026-08-29)** | `PublicationSystem` is a real publication/lag/revision layer (preliminary → revised figures) — the substrate for coverage, but coverage itself is NEW. `MediaSystem.cs`: coverage as a decaying, saturating stock (cannot spiral), media INTEREST as availability (bookings by the outlets, a ledger fair over time), coverage → momentum bounded, own channels reach a party's following and the press in proportion to interest. |
| 14 | Media Bias / Audience | **NEW → BUILT W-B9 (2026-08-29)** | Outlet-audience segmentation. Outlets with reach ceilings and audience compositions over voter groups; the same message resolved per group through the outlet (climate ×1.9 through the young-urban outlet, crime ×2.0 through the older-rural one); the roster is archetypes, real reach billed. |
| 15 | Debates | **NEW** | The decision-modal idiom exists (`DrawCabinetDecisionModal`, the Fed-chair picker) and is the UI pattern it would reuse — when wiring is allowed. **W-B7 (2026-08-29): `Debates.cs` — exchanges as skill × prepared × ownership × clash + one seeded draw on the appended `Debate` stream; the result a coverage and momentum shock with no share member; the AI campaign holds two.** |
| 16 | Candidate Attributes | **EXTENDS** | `CabinetMinister` (philosophy + `CompetenceBias`) and `FedChair` (philosophy) are the established attribute idiom, and 25 portraits are delivered. The spec's eleven-attribute candidate is a wider version of a thing that exists. **W-B7 (2026-08-29): `CandidateProfile`'s attributes drive the debate's move blends; unnamed `[AUTHORED-DRAFT]` candidates per personality (W-F6 labels real leaders).** |
| 17 | Scandals | **EXTENDS** | `EventSystem` already fires authored events with real effects through a seeded stream; a scandal is an event kind with a response set. |
| 18 | Political Events | **EXISTS** | `EventSystem` with its authored pool, effects and seeded draw. What it does NOT do is move **issue salience** — that hook is the EXTENDS half. |
| 19 | Government Performance | **EXISTS** | This is the entire macro simulation (GDP, unemployment, inflation, real wages, crime, debt, services) plus the approval ledger. **The spec's actual-vs-perceived split already exists too**, as `PublicationSystem`'s preliminary/revised figures — the game's most under-used asset for this system. |
| 20 | Polling System | **EXTENDS** | Same substrate as §19's perception layer; the poll object (sample, MoE, field date, breakdowns) is NEW. |
| 21 | Internal Polling | **NEW** | The paid-information economy. |
| 22 | Polling Momentum | **NEW** | Moving average + decay. |
| 23 | Tactical Voting | **NEW â BUILT (threshold form) 2026-08-29 (W-A4)** | Requires §20 (what voters believe) and §28 (what the system rewards) to exist first. **W-A4: `TacticalVoting.cs` â the belief from the PUBLISHED poll widened by a belief-sigma, the bloc lending where the race is in play and a party's own voters abandoning it where hopeless; no threshold â the identity. SCB's May PSU sourced (`sweden/psu_2018_2022.md`); 2022 reproduced better than the poll alone. Other forms (FPTP districts, runoffs) and the wiring between the final tracker and the count (W-G1) remain.** |
| 24 | Regional Politics | **EXTENDS** | ⚠ The game's "regions" are countries; sub-national regions do not exist as model objects. But the **data does**: 29 valkretsar, 41 okręgi with magnitudes, 16 Länder, 13 régions, 8+ regioni, 51 US jurisdictions — all sourced with results. Building §24 is modelling, not research. **W-E2 (2026-08-29): the 29 valkretsar drawn as a cartogram of support AS POLLED (`SwingRegions.cs`, `GameController.CampaignMap.cs`); per-valkrets demographics and priorities remain W-F4's.** |
| 25 | Swing Regions | **NEW** | Pure derivation from §24 + §20; the spec's "do not hand the player the answer" rule makes it an intelligence product, not a readout. **W-E2 (2026-08-29): the index `100 × max(0, 1 − gap/20)` DERIVED from a poll of the valkrets, too-close-to-call when the lead is inside its own ±, and §36's gate as ABSENCE — an unbought valkrets carries no reading.** |
| 26 | Get-Out-The-Vote | **NEW → BUILT 2026-08-29** | `Elections/TurnoutModel.cs` — the spec's five-factor product (base × engagement × mobilisation × enthusiasm × salience), clamped, with the GOTV actions left as the NEW half. **W-B11 (2026-08-29): `GotvModel.cs` — the mobilization INPUT as volunteer-bound contacts per region and per party on §35's curve; worked valkretsar rise, the other 26 stay at exactly base, the nation within 2002–2022's range.** |
| 27 | Election-Day Simulation | **NEW → BUILT 2026-08-29** | `Elections/RegionalAggregation.cs` — per region, per group: population × eligible × turnout × preference, aggregated, plus `Final Vote = Expected + Election Noise` on its own named seeded stream. **W-D1 (2026-08-29): `ElectionDay.cs` — every valkrets counted from W-B11's `RegionVotes`, noise on `ElectionNoise` at the declared 1.2 pp, national σ 0.259 pp against 0.260 predicted by 1/√N_eff on the real 29; seed-exact.** |
| 28 | Vote-to-Seat Conversion | **EXISTS — DONE AND PROVEN** | `SeatAllocation` (d'Hondt, Sainte-Laguë, modified Sainte-Laguë, per-district sum), `Rosatellum` (floored Hare ×2), `ElectoralCollege` (WTA + ME/NE district method). **Five of six real chambers reproduce EXACTLY from official counts.** The spec's PR / FPTP / mixed-member triad is covered; France's two-round SMD is the one uncovered family. **W-D2 (2026-08-29): `SeatConversion.cs` — vallagen 14 kap. on the live path (fixed seats per valkrets by the 310th-part rule, totalfördelning, återföring, adjustment); 2022 seat-for-seat from an `ElectionDay.Result`; the 12 % and return branches made to fire.** |
| 29 | Coalition Formation | **NEW** | Sourced coalition structures exist for Italy and Poland; the negotiation model does not. |
| 30 | Election Results Screen | **NEW** | UI — blocked by R-N2 until wiring is ruled. Board 1h (election night) is already the Design-side placeholder. |
| 31 | Post-Election Analysis | **EXTENDS** | ⚠ **The game already has exactly this idiom**: the approval attribution ledger decomposes a number into named contributions the boundary audit proves. §31's "why you won" table is that ledger pointed at a vote share. |
| 32 | Campaign AI | **NEW → BUILT W-C1 (2026-08-29)** | Personalities over §12's action set. `CampaignAi.cs`: the five as parameters over §33's terms; chaotic and populist measurably distinct, the rational three collapse onto the free interview — PENDING W-B9 / W-B4-B11, recorded not forced. |
| 33 | AI Decision-Making | **NEW → BUILT W-C1 (2026-08-29)** | Expected-value scoring; needs §12 and §20. Built in ONE unit (compatibility points): §42's band on MEASURED inputs × affinity × probability of success − money at the action's own §35 efficiency, per hour, × risk on the band's relative width; no kronor-to-votes exchange rate authored; the attack verb of the spec's example left to W-B8. |
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
