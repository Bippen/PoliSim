# Review: wave/ledg (K45-K52 on the impact ledger: K46, K47, K51)

**Branch:** `wave/ledg`, base `1768a303`, HEAD `bc49605a` (three commits: e9baf8c2, 32970b50, bc49605a).
**Reviewer:** an independent reviewer. I read the code and ran no Unity. The compile gate (`Tools/cgate/gate.sh`) printed `GATE OK`.
**Files reviewed:** `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/Simulation/ParliamentSystem.cs`, `Assets/Scripts/Simulation/PolicyImpactLedger.cs`, `Assets/Scripts/Data/CarbonRateStatute.cs`, `Assets/Scripts/Simulation/ShadowBaseline.cs` (the money paths: I read every changed line), and also `Assets/Scripts/Simulation/PlayerLever.cs`, `Assets/Scripts/UI/GameController.cs`, `Assets/Scripts/Persistence/SaveGame.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, `Assets/Editor/CarbonTaxUnitDiagnostic.cs`, `Assets/Editor/ForkAnswersDiagnostic.cs`, `Assets/Editor/PolicyImpactLedgerCheck.cs`, `Assets/Editor/PolicyImpactLedgerDiagnostic.cs` and `Assets/Editor/ShadowBaselineDiagnostic.cs`. Context read: `ApplyBillResult` in full, `CarbonRateStatute.AdvanceYear`, `TaxTerm.Charge/Move/ChargeApproval`, `ShadowBaseline.Carry/CatchUp/OnOwnGenerator`, `SimulationManager.Pull`, `AnswerWaitsAsTheAi`, `PassageYear`, `CommitCalendarYear`, and COMPLETED.md §872, §873 and §890.

## Verdict: READY

There are no High findings. Two Medium findings should be filed or put to Elias, but neither blocks the merge. One condition is not a finding: inertness on the AI path is argued below from the code, and CLAUDE.md requires it to be proved by the dump diff. The simulation bar (TrajectorySentinelCheck) must be green on this tree before the merge.

## The implementer's questions

1. **AI-path inertness: holds by reading.**
   - AI bills never fill `BudgetBill.CarriedTaxLines`. The only writer is the player's draft (`GameController.cs` ~9849). So `CarryMoves` returns at its first guard (`ParliamentSystem.cs:661`).
   - `GetBillDirection` returns `direction + CarryDirection(...)` (`ParliamentSystem.cs:251`), which is `direction + 0f`.
   - `direction` is never -0f: it starts at +0f, and round-to-nearest gives +0 for every x + (-x). So the sum is the identity, bit for bit.
   - In `ApplyBillResult` the new local `carryImpulse` (line 567) is a store of its own. No existing local is written again: `taxTermImpulse`, `clampedRate`, `moved`, `rateMoves` and `fromPoints` are untouched, and the existing `ChargeApproval(country, taxTermImpulse)` keeps its form.
   - The extra charge (line 643) is behind `governmentBears && carryImpulse != 0f`, so `TaxTerm.Charge` (pure) and the second `ChargeApproval` never run for an AI bill.
   - The IL-shape rule (EN-7b) says only the dump diff proves this. That is the condition above.
2. **`CarryMoves` mirrors the carry condition: yes, for every reachable state.**
   - Same filters: `IsImplemented`, `IsPerTonne`, `StatuteMoves`, a stamp above 0.
   - The bill's own move stamps `passageYear` only where `!Mathf.Approximately(clamped, Rate)`, as GA-8-1's `moved` does, with the same `Mathf.Clamp(figure, MinRate, MaxRate)`.
   - It skips a carry whose `Max(stamp, passedYear+1)` would not extend the stamp. `ApplyBillResult` still spends the draft (`CarriesPassed++`) there, which is the intended "prevents nothing".
   - Duplicates in `CarriedTaxLines` cannot arise: the draft adds one entry per line.
   - The step priced is `StepOf(stamp+1)`. I checked it against `AdvanceYear`'s `PassedForYear >= boundaryYear` rule with the yearly boundary: the first boundary that takes back a figure stamped S is the one in calendar year S+1. See L2 for an older save's stale stamp.
3. **Vote year against charge year: they cannot disagree today.**
   - In every path where the statute moves a figure (Sweden, Germany), the vote and the apply run in the same call on the same day: `SimulationManager.cs` 2021→2035 and the Swedish frame decision 1410/1423→1451.
   - `CommitCalendarYear` runs at the top of `AdvanceDay`, so `Country.CalendarYear == CurrentDate.Year == passedYear`.
   - The paths that apply with another year (`PlayerPresident.cs:258` `year`, the docket's `DocketLawDay`) are Poland, the USA and France. All three are `NominalFixed`, so `CarryMoves` is empty there.
   - The UI previews do diverge at a year turn (L1).
4. **The carry charge bypasses `TaxTermNet` inside the brackets: correct as declared.**
   - `ChargeApproval` books the impulse into `TaxTermStock` (it fades), and every caller's `ApprovalLedgerRecorder.RecordEvent` brackets `ApplyBillResult`, so the approval move is recorded.
   - The carry does not enter the fiscal year's net, so a later move of the line that year does not net against it (DECLARED in the `CarryMoves` summary).
   - It is charged in its own `ChargeApproval` call. That differs from one summed call only where the [0,100] clamp binds, and only on bills that carry.
5. **The fork before the act: holds.**
   - `BeforeAnswer` (`PolicyImpactLedger.cs:283`) uses the same fork as `BeforeLever`: `insideTurn: true` (an inactive host), the generator swapped in `OnOwnGenerator`, the played streams handed back.
   - `Answered` (`SimulationManager.cs:3798`) calls it outside `_answering`. A nested answer forks nothing.
   - A refused bill reaches `ForgetUnlaid` (line 3808), which disposes the fork.
   - Diagnostic case 10 has teeth on the played world being untouched: the Trade world's fingerprint, draws included, must equal the played world after the bill.
   - Untested: the refused-bill path (M2).
   - A throwing act leaves the fork standing. That is harmless, because a world with nothing withheld is the played world.
6. **K46's bookkeeping is complete.** These are the ways a ledger world can part from the played world:
   - a dial stripped (`AdvanceTurn`, line 239);
   - a lever left undone (`AfterLever`);
   - a bill left unlaid (`AfterAnswer`, line 271);
   - a replay that misses (`ShadowBaseline.Replay`, line 270);
   - a wait answered by the AI's rule (line 278).

   Each of them sets `_withheld` or `AnsweredAlone`. A carried world is marked withheld (line 431).

   A lever replay's return value is ignored in `Schedule`/`DoScheduled`. It can only fail in a world that already differs, so it is already flagged.

   `Carry` reads `AnsweredAlone` again after `CatchUp` and only then compares (line 336). `StatesAlike` (line 381) compares only `EconomyState`'s floats, as a backstop behind the bookkeeping. A world wrongly called "same" needs both a missed flag and equal economies. I found no missed flag.

   Stale `_withheld` entries can only make a save carry more (the safe direction).
7. **`StepOf` writes nothing: confirmed.** It reads `YearRatio`, `RuleFor` and `BehgFor`. The post-schedule `lawsFigureInForce` branch of `AdvanceYear` (turn 0, no stamp ≥ 2026) cannot be reached for a stamped line in a German game, whose first boundary is 2025-11-06.
8. **The rebuild on load is deterministic and draws no played stream.** It uses the same fork. Case 5 asserts that two loads of one autosave give the same worlds, each equal to the played fingerprint, draw counts included.
9. **Format 60 with no step is right.** `ImpactLedgerRebuild` is additive under ruling A, as in §872 and §890. A null in an older save rebuilds nothing, as before. The behaviour change for an older save (a pending carry is now voted and charged) is written in `SaveGameService.cs`.
10. **The diagnostics fail while their statements are false, mostly.**
    - CarbonTax (9) fails on a wrong direction, load or price, and on a priced no-op.
    - ShadowBaseline case 5 now fails where the fault escapes or no fault occurs. The old code passed silently.
    - The K46 bookkeeping and the refused bill have no teeth (M2).
11. **Rules.**
    - CRLF: all 14 files are pure CRLF, with no doubled CR (perl count).
    - The new family names are plain words with no register codes (MetaTextCheck).
    - The new public members are wired: `Rebuild`, `FamiliesToRebuild` and `Rebuilt` from `GameController`, `StepOf` from `ParliamentSystem` (UnwiredSubsystemCheck).
    - No new runtime constant needs provenance. The diagnostic's 55 cites BEHG § 10 Abs. 2.
    - The new doc comments use backticks. The only `<see cref` added is in the diagnostics, and `PolicyImpactLedgerCheck` reads through `SourceText.ReadWithoutComments`.

## Findings

### M1: K46 as built drops almost no world in real play, and case 5's byte saving does not represent a real game
`PolicyImpactLedger.cs:332-336`. Every touched family is marked `_withheld` from its first touch: a dial, a lever and a bill each mark it at once. So a manual save leaves out only two kinds of world: one rebuilt at an earlier load and untouched since, and a lever fork whose act failed (`Pull` forks, then returns false). In a game never loaded, a manual save carries every touched family, as before K46.

The ruling ("only for families whose ledger line is non-zero") was mostly about save size, about 1.76 MB per world (§872). The reading built here is DECLARED and safe: a family whose line reads zero with a bill still pending is carried.

`ForkAnswersDiagnostic.MeasureTheCost` (line 502) plants every family directly into `_except`, bypassing the ledger, so none is withheld. Its "K46" byte figure therefore shows a saving a real game will not see.

Recommendation: put the reading to Elias with a measurement from a played game, and label case 5's figure as planted.

### M2: K46's guards and K51's refused bill have no teeth
- If either `_withheld` or `AnsweredAlone` is removed from `Carry`, case 5 still passes. The Budget bill world differs in state, so `StatesAlike` alone carries it.
- If `StatesAlike` returns true always, case 5 also still passes. The only differing world is the withheld one.
- No case covers these:
  - a withheld world whose state is alike (it must be carried);
  - a world that answered alone and is alike (it must be carried);
  - a world neither withheld nor alike (`StatesAlike` must catch it);
  - a bill the gate refuses, which must leave no "Budget bill" world (`ForgetUnlaid`).

The worst failure mode, a world wrongly judged "same" and dropped, loses a line silently. Recommendation: add these four planted cases.

### Low
- **L1. The UI preview's year against the resolution's year.** The vote preview (`GetBudgetBillConcern` from `GameController.cs` 1270, 9157-9200) reads `CalendarYear` now. The approval preview (`SimulationManager.cs` 6918, 7217) uses `PassageYear`, the resolution's year.
  - Scenario: Germany, a bill drafted in December that resolves in January. The bill moves the line, so the preview prices `StepOf(Y+1)` and the vote prices `StepOf(Y+2)`; through 2026 `BehgFor` differs.
  - Second scenario: a figure already stamped Y+1 reads "prevents nothing" in the preview but is priced at the real vote.
  - The real vote and charge agree. This is latent for any statute-moving country that gains a held or docket path.
- **L2.** A stale stamp S < passageYear−1 (an older save; AF-36 now clears stamps) is priced at `StepOf(S+1)`, an old BEHG year. The UI drafts no carry for a stale stamp (`PassedFigureStands`), so this is unreachable from play (`ParliamentSystem.cs:676`).
- **L3.** `CarryTheLedger` (`GameController.cs:882`) passes `out rebuild` straight through. If `Carry` throws after naming families, the save keeps the partial `ImpactLedgerRebuild` while the log says "counts each line from the load". The effect is harmless, since the named worlds are exact, but the log is wrong. Set `rebuild = null` in the catch.
- **L4.** `CarryMoves` allocates a `List` on every call, including every AI vote and every UI frame's concern, before its early return. Return a shared empty list, or test before allocating.
- **L5.** `PolicyImpactLedgerCheck` skips any `new PlayerAnswer(` whose first argument is not a string literal (a ternary label), so a future bill verb labelled that way escapes it. The debt-limit raise, which is voted as a budget bill through `ApplyBillResult`, stays an answer of no family (DECLARED). It should be listed for Elias beside K51.
- **L6.** Case 5 is fragile. If a budget bill was laid before `HeadDay` and none after, `_withheld` keeps "Budget bill" from the disposed earlier world, the world is carried, and the expectation fails. Clear the planted families' marks, or plant before any bill.
- **L7.** `CatchUp` clamps to `DaysPerTurn-1`. A manual save on the boundary day compares a world one day behind and carries it spuriously (the safe direction). For information.
- **L8.** A rebuild is equivalent to the lazy fork at the next touch. It costs one fork per named family at load (case 5 reports the ms). It is built as ruled; for information.

## Owed before the merge
- The cheap bar and the simulation bar, green on this tree. TrajectorySentinelCheck's digest must be unmoved, which proves the AI-path inertness argued in point 1.
- The review-ledger row (`Tools/review_row.ps1`) for this report.
