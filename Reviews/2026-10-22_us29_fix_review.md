# Review: the US-29 fix round (the Fed chair by statute, answering the review of 2026-10-22)

**Reviewed:** branch `wave/us29`, `c63902a4..b399da17` (worktree `G:/UNITY/Projects/PoliSim-wt/us29`). Read-only. No Unity run, no dump, no bar. It answers `Reviews/2026-10-22_us29_review.md`, which this review does not redo.

**Files reviewed:**
- `CLAUDE.md`
- `Reviews/2026-10-22_us29_review.md`
- `Assets/Editor/FedChairConsentDiagnostic.cs`
- `Assets/Scripts/Simulation/SimulationManager.cs` (PlayerGoverns, PlayerMayIntroduce, AppointCentralBankHead, PlayerAppointsCentralBankHead, CentralBankHeadAwaitsPlayer, AppointCentralBankHeadsWhoseTermsEnded, ConsentsToAiNominee, the AdvanceDay call site)
- `Assets/Scripts/Simulation/UsFedConsent.cs`
- `Assets/Scripts/Simulation/CentralBankTerms.cs` (TermEnds, Ended, the Fed row)
- `Assets/Scripts/Testing/UiScreenshotDriver.cs` (CaptureHeldState, CaptureFedHoldover, CaptureDeskUsBudget, StageUsVetoDivision, SwappedSeats, Settle, the session's flow around CaptureHeldState)
- `Assets/Scripts/UI/GameController.cs` (Update's gate, UpdateFedChairSelectionState, PlayerFedChairBarred, PlayerFedChairBarredWords, DrawRiksbankPage's lever, DeclarationHolds, FormationHolds)
- `Assets/Scripts/UI/GameController.PoliticsV35.cs` (the bank page's dispatch, the governor block and its slip)
- `Assets/Scripts/UI/GameController.DocketV35.cs`
- `Assets/Scripts/UI/GameController.Campaign.cs` (HasPendingCampaignOpening, HasPendingScandalAnswer)
- `Assets/Scripts/UI/UiFormat.cs` (Number, Seats, Possessive)
- `Assets/Scripts/Elections/UsSenate.cs` (On and its memo)
- `Assets/Scripts/Elections/WorldClock.cs` (SecondChamberOfRecord)
- `Assets/Scripts/Elections/UsPresidentialElection.cs` (OathDayAfter)
- `Assets/Scripts/Data/Country.cs` (SecondChamberSeats, PlayerHoldsPresidency)
- `Assets/Editor/MetaTextCheck.cs`
- `Assets/Editor/CommentClaimCheck.cs`
- `Assets/Editor/UiLeverVerbCheck.cs`
- `Assets/Editor/ChamberVerdictCacheCheck.cs`
- `Assets/Editor/NoPolicyCentury.cs`
- `Assets/Editor/TrajectoryBaselineDump.cs`
- `ElectionsData/usa/raw/fed/fetch_log.txt` (the NOTE)

## Findings

No H. No M. Each of M1-M4 and L1-L4 is closed. What follows are seven L findings: three new edges in the film staging, one gap in the log-day fix, one duplicated rule, and two items still owed.

### L1. The film's "hold did not return" guard checks only the values it just wrote back
`Assets/Scripts/Testing/UiScreenshotDriver.cs:4011-4014`.

After the `finally`, the guard compares three things: the candidates' count with the snapshot, the speed with the snapshot, and the party with `mine`. The `finally` has just written every one of them back. One `Settle` runs in between, and only `UpdateFedChairSelectionState` could change the candidates in it. So the guard catches almost nothing, and it does not check the invariant the summary claims ("No day passes", "a hold that does not come back fails the run"). It inherits this from the 93a-93c guard at `:2588-2591`.

*Failure scenario:* a later edit makes a staged frame tick a day, for example by putting a `Settle` before the timer is set, or by a gate that skips the timer. The sim's date or the Fed hold then differs after the staging, and every later frame in the US session (the saves menu, the state pins, the Desk by role) is filmed on another day. The guard stays green.

*Fix:* before the `try`, record `bool holdWas = sim.CentralBankHeadAwaitsPlayer(player.Id)`. After the closing `Settle`, also assert `sim.CurrentDate == day`, `sim.CentralBankHeadAwaitsPlayer(player.Id) == holdWas` and `ReferenceEquals(player.SecondChamberSeats, senate0)`.

### L2. `_onDesk` and `_docketOpenItem` are written and never restored
`UiScreenshotDriver.cs:3948` (`_onDesk` set false) and `:3992` (`_docketOpenItem` set null). The summary says the tab and everything else are put back.

Today this changes no pixels. The tab sweep leaves `_onDesk` false (`:720`), and the earlier frames leave `_docketOpenItem` null (`:918`, `:3310`). So both values are already what the staging writes.

*Failure scenario:* a reorder that reaches `CaptureHeldState` with the Desk up, or with a Docket item open. Every later frame in the session then films a different screen under its own name.

*Fix:* snapshot both fields beside `tabWas` and `categoryWas`, and restore them in the `finally`.

### L3. The staging cannot turn over an equal division
`UiScreenshotDriver.cs:3943-3946`. `SwappedSeats` exchanges the two parties' counts, so a 50-50 Senate stays 50-50. If the Vice President is of the president's party, the count still confirms after the swap. The 90h guard at `:3955` then fails, `_failed++` runs, and all four frames are MISSING (the film goes red).

The film is deterministic on its seed, so this either never happens or fails loudly. It is never silent. It is reachable because the game elects the 2026 and 2028 Senate classes before the seeded chair's term ends.

*Fix:* where the swapped count still confirms, move one more seat from the president's party to the other, and say so in the log line.

### L4. The holdover frames are skipped silently when the term's end is the AI's and is confirmed
`UiScreenshotDriver.cs:3849-3870`. Take a US session whose player is not the president at the term's end, for example under `-shotparty=` the other party. The AI president's nominee is confirmed, a new chair is seated, the search runs to `MaxHeldSearchDays`, and the session ends with the warning at `:3869`. None of 90h/90i is captured and nothing is counted. M3 is met for the default USA session (the largest party is the president's), but not for every US session.

*Fix:* in that branch, where `UsFedConsent.Applies(held)`, either log the four frames as MISSING with `_failed++`, or stage them on the new chair by planting its `TermEnds` to the day (and restoring it in the same `finally`).

### L5. The 3 January log day does not cover the case its comment names
`Assets/Scripts/Simulation/SimulationManager.cs:3882`, with its comment at `:3873-3877`.

The comment gives "a player-president leaves office inside a holdover" as the reason for the second log day. But office changes on the oath day, 20 January (`UsPresidentialElection.OathDayAfter`, `UsPresidentialElection.cs:134`), not on 3 January. A refusal of the incoming AI president's nominee that begins on 20 January stays silent until the next 3 January, about eleven months later.

In practice the gap is small. Under a two-party Senate, an incoming president of the other party is nearly always confirmed, and confirmations always log. The line is only a log, so it is byte-inert either way.

*Fix:* add `|| (CurrentDate.Month == 1 && CurrentDate.Day == 20)` to `logDay`, or reword the comment to the cases 3 January does cover (a new Congress, a save cut inside a holdover).

### L6. The barred state re-derives the manager's rule, and rebuilds the holdover's date by hand
`Assets/Scripts/UI/GameController.cs:3848-3857` and `:4174`.

`PlayerFedChairBarred` builds its own gate from `PlayerMayIntroduce`, `CentralBankTerms.Ended` and `Vote`. The manager already has that gate (`PlayerAppointsCentralBankHead`, plus the consent clause of `CentralBankHeadAwaitsPlayer`). The two agree in the played world today: the UI world is never counterfactual, and the chair is non-null at both call sites. But this is the same two-sources-of-truth shape that the first review's L4 removed from the Docket. The lever also re-types `HoldoverWords`' `"THE TERM ENDED " + d MMM yyyy`.

*Fix:* gate on `_simulationManager.PlayerAppointsCentralBankHead(_playerCountry)`. Check first that `UiLeverVerbCheck`'s (4) declared list classes it as a read; add it there if not. Move the term-ended prefix into a `UsFedConsent` helper that both strings call.

### L7. Still owed from the first review (tracking, not a defect of this commit)
- M4's first bullet is not in the commit, and the diagnostic now says so itself ("The dump diff is still the proof"). It asks for the dump on both seeds, diffed against `aloopsal2` with `Tools/traj_diff.pl` and recorded byte-identical, plus the `BaselineLabel` line.
- L5 (the plan's "2013 and 2017 precedents, read") is not in the commit. It is a record line, so it can ride the item's section.

## Checked and clean

- **M1 closed.** No player-facing string cites 12 U.S.C. 242 for the holdover any more: `HoldoverWords`, `RefusedWords`, the governor block, the slip and the manager's summary are all clear. The one citation left (`CentralBankTerms.cs:53`) is the four-year term's, which is correct. The class note now states the reading as DECLARED, quotes the members' sentence, and names the section 244 question as asked.
- **M2 closed, and the gating is exact.** `PlayerFedChairBarred` is non-null only when all of these hold:
  - the country is the USA;
  - no candidates are drawn;
  - the term has ended;
  - the player governs (`PlayerMayIntroduce`, the same gate that `PlayerAppointsCentralBankHead` uses);
  - the day's count exists and refuses.

  It is null for the AI president's refusal (a real count, still worded "THE SENATE REFUSES"), for a hold, for a term not ended, and for a null count. The lever, the state line, the governor block, the slip and the Docket all switch on it, so "THE SENATE REFUSES" never stands beside "NO NOMINEE" for the player-president. NOMINATE stays shut (`GUI.enabled = pending`). For the president the role gate means the player's party, so "your party" is right, and `For` is that party's seats as the seated Senate holds them (caucus-mapped through `UsSenate`'s sides in `SecondChamberOfRecord`).
- **The Docket item does not hold the clock.** The clock's hold comes from `UpdateFedChairSelectionState` in `Update`'s gate, and that is false in the barred state. `holdsTime: false` only sets the stamp. The item does not set `anyPending`, matching the minister-alert and majority items. No other counter of pending items exists in `Assets/Scripts/UI`. The banner's flag (`hasPendingFedChairSelection`) is false as well.
- **Text.** Numbers go through `UiFormat.Seats` / `UiFormat.Number`, and the possessive through `UiFormat.Possessive`. The new literals carry no MetaTextCheck-banned token. The bank is named through `GetCentralBankName`, not by literal (the widened "FEDERAL RESERVE" rule). No developer-facing text reaches a player surface. Comments are stripped before the scan, so the note's "section 244" is not a hit.
- **Per-frame cost.** At most three extra `Vote` calls per OnGUI event, and only in the holdover state. Each is one `Congress.PresidentOn` plus a sum over the seated sides, or `UsSenate.On`'s per-day memo. The Docket's call short-circuits on `Applies`, then on candidates and `Ended`, before it counts.
- **CaptureFedHoldover's restore** covers every exit path that mutates state:
  - The two early returns come before any write.
  - Every mutation sits inside `try/finally`, which puts back the party, the Senate (by reference), the candidates, their term key, the speed, the timer, the tab and the category, then resets the scrolls.
  - No `yield break` sits inside the `try`.
  - An exception in its own `MoveNext` runs the `finally`.
- **The staging and the simulation.** The clock is held: the timer is set to `-ClockHoldSeconds` (3600 s) before the first swap, and no `AdvanceDay` is called. In neither staged state does `UpdateFedChairSelectionState` draw candidates (90h: the role gate is false, so the candidates are nulled and then restored; 90i: no hold), so the counted FederalReserve stream is not drawn. After the restore the term key matches, so nothing is drawn again. Nothing is written into the simulation: no division, no contest, no log entry in the country's own record, no approval or confidence change. `UsSenate`'s memo is keyed on the record's day, not on the world's seats. The UI's `ChamberVerdicts` cache drops its entries when the seats and the party change, and the restore puts back the same objects. Other countries' sessions are unaffected: both call sites are gated on `UsFedConsent.Applies`.
- **Simulation inertness, by reading** (the dump is still the proof, L7). The SimulationManager change gates only `Debug.Log` / `Debug.LogError`. The UsFedConsent changes are string literals and comments. Non-US worlds never reach `ConsentsToAiNominee`. A US world with no ended term never calls it. The 3 January line fires only while a term has ended and is unconfirmed (or with a confirmation, which logs anyway): at most once a year per holdover per world, and never on a non-holdover.
- **The diagnostic.**
  - **(f) has teeth.** A null or refusing count on any term day breaks the loop and fails the `Check` by name, with its date and count. The loop steps by `CentralBankTerms.TermEnds(Fed, day)` (four years from a confirmation on the term's day, which is what `Seat` does) up to `EpochDate + NoPolicyCentury.Years`. That is about 25 counts per start, with no century stepped. Each start runs under its own nested `EpochScope` with the seed re-seeded. The outer `finally` restores the seed, the draw counts and `EnergyMarket`, and destroys the hosts. `TrajectoryBaselineDump` runs Sweden's start with no player, so (f)'s every-start, no-player sweep covers it. `NoPolicyCentury` for the USA has a US player who appoints, so the AI path never runs there.
  - **(e2) has teeth.** A role gate that leaked would rise the hold or refuse with Senate words. Its world opens inside the outer `EpochScope`. The check is narrowed to "THE SENATE REFUSES" / "THE SENATE CONFIRMS", which is right, because `Congress.IntroducesWords` may name the Senate.
- **Static guards.**
  - **CommentClaimCheck:** every backticked `Type.Member` exists (`SimulationManager.PlayerMayIntroduce`). The others are bare types, paths or frame names, which the regex does not read. The transcribed tallies are gone, and the fetch log's NOTE and (a) hold them.
  - **ConstantProvenanceCheck:** the diff adds no const under `Assets/Scripts/Simulation`.
  - **UnwiredSubsystemCheck:** the diff adds no public member.
  - **UiLeverVerbCheck:** the UI's only new manager call, `PlayerMayIntroduce`, is already called from UI files.
  - **ChamberVerdictCacheCheck:** not affected.
- **Line endings.** All seven edited files are CRLF-only at `b399da17`: every line, the same style as before.

## Verdict

**MERGE.** No H or M. The fix round closes M1-M4 and L1-L4 as the integrator ruled them. The new code is gated correctly, restores what changes the film, writes nothing into the simulation, and is inert by reading. L1-L4 above are cheap hardening of the film staging and can ride the next US commit. L5 and L6 are one-liners. The merge still owes what the first review asked for and this commit does not claim (L7): the trajectory dump on both seeds, diffed byte-identical against `aloopsal2`, recorded with the `BaselineLabel` line.
