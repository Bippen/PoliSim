# Elections architecture — E-0 (overnight 2026-08-28→29; spec-blind halves only)

**Status — COMPLETED 2026-08-29 against the spec**, which arrived on Day-1's second attempt and
passed Phase 0's content check (44 sections, §42 the causal chain, §44 the last) and is installed
verbatim at root. The stubs this document carried while the spec was missing are now either built
or classified in `ELECTIONS_GAP_TABLE.md`. This document deliberately did not read the stranded branch
(R-EL5 cites the surviving verification DOC, not the branch), which was the right discipline while the
branch was an open obligation.

⚠ **Re-derived 2026-08-31 (C-0.2).** This section said the stranded branch (`stranded/politics-elections`,
`ca6c510`) *"remains UNINSPECTED per D0 — its work is proposals to verify at item 10"*. It was inspected
once and **DISPOSED at C-0.3**: the ref is kept, the obligation retired, its C# superseded by
`Assets/Scripts/Elections/` and W-G1, and the four pieces of its roadmap doc that were NOT superseded
migrated to `COMPLETED.md` as history.

## R-EL1 — the PoliSim idiom mapping (at the queue's own level)

- **Catalogs, not ScriptableObjects:** static C# tables in code (the `FederalReserveSystem.
  CandidatePool` / `WorldFactory` seed idiom) for anything the game ships; the night's sourced
  data stages in `ElectionsData/` (out of tree for Unity, in tree for git) until item 10 wires
  real catalogs.
- **Deterministic seeded streams:** every random draw through `SimulationRandom.For(stream)` —
  never `new System.Random()`, never `UnityEngine.Random` (the A0 lesson recorded in
  `SimulationRandom`'s own doc).
- **Pure functions + editor harnesses:** the mechanics chain as static, side-effect-free
  functions over plain data, each with a batch `-executeMethod` harness in `Assets/Editor/`
  (the `*Check` idiom, `CheckExit.Finish` exit codes), runnable with `-nographics`.
- **InvariantCulture at every format/parse site** (B3; `UiFormat`'s pinning is the precedent).

## Election state vs config

- **Config (immutable per campaign):** electoral rules (system, tiers, thresholds, allocation
  method, district structure and magnitudes), party list, positions, the calendar date. Sourced
  or seed-derived; never mutated by play.
- **State (evolves):** preferences/loyalty, momentum, polling history, the returns as counted.
  Everything state lives in plain serialisable types so a save can carry it when item 10 wires
  saves — NOT tonight (R-N2: no save hook).
- STUB (spec): the exact state objects, §7's types, §8's preference damping, §§20–22 polling
  objects — blocked-on-the-spec, billed.

## The seed-stream policy

Election noise gets its OWN named stream appended to `SimulationRandom.Stream` (append-only
enum, per its own doc: never reorder). Tonight the enum is NOT touched (R-N2: not one byte of
the existing model moves); the name is reserved here instead: `ElectionNoise` as the next
member, taken at item 10's wiring pass. Until then the pure functions take a `System.Random`
(or a seed int) as an explicit parameter — deterministic under the harness, wireable to the
stream later without changing a formula.

## Where the system will EVENTUALLY wire (documented, not done — R-N2)

1. `SimulationManager`'s turn boundary: the campaign clock (replaces `ElectionSystem.
   IsElectionTurn`'s fixed cycle for countries under the new regime).
2. `GameController`'s politics screens + Canvas screen 1h (election night) — item 10's UI, none
   tonight.
3. `SaveGame` — new state objects join the save the way `FedChairCandidates` did.
4. The D0 collision map EXECUTED at item 10 (W-G1, `a289e1e`, 2026-08-30) - it is history, not a plan: the four archetypes were retired, `TotalSeats
   = 200` yields to real chamber sizes, `ElectionSystem`'s approval threshold yields to the
   vote model; `PublicationSystem` stays as the polling substrate; seat drift, bill scoring and
   the renderers stay.

⚠ **Point 4 EXECUTED on 2026-08-30 (W-G1, `a289e1e`), and one clause of it did not survive contact
(re-derived 2026-08-31, C-0.2).** `PartyArchetype` retired for 53 real parties; `TotalSeats = 200` yielded
to six real chamber sizes; `PublicationSystem` stayed; the renderers stayed; bill scoring stayed but was
**re-expressed** over the measured chamber rather than left alone. **Seat drift did NOT stay — it retired
too**, because a parliament's composition does not drift week by week with the government's approval, and
the per-archetype sensitivity figures it drifted on are published for no real party by anyone.
**`ElectionSystem`'s approval threshold did NOT yield** — it was left exactly as it was, because the game
assigned the player no party identity for the vote model to award a fate to; that question was ruled
2026-08-30 as R-CL1 and the replacement is register row C-R4 in `POLISIM_BACKLOG.md`.

## What existed on main before the wiring (R-EL6, measured — the inventory the gap table built on)

*(filled by the overnight inventory — see the morning report's Part 2 section for the measured
line numbers; summary:)* `ElectionSystem` (fixed cycle + approval threshold + transient
result), the four archetypes, the hardcoded 200-seat chamber, seat drift, the hemicycle/
compass/map renderers, `PublicationSystem` (the polling substrate), five `mark_party_*` sprites
drawn by nothing, `FedChair` election-eve pause. ⚠ **This is a 2026-08-29 inventory kept as the gap
table's EXISTS column, not a description of HEAD** — four of its entries no longer exist and the sprites
are drawn now. What is on main today is `COMPLETED.md` §§79–84.

## The spec's own architecture (§40) vs this one — a ruled divergence, stated plainly

Spec §40 asks for **ScriptableObjects** and a thirteen-manager **MonoBehaviour** tree
(`ElectionManager` over `CampaignManager`, `PollingManager`, `VoterSimulation`, …). Two standing
rulings cut across it, and both win:

- **R-EL1 — the PoliSim idiom wins: catalogs in code, not ScriptableObjects.** The game's own
  precedent (`FederalReserveSystem.CandidatePool`, `WorldFactory`, `LawCatalog`) is static tables
  a diff can review and a batch harness can read without a scene. `ElectionTypes.cs` follows §41's
  field lists closely — `PartyData` → `PartyProfile`, `VoterGroupData` → `VoterGroupProfile`,
  `RegionData` → `RegionProfile`, `CandidateData` → `CandidateProfile` — as plain value types.
- **R-N2 — nothing is wired.** A MonoBehaviour tree IS wiring. Every unit is instead a pure static
  class in `PoliSim.Elections`, callable from an editor harness and from nothing else.

**What is kept from §40 is its actual point: modularity.** One concern per file, no god-object —
`Compatibility`, `PreferenceModel`, `TurnoutModel`, `RegionalAggregation`, `SeatAllocation`,
`Rosatellum`, `ElectoralCollege`. When wiring is ruled, those managers become thin drivers over
these functions rather than containers of the logic. The gap table records §40 as N/A-by-ruling.

**§42 is binding on everything above these layers.** No campaign action may ever be a flat vote
delta; it must travel the chain (reach → salience → exposure → relevance → credibility →
persuasion/enthusiasm → preference → turnout → regional vote → electoral system → seats). The
units built so far occupy the second half of that chain, which is why the first half (§12's
actions) cannot be shortcut later — the join is already the right shape.

## Built as of 2026-08-29 (all pure, all unwired)

`ElectionTypes.cs` (§41/§4/§5/§6 shapes) · `Compatibility.cs` (§7, five weighted terms) ·
`PreferenceModel.cs` (§8 loyalty damping) · `TurnoutModel.cs` (§26's five-factor product) ·
`RegionalAggregation.cs` (§27, with noise on its named stream) · `SeatAllocation.cs` /
`Rosatellum.cs` / `ElectoralCollege.cs` (§28 — five real chambers reproduced exactly).

## Still NEW, in the order the measurements say to build them

§39's remaining terms — base support, candidate appeal (§16), campaign effects (§12), media
(§13/§14), momentum (§22), tactical voting (§23) — then §24's regional objects, §20/§21's polling,
and the campaign layer proper (§9–§12, §15, §17, §32–§33). The gap table carries the full
classification and the reasons; the Day-1 report carries the sizing.
