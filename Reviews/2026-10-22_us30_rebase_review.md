# US-30 rebase review: the USA's campaign on the history rolled forward, and save format 51

**Reviewed:** branch `wave/us30` (worktree `G:/UNITY/Projects/PoliSim-wt/us30`), the rebase only. I compared the old range `45d4159c..0f4865fe` with the rebased range `0244e796..7092ce2b` using `git range-diff`. That showed changes in commit 2 only (e52a733b: the `HoldUsPresidentialCount` conflict, and `played` threaded into `TryUsa`) and one new commit (7092ce2b: `UsCampaignDiagnostic` (h)/(j), and `CurrentSaveVersion = 51`). I read all of `git diff 0244e796 7092ce2b` for the code files. I read the US-26/FORK-2 code the rebase composes with at its current text. Read-only: nothing was run in Unity.

**Files reviewed:**
- `Assets/Scripts/Simulation/SimulationManager.cs` (`HoldUsPresidentialCount`, `TryPlayerCampaignShares`, `AdvancePlayerCampaign`, `AdvancePreCampaign`, `RestoreCampaign`, `ClonePreviewCountry`, `Answered` call sites)
- `Assets/Scripts/Elections/LiveCampaignSetup.cs` (`TryFor`, `TryUsa`, `HistoryPhrase`)
- `Assets/Scripts/Elections/NationalElection.cs` (`TryCompatibility`, `TryPredictShares`)
- `Assets/Scripts/Elections/UsPresidentialElection.cs` (`StatesOf`, `PreviousCount`, `Hold`, `CountedPoll`, `IsPollingDay`)
- `Assets/Scripts/Data/PartySystem.cs` (`TryHistory` with `played`, `OwnCounts`, `OwnCountsAsListed`)
- `Assets/Scripts/Persistence/SaveGameService.cs` (`CurrentSaveVersion`, `Deserialize`, `RestoreInto`)
- `Assets/Scripts/Persistence/SaveGame.cs` (`ImpactLedgerWorlds`, `NoPolicyShadow`)
- `Assets/Scripts/Simulation/ShadowBaseline.cs` (`FromCarried`, the log filter in counterfactual steps)
- `Assets/Scripts/Simulation/PolicyImpactLedger.cs` (`FromCarried`)
- `Assets/Editor/UsCampaignDiagnostic.cs`
- `Assets/Editor/UsOwnNomineeDiagnostic.cs`
- `Assets/Editor/UsPresidentialElectionDiagnostic.cs`
- `Assets/Editor/PreStartRecordDiagnostic.cs`
- `Assets/Editor/HistoryRolledForwardDiagnostic.cs`
- `Assets/Editor/TermLimitDiagnostic.cs`
- `Assets/Editor/SaveLoadRoundTripDiagnostic.cs`
- `Assets/Editor/PlayProtocolStaging.cs`
- `Assets/Editor/PlayProtocolCheck.cs`
- `Assets/Editor/UiLeverVerbCheck.cs`
- `CLAUDE.md`, `COMPLETED.md` §890 only

## Findings

No H and no M findings.

**L1. `TryUsa`'s staging note reads `OwnCounts(played)` without the country guard that `TryHistory` applies.**
- Where: `Assets/Scripts/Elections/LiveCampaignSetup.cs:297`.
- `PartySystems.TryHistory` (`Assets/Scripts/Data/PartySystem.cs:603`) reads own counts only when `played.Id == id`. The note's `PartySystems.OwnCounts(played, cal.ElectionDate)` checks nothing.
- `TryUsa` is public. Called directly with a non-US `played` (for example a harness passing Sweden), the prior would be the record's pair, as it should be. The note would still say "on the history rolled forward - the game's House count(s) of …", read from Sweden's `ElectionHistory`. That is a false staging note.
- `TryFor` dispatches on the country, so no game path can reach this today. It is latent.
- Fix: `PartySystems.OwnCounts(played?.Id == CountryId.USA ? played : null, cal.ElectionDate)`. Or take the count from `TryHistory`'s `ownCounts`, as `HistoryPhrase` does for Sweden and Germany.

**L2. (j) cannot tell "before = the polling day" from "before = the staging day", and no check counts a campaign on top of a previous game count.**
- Where: `Assets/Editor/UsCampaignDiagnostic.cs:271-295`.
- The game's only US own count is the 2024 House on 2024-11-05. Any `before` between 2024-11-06 and 2028-11-07 reads the same history. So (j) would pass if `TryUsa` were changed to stage on `cal.PreCampaignStart` or `cal.CampaignStart`. The one property the composition rests on (the same `before` as the polling-day prediction) has no check that fails without it.
- Also, (h)'s new `PreviousCount(usa, today)` is null at 2024. So the campaign-path count with the game's own previous states (`Hold` → `StatesOf(country, previous)`) never runs under any check.
- Today the gap is harmless. `UsCongressElection.Hold` runs only from `HoldUsPresidentialCount` on a presidential polling day, and midterms are US-27's. So no own count can land inside a window.
- Fix:
  - Make (j) discriminating: plant a `UsCongressElection.Contest` dated inside the 2028 window (after `cal2028.PreCampaignStart`, before the polling day). Assert that the staging and the polling-day prediction both read it, the same way.
  - Optionally, step the (h) game through 2028-11-07 so that one count runs with both a campaign and a previous game count.

**L3. US-26's inertness section does not cover the USA's staging.**
- Where: `Assets/Editor/HistoryRolledForwardDiagnostic.cs:114`.
- (a) asserts that staging with `played` and no own count equals the unplayed staging, bit for bit, note included. It only does this for `id == CountryId.Sweden || id == CountryId.Germany`. That was right while `TryFor(USA)` returned false. Since this rebase the USA stages, and the game stages it with `played` on every path: `AdvancePreCampaign`, the campaign's opening, and `RestoreCampaign`.
- By reading the code, `TryHistory` returns the very arrays of the record's pair when `own.Count == 0`, and the note's `rolled` is empty. So it holds. But CLAUDE.md's NUMERIC INERTNESS rule says inertness is proved by measurement, never by reading.
- Fix: add `CountryId.USA` to the branch at line 114. Its note equality then also covers the `rolled` suffix.

**L4. Two stale comment claims.**
- `Assets/Editor/PolishPresidentialStartDiagnostic.cs:41` says the oath save is "format 50". It is now cut at `CurrentSaveVersion` (51). The checks at :239 and :286 read the constant, so only the comment is wrong. Under the claim convention it should be REFERENCED, for example "at this build's format".
- `Assets/Editor/UsCampaignDiagnostic.cs:30-32`: the class summary lists "(j) … ; (i) …" in that order, while the code runs (h), (j), (i). Reorder the summary or rename the section letters.

## Checked and clean

- **(1) The polling-day order.**
  - `TryPlayerCampaignShares` is tried first. It requires `PlayerCampaignResult`, `PlayerCampaign` and `CampaignRecord` with `CampaignRecord.ElectionDate == CurrentDate`.
  - All of these are instance fields of the manager stepping the day. A ledger fork or the no-policy shadow has its own manager. `PlayerCampaignResult` is nulled at every begin and every restore. So the run read is always this world's run for today's polling day, never another cycle's or another world's.
  - The fallback still passes `played: country` (`SimulationManager.cs:5696`), the same call as US-26's on main. With no campaign, the expression is identical to main's, so the no-campaign path is unchanged.
  - The presidency (`UsPresidentialElection.Hold` → `CountedPoll(country, date, poll)`) and Congress (`UsCongressElection.Hold(..., CountedPoll(country, CurrentDate, poll), held, ...)`) read the same `poll`, whichever branch made it. `Hold` takes `PreviousCount`/`StatesOf(country, previous)` downstream on both branches.
- **(2) The staging's prior is the prediction's prior.**
  - `TryUsa` calls `NationalElection.TryCompatibility(USA, ..., played, cal.ElectionDate)` (`LiveCampaignSetup.cs:260`).
  - `TryPredictShares(..., on: CurrentDate, played: country)` calls `TryCompatibility(..., played, on)` (`NationalElection.cs:231`). On the count day, `on` is the polling day, which equals `CampaignRecord.ElectionDate`.
  - All three game stagings pass `calendar.ElectionDate` as `before`: the opening (`SimulationManager.cs:5804`), the run-up (`:5858`) and the restore (`:6089`, `:6109`, with `FromWorldStart(record.ElectionDate, EpochDate)`). Each also passes `_world.GetCountry(PlayerCountryId)`.
  - `OwnCounts` drops counts on or after `before` (`>=`), so the polling day's own Congress count is never read by the election that makes it.
  - No own US count can land between a window's opening and its polling day (see L2).
  - `RestoreInto` restores the world (`RestoreSaveState`) before `RestoreCampaign` (`SaveGameService.cs:310-313`). So a load or a ledger fork restages on the restored world's own `UsCongressElections` (saved since format 49) and replays onto the saved stream.
  - `ClonePreviewCountry` shares the US contest lists by reference, but the preview turn never calls `LiveCampaignSetup`. No `TryFor` call site passes anything other than the manager's own `_world` country.
- **FORK-2 (§890).**
  - `QueueCampaignDecision` and `ClearCampaignQueue` report through `Answered` (`SimulationManager.cs:6023`, `:6058`), and so does the scandal answer (`:5992`). The ledger replays them in every except-world, so a fork with no policy difference steps the same US campaign on the same restaged prior.
  - The new public `TryPlayerCampaignShares` is not called from `Assets/Scripts/UI`, so `UiLeverVerbCheck` (4) is unaffected.
  - The new "PRESIDENTIAL: … the campaign ran to the polling day" line is an info line. `ShadowBaseline` raises `filterLogType` to Warning during counterfactual steps, so forks do not print it. Its only reader is `UsPresidentialElectionDiagnostic:315`, in its own played world.
  - The check at `:350` counts `"PRESIDENTIAL: USA - the count of "` lines and `TermLimitDiagnostic:303` matches `"the count of 2028-11-07"`. Neither matches the new line.
- **(4) Format 51.**
  - `SaveGameService.Deserialize` is the only `DeserializeObject<SaveGame>` in `Assets`, and it refuses any `version != CurrentSaveVersion` before deserializing. So the saves screen, autosave, `LoadFromFile`, `ShadowBaseline.FromCarried` (the no-policy world) and `PolicyImpactLedger.FromCarried` (each carried family world) are all gated.
  - A v50 outer save is refused before its carried worlds are read. A v51 save's carried worlds are written by `Carry` in the same build at 51.
  - The message is the existing ruling-A sentence, unchanged.
  - Every Editor format pin reads the constant. Spot-checked: `SaveLoadRoundTripDiagnostic:744` (tamper +1), `PlayProtocolStaging:102`, and the "−1 is refused" probes in Congress/EventSalience/Presidency/Senate/UsBudgetProcedure. No committed fixture save carries a `SaveVersion`.
  - `PlayProtocolStaging` writes to `SaveGameService.DefaultSaveDirectory` (the desk, not the repo). `PlayProtocolCheck` cuts fresh in `temporaryCachePath`. So no bar fails at 51, but the desk's sitting saves owe a re-stage, as at every step (tracking for the record, not a defect).
  - The step is conservative under ruling A, matching §881's step 47. It refuses non-US v50 saves too, which the global gate implies. It is put to Elias, as the comment says.
- **Byte-inertness.** `HoldUsPresidentialCount` returns unless a player country that `ElectsIn` exists, so the no-policy dump never reaches it. `TryFor`'s change only threads `played` into the USA branch. No new RNG draw is added on any non-US path or any no-campaign path. The `SaveVersion` integer stays the same width in the JSON.
- **Static guards.**
  - Backticked `Type.Member` in the diff: `LiveCampaignSetup.TryUsa`, `NationalElection.TryCompatibility`, `CampaignCalendar.Sweden2026` (in `CampaignClock.cs`). All exist.
  - No possessive is built on a value in the new strings ("the USA's", "the game's", "the record's" are literals).
  - No new constants.
  - Every changed `.cs` file is CRLF throughout (CR count equals line count).
- **The diagnostics.**
  - (h)'s recount passes `PreviousCount(usa, today)` and `StatesOf(usa, previous)` as `Hold` does.
  - (j) compares `Setup.PriorShares` and `LoyaltyPerParty` (stored as-is, `CampaignRun.cs:168`) bit-exact against `TryCompatibility(USA, ..., usa, 2028)`. It also checks the moved prior, the note's words, and the idle run against `TryPredictShares(USA, …, null, 2028, usa)` within 1e-12. The arguments are in the right positions.
  - `UsOwnNomineeDiagnostic` (e)/(g): the stripped save keeps format 51, so it passes the gate. `PlayerCampaign = null` is the first review's M1 fix.

## Verdict

**SHIP.** The conflict resolution is correct. The campaign's shares come first, and only this world's run for today's polling day is used. The fallback still reads `played:`, and the presidency and Congress share one poll. The US-26 composition stages on exactly the prediction's `(played, polling day)` on every path, including a load and a fork's restore. Format 51's refusal reaches every load path, including the ledger's carried worlds. L1–L4 are a latent false note, two coverage gaps (one of them the proof that the composition is inert) and two stale comments. All are cheap to fix, and none blocks. The desk's play saves owe a re-stage at 51, and the conservative step is for Elias to rule.
