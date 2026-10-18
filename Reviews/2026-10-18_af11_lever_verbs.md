# AF-11 (q11) - adversarial review, two passes (2026-10-18)

An independent reader wrote this review: a subagent, read-only and refute-first. The integrator committed it as returned.

**The item.** Elias's ruling q11 of 2026-10-18: "every lever the UI writes gets a manager verb, and the impact ledger reads it" (audit row GA-0-55).

**The branch.** `wave/af11`, base 40f33c75. Pass 1 reviewed 56cfd190; pass 2 reviewed 66204ecf.

**Mode.** Read-only. Unity was not run. The compile gate printed GATE OK at both heads.

## Pass 1 (56cfd190): method

- Read the whole diff.
- Checked each of the five verbs against the UI code it replaced: the stored expression, the clamp constants, the order of operations, the turn and date it captures, and the gate overload.
- Traced the game's turn order (`GameController.Update` → `AdvanceTurn`) against `ShadowBaseline.AdvanceTurn`.
- Read `RestoreInto` and `SetWorld`, to see what a fork with an inactive host skips.
- Grepped every caller of `ShadowBaseline`, `PolicyImpactLedger` and `DaysIntoTurn`.
- Read the `CheckSuite` tables and the check's regex teeth.

## Pass 1: findings, and how each was met at 66204ecf

| # | Sev | Finding | Met |
|---|---|---|---|
| H1 | H | `UiLeverVerbCheck` and `PlayerLeverLedgerDiagnostic` are in no `CheckSuite` table, so neither runs, and `RunNamedBatch` refuses them by name. | The integrator registers them at integration. OPEN until a bar log shows both ran green. |
| M1 | M | In play, a dial family's boundary fork has `DaysIntoTurn` = 365. Before AF-11 it ran a whole extra turn; AF-11's realignment fixes that, but nothing tested it in the game's order. Two stale comments. | **Met.** A Taxes dial from turn 2 is forked by `ledger.AdvanceTurn` after the 365 days and before `sim.AdvanceTurn`. It is asserted EXACT against a from-seed run without it. The comments are rewritten. |
| M2 | M | `SetFinanceStanceTarget` is a lever the ledger did not read; the event answers have the same shape. | **Met for the stance:** `PlayerLeverKind.FinanceStance`, in its own family "Fiscal stance", through `Pull`. The event answers are declared out of scope (see L-B). |
| L1 | L | A fleet order `Place` turns down after the fork leaves a family world with no lever. | **Met.** A zero order and a retirement with nothing left are refused before `Pull`, by the same expressions `Place` uses. |
| L2 | L | One minister or head object seated in several worlds (GA-1-5). | **Met.** Replays seat `Copy()`; the real game seats the candidate itself. |
| L3 | L | The load fork's `Awake` reset the market's turn state after `BeginTurn`. | **Met.** The load fork is built with `insideTurn: true`. |
| L4 | L | A fork inside a turn names `lever.CountryId` as the player. | **Met.** It uses `realSim.PlayerCountryId ?? lever.CountryId`. |
| L5 | L | The tariff limits are stated twice. | **Met.** The manager's public constants are read by the UI (still const 0f/50f, so no IL or value change). |
| L6 | L | False negatives in the regexes. | **Met.** One assignment form covers `??=`, `%=`, the compound operators and `=` (not `==` or `=>`); index patterns one level deep; object initialisers; nested and indexed State writes. 26 planted writes caught and 26 planted reads passed. The scope (aliases, ref/out, files outside UI) is stated in the check's output. |

## Pass 2 (66204ecf): method

- Read the diff `56cfd190..66204ecf` (8 files, +110/−52).
- Checked that `git status` is empty, and re-ran the gate.
- Grepped the three callers of `PlayerLever.ApplyTo` (`Pull` and the two replay paths in `ShadowBaseline`); each passes `replay` correctly.
- Grepped for identity comparisons on `FedChair` and `CabinetMinister`; there are none.

## Pass 2: verifications

1. **`FedChair.Copy` (MemberwiseClone) is safe.** The class holds a string `Name`, a `Philosophy` enum, a string `Description` and a float `RateBias`, with no mutable reference fields, so the shallow copy is complete. Nothing compares heads or ministers by identity.
2. **The FinanceStance lever keeps its stored value and its gate.**
   - The stored expression is still `Mathf.Clamp(target, -PlayerTargetLimit, PlayerTargetLimit)` into `c.FinanceStancePlayerTarget`. `target` becomes a float closure field of the same width, already rounded.
   - The four-part guard still returns before `Pull`, so a refusal neither forks nor schedules.
   - Its callers are the UI (only when the dial moved) and `FinancePartnerDiagnostic`, which runs with no ledger. No dump path calls it.
3. **The inactive host at load skips nothing the fork needs.** `Awake` only builds a default world and seeds that world's published history, and `RestoreInto` → `SetWorld` replaces both. In the Editor, `Awake` never ran for any host, so the bars see no change.
4. **The new dial case is the game's order:** 365 × `AdvanceDay` with the levers between days, then `ledger.AdvanceTurn` (fork at 365), then `sim.AdvanceTurn`, then `None.AdvanceTurn`, then `EnergyMarket.BeginTurn`.
   - The one stated exception is the player's `AdvanceCountryDayTick`, D-45's gap.
   - The Taxes target is read once at the boundary, the same in every run.
   - The other families' except-worlds carry the dial on both sides.
5. **The new state is clean.**
   - **SimulationManager.cs:** the verbs, the stance routing, the two refusals and the public constants.
   - **PolicyImpactLedger.cs:** the "Fiscal stance" family has an empty field list (`Touches` is false), is iterated in `AdvanceTurn`, and is checked both ways by `AssertPartitionCovers`.
   - **FedChair.cs:** one method added.
   - No trajectory-path method body changed apart from the stance store moving into its closure. A dump calls no verb, so the digest cannot move.

## Pass 2 findings (all Low)

- **L-A:** `FinanceStance` is not exercised by the lever-ledger diagnostic. The USA with no party seated holds no Finance partnership, so the verb is refused there and "Fiscal stance" is never forked; its exactness is argued, not measured. Part of the stance acts in the budget process, which an except-world reaches only through its day tick (D-45), so the family's line may miss part of its effect. Fix: a Germany/FDP case, or state the gap in the § record.
- **L-B:** "the event answers are out of scope" is not yet in the tree (`ResolveCabinetDecision`, `ResolveForeignPolicyMeeting`, `AnswerOffer`). Fix: the scope line in the § record and in GA-0-55's row, which should also say the lever map (`LeverMapDump`) still cannot see these levers.
- **L-C:** a drag of the stance dial pulls once per changed frame. Harmless, since the acts replay in order to the same final value; coalescing is optional.
- **L-D:** the regex scope is narrower than the ruling, and the check's output says so. A legitimate UI write to a preview clone's state would be a false positive; none exists today.

## What held, across both passes

- The stored values are the same in all six verbs.
- Each verb asks the gate overload the UI draws its lock from.
- `DaysIntoTurn` is correct at the boundary, at a load, from the seed and mid-turn.
- A counterfactual world never runs ahead of the real game.
- The no-policy world never receives a lever.
- The ledger exists only in `GameController`.
- Every existing bar user of `ShadowBaseline` forks at day 0.
- The ledger is not saved, so pending acts vanish with their worlds on load, consistent with its history.

## VERDICT for 66204ecf

**SHIP WITH FIXES.** The code is ready. Blocking: H1, the registration of `UiLeverVerbCheck` (cheap table) and `PlayerLeverLedgerDiagnostic` (simulation table), confirmed by a green bar log. Record-only: L-B. Optional: L-A, L-C, L-D.
