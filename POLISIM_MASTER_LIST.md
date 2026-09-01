# PoliSim — THE MASTER WORK LIST

**Derived 2026-09-01 by sweeping the repo, not by reading a list.** Every row below was found by
enumerating the sources named in the consolidation brief and the code's own markers, then checked against
`git log` and the code. ⚠ **Where a document and the code disagreed, the code won and the document is
corrected here** — three rows were found already done and are recorded closed rather than re-worked.

⚠ **This file does not replace `POLISIM_BACKLOG.md`.** The register stays the detailed home of every row;
this is the ordered view across all fourteen sources, built so the CODE column can be emptied in a known
order. **Where they disagree, the register's detail wins on facts and this file wins on ORDER.**

---

## ⚠ READ THIS BEFORE READING THE NUMBER

**A zero on `InstructionResidueCheck` means NO CODE ROW IS STARTABLE. It does NOT mean the work is
finished.** ⚠ **Two of the three dependency chains below terminate in an owner no session can be** —
`M-D3` needs Elias to register an ITANES account, and the `M-B4`/`M-B5` chain waits behind `M-D1`. The
**reachable** CODE column therefore empties well before the **project's** open work does.

Anyone reading a zero as completion has read a statement about *reachability* as a statement about
*scope*. That is this repo's signature defect — a claim whose evidence does not reach as far as the claim
— wearing the one number built to be trustworthy. The check prints the same sentence in its own output
every run, because **the number travels further than the file it came from.**

---
## THE COUNT AND THE SHAPE

| | |
|---|---|
| **open items, all owners** | **73** |
| **OWNER = CODE** | **31** |
| OWNER = ELIAS | 26 |
| OWNER = DESIGN | 8 |
| OWNER = CALENDAR | 1 |
| standing findings that own no item (recorded, not startable) | 7 |

| CLASS | count | of which CODE |
|---|---|---|
| SAFE | 21 | 17 |
| BASELINE | 6 | 6 |
| DATA | 9 | 3 |
| RULING | 16 | 0 |
| RECORDS | 21 | 5 |

### The longest dependency chain — five deep

**`M-D1` runtime data layer → `M-B4` `RegionalVoteModel` wired → `M-B5` the live election produces a
regional count → `M-S7` board 1h has a route → `M-R6` `PlayerReachabilityCheck`'s ratchet reaches 0.**

Two others run four deep:

- **`M-B2` P-I2 stage 3 → `M-B3` the cohort substrate wired → `M-D3` ITANES 2013/2018 → `M-B6` per-group
  loyalty → the FdI ceiling** (⚠ `M-D3` is OWNER **ELIAS** — a registration — so this chain **cannot be
  emptied by a session at all**, and that is the single most important fact on this page).
- **`M-B2` P-I2 stage 3 → `M-B3` wired → `M-B7` the tax instruments build** (F-7's real third condition:
  *a bracket schedule needs more than one income to apply itself to*).

### ⚠ Sessions to empty the CODE column — the estimate, and it is not a week

**Eleven to sixteen sessions**, and the honest shape of that number:

- **six BASELINE items, and they land one at a time by rule** — each needs a trajectory suite (historically
  over an hour of wall-clock) plus a per-country explanation. **That is six sessions minimum and they
  cannot be parallelised**, because two families in one pass cannot be explained apart.
- **seventeen SAFE items**, most small; realistically three to four sessions batched.
- **three CODE DATA items** whose sources have refused four fetch attempts between them; each is one
  session *if* the source answers, and otherwise it is a bill, not work.
- **five RECORDS items**, one session batched.

⚠ **More than a week, plainly.** And two of the three chains terminate in something no session can do:
`M-D3` needs Elias to register an account, and `M-B5` needs `M-B4` which needs `M-D1` — so the *reachable*
CODE column empties well before the *project's* open work does. **Anyone reading a zero on
`InstructionResidueCheck` should read it as "no CODE row is startable", never as "the work is finished."**

---

## ⚠ WHAT ARMING THE TERMINATION CONDITION FOUND — read this before trusting the 31

Building `InstructionResidueCheck` surfaced two defects in the instruments that produce these numbers, and
by this project's own rule a finding surfaced by an instrument fix is **fixed, not absorbed**. Both are
closed (`COMPLETED.md` §162), and both are recorded here because they change what a reader may infer:

- **Two ratchets were outside the audit entirely.** `RatchetSlackCheck`'s enrolment asked whether a
  `RatchetLedger.Report` call was *written*, not whether it *ran* — and `CohortAgingStepDiagnostic.RUNAWAY`
  and `PublicationCadenceCheck`'s floor report into the simulation batch's ledger, which nothing read.
  ⚠ **`M-R5`'s backlog was therefore unverified while the audit printed `0 unreported`.** The slack audit
  now runs at the end of both batches and `RatchetResidency` names the deferred ones.
- **The residue check's ratchet enrolment could not tell its subjects apart** — it asked whether *any* row
  id began `M-R`, so one row satisfied every ratchet at once. Each ratchet must now be **named** in a row.

⚠ **Neither number below moved**, which is the point worth stating: the residue was 31 before and after,
so the fixes bought no progress and were never going to. **What changed is whether the 31 is evidence.**

---
## SECTION 1 — SAFE, OWNER CODE. Startable now, in this order.

| ID | what | done-when | OWNER | CLASS | BLOCKS-ON | size |
|---|---|---|---|---|---|---|
| **M-S5** | **`C-0.3`** — the stranded branch disposed: migrate its four unsuperseded pieces, retire the obligation, keep the ref | ⚠ `stranded/politics-elections` still exists locally and on origin. Done when the four pieces are in `COMPLETED.md` and the branch is a recorded ref only | CODE | RECORDS | — | M |
| **M-S6** | **`C-0.2`** — the post-wiring re-derivation: no live document asserts a pre-wiring premise | `DocumentClaimCheck` covers the identifier half; this is the PROSE half, and ⚠ it is S-22's class — sized as a read, not a scan | CODE | RECORDS | — | L |
| **M-S7** | **`S-32`** — board 1h gets a route from the running game | `PlayerReachabilityCheck` reports 0 and the route shows a real count | CODE | SAFE | **M-B5** | M |
| **M-R1** | ratchet: `UnwiredSubsystemCheck.UNWIRED` = **7** | each of the seven is wired, deleted, or parked with a trigger; ceiling lowered | CODE | SAFE | — | L |
| **M-R2** | ratchet: `UnwiredSubsystemCheck.UNREACHABLE` = **6** | as above | CODE | SAFE | M-B4 | L |
| **M-R3** | ratchet: `DocumentClaimCheck.MEMBER_GONE` = **2** | both are struck-through history inside live documents; either re-home the history or accept and record | CODE | RECORDS | — | S |
| **M-R4** | ratchet: `PartyMarkCoverageCheck.UNCONSUMED` = **1** | ⚠ `mark_party_us_lib` has no seeded party and **must not get one invented**; closes when a Libertarian party exists for a reason of its own, or the file is retired | CODE | SAFE | — | S |
| **M-R5** | ratchet: `CohortAgingStepDiagnostic.RUNAWAY` = **2** | Italy and Poland stop hitting `MinPopulation` at the 100-year horizon | CODE | SAFE | **M-B2** | — |
| **M-R6** | ratchet: `PlayerReachabilityCheck.UNREACHABLE_TAKEOVER` = **1** | board 1h has a route | CODE | SAFE | **M-S7** | — |
| **M-S9** | **`S-22`** — nothing checks a PROSE claim about behaviour, in a comment or a document | ⚠ the honest done-when may be *"recorded as undecidable"*; it is the larger half of what a comment asserts | CODE | SAFE | — | M |
| **M-S11** | **`S-13`** — the Policy Web's ~40 incoming edges converge on one point at a focused stat node | DESIGN ruled it a comprehension judgement (board 2b); this row is the code half once ruled | CODE | SAFE | — | S |
| **M-S15** | **`S-4`** — five of §4's eight axes are UNDEFINED and not centred; `FlatIssueMatch = 0.5` stands in | the axes are defined from a source, or the stand-in is ruled permanent | CODE | DATA | — | M |

## SECTION 2 — BASELINE, OWNER CODE. One at a time, each with its per-country explanation.

| ID | what | done-when | OWNER | CLASS | BLOCKS-ON | size |
|---|---|---|---|---|---|---|
| **M-B1** | **D-16** — the sourced tax-base table for **five** countries, `CollectionEfficiency` re-solved and re-documented as the coverage bridge, the USA excluded on the perimeter rule | the five land back on their recalibrated targets; the per-country response differs; the trajectory diff is explained as float-path divergence | CODE | BASELINE | — | L |
| **M-B2** | **D-15 stage 3** — the cohort retirement, scaling the whole pyramid toward the sourced projection | hindcast against the publisher's intermediate years passes; the runaway ratchet is **lowered, not removed** | CODE | BASELINE | **M-D2** | XL |
| **M-B3** | the cohort substrate **wired** — `StepOneYear` gains a caller in game code | `UnwiredSubsystemCheck` stops naming the cohort files | CODE | BASELINE | **M-B2** | M |
| **M-B4** | **`RegionalVoteModel` wired** — Germany's per-Land vote replaces the national one | the measured +7.4 pp CSU over-prediction moves, explained per country | CODE | BASELINE | **M-D1** | L |
| **M-B5** | the live election produces a **per-constituency count** | `NationalElection` yields regional returns, not only national shares | CODE | BASELINE | **M-B4** | L |
| **M-B6** | **D-10 (a)** — `TacticalVoting` wired into the vote model | ⚠ it moves C-A1's recorded FdI figures, so it lands with its own before/after | CODE | BASELINE | **M-D4** | L |
| **M-B7** | **D-11 (c)** — give door-to-door a job (target a region's swing voters) | §34's "no single dominant approach" bar is met without re-pricing | CODE | BASELINE | **M-B5** | M |

## SECTION 3 — DATA, OWNER CODE. Startable only while the source answers.

| ID | what | done-when | OWNER | CLASS | BLOCKS-ON | size |
|---|---|---|---|---|---|---|
| **M-D1** | the **runtime data layer** — the generated catalog moved into `Assets/Scripts` with its first consumer | ⚠ **the mechanism is built and proven** (generator, header assertion, definitional reconciliation, digest check, both directions); only the move waits, and it waits on a consumer | CODE | DATA | — | M |
| **M-D2** | the two population projections catalogued | ⚠ the Eurostat query shape is **proven** and Sweden 2050 reads 12 130 240; **the `age` dimension mixes single years with aggregates — a band build must FILTER, never sum, or Sweden reads 44.8 M** | CODE | DATA | — | M |
| **M-D4** | a **live poll** at election time, so the tactical layer has polled shares to read | the campaign layer's polling is reachable from the game loop (C-R4b's layer) | CODE | DATA | **M-D1** | L |

## SECTION 4 — RULING. Sheets in `POLISIM_BACKLOG.md` §D.

⚠ **None of these blocks a session**: each carries a recommendation already taken as strikeable, except
where Elias has ruled. **D-1, D-2, D-3, D-4, D-5** ruled and executed · **D-6, D-7, D-8, D-10, D-11, D-12,
D-15** self-ruled and strikeable · **D-9, D-13, D-14, D-16** ruled by Elias. **Sixteen sheets, zero
blocking.**

---

# ⚠ OWNER ≠ CODE — NOT STARTABLE BY A SESSION

**Nothing in this section may be picked up as work.** It is here so that a zero on the CODE column is not
mistaken for an empty project.

## OWNER = ELIAS — 26

- **`ERRANDS.md` E-1** — register at `itanes.it` and download the 2013 and 2018 waves. ⚠ **Unblocks the
  whole per-group-loyalty chain**, which is otherwise complete.
- **`ERRANDS.md` E-3** — the one paste: the GO on the seven marks, the derived mandate column, the two
  stat-icon files. **Unblocks Design's row 6 and row 9.**
- **The §V sitting** — 54 rows, `sv_index.html`. ⚠ **43 of them carry no stated question**, deliberately
  left empty.
- **The felt verdicts** (§P) — decision density, the Trade bill's costs.
- **The 20 play-calibration entries** — judged by playing, not by reading.
- **C-C6's basis ruling · C-C11's recalibration lines · C-D2's pool · C-D3's språkrör** — each executes as
  written unless struck.

## OWNER = DESIGN — 8

D-7 board 2b · D-8.1 the seven Swedish marks then the batch of forty-five · D-8.2 party colours for five
countries · D-8.3 the valkrets map (⚠ **waiting on E-3's mandate column**) · D-8.4 election night's paper
· D-8.5 the verdict stamp · D-8.6 modal-or-stage · D-E4 the two Society icons.
⚠ **All eleven D9 rows are ANSWERED**; what remains is the batch, and the batch waits on E-3.

## OWNER = CALENDAR — 1

**K-1** — the seed refresh from Sweden's real result, 13 September.

## Standing findings that own no item — 7

`S-1` the unmoving electorate · `S-2` Germany's threshold cliff · `S-5` Sweden's top issue unrepresentable
· `S-6` Sweden 2014 does not reproduce (6 seats) · `S-25` uniform consumption and investment rates ·
`S-36` documentation of an absence · `S-37` a bound is a direction. ⚠ **Recorded, not startable** — each
is a fact the next relevant item must not contradict.

---

## THE RESIDUE

`InstructionResidueCheck` counts what is left, mechanically, and the run ends when it reports zero.
**Opening number and its composition are in the check's own output** — this file does not restate it,
because a hand-copied count is the thing the check exists to replace.

---

# ⚠ CLOSED — NOT STARTABLE BECAUSE DONE

⚠ **A second boundary, and it is deliberately NOT the same heading as `OWNER ≠ CODE`.** Filing a closed
CODE row under that one would make it assert something false about every row beneath it, and a heading
that lies is how this repo has lost five things to a comment (S-36).

⚠ **Every row here must PRODUCE ITS COMMIT, and `InstructionResidueCheck` fails if one does not.**
Without that, this section is simply where a row goes to stop being counted, and the residue would
measure willingness to move rows rather than work done.

| ID | what | done-when | OWNER | CLASS | closed at | size |
|---|---|---|---|---|---|---|
| **M-S1** | ⚠ **`C-0.4` was DONE and the register said open.** `CheckSuite.RunAllBatch` exists and every bar run uses it | the register's row cites the commit and reads closed — **it does** | CODE | RECORDS | `12a4833` (the correction); the work itself at `9489d97` | XS |
| **M-S2** | R-T3's owed enumeration: what "the width assertion" was, and every consumer of it | ⚠ **the phrase DID resolve uniquely — trap 2 — and the enumeration is where the unguarded half was hiding: HEIGHT, unchecked for a month.** `GameViewChromeHeight` named and dated; both directions proved on real film runs | CODE | RECORDS | `2777f18` | S |
| **M-S4** | **`S-26`** — the dial midpoint `50` stated in four places | ⚠ **it was FIVE.** One statement now; the other four reference it. Closed by `SharedMidpointCheck`, not by the cleanup — each of the four already carried a comment saying the others existed | CODE | SAFE | `ab279d7` | S |
| **M-S16** | ⚠ **`G-1`'s guard was armed for a human who remembers** — `ScreenEdgeCheck` in neither batch, firing only if invoked after a capture pass | the capture driver runs it over its own label before exiting, and the hook can only make the exit code worse; proved with 81/0 exiting **2** when the guard verified nothing | CODE | SAFE | `e8a9bb1` | M |
| **M-S14** | **`S-3`** — W-B12's residual: SD keeps 6 of 38 unpaid staff-days | ⚠ **stale in both halves** — it is SD 6, V 12, MP 12, and the two parties that hired FEWEST have the WORST record. Arithmetic closes to the krona; assertion **1j** now separates poverty from a bug | CODE | SAFE | `d31dec3` | S |
| **M-S3** | **`S-29`** — the party-ink **draw-site** check | ⚠ **the surface is ONE file** — `HemicycleRenderer` — so clause 1 is an allow-list with arguments, clause 2 takes the FILE as the unit of adjacency and says so, and clause 3 is subsumed by construction. Four failure paths proved | CODE | SAFE | `a77c243` | M |
| **M-S10** | **`S-17`** — the capture command's two silent defaults (film geometry is load-bearing) | ⚠ **both guarded, and the finding reproduced itself in the closing**: 1280×800 films 8 text overflows where 1280×720 films 0, on identical code. The four geometries also gave `GameViewChromeHeight` four corroborations | CODE | SAFE | `7ae67e0` | S |
| **M-S8** | **`S-23`** — `DeadStateCheck` still cannot distinguish a read from a write | ⚠ **it did not need "more than a regex" — it needed a regex AND a classifier.** Built; its first run found **six dead fields**, all deleted, ceiling untouched, plus one false positive that would have had somebody delete a live loop bound | CODE | SAFE | `8daa676` | M |

---

# ⚠ STANDING WATCH — NEVER STARTABLE, NEVER DONE

⚠ **A third boundary, and the easiest of the three to abuse.** A row whose content is *"re-verified each
cycle"* has no completed state, so counting it makes zero **unreachable by construction** — the
termination condition would read false for a reason that has nothing to do with the work. But "it is a
standing watch" is also exactly what someone would say about a row they did not want to do.

⚠ **So it is policed harder than the other two: every row here must NAME a check that exists AND is
registered in a batch.** A watch nobody runs is not a watch; it is a row in a quieter place.

| ID | what | the check that performs it | OWNER | CLASS | verified | size |
|---|---|---|---|---|---|---|
| **M-S12** | **`G-2`** meta text — a standing guard, re-verified each cycle | `MetaTextCheck`, registered in the cheap suite | CODE | RECORDS | every bar run | — |
| **M-S13** | the stripper enrolment and the ledger enrolment | `CommentImmunityCheck` and `RatchetSlackCheck` — both read 0 unenrolled / 0 unreported | CODE | SAFE | every bar run | — |
| **M-R7** | ratchet: `DeadStateCheck.WRITE_ONLY` = **0** — ⚠ a MEASURED zero, not an aspirational one: its first run found six, all **deleted** rather than absorbed, and the ceiling was never touched | `DeadStateCheck`, in the cheap suite | CODE | SAFE | every bar run | — |
