# Review 2: wave/form, the delta since the first review (K79, K80, K87, the review fixes)

**Branch:** `wave/form` in `G:/UNITY/Projects/PoliSim-wt/form`. The range is `ace6a9e0..b541e686`, three commits: `2a0d60aa` (K79), `20491d41` (review fixes + K80 + K87's MP/four/L2 parts) and `b541e686` (K87's empty chair). The first review is `Reviews/2026-10-23_form_review.md` (`9c74baea`).
**Reviewer:** independent and read-only. No Unity was run and nothing was pushed. The compile gate was run (`GATE OK`), and so was `Tools/textcheck`: 4 of 5 pass, and UpstreamCheck fails only because this worktree branch has no upstream, which is the environment, not the delta. One dotnet probe was run (`G:/Claude/tmp/claude/C--Users-elias/1fd816b7-8c12-47d8-ae29-269db5403b26/scratchpad/form_review2/playedday/`). The two SVT pages were read with perl (tags stripped). An in-memory `git merge-tree` against main `784f93b2` was also run.
**Files reviewed:** `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/Simulation/SimulationManager.PlayerPresident.cs`, `Assets/Scripts/Simulation/CabinetSystem.cs`, `Assets/Scripts/Simulation/FederalReserveSystem.cs`, `Assets/Scripts/Simulation/StanceModel.cs`, `Assets/Scripts/Elections/NationalElection.cs`, `Assets/Scripts/Data/Country.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, `Assets/Scripts/UI/GameController.cs`, `Assets/Editor/AverageCabinetDiagnostic.cs`, `Assets/Editor/CentralBankTermCheck.cs`, `Assets/Editor/D16AcceptanceCheck.cs`, `Assets/Editor/SwedenBlocsDiagnostic.cs`, `ElectionsData/sweden/2026/coalition_declarations_2026.md`, `ElectionsData/sweden/2026/raw/declarations/svt_20260609_c_andersson_mest_sannolika.html`, `ElectionsData/sweden/2026/raw/declarations/svt_hellden_karnkraft_regering.html` (+ both folders' `SHA256SUMS.txt`). Also read, unchanged, as readers of what changed: `ShadowBaseline.cs`, `PolicyImpactLedger.cs`, `HemicycleRenderer.cs`, `PoliSimTheme.cs` (the fence and `NudgedCache`), `GameController.PoliticsV35.cs`, `GameController.DocketV35.cs`, `ParliamentSystem.cs`, `CentralBankTerms.cs`, `FinancePartner.cs`, `WorldClock.cs`, `CampaignClock.cs`, `SimulationTestRunner.cs`, `TrajectoryBaselineDump.cs`, `CheckSuite.cs`, `docs/RULINGS_2026-10-23.md` (K53, K56, K58, K59, K79, K80, K87), `CLAUDE.md`.

## Verdict: NOT READY

There are two H findings:
- **H1 is new.** `20491d41` committed `SimulationManager.cs` with CRLF line endings, which makes it a whole-file rewrite. It cannot merge with main.
- **H2 is the first review's H2, unfixed.** The two diagnostics are still in no bar.

The one M finding is about the K80 day: the fork's restore clears `PlayedDay`. The rulings themselves are built correctly, and the sources hold.

## What was verified (and holds)

**(1) The dated sides: the sources and the register.**
- SVT, 2026-06-09: `datePublished 2026-06-09T09:01:03+02:00` and "Publicerad 9 juni 2026 kl 09:01". After the press-conference lines ("Centerpartiet meddelade beslutet på en pressträff på tisdagen"; 9 June 2026 is a Tuesday), it carries verbatim: "– Därför är Magdalena Andersson och Socialdemokraterna den mest sannolika samarbetspartnern för oss i Centerpartiet, säger Thand Ringqvist."
- SVT, Helldén: `datePublished 2026-01-22T17:43:44+01:00`. It carries "– Det tror jag inte. Jag hoppas att Socialdemokraterna ändrar sin analys." as the answer to "Det låter som att det blir regeringskris redan innan ni har tillträtt?". That is a DECLARED reading, and K87 rules it.
- Integrity: `sha256sum -c` passes on all 65 pages and all 7 pages. Each stored blob is byte-identical to its working file. `git check-attr text` reports `unset`. The delta touches no raw file.
- The register's correction (`coalition_declarations_2026.md:857-864`) is honest: it names the earlier error and what the page holds. The MP paragraph quotes the SVT exchange in full.
- `DatedSide` (`NationalElection.cs:350-361`) dates the sides as follows:
  - C: neither (-1) from 2025-11-13 to 2026-06-09 (exclusive), then 0.
  - MP: 0 from 2026-01-22.
- Against 2022's table, only C differs, and only inside that window. A search of the code for "C neither" leaves no stale claim.

**(2) `CalendarDay` and `PlayedDay`.**
- `CommitCalendarYear` (`SimulationManager.cs:3453-3459`) is called from `SetWorld`, `ResetClockToEpoch`, `AdvanceDay` and `RestoreSaveState`.
- `IsCounterfactual` is set before any of these calls in all three `ShadowBaseline` constructors, and `Awake` commits nothing. So a shadow or fork never writes `PlayedDay`.
- `SetEpoch` resets `PlayedDay` before its early return, so a new game, a load and every `EpochScope` dispose start clean.
- On a load, `RestoreSaveState` commits the restored date before any bloc reader runs.
- `ClonePreviewCountry` carries `CalendarDay` (`:7484`).
- What does not hold is the fork's restore clearing the played day: see M1.

**(3) `BlocOf` callers.**
- `D16AcceptanceCheck` across the whole range is a comment change only: `PairsClearingRatchet = 12`, and K79's re-pin and its C assertions are gone.
- No ink moves. At base the fence moved only V (against S) and M. With C out of the bloc, it skips C as a mover and as a "larger" party, so neither of those two moves changes.
- The stance model reads `BlocOf(Country)`, so a played game moves on 9 June 2026 through `Country.CalendarDay`. That is intended.
  - In practice the change is inert there: term 2b pulls only a party in the government's bloc. Before 9 June that bloc is 1 (Kristersson), and C is -1 at HEAD and 0 at base, so C is pulled in neither.
  - It bites only for an S-led cabinet formed in play before 9 June.
- The no-policy dump cannot move from this:
  - `TrajectoryBaselineDump` seats no player and runs no day tick, so `TryOpenBudgetProcess` never runs and there are no bills.
  - The tactical layer runs only on the player's staged campaign (`PlayerElection.cs:122-131`).

**(4) K56 is append-only.**
- `FederalReserveSystem.cs:123-131` appends the no-lean candidate and replaces nothing. The draws above it are unchanged, so no generator moves.
- `AiNominee` (`CentralBankTerms.cs:131-140`) takes the strict minimum |lean|, and the no-lean candidate is the only 0. So every AI pick equals the first K56 version's.
- The player's list keeps every drawn candidate.
- The Docket lays the list out two per row (`GameController.DocketV35.cs:245-252`), so four candidates make two rows.
- `CentralBankTermCheck` (f) now bounds a list at four and counts the fours.

**(5) K87.**
- `DecisionApprovalFactor` (`CabinetSystem.cs:635`) is read by `ApplyDecisionOption` (`:769`) and by the card (`GameController.cs:8947`).
- `ResolveCabinetDecision` is the one apply path, from the player's card or a test runner. Decisions roll only for a player who holds a portfolio (`SimulationManager.cs:6806-6810`), so K87 cannot reach the dump.
- The dismissal cost still reads `PopularityFactor` after the minister is removed (`SimulationManager.cs:3962`), so a vacancy stays x1.
- `AverageCabinetDiagnostic`'s K87 case runs the real apply path.

**(6) K59 at the Polish presidency's end.**
- `NotePlayerPresidency` reads `PlayersChairs` before the flip and seats after it (`SimulationManager.PlayerPresident.cs:55-57`).
- At the oath, `ChairIsThePlayers` is false while the office is held, so nothing is seated. At the end, the party's vacant chairs are seated.
- The guard's widened regex finds six write sites in the manager's partials (`:4472`, `:5449`, `:5476`, `PlayerElection.cs:226`, `Sejm.cs:167`, `PlayerPresident.cs:55`), each bracketed within 3 lines on comment-stripped text.
- I found no in-place mutation of `Government` or its portfolios in the manager that could hand the player chairs without a write.

**(7) The first review's findings.**
- M1, M2, L2, L3 and L5 are fixed. L3's regex matches the live two-line call (`[^;]*` spans the newline).
- L4 is taken up (MP on Helldén's words, K87).
- L1 (K33's undeclared France pre-start seeding) is not addressed. It stays owed with the family diff.
- H2 is not fixed: see H2.

**(8) BASELINE family `form`.**
- By reading, only K56 moves the no-policy trajectory.
- The C bloc change reaches nothing in the dump (see (3)).
- K87 reaches nothing either (decisions roll only for a player who holds a portfolio).
- `CalendarDay` is read only by `BlocOf`.
- The K59 presidency seat needs a player candidate.

**(9) Rules.**
- The gate is OK. The delta adds no file and no `.meta`.
- The delta adds no `<see cref` and no player-visible string, so it touches no MetaTextCheck surface.
- The new public members all have readers, so UnwiredSubsystemCheck is not affected.
- The working-tree CRLF is as checked out. The blob is not (H1).

## Findings

### H1: `SimulationManager.cs` committed with CRLF, a 9,009-line whole-file rewrite that conflicts with main as one hunk

**Where:** `Assets/Scripts/Simulation/SimulationManager.cs` in `20491d41`.

**The problem:**
- The blob at `ace6a9e0` and `2a0d60aa` has 0 CRLF lines out of 9,001. At `20491d41` and `b541e686` it has 9,009 CRLF lines out of 9,009.
- Main's blob (`784f93b2`) is LF. Every other file in the delta is LF in the blob.
- `git diff --stat` reports 18,010 changed lines. With `--ignore-cr-at-eol` the real change is 12 lines (the `PlayedDay` property, the reset in `SetEpoch`, the commit in `CommitCalendarYear`, the clone line).

**Failure scenario:** `git merge-tree --write-tree 784f93b2 b541e686` gives one conflict covering 18,028 lines, because main has changed this file since `140aab81`. An integrator who takes either side whole drops one side's work in the hub of the simulation: main's §907 changes, or K80's day.

Every later diff and blame of the file is also unreadable. That alone made this delta's real change reviewable only with `--ignore-cr-at-eol`.

**Fix:** Re-commit the file with LF: `git add --renormalize`, or write it with LF and commit. Then confirm `git diff --stat ace6a9e0..HEAD -- Assets/Scripts/Simulation/SimulationManager.cs` shows about 12 lines.

### H2 (carried from the first review): `SwedenBlocsDiagnostic` and `AverageMinisterLeakMeasure` are still in no bar

**Where:** `Assets/Editor/CheckSuite.cs`. Only `AverageCabinetDiagnostic` (`:205`) and `CentralBankTermCheck` (`:239`) are listed there.

**The problem:** `git log 140aab81..b541e686 -- Assets/Editor/CheckSuite.cs` is empty. The fix commit's message names M1, M2, L2, L3 and L5, not H2.

**Failure scenario:** This delta added checks (d)'s live-call regex and (e)'s "BlocOf reads the dated sides". Both run nowhere, so a revert of K80's `DatedSide` rows, or a `BlocOf` back on `SwedenBlocs2022`, stays green.

Nothing in any bar exercises the K80 mechanism itself: a played manager advanced across 9 June 2026 and then read through `BlocOf(Country)` or `BlocOf(CountryId)`. Check (e) reads only the epoch fallback.

**Fix:** Register both diagnostics, the measure in the simulation tier. Add a case to (e) that advances a Sweden-start manager from 2026-06-08 to 2026-06-09 and reads C through both overloads, and that reads it after a `ShadowBaseline` fork (see M1).

### M1: the shadow's restore clears the played world's day, so the seat map reads C on the epoch's date after every load and every first lever touch

**Where:**
- `Persistence/SaveGameService.cs:308`: `SetEpoch(save.EpochDate)` runs inside every `RestoreInto`, including a fork's (`ShadowBaseline.cs:165`, `:411`).
- `SimulationManager.cs:296`: `SetEpoch` resets `PlayedDay` unconditionally.
- `NationalElection.cs:272-273`: with `PlayedDay == MinValue`, the id-only overload reads `EpochDate`.

**Who forks:**
- `GameController.RestoreFromSave` restores the played manager (`:983`) and then builds the carried or forked shadow (`:1006`).
- `PolicyImpactLedger.BeforeLever` (`:219`) forks on a family's first touch.
- The fork is counterfactual, so it never writes the day back. `PlayedDay` stays MinValue until the played world's next `AdvanceDay`.

**Who reads it:** every caller of the id-only overload, which is the CLAUDE.md "several worlds" class (a process static written at run time):
- the hemicycle's order and legend (`HemicycleRenderer.cs:98-99`, `142`, `429`, `470`, `521`, `571`);
- the parliament slip's bloc (`GameController.PoliticsV35.cs:929`);
- the fence (`PoliSimTheme.cs:563`).

**Failure scenario (probe `playedday/`, replicating the four sites):**
1. Sweden's start is 2026-01-18.
2. A save cut on 2026-07-15 loads, and `PlayedDay` becomes 2026-07-15 (C = 0).
3. The shadow is restored, and `PlayedDay` becomes MinValue. C is now read on 2026-01-18: -1, "unaffiliated".
4. The seat map moves C out of the left bloc while the game sits paused after the load.
5. The same happens mid-game the first time the player pulls a lever of an untouched family on any day after 9 June, until the clock moves.

**Why it is not higher:** No ink moves (see (3)), and the cache in `PoliSimTheme.NudgedCache` (`:432`, `:486`) is built once in any case.

**Fix:** Keep the played day across a counterfactual's restore, for example by having `RestoreInto` save and put back `PlayedDay` when `sim.IsCounterfactual`. Or have the UI readers pass the played `Country`, so `BlocOf(Country)` reads its own `CalendarDay`.

### L1: `Country.CalendarDay` IS saved, against its doc and the format note

**Where:** `Country.cs:168-171`; the format-57 note at `SaveGameService.cs:104`.

**The problem:** The doc says "Not saved", and the note says "no field is added". But `Country` is serialised by Json.NET with default member selection (`SaveGameService.BuildSettings`, no `[JsonIgnore]`), so the public field rides every save, as `CalendarYear` does. It is harmless, since `RestoreSaveState` commits over it, but both texts are false.

**A cosmetic point on the same line:** `Country.cs:171` is indented one space (`␠public System.DateTime CalendarDay;`).

**Fix:** Either mark the field `[Newtonsoft.Json.JsonIgnore]` or correct both texts.

### L2: the bar's `BlocOf(CountryId)` reads are order-dependent on a leaked `PlayedDay`

**Where:** `D16AcceptanceCheck.cs:68-71`.

**The problem:** A non-counterfactual manager built outside an `EpochScope` leaves `PlayedDay` set for the next check in a batch. Today only a stale day inside 2025-11-13 to 2026-06-08 changes an answer, and only Sweden's start opens there. If such a check ever runs before D16, D16's first assertion fails (C not grouped with V) for a reason unrelated to the inks.

**Fix:** Have D16 read `BlocOf(CountryId.Sweden, abbrev, EpochDate)`, the dated overload, explicitly.

### L3: the ink fence is cached at the first date it is read

**Where:** `PoliSimTheme.cs:483-612`.

**The problem:** `NudgedCache` is per country for the process, and nothing clears it when the day crosses a dated side. Today no ink moves, so this is latent only. A future dated side that does move an ink would freeze at whatever day the first frame drew. Worth a line beside the "no ink moves" claim in the register.

### L4: the first review's L1 (K33's France pre-start seeding) is still undeclared

**Where:** K33's record.

**The problem:** No change in the delta touches `ElectionsData/france/records_by_date.md` or the K33 text. It stays owed with the family diff for the Italy and Poland starts.

### L5: a stale doc line

**Where:** `FederalReserveSystem.cs:85`.

**The problem:** It still opens "Draws 2-3 candidates". The next line now says a list may hold four.

## Owed at integration (not findings)

- The review-ledger rows for the money paths (`Tools/review_row.ps1`).
- The simulation bar.
- The BASELINE family `form` with its `traj_diff` read for every start. It is expected to show K56's movers only, from turn 5 (plus L4's France seeding in the Italy and Poland starts).
- The dry film and one filmed width for the Docket with four central-bank candidates and the cabinet card's scaled approval arrow (GameController is in the delta).
- The SaveGameService merge with main's format 58 (an expected conflict, separate from H1).
