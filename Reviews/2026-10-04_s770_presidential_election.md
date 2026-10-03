# Review - §770, PS-5 item C4: the game's own presidential election (2026-10-04)

The money-path rule: `Assets/Scripts/Simulation/SimulationManager.cs` (the day loop's hook, the veto gate's call). An independent reviewer read the staged diff against HEAD b86e730; read only, no Unity run.

## The first pass (verbatim)

Reviewed: the staged diff against HEAD b86e730 (19 files, including docs/reference/PRESIDENTIAL_VOTE.md), read in full. I also read the code around it: TwoRoundElection, PresidencyOfRecord, PresidentialVeto, NationalElection.TryPredictShares/DeriveRegional and the LastRegional* readers, EconomicVote.RecordOverTerm and PerceivedPerformance.OverTerm, PartySystems.For/JointList and the Polish roster, CreatedParties.Extend/ToParty, SimulationManager.AdvanceDay/PresidentialVetoGate/PolishTaxAct/ClonePreviewCountry/preview paths, ShadowBaseline and its constructors in GameController, SaveGameService.BuildSettings/RestoreInto, ChamberVerdicts, the GameController veto readers, the film driver's earlier pin frames, PreviewParityDiagnostic's clone audit, DeadStateCheck's scope, NoPolicyCentury, PresidentialVoteBacktest's field and CHES loader, the Polish rows of CHES_2024_final_v2.csv, and COMPLETED.md §764–§766. I did not run Unity.

Verdict: NOT READY

### Defects
1. **The run-off places Hołownia somewhere B4 was never fitted, and that placement decides the 2025 winner.** Locations: `PresidentialElection.cs:263`/`:324` (`PointOf(backing party)`), the doc at `:28`, `PresidencyOfRecord.cs:66`, `PresidentialVoteBacktest.cs:172`, and `docs/reference/PRESIDENTIAL_VOTE.md:16` against `:58`.
   - **Placement.** The runtime puts each candidate at the backing roster party's position, so TD's candidate sits at TD's seat-weighted mean of PSL and Polska 2050 (D2). B4's τ was fitted in `PresidentialVoteBacktest` with Hołownia's electorate at Polska 2050's own CHES row (`Field2025`, `ChesParty`). The model card's B4 row still reads "each candidate at its party's CHES 2024 row". The new backtest check at `:172` compares committees only, never positions, so the difference passes silently. I found no ruling on the placement in COMPLETED.md; the model card's new paragraph states the difference but not what it does.
   - **What it does.** I recomputed `RunOffShare` by hand (a perl script in my scratchpad, not the repo) on the diagnostic's first round. With TD's mean I get Nawrocki 50.30 % of the two, matching the diagnostic exactly. With Polska 2050's row I get Nawrocki 47.40 %, so Trzaskowski is elected. At TD's mean, Hołownia's electorate goes 35.5 % to Nawrocki. The exit poll says 13.8 and the fit's own placement gives 23.7. TD's mean is also the farthest electorate, so all of it takes the full 25 % abstention.
   - **Fix (Elias's to rule: it is a model specification).** Put both placements and both winners to Elias. Until he rules, either:
     - give `CandidateOfRecord` a position party (Polska 2050 for HOŁOWNIA, the backtest's `ChesParty`) and assert in `PresidentialVoteBacktest` that the runtime's position for every 2025 candidate equals the backtest's row; or
     - declare the TD placement as a PREMISE in the class doc and the model card, naming the winner it decides.
2. **`UiScreenshotDriver.cs:1317` crashes the film when the first round elects outright.** `held.RunOffOn += back` runs whenever the contest is decided. If the first round elects someone outright, `RunOffOn` is `DateTime.MinValue`. `back` is always negative, because the first vote is never before the film's day minus 20. The addition throws `ArgumentOutOfRangeException` and the coroutine dies before the `RemoveRange` at `:1327`. The planted contest then stays in the live world and the rest of the session goes unfilmed. Fix: shift the run-off date only when `held.RunOffA != null`, and put the removal in a `finally` (an iterator allows `yield` inside try/finally).
3. **Comments that do not match the code, or that break the claim convention:**
   - a. `PresidentialElection.cs:308` says "before date", but `:315` tests `<=`. The 2025 game round therefore reads 2025's own valid total; the diagnostic expects this, so the wording is what is wrong.
   - b. `:92` says the backtest proves the arithmetic "on the 2025 record". The check sits inside the 2020/2025 loop, so it covers both years.
   - c. `:254` says "the record's president holds". That is true only while no game contest has been decided. A later undecided contest leaves the game's previous president in office, and an undecided 2025 contest seats the record's Nawrocki, the record's winner.
   - d. `:161` does not mention the third way `TryNextFirstVote` returns false: the four-term loop runs out.
   - e. `PresidentialElectionLiveDiagnostic.cs:17` and the save check's label say "the save's own serializer". The check uses `JsonConvert.SerializeObject` with default settings, not `SaveGameService.BuildSettings()`/`Serialize`. Use the save's settings, or reword.
   - f. `PresidentialElectionLiveDiagnostic.cs:14` copies derived dates into a doc comment (the premise's 19 May 2030, 6 August). The check label at `:44` copies a window (2030-04-28 to 2030-05-23) that nothing asserts.
   - g. `PresidentialElection.cs:35` "five pairs" is a count in a comment (minor).
4. **`GameController.ParliamentRows.cs:510` shows ruling ids to the player.** The slip line ends "(B1, B2)", and a grep found no other player-facing text carrying ruling ids. Fix: drop the parenthesis. Same line, minor: "HIS BACKING PARTY" will read wrong for a female game president; `ChamberVerdicts.VetoLine` already has the same wording.

### The questions asked
1. **Simulation correctness.**
   - **Twice:** not possible. The first vote is guarded by `Exists(c => c.FirstVote == date)`, the run-off by `!RunOffHeld` plus an exact-day match. A save loaded mid-contest carries `RunOffA`/`RunOffOn`, so the run-off is held on its day; a save made on the first vote's day resumes the next day.
   - **Skip:** if `TryPredictShares` fails on a first vote's day, nothing is held, nothing is logged, and the next day `TryNextFirstVote` moves on to the next term. This cannot happen for Poland today, but a warning would be cheap. A run-off is matched with `==`, which is safe while the day loop steps one day at a time; `<=` would be sturdier.
   - **Wrong day:** none found. Between the first vote and the oath the president is the record's Duda. The term chain (TookOffice + 5 years, looped) gives 2030 correctly, because `TakesOffice` is always the term's end.
   - **Tie, or a finalist with no position:** the contest stays undecided for good; see 3c for who holds the office then.
   - **Outright winner:** correct, apart from the film crash in defect 2.
   - **After 2030:** the chain runs from the game's president. `TermsServed` is right for Nawrocki whether or not the run held 2025. It undercounts Duda (the record holds only his 2020 term), but that is unreachable because `CandidatesOf(2025)` fields PiS's record candidate first. The four-term loop runs out only after repeated undecided contests, and then elections stop silently.
   - **`PresidentAt`:** right. It takes the latest decided contest whose oath is on or before the date, ends that term at the next decided oath, and ignores undecided contests.
   - **Exceptions:** none found.
     - Nobody clears the gate: the vote list is empty and `CountFirstVote` returns "fewer than two"; `sum` is only divided inside the empty loop.
     - A finalist without a position is reported and left undecided.
     - A NaN predicted share would pass both guards (NaN comparisons are false). I saw no source of NaN; a `double.IsNaN` guard is cheap.
     - Created parties are included through `PartySystems.For`. A created party fields its `LeaderName` — in practice the player's chosen leader — so the player's leader can stand and win, although StartPoints says that start is not built. That is a question for Elias.
2. **Determinism and side effects.**
   - **No RNG, economy or approval writes.** None of NationalElection, PreferenceModel, EconomicVote or PerceivedPerformance draws randomness, and the hook writes only `Country.PresidentialElections` and the static `NationalElection.LastRegional*`.
   - **The static is harmless.** For Poland it is set to the Polish keys with null shares. Its readers are the Swedish and German nights and German-only paths, and the hook only runs when the player is Poland.
   - **Other countries:** it runs only for `PlayerCountryId`, but in every SimulationManager that has one set:
     - **Forked shadows:** the impact ledger's except-worlds and the baseline forked after a load hold their own rounds and print their own "PRESIDENTIAL:" lines, so a log can carry several such lines for one round.
     - **New-game baseline:** the seeded baseline has no player and never holds a round, so the no-policy baseline's Polish president differs between a new game and a loaded one. Other player-only logic already behaves this way.
     - **Non-player Poland:** keeps the record's president indefinitely.
   - **Money path:** once a president from a different party takes office, the veto reads that party, so runs with Poland as the player can move — NoPolicyCentury, for one, sets the player. Not verified.
3. **The veto's callers.** The gate and `ChamberVerdicts.Veto` both pass the country's own contests; Editor callers pass null or `pl`'s; grep finds no other runtime caller. `ChamberVerdicts.Veto` is not cached: it calls `Decide` with the date on every call, over cached stances that do not read the president. The UI recomputes it on each draw. It cannot go stale.
4. **The save.**
   - **Serialization works:** public fields under the default contract. `PreserveReferencesHandling.Objects` only adds `$id`s. `RoundtripKind` keeps the dates' ticks, which is what the `==` tests compare. The field initialiser plus `ObjectCreationHandling.Auto` fills a fresh empty list.
   - **The version reason is correct**, and incomplete: a v37 save made mid-contest would also lose its pending run-off.
   - **The preview clone** gets an empty list. The parity audit passes because the list is non-null, and the preview never reaches the veto gate, so it is harmless.
5. **The row, the slip and the calendar.**
   - **Basis prefix:** the "ELECTED IN THE GAME / OF RECORD" test works today, since both record Basis strings begin "the run-off of". It is an unshared string coupling, though: rewording `PresidentAt`'s Basis would silently turn every game president into "OF RECORD". Use a shared constant or a flag.
   - **Undecided count, same day:** on a first vote's own day after an undecided count, the row shows NEXT VOTE with today's date.
   - **Premise not shown:** a held contest's `DayBasis` — a DECLARED premise for every contest after 2025 — is written but never read, so the slip prints the date bare.
   - **"Nothing is decided" not shown:** an undecided contest's `Line` never reaches the slip.
   - **Row width:** grows without a bound; not measured.
   - **Calendar markers:** correct.
6. **Text accuracy:** see defect 3.
7. **The film frame.**
   - **State:** it removes the contest (barring the crash in defect 2) and clears all pins, as the §768 frame does. It overwrites `LastRegional*` with Poland's, which is harmless. `HoldRounds` changes nothing except the contest list and the contest itself.
   - **Honesty:** the log says the rounds were held for the film and the days moved. The frame shows the moved dates as if real, and the contest's `Line` keeps the original dates, though the frame never shows it.

### Notes
- `ProjectSettings/ProjectAuditorSettings.asset` is modified but unstaged; it is not part of this change.
- Not verified: any Unity run (bars, sentinel or family digests for runs with Poland as the player, dry or real film), the row's width at 1280/2560, CommentClaim/DeadState results, and whether the film harness lists its expected 07a frames.
- The runtime's positions are the roster's two-decimal rows, while the fit reads the CSV's raw values. The effect is negligible next to defect 1.
- Performance: `TryNextFirstVote` → `FirstVoteFor` → `RoundOfRecord` builds a list on each call, up to four calls each time, and OnGUI calls it twice per pass (the row and the calendar). Small.
- Poland's real parties have no Leaders, so from 2030 every party but the incumbent's fields an unnamed "<party>'s candidate (year)".
- `PresidencyOfRecord`'s class doc still says the record holds "until the game's own presidential election is simulated (part two)". It is now simulated, at C4.

## What the author did about the first pass

- **Defect 1 - fixed, and its consequence put to Elias.** `PresidencyOfRecord.CandidateOfRecord.PositionUnit`: a candidate of record stands at the CHES unit B4 was fitted on - Hołownia at Polska 2050's own row (`PartySystems.PolandTdMembers`, through `PresidentialElection.TryPosition`), the others at their roster rows. Each `Candidate` keeps its position from the first vote; the run-off reads the kept positions. `PresidentialVoteBacktest` asserts every 2025 candidate a committee runs stands, at its runtime position, within half a hundredth of its own CHES row on each axis. **The game's 2025 run-off now elects Trzaskowski, 52.60 % of the two** (n770c) - the reviewer's hand figure; recorded in §770 as the first ruling owed to Elias (the first round's known miss, §727), not tuned.
- **Defect 2 - fixed.** The film frame moves the run-off's day only where a run-off was called; the pins and the planted contest's removal sit in a `finally` around the yields; the log says PLANTED.
- **Defect 3 - fixed:**
  - a-d: the comments reworded to the code.
  - e: the round-trip uses `SaveGameService.BuildSettings()` and compares the positions.
  - f: the derived dates are gone from the doc; the 2030 window is asserted through `TwoRoundElection.TermOf`.
  - g: the count is dropped.
- **Defect 4 - fixed.** No ruling ids on the slip; the veto line names no pronoun.
- **The questions' smaller points, taken:**
  - `PresidentialElection.GameBasis` / `ElectedInGame` replace the prefix test.
  - `TryNextFirstVote` passes over a first vote already held, so neither the row nor the calendar shows a held day as the next.
  - The row shows NOTHING DECIDED and the slip shows the undecided count's line.
  - The slip prints a premise day's basis.
  - A NaN guard on the shares, and a warning where no prediction can be made on a first vote's day.
  - The save version's reason names a pending run-off.
  - The `PresidencyOfRecord` class doc is updated.

## The second pass (verbatim) - on the first's fixes

Reviewed: the second pass on §770 (PS-5 item C4). I read the full staged diff against HEAD b86e730 (20 files, including the staged first review), all of the new `PresidentialElection.cs` and `PresidentialElectionLiveDiagnostic.cs`, and the changed parts of `PresidencyOfRecord.cs`, `PresidentialVoteBacktest.cs`, `UiScreenshotDriver.cs`, `GameController.ParliamentRows.cs`, `GameController.cs`, `SimulationManager.cs`, `SaveGameService.cs` and the model card. I checked them against `TwoRoundElection.CountFirstVote`'s lines, the roster and `PolandTdMembers`, the Polish CHES rows, and which Editor runs set Poland as the player. I did not run Unity, so the n770c result and the 52.60 % are the author's; they match my hand computation from the first pass.

Verdict: READY

### Defects
None of these blocks the commit.

1. **"Nothing is decided" prints twice after a first-round tie.** `PresidentialElection.cs:282` adds "; nothing is decided" to `counted.Line`. For a tie, `TwoRoundElection.CountFirstVote`'s line already ends "...not modelled; nothing is decided", so the slip (`ParliamentRows`, the `undecided` branch) shows the phrase twice. Fix: add the suffix only when the counted line does not already end with it, or only in the "fewer than two" case.
2. **The new warning repeats the wording my first-pass 3c corrected.** In `SimulationManager.HoldPresidentialRound` the warning says "the record's reading of the office stands". After the game's first decided contest, a failed prediction leaves the game's own president in office, not the record's. Suggested wording: "the office stands as it is".
3. **One doc claim is broader than what the check does.** The `PresidencyOfRecord.CandidateOfRecord` doc says the backtest holds the field to these readings "the positions included". The new assertion covers only the candidates a committee runs. Zandberg's `PositionUnit` is null where the backtest places him at Razem, and Braun's "Konf" is never read. Both are harmless because neither stands. Fix: say "the positions of the candidates who stand", or null out both.

### Notes
- **The `TryNextFirstVote` skip is correct.** A day is skipped only when a contest with that exact `FirstVote` is already in the list, and a contest gets into the list only by being held. So it cannot hide a day that is still owed. The term chain is unchanged (the incumbent's `TookOffice` plus the term, then later terms), and on failure `termEnds` and `basis` are reset.
  - **Double holds.** `HoldRounds` and `IsRoundDay` no longer test for a held day themselves. They are still guarded: on a held day, `TryNextFirstVote` returns the next term's day, which is not equal to `date`.
  - **Display.** On the first vote's own day, after it is held, the row and the calendar now show the next term's day, which fixes the case I raised. A first vote whose prediction failed is not in the list; it is passed over by date the next day, as before.
  - **Film interaction.** The planted contest's moved date is earlier than the film's day, so it cannot hide a day on or after it. No yield falls between holding the contest and moving its dates.
- **The film's try/finally is correct.** Every `yield` is inside the `try`, none is in the `finally`, and there is no `catch` (any of those would not compile; n770c compiled the file).
  - **Exceptions.** If `MoveNext` throws, the compiler-generated fault handler disposes the iterator, so the `finally` clears the pins and removes the contest.
  - **Outright winners.** The run-off date is moved only when `RunOffA != null`.
  - **When it may not run (uncertain).** Unity does not dispose a coroutine stopped from outside, and I believe the same holds when a nested yielded coroutine throws. In those cases the `finally` does not run. This is harmless in a film session.
  - **The log.** It now says PLANTED.
- **The backtest's position assertion is right.** It compares each committee candidate's runtime position for its `PositionUnit` against the backtest's own CHES row.
  - **Tolerance.** The roster's two-decimal rows differ from the CSV by at most 0.0048 (KO/PO galtan), under the 0.0051 tolerance.
  - **What it catches.** TD's joint mean sits about 1 apart on galtan and would fail.
  - **The runtime side.** The live diagnostic's check at `:67`–`:68` confirms the contest actually stores Polska 2050's row for Hołownia and the party's own row for each other candidate. Together the two checks hold the data and its use.
- **D1 fix.** Positions are taken at the first vote and stored with each candidate, the run-off reads the stored positions, and the save round-trip now compares them under `SaveGameService.BuildSettings()`.
  - **The owed ruling.** The 2025 winner is now Trzaskowski, recorded as a ruling owed to Elias; it should stay in front of him.
  - **What it changes.** In any run with Poland as the player that crosses 6 August 2025, the veto reads KO from then on. That covers a game from the Polish start, and `NoPolicyCentury.For(Poland)`, which HealthTrend and InfrastructureReadout read. `TrajectorySentinelCheck` sets no player (grep), so the sentinel itself should be unaffected. Not verified.
- **Small leftovers:**
  - **Latent `TryPosition` trap.** `TryPosition` searches the roster before `PolandTdMembers`. A unit that is both a roster key with no position and a TD member, as "PSL" is, returns false without reaching the member's row. No candidate uses it today.
  - **Stale "NOTHING DECIDED".** The row keeps showing "NOTHING DECIDED" for the last undecided contest until the next first vote, possibly for years.
- **The other fixes are in as described:** the first-pass D2, D3a–g and D4, `GameBasis`/`ElectedInGame`, `RunOffPending`, the NaN guard, the premise day on the slip, the save-version reason, the `PresidencyOfRecord` doc and the model card.
- **Bookkeeping:** the staged Reviews file holds only the first pass. This report and its ledger row (`review_row.ps1`) still need adding before the bar.

## What the author did about the second pass

- **1 - fixed.** The suffix is added only where the count's own line does not already end with it.
- **2 - fixed.** The warning now reads "the office stands as it is".
- **3 - fixed.** The doc says what the check holds: the committees, and the positions of the candidates who stand.
- **Leftovers - both fixed:**
  - `TryPosition` reads a joint list's member row where the roster holds no *positioned* party by that key.
  - NOTHING DECIDED shows only until the term it was for ends.
