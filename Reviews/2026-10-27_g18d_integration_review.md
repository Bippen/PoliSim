# wave/g18d integration review - the merge fixes on main (2026-10-27)

**Files reviewed:** `Assets/Scripts/Simulation/SimulationManager.FormationProjection.cs` (money path), `Assets/Editor/ChamberVerdictCacheCheck.cs`, `Assets/Editor/UiLeverVerbCheck.cs`, `Assets/Editor/CreatedPartyReachDiagnostic.cs`, `Assets/Scripts/Testing/UiScreenshotDriver.cs`, `Assets/Editor/CheckSuite.cs` (staged hunk), with reference reads of `Assets/Scripts/Data/EnglishText.cs`, `Assets/Scripts/Elections/NationalElection.cs` (`Run`'s signature), `Assets/Scripts/Elections/FormationProposal.cs`, `Assets/Editor/FormationProjectionDiagnostic.cs` and `Assets/Scripts/UI/GameController.cs` (the draft's rebuild at line 9551).

Scope: the unstaged working-tree diff on top of the staged squash of wave/g18d, read only. ProjectSettings/ProjectAuditorSettings.asset line-ending noise ignored. Not run in Unity. The diff also touches `Tools/film_scope.tsv` and `Tools/review_ledger.tsv` (bookkeeping rows); they were not in the brief and are not judged here.

## Per item

### 1. SimulationManager.FormationProjection.cs - the possessive (money path)

`EnglishText.Possessive(string name, bool upper, bool abbreviation = false)`: for a non-empty name with `abbreviation == true` it returns `name + "'s"` when `upper` is false, never the apostrophe-alone form. So for every party key - "CDU", "SPD", "FDP", "AfD", "Linke", and also a key ending in s - the result is `KEY's`, and `"{0} candidate"` with `Possessive(key, false, true)` gives byte-for-byte the old `"{0}'s candidate"` text. The three sites are in `FirstBallot` (one `Steps.Add`, two `projection.Reason` assignments). Only the format string and one format argument change. `got`, `majority`, `Describe(government)`, the `if` tests, `Expect(...)`, `Partial`, `View` and the call order are the same, and the helper is pure (no random draw, no state). No figure, branch, stored value or draw moves. The capitalised headline `"THE BUNDESPRAESIDENT'S CANDIDATE ..."` has no name in it and stays as it was, which is right.

The one way the text can differ: a null or empty `proposal.Formateur`. The old code rendered "'s candidate", the new code " candidate". `FirstBallot` runs only on a tabled proposal, and the line above it puts `proposal.Formateur` into `candidates` and into the `Tally`, so that case cannot be reached. Both outputs would be wrong anyway. No consumer matches on the string: grep finds no `Contains("'s candidate")` or `Reason ==` on the projection under Assets/Scripts/UI or in the diagnostic. Correct and minimal.

### 2. ChamberVerdictCacheCheck.cs - the guard's receiver allow-list

`DirectWrite` captures the receiver identifier `(\w+)` before `.Cabinet/.Support/.PmParty/...`. The six hits in Assets/Scripts with receiver `projection` are all in SimulationManager.FormationProjection.cs (lines 222-224 and 253-255). There the local is `var projection = new FormationProjection {...}` (lines 58 and 155) or a `FormationProjection projection` parameter. The only other local of that name in Assets/Scripts is `PoliSim.Elections.FormationProjection projection` at GameController.cs:6701, and it does not write. No `GovernmentRecord` anywhere is named `projection` today, so the change opens no hole now. The message string was updated to match. Correct and minimal.

### 3. UiLeverVerbCheck.cs - `ProjectFormation` as a read

`ProjectFormation` sends work to `ProjectRound`, which builds its own `SpeakerRound` (not `RoundOf(country)`) and a new `FormationProjection`, or to `ProjectSejm`. FormationProjectionDiagnostic's `Moved(...)` compares before and after: the government record's reference, the round's reference and the Sejm's steps; the signature of divisions, cabinet/support/PM/caretaker/version/FormedOn, breaks, agreements, the round's stage, the Sejm's stage and the seats; the master seed and every stream's draw count. A planted break and a planted draw first prove that this check catches real changes. That diagnostic is now in the cheap group (CheckSuite). So "a read" is true and enforced. Correct.

### 4. CreatedPartyReachDiagnostic.cs - `counted` passed to `Run`

The signature is `Run(CountryId country, int turn, IReadOnlyDictionary<string,double> shares, DateTime date = default, double rosterShareOfVote = 1.0, ParliamentaryCount.Result counted = null, double[] regionRosterShare = null, bool logFallback = true)`. The call `Run(c, 0, counted.Shares, day, 1.0, counted)` puts every argument in the right place. `1.0` is the default roster share, so only `counted` changes. `counted` is null-checked before use. `countedSeats` is only printed and reported (line 146 adds to `crossings`, which by K123 is a REPORTED line, never a failure), so the change cannot flip the gate. The comment's "MERGE" note was rewritten as history. Correct and minimal.

### 5. UiScreenshotDriver.cs - seating `askedSupporter` with no post

`draft`, `rowsDraft` and `rowsBack` are all the same `_formationDraft` object. By the time the new branch runs, `rowsBack` has already removed `askedSupporter` from `Supporters` and `AcceptedDemands`, so the party is never both cabinet and supporter. `PostsOf` reads a missing or empty list as 0, and the explicit empty list is harmless. The selection loop skipped cabinet parties and supporters, so `askedSupporter` is never the formateur or the player's party.

After this block, the same film step closes the sheet, films `_speaker_asks` and puts back `roundRecord.Round = filmRound` and `PlayerPartyAbbrev`. GameController.cs:9551 rebuilds `_formationDraft` whenever the round reference or turn changes. Putting the film's round back therefore throws the edited draft away, the same way it already throws away the partner-stint edit made a few lines earlier. Nothing later reuses the edited draft.

## Findings

**Low L1 - the guard exempts a receiver by name, in every file.** `projection` (like `view` before it) is allowed anywhere in Assets/Scripts, whatever its type. Failure scenario: a future change writes `GovernmentRecord projection = country.Government; projection.Cabinet.Add(k);` in some other file. The guard counts it as a "projection" write and passes. The record's version does not move, and the chamber verdict cache serves the old government. No such code exists today (grep above). A tighter form would exempt `projection` only in `SimulationManager.FormationProjection.cs`. This is optional and does not block the merge.

**Low L2 - the film premise passes on any refusal, not only on "no post".** The check after the new branch requires only `!stintedAnswer.Accepts`. On the new path, `askedSupporter` is a party the formateur never invited. It may refuse on a red line or on its compatibility instead of on posts offered against posts expected. Failure scenario: Sweden's alone-formateur run films "a partner offered no post should refuse" while the party actually refuses for a red line, and the frame's caption tells the wrong story with the film still green. The reason is logged (`SHOT: ... refuses: {Reason}`), so a reader can catch it. Per the canvas-film rule, check that line in the next film log. Checking `PostsOffered == 0 && PostsExpected > 0` on the answer would make the premise exact. Does not block the merge.

**Low L3 - `askedSupporter`'s frozen `Tabled` entry remains in the draft.** `FreezeTabled` wrote `Tabled[askedSupporter]`, and the rows clean-up does not remove it. Now that the party sits in the cabinet, a verdict that read `Tabled` for cabinet parties would see demands. Failure scenario: none found. `Tabled` is read through `TabledOf(supporter)`, and the draft is thrown away when the round is put back. Noted only for completeness.

No High or Medium findings. No figure, branch, stored value or random draw moves on the money path.

## Verdict: READY
