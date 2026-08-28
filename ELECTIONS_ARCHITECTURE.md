# Elections architecture — E-0 (overnight 2026-08-28→29; spec-blind halves only)

**Status:** drafted WITHOUT `ELECTIONS_CAMPAIGN_SPEC.md` (the queue arrived without it — the
morning report's first call line). Every section the spec alone can settle is a marked stub;
nothing here guesses spec content. The stranded branch (`stranded/politics-elections`,
`ca6c510`) remains UNINSPECTED per D0 — its work is proposals to verify at item 10, and this
document deliberately does not read it (R-EL5 cites the surviving verification DOC, not the
branch).

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
4. The D0 collision map executes at item 10, not before: `PartyArchetype` retires, `TotalSeats
   = 200` yields to real chamber sizes, `ElectionSystem`'s approval threshold yields to the
   vote model; `PublicationSystem` stays as the polling substrate; seat drift, bill scoring and
   the renderers stay.

## What exists on main today (R-EL6, measured — the inventory the gap table builds on)

*(filled by the overnight inventory — see the morning report's Part 2 section for the measured
line numbers; summary:)* `ElectionSystem` (fixed cycle + approval threshold + transient
result), `PartyArchetype` (four archetypes), `TotalSeats = 200`, seat drift, the hemicycle/
compass/map renderers, `PublicationSystem` (the polling substrate), five `mark_party_*` sprites
drawn by nothing, `FedChair` election-eve pause. The 44-section gap table is blocked-on-the-
spec; this inventory is its EXISTS column, ready.

## STUBS awaiting the spec (billed, not guessed)

§7 types + compatibility core · §8 loyalty-damped preference · §26 turnout model · §27 regional
aggregation + noise placement · §§20–22 polling/momentum · the 44-section gap table (EXISTS /
EXTENDS / NEW / N/A per R-EL7) · anything the spec's illustration numbers would have tempted
(none ship as data regardless — R-N4).
