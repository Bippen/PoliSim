# Review - §782, PS-6 item US-1: the US start says what it holds (2026-10-05)

A workflow review (`polisim-staged-review`) of US-1 - `WorldClock.NoElectionYet` and the folder card's line, `StartBrief`'s clause and ledger row, `NationalElection.NotHeldReason`, and `StartBriefDiagnostic`'s pins - in two lenses: the words' truth, and the checks' power and the film. Every finding was put to a refute-first skeptic; the reports are verbatim. A second pass read the change after the fixes. Not a money path (`Tools/bar_tier.ps1`'s pattern names none of these files), so no ledger row.

## The first pass - confirmed (verbatim)

The workflow polisim-staged-review on US-1's four files (the working tree's diff of WorldClock.cs, StartBrief.cs, NationalElection.cs and StartBriefDiagnostic.cs), two lenses (the words' truth, the checks' power and the film), every finding put to a refute-first skeptic.

### 1. The start card still promises the run-up to a 5 Nov 2024 presidential election the game never holds

- **Lens:** truth - **reviewer:** defect - **skeptic:** defect
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/StartPoints.cs:124

**The scenario.** Open the selector and click the United States folder. ShowStartPanel(USA) calls BuildStartCard, which draws three lines on the card. At CountrySelectorScreen.cs:621 the stamp is StartPoints.DateLine = "5 NOV 2024" (PollingDay = WorldClock.LatestElectionDay(USA)). At :624 the mode is StartPoints.ModeLine = "RUN-UP · OPENS 12 MAR 2024": ModeLine never consults WorldClock.NoElectionYet, and the USA is neither GoverningModeOnly nor IsSnapStart, so it falls through to "RUN-UP · ". At :638 the name is "Presidential election". Directly beneath, the ledger row now reads "Election · NONE IN THIS GAME YET · AS OF RECORD", so one sheet says both things. This sheet is the start panel that US-1's owed REAL film captures (UiScreenshotDriver 01f2_start_points calls ShowStartPanel). Plan §2.5 item 1's defect ("A US game advertises an election it never holds") therefore survives on the very frame US-1 films. Neither bar sees it. StartBriefDiagnostic.cs:72 asserts !Contains("RUN-UP") only on WorldClock.StartLine, which is the folder card's line (CountrySelectorScreen.cs:992), and StartPointsDiagnostic pins no mode line for the USA. NoElectionYet's own summary (WorldClock.cs:104-105) says "the start card, the brief and the not-held reason say so instead of promising a polling day". That claim is false for the start card.

**The fix proposed.** Route StartPoints.ModeLine through WorldClock.NoElectionYet, as it already does for GoverningModeOnly (e.g. "NO ELECTION YET · OPENS 12 MAR 2024"). Pin ModeLine(usa) with no "RUN-UP" in StartPointsDiagnostic or StartBriefDiagnostic. Judge the 5 NOV 2024 stamp and the "Presidential election" name on the 1280 real film. France's playable card keeps a past polling-day stamp beside a WHAT-IF mode line, so the stamp can stay once the mode line stops saying RUN-UP. Otherwise, narrow the NoElectionYet summary to what is true.

**The skeptic's evidence.** I could not refute it. I traced the path at HEAD plus the working tree; StartPoints.cs and CountrySelectorScreen.cs are unchanged.

1. **What the US card holds.** StartPoints.cs:84 builds the USA through `Ruled(id, "PRESIDENTIAL ELECTION", ...)`. Line 100 then sets Playable = true, PollingDay = `WorldClock.LatestElectionDay(USA)` (2024-11-05, WorldClock.cs:91) and Opens = `WorldClock.StartDate(USA)`. StartDate is `CampaignCalendar(5 Nov 2024).PreCampaignStart`, which is 8 + 26 weeks earlier: 12 Mar 2024.

2. **The mode line still says RUN-UP.** `StartPoints.ModeLine` (StartPoints.cs:119-125) has no `NoElectionYet` branch. `GoverningModeOnly(USA)` is false (WorldClock.cs:101) and `IsSnapStart(USA)` is false (:98). So line 124 returns "RUN-UP · OPENS 12 MAR 2024".

3. **The sheet draws it next to the new row.** Every folder click calls `ShowStartPanel` (CountrySelectorScreen.cs:177), which calls `BuildSheet`. `BuildStartCard` then draws:
   - :621, the DateStamp `StartPoints.DateLine` = "5 NOV 2024";
   - :624, the Mode = `StartPoints.ModeLine` = "RUN-UP · OPENS 12 MAR 2024";
   - :638, the Name = "Presidential election".

   Beneath the card, `BuildBriefLedger` (:503, :792) draws `StartBrief.Rows`, which now include "Election · NONE IN THIS GAME YET · AS OF RECORD" (StartBrief.cs:201-204). One frame says both things.

4. **The changed line is not on this card.** `WorldClock.StartLine` is drawn only on the folder card (CountrySelectorScreen.cs:992). `StartPoint.Line` is never drawn on a playable card, because `StartPoints.Reason` returns null when Playable (StartPoints.cs:130).

5. **The build runs no US run-up.** `TryNextPollingDay(USA)` returns false: after the Germany and Poland branches, `if (id != CountryId.Sweden) { return false; }`. So `TryPlayerPollingDay` is false and `CurrentCampaignCalendar` is null. `AdvanceCampaign` then returns at SimulationManager.cs:4246 with the comment "no election calendar for this country yet - no run-up and no campaign, as there is no election". "RUN-UP" therefore names a mode the build never runs, and with the 5 NOV 2024 stamp it still advertises the election.

6. **The new summary's claim is false.** `NoElectionYet`'s new summary (WorldClock.cs:103-105) says "the start card, the brief and the not-held reason say so instead of promising a polling day; the card stays playable". The card that "stays playable" is the StartPoint card that `BuildStartCard` draws, and that card still carries the polling-day stamp and RUN-UP.

7. **No bar sees it.**
   - StartBriefDiagnostic.cs:72 checks `!usaLine.Contains("RUN-UP")` only on StartLine.
   - StartPointsDiagnostic pins ModeLine only for France (:53) and Sweden (:55); :44 only requires it to be non-null.
   - The owed film shot 01f2_start_points (UiScreenshotDriver.cs:346-363) calls `ShowStartPanel`. Its `RecordCanvasTextAssert` is `CanvasTextGuard`, which checks geometry, not content.

The plan's US-1 text points to "the card's line (`WorldClock.StartLine`)". That is the folder's line, not the start card's line, so the change met the plan's wording but not its purpose (§2.5 item 1).

**The skeptic's corrected fix.** Add a branch to `StartPoints.ModeLine` after the GoverningModeOnly one:

`if (WorldClock.NoElectionYet(p.Country)) { return "NO ELECTION YET · " + opens; }`

This uses the same words as StartLine. It fits the lane: France's longer WHAT-IF line already sits in the same head row.

Then:
- Name the US case in ModeLine's 18a summary.
- Pin it in StartPointsDiagnostic beside the Sweden and France pins, for example `ModeLine(usa).StartsWith("NO ELECTION YET · ") && !ModeLine(usa).Contains("RUN-UP")`, or the literal string in that file's house style. Or pin it in StartBriefDiagnostic's US-1 block.
- Optionally widen the folder-card comment at CountrySelectorScreen.cs:990-991 to cover the no-election-yet case.

Judge the "5 NOV 2024" stamp and the "Presidential election" name on the owed REAL 1280 film (01f2_start_points). France keeps a past polling-day stamp beside WHAT-IF, but the USA's stamp is in the game's future, so the film (or Design) should decide whether it needs a word.

If ModeLine is left as it is in this item, narrow `NoElectionYet`'s summary to what is true: the selector's folder line, the brief and the not-held reason. Also correct the plan's US-1 pointer, which names `WorldClock.StartLine` as the start card's line.

### 2. 'Hold as of record' is true only in the as-at-start sense (R-US1 (b)), not in the by-date sense of R-US1 (a), the ruling installed: a US game keeps Biden and the 118th House for the whole run

- **Lens:** truth - **reviewer:** defect - **skeptic:** defect
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/StartBrief.cs:115

**The scenario.** Open the USA (epoch 2024-03-12), seat DEM and step past 2025-01-20.

(1) The government is set once. Country.Government is written only at the epoch (WorldFactory.cs:1092; GameController.cs:2188 and 2230). GovernmentRecord.AtStart reads SeatedGovernment, which installs Biden with a DEM cabinet.

(2) Every in-run writer of the government is closed to the USA:
- InstallSuccessor is the Bundestag's.
- The Speaker round needs RoundsApply (Riksdag or Bundestag rules). ConfidenceProcedure.RulesOf(USA) is Unsourced, so MoveNoConfidence refuses at SimulationManager.cs:2831.
- The post-election formation (GameController.cs:6429) needs PollingDayToday. That never rises for the USA, because TryNextPollingDay returns false for it (WorldClock.cs:435).

(3) The chamber is set once. ParliamentSeats are written only at seeding (WorldFactory.cs:1032; ParliamentSystem.UpdateSeats is idempotent) and by SetSeatsFromElection after a held election (GameController.cs:6692).

(4) No runtime reader resolves the US government by the current date.

So in 2025-26 the world still seats the Usa2022 table (REP 222 / DEM 213) and Biden's DEM administration. The Desk role chip reads GOVERNING for a DEM player and IN OPPOSITION for a REP one (GameController.Desk.cs:282-285). Meanwhile WorldClock's own record has the Usa2024 chamber from 2025-01-03 and Trump (REP) from 2025-01-20.

R-US1 (a), ruled with this change, defines 'as of record' by date (USA_STAGE_PLAN.md:460-465: "the record's own reading of 'as of record'"). What the build does is option (b), "As at the start ... said on screen". So the brief clause, the ledger's "AS OF RECORD" (StartBrief.cs:203), the not-held reason (NationalElection.cs:416) and the NoElectionYet summary (WorldClock.cs:104) are all false from 3 Jan 2025 for the House and from 20 Jan 2025 for the president, until US-2 lands. US-2's done-when already owes "US-1's words updated".

**The fix proposed.** Until US-2 lands, state option (b) plainly. For example:
- Clause: "No election is held in this game yet - the president and the House seated on this date hold for the whole game; the record's later Congress and president are not seated yet."
- Ledger: "NONE IN THIS GAME YET · THE START'S PRESIDENT AND HOUSE HOLD".
- NotHeldReason and the NoElectionYet summary: the same meaning.

Alternatively, land US-2 before or with US-1 so that 'as of record' becomes true by date.

**The skeptic's evidence.** I could not refute the finding. I traced every path and it holds: a US game keeps the start's president and House for the whole run, and the new words claim the by-date record that R-US1 (a) ruled.

**1. The government is written only at the epoch.**
- `WorldFactory.cs:1092` calls `GovernmentRecord.AtStart(country, EpochDate)`. `GameController.cs:2188` and `:2230` do the same. `SeatedGovernment.TryFor` also reads `EpochDate`.
- Every in-run writer is closed to the USA:
  - `ConfidenceProcedure.cs:34` returns `Unsourced` for the USA. So `MoveNoConfidence` refuses (`SimulationManager.cs:2831`), and `RoundsApply` (`:3230-3232`) is false, which rules out `Install` (`:3974`). `InstallSuccessor` is reached only by the Bundestag.
  - The formation at `GameController.cs:6429` needs an election record dated today. `CheckElection` returns at `:6352` unless `PollingDayToday` is true. That flag comes from `TryPlayerPollingDay` (`SimulationManager.cs:477`, `:4210`), and `WorldClock.TryNextPollingDay` returns false for the USA (`WorldClock.cs:435`).
  - No extra election is possible. `ScheduleExtraElection` is called only at `:2898` and `:4005`, and both need a Riksdag declaration or a Speaker round.
  - `TwoRoundElection.RuleOf(USA)` is null (`TwoRoundElection.cs:63`), and `PresidencyOfRecord` covers Poland only.

**2. The chamber is set once.**
- `WorldFactory.cs:1032` seats `InitialSeats(Seated)`, which resolves to `SeatedVintage(USA, 2024-03-12)` = `Usa2022` (REP 222 / DEM 213, `PartySystem.cs:773`).
- After that, only `SetSeatsFromElection` changes it (`GameController.cs:6692`, after a held election). `UpdateSeats` is idempotent.

**3. A US run continues past both dates.** It ends only on an election verdict, so it passes 2025-01-03 and 2025-01-20.

**4. The record moves; the game does not.**
- The record has `Usa2024` from 2025-01-03 (`WorldClock.cs:169`) and Trump (REP) from 2025-01-20 (`:270`).
- The Desk role chip reads `Country.Government` (`GameController.Desk.cs` `RoleChip`). In 2025-26 it still shows GOVERNING for a DEM player and IN OPPOSITION for a REP one.

**5. The ruled meaning of "as of record".**
- `USA_STAGE_PLAN.md`, R-US1: "Today a US game keeps the start's: Biden and the 118th House govern for the whole run." Option (a), by date, "is the record's own reading of 'as of record'", and (a) is what was RULED. Option (b) says the start's state is "said on screen".
- §777 lists "Biden and the 118th House govern for the whole run" as "wrong or misleading today".
- US-2 "Needs: US-1", and its done-when owes "US-1's words updated". US-1 therefore lands with these words untrue from 3 Jan 2025 (the House) and from 20 Jan 2025 (the president).

**6. Two further overclaims.**
- "Congress" claims a Senate the game does not have. No US Senate is modelled anywhere (§777: "missing: ... no Senate"; US-15 builds it). So "the president and Congress hold" overclaims even on the start date.
- On the screen, the row "Election · NONE IN THIS GAME YET · AS OF RECORD" reads as if the record's 2024 result takes effect.

**Where the finding overstates.** Of the four places it names, only the ledger row is drawn (`StartBrief.cs:203`, through `CountrySelectorScreen.cs:792`).
- `StartBrief.Text`/`Clauses` (`:115`) are read only by `StartBriefDiagnostic`.
- `NotHeldReason(USA)` (`NationalElection.cs:416`) is stored only by `GameController.cs:6681`, which a US game never reaches, and no UI reads it.
- `WorldClock.cs:104` is a comment, so it falls under the claim convention rather than the screen.

The visible row is still the deliverable that US-1's real 1280 film captures, which is why this stays a defect.

**Outside this finding.** The US start-point card's head still shows a date stamp of "5 NOV 2024" and the mode line "RUN-UP · OPENS 12 MAR 2024" (`StartPoints.DateLine`/`ModeLine`, `CountrySelectorScreen.cs:621-624`), under the name "Presidential election". The diagnostic's `!usaLine.Contains("RUN-UP")` checks only `StartLine`.

**The skeptic's corrected fix.** Until US-2 lands, the words should say what the build does: R-US1 (b)'s state, said on screen. They should also name only the House, because no Senate is modelled. Alternatively, land US-2 in the same commit, so that "as of record, by date" is true.

**Proposed wording**
- **The clause (`StartBrief.cs:115`):** "No election is held in this game yet - the president and the House seated on this date hold for the whole game; the record's later House and president are not seated yet, and the Senate is not modelled."
  - If the later dates are named, derive them from the next entries after `opens` in `WorldClock.Chambers(id)` and `WorldClock.Governments(id)`. Do not type them: the brief is derived, never written by hand.
- **The ledger row (`StartBrief.cs:203`):** "NONE IN THIS GAME YET · THE START'S PRESIDENT AND HOUSE HOLD".
- **`NationalElection.NotHeldReason(USA)`:** "No US election is held in this game yet: the president and the House seated at the start hold until the record is seated by date (US-2); the Electoral College count and the House races are not built, and the Senate is not modelled."
- **`WorldClock.NoElectionYet`'s summary:** the same meaning, with no "as of record" claim, pointing at US-2 for the by-date seating.

**Then**
- Re-pin `StartBriefDiagnostic` to the new words, keeping its check that no polling-day clause appears.
- US-2 rewrites all of these to R-US1 (a)'s by-date sentence, as its done-when already owes.

**Separately:** the US start-point card's mode line and date stamp should stop saying "RUN-UP" and showing the 5 Nov 2024 polling day for a start whose election the game does not hold.

### 3. 'Congress' claims a Senate the build does not hold in any form

- **Lens:** truth - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/StartBrief.cs:115

**The scenario.** There is no US Senate in code. In Assets/Scripts, 'Senate' appears only in:
- Poland's notices (WorldClock.cs:89 and 381)
- an Italian electorate note (CohortVoterGroups.cs:43)
- the [SEN-DATES] basis tag
- the new string

The US chamber is the House's 435 (PartySystems.SeatsAt Usa2020/2022/2024; StartBrief.ChamberOf = 'House'), and plan §2.1 says "no Senate anywhere in code". So "the president and Congress hold as of record" (StartBrief.cs:115, NationalElection.cs:416, WorldClock.cs:104) asserts a Senate of record the game never holds, in either sense of 'as of record'. Neither string reaches a screen today: StartBrief.Clauses/Text are read only by StartBriefDiagnostic, and NotHeldReason(USA) is read only by RunNationalElection, which a US game never reaches. The overclaim therefore lives in pinned text and comments, and it surfaces the moment either string is drawn.

**The fix proposed.** Say "the president and the House" (optionally adding "no Senate is modelled yet") until US-15 seats a Senate of record. Re-pin StartBriefDiagnostic's strings to match.

**The skeptic's evidence.** THE WORDS (diff vs d00b66aa):
- StartBrief.cs:115: "No election is held in this game yet - the president and Congress hold as of record."
- NationalElection.cs:416-417: "No US election is held in this game yet: the president and Congress hold as of record until the Electoral College count and the House and Senate races are built." Its own summary at :404 says this text is "for a screen".
- WorldClock.cs:104, the NoElectionYet doc: "Its president and Congress hold as of record meanwhile (§618's ruling 4)". §618's ruling 4 actually says "its chamber and head of state hold as of record" (USA_STAGE_PLAN.md §1.3). So the comment paraphrases the ruling it cites wrongly.
- StartBriefDiagnostic.cs:71 and :73 pin both "Congress" sentences as pass conditions.

NO SENATE IN ANY FORM. I searched Assets/**/*.cs for Senate, Senator, upper chamber, second chamber, bicameral and Bundesrat. The only hits are:
- Poland's PKW notices (WorldClock.cs:89, :381)
- Italy's voting-age note (CohortVoterGroups.cs:43)
- a constitutional quote on how electors are apportioned (ElectoralCollege.cs:15). The finding missed this one, but it is not a Senate either.
- a Polish veto comment in an Editor diagnostic (PresidentialVetoDiagnostic.cs:225)
- the new strings.
The US chambers of record (WorldClock.cs:167-169) are House vintages only (Usa2020, Usa2022, Usa2024). There, [SEN-DATES] is the Senate's dates-of-sessions table, cited only for the House's Congress dates. StartBrief.ChamberName[USA] = "House" (:27) and ChamberFullName = "House of Representatives" (:46). The brief's own other clauses name only the House. The plan agrees: §2.1 says "no Senate anywhere in code" and §2.5(4) says "Bills and budgets pass by the House alone".

THE HOUSE HALF IS ONLY TRUE AS AT THE START. Nothing re-seats anything by date. WorldFactory seats the chamber at the epoch (:1031), and GovernmentRecord.AtStart sets the government (WorldFactory:1092, GameController:2188/2230). The government swaps at SimulationManager 3142 and 3989 are the Riksdag/Bundestag confidence rounds. So the 118th House and Biden stand for the whole run. "As of record" is therefore true for the House only in §618's as-at-start sense, and the Senate is held in no sense at all.

NOT ON SCREEN TODAY (the finding's concession holds):
- CountrySelectorScreen draws only StartBrief.Head, Rows and Tagline (:789, :792, :817) and WorldClock.StartLine (:992). The US rows are President, Since, House, Election ("NONE IN THIS GAME YET · AS OF RECORD") and In the House, with no "Congress". The line is "OPENS … · NO ELECTION YET".
- StartBrief.Clauses and Text are read only by StartBriefDiagnostic, which writes them to the Unity log.
- NotHeldReason(USA) is reached only by GameController.RunNationalElection (6681/6683), and that runs only when PollingDayToday is set. TryNextPollingDay returns false for the USA (WorldClock.cs: "if (id != CountryId.Sweden) return false" after the Germany and Poland branches). ConfidenceProcedure.RulesOf(USA) is Unsourced (ConfidenceProcedure.cs:34), so SimulationManager.cs:2831 refuses any declaration and no extra election can be ordered.
- No UI reads ElectionRecord.NotHeldReason; only ElectionDayReachDiagnostic:101 does.

SEVERITY: minor. The sentence is false about the build, and it is pinned as a pass condition and sits in a doc comment that misquotes §618, under a claim convention that governs every source comment. But no player sees it today, and the US-1 film cannot show it. The word comes from the plan's own US-1 line ("the president and Congress as of record"), so the plan carries the same overclaim.

**The skeptic's corrected fix.** Replace "Congress" with "the House" until US-15 seats a Senate of record:
- StartBrief.cs:115: "No election is held in this game yet - the president and the House hold as of record; no Senate is modelled yet." This is still one clause, so the four-clause check holds.
- NationalElection.cs:416: "No US election is held in this game yet: the president and the House hold as of record, and no Senate is modelled, until the Electoral College count and the congressional races are built."
- WorldClock.cs:104 doc: quote §618's ruling 4 as written ("its chamber - the House - and head of state hold as of record") and say that no Senate is held.
- Re-pin StartBriefDiagnostic.cs:63, :71 and :73 to the new strings.
- Amend US-1's line in docs/specs/USA_STAGE_PLAN.md ("the president and the House as of record") so the plan and the build agree. US-15's done-when already owes the Senate clause.

Keep the wording compatible with the separate by-date issue. Until US-2 lands, the House and the president hold as seated at the start (the 118th and Biden for the whole run), so whoever fixes that finding may also qualify "as of record".

### 4. On screen nothing says what holds: the ledger's 'AS OF RECORD' has no subject and no date, and the diagnostic's 'said alike' comment is untrue of the card's line

- **Lens:** truth - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/StartBrief.cs:203

**The scenario.** A player sees only two US-1 strings:
- the folder line "OPENS 12 MAR 2024 · NO ELECTION YET" (CountrySelectorScreen.cs:992)
- the ledger row "Election · NONE IN THIS GAME YET · AS OF RECORD" (CountrySelectorScreen.cs:792-815 draws StartBrief.Rows, never the clauses)

Neither names the president or Congress. 'AS OF RECORD' has no subject and no date. Every other on-screen 'as of the record' is dated by WorldClock.RecordDate (WorldClock.cs:362-363 and 516; ElectionNightScreen.cs:1135), and on that date the record's president is Trump and the House is the 119th. In the Election slot the phrase reads naturally as 'the election goes as the record has it', which the build does not do. StartBriefDiagnostic.cs:62-63 says the holding is "said alike by the card's line, the brief and the not-held reason". The card's line does not say it.

**The fix proposed.** Give the holding its own subject. For example, keep 'Election · NONE IN THIS GAME YET' and add a 'Holds' row reading 'THE START'S PRESIDENT AND HOUSE' (by date once US-2 lands). Correct the diagnostic comment to say what each surface actually says.

**The skeptic's evidence.** THE SCENARIO HOLDS.
- Only two of the changed strings reach the screen.
  - The folder card (BuildFolderCard, CountrySelectorScreen.cs:992) draws WorldClock.StartLine(USA). WorldClock.cs:127 makes that "OPENS 12 MAR 2024 · NO ELECTION YET" (12 Mar 2024 is 5 Nov 2024 minus 34 weeks).
  - BuildBriefLedger (CountrySelectorScreen.cs:789-822) draws Head, Rows and Tagline, and Tagline is null. StartBrief.Text and Clauses are called only by StartBriefDiagnostic, so the clause "the president and Congress hold as of record" is never on screen.
  - NotHeldReason(USA) is never reached. TryNextPollingDay returns false for the USA (WorldClock.cs:435), so PollingDayToday is never set (SimulationManager.cs:477) and CheckElection returns early. Even when it is built, it is only stored and logged (GameController.cs:6681-6683).
- The US ledger reads, in order:
  - President · Joseph R. Biden Jr. (DEM)
  - Since · 20 JAN 2021
  - House · REP · 222 OF 435
  - Election · NONE IN THIS GAME YET · AS OF RECORD (StartBrief.cs:203)
  - In the House · 2 PARTIES · 435 SEATS · ELECTED 8 NOV 2022
- What a US game does: GovernmentRecord.AtStart seats the government once, at the epoch (WorldFactory.cs:1092). It is replaced only by the Riksdag/Bundestag installs (SimulationManager.cs:3142, 3989) or after an election the game held (GameController.cs:6429, 6692). Nothing reseats a chamber or swaps a president by date, so Biden and the 118th House stay for the whole run.
- The plan's own R-US1 text says the same thing: "Today a US game keeps the start's: Biden and the 118th House govern for the whole run". It also calls the by-date reading (a) "the record's own reading of 'as of record'", and that is the reading Elias ruled. So the subjectless "AS OF RECORD" is ambiguous at best. Read the way the project now defines the phrase, it promises the switch to the 119th Congress and Trump, which is US-2's work and not built.
- Every other on-screen "AS OF" carries a date: WorldClock.cs:516, ElectionNightScreen.cs:1135, GameController.CampaignDeclared.cs:180. WorldClock.cs:362 defines an on-screen "as of the record" as RecordDate, 24 Sep 2026, when the record has Trump and the Usa2024 chamber (WorldClock.cs:169-170, 270).
- The comment is false. StartBriefDiagnostic.cs:62-63 says "the president and Congress hold as of record, said alike by the card's line, the brief and the not-held reason". Its own check at :72 pins only EndsWith(" · NO ELECTION YET"), and the card's line does not mention what holds. WorldClock.cs:104-105 makes the same over-claim ("the start card, the brief and the not-held reason say so").

WHAT IS OVERSTATED.
- "On screen nothing says what holds" goes too far. The President, Since and House rows sit directly above the Election row and name the start's president and House, and the ledger head is dated "BRIEF · UNITED STATES, 12 MAR 2024".
- What is actually missing: the row's subject, and the fact that these people hold for the whole run. "Congress" also implies a Senate, and the game seats none.

RELATED, NOT THIS FINDING. The start card itself (BuildStartCard) still shows the stamp "5 NOV 2024", "RUN-UP · OPENS 12 MAR 2024" and "Presidential election". StartPoints.DateLine and ModeLine (StartPoints.cs:115-125) have no NoElectionYet branch. So the card that US-1's 1280 film is meant to show still promises a run-up to a polling day the game does not hold.

**The skeptic's corrected fix.** 1. In StartBrief.Rows (StartBrief.cs:203), give what holds its own subject and make it true today. Keep "Election · NONE IN THIS GAME YET" and add a row such as "Holds · THE PRESIDENT AND HOUSE ABOVE, FOR THE WHOLE RUN".
   - Say House, not Congress: no Senate is seated until US-15.
   - US-2 rewrites the row to by-date, as its Done-when already requires ("US-1's words updated").
   - Update the pins at StartBriefDiagnostic.cs:67/75.
2. Bring the clause (StartBrief.cs:115) and NotHeldReason (NationalElection.cs:416) into line with the same honest wording. Today's sense is "the start's president and House hold"; R-US1's by-date sense stays out until US-2.
3. Fix the comments.
   - StartBriefDiagnostic.cs:62-63: say what each surface says. The card's line says only that no election is held yet; the ledger rows, the brief and the not-held reason say what holds.
   - WorldClock.cs:104-105: the same, and say "the folder card's line", not "the start card". BuildStartCard does not draw StartLine.
4. Separate finding: add a NoElectionYet branch to StartPoints.ModeLine and DateLine, so the start card stops showing "RUN-UP" and the "5 NOV 2024" polling-day stamp. Then pin it next to the existing !usaLine.Contains("RUN-UP") check.

### 5. NoElectionYet's summary implies the USA is the only start without a held election; Italy's start holds none either

- **Lens:** truth - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/WorldClock.cs:103

**The scenario.** Italy's playable start never holds an election either: TryNextPollingDay is false for it (WorldClock.cs:435; asserted at PollingDayDiagnostic.cs:82-85) and its confidence rules are Unsourced, so no extra election is possible. Yet it still promises one:
- folder line: "OPENS 21 JUL 2022 · THE SNAP ELECTION OF 25 SEP 2022"
- start card: "SNAP ELECTION · OPENS 21 JUL 2022"
- brief: "Polling day is 25 September 2022."

The new check at StartBriefDiagnostic.cs:76 ("the countries whose elections the game holds keep their polling day") covers Sweden and Poland only, so Italy falls in neither class and is not asserted. This is pre-existing and outside US-1's scope. Still, the summary's "a start whose elections the game does not hold yet - the USA's" reads as exhaustive, and it is not.

**The fix proposed.** Say in the summary that Italy's start also promises a polling day the game does not hold (as its own item), or file it. Do not widen NoElectionYet to Italy without Italy's own words, because the US clause names a president and Congress.

**The skeptic's evidence.** I could not refute it. Every factual premise checks out against the code. It stays a note because the diff neither causes nor worsens the Italy problem.

1. What the summary says. WorldClock.cs:103-106 describes the predicate as a class: "a start whose elections the game does not hold yet - the USA's ...". The body is `public static bool NoElectionYet(CountryId id) => id == CountryId.USA;`.

2. An Italy game never holds an election.
   - **No polling day.** WorldClock.TryNextPollingDay handles Germany and Poland, then hits `if (id != CountryId.Sweden) { return false; }` (working tree line 435; the same guard is at line 429 in HEAD, so this predates US-1). PollingDayDiagnostic.cs:82-84 asserts that Italy, the USA and France have no polling day and no basis.
   - **No extra election.**
     - ConfidenceProcedure.cs:34: `RulesOf` returns Unsourced for Italy.
     - SimulationManager.MoveNoConfidence refuses with "THIS COUNTRY'S CONFIDENCE RULES ARE NOT YET MODELLED".
     - TryAiMotion returns early unless the rules are Riksdag or Bundestag.
     - RoundsApply (line 3230) is false, so BreakOff never runs.
     - The only places that set NoConfidenceOn (lines 2865, 3050) sit behind those gates. So ScheduleExtraElection and OrderExtraElection cannot be reached.
   - **No election night.** TryPlayerPollingDay (lines 4207-4213) is always false, so PollingDayToday (line 477) is never set and GameController.CheckElection (line 6349) never runs. The record agrees: COMPLETED.md's economic-vote table has "Italy 2022-07-21 ... no election on the calendar - never judged".

3. Italy's screens still promise the election.
   - StartLine (IsSnapStart is true for Italy) gives "OPENS 21 JUL 2022 · THE SNAP ELECTION OF 25 SEP 2022". It appears on the folder card (CountrySelectorScreen.cs:992) and as the StartPoint's Line.
   - StartPoints.ModeLine gives "SNAP ELECTION · OPENS 21 JUL 2022" (CountrySelectorScreen.cs:624).
   - NoElectionYet(Italy) is false, so StartBrief falls into the else branch. Italy's PollingDay is LatestElectionDay = 2022-09-25, later than the 2022-07-21 opening. The brief therefore prints "Polling day is 25 September 2022." and the ledger row "Polling day · 25 SEP 2022".

4. The new test does not cover Italy. StartBriefDiagnostic.cs:76-77 checks only Sweden and Poland under "the countries whose elections the game holds", so Italy is in neither class.

Why this is only a note:
- The summary has no effect at runtime.
- Its scope can be worked out from the text: it says "the USA's", names US-8 and US-16, and says "Its president and Congress".
- The fault is that the opening phrase defines the predicate by a class that Italy also belongs to (France does too, but GoverningModeOnly gives France its own honest "NO ELECTION" words). The body only tests for the USA.
- The new inline comments read like a general rule while Italy's branch two lines away breaks it: StartLine:127 says "no polling day promised that the game does not hold", and StartBrief:114 says "never a polling day it does not hold".
- PS-7's row lists "the snap start" as owed, but nothing records that Italy's card promises 25 Sep 2022.

Aside, outside this finding but under the same lens: US-1 left StartPoints.ModeLine alone. The USA's start card head still reads "5 NOV 2024" (the DateLine stamp) followed by "RUN-UP · OPENS 12 MAR 2024" (CountrySelectorScreen.cs:620-624), with the name "Presidential election". That is a run-up to a polling day the game does not hold, on the very card US-1's done-when films. The new check (`!usaLine.Contains("RUN-UP")`) tests StartLine only, not ModeLine.

**The skeptic's corrected fix.** Make the summary name its scope rather than define a class. For example: "PS-6, US-1: the USA's start. The game holds none of its elections yet, until its presidential count (US-8) and Congress's (US-16) are built ... This is not a general 'is this start's election held' test." Either point to Italy as a TRACKING item ("Italy's snap start, PS-7, still names a polling day the game does not hold") or leave Italy out of the code comment.

File Italy's on-screen promise as its own errand, or add it to the PS-7 row. Five surfaces need US-1's kind of fix, in Italy's own words: the folder line, the mode line, the brief clause, the ledger row and the diagnostic. The evidence: TryNextPollingDay gives Italy no polling day, and its confidence rules are Unsourced, so no extra election can be ordered either.

Do not widen NoElectionYet to Italy. Its clause, row and not-held reason name a president and Congress and cite §618's ruling 4. Italy would need its own words (the Camera and the government of record) and its own branch in the snap-start StartLine and ModeLine.

Separately, under the same US-1 lens: route StartPoints.ModeLine through NoElectionYet as well, so the US card's head stops reading "RUN-UP · OPENS 12 MAR 2024", and pin that in StartBriefDiagnostic.

### 6. The old House-system reason is gone from code except in NationalElection's class header, which now misstates what the USA's record carries

- **Lens:** truth - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/NationalElection.cs:57

**The scenario.** Only the USA branch changed; France, Italy and the default are as before, and no test or reader pinned the old string. The class header still gives the USA's reason as "435 SINGLE-MEMBER DISTRICTS, first past the post" and says that "for those four ... the record carries the reason" (NationalElection.cs:57-60). The record now carries "No US election is held in this game yet ..." instead. Apart from that header, the old description survives only as the 2 U.S.C. §2c fact in ElectionsData/usa/returns_2024.md:56. Separately, USA_STAGE_PLAN.md:235 (US-8) says NotHeldReason will change "instead of its House-only line", but US-1 has already removed that line.

**The fix proposed.** Rewrite the header's USA bullet to say why no vote is held (no vote model, no Senate, no state layer), or point it at NotHeldReason. Reword US-8's sentence.

**The skeptic's evidence.** Partly real. The header half is overstated, and the plan half is real but trivial.

1) The class header does not misstate the USA. NationalElection.cs:57-58 says "USA is 435 SINGLE-MEMBER DISTRICTS, first past the post. A proportional allocation of the national House vote is famously not what that produces." That is a statement about US law and why the national PR allocator is not run, and it is still true. Line 60 says "For those four the chamber is left untouched and the record carries the reason." For the USA that is also still true: in Run's default case (398-400) `record.NotHeldReason = NotHeldReason(country);` sets the reason. The field's only reader, ElectionDayReachDiagnostic.cs:101, checks `!string.IsNullOrEmpty(record.NotHeldReason)`. No pin of the old string exists: a repo grep finds it only in the header, the exempt COMPLETED.md:7576 and returns_2024.md:56. The header paraphrased the old reason but never claims what text the record holds. For the USA it is now incomplete (it gives a House-only reason, while the code's reason covers the president and Congress), not false. The claim convention accepts incomplete ("no document becomes wrong - only incomplete").

What is actually false in that header predates this change and goes unnamed in the finding. Lines 43-51 and 60 say "TWO OF SIX, AND THE OTHER FOUR", with POLAND listed as not built. Poland is built: `PolandDistricts` (line 15) and `case CountryId.Poland` (390-396). Both landed in s762 (46debcd6), which removed Poland's NotHeldReason case and left the header as it was. `git diff HEAD` shows lines 39-63 unchanged.

In a US game the record never exists anyway. WorldClock.cs:435 `if (id != CountryId.Sweden) { return false; }` means TryNextPollingDay(USA) is false. `ConfidenceProcedure.RulesOf` gives the USA Unsourced (ConfidenceProcedure.cs:34). So GameController.cs:6675-6683 never writes a US record, and nothing on screen depends on this header.

2) The plan sentence is real. USA_STAGE_PLAN.md:235 (line 232 at HEAD), under US-8, says "Congress stays as of record by date (US-2), and `NationalElection.NotHeldReason` says so instead of its House-only line." After US-1, NationalElection.cs:416-417 reads "No US election is held in this game yet: the president and Congress hold as of record until the Electoral College count and the House and Senate races are built." That is not House-only, so the line US-8 names as its predecessor no longer exists. US-1's code is what made this descriptor false. The plan was also inconsistent with itself, since its own US-1 (line 169) replaces that line before US-8. Line 113 (§2: "naming the House alone") is covered by the plan's own status disclaimer that §2's code facts "go stale as PS-6 lands".

No behaviour, test, film or on-screen word is affected.

**The skeptic's corrected fix.** Owed: in USA_STAGE_PLAN.md US-8 (line 235), change "instead of its House-only line" to "in place of US-1's line", or drop the clause. This is a documents-tier edit. Optional, and not owed by US-1: if the NationalElection class header is touched, fix the stale part, which is Poland and the count, not the USA bullet. Under the claim convention, replace the transcribed "TWO OF SIX ... THE OTHER FOUR" and the per-country list with a pointer: the countries with a live path are Run's cases, and every other country's reason is `NotHeldReason`. The USA bullet can stay as a sourced fact about US law (2 U.S.C. §2c, per ElectionsData/usa/returns_2024.md), or become "no US election is held yet; see NotHeldReason".

### 7. 'NO ELECTION YET' on the folder card reads in the world's tense

- **Lens:** truth - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/WorldClock.cs:127

**The scenario.** Beside "OPENS 12 MAR 2024", "NO ELECTION YET" reads as 'no election has happened yet as of the opening', i.e. one is coming. The start card's "RUN-UP" and its "5 NOV 2024" stamp reinforce that reading. The brief and the ledger say "in this game"; the card's line drops it, which is the one qualifier that removes the promise.

**The fix proposed.** Use "NO ELECTION IN THIS GAME YET" or "NO ELECTION HELD", and measure it on the 1280 real film; the line's own comment records France's longer line wrapping at 1280.

**The skeptic's evidence.** The finding is real, and I could not refute any code fact in its scenario. I re-grade it from note to minor, because the start card it calls "reinforcing" is where the uncorrected claim actually sits.

1. **Folder line.** `WorldClock.cs:127` reads `if (NoElectionYet(id)) { return $"OPENS {opens} · NO ELECTION YET"; }`. `StartDate(USA)` is `new CampaignCalendar(LatestElectionDay(id)).PreCampaignStart` (line 117). That is 5 Nov 2024 (line 91) less 8+26 weeks, so the line reads "OPENS 12 MAR 2024 · NO ELECTION YET". `CountrySelectorScreen.cs:992` draws it on the folder card. The other lines in that grid all name a date on the world's calendar ("THE RUN-UP TO 13 SEP 2026", "THE SNAP ELECTION OF 23 FEB 2025"), so "YET" in that slot reads in the world's tense. "IN THIS GAME" appears only in the brief (`StartBrief.cs:115`, drawn nowhere on screen; only `StartBriefDiagnostic` reads `StartBrief.Text`) and in the ledger row (`StartBrief.cs:203`).

2. **Start card.** The folder click opens `ShowStartPanel` (`CountrySelectorScreen.cs:177`), which builds the start card:
   - `:621` `BuildStamp(..., StartPoints.DateLine(point), ...)` gives "5 NOV 2024" (`StartPoints.cs:116`, with `PollingDay = LatestElectionDay` at `:100`).
   - `:624` `StartPoints.ModeLine(point)` gives `(WorldClock.IsSnapStart(p.Country) ? "SNAP ELECTION · " : "RUN-UP · ") + opens` (`StartPoints.cs:124`). The USA is neither snap nor `GoverningModeOnly`, so the card reads "RUN-UP · OPENS 12 MAR 2024".
   - The card's name is "Presidential election" (`StartPoints.cs:84`).
   - `StartPoints.cs` and `CountrySelectorScreen.cs` are not in the diff. Only the ledger under the card says "Election · NONE IN THIS GAME YET · AS OF RECORD", so the sheet contradicts itself.

3. **No US election is held.** `TryNextPollingDay` returns false for the USA (`WorldClock.cs`: `if (id != CountryId.Sweden) { return false; }` after the Germany and Poland branches). `TwoRoundElection.RuleOf(id) => id == CountryId.Poland ? Poland : null`, so `PresidentialElection.TryNextFirstVote` is false too. A run that the start card calls the "RUN-UP" to 5 Nov 2024 never reaches a vote.

4. **Nothing pins the card's RUN-UP.** `StartBriefDiagnostic.cs:72` checks `!usaLine.Contains("RUN-UP")` on `WorldClock.StartLine` only. `StartPointsDiagnostic.cs:53/55` pin `ModeLine` for France and Sweden, never the USA.

5. **Both surfaces are filmed.** `UiScreenshotDriver.cs:331` captures `01_country_selector` (the folder line) and `:356-359` captures `01f2_start_points` (the start card). The REAL film of "the start card" that US-1's done-when owes would therefore show "5 NOV 2024 · RUN-UP · OPENS 12 MAR 2024 · Presidential election".

Partial mitigation, which does not refute the finding: France's "A WHAT-IF · NO ELECTION" in the same grid invites the build-tense reading of "NO ELECTION YET", and the ledger row says "in this game". The ambiguity remains, and the card the folder opens onto pushes the false reading.

**The skeptic's corrected fix.** 1. **Folder line (`WorldClock.StartLine`).** Carry the qualifier: `$"OPENS {opens} · NO ELECTION IN THIS GAME YET"`, or a shorter form such as `"· NO ELECTION HELD · AS OF RECORD"`. The folder column is 542−52 = 490 canvas units wide, and the line is set at size 9, floored to 14 at 1280. The 48-character form is shorter than Germany's and Italy's 51–52-character lines, which already fit, but France's "MODELLED" line wrapped and clipped. Measure it on the 1280 REAL film `01_country_selector`.

2. **Start card (the larger fix, outside this diff).** Give `StartPoints.ModeLine` a `WorldClock.NoElectionYet` branch, as France has one. For example: `"AS OF RECORD · NO ELECTION IN THIS GAME YET · " + opens`, or `"AS OF RECORD · " + opens`. Without it, the start card the folder opens onto says "RUN-UP". Whether the "5 NOV 2024" stamp should still head a card named "Presidential election" that the game does not hold is a call for Design or Elias. Under SP-1 the stamp is the start point's identity, and France's stamp is a past day.

3. **Pins.** Pin `!StartPoints.ModeLine(usa).Contains("RUN-UP")` (and the new wording) in `StartPointsDiagnostic` or `StartBriefDiagnostic`, beside the existing `!usaLine.Contains("RUN-UP")`. Then film `-country=USA` REAL at 1280, covering both `01_country_selector` and `01f2_start_points`.

### 8. The start card on 01f2 still advertises the run-up to 5 Nov 2024, right above the ledger row that says no election is held

- **Lens:** checks - **reviewer:** defect - **skeptic:** defect
- **Where:** Assets/Scripts/Elections/StartPoints.cs:119

**The scenario.** Film with -shotcountry=USA. On 01f2_start_points, BuildStartCard (CountrySelectorScreen.cs:621-626) draws the stamp from StartPoints.DateLine as "5 NOV 2024" and the mode line from StartPoints.ModeLine as "RUN-UP · OPENS 12 MAR 2024" (StartPoints.cs:119-125: the USA is neither governing-mode nor snap). It names the card "Presidential election". The ledger directly below now reads "Election · NONE IN THIS GAME YET · AS OF RECORD" (StartBrief.cs:203). The changed WorldClock.StartLine is drawn only on the folder card (CountrySelectorScreen.cs:992). StartPoint.Line is never drawn on a playable card, because Reason() returns null for it (StartPoints.cs:128-132). So the card that US-1's done-when films (the start card, in this repo's vocabulary) still promises the polling day that §2.5 item 1 calls wrong. The new NoElectionYet doc (WorldClock.cs:104-105) also says "the start card ... say[s] so instead of promising a polling day", which is false. No check pins the USA's ModeLine: StartPointsDiagnostic.cs:53/:55 pin only France's and Sweden's.

**The fix proposed.** Give ModeLine a NoElectionYet branch, the way France has its what-if line, e.g. "NO ELECTION YET · OPENS 12 MAR 2024". Pin it in StartPointsDiagnostic or StartBriefDiagnostic. Correct the doc comment to name the surfaces that actually change: the folder line and the ledger row.

**The skeptic's evidence.** I could not refute it. Every step of the scenario holds at HEAD plus the working tree. Neither StartPoints.cs nor CountrySelectorScreen.cs has uncommitted changes.

1. What the US start point holds (StartPoints.cs:83-84, 99-100)
- Ruled(USA, "PRESIDENTIAL ELECTION", ...) builds a playable point.
- Its PollingDay is WorldClock.LatestElectionDay(USA), which is D(2024,11,5) (WorldClock.cs:91).
- Its Opens is StartDate(USA), which is CampaignCalendar(2024-11-05).PreCampaignStart: 8 weeks plus 26 weeks earlier (CampaignClock.cs:72, 75, 112, 114), so 12 MAR 2024.

2. What the card's mode line returns (StartPoints.cs:119-125)
- ModeLine has only a GoverningModeOnly branch (France, WorldClock.cs:101) and then `return (WorldClock.IsSnapStart(p.Country) ? "SNAP ELECTION · " : "RUN-UP · ") + opens;`.
- IsSnapStart covers only Germany and Italy (WorldClock.cs:98).
- So the USA gets "RUN-UP · OPENS 12 MAR 2024". There is no NoElectionYet branch.

3. What the start card draws (CountrySelectorScreen.cs)
- :621 draws BuildStamp(StartPoints.DateLine(point)), which reads "5 NOV 2024".
- :624 draws StartPoints.ModeLine(point), which reads "RUN-UP · OPENS 12 MAR 2024".
- :638 draws Name, which reads "Presidential election".
- :503 draws BuildBriefLedger beneath the card. Its rows come from StartBrief.cs:203: "Election · NONE IN THIS GAME YET · AS OF RECORD".
- The sheet is opaque and hides the folder grid (`_folderGrid.SetActive(false)`, :400). The changed StartLine is drawn only on the folder card (:992), so it is not in the 01f2 frame.
- The file has no `.Line` reference at all, and Reason() returns null for a playable card (StartPoints.cs:130). So the new StartPoint.Line text never appears on the start sheet.

4. The build runs no run-up and holds no US election
- WorldClock.TryNextPollingDay has branches only for Germany, Poland and Sweden; for the USA it returns false.
- So SimulationManager.AdvanceCampaign stops at `if (!next.HasValue) { return; }   // no election calendar for this country yet - no run-up and no campaign`.
- The card's "RUN-UP", with its 5 NOV 2024 stamp, claims both things the build does not do. That is §2.5 item 1's "advertises an election it never holds", on the very surface US-1's done-when films.

5. The new doc comment is false (claim convention)
- WorldClock.cs:103-105 says "the start card, the brief and the not-held reason say so instead of promising a polling day".
- In this repo "start card" means BuildStartCard's card: CountrySelectorScreen.cs:541 ("One start card (18a/18b)"), :641 (§627), and USA_STAGE_PLAN.md ("the start card ... keeps §627's House bar").

6. The checks do not cover what the card draws
- The new StartBriefDiagnostic check forbids "RUN-UP" only in WorldClock.StartLine (`!usaLine.Contains("RUN-UP")`), a string the start card does not draw.
- StartPointsDiagnostic pins the mode line only for France (:53) and Sweden (:55); for the USA it checks only that the mode line is not null (:44).
- The film's RecordCanvasTextAssert (UiScreenshotDriver.cs:2129-2152) checks clipping, not wording. So the bars go green while the frame reads RUN-UP above NONE IN THIS GAME YET.

7. Precedent
- §631 fixed France the same way, through ModeLine ("WHAT-IF · YOUR PARTY GOVERNS · OPENS 18 JUL 2024"), so that "the card states the what-if plainly".

Mitigating points (not enough to change the grade):
- The plan's US-1 names only `WorldClock.StartLine` as "the card's line", so the build followed the plan's pointer.
- "RUN-UP" was already on the card before this change.
- The ledger row directly beneath does say there is no election.

It still counts as a defect: the change adds a false claim to a comment, and the one frame it must film now contradicts itself.

**The skeptic's corrected fix.** 1. Change ModeLine. In StartPoints.ModeLine, after the GoverningModeOnly branch, add:
   `if (WorldClock.NoElectionYet(p.Country)) { return "NO ELECTION YET · " + opens; }`
   The USA's start card then reads "NO ELECTION YET · OPENS 12 MAR 2024", following France's what-if line (§631). The 5 NOV 2024 stamp can stay as the record's date, as France's card keeps 7 JUL 2024 under its what-if line, unless US-2's words revisit it.

2. Pin it in StartBriefDiagnostic's US-1 check (the diagnostic US-1's done-when names), or beside France and Sweden in StartPointsDiagnostic:
   `StartPoints.TryPlayable(CountryId.USA, out var us); Check(StartPoints.ModeLine(us).StartsWith("NO ELECTION YET · OPENS ", StringComparison.Ordinal) && !StartPoints.ModeLine(us).Contains("RUN-UP"), ...)`
   Sweden's pinned "RUN-UP · OPENS 18 JAN 2026" stays as the control.

3. The WorldClock.NoElectionYet doc comment is then true. If ModeLine is not changed, reword the comment to name only what actually changes: the folder card's start line, the brief (its clause and ledger row) and the not-held reason.

4. Correct US-1's pointer in USA_STAGE_PLAN.md (TRACKING). `WorldClock.StartLine` is drawn only on the folder card; the start card's line is `StartPoints.ModeLine`. Name both.

5. Shoot the owed REAL film 01f2_start_points with -shotcountry=USA at 1280. Read the card's mode line and the ledger row together; the film's text guard checks only clipping, so the wording has to be read by eye.

### 9. "Hold as of record" is not what the build does before US-2, and "Congress" claims a Senate that no code holds

- **Lens:** checks - **reviewer:** defect - **skeptic:** minor
- **Where:** Assets/Scripts/Elections/StartBrief.cs:203

**The scenario.** Verified: a US game keeps Biden and the 118th House for the whole run. Seats are set once at the epoch (WorldFactory.cs:1032, the Usa2022 table at 12 Mar 2024). The government is set once at the epoch through GovernmentRecord.AtStart (WorldFactory.cs:1092; GameController.cs:2188, :2230). The only writers that change them later are SetSeatsFromElection (GameController.cs:6692) and the Riksdag/Bundestag installs (SimulationManager.cs:3142, :3989). The first needs a polling day, but TryNextPollingDay(USA) is false (WorldClock.cs:435) and no extra election is possible. The second never runs for the USA (RoundsApply excludes it, SimulationManager.cs:3230-3232). Scenario: a player opens the USA, reads "Election · NONE IN THIS GAME YET · AS OF RECORD" on 01f2, and plays to February 2025. The game still seats the start's House and Biden as the executive. The record says the 119th House from 2025-01-03 and Trump from noon 2025-01-20 (WorldClock.cs:169, :270). That by-date seating is R-US1's sense and the plan's own §2.5 item 2. So the words are true only at the start. The same overclaim is in the clause (StartBrief.cs:115), NotHeldReason (NationalElection.cs:416-417: "...hold as of record until the Electoral College count and the House and Senate races are built") and the NoElectionYet doc (WorldClock.cs:104). "Congress" also names a Senate that does not exist anywhere in code: grep finds none, and the plan's §2.1 says so.

**The fix proposed.** Until US-2 lands, word it as what is held. For example, the row "NONE IN THIS GAME YET" without "AS OF RECORD" (or "THE START'S PRESIDENT AND HOUSE HOLD"), and the clause and reason "the president and the House seated at the start hold throughout". Let US-2's done-when ("US-1's words updated") restore "as of record". Say "the House" rather than "Congress" until US-15 seats a Senate. Alternatively, land US-1 together with US-2.

**The skeptic's evidence.** The failing path is real. A US run keeps the start's president and House for its whole length.
- Seats: they are set once, at WorldFactory.cs:1032. The USA's epoch is 12 Mar 2024 (26+8 weeks before 2024-11-05, CampaignClock.cs:72,75), so the seated chamber is Usa2022, REP 222 / DEM 213 (WorldClock.cs:168; PartySystem.cs:773).
- Government: it is set once, to Biden, through GovernmentRecord.AtStart (WorldFactory.cs:1092; GameController.cs:2188, :2230; WorldClock.cs:269).
- Nothing changes either one later:
  - SetSeatsFromElection (GameController.cs:6692) and FromView (:6429) run only inside the election path. That path is gated by PollingDayToday (SimulationManager.cs:477).
  - TryNextPollingDay(USA) returns false (WorldClock.cs:435).
  - An extra election needs NoConfidenceOn. That is set only at SimulationManager.cs:2865 or :3050, under Riksdag rules. RulesOf(USA) is Unsourced (ConfidenceProcedure.cs:34), so MoveNoConfidence refuses (:2831) and TryAiMotion returns (:3027).
  - RoundsApply is false (:3230-3232), so Install (:3989) and InstallSuccessor (:3142) are never reached.
  - TwoRoundElection.RuleOf(USA) is null (TwoRoundElection.cs:63), so HoldPresidentialRound returns.
  - Nothing ends a US run before 2025. The only game-over sets are GameController.cs:2099 (scenario) and :6718 (election verdict).
- The record seats the 119th House from 2025-01-03 (WorldClock.cs:169) and Trump from 2025-01-20 (:270).
- The plan reads the phrase the same way. USA_STAGE_PLAN.md:459-465 says "Today a US game keeps the start's", and calls by-date seating "the record's own reading of 'as of record'". Option (b), "as at the start", must be "said on screen". (a) was RULED.
- §2.5 item 2 (:124) lists the at-start behaviour as "Wrong or misleading today".
- US-2's done-when (:176) owes "US-1's words updated".

So in R-US1's ruled sense, "AS OF RECORD" (StartBrief.cs:203) is false in any US run past 3 Jan 2025 until US-2 lands. "Congress" also names a Senate that no code holds: grep finds no Senate in Assets/Scripts beyond Poland comments and the new string, and the plan says so at :83.

Why I graded it minor, not a defect:
- Only the ledger row reaches a screen. CountrySelectorScreen.cs:792 draws StartBrief.Rows, and the row sits under "BRIEF · UNITED STATES, 12 MAR 2024". It is true on that date and stays true until 2 Jan 2025 of game time.
- StartBrief.Clauses and Text are read by no screen, only by StartBriefDiagnostic.cs:34-71.
- NotHeldReason(USA) cannot be reached in play. Its only caller (GameController.cs:6681/6683) sits behind PollingDayToday, which is never true for the USA, and nothing displays ElectionRecord.NotHeldReason.
- So "Congress" and "House and Senate races" appear on no screen. They are code strings plus the NoElectionYet doc comment (WorldClock.cs:103-105).
- The repo's earlier wording uses "stands/holds as of record" for exactly the at-start behaviour: CLAUDE.md's World Clock rule, SimulationManager.cs:2611-2612 and PollingDayDiagnostic.cs:84. The plan also prescribed these words for US-1 (:169). The overclaim is against R-US1's new reading only.

Outside this finding: the US start card still promises the election. CountrySelectorScreen.cs:621 stamps StartPoints.DateLine as "5 NOV 2024", and :624 draws StartPoints.ModeLine as "RUN-UP · OPENS 12 MAR 2024" (StartPoints.cs:115-125), under the name "Presidential election".

**The skeptic's corrected fix.** Until US-2 lands, describe what the build actually does: the president and House seated at the start hold for the whole run. Say "the House", not "Congress".
- StartBrief.cs:203: make the figure "NONE IN THIS GAME YET · THE START'S PRESIDENT AND HOUSE STAND", or just "NONE IN THIS GAME YET". The figure lane is 1260-190-12 units at the 1920 basis, so either fits.
- StartBrief.cs:115: "No election is held in this game yet - the president and the House seated at the start hold until one is."
- NationalElection.cs:416-417: "No US election is held in this game yet: the president and the House seated at the start hold until the Electoral College count and the House races are built."
- WorldClock.cs:103-105: the doc should say the president and House as seated at the start hold meanwhile, and that seating them by the record's dates is R-US1 (a), built in US-2.
- StartBriefDiagnostic.cs:62-75: re-pin the strings and the comment.
- Let US-2's "US-1's words updated" step bring back "as of record, by date", and say "the 119th stands" where the 120th is not sourced. Keep "Congress" out until US-15 seats a Senate.
- Alternatively, commit US-1 together with US-2 so the words are never ahead of the build.
- Re-cut the REAL 1280 film after the row changes.

### 10. -shotstop=01g_party_picker fails the film on TL-1 and also cuts 01g's own canvas assert

- **Lens:** checks - **reviewer:** minor - **skeptic:** note
- **Where:** Assets/Scripts/Testing/UiScreenshotDriver.cs:370

**The scenario.** 01f2's canvas assert (:360) does run under this stop, because it is recorded before 01g is captured. But :370 declares 01g_party_picker and 01h_scenario_party_picker together. A stop at 01g calls EndSweep inside Capture (:2667-2671). EndSweep finds 01h declared and never captured, logs "EXPECTED FRAME MISSING" and increments _failed (:2246-2252), and Finish exits 1 through EditorApplication.Exit (:2888). The same exit means RecordCanvasTextAssert("01g_party_picker") at :382 never runs. Result: a red film for the US-1 done-when.

**The fix proposed.** Stop at 01h_scenario_party_picker. By then every declared frame (00, 00a, 01f2, 01g, 01h) is captured, and the asserts for 01_country_selector, 01f2 and 01g all run. The 01k* steps are declared only where PartyCreationFlow.Offered, after 01h. Alternatively use the precedent -shotstop=01c_desk from the §626/§627/§629 start-card films. Run with -shotcountry=USA (the default) so that 01f2 shows the US card and ledger. 01_country_selector shows the US folder line whatever the country.

**The skeptic's evidence.** Every claimed line checks out in G:/UNITY/Projects/PoliSim/Assets/Scripts/Testing/UiScreenshotDriver.cs at HEAD d00b66aa. The driver is not in the change.

The failing path, traced:
- :370 `Expect("01g_party_picker", "01h_scenario_party_picker");` is unconditional and runs before either frame is captured.
- :2479 `_capturedFrames.Add(name);` runs only when a Capture starts, so 01h is never marked if the run stops at 01g.
- On a real film, after 01g's PNG is written, :2667-2671 `if (!string.IsNullOrEmpty(StopAfter) && name == StopAfter) { ... EndSweep(); yield break; }` ends the run.
- EndSweep :2246-2252 has no exemption for a stopped run. It logs `EXPECTED FRAME MISSING - '01h_scenario_party_picker'` and does `_failed++`.
- :2271 then makes `clean` false, :2280 calls `Finish(1)`, and :2888 runs `UnityEditor.EditorApplication.Exit(exitCode)` with exit code 1.
- `RecordCanvasTextAssert("01g_party_picker", controller)` at :382 comes after the `yield return Capture(...)`, so it never counts. This matches the standing memory note "-shotstop=X cuts X's own canvas assert".
- 01f2's assert at :360 does run, because StopAfter is not 01f2.

Why someone would plausibly hit it:
- The memory rule (polisim-2026-09-24-k1f-k1e-pf16.md:21) says "stop one frame later". Applied to the start card (01f2_start_points), that gives exactly -shotstop=01g_party_picker.
- That rule predates TL-1 (§645, 2026-09-26). §645's record never mentions -shotstop.
- The -shotstop doc at :80-84 still says a stopped sweep is judged "over what it filmed". Since TL-1 it is also judged over what it declared.
- Stops that passed after §645 (§676 and §680, at `01k6_created_seated`) were on the last frame of their declared group.

The suggested stops are safe:
- `PartyCreationFlow.Offered` (PartyCreationFlow.cs:25) is `country == CountryId.Sweden`. So a USA run declares only 00, 00a, 01f2, 01g and 01h before 01a, 01b and 01c_desk.
- A stop at 01h, or at the precedent `01c_desk`, misses nothing. The precedent §627/§629 USA films used `-shotstop=01c_desk` and recorded "9 captured", the same nine frames.
- The country defaults to USA (driver :62; UiScreenshotCapture.cs:156 and :439).
- CountrySelectorScreen.cs:175-177 and :992 draw `WorldClock.StartLine(country.Id)` on every folder, so 01_country_selector shows the US line whatever the country.

Nobody has proposed this stop yet. No repo file or scratchpad script uses -shotstop=01g. The outcome would be a loud exit 1 naming the frame, which costs one rerun. That is why it is a note, not minor.

Side observation: Tools/bar_tier.ps1:46 lists CountrySelectorScreen's frames as `01_country_selector, 01g_party_picker, 01h_scenario_party_picker` and omits 01f2_start_points, which is the US-1 subject. It also prints the canvas line only when CountrySelectorScreen.cs itself changes. So for US-1 the owed film comes from the done-when alone.

**The skeptic's corrected fix.** Cut the owed real film at 1280 with -shotcountry=USA, which is also the default.

Use -shotstop=01h_scenario_party_picker. It is the shortest stop at which all five declared frames (00, 00a, 01f2, 01g, 01h) are captured, and the canvas asserts for 01_country_selector (folder StartLine), 01f2_start_points (card plus brief ledger) and 01g all count. The precedent -shotstop=01c_desk also works: for the USA it captures 9 frames, because the 01k* block is Sweden-only.

Do not stop at:
- 00_main_menu, 01g_party_picker or 01k1-01k4: each sits inside a declared group, so TL-1 fails the film.
- 01f2_start_points: that cuts the subject's own canvas assert.

Optional follow-up, outside this change: update the -shotstop summary at UiScreenshotDriver.cs:80-84 and the "stop one frame later" memory note to say that since TL-1 (§645) the stop frame must be the last frame of its declared group. Also consider adding 01f2_start_points to bar_tier.ps1's CountrySelectorScreen shot list.

### 11. StartBriefDiagnostic's new checks: what each catches and what each misses

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/StartBriefDiagnostic.cs:71

**The scenario.** Catches: NoElectionYet returning false; the StartLine branch removed (EndsWith fails and "RUN-UP" is present); the clause reverting to "Polling day is ..."; Rows reverting to a polling-day row (:75); NoElectionYet true for Sweden or Poland (:76). Misses: (a) at :72, a line that keeps the date without the word RUN-UP passes, e.g. "OPENS 12 MAR 2024 · THE ELECTION OF 5 NOV 2024 · NO ELECTION YET". (b) At :73 only the reason's prefix is pinned, so its tail can drift. (c) At :76 only Sweden and Poland are guarded. `NoElectionYet => id == USA || id == Germany` passes every check, even though Germany's 23 Feb 2025 polling day is one the game holds (WorldClock.cs:414-419). StartPointsDiagnostic.cs:35 compares StartLine with itself, so it does not help. Poland's brief text is never read, so a StartBrief-side gate (`|| id == CountryId.Poland`) drops Poland's clause unseen; the four-clause count still holds. (d) Nothing pins the US card's ModeLine or DateLine (finding 1). (e) Clause, row, folder line and reason are pinned independently: re-pin one of them and the others drift apart unseen.

**The fix proposed.** Pin the whole US line ("OPENS 12 MAR 2024 · NO ELECTION YET"), or assert the 5 NOV 2024 stamp is absent. Pin the reason whole. Add one invariant over every playable start: NoElectionYet(id) == (!GoverningModeOnly(id) && !TryNextPollingDay(id, start.Opens, out _)), and the brief and row carry "Polling day is <Long(day)>" exactly when the calendar returns one. Note that this invariant would fail today on Italy (next finding).

**The skeptic's evidence.** I couldn't refute it. Every factual claim in the inventory checks out, and two of the misses can be shown to happen.

What the checks pin (Assets/Editor/StartBriefDiagnostic.cs):
- :72 checks only the end of the line, `usaLine.EndsWith(" · NO ELECTION YET") && !usaLine.Contains("RUN-UP")`. Miss (a) is true, but its example is a self-contradicting edit nobody would make. A plausible revert, such as "THE PRESIDENTIAL ELECTION OF 5 NOV 2024", fails EndsWith. Weak point.
- :73 checks only the start of the reason (`usaReason.StartsWith(...)`), so (b) is true. Prefix pins are the repo's normal pattern. Weak point.
- :76-77 is labelled "the countries whose elections the game holds keep their polling day", but it tests only `!NoElectionYet(Sweden) && !NoElectionYet(Poland) && text.Contains("Polling day is 13 September 2026.")`, and `text` is Sweden's brief (:50). Germany is left out, yet WorldClock.cs:414-420 returns its 23 Feb 2025 polling day for a game that opens 6 Nov 2024. Poland's brief is never read.

Trace of (c), the edit `NoElectionYet => id == USA || id == Germany`:
- StartBrief.cs:112-124 swaps one clause for one, so Germany still has 4 clauses (:35).
- The new clause's basis "WorldClock.NoElectionYet (...)" passes :38.
- Germany shows no gap (:45), and :76 is unchanged, so every check stays green.
- StartPointsDiagnostic.cs:35 compares `start.Line` with `WorldClock.StartLine(id)`. StartPoints.cs:100 sets that same value, so the comparison always passes.
- I searched the tracked files for "SNAP ELECTION OF", "THE RUN-UP TO", "Polling day is 23" and the new strings. They appear only in the source and in this diagnostic. Nothing else in the bar would notice Germany's card and brief turning to "NO ELECTION YET" or to "the president and Congress".

(d) is the point with weight, because it hides a problem on screen today:
- For the USA, StartPoints.ModeLine (StartPoints.cs:119-125) gives "RUN-UP · OPENS 12 MAR 2024", since the USA is neither governing-mode nor a snap start.
- DateLine (:115-116) gives "5 NOV 2024", because PollingDay = LatestElectionDay(USA) (WorldClock.cs:91).
- BuildStartCard draws both, at CountrySelectorScreen.cs:621 and :624, with the name "Presidential election". StartLine is drawn only on the folder card (BuildFolderCard, :992).
- So the "no RUN-UP" guard at :72 watches the folder card. The start card, the one US-1 owes a film of, still says RUN-UP to 5 Nov 2024. StartPointsDiagnostic.cs:53/:55 pin ModeLine and DateLine for France and Sweden only.
- The campaign strip's "POLLING DAY" (GameController.Campaign.cs:1083) can't appear in a US game: LiveCampaignSetup.TryFor stages Sweden and Germany only (:148).
- That leaves the start card's mode line and date stamp as the one on-screen place still promising a US polling day, and no check sees it.

(e) is true but generic.

Re-grade: minor. The checks meet US-1's done-when as written. (a), (b) and (e) add little. The defect behind (d) belongs to finding 1. The real problems here are the :77 label claiming more than it tests and the RUN-UP guard watching the wrong card.

Correction to the proposed fix: the calendar invariant would turn the bar red as soon as it lands. For Italy, TryNextPollingDay returns false (WorldClock.cs:435) and NoElectionYet(Italy) is false, so `NoElectionYet == !GoverningModeOnly && !TryNextPollingDay` fails today.

**The skeptic's corrected fix.** (1) Replace the guard at :76 with one check over all six countries: `NoElectionYet(id) == (id == CountryId.USA)`. This pins the whole set in one line, and it catches the Germany or Italy edit. It also matters because the clause text "the president and Congress" fits only the USA. In the same loop, for every playable start where `!NoElectionYet` and `PollingDay != MinValue`, assert that the brief carries "Polling day is " + Long(PollingDay) + "." and that a "Polling day" or "Last polling day" row exists (France's start uses the "Last" form). If that is not done, narrow the label to what is actually checked.
(2) Pin the US StartLine whole, `== "OPENS 12 MAR 2024 · NO ELECTION YET"`, and pin NotHeldReason(USA) whole.
(3) In StartPointsDiagnostic, next to the France and Sweden pins at :53/:55, pin the US start card's ModeLine and DateLine. At minimum assert `!StartPoints.ModeLine(usa).Contains("RUN-UP")`. That check fails today, which is finding 1's defect, so it lands with finding 1's change to the card's mode line and date stamp. Until then the 1280 film of the start card will show "5 NOV 2024 · RUN-UP · OPENS 12 MAR 2024".
(4) Defer the calendar invariant (`NoElectionYet == !GoverningModeOnly && !TryNextPollingDay(id, start.Opens)`). It is the right long-term link to the calendar, but it fails on Italy today. Add it either with Italy named as a stated exception, or after the Italy finding settles Italy's words.

### 12. NoElectionYet's doc gives a general definition that Italy also meets, but the predicate is hard-coded to the USA

- **Lens:** checks - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/Elections/WorldClock.cs:103

**The scenario.** The summary defines "a start whose elections the game does not hold yet - the USA's". Italy's start meets that definition too: TryNextPollingDay(Italy) is false (WorldClock.cs:435), PollingDayDiagnostic.cs:82-84 asserts it, and NationalElection.Run gives NotImplemented. Yet Italy's folder line, card and brief still promise "THE SNAP ELECTION OF 25 SEP 2022" / "Polling day is 25 September 2022.". That gap is older than this change (PS-7 owns Italy's snap start), but the new comment's general claim is false today under the claim convention. Separately, the clause and row under the generic predicate carry US-only words ("the president and Congress"). Widening the predicate to Italy would print them for Italy.

**The fix proposed.** Narrow the doc to "the USA's start, until US-8 (US-1)" and drop the general definition. If the predicate is ever derived from the calendar, key the clause and row words by country.

**The skeptic's evidence.** The doc and the predicate disagree today. Nothing fails at runtime.

1. The new summary at WorldClock.cs:103-106 reads: "a start whose elections the game does not hold yet - the USA's, until its presidential count (US-8) and Congress's (US-16) are built". The body is `public static bool NoElectionYet(CountryId id) => id == CountryId.USA;`. The file's own sibling at :97-98 (`IsSnapStart`, "True for a start that opens on a snap election's trigger day…" => `id == Germany || id == Italy`) writes a general definition whose extension is exactly the body. Read the same way, this summary claims to cover every start the game holds no election for.

2. Italy meets that definition. I traced every path that could hold an election:
   - Calendar: `TryNextPollingDay` returns false at WorldClock.cs:435 (`if (id != CountryId.Sweden) { return false; }`; Italy falls through). So `SimulationManager.TryPlayerPollingDay` (4207-4214) is false, `CurrentCampaignCalendar` returns null (4219), and `PollingDayToday` is never raised (477).
   - Extra election: unreachable. `ConfidenceProcedure.RulesOf` gives Unsourced for Italy and the USA (ConfidenceProcedure.cs:34). The player's motion is refused at SimulationManager.cs:2831, and the AI motion is Riksdag-only (3026-3027).
   - Presidential rounds: Poland only (`TwoRoundElection.RuleOf`, TwoRoundElection.cs:63).
   - Election run: `NationalElection.Run`'s default branch returns NotImplemented (NationalElection.cs:398-401).
   - The check: PollingDayDiagnostic.cs:82-85 asserts no polling day for Italy, the USA and France.

   France meets the definition too (via `GoverningModeOnly`), yet `NoElectionYet(Italy)` and `NoElectionYet(France)` are both false.

3. Italy's on-screen promise is real but older than this change.
   - The folder draws `StartLine` (CountrySelectorScreen.cs:992): "OPENS 21 JUL 2022 · THE SNAP ELECTION OF 25 SEP 2022" (WorldClock.cs:129-130).
   - The brief says "Polling day is 25 September 2022." (StartBrief.cs:117-119).
   - The ledger row is "Polling day · 25 SEP 2022" (StartBrief.cs:205-209).
   - The sheet's ModeLine is "SNAP ELECTION · OPENS 21 JUL 2022" (StartPoints.cs:124).
   - PS-7 owns "the snap start" (POLISIM_FEATURE_LIST.md:85).

4. The US-only words under a generally named predicate are a risk only if someone widens it, which nothing does today: StartBrief.cs:115 ("the president and Congress hold as of record") and :203.

5. A smaller slip in the same summary: "until … (US-8) and … (US-16) are built" runs past US-1's own "Until US-8" (USA_STAGE_PLAN.md:169). US-8's done-when (line 243) rewrites these words once a polling day is held.

Aside, not this finding but on the task's question: the USA's open sheet still promises its polling day. Its head row draws `StartPoints.DateLine` = "5 NOV 2024" and `ModeLine` = "RUN-UP · OPENS …" (StartPoints.cs:115-125, drawn at CountrySelectorScreen.cs:621 and 624), above the name "Presidential election". That sits beside the folder's "NO ELECTION YET". The diagnostic's `!usaLine.Contains("RUN-UP")` tests only `StartLine`, so it does not catch this.

**The skeptic's corrected fix.** Narrow the summary to the item's own scope. Point to the general test instead of defining it here. For example:

/// <summary>PS-6, US-1 (`docs/specs/USA_STAGE_PLAN.md`): the USA's start, until the presidential count lands (US-8). Its card's line, brief and not-held reason say that no US election is held and what holds instead (§618's ruling 4), never a polling day; the card stays playable (§618). Whether a start's election is held is `TryNextPollingDay`'s answer, not this predicate's.</summary>

Keep the US-only clause and row words as they are. If the predicate is ever widened or derived from the calendar, key those words by country (or by `ExecutiveKind`) first. Italy's snap-start promise stays with PS-7, outside US-1. Separately, consider a US-1 follow-up for the US sheet's `DateLine` and `ModeLine` ("5 NOV 2024", "RUN-UP · OPENS …"), pinned by `StartBriefDiagnostic`.

### 13. The new clause and NotHeldReason appear on no screen; only two strings can be filmed

- **Lens:** checks - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/Elections/StartBrief.cs:115

**The scenario.** StartBrief.Clauses and StartBrief.Text are read only by StartBriefDiagnostic; CountrySelectorScreen draws Head, Rows and Tagline (:789-817). NotHeldReason is only stored and logged by RunNationalElection (GameController.cs:6681-6683), which a US game never reaches because there is no polling day. So the real film can show only the folder line (01_country_selector, assert at UiScreenshotDriver.cs:332; also visible under the scrim in 01a, which has no assert) and the Election row (01f2_start_points, assert at :360). Separately, the hand-written clause departs from StartBrief's own SP-2 contract ("generated from the sourced records ... never written by hand, so it cannot go stale", StartBrief.cs:9-13). Its basis names a code predicate, not a record, and the diagnostic's basis test accepts it only through the "WorldClock" substring (StartBriefDiagnostic.cs:38).

**The fix proposed.** Say in the record which US-1 strings the film evidences. Either state the clause as a declared exception to SP-2's derived-brief contract, or give it a record-shaped basis.

**The skeptic's evidence.** I tried to refute each claim in the finding and could not. All of them hold. None of it is a code defect, and nothing the player sees is false.

**1. The new clause is on no screen.**
- A grep over Assets finds `StartBrief.Clauses` and `StartBrief.Text` read only by `StartBriefDiagnostic` (lines 34, 43, 46, 50, 56, 79).
- `CountrySelectorScreen` draws the ledger's Head (:789), Rows (:792-815) and Tagline (:817-822).
- The only other new string that reaches a screen is `WorldClock.StartLine`, drawn on the folder card at `CountrySelectorScreen.cs`:992.

**2. `NotHeldReason` is on no screen, and a US game never calls it.**
- It is written at `NationalElection.cs`:400 and `GameController.cs`:6681, and logged at :6683. No UI code reads `ElectionRecord.NotHeldReason`; only `ElectionDayReachDiagnostic`:101 does.
- `CheckElection` (`GameController`:6352) and `HoldElectionWithoutTheNight` (:6341) are gated on `PollingDayToday`. That flag is set at `SimulationManager`:477 from `TryPlayerPollingDay`.
- For the USA, `TryNextPollingDay` returns false (`WorldClock.cs`:435).
- No extra election can be ordered: `ConfidenceProcedure.RulesOf(USA)` is Unsourced (:34). So the player's motion is refused (`SimulationManager`:2831), AI motions return early (:3027) and `RoundsApply` is false (:3230).
- The presidential rounds are Poland only (`TwoRoundElection.RuleOf`, :63).

**3. What the film can show.**
- 01_country_selector carries the canvas-text assert at `UiScreenshotDriver`:331-332.
- 01a is captured at :473 with no assert.
- 01f2_start_points is asserted at :359-360. It opens the start panel for the film's own country (:353-356), so the Election row is filmed only in a USA run.
- So only two of the four new strings can be filmed: the folder line and the Election row.

**4. The SP-2 contract claim is accurate.**
- `StartBrief.cs`:9-13 says the brief is "generated from the sourced records … never written by hand, so it cannot go stale".
- The clause at :115 is fixed text, gated on a hand-kept predicate: `NoElectionYet(id) => id == CountryId.USA` (`WorldClock`:106). That predicate is not derived from `TryNextPollingDay`.
- Its basis names a predicate and a ruling, not a record. The diagnostic's test at `StartBriefDiagnostic.cs`:38 (`Contains("WorldClock") | ...`) accepts it through the substring alone.
- Further sign the predicate is hand-scoped and not derived: Italy also has no polling day (`PollingDayDiagnostic`:82-85), yet its card and brief still promise 25 Sep 2022.

**Why the grade stays at note.**
- `USA_STAGE_PLAN.md`:171 already limits the film to "Canvas text drawn by `CountrySelectorScreen`" and gives the clauses to `StartBriefDiagnostic`.
- US-8's done-when (:243) already owes the update to these words, so the stale-text risk is tracked.
- No `COMPLETED.md` entry exists yet that could overclaim.

**Adjacent (not this finding).** The same 01f2 frame also films the start card's mode line "RUN-UP · OPENS 12 MAR 2024" (`StartPoints.cs`:124) and its "5 NOV 2024" date stamp (:115-116). Neither checks `NoElectionYet`. The diagnostic's `!usaLine.Contains("RUN-UP")` (:72) covers only `StartLine`.

**The skeptic's corrected fix.** 1. When the US-1 record is written, name what each check proves:
   - The REAL film at 1280 shows two strings: the folder card's `StartLine` (01_country_selector, in any film) and the ledger's Election row (01f2_start_points, in a USA film only).
   - `StartBriefDiagnostic` alone proves the brief's sentence and `NotHeldReason`. No screen draws them, and a US game never reaches `RunNationalElection`.

2. Amend `StartBrief`'s class summary (:9-13) to declare the exception. The election slot reports the game's own calendar, not the record's, where the game holds no election (PS-6 US-1, until US-8 and US-2 update the words). That clause's basis is a ruling, not a record.

3. Optionally tighten `StartBriefDiagnostic`:38 so a basis with no record id must be one of a named set (the government gap, NoElectionYet), not any string containing "WorldClock". Or derive `NoElectionYet` from the calendar: no `TryNextPollingDay` from the start date, no two-round rule, not governing-mode-only, with the sentence worded per country. That form would also catch Italy.

4. Separately, key `StartPoints.ModeLine` and the start card's date stamp on `NoElectionYet`, so the frame that films "Election · NONE IN THIS GAME YET" does not also film "RUN-UP".

## The first pass - refuted by the skeptics

- [checks] The tier tool will not ask for a Canvas film for this change, and its selector shot list omits 01f2 - *The finding's facts are right, but no failing path exists for this change.

(1) The tier stays silent by its rule, not through a gap. bar_tier.ps1:51-52 matches file base names against :45. WorldClock, StartBrief and NationalElection are SIMULATION (:56). StartBriefDiagnostic is registered only in the cheap Suite (CheckSuite.cs:200, inside the Suite block at 171-363), so the tier files it as tooling (:61). Only SIMULATION is touched (:72-74), so the whole UI block (:97-122, film scope and canvas) never runs. The file-name test never even matters. That matches CLAUDE.md:61 as written: "A UI commit touching SigningScreen, ElectionNightScreen, CountrySelectorScreen or CanvasChrome owes a REAL film". The film for this change is owed by US-1's own done-when (the finding concedes this). The tier still asks for "the four-width film matrix" at close (:101), and CLAUDE.md:61's track close films every Canvas surface real at 1280 and 2560.

(2) Leaving 01f2 off the list cannot drop the frame. UiScreenshotDriver.cs:348 declares Expect("01f2_start_points") unconditionally. EndSweep fails any declared frame that was never captured (:2244-2252). 01f2 is captured under the same controller.CanvasSelectorActive test (:349) as 01g/01h (:371), and before them (:359 vs :381/:392). Capture has no frame filter, only -shotstop (:2574, :2667). The canvas assert at :360 counts into the clean fold (:2129-2151, :2271). So any real film that reaches the listed 01g/01h has already filmed and asserted 01f2. The default -shotcountry is USA (UiScreenshotCapture.cs:439, UiScreenshotDriver.cs:62), so a default real film's 01f2 is the USA's sheet with the new Election row. The changed card line (WorldClock.StartLine) is drawn on every folder card (CountrySelectorScreen.cs:177, :992), which is 01_country_selector, a listed frame. For this change the list is never printed at all. The list is stale (01f2 was added in s621, 87630c09, after s578, 6667e8c8, wrote the list), but only as a printed hint.

Aside, a different finding: on that same 01f2 frame the USA's start card still shows a date stamp of 5 NOV 2024 (StartPoints.cs:115-116; PollingDay = LatestElectionDay(USA) = 2024-11-05, WorldClock.cs:91) and the mode line "RUN-UP · OPENS ..." (StartPoints.cs:119-124). That is a run-up to an election the game does not hold, drawn beside the ledger's "NONE IN THIS GAME YET". The diagnostic's !RUN-UP check covers StartLine only.*

## What the author did about the first pass

- **1, 8** (the start card still said "RUN-UP · OPENS 12 MAR 2024") - `StartPoints.ModeLine` has a branch for the USA's start: "NO ELECTION YET" and the opening; `StartPointsDiagnostic` pins it beside Sweden's and France's. The "5 NOV 2024" stamp and the card's name stay - the record's election, as France's card keeps its past stamp - judged on the real film.
- **2, 9** ("hold as of record" untrue before US-2) and **3** ("Congress" claims a Senate) - every surface now says what the build holds until US-2 seats the record by its dates: the president and the House seated at the start, no Senate modelled - the brief's clause, the ledger row ("NONE IN THIS GAME YET · THE START'S PRESIDENT AND HOUSE HOLD"), `NotHeldReason` and the docs. US-2 rewrites them to the by-date words its done-when owes.
- **4** (the ledger row had no subject) - the row names what holds; the diagnostic's comment says which surface says what.
- **5, 12** (`NoElectionYet`'s doc defined a class Italy also meets) - narrowed to the USA's start; Italy's snap start is PS-7's, listed for it.
- **6** (the plan's US-8 line) - the plan's US-1 and US-8 lines are corrected in the plan with this commit's words.
- **7** ("NO ELECTION YET" in the world's tense) - the folder line reads "NO ELECTION IN THIS GAME YET"; measured on the real film.
- **10** (`-shotstop=01g_party_picker` fails the film's declared group) - the film stops at `01h_scenario_party_picker`.
- **11** (the pins' power) - every string pinned whole; `NoElectionYet` is the USA's alone over every country; every other playable start with a polling day keeps it in its brief and its ledger.
- **13** (the clause and the reason appear on no screen) - the record names what the film shows (the folder line, the start card's mode line, the ledger's Election row) and what only the diagnostic proves (the brief's sentence, the not-held reason); `StartBrief`'s class doc declares the exception.
## The second pass - confirmed (verbatim)

A second workflow (us1-fix-pass) on the change after the first pass's fixes - now six files, StartPoints.cs and StartPointsDiagnostic.cs added - in two lenses (the words' truth and consistency, the pins and the canvas), every finding put to a refute-first skeptic.

### 1. ModeLine's summary and StartBrief's class doc apply the new branch to any start whose election is not held, but Italy's is one and gets neither; the summary also copies output strings into the comment

- **Lens:** truth - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/Elections/StartPoints.cs:119

**The scenario.** Line 119 says that "a start whose elections the game does not hold yet" gets "NO ELECTION YET · OPENS 12 MAR 2024". StartBrief.cs:13-15 says that "where the game holds no election for the start" the election slot reports the game's own calendar. Italy's start is such a start: `TryNextPollingDay(Italy)` is false and its NotHeldReason says so. Yet ModeLine returns "SNAP ELECTION · OPENS 21 JUL 2022" for Italy, and its brief still says "Polling day is 25 September 2022." The first review removed this same over-reach from NoElectionYet's own doc, but these two docs were not narrowed. The rewritten summary also copies output strings into the comment, which the claim convention forbids for DERIVED facts. One copied example on line 118 is already false: "GOVERNING · OPENS 18 JUL 2024", where ModeLine actually returns "WHAT-IF · YOUR PARTY GOVERNS · OPENS 18 JUL 2024". The new copy "NO ELECTION YET · OPENS 12 MAR 2024" goes stale with any rewording, including the fix for the finding above.

**The fix proposed.** Scope both docs to the USA the way NoElectionYet's doc now is: "the USA's start (`WorldClock.NoElectionYet`)", with Italy left to PS-7. Replace the copied output strings with a reference to the method that produces them, or delete them.

**The skeptic's evidence.** I traced the finding and could not refute it. It touches only comments: no behaviour changes, no player-facing text is wrong, and no check fails.

1. Italy's start is a start whose election the game does not hold.
- `WorldClock.TryNextPollingDay` (WorldClock.cs:413-443) returns true only for Germany, Poland and Sweden.
- `SimulationManager.TryPlayerPollingDay` (SimulationManager.cs:4207-4214) gates `PollingDayToday` on it, so an Italian game never reaches a polling day.
- `NationalElection` takes the default branch for Italy, which is `NotImplemented` with a not-held reason.

2. Italy gets neither new branch.
- `ModeLine` (StartPoints.cs:121-128) finds `NoElectionYet(Italy)` false and `IsSnapStart(Italy)` true, so it returns "SNAP ELECTION · OPENS 21 JUL 2022".
- `StartBrief.Text` takes the `else if (start.PollingDay != DateTime.MinValue)` branch and writes "Polling day is 25 September 2022." with the record as its basis.
- The new StartBriefDiagnostic loop pins this. It checks every playable start except the USA that has a polling day, and Italy is playable with 25 Sep 2022.

3. The two general phrases are therefore false for Italy.
- StartPoints.cs:119 says "a start whose elections the game does not hold yet".
- StartBrief.cs:13-14 says "where the game holds no election for the start ... the election's slot reports the game's own calendar, not the record's".
- In the same diff, `NoElectionYet`'s own doc (WorldClock.cs:103-107, narrowed by the first review for this same overreach) says it is "Not a general test of whether a start's election is held ... Italy's snap start (PS-7) is outside it". The three docs contradict each other.
- The backticked `WorldClock.NoElectionYet` reference does not rescue the wording: the description beside it is itself a claim, and it is broader than the predicate.

4. The summary also copies output strings into the comment.
- StartPoints.cs:118 is a '+' line, and its example "GOVERNING · OPENS 18 JUL 2024" is false. `ModeLine` returns "WHAT-IF · YOUR PARTY GOVERNS · OPENS 18 JUL 2024" for France, and StartPointsDiagnostic.cs:53 pins that.
- The wrong example is older than US-1: it was written at c753359e (s626), the code changed at 6549df2f (s631), and the comment was never updated. This diff carried it onto a rewritten line.
- The new example "NO ELECTION YET · OPENS 12 MAR 2024" is correct today (pinned at StartPointsDiagnostic.cs:55), but it is copied output, including a computed date. CLAUDE.md's claim convention forbids that: "Nobody transcribes, anywhere, in any file".
- CommentClaimCheck only checks that a backticked `Type.Member` exists, so no check catches either problem.

Grade: minor. Only comments are wrong, and the first review counted the same overreach in `NoElectionYet`'s doc as a lesser finding.

**The skeptic's corrected fix.** Comment-only edits; no code or strings change.

(1) Replace the summary at Assets/Scripts/Elections/StartPoints.cs:118-120 with:
/// <summary>18a: the playable card's mode and opening in caption mono, in the words this method returns: the run-up, a snap start's election, France's what-if
/// (`WorldClock.GoverningModeOnly`), or, for the USA's start alone (`WorldClock.NoElectionYet`, PS-6 US-1; Italy's snap start is PS-7's), that no election is held
/// yet, never a run-up. Null for a locked card (its stamp reads LOCKED there).</summary>
This removes the copied output strings (including the stale "GOVERNING · OPENS 18 JUL 2024" and the new "NO ELECTION YET · OPENS 12 MAR 2024"). StartPointsDiagnostic already pins the exact strings.

(2) In Assets/Scripts/Elections/StartBrief.cs:13-14, change "One exception, stated: where the game holds no election for the start (PS-6 US-1, `WorldClock.NoElectionYet`), the election's slot reports ..." to "One exception, stated: for the USA's start alone (PS-6 US-1, `WorldClock.NoElectionYet`; Italy's snap start is PS-7's), the election's slot reports ...". Leave the rest of the sentence as it is.

Every backticked reference left (`WorldClock.GoverningModeOnly`, `WorldClock.NoElectionYet`) names a real member, so CommentClaimCheck stays clean.

### 2. The ModeLine summary the diff rewrote keeps a stale "GOVERNING · OPENS 18 JUL 2024" and adds a new copied string

- **Lens:** checks - **reviewer:** note - **skeptic:** minor
- **Where:** Assets/Scripts/Elections/StartPoints.cs:118

**The scenario.** The rewritten <summary> still gives France's mode line as "GOVERNING · OPENS 18 JUL 2024". ModeLine actually returns "WHAT-IF · YOUR PARTY GOVERNS · OPENS 18 JUL 2024" (PS-3d), and StartPointsDiagnostic.cs:53 pins that. The diff also copies the US string and its date into the same comment. That copy goes stale with any rewording, including the one the finding above proposes. CLAUDE.md's claim convention bans copying derived facts into source comments.

**The fix proposed.** Replace the copied examples with a pointer, such as "the forms StartPointsDiagnostic pins". At minimum, correct France's example to WHAT-IF · YOUR PARTY GOVERNS.

**The skeptic's evidence.** The finding is correct on both counts. It is comment-only, with no runtime or player-facing effect.

1. The France example is false. G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/StartPoints.cs:118 is a '+' line of this diff. It lists "GOVERNING · OPENS 18 JUL 2024" as one of ModeLine's forms, but no branch returns that string. Line 125 returns "WHAT-IF · YOUR PARTY GOVERNS · " + opens when `WorldClock.GoverningModeOnly` is true, which is France only (WorldClock.cs:101). The other branches return NO ELECTION YET, SNAP ELECTION, RUN-UP or null.
   - History: `git log -S` shows the example came in with c753359e (s626). The return changed at 6549df2f (s631, PS-3d), and that commit touched only the return line, not the summary.
   - COMPLETED.md §631 records that the card "reads WHAT-IF · YOUR PARTY GOVERNS · OPENS 18 JUL 2024 where it read GOVERNING · OPENS 18 JUL 2024".
   - StartPointsDiagnostic.cs:53 pins the WHAT-IF form.
   - This diff turned the summary's closing '.' into ';' and appended a fourth form to the same list, so it carries a known-false list forward on a line it wrote.

2. The new US example breaks the claim convention. Line 119 is new and copies "NO ELECTION YET · OPENS 12 MAR 2024" into the comment. The date is computed, not ruled: the default branch of `WorldClock.StartDate` returns `new CampaignCalendar(LatestElectionDay(USA)).PreCampaignStart`, counted from 5 Nov 2024. That makes it a DERIVED figure. CLAUDE.md's claim convention covers every source comment, allows a DERIVED claim only as GENERATED, REFERENCED or DELETED, and says "Nobody transcribes, anywhere, in any file". Neither CLAUDE.md nor docs/archive/CLAUDE_HEAD_LONGFORM.md exempts illustrative examples.

3. No check catches either problem. CommentClaimCheck only resolves backticked `Type.Member` names. s631 shows the failure this causes: the diagnostic was updated and the comment was not.

Why each refutation fails:
- "The France error predates the diff": line 118 is rewritten in this diff and its list is extended, so it is in scope.
- "The list quotes Board 18a": Design never drew the US form (it came from a review finding), and §631 replaced the board's France form.

One caveat on the finding's suggested pointer: StartPointsDiagnostic pins the whole lines for France (53), the USA (55) and Sweden (57), but not the snap form (Germany/Italy). "The forms StartPointsDiagnostic pins" therefore must not read as the complete list.

**The skeptic's corrected fix.** Rewrite the summary at G:/UNITY/Projects/PoliSim/Assets/Scripts/Elections/StartPoints.cs:118-120. Name the modes by kind and drop the copied strings and dates. The function body four lines below holds the actual text, and the diagnostics pin it, so nothing needs to be repeated:

/// <summary>18a: the playable card's mode and opening in caption mono - one mode per kind of start (the run-up, a snap election, governing mode's what-if (PS-3d, §631),
/// and a start whose elections the game does not hold yet (PS-6 US-1, `WorldClock.NoElectionYet`), which never promises a run-up to an election the game does not hold),
/// then the day the start opens. Null for a locked card (its stamp reads LOCKED there).</summary>

`WorldClock.NoElectionYet` resolves, so CommentClaimCheck stays clean. If a pointer is wanted, do not say StartPointsDiagnostic pins every form, because the snap form is not pinned whole.

The bare minimum fix is to replace "GOVERNING · OPENS 18 JUL 2024" with "WHAT-IF · YOUR PARTY GOVERNS · OPENS 18 JUL 2024" on line 118. That fixes the false example but leaves the copied dates, including the new "12 MAR 2024" on line 119, so it still breaks the claim convention.

### 3. StartBrief's declared exception is worded wider than the predicate it names

- **Lens:** checks - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/Elections/StartBrief.cs:14

**The scenario.** The new class doc says the election's slot reports the game's own calendar "where the game holds no election for the start". Italy's snap start (TryNextPollingDay(Italy) is false) and France's governing start also hold no election. Yet their slots carry the record's polling day: "Polling day is 25 September 2022." for Italy and "Last polling day · 7 JUL 2024" for France. The new loop at StartBriefDiagnostic.cs:79-91 pins exactly that. This fix narrowed NoElectionYet's own doc this way, but not this one.

**The fix proposed.** Word it as the predicate: "where `WorldClock.NoElectionYet` holds (the USA's start, until US-8; Italy's snap start is PS-7's)".

**The skeptic's evidence.** I traced it and could not refute it. The new class doc at Assets/Scripts/Elections/StartBrief.cs:13-15 says: "where the game holds no election for the start (PS-6 US-1, `WorldClock.NoElectionYet`), the election's slot reports the game's own calendar, not the record's".

Read in its plain words, that condition covers three starts, not one:
- **Italy:** `WorldClock.TryNextPollingDay` returns false (WorldClock.cs:437, `if (id != CountryId.Sweden) { return false; }`). `SimulationManager.TryPlayerPollingDay` (SimulationManager.cs:4206-4213) reads only that answer and an ordered extra election, so an Italy game never holds its 25 Sep 2022 polling day.
- **France:** its `StartLine` reads "A WHAT-IF · NO ELECTION" (WorldClock.cs:128), and `TryNextPollingDay` is also false.
- **The USA:** the one start the doc means.

Both branches in StartBrief test `NoElectionYet` alone (StartBrief.cs:114, 203), and that predicate is USA-only (WorldClock.cs:108). So Italy's and France's starts take the record's polling day from `StartPoints.Ruled` (StartPoints.cs:81, 91, 99-100, `WorldClock.LatestElectionDay`):
- Italy's brief reads "Polling day is 25 September 2022." (StartBrief.cs:121).
- France's ledger reads "Last polling day · 7 JUL 2024" (StartBrief.cs:210, past because 7 Jul < the 18 Jul opening).
- Both cite StartPoints, the record, as their basis.

StartBriefDiagnostic.cs:78-91 pins exactly this. So the class doc's general claim is false for two of the six starts today.

It also contradicts the predicate's own doc from the same fix round. WorldClock.cs:103-107 says `NoElectionYet` is "Not a general test of whether a start's election is held: that is TryNextPollingDay's answer, and Italy's snap start (PS-7) is outside it". The StartBrief doc describes it as exactly that general test. CLAUDE.md's claim convention covers every source comment, so this counts as a real defect. It is a note, not a defect: nothing changes at runtime and no player-facing text is involved.

The same over-wide wording is at StartPoints.cs:119 in the `ModeLine` doc: "a start whose elections the game does not hold yet (PS-6 US-1, `WorldClock.NoElectionYet`) NO ELECTION YET". Italy is such a start, yet its card reads "SNAP ELECTION · OPENS 21 JUL 2022".

The finding's proposed text "(the USA's start, until US-8; ...)" matches spec line 169 ("Until US-8"). But it adds a tracking phrase that differs from NoElectionYet's own "(US-8) and the House's (US-16)", so it is better to name the predicate and not restate the timing.

**The skeptic's corrected fix.** At StartBrief.cs:13-14, make the predicate the condition instead of the general description. Replace "where the game holds no election for the start (PS-6 US-1, `WorldClock.NoElectionYet`), the election's slot reports..." with "where `WorldClock.NoElectionYet` holds (PS-6 US-1) - not every start whose election the game does not hold (that is `WorldClock.TryNextPollingDay`'s answer; Italy's snap start is PS-7's) - the election's slot reports the game's own calendar, not the record's - its basis is that ruling, not a record." For consistency, make the same change at StartPoints.cs:119: replace "a start whose elections the game does not hold yet (PS-6 US-1, `WorldClock.NoElectionYet`) NO ELECTION YET · OPENS 12 MAR 2024" with "where `WorldClock.NoElectionYet` holds (PS-6 US-1), NO ELECTION YET · OPENS 12 MAR 2024". Both are comment-only edits, so no film or bar beyond the documents tier is owed.

## The second pass - refuted by the skeptics

- [truth] The US start card still stamps the 5 Nov 2024 polling day, and 'NO ELECTION YET' beside it reads as a countdown to that election - *The finding describes what the card draws correctly, but it is wrong to grade it a defect.

What it gets right: BuildStartCard draws the stamp DateLine(us) = "5 NOV 2024" (CountrySelectorScreen.cs:621), the mode line "NO ELECTION YET · OPENS 12 MAR 2024" (:624) and the name "Presidential election" (:638). WorldClock.TryNextPollingDay returns false for the USA (WorldClock.cs:437), so nothing happen*
- [truth] US-1's instruction in the plan still prescribes 'the president and Congress as of record' - *Line 169 of the plan does still say "the president and Congress as of record", and the plan's working-tree diff only carries F8's edits. But this is not a defect in the US-1 change.

1. **Out of scope.** The task names six code files and says to ignore every other file in the working tree. `docs/specs/USA_STAGE_PLAN.md` is one of those other files, and its changes are s780/F8 work for another comm*
- [checks] The US card's head row still reads as a run-up: the 5 NOV 2024 stamp sits beside "NO ELECTION YET" - *The finding is about how a player might read the line. No stated rule is broken, and the sheet already settles the reading in the line right below the card.

1. The text is true for the build. StartPoints.cs:126 returns "NO ELECTION YET · OPENS 12 MAR 2024". The game holds no US election, and in the world no election had happened by 12 Mar 2024, so both readings are true. The string is shorter tha*

## What the author did about the second pass

- **1, 2, 3** (the mode line's summary and the brief's exception worded wider than the USA-only predicate; copied output strings, one stale since §631) - both docs name `WorldClock.NoElectionYet` as the condition (the USA's start; Italy's is PS-7's), and the summary points at the strings `StartPointsDiagnostic` pins instead of copying them.
