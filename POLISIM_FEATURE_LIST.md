# PoliSim — THE FEATURE LIST (2026-09-01)

**The audit era is closed by ruling.** Twenty-five checks stand as the bar and keep running; no new check
is written unless a defect has actually cost something **twice**. Everything below is work a player can
see or feel. Work down in order, continuously, one commit per item, R-SP1 push per item or coherent batch,
one report at the end of budget.

⚠ **This file is the governing objective.** `POLISIM_MASTER_LIST.md` is deleted and
`InstructionResidueCheck` is retired with it — the residue number was the audit era's goal and the era is
over. `POLISIM_BACKLOG.md` remains the detailed register of findings and rulings; **where they disagree,
the register wins on facts and this file wins on ORDER.**

## The era's one inheritance

- **The 25 checks in the bar** (`CheckSuite.RunAllBatch`) and the 9 simulation checks
  (`RunSimulationBatch`). They keep running and they keep failing when they should.
- **The standing rules**: R-T1 (a trigger carries its defence clause) · R-T2 (a shell probe is the weakest
  evidence and may not carry a finding alone) · R-T3 (an instrument fix is finished when every other
  consumer is enumerated) · S-36 (when a comment says a thing is dead, that sentence is the last place its
  name appears) · S-37 (every bound is a direction as well as a number) · S-38 (a prose claim about
  behaviour should name the instrument that checks it) · **measure the premise before fixing it** · **open
  the source rather than recalling it** · **evidence binds where the change lives** · **never tune to
  pass** · **never invent a figure** · **never lower a ratchet you have not cleared**.
- **`ERRANDS.md`**, and the outward-facing discipline around it.

## R-N5 — a new check is written only after a defect has cost something TWICE

⚠ **Recorded 2026-09-01, and it is a reversal of how the last week worked.** Instrument work is now a
**consequence of a real failure, never a prophylactic**. One instance is a fix; two is a class, and only a
class earns a check. A guard written for a defect that has not happened twice is a guess about the future
wearing the costume of rigour — and every one of them costs a suite slot, a session, and a reader's
attention for as long as the project lives.

## The two process findings the era leaves behind

- ⚠ **Committing on a red bar happened TWICE in one session.** Both were caught by the next run and fixed,
  and both were avoidable by reading the exit code before typing the commit. **The rule is one green bar
  per commit, no exceptions** — not "green when I last looked", green for the tree being committed.
- ⚠ **A safety rule over-applied looks careful while doing nothing, and nothing in the bar catches it.**
  E-4: a standing convention (R-SP1: sessions push fast-forward-only) was filed as an errand needing
  permission, on the reasoning that pushing to clear a red check would be tuning to pass. **The tripwire is
  not the thing being satisfied; it is the thing asking.** The note stays in `ERRANDS.md` for its shape,
  because the failure mode is invisible from inside: refusing to act reads as prudence in every log line it
  produces.

---

# F1 — ✅ COMPLETE 2026-09-01 (`COMPLETED.md` §§178–180)

⚠ **A player can start a Sweden game, reach election night from the game itself, and watch 29 of 29
constituencies declare from the model's own count.** Filmed at 1280×720 and 2560×1440 **from the model,
not from the staged fixture** — every prior election-night film was of a fixture, which exercises the
screen and says nothing about the model. `PlayerReachabilityCheck.UNREACHABLE_TAKEOVER` reads **0**.

*The original text is kept below, because the ORDER it prescribed is what made the result honest.*

# F1 (as written) — The runtime data layer, and everything it unblocks

**The largest correctness gap in the game: board 1h is built, filmed, delivered, and unreachable.** The
root is that `ElectionsData/` lives outside `Assets/`, so only editor code can read it — which makes
`RegionalVoteModel` unreachable, which means the live election produces no per-constituency count, which
means election night has nothing honest to draw.

Build it in dependency order, each step landing with its proof:

1. **The generated catalog moves into the runtime assembly WHEN A RUNTIME CONSUMER EXISTS** — not before.
   ⚠ A data layer landing ahead of its consumer is queued art in another costume.
2. **`RegionalVoteModel` wired into the live election path**, with the measurement that proves it now runs
   and produces the per-region result.
3. **The live election produces a real per-constituency count.**
4. **Board 1h reachable from a player path**, the count real, filmed at 1280/2560, `MetaTextCheck` and the
   edge checks silent.

⚠ **Wire nothing that makes a check green and a screen a lie** — no one-constituency "night", no 2022
counts standing in for a simulated election. If a step cannot be honest, **stop that step and report its
remainder in order.**

**Done when:** a player can start a Sweden game, reach election night from the game itself, and watch
constituencies declare from the model's own count.

*Carries the old rows: `M-D1` (step 1) · `M-B4` (step 2) · `M-B5` (step 3) · `M-S7` (step 4) · and the
ratchets `UnwiredSubsystemCheck.UNREACHABLE` and `PlayerReachabilityCheck.UNREACHABLE_TAKEOVER` fall out of
step 4 rather than being worked directly.*

# F2 — The cohort substrate, P-I2 stage 3 onward

The spec-let is ruled and stages 1–2 have landed. **This is the risky half and it is BASELINE** — it lands
one family at a time with the per-country explanation, or it does not land.

- **D-15 (c) stands:** scale the whole pyramid toward the sourced projection. Nothing anchors the level,
  and converging a rate would slow divergence **while looking like an improvement**.
- The four steps as sized: catalog the two projections → rebuild the step → hindcast against the
  publisher's intermediate years → trajectory suite, **ratchet lowered not removed**.
- ⚠ **The Eurostat trap rides with it:** the age dimension holds **110 categories mixing single years with
  aggregates**. A band build must **filter, never sum** — a naive sum reads **44.8M for a country of
  12.1M**.
- Then the collision map's dangerous half: retire the eight demographic scalars, **re-point both player
  levers in the same pass** (three dead levers would be a pattern, not an accident), and keep
  `NaturalBirthRate`/`NaturalNetMigrationRate`'s **anchor semantics** or every demographic policy effect
  loses its zero and starts compounding.

**Done when:** population, dependency ratio and participation are derived from cohorts, both demographic
levers measurably move the model, and the family is explained per country.

*Carries: `M-B2` (D-15 stage 3) · `M-B3` (the substrate wired) · `M-D2` (the two projections) · and
`CohortAgingStepDiagnostic.RUNAWAY` falls out of the rebuilt step.*

# F3 — The voter groups over the substrate, and the FdI chain

Voter groups become a **view** over F2's cohorts, **never a parallel population**: a predicate over cohorts
plus a non-demographic axis, `PopulationShare` **computed and never seeded**, shares summing to 1 and
covering the eligible population — **asserted**, the way the approval ledger asserts its identity.

Then C-D1's Swedish marginals (billed: SCB per-valkrets, ⚠ PxWeb is POST-only), then per-group loyalty,
then **re-run the Italy 2022 test**: with per-group loyalty and the media/momentum layers live, is FdI's
**4.35 → 29.27** surge reachable?

⚠ **Reachable is the strongest validation this model can get; unreachable is a named ceiling. Do not tune
toward it.**

*Carries: `M-B6` (D-10 (a), `TacticalVoting` wired) · `M-D4` (a live poll, so the tactical layer has polled
shares to read) · and E-1's ITANES registration is the blocker on per-group loyalty, which is Elias's.*

# F4 — The tax instruments

Downstream of F2's income dimension **by the spec-let's own §S3** — a bracket schedule over a single
average income is **arithmetically identical to a flat rate**. When the substrate carries income:
**(b) pluggable schedules**, per D-3, **not bracket tables** — Germany's tariff is a formula, France's a
quotient familial, Italy three layers, and **no table represents any of them**.

Sweden's blended 52 retires into kommunal + statlig, which is BASELINE and takes its own explained family.
The five billed countries' sourcing can proceed at any time and is **not wasted whichever branch runs**.

*Carries: `M-B7` (D-11 (c)) and, adjacent, `D-16`'s landing — see the appendix.*

# F5 — The five CHES axes (the afternoon that was a bill)

S-4's premise was false: all five undefined axes are published columns in `CHES_2024_final_v2.csv`, already
parsed for the four we took, **44 parties, every cell populated**. They are banked in
`ElectionsData/positions/party_positions.md`.

⚠ **Do not wire an axis whose endpoints are unread.** The codebook is behind subsetted-font ciphers, and
**S-37 says a bound is a direction as well as a number** — an inverted axis reverses every comparison and
goes on looking like it works. **Read the endpoints, or wire nothing.** D-18 stands.

*Carries: `M-S15`.*

# F6 — What the campaign still lacks to be played

In order of what a player would notice first:

1. **C-N1's ruling, then its build.** Media and momentum currently **terminate at the poll** —
   `MomentumTracker.Apply`'s only call sites are inside `PollingSystem.Conduct`, and election day counts
   true preference. Either perception-only media **is** the design and the record must say **no amount of
   coverage changes a vote**, or coverage reaches persuasion and it is a **§42 chain change with its own
   family**. Rule it in the register, then build the ruled branch.
2. **C-N2** — optimising personalities refuse the action they most prefer (door affinity 2.2 × enthusiasm
   1.6, still zero doors). ⚠ A **§12 verb-set question — fix the mechanism, adjust no affinity.**
3. **C-R4** — the war chest and the win/lose rule, so a campaign has **stakes and an ending**.
4. **The play-calibration list**, once the loop is playable — its constants are judged by playing, which is
   Elias's, and the list exists for exactly that sitting.

---

# APPENDIX — the short tail

⚠ **Nothing here outranks F1–F6.** One line each, with its owner.

## CODE

- **`D-16` — the sourced tax-base table for five countries**, `CollectionEfficiency` re-solved and
  re-documented as a coverage bridge, the >1 values named as coverage rather than efficiency. **BASELINE**;
  ruled and TAKEN, execution logged STOPPED with its five-step order in `POLISIM_BACKLOG.md` §D. Rides F4.

## Elias's, unchanged

- **`ERRANDS.md` E-1** — register at `itanes.it`, download the 2013 and 2018 waves. ⚠ **Blocks F3's
  per-group loyalty**, which is otherwise complete.
- **`ERRANDS.md` E-3** — the one paste: the GO on the seven marks, the derived mandate column, **the two
  stat-icon files** (files, not digests — ⚠ a manifest naming a file is not the file).
- **The §V sitting** — 54 rows, `sv_index.html`; ⚠ 43 carry no stated question, deliberately.
- **The felt verdicts** (§P) — decision density, the Trade bill's costs.
- **The 20 play-calibration entries** — judged by playing, not by reading.
- **C-C6's basis ruling · C-C11's recalibration lines · C-D2's pool · C-D3's språkrör** — each executes as
  written unless struck.

## Design's

- **The two stat icons**, once the convention lands. Plus the standing board list: D-7 board 2b · D-8.1 the
  seven Swedish marks then the batch of forty-five · D-8.2 party colours for five countries · D-8.3 the
  valkrets map (⚠ waits on E-3's mandate column) · D-8.4 election night's paper · D-8.5 the verdict stamp ·
  D-8.6 modal-or-stage · D-E4 the two Society icons. **All eleven D9 rows are answered; the batch waits on
  E-3.**

## The calendar's

- **K-1** — the seed refresh from Sweden's real result, **13 September**.

## Standing findings that own no item

`S-1` the unmoving electorate · `S-2` Germany's threshold cliff · `S-5` Sweden's top issue unrepresentable
· `S-6` Sweden 2014 does not reproduce (6 seats) · `S-25` uniform consumption and investment rates ·
`S-36` documentation of an absence · `S-37` a bound is a direction · `S-38` a claim should name its
instrument · `S-39` an outward-facing ask goes stale silently. ⚠ **Recorded, not startable** — each is a
fact the next relevant item must not contradict.
