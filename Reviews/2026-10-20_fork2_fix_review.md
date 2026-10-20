# Review: the FORK-2 fix round (`wave/fork2`, a0f324f6..76073230), 2026-10-20

An independent adversarial reader of the fix round that answers `Reviews/2026-10-20_fork2_review.md`. Read-only: no code edited, nothing run, Unity not opened. Every finding comes from reading the code at 76073230. Findings I could not back by reading were dropped.

**Verdict: SHIP WITH FIXES.** Most of the round holds:
- The played world's election path is still byte-identical.
- The AI rule never runs in the played world or on its generator.
- The generator and the log filter are restored in a `finally`.

One blocking defect remains. The stall the first review's H2 named is fixed for the no-policy world, but not for an except-world whose round waits on the player where the played world's round never did. The first review's H3 (the proof gates on no bar) is also still open: `CheckSuite.cs` is untouched by this commit.

## Files reviewed

- `Assets/Scripts/Simulation/SimulationManager.cs`:
  - the 25 `Answered` wrappers, with the five new ones (`WithdrawSupport`, `LeaveGovernment`, `AskToBeDischarged`, `PassFormation`, `ClearCampaignQueue`);
  - `Answered`, `ResolveCabinetDecision`, `ResolveForeignPolicyMeeting` and `AnswerDocket`;
  - `AskNext`, `Table`/`TableAndVote`, `AdvanceSpeakerRound`, `Investiture`, `DraftProposal`, `SubmitFormationNow`, `PassFormationNow` and `AnswerOfferNow`;
  - `AdvanceDay` (the polling flag), `TryPlayerPollingDay`, the cabinet roll in `AdvanceTurn`, `HoldPresidentialRound` and `HoldUsPresidentialCount`.
- `Assets/Scripts/Simulation/SimulationManager.PlayerElection.cs`: `HoldPlayerElection` with its once-a-day guard, `RecomputePollingDayToday`, `AnswerWaitsAsTheAi`, and `RunPlayerElection`/`RunPlayerElectionNow`.
- `Assets/Scripts/Simulation/PolicyImpactLedger.cs`: `Fork`, `RetireIfFailed`, `RetireTheFailed`, `Unavailable`, `AdvanceTurn`, `BeforeLever`, `AfterAnswer`, `AfterLever`, `Carry`, `FromCarried` and `LinesFor`.
- `Assets/Scripts/Simulation/ShadowBaseline.cs`: `OnOwnGenerator`, `Schedule` (both overloads), `Replay`, `AnswerTheWaits`, `DoScheduled`, `RunDaysThrough`, `CatchUp`, `Carry`, and the three constructors.
- `Assets/Scripts/Simulation/PlayerLever.cs`: `PlayerAnswer.AnswersAWait`.
- `Assets/Scripts/Elections/NationalElection.cs`: `KeepRegionalReadout`, plus the readers and writers of `LastRegional*`.
- `Assets/Scripts/UI/GameController.cs`: `Update`'s holds, `SaveToPath`, `CarryTheShadow`, `CarryTheLedger`, `CarriedOrEmptyLedger`, `AutosaveIfDue`/`_savingAutomatically`, `DeclarationHolds`, `FormationHolds` and `AdvanceTurn`.
- `Assets/Editor/UiLeverVerbCheck.cs`: part (4), with `Classify`, `BodiesOf`, `BlankLiterals`, `CallsIn` and `NotAnAnswer`.
- `Assets/Editor/ForkAnswersDiagnostic.cs` (all five cases) and `Assets/Editor/ForkDayTickDiagnostic.cs`.
- `Assets/Editor/CheckSuite.cs`: registration and `CheckExit.ArmLogFold`.
- Context:
  - `Assets/Scripts/Data/DivisionRecord.cs` and `Assets/Scripts/Simulation/Congress.cs` (`Divide`/`Record`);
  - `Assets/Scripts/Elections/FormationProposal.cs` (`Formateur.Answer`) and `Assets/Scripts/Elections/SpeakerRound.cs` (`RoundStage`);
  - `Assets/Scripts/Simulation/SimulationManager.Sejm.cs`, `SimulationManager.PlayerPresident.cs` and `SimulationManager.AiVoteMemo.cs`;
  - `Assets/Scripts/Simulation/ForeignPolicySystem.cs`, `EnergyFleet.cs` and `EnergyMarket.cs`;
  - `Assets/Scripts/UI/GameController.Campaign.cs`, plus the UI call sites of the wrapped verbs (FormationSheet, ParliamentRows).
- Static checks, for their rules: `Assets/Editor/CommentClaimCheck.cs`, `Assets/Editor/ConstantProvenanceCheck.cs` (scope), `Assets/Editor/ChamberVerdictCacheCheck.cs` and `Assets/Editor/MetaTextCheck.cs`.

## What holds (verified by reading)

### The answers

- **All 25 `Answered` sites are in `SimulationManager.cs`, and the five new wrappers move their bodies verbatim** into `...Now`.
  - The identity is captured before the act: the party asked is read before `AskNext` changes it, and the meeting's name before the slot empties.
  - No simulation code calls the five verbs internally, so the played world's own paths never fire a stray `AfterAnswer`. The only callers are the UI and `UiScreenshotDriver`.

### The AI rule

- **The AI rule never touches the played world.**
  - `AnswerWaitsAsTheAi` returns at once unless the world is `IsCounterfactual`.
  - Its only callers are `ShadowBaseline.Replay` and `AnswerTheWaits`, and both run inside `OnOwnGenerator`. So it draws only on the world's own streams, and the played world's draw counts cannot move.
- **The rule's `PlayerAsked` branch is the AI's.** It matches the `Consulting` case of `AdvanceSpeakerRound` (`SimulationManager.cs:4750-4762`): the same `ConsultationDays` wait from `AskedOn`, then `DraftProposal(country, round, round.Asked)` and `TableAndVote`. The `Involves → OfferToPlayer` step is skipped, which is correct when the player's own party is the formateur.
- **The rule cannot loop.**
  - A failed investiture counts toward `ProposalLimit` and ends in `BreakOff`. A declined offer either tables a proposal without the player or calls `AskNext`. A Bundestag round ends by its phase timers in `PluralityBallot`.
  - Nothing re-asks on the same day, so I found no unbounded cycle.

### Containing a failure

- **The generator and the log filter are always put back.**
  - `OnOwnGenerator` restores both in its `finally`.
  - The fork constructors restore the generator in their own `finally`.
  - `RunPlayerElection` restores the regional readout in a `finally`.
- **The process-wide state a swallowed exception could half-write is safe.**
  - `EnergyFleet.For` and `EnergyFleet.RecordOnly` are `using` scopes, so `_recordDepth` and `Ambient` unwind on a throw.
  - The `EnergyMarket` turn state is set at the top of every world's `AdvanceTurn` (`SimulationManager.cs:6304`) and again by the controller after the no-policy world's turn (`GameController.cs:6117`).
- **A swallowed fault does not pass a bar.** `CheckExit.ArmLogFold` counts `LogError` as a failure, and `OnOwnGenerator` logs the fault at error level.

### The election

- **The played world's election path is unchanged.**
  - For the played world, `RunPlayerElection` calls `RunPlayerElectionNow`, whose body is the old one.
  - `GameController.CheckElection` is untouched by this commit.
  - `HoldPlayerElection`, and with it the new guard, is reached only by forks and harnesses.
- **The once-a-day guard cannot suppress a legitimate election.**
  - `ElectionHistory` is written only by `RunPlayerElectionNow` (three sites).
  - `TryPlayerPollingDay` yields one date, so an extra election on the scheduled date is the same election.
  - Presidential rounds and run-offs (`HoldPresidentialRound`, `HoldUsPresidentialCount`) do not write `ElectionHistory`.
- **M1 is fixed.** `RecomputePollingDayToday` uses `AdvanceDay`'s exact formula (`SimulationManager.cs:535`). A boundary fork on the polling day now holds the election after the boundary, and case 3 has teeth (`ForkHeld`).

### The autosave and the static checks

- **The autosave flag always resets.** `_savingAutomatically` is reset in a `finally` (`GameController.cs:1532`), and `SaveToPath` catches every exception anyway. A manual save after an autosave carries the family worlds.
- **The static guards hold:**
  - Every backticked `Type.Member` added exists. I checked 9 claims, including `Formateur.Answer`, `GameController.ForkTheShadowAtTheStart`, `NationalElection.KeepRegionalReadout` and `ShadowBaseline.HoldsPlayerElection`.
  - `ConstantProvenanceCheck`: no const is added under `Assets/Scripts`.
  - `ChamberVerdictCacheCheck`: no direct chamber or government write is added. The rule installs only through the existing `TableAndVote`/`Investiture`.
  - `MetaTextCheck`: no banned token is added.
- **The UI-1 possessive pass will not trip on this diff.** No new runtime string builds "'s" after an interpolation hole or a concatenated variable. The only "'s" in new runtime literals are fixed text: "by the AI's rule" (`ShadowBaseline.cs:260`), "the campaign's day cleared" (`SimulationManager.cs:6024`) and "the impact ledger's carried worlds" (`GameController.cs:895`). The `{family}` and `{round.Asked}` holes are followed by " world", " line" and " proposes", never by "'s".

## Findings, ranked

### H1. An except-world still stalls when its round waits on the player and the played world's never did.

**Where:**
- `ShadowBaseline.cs:257-271`: `Replay` and `AnswerTheWaits`.
- `ShadowBaseline.cs:72`: `AnswersWaitsAsTheAi`, false by default.
- `SimulationManager.cs:4767-4768`: `PlayerAsked`/`OfferToPlayer` wait with no timeout.

**Why:** an except-world switches to the AI's rule only after a replayed answer to a wait misses. If the played world's round never stops at the player, the player gives no formation answer, nothing is replayed, nothing misses, and the except-world waits indefinitely.

**Failure scenario:**
1. A Swedish player in opposition touches the tax family.
2. At the next count the played world's Speaker asks S. S forms without the player's party, so there is no `PlayerAsked` and no offer.
3. In the tax except-world the economy, and so the seats, differ. The Speaker asks the player's party first, or the first formateur's `DraftProposal` involves the player and the stage becomes `OfferToPlayer`.
4. That world's round reaches `PlayerAsked` or `OfferToPlayer` and stops (`AdvanceSpeakerRound`'s default case).

The meetings still match by name, so no later miss flips the world to the AI's rule. It governs with a caretaker until the next election ends the round (`SimulationManager.cs:5441-5446`), up to four years. The whole government difference is booked to the tax line.

`ForkAnswersDiagnostic` demonstrates this exact shape: case 2's `FormationAround` control, a fork handed no formation answer, is asserted to stall (`ForkAnswersDiagnostic.cs:127`). The fix round calls that shape "every fork before the fix", but it is still the live behaviour for any except-world whose wait has no counterpart in the played world. The H2 fix covers only the no-policy world.

**Fix:**
1. Turn the AI's rule on for every ledger world.
2. Run it at the START of the next day, before `AdvanceDay`, not after the day.

This is safe because the played clock holds on every such wait (`GameController.Update`'s gates, `FormationHolds`). So the played world's answer to a day-*d* wait is always scheduled for day *d* and is replayed in `DoScheduled` before the next day's rule runs. Deferring to the next morning also closes M3 below, and it keeps the no-policy world's `ForeignPolicy` stream in step: a meeting is set aside before the next day's roll.

**Test:** add a diagnostic case where the except-world's round reaches the player and the played world's does not. One way is a dial at the boundary that moves the seats; another is to plant `PlayerAsked` in the fork only.

### H2 (carried from the first review's H3). Neither proof gate is on a bar.

**Where:** `Assets/Editor/CheckSuite.cs` is not in the fix round's diff. A grep finds no registration of `ForkAnswersDiagnostic` or `PolicyImpactLedgerDiagnostic` anywhere in `Assets/Editor` or `Tools/`. `ForkDayTickDiagnostic` and `PlayerLeverLedgerDiagnostic` are registered (`CheckSuite.cs:590-591`).

**Failure:**
- FORK-2's done-when check and LEDGER-1's gate still run on no bar.
- The commit names no evidence log for the rebuilt five-case diagnostic.
- Nothing proves the new code compiles and passes, and nothing will catch it rotting.

**Fix:** register both in the simulation group, run the group, and record the bar's log in the commit or `COMPLETED.md`.

### M1. A docket answer is matched by `DivisionRecord.Number`, a per-world counter, so one divergence drops every later docket answer in that world.

**Where:** `SimulationManager.cs:1851-1857` (`AnswerDocket`'s replay); `DivisionRecord.cs:24-25,146-151`.

**Why:** `Number` is "monotonic per country", incremented by every division. That includes:
- each chamber's vote (`Congress.Divide`). The Senate's record is written only when the House passed (`Congress.cs:483-487`);
- override votes;
- confidence motions (`SimulationManager.cs:4077/4087/4303`);
- investitures (`:4805`) and person ballots (`:4859`).

Once an except-world records one division more or fewer than the played world, every later passage's `Number` is offset.

**Failure scenario:** a US player-president. In the fiscal except-world, one bill fails in the House, so there is no Senate division. From then on every Sign or Veto replay finds no match. `AnswerDocket`'s answer is not `AnswersAWait`, so nothing is answered there: a vetoed bill becomes law at its term in that world, and the family line absorbs the difference. The only signal is one warning per miss.

The first review suggested `Number`. That suggestion was wrong, because `Number` is not an identity across worlds.

**Fix:** match on `Title` + `Date` + `Chamber` + `Origin`, which are what the passage is in every world. Better still, give `DocketEntry` a stable bill identity (kind and introduced date) and match on that.

### M2. The AI-mode flag is not carried in a save, so a load changes an except-world's behaviour.

**Where:**
- `ShadowBaseline.cs:72`: `AnswersWaitsAsTheAi` is an auto-property.
- `ShadowBaseline.cs:358-384`: `Carry` snapshots only the manager and world.
- `ShadowBaseline.cs:393-416`: the carried constructor leaves the flag false.
- `PolicyImpactLedger.cs:156-157`: `_unavailable` is not carried either.

**Failure:** an except-world that switched to the AI's rule after a miss is saved and loaded. It comes back in waiting mode. If it was relying on the rule to clear a wait the played world will never answer, it stalls again (H1's shape). It also now behaves differently from the never-saved game, so "a load keeps every line's span" is false for that family.

Case 4 cannot see this, because it never makes a miss. Similarly, a family retired as unavailable is forked afresh after a load, from the load.

**Fix:** carry the flag, and `Unavailable`, beside the world: in `SaveGame.ImpactLedgerWorlds`'s value, or a sibling map. H1's fix (the rule always on) makes the flag moot.

### M3. A manual save during a held wait lets an AI-mode world answer the wait before the player.

**Where:** `PolicyImpactLedger.Carry` calls `ShadowBaseline.Carry(realSim.DaysIntoTurn)`, which calls `CatchUp`, which calls `RunDaysThrough(through)` (`ShadowBaseline.cs:318-327`, `:294-304`). `RunDaysThrough` runs the played world's current day *d* and then `AnswerTheWaits()`.

**Failure:**
1. The clock is held on a meeting or a formation question on day *d*, and the player saves from the saves screen.
2. Each AI-mode except-world is caught up through day *d*. Its rule then sets the meeting aside, or answers the offer.
3. The player answers. `Schedule(d, …)` replays at once because `d <= _daysRun`, finds no match, and logs a miss. The player's actual choice is lost in that world.

The autosave cannot reach this path (no autosave runs during a hold, and autosaves no longer carry family worlds). A manual save at a decision point is a natural thing for a player to do.

**Fix:** run the rule at the start of the next day (H1's fix). Alternatively, never run `AnswerTheWaits` for the day a `CatchUp` stops on.

### M4. A failure is contained but surfaces nowhere, and a failed no-policy world freezes every line silently.

**Where:**
- `PolicyImpactLedger.cs:156` (`Unavailable`) has no reader. A grep finds none in `Assets/Scripts`, and `LinesFor` (`:309-328`) iterates only `_except`.
- Nothing reads `ShadowBaseline.Faulted` for the no-policy world. The only reader outside the ledger is `ForkAnswersDiagnostic.cs:250`.

**Failure:**
- **A failed family world:** its line disappears from the ledger, its share silently joins "interaction", and the screen says nothing. The doc's "its line is unavailable" is not shown to anyone.
- **A failed no-policy world (`_none`):** `OnOwnGenerator` returns at once from then on. `LinesFor`'s `divergence = real − none` then reads a frozen world for the rest of the session. So does every "without your policies" graph, while `HasAnything` stays true.

Before this round the exception would at least have been loud in play.

**Fix:**
- Have `LinesFor` return an "unavailable" line for each entry in `Unavailable`.
- Have the controller check `_shadowBaseline.Faulted` after its `AdvanceTurn`, then label the graph and drop the ledger, or re-fork it with the count-from-now caveat.

### L1. The offer rule uses `AllAccept`, not the party's own answer, so "as it answers for an AI-run party" overstates it.

**Where:** `SimulationManager.PlayerElection.cs:56-60`.

**What it does:** the player's party declines the offer whenever ANY invited party refuses. The decline writes `round.Declines` ("X>formateur"), so `DraftProposal` keeps the player's party out of that formateur's later drafts (`SimulationManager.cs:4575-4582`).

**Why that is not the AI's behaviour:** an AI-run party is never offered anything. The AI formateur tables, and each party's consent is its own `PartyAnswer.Accepts` at the investiture.

**Effect:** the effect is narrow, because `DraftProposal` picks from `Holding`, which is rarely refused.

**Fix:** use `verdict.Answers.Find(a => a.Party == country.PlayerPartyAbbrev).Accepts`. Alternatively, accept and let the investiture decide, which is closer to the AI path. Either way, correct the doc to match.

### L2. L1's fix is partial: a fork's ordinary days still write the regional readout.

**Where:** the keep wraps only `RunPlayerElection` (`SimulationManager.PlayerElection.cs:70-76`). Every fork's `AdvanceDay` also runs `HoldPresidentialRound` (Poland, `SimulationManager.cs:5648`) and `HoldUsPresidentialCount` (the USA, `:5677`). Both call `TryPredictShares`, whose `DeriveRegional` writes `LastRegional*` (`NationalElection.cs:229,320-367`).

**Effect:** today's readers (`ElectionNightFromModel`, `GameController.cs:6516`) read only Sweden's and Germany's readout, so nothing is wrong yet.

**Fix:** move the keep into `ShadowBaseline.OnOwnGenerator`, so every counterfactual step leaves the readout as it found it.

### L3. A fork that throws in its constructor leaks its hidden host.

**Where:** `ShadowBaseline.cs:145-175`. `_host` is created before the throwing restore. `PolicyImpactLedger.Fork` (`:178-183`) catches the throw but has no object to dispose.

**Effect:** a `HideAndDontSave` GameObject with a `SimulationManager` stays alive for the session. That component has no `Update`, so the cost is memory only.

**Fix:** catch inside the constructor, `DestroyImmediate(_host)`, and rethrow.

### L4. The AI's rule does not answer cabinet decisions, so they pile up in the no-policy world and in AI-mode worlds.

**Where:**
- `AnswerWaitsAsTheAi` covers the meeting and the round only.
- `SimulationManager.cs:6643-6652` appends decisions every boundary while the player's party holds a portfolio.
- `ResolveCabinetDecision`'s replay is not `AnswersAWait`.

**Effect:**
- The no-policy world never resolves a decision. This is unchanged from a0f324f6, but the doc now claims the rule answers "every wait on the player".
- Pending decisions accumulate for the whole game and are carried in every save.
- The identity `Find` (`:1229`) takes the OLDEST pending decision with the same name and portfolio, so a fork's replay can land on a stale one.

**Fix:** either give the rule an answer (set the decision aside, or take the AI's option) or narrow the doc. Also match the newest pending decision, not the first.

### L5. `UiLeverVerbCheck` part (4): what it cannot see.

The classifier reads `_simulationManager.X(` in `Assets/Scripts/UI` and, for each call, the manager's bodies one call deep. It misses:
1. **Overloads:** `BodiesOf` merges every overload of a name, so a writing overload passes when another overload of the same name is an answer.
2. **Mixed bodies:** a body that contains `Answered(` or `Pull(` anywhere passes, even if it also writes directly, on another branch, or before the wrapper. One call deep, any helper that answers clears the caller.
3. **Calls that are not `_simulationManager.X(`:**
   - local aliases (`var sim = _simulationManager`);
   - method groups or delegates (`_simulationManager.X` with no parentheses);
   - static `SimulationManager.X(...)` calls.

   None of these exist in the UI today. I grepped for them.
4. **Property setters:** `_simulationManager.P = ...` is not classified. Today these are only `ImpactLedger` and `PlayerCountryId`, both setup.
5. **Writes on live objects:** mutations the UI makes on objects a declared read returns (for example the live list from `GetPendingCabinetDecisions`). None were found.
6. **Callers outside `Assets/Scripts/UI`:** `Assets/Scripts` holds only Data, Elections, Persistence, Simulation, Testing and UI, so there are none today.

**The declared reads:** of the 52 entries, I spot-checked about 20 for writes and found none: `DraftProposal`/`PreviewFormation` (they freeze the proposal object only), `ProjectConstructiveVote`, `ProjectPersonBallot`, `CentralBankHeadAwaitsPlayer`, `TryPlayerPollingDay`, `PendingPlayerScandal`, `QueuedScandalResponse`, `GetPending*`, `Estimate*` (throwaway clones), and the setup verbs, whose call sites at `GameController.cs:2186-2248` precede `ForkTheShadowAtTheStart`. I did not verify all 52.

### L6. ForkAnswersDiagnostic: coverage gaps, and no case passes vacuously.

**Not vacuous:**
- Case 2 depends on the Speaker asking S first at seed 777, but `PlayerAskedFirst` fails loudly ("NO - the case has no teeth").
- With no fork, `TheFork` returns null and the equality checks fail.
- A faulted fork logs an error, which `ArmLogFold` folds into a failure.

**Gaps:** no case exercises:
- a replay MISS and the switch to the AI's rule in an except-world;
- H1's one-sided wait;
- a load of an AI-mode world (M2);
- the docket identity (M1);
- a fault: no planted throw reaches `Faulted`, `RetireIfFailed` or `Unavailable`;
- `_savingAutomatically`.

**Case 5:** it is report-only. It has no cost threshold, and asserts only that all 13 families carry. Its line prints a hard-coded `turn 1` (`ForkAnswersDiagnostic.cs:427`), wrong if the guard at `:408` breaks later.

### L7. Small items.

- **A misattached doc comment.** `AutosaveIfDue`'s MM-2 `<summary>` now sits above the new field, so `_savingAutomatically` carries two summaries and `AutosaveIfDue` has none (`GameController.cs:1513-1519`).
- **A loosened log filter.** `OnOwnGenerator` sets `filterLogType = Warning` unconditionally (`ShadowBaseline.cs:215`), which would loosen a stricter filter a caller had set. Nothing sets one today. Use the stricter of the two.

## Suggested order

1. **H1 together with M3:** the rule on for every ledger world, run at the start of the next day, plus a one-sided-wait case.
2. **H2:** register both gates and record a bar.
3. **M1:** a docket identity that does not use `Number`.
4. **M2:** carry the flag and `Unavailable`. This becomes moot once H1 is fixed.
5. **M4:** surface faults.
6. **Then the L items.** L1 and L4 belong with the DECLARED ask to Elias about the AI's rule.
