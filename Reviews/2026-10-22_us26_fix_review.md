# Review - PS-6 US-26 fix round, rebase onto FORK-2, integration onto b198880e

**Reviewed:** the fix round `bdecca3d` (`git diff 512e7402 bdecca3d`); the rebase `064ce8fe..bdecca3d` onto `193d6771` (§890 FORK-2) against the original `45d4159c..46feb484`; the staged squash onto `b198880e` (§891 US-20), including the integrator's `Assets/Editor/CheckSuite.cs` line and the reworded registration sentence. Read-only: no file edited but this one, nothing committed, Unity not run. Line numbers are the staged tree's on 2026-10-22.

**Files reviewed:**
- `CLAUDE.md`
- `Reviews/2026-10-20_us26_review.md`
- `Assets/Scripts/Elections/UsPresidentialElection.cs` (`Count`, `StatesOnRecord`, `GameStatesBasis`, `StatesOf`, `TryCountedOnGameStates`, `PreviousCount`, `CollegeOf`, `StatesOn`, `OthersOnTheBallots`, `Hold`, `HoldHouseChoice`, `Contest`)
- `Assets/Scripts/Data/PartySystem.cs` (`TryHistory` with a country, `OwnCounts`, `OwnCountsAsListed`)
- `Assets/Scripts/Elections/LiveCampaignSetup.cs` (`TryFor`, `Germany`, `Sweden`, `HistoryPhrase`)
- `Assets/Scripts/Elections/NationalElection.cs` (`TryCompatibility`, `TryPredictShares`, `Run`)
- `Assets/Scripts/Elections/UsCongressElection.cs` (the `StatesOf` call in `Count`, doc crefs)
- `Assets/Scripts/Elections/CampaignAi.cs` (its draw sites)
- `Assets/Scripts/Elections/WorldClock.cs` (`ApplyStart`)
- `Assets/Scripts/Simulation/SimulationManager.PlayerElection.cs` (whole file)
- `Assets/Scripts/Simulation/SimulationManager.cs` (the six `played:` call sites, `HoldPresidentialRound`, `HoldUsPresidentialCount`, `RestoreCampaign`'s replay, `AssertReplayLandedOn`, `EpochScope`, `PreviewTurnOnClone`, `ClonePreviewCountry`'s list, `PassageYear`'s US-20 line)
- `Assets/Scripts/Simulation/ShadowBaseline.cs` (fork construction, `RecomputePollingDayToday`, the day tick's `HoldPlayerElection`)
- `Assets/Scripts/Simulation/EnergyMarket.cs` (`ResetCalibration`)
- `Assets/Scripts/Persistence/SaveGameService.cs` (`BuildSettings`, `CurrentSaveVersion` and its format history)
- `Assets/Scripts/UI/GameController.cs` (the election call sites after FORK-2)
- `Assets/Scripts/Testing/UiScreenshotDriver.cs` (the planted contest)
- `Assets/Scripts/Data/Country.cs` (`ElectionHistory`, `UsPresidentialElections`)
- `Assets/Editor/HistoryRolledForwardDiagnostic.cs` (whole file, staged)
- `Assets/Editor/UsPresidentialElectionDiagnostic.cs` (the fix hunk)
- `Assets/Editor/CheckSuite.cs` (cheap list and documents list)
- `Assets/Editor/ForkAnswersDiagnostic.cs` (summary and case 1)
- `docs/reference/US_ELECTIONS.md` (the two edited rows, the generated blocks' bounds)
- `docs/reference/PRESIDENTIAL_VOTE.md` (the edited row)
- `docs/specs/USA_STAGE_PLAN.md` (US-26, R-US11, R-US24)
- `COMPLETED.md` (§890 only)

## Findings (ranked)

No H. No M.

### L1 - The (d) "whatever order the list holds them in" case has no teeth against the code it replaced
- Where: `Assets/Editor/HistoryRolledForwardDiagnostic.cs:264-272`.
- What it does: moves the 2028 contest to the list's head and checks it re-derives the same states.
- Why it guards nothing: at `46feb484`, `StatesOf(Country, Contest)` found the previous contest with `PreviousCount`, which takes the latest earlier contest by date, whatever the list order. So this case passes on the pre-fix code as well.
- The hazard M2 actually named is different: a contest with an EARLIER date sitting between the two. For example, a planted contest dated 3 Nov 2026 next to the 2024 and 2028 counts. The old code takes it as 2028's previous; `StatesOn` gets null states for it and falls back to the record form, so 2028 re-derives off the states it was counted on. The new code reads 2024 from the basis and is right.
- Only the planted record-form case at `:275-281` would catch a revert of M2's fix.
- Fix: plant an intervening dated contest, assert that 2028 still re-derives on the game's 2024 states, and remove it in the `finally`. Or reword the case's claim to what it proves (order alone).

### L2 - No check calls the manager's election code, so a dropped `played:` would pass every bar
- Where:
  - The call sites: `Assets/Scripts/Simulation/SimulationManager.PlayerElection.cs:150` and `Assets/Scripts/Simulation/SimulationManager.cs:5659`, `:5688`, `:5784`, `:5836`, `:6067`, `:6089`.
  - The diagnostic's own copy of the count: `HistoryRolledForwardDiagnostic.cs:143-147`.
  - A stale doc line: `:301`.
- How the gap arises: this rebase moved the controller's `played: _playerCountry` into the manager by hand. It landed correctly (checked below), but no check calls that code:
  - (b) calls `NationalElection.TryPredictShares(..., country)` / `NationalElection.Run` / `ElectionHistory.Add` itself, and never calls `SimulationManager.RunPlayerElection`.
  - `ForkAnswersDiagnostic` case 1 crosses only Sweden's first polling day. Both worlds hold zero counts of their own there, so the fork equals the played world with or without `played:`.
  - The batch dump holds no player election.
- Failure: the next hand-move of these verbs (FORK-3, a partial-class split, a merge that picks the wrong side) loses `played: country` at `PlayerElection.cs:150`. Played worlds and forks then read the record's pair at every second election, and nothing fails. Only an audit trajectory run through a second election would show it.
- Also: the `Step` doc at `:301` still gives the controller's order as it was before FORK-2. The player's election (`HoldPlayerElection`) is missing from it.
- Fix: in (b), for one country:
  1. Hand a `SimulationManager` the world holding the first count, dated on the second polling day (`RestoreSaveState` takes a date).
  2. Call `RunPlayerElection()`.
  3. Assert that the record's shares equal `ownPrediction`.
  
  Then update the `Step` doc.

### L3 - The save-format declaration is adequate in substance, but it is in the wrong place and overstates what gets reported
- Where: the declaration at `Assets/Scripts/Elections/LiveCampaignSetup.cs:144-146`. The format history at `Assets/Scripts/Persistence/SaveGameService.cs:104-105`. `AssertReplayLandedOn` at `Assets/Scripts/Simulation/SimulationManager.cs:6113`.
- Where it lives: the declaration sits only in `TryFor`'s doc. The format history is where US-8 wrote its "NO STEP" answer. It still ends format 39's entry with "R-US11 (a) is US-26's to build, with its own step if it stores more", and it has no US-26 line. Format 47 took a STEP (refused under ruling A) for the very same class: a mid-campaign save that replays to a different campaign because the model changed under it. A reader of the format history therefore finds the question open, and finds the precedent pointing the other way.
- What it overstates: it says the replay "can land off the saved stream counts, which the replay's own assertion reports". `AssertReplayLandedOn` compares draw COUNTS only. The campaign's polls draw a fixed number of times per measure (`CampaignAi.cs:1049`, n respondents), and only some AI choices draw conditionally (`:691`). So the usual outcome is a different campaign replayed with matching counts, and no line is logged.
- Fix: add one sentence to the format history, e.g. "US-26 (2026-10-22): NO STEP - it stores nothing; a mid-campaign save cut by a build before it replays on the rolled history, DECLARED at `LiveCampaignSetup.TryFor`". Reword the tail to "...can land off the saved stream counts, which the assertion reports; where the counts happen to match, the different campaign is not reported".
- On whether a step is owed: no. US-26 adds no field and every count it reads is already saved. The only affected save:
  - is format 50,
  - was cut by a build between §886 and US-26's landing,
  - is mid-way through the game's SECOND campaign (Sweden or Germany, at least one game count before the polling day), which in Sweden is four in-game years in.
  
  Outside that window, a loaded save and a continuous run on this build are the same game. US presidential contests saved before US-26 now re-derive in their own record form (M2's fix), so the 2032 chain stays exact for them. If 47's precedent should bind, a step to 51 is Elias's call. Declaring is consistent with US-6c's DECLARED no-step at `UsPresidentialElection.cs:630`.

## Checked and clean

- **M1 closed by the integration.** The cheap list registers `HistoryRolledForwardDiagnostic` directly after `UsPresidentialElectionDiagnostic` (`CheckSuite.cs:302`). The summary now says "Registered in the cheap bar (CheckSuite)", which is true. The diagnostic owns no card block, so it rightly stays off the documents list (`:771-795`). Its statics match its siblings': it runs inside `EpochScope` (and `ApplyStart` is only `SetEpoch`), restores the RNG and the regional readout, and calls `EnergyMarket.ResetCalibration`/`ResetTurnState` as `CongressElectionDiagnostic` does. Condition: the comment cites §892, which must be written in the same commit (`COMPLETED.md` ends at §891).
- **M2 closed.**
  - The form is chosen from the contest's own basis.
  - The phrase is built from one constant in both places: `Count` concatenates `GameStatesBasis` into the format string, and `TryCountedOnGameStates` searches for the same constant. The concatenated format string is character-for-character the old literal. `GameStatesBasis` has no braces, and the date is `{1:yyyy-MM-dd}` in the invariant culture, read back with `TryParseExact` in the same format.
  - No record-form basis can contain the phrase. It is `UsPresidentialElection.DayBasis` + "; the states from {int}'s ..." + fixed suffixes, `DayBasis` itself does not contain it, and planted bases ("planted (PresidencyDiagnostic) ...") do not either.
  - Every game-form basis contains it once, after `DayBasis`. `Contest.DayBasis` is written only in `FromCollege`. `HoldHouseChoice` mutates the contest in place and never rewrites it. It is a public field serialized by Json.NET, so it survives saves and fork round trips. `PollingDay` is `.Date` with Unspecified kind, so the `==` lookup holds after `RoundtripKind`.
  - Count-time and re-derivation agree by induction. When `Hold` passes the previous count's states and `onBallots` is null, the basis names that day. `OthersOnTheBallots` is a pure function of year and pooled share, so re-derivation takes the same branch. When the previous states are null (a planted or tally-less previous), both sides use the record form.
  - Recursion stops because each step goes strictly earlier in date. Substring bounds are checked and no path throws.
  - On the first-count / no-own-count path, `Hold` → `Count(..., null, null)` is unchanged, and `StatesOf` → `StatesOnRecord` is the old one-argument call. No RNG, no new statics.
  - The planted record-form case (`:275-281`) does have teeth against `46feb484`.
- **L1 closed.**
  - `OwnCounts` sorts by `Day` with an index tiebreak, a total order, so the result is deterministic.
  - An undated record is skipped whenever `before` is given. Every game path dates its record (`NationalElection.Run`'s `date`, `PlayerElection.cs:98/155`).
  - `HistoryPhrase` reads the same sorted list that `TryHistory` reads.
  - With no own counts, `TryHistory` returns the old call early. Only an empty list is allocated, with no float expression and no draw.
- **L3 closed.**
  - `StatesOf(Contest)` became `StatesOnRecord`, whose doc says "THE RECORD FORM ONLY". Its only caller outside the class is a first count (`UsPresidentialElectionDiagnostic.cs:280`).
  - The remaining `cref="StatesOf"` / backticked `UsPresidentialElection.StatesOf` mentions in `Country.cs`, `UsCongressElection.cs`, `SaveGameService.cs`, `USA_STAGE_PLAN.md` and `US_ELECTIONS.md` now resolve to the one remaining (country) overload, so they are not ambiguous.
  - The card row names both methods, outside the generated blocks.
- **L4 closed.**
  - With no own counts, `HistoryPhrase` gives "Germany 2025 (SOURCED prior), loyalty derived from 2021->2025" and Sweden's equivalent with " (W-A1)". These are byte-equal to the old notes, and (a) asserts `n0 == n1`.
  - The rolled form uses no hole possessive. No reader other than (b) matches the note's words.
- **L2 declared.** See L3 above for where the declaration sits.
- **The rebase.**
  - Every hunk of the original carried over: the two presidential call sites, the four `TryFor` stagings and the controller's `TryPredictShares`. The controller's prediction is now `PlayerElection.cs:150` with `played: country`, where `country = _world?.GetCountry(id)`, the world being stepped.
  - `GameController.cs` no longer predicts or appends. It calls `RunPlayerElection` (`:6648`) and `SeatPlayerElection` (`:6384`).
  - All other staged blobs equal `bdecca3d`'s, except `CheckSuite.cs` and the one summary line of the diagnostic.
- **FORK-2 (a).**
  - `ShadowBaseline` builds each fork by a save round trip (`CreateSaveGame` → `Deserialize` → `RestoreInto`) and from carried saves. Each fork's `Country` therefore owns its `ElectionHistory` and `UsCongressElections`.
  - The fork's `HoldPlayerElection` (day tick, `ShadowBaseline.cs:318`) and its campaign stagings read `_world`, which is the fork's own.
  - `ForkAnswersDiagnostic` case 1 crosses a first election with zero own counts on both sides, so it stays equal.
- **FORK-2 (b).** No fork is built on `ClonePreviewCountry`; only `PreviewTurnOnClone` and the draft previews use it. `PreviewTurnOnClone` runs one boundary and holds no election. None of the `played:` sites can be handed a clone, since all read `_world`. So the shared `UsPresidentialElections`/`UsCongressElections` references and the clone's empty `ElectionHistory` are never written or read by US-26's paths.
- **FORK-2 (c), the date gate in the manager's order.**
  - The append is now `PlayerElection.cs:166` (also `:104` and `:160`, both `NotImplemented` and skipped by `OwnCounts`). It comes after the prediction at `:149`.
  - The US House count is appended by `UsCongressElection.Hold` after `HoldUsPresidentialCount`'s prediction.
  - In both cases `>=` excludes the day's own count, so order within the day does not matter: true in played worlds and in forks.
  - A fork made on the polling day after the count inherits that day's record. `HoldPlayerElection`'s once-a-day guard skips it, and a mid-campaign replay on that day still excludes it by `>=`, so the replay matches.
  - `RestoreInto` restores the world before `RestoreCampaign` re-stages, so the replay reads the saved history.
- **The integration.**
  - The staged `SimulationManager.cs` diff against `b198880e` has changed lines byte-identical to `git diff 193d6771 bdecca3d` (12 lines compared).
  - US-20's `WorldClock.StatutePartsAreActs(countryId)` predicate stands at `SimulationManager.cs:6707`.
  - The staged diff carries no CR.
  - The edited `US_ELECTIONS.md` rows sit above the US-8 generated block. The fix touched only `Check` texts in `UsPresidentialElectionDiagnostic`, not `card.Add`.

## Verdict

The fix round closes all six findings of the first review:
- M2 by construction: the stored basis names the form, and the phrase can neither misfire nor go missing.
- L1 with a deterministic date order and undated records never read before a day.
- L3 by the rename.
- L4 byte-inert on the no-count path.
- M1 by the integrator's registration.

On the path with no own counts, nothing changed: the same calls, arguments and strings, no draw, no static and no new float expression. The rebase onto FORK-2 kept every `played:` argument, and each now resolves to the world being stepped, so forks read their own counts. The preview clone cannot reach any path that rolls history. The integration onto `b198880e` is the branch's own change with US-20's line intact. No format step is owed.

What remains is low. One (d) case proves less than its claim. Nothing in the bar calls the manager code that now carries `played:`. The no-step declaration should also get a line in the format history, with its "reports" softened. None of these blocks the commit: land it with §892 written, and fix L1-L3 in the next pass that touches the diagnostic.
