# Review: FORK-2 + LEDGER-1 (`wave/fork2`, a0f324f6 on 81007c26), 2026-10-20

An independent adversarial reader. Read-only: no code edited, nothing run, Unity not opened. Every finding below comes from reading the code at a0f324f6. Findings I could not back by reading were dropped.

**Verdict: SHIP WITH FIXES.** The played world's election move is verbatim, and the determinism plumbing (generator swaps, a world with no player left untouched) holds. Three findings are blocking. Five verbs the player answers with are not replayed, and FORK-2's own change turns one of them into a permanent stall. The no-policy world now holds the election but is never handed the formation answers, so it stalls too. Neither diagnostic is on a bar.

## Files reviewed

- `Assets/Scripts/Simulation/SimulationManager.cs` (the 20 `Answered` wrappers, `Pull`, `CopiesForLedger`, `SetAsideForeignPolicyMeeting`, `PollingDayToday`, the round's stage switch, the unwrapped verbs)
- `Assets/Scripts/Simulation/SimulationManager.PlayerElection.cs` (new: `HoldPlayerElection`, `RunPlayerElection`, `SeatPlayerElection`)
- `Assets/Scripts/Simulation/PolicyImpactLedger.cs` (`AfterAnswer`, `Carry`, `FromCarried`, `seatAsPlayed` at both fork sites)
- `Assets/Scripts/Simulation/ShadowBaseline.cs` (`Schedule(int, PlayerAnswer)`, `DoScheduled`, `RunDaysThrough`, `HoldTheElection`, `CatchUp`, `Carry(int)`, the fork constructor)
- `Assets/Scripts/Simulation/PlayerLever.cs` (`PlayerAnswer`, `Copies<T>`)
- `Assets/Scripts/Persistence/SaveGame.cs`, `Assets/Scripts/Persistence/SaveGameService.cs` (`ImpactLedgerWorlds`, `RestoreInto(sim, save, seatsPlayer)`)
- `Assets/Scripts/UI/GameController.cs` (`CheckElection`, `ResolveElectionVerdict`, `RunNationalElection`, `SaveToPath`, `CarryTheShadow`, `CarryTheLedger`, `CarriedOrEmptyLedger`, `RestoreFromSave`, `Update`, `AdvanceTurn`, the hold gates)
- `Assets/Scripts/UI/GameController.SejmVerdict.cs`
- `Assets/Editor/ForkAnswersDiagnostic.cs` (new), `Assets/Editor/PolicyImpactLedgerDiagnostic.cs`, `Assets/Editor/ForkDayTickDiagnostic.cs`, `Assets/Editor/PlayerLeverLedgerDiagnostic.cs`, `Assets/Editor/NoPolicyShadowCarryDiagnostic.cs`, `Assets/Editor/CheckSuite.cs`
- Static checks, for their rules: `Assets/Editor/ChamberVerdictCacheCheck.cs`, `Assets/Editor/UiLeverVerbCheck.cs`, `Assets/Editor/ConstantProvenanceCheck.cs`, `Assets/Editor/MetaTextCheck.cs`
- Context: `Assets/Scripts/Elections/NationalElection.cs` (`DeriveRegional`, `LastRegional*`), `Assets/Scripts/Elections/WorldClock.cs` (`StartDate`, `VintageOfElection`), `COMPLETED.md` §871/§872, `POLISIM_FEATURE_LIST.md` rows 2. FORK-2 and 2. LEDGER-1, `docs/ASKS_FOR_ELIAS.md`

## What holds (verified)

- **The election move is verbatim for the played world.** I compared `RunPlayerElection` with the removed `RunNationalElection` line by line: the same US branch, the same campaign condition, the same `SharesFromCampaign`/`TryPredictShares` calls with the same arguments, the same record writes, and `SetSeatsFromElection` then `PartyCapital.CarryOver`. The only changes are the `IsCounterfactual` gates on the logs.
- `SeatPlayerElection` keeps the removed verdict's order and guards:
  1. the presidential early return, before any formation;
  2. `OfElection`, then `Form`, then `ViewOf`, then `StandingRefusals.Clear`;
  3. `RoundsApply`;
  4. `SejmStepsApply`, with `OpenSejmFormation` (until now the first line of `ResolveSejmVerdict`);
  5. the install with `ResetArrivalBudgetWindow`.

  The dropped `WorldClock.VintageOfElection` call was an unused local, and the function is pure. The controller still owns the verdict, the night (`ShowElectionNight`), `_pendingElection*` and the game over (`ApplyElectionVerdict`), none of which writes the world. `PollingDayToday` needs the manager's `PlayerCountryId`, so the controller-id/manager-id difference (`_selectedPlayerCountryId ?? USA`) cannot change which elections run.
- **The USA:** no double hold and no conflict. The day loop (`HoldUsPresidentialCount`) elects the president and Congress in a fork as before, and `HoldPlayerElection` then adds only the NotImplemented record the controller path adds. `SeatPlayerElection` returns false on it.
- **The last-day order:** `RunDaysThrough` skips the election on day 365 and `AdvanceTurn` holds it after the fork's boundary. That matches `Update`'s order: the day, the tick, the boundary, then `CheckElection`.
- **Determinism:**
  - `Schedule(int, PlayerAnswer)` swaps in the fork's own generator, and `DoScheduled` runs inside `AdvanceTurn`/`CatchUp`, whose swaps are already in place.
  - `AfterAnswer`, `CopiesForLedger` and the index lookups draw nothing.
  - A world with no ledger (`ImpactLedger == null`) runs every verb's act unchanged, and `HoldTheElection` needs `_dayTickFor`, so batch dumps, the seeded shadow and `TrajectorySentinelCheck`'s worlds are untouched.
  - A fork's manager has no ledger, so a replay never re-reports.
- **Save/load:**
  - `CatchUp` mutates only counterfactual worlds, inside the generator swap.
  - `SaveGameService.CreateSaveGame` does not set `ImpactLedgerWorlds`, so the SaveLoadRoundTrip string-equality is unaffected.
  - An older save (null field) and a refused carry both fall back to an empty ledger and log it.
  - `FromCarried`'s `ledger.Dispose()` disposes only the except-worlds, not the caller's no-policy world.
- **The static checks:**
  - Every backticked `Type.Member` in the added text exists (15 checked).
  - The only new consts are in an Editor diagnostic, outside `ConstantProvenanceCheck`'s `Assets/Scripts/Simulation` scope.
  - `ChamberVerdictCacheCheck`'s `DirectWrite` regex does not match `country.Government = ...`, the whole-record install now in `SeatPlayerElection`.
  - `UiLeverVerbCheck` covers lever kinds only, and the UI lost a world write rather than gaining one.
  - No new player-visible string reaches `MetaTextCheck`.

## Findings, ranked

### H1. Five of the player's answers are not replayed. With FORK-2 one of them stalls a fork's government permanently.

**Where:** `SimulationManager.cs:5462` `PassFormation`, `:3985` `WithdrawSupport`, `:4083` `LeaveGovernment`, `:4144` `AskToBeDischarged`, `:5975` `ClearCampaignQueue`. None goes through `Answered`, and all five are called from the UI:
- `GameController.FormationSheet.cs:377`;
- `GameController.ParliamentRows.cs:127`, `:348`, `:1151`, `:1199`;
- `GameController.Campaign.cs:853`, `:898`.

**Failure:** a Swedish player is asked first after the 2030 count and passes. The played world's round moves on (`AskNext`), but the except-world's round, now opened because the fork holds the election, reaches `RoundStage.PlayerAsked`. The stage switch at `SimulationManager.cs:4713-4733` then does nothing ("PlayerAsked and OfferToPlayer wait on the player"). That fork never forms a government, and the whole difference is booked to every touched family. Before FORK-2 the fork held no election, so it could not stall here: this is a regression FORK-2 introduces.

The other four do the same thing more quietly:
- `LeaveGovernment` and `WithdrawSupport`: the fork keeps the player's party in the cabinet or support it left.
- `AskToBeDischarged`: the fork never discharges.
- `ClearCampaignQueue`: the fork runs the campaign actions the player cancelled, which moves the election count.

The done-when ("a fork with no policy difference equals the played world") is false on each of these paths.

**Fix:** wrap all five in `Answered(...)` the same way: `PassFormation`, `WithdrawSupport`, `LeaveGovernment` and `AskToBeDischarged` replay by country, and `ClearCampaignQueue` replays as itself. Then add a check that enumerates the manager's public verbs called from `Assets/Scripts/UI` and fails on any world-writing verb that is neither a lever nor an `Answered` wrapper. `UiLeverVerbCheck`'s route scan is the model to copy, and that check is what would have caught this.

### H2. The no-policy world now holds the player's election, which is an open ask. It is never handed the formation answers, so its round stalls permanently.

**Where:** `ShadowBaseline.cs` `HoldTheElection` runs for every world with `_dayTickFor`, and that includes the game's no-policy world (`ForkTheShadowAtTheStart` and the carried constructor both set it). The class comment in `SimulationManager.PlayerElection.cs` says so ("and the no-policy world"). `docs/ASKS_FOR_ELIAS.md:237` still lists "Should the counterfactual hold its own elections? That rides on FORK-2" as unruled, and nothing in the diff marks this as DECLARED or provisional.

**Failure:** `PolicyImpactLedger.AfterAnswer` hands `_none` only `InPlaceOfIt`, which is the meeting set-aside. On any election where the Speaker asks the player's party first, or a formateur offers it a place, the no-policy world's round reaches `PlayerAsked`/`OfferToPlayer` and waits indefinitely (`:4733`). The "without your policies" line then runs on a caretaker government for the rest of the game. Every graph and the ledger's divergence read against it.

**Fix:** either keep `HoldsPlayerElection = false` for the no-policy world until Elias rules on the ask, or give `SubmitFormation`/`AnswerOffer`/`PassFormation` an `InPlaceOfIt` for `_none`. The second option needs a rule for what a party "with no changes" answers, and that rule is itself an ask. Record the choice as DECLARED and put it with the open ask.

### H3. Neither proof gate is on any bar, so LEDGER-1's done-when is unmet.

**Where:** `Assets/Editor/CheckSuite.cs` at a0f324f6 registers `PlayerLeverLedgerDiagnostic`, `ForkDayTickDiagnostic` and `NoPolicyShadowCarryDiagnostic` (`:590-592`). It registers neither `PolicyImpactLedgerDiagnostic` nor `ForkAnswersDiagnostic`. A grep finds no other enrolment.

**Failure:**
- The LEDGER-1 row's done-when is "`PolicyImpactLedgerDiagnostic` ALL PASS in the simulation bar". The commit says the diagnostic "restores the process state for the simulation bar", yet it is still in no bar. That is the exact gap that let LEDGER-1 rot (§871).
- FORK-2's done-when check (`ForkAnswersDiagnostic`) is not run by any bar either.
- I found no FORK-2 evidence log. The `pilmain.log` named as evidence is dated 2026-10-19 12:05, before this commit, and records the old failure.

**Fix:** register both in the simulation group, then run that group and record the bar.

### M1. A fork made at a boundary that is also the polling day skips the election.

**Where:** `PollingDayToday` is assigned only in `AdvanceDay` (`SimulationManager.cs:535`, `private set`). It is not saved, so the fork's save/restore round trip leaves it false. When `PolicyImpactLedger.AdvanceTurn` forks a dial family first touched in a turn whose last day is the player's polling day, the fork has `_daysRun = 365` and runs no day. It then crosses the boundary, and `HoldTheElection` calls `HoldPlayerElection`, which returns because `PollingDayToday` is false. The played world holds the election in `CheckElection` right after.

**Failure:** that family's except-world keeps the old chamber, and the whole election is booked to that family. This is rare, since boundaries are start-date anniversaries (`WorldClock.StartDate`), but an extra election (`OrderExtraElection`) can land on any day, and the failure is silent. No diagnostic covers it: `ForkAnswersDiagnostic` forks at a lever inside the turn.

**Fix:** restore the flag on the fork. Recompute `PollingDayToday` in `RestoreSaveState` (from `TryPlayerPollingDay` against `CurrentDate`), or have the fork constructor copy `realSim.PollingDayToday`. Then add a ForkDayTick-style case with the polling day on a boundary; a planted extra-election date can produce one.

### M2. Replay by position applies the answer to the wrong item, and drops refusals silently.

**Where:**
- `ResolveForeignPolicyMeeting` replays `pick` onto whatever meeting is pending, with no identity check.
- `ResolveCabinetDecision` checks only the portfolio at index `at`.
- `AnswerDocket` checks only index `at` against `GetDocket` (`SimulationManager.cs:1220-1300`, `:1841-1855`).

**Failure:** an except-world whose `Event`/`ForeignPolicy` draws have parted (a bill voted differently, or the election held on its own chamber) rolls a different meeting. The replay then applies option `pick` of the played world's meeting to that different meeting. A docket whose bills differ (a bill that failed in the fork, a different resolution) gets the played world's sign or veto on the wrong bill.

The reverse case is also unhandled. An except-world that rolls a meeting the played world never had, or whose replay refuses, keeps the meeting slot full indefinitely, which is the M2 shape now in except-worlds, because `InPlaceOfIt` goes only to `_none`.

`DoScheduled` and `Schedule` ignore `ReplayOn`'s result, so none of this is logged.

**Fix:**
1. Match by identity: the meeting's `Name`; the decision's `Name` with the portfolio; the passage's `Number`, `Date` and `Title`.
2. When the replay refuses, run `InPlaceOfIt` in the except-world too, so the meeting is set aside.
3. Log every refused replay once, at warning level.

### M3. An exception in a counterfactual's replay or election now reaches the played game.

**Where:** `Answered` calls `ImpactLedger.AfterAnswer`, which calls `Schedule`, which can call `ReplayOn` right away, all after the played world's act has committed. At the boundary, `GameController.AdvanceTurn` calls `_impactLedger.AdvanceTurn` before `_simulationManager.AdvanceTurn(decisions)`, with no guard. That call now runs full verbs (votes, formation) and `HoldPlayerElection` (formation, Sejm) inside `DoScheduled`/`RunDaysThrough`.

**Failure:**
- A throw inside a fork's replay escapes the played world's verb after its act. The UI's follow-up, for example `_formationSheetOpen = false`, is skipped, and the later except-worlds are not scheduled.
- A throw in a fork's day or election at the boundary skips the played world's `AdvanceTurn` for that frame.
- `Copies<T>`'s `DeserializeObject` runs inside the replay, and unlike `SerializeObject` in `CopiesForLedger` it is not guarded.

Before FORK-2 a fork's scheduled work was trivial lever assignments.

**Fix:** catch exceptions around `ReplayOn` in `Schedule`/`DoScheduled`, and around each world's `AdvanceTurn` inside `PolicyImpactLedger.AdvanceTurn`. Log the error and mark that family's line as failed. A counterfactual must never break the played game.

### M4. ForkAnswersDiagnostic cannot see the failures it most needs to.

**Where:** `Assets/Editor/ForkAnswersDiagnostic.cs`.
1. **Section 2's control is not unsaved.** `g.None.Carry(...)` and `g.Ledger.Carry(g.Sim)` run `CatchUp` on `g`'s own counterfactuals, so the "game never saved" has been caught up exactly as the loaded one was. "Every line keeps its span" therefore cannot detect a `CatchUp` that differs from the boundary's days. Run a third game, identical and never carried, as the control.
2. **Section 1's answers control lumps the two answer kinds together.** It passes when either the meetings or the budget bills make a difference, so a broken meeting replay could be masked by the bills. Add a meetings-only control.
3. **Section 1's coverage is narrow.** It exercises 2 of the 20 wrapped verbs, never reaches `PlayerAsked` (H1/H2), and never puts a polling day on a boundary (M1).
4. **The "5% no-meeting" risk** is deterministic at seed 777: the run either always passes or always fails. A cadence change that leaves no meeting fails loudly (`t.Meetings > 0`), not silently, so this one is acceptable.

### M5. Each save can now carry one full world per touched family, and catch each one up.

**Where:**
- `PolicyImpactLedger.Carry` runs up to 13 worlds (the families), each a full snapshot. §872 measured about 1.76 MB per world, and the snapshot is escaped again inside the outer JSON.
- Each world with scheduled entries runs `CatchUp`, up to 364 days, on the save's frame. That includes autosaves.
- The diagnostic prints the milliseconds and bytes but asserts nothing. AF-12's size question is still an open ask.

**Fix:** state the per-family cost in the ask to Elias, or bound it, for example by not carrying a world whose family line is zero. Time an autosave with all 13 families forked.

### L1. Counterfactual elections overwrite the process-wide regional readout.

`NationalElection.DeriveRegional` writes the process statics `LastRegionalShares`, `LastRegionalNames`, `LastRegionalWeights` and `LastRegionalKeys`. Fork elections at the boundary, and a `CatchUp` across a polling day at a save, now overwrite them with a counterfactual's derivation. Today's only reader is `ShowElectionNight`, in the same frame as the played world's count, so nothing is wrong yet. The hazard is a future reader (`ElectionNightFromModel.Available`, the German keys) seeing a fork's regions. Have the counterfactual skip the static write, or snapshot and restore it around the fork's election.

### L2. The forks' verbs write ungated lines into the played game's log.

The replayed verbs log without an `IsCounterfactual` gate: `LEVERS: ... a bill was refused`, `SPEAKER:`, `SEJM`, `CONFIDENCE`, `AGREEMENT`. Those lines now land in the played game's log and can mislead log-parsed figures (the memory rule is to parse figures from logs). Gate them as `RunPlayerElection` gates its own.

### L3. A meeting can be set aside in the no-policy world when the player answered nothing.

`ResolveCabinetDecision` and `ResolveForeignPolicyMeeting` report "acted" unconditionally. If the option is not in the pending meeting (`pick == -1`), the played world's act changes nothing, yet `_none` still sets its meeting aside. Return false where `pick < 0` or `at < 0`.

### L4. The lever-guard inside an answer is never exercised.

`Pull`'s `_answering > 0` guard is dead today: no wrapped `...Now` body pulls a lever. That is harmless, but its "the answer's replay pulls it again" rule has no test.

### L5. One load case drops carried family worlds without its own log line.

In `CarriedOrEmptyLedger`, a save that carries family worlds but whose no-policy world was refused drops them with no line of its own. Only the shadow's refusal is logged.

### L6. ForkDayTickDiagnostic's control now differs for two reasons.

The control, stepped as before J8, now differs by the election as well as the tick. That control proves J8's tick less sharply than before. A note is enough.

## Suggested order

H3, then H1 with a verb-enumeration check, then H2 (a ruling or a provisional switch-off), then M1, M3, M2, M4. M5, and the AF-12 asks that still stand, go to Elias.
