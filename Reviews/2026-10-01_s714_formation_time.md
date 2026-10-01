# Review — §714 formation time from the record; the caretaker governs meanwhile (2026-10-01)

Reviewed: `git diff --cached` against HEAD 9874845 (all 7 files). I read:
- the staged `Assets/Scripts/Elections/WorldClock.cs`: `ChamberOfRecord` and `GovernmentOfRecord` 30-75, `Chambers` 129-175, `Governments` 228-280, `FormationDays` 296-336;
- `SpeakerRound.cs` (`FormationDue`);
- the staged `Assets/Scripts/Simulation/SimulationManager.cs`: `OpenSpeakerRound` 3035-3066, `Table` 3130-3150, `TableAndVote` and `AdvanceSpeakerRound` 3150-3200, `OpenRoundAfterElection` 3680-3712, `DischargeAndRound` 3795-3806, every `Caretaker` reader, and `TryOpenBudgetProcess` 2193-2245;
- `GameController.ParliamentRows.cs` 172-195 and 265-280, and `GameController.FormationSheet.cs` 70-80 and 365-385;
- `UiScreenshotDriver.cs` 1005-1016 and 4838-4850;
- the new `FormationTimeDiagnostic.cs`, plus the diffs of `GermanFormationDiagnostic` and `CheckSuite`;
- the walking checks `SpeakerRoundDiagnostic` 44-96, `ConfidenceDiagnostic` 191-201 and 343-385, `ChamberVerdictCacheCheck` 228-250, and `AiMotionReachDiagnostic` 165-240;
- the RF text on disk: `sweden/confidence_rules.md:45-46` [RF-R:6:4] and `sweden/2026/government_2026.md` 122-150 [RD-SBR].

One independent reading, read-only; Unity not run (a bar is running). **Verdict NOT READY, one small fix (defect 1).**

## Defects

1. **The game tells the player a vote day it no longer keeps.** Two helpers still state the vote's day without `FormationDue`.
   - **`RoundVoteWhen`** (`GameController.ParliamentRows.cs:189-195`) is drawn on the formation sheet's act and its slip (`FormationSheet.cs:373, 381`, read when the player decides to table). It says "VOTE ON DAY 4" / "TABLED, THE CHAMBER VOTES ON THE FOURTH DAY" for the Riksdag. For the Bundestag it says "VOTE THE SAME DAY" or "VOTE ON <Convenes>".
   - **`RoundRule`** (172-179; the sheet at `FormationSheet.cs:75` and the round's slip) says "THE CHAMBER VOTES ON THE FOURTH DAY AFTER A PROPOSAL IS TABLED".
   - **Failing scenario, Germany 2025:** the player's CDU is asked on 24 February. The sheet says "VOTE ON 25 MAR", while the ballot comes on 6 May (`VoteOn = FormationDue`).
   - **Failing scenario, Sweden:** asked the day after polling, the sheet says "VOTE ON DAY 4", while the vote comes 37 days after polling.
   - **What still reads correctly:** the Parliament row's `DateStamp(round.VoteOn)` (272, 277).
   - **The Riksdag's log line contradicts the rule it cites.** "{formateur} tables …; the Riksdag votes on {VoteOn} (RF 6 kap. 4 §)" reads as a submission today with a vote weeks later. RF 6 kap. 4 § says "Riksdagen ska inom fyra dagar … pröva förslaget" once the Speaker "lämnar … förslag".
   - **Fix:** one helper for the vote's day, the same max the `Table` method takes, used by `RoundVoteWhen` and `RoundRule` (a date, not "the fourth day", while a formation's time runs). On the Riksdag line, say the Speaker submits on `VoteOn − 4` (the author's reading, question 3).

**Latent:**
- **The (d) test is partly in-sample.** "The record's own day" in check (d) holds by construction: 2025's own 72 days are in the mean (with 2021's 73), and the floor of 72.5 lands exactly on it. Rounding would give 73, which is 7 May; leaving 2025 out also gives 73. The check proves the plumbing, not a forecast, and its message should not read as one.
- **"Recent" is set by how far back the record goes, and here n is small.** Sweden's 37 is one formation (Kristersson, 2022). Germany's 72 is two.
  - **Excluded by the record's start:** Löfven II (polling 2018-09-09, in office January 2019, about 134 days) and Merkel IV (2017-09-24 to 2018-03-14, about 171 days). The window comment names them as missing.
  - **What including them would give:** Sweden about 85 and Germany about 105 by the mean. The ruling's "recent formations" leaves the depth open, so state it for Elias.
- **The statistic is the author's own choice and should be marked [AUTHORED-DRAFT].** The "mean, whole days, rounded down" is the author's, but only the window carries the tag.
- **Caretaker detection is a text search.** It reads the head's text (`IndexOf("caretaker")`). It is right for every entry today (Sweden 2026, France's Attal), but a caretaker entry written differently would count as a formation. A flag on `GovernmentOfRecord` would be safer.
- **The wait sits after tabling, not in the talks.** `FormationDue` is a floor on the first vote, placed after tabling. A proposal is drafted on day 8 and then waits weeks as VotePending, its cabinet and frozen demands fixed. The record's long phase is the talks, before any proposal. A failed first vote adds consultation and voting days, so the game's formation can run longer than the mean. This is a premise to state; stretching the consultation would be the alternative.
- **Runs not covered by n714d:**
  - **`AiMotionReachDiagnostic`** opens post-election Swedish rounds (`electionDay: 2026-09-13`, 177 and 194), which now carry a formation due date of 2026-10-20. Its stand-in still installs within its 30-day wait (19 days), but every later date in its budget-vote chain moves by about 15 days, and that chain's outcome is asserted.
  - **The play protocol's post-election round and the formation film:** `-shotformation` walks to the ballot's eve, which is now weeks later, so frame e8c's date changes.
  - **Owed:** the cheap and simulation bars, and a film.
- **No save-format bump.** A pre-§714 save mid-round loads with `FormationDue = MinValue`, i.e. no delay, as the doc states. That matches the project's practice for the round's fields since §705.

## The questions asked

1. **The 365-day window: defensible, and insensitive.** Every counted span is at most 73 days (Germany 73 and 72, Sweden 37, Poland 33 and 59, Italy 27, France 60, each checked by hand against `Governments`). Every government it must exclude took office at least 1,077 days after the election (Andersson 2021 after 2018, Scholz's minority after 2021, Draghi after 2018). Any window from 74 to 1,076 days gives the same result. A year is a round bound above every realistic formation in these five countries; the longest in the comment's own list, Merkel IV, took about 171 days. It is marked [AUTHORED-DRAFT].
2. **Mean versus median: identical today.** n is at most 2 per country, and the median of two is their mean, so the floor applies equally. They part only when the record grows: with Merkel IV, the mean is about 105 and the median 73. A median keeps one long formation from setting the clock. Choose now and mark it; I would take the median.
3. **The Riksdag's `max(today + 4, FormationDue)` squares with RF 6 kap. 4 § under the author's reading.** The four days run from the Speaker's submission ("Talmannen … lämnar sedan förslag till riksdagen. Riksdagen ska inom fyra dagar … pröva förslaget"; riksdagen.se: "senast på fjärde dagen efter den dag då förslaget lagts fram"). When the Speaker submits after the talks is the Speaker's choice, so holding the proposal until four days before the vote is lawful. The game's own text must then say so (defect 1).
   - **Fit to the record:** in 2022 the vote was on 17 October and the government took office on 18 October. The game votes and installs on the 18th (polling + 37).
4. **Does any path read `VoteOn` as "tabled + 4"? In code, no.** `AdvanceSpeakerRound`, `TableAndVote` and the film's eve all read `round.VoteOn`. In text, yes: `RoundVoteWhen` and `RoundRule` (defect 1).
   - **SpeakerRoundDiagnostic** asserts `VoteOn == today + VoteDays`; its round has no election day, so that still holds.
   - **The ConfidenceDiagnostic and ChamberVerdictCacheCheck** walks are mid-term rounds, so they are unaffected.
5. **Mid-term rounds untouched: right.** The ruling's measure is "election date to installation", and a round after a discharge (`DischargeAndRound`, `midTerm: true`) has no election date. The record's mid-term take-overs (Andersson 2021) are exactly what the window keeps out of the mean. Only `OpenRoundAfterElection` passes `electionDay`, so every real post-election round gets the due date, including a Swedish start round opened after the 2026 polling day. The checks' rounds opened with no election day keep the four days. A round opened late finds the due date already past, so it adds nothing.

## Confirmed sound, stated

- **The derivation follows the ruling's words.** It uses the record's own dates (the chamber's `ElectionDay` and the government's `From`), election to installation. "Installation" is the day of taking office (Scholz 8 Dec 2021, Merz 6 May 2025, Kristersson 18 Oct 2022, Tusk 13 Dec 2023, Meloni 22 Oct 2022).
  - **Skipped correctly:** caretaker entries, presidencies (the USA gives null), and a missing polling day (France 2022).
  - **The doc's figures** are the ones the code yields.
  - **`FormationTimeDiagnostic`** asserts all six and prints each formation; it is in the cheap table.
- **Wiring is complete.** The due date is set only where an election day exists and a time is on record, and the log names the formations it averages. Both `Table` branches take the max, and later tablings are unaffected once the date has passed. Phase 2's dates in check (e) move consistently (6, 20 and 21 May).
- **"The caretaker governs meanwhile" holds as built.** Germany's government stays in office to the convening and serves on from it (Art. 69 Abs. 2-3). Sweden's is discharged when the round opens (§646). A caretaker keeps every lever: the only caretaker gates are no motion (2548), no extra election (2616) and no discharge (2637). The budget windows are not reached by a 37- or 72-day formation after an autumn election, since fiscal years start on 1 January and the arrival window re-arms at install.
- **No random stream is drawn.** The new saved field defaults to "none". Sweden's behaviour changes only by the ruling (the first vote after a post-election round).

**Money path:** yes by name (`SimulationManager`), so the ledger wants this review's row. No arithmetic is touched. Who governs, and when, moves later after an election, so the caretaker's book runs longer, but nothing computes differently.

**Verdict: NOT READY, one small fix.** Fix defect 1: the vote's day on the sheet and in the round's rule from one helper, and the Riksdag's log stating the Speaker's submission day. Then the bars, `AiMotionReachDiagnostic` above all, and the formation film. The window, the statistic and how far back "recent" reaches go into the record for Elias: the window is defensible and insensitive, and mean and median coincide today.

## The author's note (2026-10-01)

- **Defect 1, fixed:** the vote's day is ONE rule, `SpeakerRound.VoteDayIfTabled` (the Bundestag's: not before the convening nor the formation's day; the Riksdag's: the fourth day after the Speaker submits it, not before the formation's day), read by `Table` and by both text helpers - `RoundVoteWhen` says *VOTE ON <day>* / *THE SPEAKER SUBMITS IT WHEN THE FORMATION'S TIME ON RECORD HAS RUN*, `RoundRule` says *NO VOTE BEFORE <day> · THE FORMATION'S TIME ON RECORD* while it runs; the Riksdag's log line names the day the Speaker submits it (VoteOn - 4) beside the vote's.
- **Question 2, taken:** the MEDIAN, [AUTHORED-DRAFT], whole days rounded down - equal to the mean today, robust as the record grows.
- **The owed run:** `AiMotionReachDiagnostic` clean on the staged state (n714e, with the formation checks and the text checks).
- **Latent, stated in COMPLETED §714:** (d) partly in-sample (2025's own 72 days in the median); "recent" is how far the record goes (Sweden rests on one formation); caretaker detection by the head's text; the wait sits after tabling (a VotePending proposal frozen for weeks, where the record's long phase is the talks).
