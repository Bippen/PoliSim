# US-27 review: the midterms - every even year's polling day, R-US20 (b)'s fitted term, the 120th Congress seated; save format 52

**Reviewed:** branch `wave/us27` (worktree `G:/UNITY/Projects/PoliSim-wt/us27`), `git diff cfaf166b..986afc63` (two commits), by one adversarial reviewer, refute-first, read-only (nothing run in Unity). A survey of every Editor check the calendar change could move was made first and read beside it.

**Files reviewed:** `Assets/Scripts/Elections/WorldClock.cs`, `UsCongressElection.cs`, `UsPresidentialElection.cs`, `UsTermLimit.cs`, `LiveCampaignSetup.cs`, `NationalElection.cs`, `Generated/UsMidtermTerm.cs`; `Assets/Scripts/Simulation/SimulationManager.cs` (`HoldUsPresidentialCount`, `HoldUsMidtermCount`, `CurrentCampaignCalendar`), `SimulationManager.PlayerElection.cs` (`RunPlayerElectionNow`); `Assets/Scripts/Persistence/SaveGameService.cs`; `Assets/Scripts/UI/GameController.cs`, `GameController.ParliamentRows.cs`; `Assets/Scripts/Data/PartySystem.cs` (`OwnCountsAsListed`); `Tools/us_midterm_fit.pl`; `Assets/Editor/UsMidtermFitCheck.cs`, `UsMidtermDiagnostic.cs`, `PollingDayDiagnostic.cs`, `UsMidtermCheck.cs`, `CongressElectionDiagnostic.cs`.

## Findings

No H findings.

- **M1 - the two new checks are not registered.** By the implementer rules `CheckSuite.cs` is the integrator's; the registration lines are handed back as drafts. Not a code change.
- **M2 - the term carries into the next count's prior and base.** `OwnCountsAsListed` and `CountHouse` read the contest's `V`, the share with the term applied, so the 2028 prediction's history and the 2028 House base start from the midterm's result. Internally consistent (the stored state shares include the term, and `VBase` matches them), and it mirrors the record, whose 2022 midterm is 2024's "latest". The fitted mean is a whole-record swing that already holds the economy's part, and the live economic vote is applied as well - a possible partial double count. DECLARED in the hand-back and ASKED of Elias; not changed.
- **L1 - fixed.** `CurrentCampaignCalendar` read `PlayerCountryId.Value` unguarded; guarded.
- **L2 - fixed.** The ELECTION log line on a US polling day tested any Congress count on or before the day; it now tests a count of that day.
- **L3 - fixed in part.** The president's slip names the next presidential polling day as such; the "not yet certified" line is shown only for a polling day the record's calendar holds (the 2026 midterm), never for a later game election. The Congress row's NEXT VOTE chip widens the row: owed to the film.
- **L4 - stated.** `UsMidtermDiagnostic` (b)'s no-campaign flag would fail spuriously if the 2024 run-up ran and its campaign failed to stage (the run-up state is dropped only with a campaign); it passes while US-30 stages the campaign.
- **L5 - stated.** The FORK-2 parity (d) relies on `RestoreInto` restoring the streams' draw counts (it does) and on no unsaved static moving the count (the count draws nothing; `NoiseSigmaPp` is 0) - the same pattern the other FORK checks hold.
- **L6 - stated.** A US world opened before 8 Nov 2022 would now hold a 2022 midterm; no start offers one.

**Verified:** every caller of the polling day (presidential-only readers moved to `TryNextPresidentialDay`; the Riksdag and Bundestag readers untouched); the term's sign on each party; `MidtermNational`'s order (DEM, REP, others) and `FromGameStates` / `FromNationalShares`'s arguments against `CollegeOf`; the perl tool and the C# check computing the same fit (digest over LF text `ba0ca90a...`, SECONDARY signing, unified/divided, the Clerk's swing, Student's t); the diagnostics' dates and weekdays; non-US play paths and a US world before 3 Nov 2026 numerically unchanged; the format-52 comment accurate.
