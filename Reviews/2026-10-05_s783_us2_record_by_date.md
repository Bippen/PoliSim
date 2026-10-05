# Review - §783, PS-6 item US-2: the record by date inside a US game (2026-10-05)

A workflow review (`polisim-staged-review`) of US-2 - `WorldClock`'s gates, rows and words, `ParliamentSystem.SeatChamberOfRecord`, `SimulationManager.SeatTheRecordOnItsDate` and `TakeOffice`, the brief's and the not-held reason's words, the Parliament page's record row, and `CongressOfRecordDiagnostic` - in three lenses: the hook's correctness, the record and the words, the checks' power. Every finding was put to a refute-first skeptic; the reports are verbatim. A second pass read the change after the fixes, in four lenses: the first pass's fixes, the record seated as a save loads, the words and the row on screen, the checks' power. **A money path:** `SimulationManager.cs` is on `Tools/bar_tier.ps1`'s list, and this file is the review its ledger row cites (`s783`).

## The first pass - confirmed (verbatim)

Three lenses - the hook's correctness, the record and the words, the checks' power - over the staged diff at `b183a332`; 19 findings survived their skeptics.

### 1. SeatChamberOfRecord was inserted inside SetSeatsFromElection's doc comment, and two 'only an election moves the chamber' claims are now false

- **Lens:** hook - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Simulation/ParliamentSystem.cs:81

**The scenario.** Lines 81-85 ("W-G1: the ONLY thing that changes a chamber - an election result ... Seats not named by the result are set to zero") now sit directly above the new method's own summary (86-87). So SeatChamberOfRecord carries two summaries, the first describing a different method, and SetSeatsFromElection (line 94) has no doc. That sentence and UpdateSeats' doc (line 55: seats 'do not move between elections') are both untrue in a US game, where SeatTheRecordOnItsDate re-seats the House on 2025-01-03 with no election held.

**The fix proposed.** Move the W-G1 summary back onto SetSeatsFromElection and put SeatChamberOfRecord above it with its single summary. Amend both claims: an election, or, where the game elects no chamber, the record by date (WorldClock.RecordSeatsChamber).

**The skeptic's evidence.** THE MISPLACEMENT HAPPENED. The hunk at `@@ -83,6 +83,14 @@` adds the new method between SetSeatsFromElection's closing `/// </summary>` and its signature. Staged file (git show :Assets/Scripts/Simulation/ParliamentSystem.cs):
  81-85  /// <summary> W-G1: the ONLY thing that changes a chamber — an election result, keyed by the same abbreviations `PartySystems` uses. Seats not named by the result are set to zero ... </summary>
  86-87  /// <summary>PS-6 US-2 (R-US1 (a)): seats the chamber of record a vintage's lists elected ...</summary>
  88     public static void SeatChamberOfRecord(Country country, ElectionVintage vintage)
  94     public static void SetSeatsFromElection(Country country, IReadOnlyDictionary<string, int> wonSeats)   <- no doc comment now
A doc comment attaches to the member that follows it, so both summaries belong to SeatChamberOfRecord. The W-G1 text describes the wrong method: "keyed by the same abbreviations" and "seats not named by the result" fit SetSeatsFromElection's wonSeats dictionary, not a vintage parameter.

The bar does not catch it. Two sibling <summary> elements are valid XML, and Unity generates no XML docs, so CS1591 never fires. This file already has the same mistake outside the diff: GetBillDirection's summary ("Bill's net fiscal direction", lines 105-125) sits above SpendingPercentChangesOf (line 129); the method it describes is at line 189.

THE STALE WORDING IS REACHED IN NORMAL PLAY, NOT ONLY IN THE DIAGNOSTIC:
- WorldClock.StartDate(USA) takes the default case, `new CampaignCalendar(LatestElectionDay(id)).PreCampaignStart`. LatestElectionDay(USA) is 2024-11-05, so a US game opens in 2024.
- At the start the 118th is seated: SeatsSourced(Usa2022) is true.
- NoElectionYet(USA) is true, so the game holds no US election.
- RecordSeatsChamber(USA) is true. On 2025-01-03, SeatTheRecordOnItsDate (SimulationManager ~3993-4004) finds the seats differ from InitialSeats(id, SeatedVintage = Usa2024) and calls ParliamentSystem.SeatChamberOfRecord (line 4001).
- Before this diff, the only runtime writers of ParliamentSeats were WorldFactory:1032 (start seeding), UpdateSeats:77 (seeding an empty chamber) and SetSeatsFromElection:102 (via GameController:6692). The diff adds a second runtime path that moves a chamber without a game election.

WHAT SOFTENS THE SECOND HALF: the author frames the record seating as "the House changed by an election, the record's" (the SeatTheRecordOnItsDate summary). Read that way, "changes only at an election" and "do not move between elections" (UpdateSeats doc, line 55) still hold loosely, since the 119th is the 2024 election's result. What is clearly wrong is "the ONLY thing", which refers to one code path, sitting on a different method. The same wording is also in the class summary (lines 25-26, "changed ONLY by an election") and the W-G1 RETIRED comment (lines 42-45, "Seats now change only at an election"). Both are outside the diff and the finding does not name them.

No runtime, numeric or money-path effect; this is about source comments only.

**The skeptic's corrected fix.** Move SeatChamberOfRecord's summary and body (staged lines 86-92) above line 81, or below SetSeatsFromElection's body. That puts the W-G1 summary back on SetSeatsFromElection and leaves SeatChamberOfRecord with its one PS-6 summary.

Reword the W-G1 summary so it references the code paths and no longer claims to be the only one. For example: "W-G1: what changes a chamber the game elects - an election result, keyed by the same abbreviations ... (a chamber the game does not elect is seated by the record on its dates: `WorldClock.RecordSeatsChamber`, `SeatChamberOfRecord`)".

Amend UpdateSeats' "do not move between elections" (line 55) the same way: they change at an election, or by the record's dates where `WorldClock.RecordSeatsChamber` holds. Optionally make the same change to the class summary (lines 25-26) and the W-G1 RETIRED comment (lines 42-45).

Keep it REFERENCED, as the claim convention requires: name the predicate and the method, and copy no dates or counts into the comment.

### 2. Record row reads the record by date while the game still holds the old state, until the first stepped day after a load

- **Lens:** hook - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/UI/GameController.ParliamentRows.cs:457

**The scenario.** Take a format-38 US save cut by a pre-US-2 build after 2025-01-20. It still holds the 118th House (REP 222, DEM 213) and Biden. The load lands PAUSED, and the hook only acts on the first stepped day. Until then the row draws THE PRESIDENT TRUMP with a 20 JAN 2025 chip (from WorldClock.TryGovernmentAt, line 457). The slip prints 'THE HOUSE: ELECTED 5 NOV 2024 · SEATED 3 JAN 2025 · REP 222 · DEM 213' (line 508: the record's words by date beside the state's seat counts). Meanwhile country.Government is still Biden, so a DEM player's role gate and the Desk still say GOVERNING. It is transient and limited to the saves the DECLARED state-not-transition premise covers.

**The fix proposed.** Either call SeatTheRecordOnItsDate once at the end of RestoreFromSave, so the state is right before the first frame. Or draw the row's president from _playerCountry.Government (Executive, FormedOn), and the House words from WorldClock.SeatedVintage compared with the state's seats.

**The skeptic's evidence.** I could not refute it. Every step of the path is in the code, but only a narrow, short-lived class of saves can reach it. "Note" is the right grade.

1. A pre-US-2 save past the dates holds the old state.
- At HEAD nothing seats the US House or president by date. US-1's doc at HEAD says "the president and the House seated at the start hold".
- A git grep at HEAD finds no SeatedVintage or InitialSeats re-seating in SimulationManager.
- The US start is the run-up before 2024-11-05 (WorldClock.StartDate's default branch). So a US world stepped past 2025-01-20 by a HEAD build still holds Usa2022, which is REP 222 / DEM 213 (PartySystem.cs:777), and Biden.

2. The load does not put it right.
- SeatTheRecordOnItsDate() has exactly one call site: inside AdvanceDay at staged SimulationManager.cs:475, after `CurrentDate = CurrentDate.AddDays(1)` (:314).
- RestoreSaveState, SaveGameService.RestoreInto (lines 230-245) and GameController.RestoreFromSave (GameController.cs:893-925) never call it.
- RestoreUiDrafts ends with `_gameSpeed = GameSpeed.Paused;` (:1075).
- Update returns at `if (_gameSpeed == GameSpeed.Paused) { return; }` (:751-754), before `_simulationManager.AdvanceDay()`.

3. While paused, the row reads the record and every other reader reads the state.
- ParliamentRows.cs:457 `bool hasRecord = WorldClock.TryGovernmentAt(country, today, out ...)` draws TRUMP with a 20 JAN 2025 chip.
- :508 `slip.Add("THE HOUSE: " + WorldClock.RecordStanding(country, today) + ...)` puts the counts from `_playerCountry.ParliamentSeats` (:504) beside that text. The slip reads "ELECTED 5 NOV 2024 · SEATED 3 JAN 2025 · REP 222 · DEM 213", which is the 119th's dates with the 118th's seat counts.
- Desk.cs:278-283 RoleChip reads `_playerCountry.Government.RoleOf(...)`, so a DEM player sees GOVERNING.
- The House's party mark does not show the gap: REP is the largest party in both the 118th (222) and the 119th (220, PartySystem.cs:528).

4. The diagnostic does not cover this frame.
- Check (f) only proves that stale state is "put right on its next day".
- Adopt() asserts the load lands PAUSED but checks nothing about that paused frame.

Reach (this is why it is only a note):
- SaveGameService.cs:162 refuses any save with `version != CurrentSaveVersion` (38).
- Format 38 dates from §770 (2026-10-04). An affected save must therefore be a v38 US game cut by a 2026-10-04/05 build before US-2, past 2025-01-03.
- The save folders (LocalLow/DWELOP Games/Incumbent/saves and DefaultCompany/PoliSim/saves) hold only Swedish saves from August-September, all older than v38. No affected save exists on this machine.
- The mismatch clears on the first stepped day, and it falls inside the DECLARED state-not-transition premise.

**The skeptic's corrected fix.** Cleanest fix: make the state right at load, so the row, the Desk, the role gate and the hemicycle all agree.
- Expose the hook, e.g. `public void SeatTheRecordNow() => SeatTheRecordOnItsDate();` on SimulationManager.
- Call it in SaveGameService.RestoreInto right after `sim.RestoreCampaign(...)`. At that point the world and PlayerCountryId are already set.
- That covers the controller's RestoreFromSave and the batch load path. RestoreFromSave forks the shadow baseline after RestoreInto, so the fork also sees the corrected state.
- The call is idempotent and does nothing for any save a US-2 build writes, because that state already matches the record.
- It runs TakeOffice (budget-window resets) on a money path, so the ledger row must cite it.
- Add a diagnostic check: put the 118th and Biden back on a post-oath world, save it, adopt it through RestoreFromSave, and assert the 119th and Trump before any step.

Cheaper alternatives, given the narrow reach:
- UI-only: in DrawRecordRow, draw the president from `_playerCountry.Government` when `Government.Executive != ofRecord.President`. Use `RecordStanding` only when the state's seats equal `PartySystems.InitialSeats(country, WorldClock.SeatedVintage(country, today))`.
- Or extend the DECLARED premise to say that before a pre-US-2 save's first stepped day, the record row reads the record by date while the Desk and the role gate still read the save's state.

### 3. TakeOffice's doc claims a close that one install path does not do

- **Lens:** hook - **reviewer:** note - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Simulation/SimulationManager.cs:4020

**The scenario.** The doc says "the close every installation ends with (the arrival budget window reset, a budget window the player no longer governs closed)". Only Install (4046-4047) and InstallSuccessor (3145-3146) do both. The election-night install at GameController.cs:6429 resets the arrival window but closes nothing, because CloseBudgetWindowIfNotGoverning is private to the manager. This is a claim in a money-path file's doc that the code does not hold.

**The fix proposed.** Reword to 'the close Install and InstallSuccessor end with', or route the election path through the same close (a separate change).

**The skeptic's evidence.** Re-graded from note to minor: the comment states something false about the code, in a money-path file, and CLAUDE.md's claim convention (its top rule) forbids exactly that. There is no runtime effect from the diff itself.

**The claim (staged SimulationManager.cs)**
- 4020-4021: `/// <summary>PS-6 US-2: a government of record takes office - the close every installation ends with (the arrival budget window reset, a budget window the player no longer governs closed).`
- TakeOffice itself does both: line 4024 sets the government, 4025 `ResetArrivalBudgetWindow(country.Id);`, 4026 `CloseBudgetWindowIfNotGoverning(country);`.
- InstallSuccessor (3144-3146) and Install (4045-4047) also do both.
- 3155: `private void CloseBudgetWindowIfNotGoverning(Country country)`. It is private, so GameController cannot call it.

**The install that skips the close (GameController.cs:6429, not touched by the diff)**
`if (formedView != null && formedView.HasGovernment) { _playerCountry.Government = PoliSim.Elections.GovernmentRecord.FromView(...); _simulationManager.ResetArrivalBudgetWindow(PlayerCountryId); }`
It resets the arrival window and closes nothing.

**Election night counts as an installation in the code's own words**
- GC 6407 (§646): "where the Speaker's round runs, the night installs nothing", which implies the other branch installs.
- SM 3153 (pre-existing, same overclaim): CloseBudgetWindowIfNotGoverning is "Called where a government is installed."

**The path is live today, for Poland**
- SM 3232-3234: `RoundsApply` is true only for Riksdag and Bundestag rules.
- ConfidenceProcedure.cs:34: `RulesOf` gives every country except Sweden and Germany `Rules.Unsourced`.
- NationalElection.cs:16: `PolandDistricts` is a live election method.
- France, Italy and the USA return NotImplemented and leave early at GC 6389. So Poland's election night installs at 6429 with no close.

**Nothing else on the election path closes the window**
- RunNationalElection (GC 6638-6701) and ApplyElectionVerdict (6714-6726) never touch the budget sets.
- The only places that remove `_pendingBudgetProcessByCountry` are:
  - `TableGovernmentBudget` (SM 1182): unreachable while a window is open, because `TryOpenBudgetProcess` returns at its first check (2418-2421).
  - `IntroduceBudgetBill` (1514): role-gated at 1507; `PlayerMayIntroduce` (2653-2657) refuses a player who is not prime minister.
  - `CloseBudgetWindowIfNotGoverning` (3158).
  - The reset when a save is restored (4669); 4696 then re-adds the open window from the save.
- Reviews/2026-09-30_s698_constructive_vote.md:41: defect 2's close was added "after `InstallSuccessor` and after the Riksdag `Install` (the same gap)". Election night was never given it.

**Why the false claim matters (the gap itself is pre-existing, outside the diff)**
- Scenario: the budget-window hold is switched off (`GameSettings.HoldOnBudgetWindow`, default on, toggled at GC 1531-1532), and a Polish prime minister leaves a window open into an election they lose.
- The window stays open. The new AI government can never table its budget, and the player cannot introduce one. This is §698 defect 2's stuck window.
- With the hold on, the clock stops while a window is open (GC 763), so this needs a window that opens on polling day itself.
- The new "every installation" sentence tells a reader (US-7 included) that this path is covered when it is not.

**The skeptic's corrected fix.** In TakeOffice's summary, drop the claim about every installation and state only what the method does, linking the helper:

`/// <summary>PS-6 US-2: a government of record takes office, with the arrival budget window reset and a budget window the player no longer governs closed (<see cref="CloseBudgetWindowIfNotGoverning"/>, §698). US-7 is to call it with the president the game elects.</summary>`

That also turns "US-7 calls it" into the open item it actually is (no US-7 code exists yet).

Do not use the reviewer's wording, "the close Install and InstallSuccessor end with". It is true today, but it copies a list of call sites into prose, which the claim convention says to reference or delete, and it goes stale the moment the next install path lands (US-7's).

Making Poland's election-night install (GameController.cs:6429) close the window is a separate money-path change, not part of US-2. It needs a public entry point on the manager, its own review row, and a diagnostic check like ConstructiveVoteDiagnostic (e). File it as an errand.

Optional, outside the staged diff: the same overclaim in CloseBudgetWindowIfNotGoverning's doc ("Called where a government is installed", SM 3153) can be fixed in the same pass.

### 4. No-policy shadow seats the record only after a load, not in a new game

- **Lens:** hook - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/UI/GameController.cs:2178

**The scenario.** In a new game (OpenWorldAt, line 2178) the no-policy shadow is new ShadowBaseline(seed) with no PlayerCountryId, so its USA keeps the 118th House and Biden for the whole run. After a load (RestoreFromSave, line 911) the shadow is forked through RestoreInto, which sets PlayerCountryId (SaveGameService.cs:243). That shadow seats the record, and its PlayerGoverns(USA) flips at the oath, which switches IndexSpendingLines between the AI's and the player's book at its turn boundaries (SimulationManager.cs:6327). So the 'without your policies' line of one game differs between unbroken and loaded play. The seeded shadow gets no economic mover from the record: with no player country, AiFinanceMinistry and IndexSpendingLines read neither the House nor the president. The asymmetry predates US-2 (the seeded shadow never had a player); US-2 adds a mid-run flip inside the load-forked shadow.

**The fix proposed.** Nothing owed in US-2's hook. If the 'without' line should match across a load, it is the shadow's construction (seeded vs forked) that needs ruling, not the hook.

**The skeptic's evidence.** I could not refute any step of the chain. It is a true note, not a defect.

1. **A new game's shadow never seats the record.** GameController.cs:2178 builds the shadow with `new ShadowBaseline(SimulationRandom.MasterSeed)` right after `SetEpoch(start)`. That constructor (ShadowBaseline.cs:53-70) never sets `_sim.PlayerCountryId`, and the hook's first line is `if (!PlayerCountryId.HasValue) { return; }`. So the shadow's USA keeps WorldFactory's epoch seating: the 118th House (WorldFactory.cs:1032, `InitialSeats(.., Seated)`) and Biden (WorldFactory.cs:1092, `Government = AtStart(.., EpochDate ..)`). The US start is the run-up before 5 Nov 2024 (`WorldClock.StartDate` default, `PreCampaignStart`). Nothing else re-seats it: the USA holds no election, and `ShadowBaseline.AdvanceTurn` (lines 138-139) steps only `AdvanceDay` and `AdvanceTurn`.

2. **A loaded game's shadow does seat it.** GameController.cs:911 forks the shadow (ShadowBaseline.cs:93-118) through `RestoreInto`, and SaveGameService.cs:243 sets `sim.PlayerCountryId = save.PlayerCountryId`. The fork's `AdvanceDay` then runs the hook (SimulationManager.cs:475), which re-seats the House and calls `TakeOffice`.
   - `PlayerGoverns` (2631-2641) reads `Government.RoleOf(PlayerPartyAbbrev)`, and that field is saved with the world (Country.cs:946).
   - So the role flips at the oath; the new diagnostic's check (b) asserts exactly this (DEM governs until the day before, REP from the day).
   - `IndexSpendingLines` line 6327 (`bool aiBudget = !PlayerGoverns(country);`), reached through `ResolveSpendingForTurn` at 6246, then switches the book at the fork's next turn boundary.
   - The finding's line references all match the staged code.

**Narrower than the finding states:**
- **Which saves:** only a save dated before 2025-01-20. Both record dates fall in a US game's first turn (`DaysPerTurn` 365).
- **Which parties:** only a player party that leads on exactly one side of the oath (DEM or REP). A created party is in opposition on both sides, so its role does not flip.
- **The House re-seat alone moves nothing economic in a shadow.** No bills or budget process run there: `AdvanceCountryDayTick` (line 308) is never called by `ShadowBaseline`.

**Why it is not a defect of US-2:**
- **Declared:** the hook's own doc says so: "Forks reach it as the game does; a world with no player country (the seeded shadow, the trajectory dump) never does".
- **The fork follows the game:** it takes the same oath and role flip the real game takes. If the fork were not seated, a DEM player's fork would go on governing under Biden while the game has Trump, which is worse.
- **Older cause:** the 'without' line already differed between unbroken and loaded play before US-2, and mostly for another reason. Under §618 R5 (GameController.cs:908-913) the load forks the shadow from the loaded state, which carries a player and every policy up to that day. US-2 adds one smaller term on top.

**Side note (predates US-2, PS-3e):** in a fork, an AI-governed player country's finance ministry goes "by bill" (4857-4861). `TableGovernmentBudget` is only reached through `TryOpenBudgetProcess` (2434), which is never called in a shadow. So after a DEM player's oath, the fork's "AI book" is the AI indexation without the ministry. It is not the same as the seeded shadow's AI book.

**The skeptic's corrected fix.** Nothing is owed in US-2's hook or anywhere in the diff. The hook's doc already declares the behaviour, and the fork's seating and oath follow the real game. If anything is wanted, add one clause to the hook's doc: a load-forked shadow takes the oath's role flip, so after a pre-oath load its 'without' line changes book at the oath. Whether the no-policy shadow should be seeded (no player) or forked (with a player) belongs to §618 R5 and needs a ruling outside US-2.

### 5. Pre-existing, outside this diff: the impact ledger's except-worlds run one period ahead of the live clock

- **Lens:** hook - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/UI/GameController.cs:6112

**The scenario.** The controller calls _impactLedger.AdvanceTurn from its turn boundary, after that period's 365 AdvanceDay calls (Update, lines 773-791; AdvanceTurn, line 6112). On a family's first touch the ledger forks the live world at that boundary day (PolicyImpactLedger.cs:142). The fork then runs ShadowBaseline.AdvanceTurn's own 365 days plus a boundary (ShadowBaseline.cs:138-139). So every except-world's date runs one period ahead of the live world, and LinesFor compares worlds a year apart. PolicyImpactLedgerDiagnostic calls the ledger before the days (lines 164-168), so it does not catch this. The load-forked shadow likewise hits its boundaries at load+365k. The hook in any fork acts on that fork's own dates. For the US no boundary fork predates 2025-03-12, so no except-world crosses a record date.

**The fix proposed.** Its own item: on first touch, fork the except-world at the turn's start, or advance it only through the boundary that turn.

**The skeptic's evidence.** I could not refute it. The failing path holds, but the bug is outside the diff and the US-2 hook does not touch it.

Play's order:
- GameController.cs:773 runs `bool turnBoundaryCrossed = _simulationManager.AdvanceDay();` once per day. At the boundary, lines 789-791 call `AdvanceTurn()`.
- Line 6112 then calls `_impactLedger?.AdvanceTurn(...)`, and line 6118 calls `_simulationManager.AdvanceTurn(decisions)`.
- SimulationManager.AdvanceTurn is the boundary only. It runs no days, and `CurrentTurn++` sits at its end (line 4901).
- So the ledger is called with the period's 365 days already run on the live world and its boundary still pending.

The fork:
- On first touch, PolicyImpactLedger.cs:142 creates `new ShadowBaseline(realSim, realWorld, _playerCountryId)`.
- That is a save/load round trip that copies the live `CurrentDate`, i.e. the boundary date (SaveGameService.cs:99, 240).
- Line 147 then calls `world.AdvanceTurn(Strip(...))`. ShadowBaseline.cs:138-139 runs `for (d < DaysPerTurn) _sim.AdvanceDay(); _sim.AdvanceTurn(...)`.
- At boundary k the live world is at 365k days. The fork copies that, then runs another 365 days plus the boundary, while the live world runs only the boundary.
- At every later boundary both worlds add 365 days, so the one-period gap never closes.
- LinesFor (lines 158-167) computes `real - except` across worlds 365 days apart. The interaction line (173) absorbs the error, so the identity assertion still passes.
- The 'Your policies' card prints these lines in play (GameController.Statistics.cs:532-550).

The checks miss it:
- PolicyImpactLedgerDiagnostic.cs:166-169 and 219-221, and UiScreenshotDriver.cs:5871-5873, all call the ledger BEFORE the days.
- So 'lazy fork EXACT' proves an order that play does not use.

Load path: GameController.cs:911 forks the no-policy shadow at the load date, so its boundaries fall at load+365k. This is also confirmed.

Pre-existing:
- GameController.cs, PolicyImpactLedger.cs and ShadowBaseline.cs are not in the staged diff.
- For each of them the index hash equals the working-tree hash.
- Both sites date from 292bd7ba and cf18f273 (2026-08-31).

Effect on the US-2 hook:
- The hook does run in forks, because RestoreInto sets PlayerCountryId (SaveGameService.cs:243).
- The US epoch is CampaignCalendar(2024-11-05).PreCampaignStart, 34 weeks earlier: 2024-03-12. The first boundary, and so the earliest possible except-world, is therefore 2025-03-12, after both 2025-01-03 and 2025-01-20.
- After that date the hook does nothing in any fork. Trump's row is Open (WorldClock.cs:320).
- The 120th's row (2027-01-03) is unsourced, so SeatedVintage falls back to Usa2024 and SameSeats is true.
- One small inaccuracy in the finding: an except-world forked at 2026-03-12 does cross 2027-01-03, but nothing changes there.
- The only fork that crosses both seating dates is the load-forked shadow of a save cut before 2025-01-03. There the hook seats the record on the shadow's own dates, which is correct for those dates; the date offset is the pre-existing one.

Severity: real, but pre-existing and outside the diff, with no interaction with the hook. That makes it a note for this review and its own item elsewhere.

**The skeptic's corrected fix.** File this as its own item, not in US-2. The finding's second alternative is the right one. Make it general so the load-forked shadow is fixed too:
- ShadowBaseline.AdvanceTurn should step days up to the boundary its own manager owes, then run that boundary. The owed date is `SimulationManager.EpochDate.AddDays((_sim.CurrentTurn + 1) * DaysPerTurn)`, which is the private ComingBoundaryDate. Replace the fixed DaysPerTurn loop with `while (_sim.CurrentDate < owed) _sim.AdvanceDay();`.
- That steps 0 days for a fork whose boundary is pending (play's first-touch fork), 365-d days for a fork taken at load on day d, and 365 days for the seeded shadow and the diagnostic's order.
- Add a play-order case to PolicyImpactLedgerDiagnostic: days first, then the ledger at the boundary, then the boundary. 'Lazy fork EXACT' then covers the order the controller uses.

### 6. The record row prints Biden's surname as "JR."

- **Lens:** record - **reviewer:** defect - **skeptic:** defect
- **Where:** Assets/Scripts/UI/GameController.ParliamentRows.cs:496

**The scenario.** In every US game from its start (12 Mar 2024) to 19 Jan 2025, ofRecord.President is "Joseph R. Biden Jr. (DEM)" (WorldClock.cs:319). Line 460 strips " (DEM)" and leaves "Joseph R. Biden Jr.". Surname (line 632) returns the last word. The row therefore reads "OF RECORD, BY DATE · THE HOUSE [REP] SEATED [3 JAN 2023] · THE PRESIDENT [DEM] JR. [20 JAN 2021]". "Donald J. Trump" gives TRUMP, so the first ten months of every US game are wrong. No check reads the row's words, and the dry film asserts no text.

**The fix proposed.** Drop a generational suffix (Jr./Sr./II/III) before taking the last word. Alternatively, carry the surname in the record's own row next to President. Pin the row's words for both presidents, for example by moving the row's word list into a helper with no UI dependency and pinning BIDEN and TRUMP in CongressOfRecordDiagnostic.

**The skeptic's evidence.** I traced the path and found nothing that prevents it.

1. The record row. WorldClock.cs:319 (staged) has the USA's first row: new GovernmentOfRecord("Joseph R. Biden Jr. (DEM), president", ..., D(2021, 1, 20), D(2025, 1, 20), ..., president: "Joseph R. Biden Jr. (DEM)"). Holds() is date >= From && date < Until, so TryGovernmentAt(USA, d) returns this row for every d up to and including 2025-01-19.

2. The surname. GameController.ParliamentRows.cs:459-460 sets president = ofRecord.President ?? ofRecord.Head and then cuts it at " (", leaving "Joseph R. Biden Jr.". Line 496 calls Words(Surname(president).ToUpperInvariant(), caption). Surname (lines 632-637) is `int space = name.LastIndexOf(' '); return space < 0 ? name : name.Substring(space + 1);`, which returns "Jr.". The row prints "JR.". PoliSimWidgets.MeasuredLabel (PoliSimWidgets.cs:370) draws the string unchanged.

3. The row draws for this president. The only gate in DrawRecordRow is `_playerCountry == null || !WorldClock.RecordSeatsChamber(PlayerCountryId)`, and RecordSeatsChamber is true for the USA. `head = hasRecord && RecordSeatsExecutive(country)` is also true for the USA. DrawParliamentPoliticalBlocks calls DrawRecordRow at ParliamentRows.cs:49 and is itself called unconditionally by the Parliament tab (GameController.PoliticsV35.cs:182).

4. Every US game spends about ten months in this window. StartDate(USA) has no case of its own and takes the default: new CampaignCalendar(2024-11-05).PreCampaignStart = 2024-11-05 - (8+26)*7 days = 2024-03-12. StartBriefDiagnostic pins "OPENS 12 MAR 2024", and GameController.OpenWorldAt sets the epoch to WorldClock.StartDate. From 12 Mar 2024 to 19 Jan 2025 the row therefore reads "... THE PRESIDENT [DEM mark] JR. [20 JAN 2021]". Trump's row is fine: "Donald J. Trump" gives TRUMP.

5. Nothing catches it. A grep for "Jr" across Assets finds no code that handles the suffix, only WorldClock.cs:319 and two diagnostics that pin the full Executive string. CongressOfRecordDiagnostic checks Government.Executive and RecordStanding's words, never the row's surname. The film_scope line drys783 is a dry film, which checks layout, not words.

6. The bug is new in this diff. The older USA reference row (WorldClock.TryReference) uses its own last-word surname, but its "after" government is Trump, so it never prints Biden's name. Surname was written for Poland's DrawPresidentRow, whose names carry no suffix. The new record row is the first caller that passes it a name with "Jr.".

Severity: a defect, but display only. There is no effect on the model's state or the money path, and the slip still prints "JOSEPH R. BIDEN JR." correctly. Even so, the row's whole job is to name the president of record on screen, and it gets the name wrong on the opening day of every US game and for about ten months after.

**The skeptic's corrected fix.** Two options in GameController.ParliamentRows.cs, plus a pinned check.

Option A: skip a trailing generational suffix in Surname. Poland's names have none, so its rows are unchanged:

private static string Surname(string name)
{
    if (string.IsNullOrEmpty(name) || name.Contains("'s candidate")) { return name ?? string.Empty; }
    string[] words = name.Trim().Split(' ');
    int last = words.Length - 1;
    // a generational suffix is not the surname ("Joseph R. Biden Jr." -> Biden)
    if (last > 0 && (words[last] == "Jr." || words[last] == "Jr" || words[last] == "Sr." || words[last] == "Sr" || words[last] == "II" || words[last] == "III" || words[last] == "IV")) { last--; }
    return words[last].TrimEnd(',');
}

Option B, closer to the claim convention: carry the surname in the record itself. Add an optional surname argument to GovernmentOfRecord, set "Biden" and "Trump" on the USA's two rows, and have DrawRecordRow prefer it over parsing.

In either case, pin the words. The row is IMGUI, so move the choice into a helper with no UI dependency (for example a static WorldClock.RecordPresidentSurname(CountryId, DateTime)) and have DrawRecordRow call it. Then check in CongressOfRecordDiagnostic that it returns BIDEN on the eve (2024-12-28) and on 2025-01-19, and TRUMP on 2025-01-20.

### 7. After 20 Jan 2029 the row still shows Trump as "of record, by date", although the record ends his term that day

- **Lens:** record - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/Elections/WorldClock.cs:320

**The scenario.** With DaysPerTurn 365 and the start on 12 Mar 2024, a US game reaches 2029-01-20 in its fifth turn. Trump's row is Open, so TryGovernmentAt keeps returning him. Under "OF RECORD, BY DATE" the row draws "THE PRESIDENT [REP] TRUMP [20 JAN 2025]" and the slip says "IN OFFICE FROM 20 JAN 2025". records_by_date.md's executive table gives his term as "(open; the term ends at noon 2029-01-20, DERIVED by the 20th Amendment)". The House gets R-US1's "not on record · the last stands" words for the term after the record; the president gets none. No DECLARED premise names the reading that the last president of record stays on past the term the record derives, until US-8.

**The fix proposed.** Say it on screen the way the House does. From the record's derived term end (computed from From by [CONST-AM] §1, not typed), the row and slip should read something like "TERM OF RECORD ENDED 20 JAN 2029 · THE LAST STANDS". Label the standing-on reading DECLARED in SeatTheRecordOnItsDate's doc and in DrawRecordRow's doc.

**The skeptic's evidence.** I could not refute it. The scenario is reachable, nothing in the code handles it, and the asymmetry is new in this diff.

Reachability: WorldClock.StartDate(USA) is CampaignCalendar(2024-11-05).PreCampaignStart = 2024-03-12, and DaysPerTurn = 365. 2029-01-20 is day 1775, so it falls in the fifth turn. A US game can get that far. NoElectionYet(USA) means no election verdict is ever reached, and the only places that set _isGameOver are DismissScenarioVerdict (GameController.cs:2099) and ApplyElectionVerdict (:6718). So a US game with no scenario runs on indefinitely.

The record: the staged WorldClock.cs:320 still reads `new GovernmentOfRecord("Donald J. Trump (REP), president", new[] { "REP" }, null, D(2025, 1, 20), Open, ...)`. The diff did not touch this row. In the same diff the House was closed and continued: line 212 `... D(2025, 1, 3), D(2027, 1, 3), "...its term ends 3 Jan 2027 [SEN-C2]"` and line 215 `new ChamberOfRecord(ElectionVintage.Usa2026, ... Open, "...not on record")`. TryGovernmentAt (:342-347) uses `Holds(date) => date >= From && date < Until`, so it returns Trump for every date from 2025-01-20 on. ElectionsData/usa/records_by_date.md:84 gives `(open; the term ends at noon 2029-01-20, **DERIVED** by the 20th Amendment)`.

The view (DrawRecordRow, new in this diff): `bool hasRecord = WorldClock.TryGovernmentAt(country, today, out ofRecord); bool head = hasRecord && WorldClock.RecordSeatsExecutive(country);` and then `Words("THE PRESIDENT"...); Mark(ofRecord.Cabinet[0]); Words(Surname(president)...); Chip(DeskDay(ofRecord.From));`. After 2029-01-20 this draws "OF RECORD, BY DATE · THE PRESIDENT [REP] TRUMP [20 JAN 2025]". The slip reads "NO US ELECTION IS HELD IN THIS GAME YET: THE RECORD'S HOUSE AND PRESIDENT ARE SEATED ON THE RECORD'S DATES" and "THE PRESIDENT: DONALD J. TRUMP · IN OFFICE FROM 20 JAN 2025". After 2029-01-20 the first of those lines is false for the president. Only the House has an else branch: `else { Words("THE ONE ELECTED " + DeskDay(chamber.ElectionDay) + " NOT ON RECORD · THE LAST STANDS", muted); }`.

The hook: SeatTheRecordOnItsDate checks `TryGovernmentAt(id, CurrentDate, out g) && country.Government?.Executive != g.President`. That never fires past 2029, so Trump stays. Neither its doc nor DrawRecordRow's doc names this case. CongressOfRecordDiagnostic only steps to 2025-01-22, plus RecordStanding for 2027-01-03.

Why minor and not a defect: the seating itself is RULED. R-US1 (a) seats "the president of record at noon on 20 Jan 2025 until the game holds the presidential election (US-8)", and CLAUDE.md's world-clock rule says the "head of state hold[s] as of record". Only the words are missing, and only in the build window before US-8. Still, the plan's own R-US3 (b) calls keeping an incumbent "wrong after noon on 20 January", and US-28 has Trump barred in 2028. So "of record, by date" in 2029 is a claim the record does not make.

The same horizon problem also affects the House. The Usa2026 row is Open, so from 2029-01-03, when the 121st would be sitting, RecordStanding and the row still say "THE ONE ELECTED 3 NOV 2026 ... NOT ON RECORD".

One correction to the finding: the stand-on is not an unnamed premise. It is the ruling's reading, so it should be cited as R-US1 (a), not labelled DECLARED.

**The skeptic's corrected fix.** 1. Keep Trump's row Open. If it is closed with no successor, TryGovernmentAt goes false past 2029-01-20, DrawRecordRow drops the president, and GovernmentRecord.AtStart throws on any such date ("has no government of record").

2. Derive the term end of a Presidency row instead of typing it: From.AddYears(4), from Art. II §1's four-year term ending at noon on 20 January under the 20th Amendment §1 [CONST-AM]. One way is a WorldClock helper, an executive counterpart of RecordStanding, for example `ExecutiveStanding(id, date)`. It returns "TERM OF RECORD ENDED 20 JAN 2029 · THE LAST STANDS" once date >= that end.

3. In DrawRecordRow, once the term has ended, draw those words in place of the from-chip. Add the slip line "THE PRESIDENT: DONALD J. TRUMP · IN OFFICE FROM 20 JAN 2025 · THE TERM OF RECORD ENDED 20 JAN 2029; NO US ELECTION IS HELD IN THIS GAME YET, SO THE LAST PRESIDENT OF RECORD STANDS". Reword the slip's first line so it stays true when either the House or the president stands past its record.

4. In SeatTheRecordOnItsDate's doc and DrawRecordRow's doc, cite the stand-on as R-US1 (a) ("until the game holds the presidential election (US-8)"), not DECLARED. Note that the words are the president's counterpart of the 120th's "said on screen".

5. Pin the words with a CongressOfRecordDiagnostic line at 2029-01-20.

6. Optionally treat the House the same way past 2029-01-03, where the Open Usa2026 row keeps naming the 2026 election.

### 8. After 3 Jan 2029 the House words name the 2026 election as the current chamber

- **Lens:** record - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/Elections/WorldClock.cs:215

**The scenario.** The 120th's row is Open, so ChamberAt(USA, d) returns it for every date from 2027-01-03 onward. In 2029 and later the row reads "THE ONE ELECTED 3 NOV 2026 NOT ON RECORD · THE LAST STANDS". The slip (RecordStanding) reads "THE ONE ELECTED 3 NOV 2026 IS NOT ON RECORD · THE ONE ELECTED 5 NOV 2024 STANDS". By the record's own [USC-7] (every even-numbered year) and [CONST-AM] §1 (terms end at noon on 3 January), the chamber of record from 2029-01-03 is a later House, so the words name the wrong House as the one not on record. SeatingDeviation, which only reaches the log, says the same.

**The fix proposed.** Word the unsourced branch so it stays true for every later term, for example "NO HOUSE ELECTED AFTER 5 NOV 2024 IS ON RECORD · THE ONE ELECTED 5 NOV 2024 STANDS". Alternatively, derive each later House's election day and term start in RecordStanding by [USC-7] and [CONST-AM] rather than relying on one Open row. Label the reading DECLARED.

**The skeptic's evidence.** The path can be reached, and nothing in the code handles it.

1. The 120th's row is new in this diff and has no end date. WorldClock.cs:215 (staged): `new ChamberOfRecord(ElectionVintage.Usa2026, D(2026, 11, 3), D(2027, 1, 3), Open, ...)` with `Open = DateTime.MaxValue` (line 77). `Holds(date) => date >= Convened && date < Until` (line 45). So `ChamberAt(USA, d)` returns Usa2026 for every d from 2027-01-03 on. The same diff closes the 119th at its term's end (line 212: `D(2027, 1, 3) ... its term ends 3 Jan 2027 [SEN-C2]`) but does not close the 120th.

2. The record contradicts the open-ended row. ElectionsData/usa/records_by_date.md:73 [SEN-C3] quotes "to the end of the 120th Congress on January 3, 2029". Line 74 [SEN-C1] names "the end of the 121st Congress on January 3, 2031". Line 161 [USC-7] says "in every even numbered year ... Congress commencing on the 3d day of January next thereafter". Line 153 [CONST-AM] §1 says Representatives' terms end "at noon on the 3d day of January". So the House on any day from 2029-01-03 is the one elected 2028-11-07, not the 2026 one.

3. The words on screen:
- GameController.ParliamentRows.cs (staged DrawRecordRow) computes `chamber = WorldClock.ChamberAt(country, today)` and, when it is not sourced, draws `Words("THE ONE ELECTED " + DeskDay(chamber.ElectionDay) + " NOT ON RECORD · THE LAST STANDS", muted)`. In 2029 that reads "THE ONE ELECTED 3 NOV 2026 NOT ON RECORD · THE LAST STANDS". DeskDay is `d MMM yyyy`, upper-cased.
- The slip adds `"THE HOUSE: " + WorldClock.RecordStanding(country, today)`. RecordStanding (WorldClock.cs:136-143) returns "THE ONE ELECTED 3 NOV 2026 IS NOT ON RECORD · THE ONE ELECTED 5 NOV 2024 STANDS".
- DrawParliamentPoliticalBlocks is called with no country gate from GameController.PoliticsV35.cs:182. DrawRecordRow's only gate is `RecordSeatsChamber(PlayerCountryId)`, which is true for the USA.

4. A US game can reach 2029.
- `_isGameOver` is set only by a scenario verdict (GameController.cs:2099) or an election verdict (line 6718), and a US game holds no election.
- R-US6 was ruled (b) (USA_STAGE_PLAN.md:474): the limit binds the person, "never the run".
- The plan expects runs to reach 2032 (R-US13).
- VeryFast is 0.25 s per day (GameController.cs:327), so 2024-03-12 to 2029-01-03 is about 1,758 days.

5. No pin covers it. CongressOfRecordDiagnostic (c) checks only 2027-01-03 (`day120`, line 132), and so does WorldClockDiagnostic (`opens120`).

Correction to the finding: SeatingDeviation does not reach even the log in 2029. In the game it is called only with start dates (GameController.cs:2197, StartBrief.cs:135, the WorldClock picker at line 620); otherwise only WorldClockDiagnostic calls it, at 2027-01-03. The RecordStanding log line (SimulationManager:4003) fires only on a re-seat. So the row and its slip are the only surfaces affected.

Why minor: only the words are wrong. SeatedVintage still resolves to Usa2024 through LatestSourced, so the seated House is the one R-US1 (a) asks for. The path needs a US game played about 4.8 game years in, before US-16 removes the row.

**The skeptic's corrected fix.** Do not just close the 120th row at D(2029,1,3). ChamberAt would then throw InvalidOperationException ("a dissolved chamber and no new one convened") on every day from 2029-01-03. The call path is SeatTheRecordOnItsDate -> SeatedVintage -> ChamberAt, so a US game would crash in the day loop. Fix the words instead, in one of two ways.

(a) Word the after-the-record branch so it stays true for every later term, without naming c.ElectionDay:
- RecordStanding: "NO HOUSE ELECTED AFTER " + Day(stands.ElectionDay) + " IS ON RECORD · THE ONE ELECTED " + Day(stands.ElectionDay) + " STANDS".
- The row's else-branch: "NO HOUSE ELECTED AFTER " + DeskDay(WorldClock.ElectionDayOf(country, WorldClock.SeatedVintage(country, today))) + " ON RECORD · THE LAST STANDS". ElectionDayOf is already public.
- SeatingDeviation's c.ElectionDay > RecordDate branch: drop the specific election day.
- Say in the Usa2026 row's comment that the Open row stands for every House elected after the record's date (DECLARED). Note there that its own term ends 2029-01-03 by [SEN-C3] and [CONST-AM] §1, and that the row is left Open only so ChamberAt never throws.

(b) Alternatively, derive the House on the date by [USC-7] and [CONST-AM] §1, labelled DERIVED. The term starts on 3 Jan of the latest odd year Y with D(Y,1,3) <= date. The House was elected on the Tuesday after the first Monday in November of Y-1 (2028-11-07 for 2029). Print that day in RecordStanding and the row in place of the 120th's fixed day.

Either way, re-pin CongressOfRecordDiagnostic (c): its exact standing120 string. Re-pin WorldClockDiagnostic too, whose `Contains("NOT ON RECORD")` breaks under (a). Add a date after 2029-01-03 (e.g. 2029-06-01) to both pins, holding the 119th's table seated and the corrected words.

### 9. A pre-US-2 save loaded past 3 Jan 2025 shows the record's dates next to the stale House until a day is stepped

- **Lens:** record - **reviewer:** minor - **skeptic:** note
- **Where:** Assets/Scripts/UI/GameController.ParliamentRows.cs:508

**The scenario.** Take a US save made before this build at 2025-06-01. It holds the 118th (REP 222 · DEM 213) and Biden, which is the state the DECLARED premise puts right "on its first day". RestoreFromSave (GameController.cs:893) leaves the game paused and never calls AdvanceDay, which is the hook's only caller (SimulationManager.cs:475). Until the player steps a day, the slip pairs the record's dates (lines 455-457) with the seated seats (504-507): "THE HOUSE: ELECTED 5 NOV 2024 · SEATED 3 JAN 2025 · REP 222 · DEM 213", the 119th's dates with the 118th's numbers. The row also draws TRUMP while country.Government is still Biden's DEM administration, which PlayerGoverns and the Desk read.

**The fix proposed.** Have the row and slip describe what is actually seated: the record chamber whose InitialSeats equals ParliamentSeats, and the game's Government.Executive. The words then never pair the record's dates with stale state. Alternatively, run the same state check once when a save is adopted, at the end of RestoreFromSave, and widen the DECLARED premise to say so.

**The skeptic's evidence.** The failing path is real. I traced it in the staged code, but it can only affect a closed and practically empty set of saves, so I re-graded it from minor to note.

How the path happens:
1. `SeatTheRecordOnItsDate()` has one caller, `SimulationManager.cs:475`, inside `AdvanceDay`. `git grep --cached` finds no other caller.
2. Loading runs `GameController.RestoreFromSave` (`GameController.cs:893`), then `SaveGameService.RestoreInto`, then `RestoreSaveState`. None of these re-seats anything. `RestoreUiDrafts` ends with `_gameSpeed = GameSpeed.Paused;`, and `Update` returns at `if (_gameSpeed == GameSpeed.Paused) { return; }` before `_simulationManager.AdvanceDay()` (`GameController.cs:773`).
3. While the game is paused, `DrawRecordRow` reads the record by date: `:455 WorldClock.ChamberAt(country, today)`, `:457 WorldClock.TryGovernmentAt(country, today, ...)`, and the TRUMP chip at `:495-497`. The seat numbers come from the game's own state instead: `:504 foreach (... in _playerCountry.ParliamentSeats)` and `:508 slip.Add("THE HOUSE: " + WorldClock.RecordStanding(country, today) + ... seats)`.
4. The US start is `CampaignCalendar(2024-11-05).PreCampaignStart`, 34 weeks earlier (`DefaultPreCampaignWeeks` 26 + `DefaultCampaignWeeks` 8). That start seats Usa2022 (`SeatsAt`: `("REP", 222), ("DEM", 213)`) and Biden. HEAD held both for the rest of the game (US-1: "the president and the House seated at the start hold").
5. So a pre-US-2 save at 2025-06-01, while paused, shows "ELECTED 5 NOV 2024 · SEATED 3 JAN 2025" with REP 222 · DEM 213. It also shows TRUMP while `country.Government.Executive` is still Biden's, and `PlayerGoverns` reads Biden's DEM government.

Why this is only a note:
- **Strict format gate.** `SaveGameService.cs:162` is `if (version != CurrentSaveVersion)` with `CurrentSaveVersion = 38`. v38 arrived in 167b4b8 (2026-10-04 01:54). The only affected saves are US games past 2025-01-03, about 10 in-game months, cut by builds 167b4b8..b183a332, a window of roughly 39 hours.
- **None exists.** The save folder (`LocalLow/DWELOP Games/Incumbent/saves`) has no such file. The newest save there is from Sep 29, and every file is Sweden or an autosave.
- **No new ones can appear.** `AdvanceDay` moves the date at `:314` before the hook runs at `:475`, so every save cut after US-2 already matches the record for its date. The next format bump will refuse the old ones.
- **It fixes itself.** CongressOfRecordDiagnostic (f) shows that a world holding the 118th and Biden is put right on its next stepped day. The DECLARED premise says "put right on its first day", which already accepts that the state is stale until then.
- **Display only.** Nothing on the money path reads this row.

**The skeptic's corrected fix.** This is optional. If it is fixed, put the state right at load and leave the words alone. Make the hook `internal` and call it once at the end of `SaveGameService.RestoreInto`, after `sim.PlayerCountryId = save.PlayerCountryId;` and `RestoreCampaign` (a US game has no campaign to replay). `RestoreInto` is the single restore path that both the game and the diagnostics use, so the House and president would match the record before any frame draws. Then change the DECLARED premise from "put right on its first day" to "put right on load", and change diagnostic (f) to assert the state right after Adopt, before any step. The reviewer's first suggestion, finding the chamber whose `InitialSeats` equals `ParliamentSeats`, is fragile: the 119th's table is the roster's own seeds, and the slip would then name a House by whichever table happens to match. The cheapest acceptable alternative is to leave the code and add one DECLARED sentence: the row reads the record's dates, and a pre-US-2 save shows them over its old House until its first day.

### 10. SeatChamberOfRecord was inserted under SetSeatsFromElection's doc comment

- **Lens:** record - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/Simulation/ParliamentSystem.cs:81

**The scenario.** The new method sits between W-G1's existing summary (lines 81-85) and SetSeatsFromElection (line 94). SeatChamberOfRecord now carries two summaries. The first says it is "the ONLY thing that changes a chamber — an election result ... Seats not named by the result are set to zero", and SetSeatsFromElection is left with no doc. Since US-2, "the ONLY thing that changes a chamber — an election result" is also untrue in substance, because the record's seating changes a chamber too.

**The fix proposed.** Move SeatChamberOfRecord and its own summary above the W-G1 block so that block documents SetSeatsFromElection again. Amend "the ONLY thing" to mention the record's seating (US-2).

**The skeptic's evidence.** The misplacement is real and this diff introduced it. Only the second half of the finding, that "the ONLY thing" is now untrue, is arguable.

HEAD (`git show HEAD:Assets/Scripts/Simulation/ParliamentSystem.cs`): lines 81-85 are W-G1's `/// <summary> ... </summary>`, and line 86 is `public static void SetSeatsFromElection(...)` directly below it.

Staged (`git show :...`): the hunk `@@ -83,6 +83,14 @@` inserts the new member after line 85's `/// </summary>`, one member too low:
- 81-85: `/// W-G1: the ONLY thing that changes a chamber — an election result, keyed by the same abbreviations ... Seats not named by the result are set to zero rather than left at their old value ...`
- 86-87: `/// <summary>PS-6 US-2 (R-US1 (a)): seats the chamber of record ...</summary>`
- 88: `public static void SeatChamberOfRecord(Country country, ElectionVintage vintage)`
- 92-93: `}`, then a blank line
- 94: `public static void SetSeatsFromElection(...)`, with no doc

Lines 81-87 are one unbroken `///` run, so C# attaches both summaries to `SeatChamberOfRecord`, and `SetSeatsFromElection` has none.

The W-G1 text is specific to `SetSeatsFromElection`. Line 99, `seats[p.Abbrev] = wonSeats != null && wonSeats.TryGetValue(p.Abbrev, out int w) ? w : 0;`, is the "keyed by abbreviations / unnamed set to zero" it describes. `SeatChamberOfRecord` takes a vintage, not a result: line 91, `country.ParliamentSeats = PartySystems.InitialSeats(country.Id, vintage);`. I believe Roslyn's Quick Info shows only the first `<summary>`. If so, hovering `SeatChamberOfRecord` at its call site (SimulationManager.cs:4001) shows the W-G1 text and hides its own summary. That is likely, not tested.

Why stacking alone is not intent: the repo does stack summaries on purpose. `GetSeatWeightedAlignment` (staged line 267) carries three. But the same slip already exists earlier in this file. Since 07e44903e (2026-09-04), GetBillDirection's "Bill's net fiscal direction" summary (staged 105-125) sits above P5-B5's `SpendingPercentChangesOf`, and `GetBillDirection` (line 189) has no doc. That one predates this diff and is out of scope. Nothing checks doc placement: `CommentImmunityCheck` only covers comment-stripping in name-scanning checks, and Unity builds no XML docs. So there is no build, runtime, save or dump effect, and that is why the severity stays minor.

The second half is weaker than stated. US-2's own text frames the record's seating as an election's result: SimulationManager's summary says "the House changed by an election, the record's", and `SeatChamberOfRecord`'s says "a vintage's lists elected". So "only an election result changes a chamber" still holds. What no longer holds is the literal reading that `SetSeatsFromElection` is the only path that rewrites a seated chamber mid-game. The writers of `ParliamentSeats` in Assets/Scripts are:
- `UpdateSeats`, line 77 (seeds an empty chamber)
- WorldFactory.cs:1032 (world creation)
- `SetSeatsFromElection`, line 102
- the new `SeatChamberOfRecord`, line 91, run daily via SimulationManager.cs:4001

**The skeptic's corrected fix.** Move `SeatChamberOfRecord` and its own US-2 summary (staged lines 86-93) out from between the W-G1 block and `SetSeatsFromElection`. It can go above line 81 or after `SetSeatsFromElection`'s closing brace (line 103). Either way the W-G1 summary sits directly on `SetSeatsFromElection` again, as it did at HEAD. The text needs no other change for the move. Optionally, at note level: add a clause to the US-2 summary rather than rewriting W-G1's history, for example: "the second writer of a seated chamber: still an election's result, the record's, seated on its date". That keeps W-G1's "only an election result" true and names the new code path. Leave the older GetBillDirection/P5-B5 slip at lines 105-128 alone in this commit, or file it separately, because it is outside the staged diff.

### 11. After 3 Jan 2027 the standing House's majority mark sits next to the words about the 2026 election

- **Lens:** record - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/UI/GameController.ParliamentRows.cs:489

**The scenario.** When the chamber is not on record, the row draws "THE HOUSE [REP mark] THE ONE ELECTED 3 NOV 2026 NOT ON RECORD · THE LAST STANDS". The mark is the majority of the standing 119th, but it lands beside the 2026 election, whose majority the record does not hold. The row also says "NOT ON RECORD" where the slip says "IS NOT ON RECORD".

**The fix proposed.** In the !onRecord branch, draw the mark after "THE LAST STANDS" (it belongs to the standing House) or leave it out. Use the same wording in the row and the slip.

**The skeptic's evidence.** The placement half holds and is reachable. The wording half does not.

Path (staged GameController.ParliamentRows.cs):
- L483-485: `largest` is read from `_playerCountry.ParliamentSeats`, the House seated now.
- L488-491 run in this order:
  `Words("THE HOUSE", muted);`
  `if (largest != null && most > 0) { Mark(largest); }` (L489, drawn whatever `onRecord` is)
  `if (onRecord) { Words("SEATED", muted); Chip(DeskDay(chamber.Convened)); }`
  `else { Words("THE ONE ELECTED " + DeskDay(chamber.ElectionDay) + " NOT ON RECORD · THE LAST STANDS", muted); }`

When it happens:
- WorldClock (staged): the Usa2024 row now ends D(2027,1,3). The Usa2026 row runs from D(2027,1,3) and is Open. `SeatsSourced(Usa2026)` returns false.
- So from 3 Jan 2027, `ChamberAt` returns the 120th, `onRecord` is false, and `SeatedVintage` falls back to `LatestSourced` = Usa2024.
- `SeatTheRecordOnItsDate` keeps `InitialSeats(USA, Usa2024)` = `Roster(USA)` seated. REP is the larger party (PartySystem.cs:528), so `Mark("REP")` lands directly before "THE ONE ELECTED 3 NOV 2026 NOT ON RECORD".
- This is reached in ordinary play. The US start is in 2024 (the default `StartDate` branch), no US election is held (US-1), and the 120th row never closes.

Why it misleads:
- In the same row the president part reads "THE PRESIDENT [mark] TRUMP [chip]" (L494-497): the mark goes with the name after it.
- By that pattern, the House mark goes with "THE ONE ELECTED 3 NOV 2026", a House the record does not hold.
- The slip orders it the other way. `RecordStanding` gives "... IS NOT ON RECORD · THE ONE ELECTED 5 NOV 2024 STANDS", and L508 appends the seats after STANDS, so they belong to the standing House.

Why this is only a note:
- No value is wrong. The mark is the seated (119th) House's majority.
- The same label goes on to say "NOT ON RECORD · THE LAST STANDS", and the slip carries the whole sentence.
- Nothing pins or films this state. CongressOfRecordDiagnostic L135 pins only `RecordStanding`'s words, and drys783 films the US start, before 2027.

Not a defect: the row's "NOT ON RECORD" against the slip's "IS NOT ON RECORD". The row is short by design: it says "THE LAST STANDS" where the slip names the 2024 date, and the doc comment says "the slip whole". No check ties the row's words to the slip's, and `DeskDay` uses the same "d MMM yyyy" upper-case form as `RecordStanding`'s `Day`.

**The skeptic's corrected fix.** Move the mark into the branches so that it always sits next to the House it marks:

Words("THE HOUSE", muted);
if (onRecord) { if (largest != null && most > 0) { Mark(largest); } Words("SEATED", muted); Chip(DeskDay(chamber.Convened)); }
else { Words("THE ONE ELECTED " + DeskDay(chamber.ElectionDay) + " NOT ON RECORD · THE LAST STANDS", muted); if (largest != null && most > 0) { Mark(largest); } }

Leaving the mark out of the else branch is also fine. Keep the row's short wording; it does not need to match the slip's "IS NOT ON RECORD". If the not-on-record row needs checking, one dry frame of a US world stepped past 2027-01-03 would show it at 1280.

### 12. (g)'s 'the USA's alone' cannot fail: the gates are pinned nowhere and no other country's record changes in the window

- **Lens:** checks - **reviewer:** defect - **skeptic:** minor
- **Where:** Assets/Editor/CongressOfRecordDiagnostic.cs:128

**The scenario.** Widen WorldClock.RecordSeatsChamber to `id == CountryId.USA || id == CountryId.Poland`, or loop the hook over every country while keeping the staged comparisons. Every case stays green.
- The only player is the USA, so no other country's gate is ever consulted.
- Between 2024-12-28 and 2025-01-22 no other country's chamber or government of record changes (Sweden2022/Kristersson, Germany2021/Scholz minority, Poland2023/Tusk, Italy2022/Meloni, France2024/Bayrou under Macron).
- Each of those countries was seated from the same InitialSeats table and AtStart record that the hook compares against. Nothing is re-seated, so the ReferenceEquals at line 128 holds.

In a Polish game, the widened gate compares the chamber the game's own count seated with InitialSeats(Poland, SeatedVintage(Poland, today)) the next day. It then re-seats the record's table, erasing the game's election, and this check stays green. No check pins either predicate: only the hook and DrawRecordRow call them, and a StartBrief basis string names them.

**The fix proposed.** Pin both predicates over every CountryId, the way StartBriefDiagnostic.cs:84 already pins NoElectionYet: `usaOnly &= WorldClock.RecordSeatsChamber(id) == (id == CountryId.USA) && WorldClock.RecordSeatsExecutive(id) == (id == CountryId.USA)`. Then give (g) something it can catch: before the loop, put one non-US country off its record (e.g. Germany's ParliamentSeats = InitialSeats(Germany, Germany2025)) and assert that its dictionary reference survives every step. Or step one day of a Poland-player world whose Sejm is off its record.

**The skeptic's evidence.** CONFIRMED (the core claim):
- The hook only ever reads the player's gate. SimulationManager.cs: `if (!PlayerCountryId.HasValue) { return; } CountryId id = PlayerCountryId.Value; ... if (Elections.WorldClock.RecordSeatsChamber(id))`.
- Every world in CongressOfRecordDiagnostic has the USA as player or no player. Open() at line 73 sets `s.PlayerCountryId = CountryId.USA`, and Adopt() at line 210 insists the controller governs the USA. So `RecordSeatsChamber(Poland)` is never evaluated. Widening it to `id == CountryId.USA || id == CountryId.Poland` leaves every case green, including line 128.
- The finding's premises about the window hold:
  - WorldFactory.cs:1032 seats each chamber with `PartySystems.InitialSeats(country.Id, ElectionVintage.Seated)`, the table the hook compares against.
  - GovernmentRecord.AtStart sets `Executive = ofRecord.President` (null for the Swedish, German, Polish and Italian records; FrancePresident for France). So the hook's `country.Government?.Executive != g.President` never fires for those countries.
  - WorldClock's tables show no other chamber or government of record changing between 2024-12-28 and 2025-01-22. Germany2021 runs to 2025-03-25, Scholz's minority to 2025-05-06, Bayrou from 2024-12-13.
  - So a hook that looped over every country while keeping the player guard would also leave line 128 green.
- No direct pin exists. Grep finds RecordSeatsChamber and RecordSeatsExecutive only in SimulationManager.cs (lines 3995 and 4006), GameController.ParliamentRows.cs (lines 452 and 458) and StartBrief.cs:123 (a basis string). StartBriefDiagnostic.cs:84 pins only `WorldClock.NoElectionYet(id) == (id == CountryId.USA)`.
- The printed label at line 129, "the record by date is the player's country's, and the USA's alone", therefore claims a property this check cannot fail on.

OVERSTATED (why minor, not defect):
1. The code under review behaves correctly: both predicates are `id == CountryId.USA`.
2. The bar catches the finding's own example through PresidentialVetoDiagnostic case (m2). That check is in the same cheap Suite (CheckSuite.cs:219, beside the new entry at 222). At lines 786–806 it:
   - sets `sim.PlayerCountryId = CountryId.Poland`;
   - plants the fall Sejm (`pl.ParliamentSeats["Konf"] = StatutoryDeputies`, every other party 0);
   - steps `sim.AdvanceDay()` to the boundary, where PartnerTaxAct (SimulationManager.cs:2733) votes the tax act on the live chamber through PolishStatuteActs.

   With a Polish gate, the hook re-seats `InitialSeats(Poland, Poland2023)` on day one. That is the same chamber the fall=false case stands on, so the act stands and `stood == !fall` fails.
3. A widened executive gate does nothing outside the USA, because Executive == President by AtStart's construction. Pinning that predicate guards almost nothing.
4. A real gap remains for chamber gates. Sweden and Germany have player checks that plant a chamber and step AdvanceDay (ConfidenceDiagnostic.cs:227, AiMotionReachDiagnostic.cs:323, GermanFormationDiagnostic.cs:365–373), which would likely catch theirs. No check names Italy as the player. The player-from-a-variable diagnostics (e.g. NoPolicyCentury.cs:68) hold no election through a controller, so the chamber never leaves its record and the hook stays inert there. A widened Italian chamber gate would therefore erase the game's own 2022 count the next day with the whole bar green.

The second scenario is only partly right. If "loop over every country" also drops the player guard, line 149 fails: the bare world's USA would get the 119th and Trump. It stays green only if the guard is kept.

**The skeptic's corrected fix.** Add the pin the finding proposes, next to the NoElectionYet pin in StartBriefDiagnostic (or in CongressOfRecordDiagnostic's (g)). Over every CountryId, require `usaOnly &= WorldClock.RecordSeatsChamber(id) == (id == CountryId.USA) && WorldClock.RecordSeatsExecutive(id) == (id == CountryId.USA);`. Only the chamber half protects behaviour; the executive half is inert outside the USA but still pins the declared scope.

Then give line 128 power against a hook that loops over every country. Before the stepping loop, put one non-US country off its record, e.g. `world.GetCountry(CountryId.Germany).ParliamentSeats = PartySystems.InitialSeats(CountryId.Germany, ElectionVintage.Germany2025)` (off record on 2024-12-28, when Germany2021 sits). Capture that reference into `others` after the plant, so line 128 asserts it survives every step.

Optionally, prove the scope by behaviour instead of by the predicate's spelling. Open one Italy-player world, the one country no other check covers. Plant a Sejm-style off-record chamber, step one AdvanceDay, and assert the reference survives.

Otherwise, narrow line 129's label to what a US-player world can show: "the other countries' chambers and governments untouched in a US game".

### 13. TakeOffice's budget-window close and reset (DECLARED) are never asserted, though both happen inside the stepped window

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/CongressOfRecordDiagnostic.cs:117

**The scenario.** In this run the DEM player governs from the first step, so on 2024-12-29 AdvanceCountryDayTick's TryOpenBudgetProcess opens the player's arrival window, and it stays open. On 2025-01-20 TakeOffice must close that window (CloseBudgetWindowIfNotGoverning, SimulationManager.cs:4026) and reset the arrival flag (4025). The same day's tick then has the incoming REP government table its arrival budget (TableGovernmentBudget writes _pendingBudgetBillByCountry).
- Delete 4026: the opposition player's window stays open past the oath (§698's held clock), and no incoming budget is tabled.
- Delete 4025: the window closes, but the new government's budget waits for the USA's fiscal-year date of 1 October (FiscalYearData).
- Move the hook to the end of AdvanceCountryDayTick: the bill arrives a day late.

Every check passes in all three cases. Nothing reads GetPendingBudgetProcess or GetPendingBudgetBill.

**The fix proposed.** Assert on 2025-01-19 that `sim.GetPendingBudgetProcess(CountryId.USA)` holds. Assert on 2025-01-20 that `!sim.GetPendingBudgetProcess(CountryId.USA) && sim.GetPendingBudgetBill(CountryId.USA) != null`. Repeat the oath-day pair after (d)'s eve-of-oath load, since the save carries both sets.

**The skeptic's evidence.** I could not refute it. I traced each step against the staged files (`git show :path`).

THE DIAGNOSTIC ASSERTS NOTHING ABOUT THE BUDGET
- A grep of the staged CongressOfRecordDiagnostic.cs for budget|Pending|TakeOffice finds nothing.
- The oath-day block (lines 114-120) checks only these: the seats, Executive, PmParty, Kind, Outcome, FormedOn, Cabinet and Governs().
- The FromSave predicates in (d) and (e) take only a Country and check the seats, Executive and FormedOn.

THE WINDOW REALLY IS OPEN ON THE OATH DAY IN THIS RUN
- Biden's record is cabinet {DEM}, so HeadParty gives PmParty DEM. RoleOf("DEM") is PrimeMinister, so PlayerGoverns is true.
- On the first Step (2024-12-29), AdvanceCountryDayTick reaches TryOpenBudgetProcess (2416). The arrival flag is unused on a fresh manager, so lines 2456-2460 add USA to _incomingBudgetWindowUsed, _incomingBudgetWindowOpenNow and _pendingBudgetProcessByCountry.
- Only four things remove USA from the window set, and none runs between 12-30 and 01-19:
  - TableGovernmentBudget (1182) is not reached while the player governs.
  - IntroduceBudgetBill (1514) is never called; there is no controller.
  - CloseBudgetWindowIfNotGoverning (3158) is not reached; no government is installed.
  - RestoreSaveState's Clear (4669) is not reached; there is no load.
- DaysPerTurn is 365, so no turn boundary falls in the window.

WHAT HAPPENS ON 2025-01-20
- The hook runs in AdvanceDay (475), before the tick, and calls TakeOffice (4022-4028).
- Line 4025, ResetArrivalBudgetWindow, clears the arrival flag.
- Line 4026, CloseBudgetWindowIfNotGoverning, closes the window. Trump's record is {REP}, so RoleOf("DEM") is Opposition and the window is removed.
- The tick's TryOpenBudgetProcess then takes the not-governing branch (2428-2435). The flag was reset, so it counts as the arrival, and TableGovernmentBudget (1171-1185) writes the bill. AiFinanceMinistry.Apply returns early on a null report, so nothing throws.

THE THREE MUTATIONS ALL PASS EVERY CHECK
1. Drop 4026: TryOpenBudgetProcess returns at 2418 because the window is still open, so no bill is tabled.
2. Drop 4025: the flag stays used since 12-29, so the arrival test fails, and IsFiscalYearStart(USA, 01-20) is false (the US year starts 1 Oct). No bill.
3. Move the hook to the end of AdvanceCountryDayTick: TryOpenBudgetProcess runs while the window is still open, so the bill is tabled on 01-21 instead.

None of these changes which ParliamentSeats or Government object the country holds, nor Executive, FormedOn or PlayerGoverns. So (a) through (g) all pass.

The loaded saves do not catch it either. The saves carry the window set, the used flag and any pending bill (CaptureSaveState 2504-2506; RestoreSaveState 4691-4711). GameController.RestoreFromSave (893-930) does not touch them.

NO OTHER CHECK REACHES TAKEOFFICE
- Its only caller is line 4009.
- The hook does nothing in a world with no player.
- No other Editor check runs a US player world across 2025-01-20. The dates and the US player assignment appear only in this diagnostic.
- There is a precedent: ConstructiveVoteDiagnostic (e), lines 235-246, asserts exactly this close for InstallSuccessor (§698, defect 2).

SEVERITY: MINOR
The production code is correct; this is a gap in what the checks can catch.
- The reset (4025) is the DECLARED premise, and it matters in every US game: the REP government lays its arrival budget on 01-20, not on 1 Oct.
- The close (4026) matters only with the budget-window hold switched off. GameSettings._holdBudget defaults to on (PlayerPrefs default 1), and with the hold on the clock cannot step past an open window. The finding's "§698's held clock" consequence is therefore slightly overstated.
- The diagnostic's Step skips the hold, so it does exercise the close. That makes the missing assertion easy to add.

**The skeptic's corrected fix.** Take the proposed fix, with four refinements in CongressOfRecordDiagnostic.

1. On the eve of the oath, in the unbroken loop at OathDay.AddDays(-1), add:
   `budgetOpenBefore = sim.GetPendingBudgetProcess(CountryId.USA) && sim.IsIncomingGovernmentBudgetWindow(CountryId.USA);`
   This is the DEM arrival window the Step left open since 12-29. Comment it as the hold-off path, because the controller's default hold would not step past it.

2. On the oath day, at OathDay, add:
   `budgetOnOath = !sim.GetPendingBudgetProcess(CountryId.USA) && sim.GetPendingBudgetBill(CountryId.USA) is BudgetBill b && b.GovernmentBill && b.DaysRemaining == ParliamentSystem.BillDurationDays;`
   This says the bill was tabled that same day. AdvanceBudgetBillDay runs earlier in the tick than TryOpenBudgetProcess, so a bill tabled on the oath day still has its full term after the Step. That also pins the hook's place before the tick. Check both flags.
   - Deleting 4026 fails it (the window stays open).
   - Deleting 4025 fails it (no bill).
   - Moving the hook after the tick fails it (no bill until 01-21).

3. For the loaded saves, change FromSave's predicate to `Func<SimulationManager, Country, bool>` and pass the adopted manager. Then repeat the oath-day pair for the eve-of-oath load, since the save carries PendingBudgetProcess, IncomingBudgetWindowUsed and PendingBudgetBills.

4. Optionally, add a hold-on variant. The DEM player calls IntroduceBudgetBill on the first tick, so by the oath day the window is closed and that bill has been voted. Then assert that the REP government's bill is tabled on 01-20. This tests the reset (4025) apart from the close, in the setup a default game runs (hold on).

### 14. CarryOver (DECLARED) is never asserted, and (f) cannot see it because it restores the 118th's seats but not the capital

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/CongressOfRecordDiagnostic.cs:139

**The scenario.** Delete SimulationManager.cs:4002, or call CarryOver before SeatChamberOfRecord (which carries against the 118th's seats at ratio 1). Each US party's PartyCapital then keeps SeatsAtLastUpdate at the 118th's mandate (WorldFactory.cs:1075 seeds it from the chamber seated at the epoch), and OrganizationalStrength is never rescaled. That is the premise's own stated failure: 'else the game's first House election would divide by the start's seats'. Every check passes, because no line reads PartyCapital.

(f) cannot catch it either. Line 139 puts the 118th's seats back but leaves the capital already carried to the 119th, so the hook's CarryOver there is a ratio-1 no-op. A real save from before US-2 would still have its capital at the 118th.

**The fix proposed.** For DEM and REP, assert `PartyCapital.For(usa.PartyCapital, k).SeatsAtLastUpdate == house118[k]` on 2025-01-02 and `== house119[k]` on 2025-01-03. In (f), reset each record's SeatsAtLastUpdate and OrganizationalStrength to the eve's values before stepping, then assert they carry. Add the same assertion after (d)'s eve-of-House load.

**The skeptic's evidence.** The hook makes three state changes, and the diagnostic asserts two of them. In staged SimulationManager.cs:4001-4002 the hook runs `ParliamentSystem.SeatChamberOfRecord(country, seated); Elections.PartyCapital.CarryOver(country.PartyCapital, country.ParliamentSeats);`. This is the only place it moves capital, and the doc marks it DECLARED: "else the game's first House election would divide by the start's seats".

Staged CongressOfRecordDiagnostic.cs: `git grep --cached` finds no PartyCapital, SeatsAtLastUpdate or OrganizationalStrength in it. Checks (a)-(g) read only ParliamentSeats, Government, PlayerGoverns and RecordStanding.

Check (f), lines 139-141: `usa.ParliamentSeats = new Dictionary<string, int>(house118); usa.Government = GovernmentRecord.AtStart(usa, Eve, world); Step(sim, world);`. The capital is not reset. The unbroken loop already carried it to the 119th on 2025-01-03. So on (f)'s step, CarryOver (PartyCampaignCapital.cs:91-103) sees `newSeats` equal to `SeatsAtLastUpdate` (both the 119th's), gets ratio 1, and is a no-op.

A real save from before US-2 differs. WorldClock.cs:160 makes the US start `new CampaignCalendar(2024-11-05).PreCampaignStart`, which is 8+26 weeks earlier (CampaignClock.cs:72,75,114). So the world opens with the 118th. WorldFactory.cs:1075 seeds `SeatsAtLastUpdate = country.ParliamentSeats[...]`, which is the 118th. NoElectionYet(USA) means no election moves the capital afterwards. So a real pre-US-2 save past 2025-01-03 holds capital at the 118th, which (f) does not rebuild.

No other check catches a mutation:
- PartyCapitalDiagnostic only checks seeding at creation and Sweden's forced carry-overs.
- SaveLoadRoundTripDiagnostic compares run A with run B; both go through the same hook, so a deleted or reordered line leaves them equal.

Mutation trace:
- Deleting 4002 leaves SeatsAtLastUpdate at house118 and OrganizationalStrength unscaled.
- Calling CarryOver before SeatChamberOfRecord gives `CarryOver(capital, 118th)` at ratio 1, so SeatsAtLastUpdate stays at house118.
- In both cases every check (a)-(g) still passes.

Why minor, not defect: the shipped code is correct. In gameplay nothing reads OrganizationalStrength or SeatsAtLastUpdate except CarryOver itself (GameController.cs:6699 is the election path, and the US holds no House election until US-16), so a regression would stay silent and latent. This is a gap in what the checks can catch.

**The skeptic's corrected fix.** The fix as proposed is sound; tighten it in CongressOfRecordDiagnostic.

1. In Open, capture each US record's eve values (SeatsAtLastUpdate and OrganizationalStrength) by value, not by reference.

2. In the unbroken loop, for k in {DEM, REP}:
   - on 2025-01-02, assert `PartyCapital.For(usa.PartyCapital, k).SeatsAtLastUpdate == house118[k]`;
   - on 2025-01-03, assert `SeatsAtLastUpdate == house119[k]` and that OrganizationalStrength approximately equals the eve strength × house119[k] / house118[k]. Compute this in code; do not transcribe the figure (claim convention).
   - On the oath day and on `After`, assert both values are unchanged, so the president's day does not carry again.
   The SeatsAtLastUpdate == house119 assertion also kills the reorder mutant, because a ratio-1 CarryOver leaves it at the 118th.

3. In (f), before stepping, reset each record's SeatsAtLastUpdate to house118[k] and its OrganizationalStrength to the captured eve value. Then assert the same carried values, so (f) models a real pre-US-2 save.

4. In FromSave for "(d) the eve of the House's day", extend the predicate to assert the loaded game's capital carries 118th to 119th on its step. For "(d) the eve of the oath" and "(e) after both days", assert the capital is unchanged.

### 15. (e)'s 'nothing changes' compares the House by value only: s0 and g0 are captured and never read

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/CongressOfRecordDiagnostic.cs:162

**The scenario.** Line 162 captures the seat dictionary and the government before the step, but nothing reads them; every predicate compares values only.

Take a variant hook that adds a re-seat on the first day after any load (a 'put old saves right' trigger set by RestoreSaveState) on top of the staged state comparison. On 2025-01-22 it gives the House a fresh dictionary with equal values, logs a RECORD line and runs a ratio-1 CarryOver.
- (e) passes.
- (d) passes.
- (a)'s reference count cannot see it, because the unbroken world is never loaded.

Only the government side of (e) is guarded, through FormedOn == OathDay.

**The fix proposed.** Pass s0 and g0 into the predicate:
- (e): `ReferenceEquals(u.ParliamentSeats, s0) && ReferenceEquals(u.Government, g0)`.
- (d) eve of the oath: `ReferenceEquals(u.ParliamentSeats, s0)`, since the House must not be re-seated on the oath day.
- (d) eve of the House's day: `ReferenceEquals(u.Government, g0)`.

**The skeptic's evidence.** I could not refute it. The facts check out against the staged blob (git show :Assets/Editor/CongressOfRecordDiagnostic.cs).

1. Line 162 `object s0 = u.ParliamentSeats, g0 = u.Government;` is the only place s0 or g0 appears (grep -w finds only that line). Both are dead stores. They are not constants, so the compiler gives no CS0219 warning.

2. The predicates at 169-171 check the House by value only. Same() at line 59 compares the count and each key/value. On the president side they check the Executive string, plus FormedOn == OathDay for the oath run and for (e).

3. The only identity checks are at lines 103-104 (and 128 for other countries). They run on the unbroken world, which never loads a save. So a re-seat triggered by a load cannot reach (a).

4. The government side of (e) is guarded, as the finding says. TakeOffice gets GovernmentRecord.AtStart(country, CurrentDate, _world). For the USA, AtStart takes the installed path (GovernmentRecord.cs:258-260, `FormedOn = record.AsOf`), and SeatedGovernment.TryAt passes the asked `date` as AsOf (SeatedGovernment.cs:62). A re-install on 2025-01-22 would therefore carry FormedOn 2025-01-22, not OathDay, and fail (e). The House side has no such guard.

5. The mutant passes. A re-seat on the first day after any load, with equal values, would go through as follows:
   - SeatChamberOfRecord puts in a fresh InitialSeats dictionary.
   - (e) and the eve-of-oath (d) still see house119 by value, and the government is untouched.
   - The eve-of-House (d) re-seats anyway.
   So (d) and (e) both pass, and (a) never sees it.

Why minor rather than defect:
- The staged hook cannot produce this today:
  - Its SameSeats is the same strict comparison as Same().
  - A Json.NET round trip keeps every Dictionary<string,int> key, zero entries included.
  - GameController.RestoreFromSave and SimulationManager.RestoreSaveState never touch ParliamentSeats or Government.
- The mutant that escapes changes almost no state:
  - CarryOver runs with newSeats == SeatsAtLastUpdate (set on the House's day), so the ratio is exactly 1.0. Strength*1.0 is exact and Clamp leaves it unchanged (PartyCampaignCapital.cs:97-102).
  - The chamber verdict cache keys on content (ChamberVerdicts.cs:185), so a new dictionary object does not affect it.
- What escapes is a false event: a "RECORD: ... seated on 2025-01-22" log line and a new object. Even so, the docstring says (d) "steps across the day as the unbroken world did" and (e) "changes nothing", and (a) defines "each change once" by object identity. The dead s0/g0 show the author meant to apply that same identity check to the loaded runs and did not.

**The skeptic's corrected fix.** Pass s0 and g0 into the predicate, for example by making it Func<Country, object, object, bool>, and add identity checks:
- (d) eve of the House's day: `ReferenceEquals(u.Government, g0)`. Biden must not be installed again on 2025-01-03. TakeOffice's budget-window reset and close are side effects that no predicate reads.
- (d) eve of the oath: `ReferenceEquals(u.ParliamentSeats, s0)`.
- (e): `ReferenceEquals(u.ParliamentSeats, s0) && ReferenceEquals(u.Government, g0)`.

These should not fail falsely with the staged hook. The unbroken run's own identity counts in (a) show the seats object replaced only on 2025-01-03 and the government only on 2025-01-20, over steps that include 2025-01-22, and nothing on the load path replaces either.

If the author decides value equality is the intended claim, the alternative is to delete s0/g0 and reword (e) to say "nothing changes by value".

### 16. (c) checks WorldClock's answer, not a game day, and its seat conjunct repeats (a)

- **Lens:** checks - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/CongressOfRecordDiagnostic.cs:134

**The scenario.** No manager is ever stepped onto 2027-01-03. The Same(...) conjunct compares InitialSeats(USA, Usa2024) with the House the world holds on 2025-01-22, which (a) already asserts.

Seat values cannot tell the 120th from the 119th anyway. InitialSeats(USA, Usa2026) gets null from SeatsAt and falls back to the roster's SeedSeats, which is exactly SeatsAt(Usa2024) = Roster(USA). So a hook changed to seat WorldClock.ChamberAt(id, CurrentDate).Vintage (ignoring SeatsSourced) gives identical values on every date, the 120th's included, and passes every case. Only the words differ, and (c) rightly pins those whole through RecordStanding. The label's 'on 2027-01-03 the day reads the 119th's table, already seated' describes WorldClock, not the hook.

**The fix proposed.** Drop the redundant Same(...) conjunct and label (c) as a WorldClock check (RecordStanding and SeatedVintage). If a claim about a game day is wanted, the only discriminators left on 2027-01-03 are the RECORD log line and the dictionary reference.

**The skeptic's evidence.** I could not refute it. Each of its claims holds against the staged code.

1. No game day reaches 2027-01-03. `AdvanceDay` adds one day (SimulationManager.cs:314, `CurrentDate = CurrentDate.AddDays(1)`). The unbroken world's loop (diagnostic line 100, `for (... day < 60 && sim.CurrentDate < After ...)`) stops on After, 2025-01-22. (f) steps once more, to 01-23. (g) stops on 01-22, and (d)/(e) each step one day from cuts on 01-02, 01-19 and 01-21. Line 132's `day120` only goes to static `WorldClock` calls.

2. The `Same` conjunct (line 134) adds nothing to (a). Given its first conjunct (`SeatedVintage(USA, day120) == Usa2024`), it is `Same(InitialSeats(USA, Usa2024), usa.ParliamentSeats)` on the world as it stands on 2025-01-22. That is house119 from line 57 against the 01-22 House. Lines 107, 110 and 117 already check the 119th by value on 01-03, 01-19 and 01-20. Line 125 adds that the dictionary reference changed exactly once, on 01-03, over every step through 01-22. Nothing under Assets/Scripts edits `ParliamentSeats` in place (no `ParliamentSeats[..] =`, `.Add`, `.Remove` or `.Clear`), so (a) already implies the conjunct.

3. Seat values cannot tell the 120th from the 119th. `Resolve` passes a vintage other than `Seated` through unchanged (WorldClock.cs: `vintage == ElectionVintage.Seated ? ... : vintage`). `SeatsAt(Usa2026)` falls to `default: return null`, so `InitialSeats` takes `if (table == null) { foreach (PoliticalParty p in For(id)) { seats[p.Abbrev] = p.SeedSeats; } }`. `SeatsAt(Usa2024)` is `Roster(CountryId.USA)`, which is `(p.Abbrev, p.SeedSeats)` for the same `For(USA)` parties (REP 220, DEM 215). The two dictionaries match in every entry.

4. The named mutant passes every case. Suppose the hook seats `ChamberAt(id, CurrentDate).Vintage` instead of `SeatedVintage`. On every day the diagnostic steps, `ChamberAt` gives Usa2022 or Usa2024 (`Holds` is `date >= Convened && date < Until`; both vintages are sourced), so `SeatedVintage` gives the same vintage. (c) never calls the hook.

One correction to the finding's last sentence. The hook compares by value (`SameSeats`), and on 2027-01-03 the two tables are equal. So neither the real hook nor this mutant re-seats there: no RECORD line, same dictionary reference. Under today's data the named mutant behaves exactly like the real hook, and no check can catch it. A step across 2027-01-03 would catch only a hook that re-seats whenever the chamber of record changes (through the reference or the log line), or an unsourced fallback that throws.

The cost is only in the label. The header lists (c) among the things seen in a world "stepped day by day" (lines 19-24). The label's "on 2027-01-03 the day reads the 119th's table, already seated" is in fact `WorldClock.SeatedVintage`'s answer plus the 2025-01-22 House. The behaviour is correct; the `RecordStanding` words are checked in full.

**The skeptic's corrected fix.** Drop the `Same(PartySystems.InitialSeats(...), usa.ParliamentSeats)` conjunct from line 134. Relabel (c), in the header item and in the Check text, as a WorldClock check: on 2027-01-03 `SeatedVintage` gives Usa2024, and `RecordStanding` gives the not-on-record words. Do not frame it as something seen in the stepped world, and say that seat values cannot tell the 120th from the 119th while the 120th is unsourced, because `InitialSeats` falls back to the roster's figures. If a game-day claim is wanted, open a cheap US world on an eve near 2027-01-03 (`SetEpoch(2026-12-28)`) and step across that day. Assert the `ParliamentSeats` reference is unchanged and no RECORD line was logged. That catches a hook that re-seats whenever the chamber of record changes, but not the ChamberAt mutant, which matches the real hook as long as the tables match.

### 17. Hook placement is not pinned: Step always ticks the player, and the load's forks are disposed without being stepped

- **Lens:** checks - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/CongressOfRecordDiagnostic.cs:80

**The scenario.** Move SeatTheRecordOnItsDate() from SimulationManager.cs:475 to the start of AdvanceCountryDayTick. Every case passes: Step ticks the player every day, and the bare world has no player.

ShadowBaseline.AdvanceTurn, however, steps a fork with AdvanceDay alone, and a fork built on a load carries PlayerCountryId (RestoreInto sets it). Such a fork would stop seating the record, making the docstring's 'Forks reach it as the game does' false, with the bar still green. Adopt (lines 204-208) disposes the forks without stepping them.

Moving the hook after AdvanceCampaign or HoldPresidentialRound is unobservable for the USA today: TryNextPollingDay(USA) is false and TwoRoundElection.RuleOf(USA) is null, so both calls do nothing.

**The fix proposed.** Drive one case with AdvanceDay() alone, the forks' order (e.g. (f)'s step at line 141), and assert the seating. The oath-day budget assertion from the TakeOffice finding also catches a move to the end of the tick.

**The skeptic's evidence.** Every claim checks out against the staged code; I found no refutation.

1. The placement is not pinned by the diagnostic. In CongressOfRecordDiagnostic.cs, Step (lines 77-87) runs `s.AdvanceDay()` and then `if (s.PlayerCountryId.HasValue) { s.AdvanceCountryDayTick(s.PlayerCountryId.Value); }` (line 80). Every case that has a player goes through Step: the unbroken loop, (f) at line 141, and FromSave on the adopted manager. RestoreInto (SaveGameService.cs:243) and RestoreFromSave (GameController.cs:906) both set PlayerCountryId on that manager. The bare world (Open(player:false)) has no player, since SetWorld (SimulationManager.cs:2471-2476) never sets one, so neither placement acts there. In the live order, nothing runs between the end of AdvanceDay and the start of AdvanceCountryDayTick (GameController.cs:773/783). Moving `SeatTheRecordOnItsDate()` from SimulationManager.cs:475 to the start of AdvanceCountryDayTick (line 296) therefore gives the same state after every Step: the same 119th on 2025-01-03, Trump on 2025-01-20, each once, (f) put right, and (g) untouched. The (c) checks are static. The window never reaches a turn boundary, because the epoch is 2024-12-28 and DaysPerTurn is 365.

2. The calls the hook currently precedes do nothing for the USA. AdvanceCampaign returns on a null calendar because TryNextPollingDay returns false for anything but Sweden, Germany and Poland (WorldClock.cs:485). HoldPresidentialRound returns because `TwoRoundElection.RuleOf` is Poland-only (TwoRoundElection.cs:63).

3. The fork path is real and goes through AdvanceDay alone. ShadowBaseline.AdvanceTurn calls `for (int d = 0; d < DaysPerTurn; d++) { _sim.AdvanceDay(); }` (ShadowBaseline.cs:138). The fork constructor (lines 98-106) goes through CreateSaveGame with the player's id, then RestoreInto, which sets `sim.PlayerCountryId`. On a load, the controller forks the shadow itself: `_shadowBaseline = new ShadowBaseline(_simulationManager, _world, save.PlayerCountryId)` (GameController.cs:911). The US epoch is 12 Mar 2024 (StartDate's default run-up, 26+8 weeks before 2024-11-05; StartBriefDiagnostic pins "OPENS 12 MAR 2024"). So any US save cut before 2025-01-20, once loaded, forks a shadow whose next AdvanceTurn steps 365 days across both record dates. That is the path the docstring's "Forks reach it as the game does" covers. The ledger's except-worlds are forked only at turn boundaries (GameController.cs:6112; PolicyImpactLedger.cs:142), and the first boundary is about 2025-03-12, so today they never cross the 2025 dates. The load-forked shadow is the live case.

4. Adopt's finally block (lines 204-208) disposes `_impactLedger` and `_shadowBaseline` without stepping them, as the finding says.

5. No other check catches the move. PolicyImpactLedgerDiagnostic does use the USA as player, but at the default epoch (2026-10-01), where the hook is inert, and with no player on its real and from-seed worlds. NoPolicyCentury.For(USA) does step a US player world from 2024-03-12 with AdvanceDay alone (NoPolicyCentury.cs:61/68/75). Its readers assert only the ceiling and floor bounds for the USA (InfrastructureReadoutDiagnostic:56-57) or report the floor year (HealthTrendDiagnostic:57-58). Under the move that run matches HEAD, which already passed. SaveLoadRoundTripDiagnostic (363-364) uses the controller's order at the default epoch. WorldClockDiagnostic and PreStartRecordDiagnostic do not step days. ShadowBaselineDiagnostic and TrajectoryBaselineDump have no player.

Severity stays at note. The staged behaviour is not wrong; the gap is that the bar cannot detect a plausible refactor. AdvanceCountryDayTick's own docstring calls it the home of "the sim-side calls one simulated day makes for a country BEYOND AdvanceDay", and that refactor would quietly make the money-path docstring's claim false.

**The skeptic's corrected fix.** The finding's fix works: step (f) with `sim.AdvanceDay()` alone instead of Step(sim, world) at line 141, and keep its assertion. A move into AdvanceCountryDayTick then fails (f).

A more faithful pin uses the fork the load actually builds. In FromSave for the eve-of-House save, before Adopt disposes the shadow, call the controller's `_shadowBaseline.AdvanceTurn()` once. That is 365 AdvanceDay calls from 2025-01-02, crossing both dates, and costs about one shadow turn. Then assert that `shadow.World.GetCountry(USA)` holds the 119th and Trump. Call `EnergyMarket.ResetTurnState()` afterwards, as the other cases do.

Before pinning this, the asymmetry it would freeze should be decided on purpose. A new game's seeded shadow has no player and so keeps the 118th and Biden. A shadow forked on load seats the record. So a US game's "without your policies" line depends on whether the game was loaded before 2025-01-20.

### 18. WorldClockDiagnostic's day-before conjunct can never fail on its comparison, and the 120th's day is not pinned from below

- **Lens:** checks - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/WorldClockDiagnostic.cs:78

**The scenario.** From 2025-01-03 on, SeatedVintage(USA, d) returns Usa2024 whichever row holds d: the 119th's own row, or the 120th's through LatestSourced. So `SeatedVintage(id, opens120.AddDays(-1)) != Usa2024` can only fail by ChamberAt throwing on a gap.

Move both boundary dates earlier together (the 119th's Until and the 120th's Convened). The view then reads 'THE ONE ELECTED 3 NOV 2026 IS NOT ON RECORD' before 3 Jan 2027, while this block and CongressOfRecordDiagnostic (c), which probes only 2025-01-03 and 2027-01-03, stay green. `dev.Contains(...)` also pins neither date nor which table stands.

**The fix proposed.** Assert that the eve is still the 119th's own chamber: `WorldClock.SeatingDeviation(id, opens120.AddDays(-1)) == null` and `WorldClock.RecordStanding(id, opens120.AddDays(-1)) == "ELECTED 5 NOV 2024 · SEATED 3 JAN 2025"`.

**The skeptic's evidence.** The finding holds, though one part is overstated. I traced it from the staged lines.

1. Why the eve check cannot tell the two rows apart.
- `WorldClock.cs:246-250` `SeatedVintage` returns the chamber of record's vintage if it is sourced. Otherwise it returns `LatestSourced(id)`.
- `WorldClock.cs:265-270` `LatestSourced` scans the rows from the last one back.
- `PartySystem.cs:740-741` has `case Usa2026: return false`, so the scan lands on `Usa2024`.
- So on 2027-01-02 both possible holders give `Usa2024`: the 119th's own row (`:212`, from 2025-01-03 up to 2027-01-03) and the 120th's row (`:215`) if its `Convened` were earlier. The conjunct `SeatedVintage(id, opens120.AddDays(-1)) != Usa2024` (`WorldClockDiagnostic.cs:78`) cannot distinguish them.

2. The mutation stays green. Set both `D(2027,1,3)` values (the 119th's `Until` at `:212` and the 120th's `Convened` at `:215`) to `D(2026,12,1)`:
- **The eve conjunct:** `ChamberAt` returns `Usa2026`, which falls back to `Usa2024`, so it passes.
- **The checks on 2027-01-03:** unchanged. The chamber's `ElectionDay` 2026-11-03 is after `RecordDate` 2026-09-24, so `dev` contains "after the record's date" and `RecordStanding` contains "NOT ON RECORD".
- **CongressOfRecordDiagnostic (c), `:132-136`:** it compares exact strings only at `HouseDay` (2025-01-03, the 119th's row) and `day120` (2027-01-03), so both comparisons pass.
- **CongressOfRecordDiagnostic's stepping loop:** it ends at 2025-01-22, and the seats never change because `SeatedVintage` stays `Usa2024`.
- **StartBrief:** `TryNextChamberOfRecord` skips unsourced chambers (`WorldClock.cs:121`), so the brief pins only 3 January 2025.
- **Dry film drys783:** it opens on the 2024 start.
- **Result:** `DrawRecordRow` reads `ChamberAt(country, today)`, gets `onRecord` false, and prints "THE ONE ELECTED 3 NOV 2026 NOT ON RECORD · THE LAST STANDS" from 1 Dec 2026, a month early. No check notices.

3. What the finding overstates.
- "Can never fail on its comparison" is too strong. The comparison does fail if an earlier sourced row overlaps the eve. For example, if the 118th's `Until` were mistyped past 2027-01-02, `ChamberAt` would return `Usa2022` first. It also fails if `Usa2026` were sourced and moved earlier. In both cases other checks already fail (CongressOfRecordDiagnostic (a), or the first conjunct), so the eve conjunct adds no detection of its own.
- The claim that `dev.Contains` "pins neither which table stands" is weak. The table named in the deviation comes from the same `LatestSourced` that the first conjunct pins to `Usa2024`, and (c) pins the whole `RecordStanding` text on 2027-01-03, including "THE ONE ELECTED 5 NOV 2024 STANDS".

4. Why the severity is a note.
- Nothing misbehaves today: both rows carry `D(2027,1,3)`.
- A boundary moved later is caught: on 2027-01-03, `SeatingDeviation` would be null, or `ChamberAt` would throw on a gap.
- Only an earlier move of both dates together slips through, and it changes only the view's words, never the seats.

**The skeptic's corrected fix.** In `WorldClockDiagnostic.cs:78`, replace the eve conjunct `WorldClock.SeatedVintage(id, opens120.AddDays(-1)) != ElectionVintage.Usa2024` with a check on which row holds each side of the boundary:
`WorldClock.ChamberAt(id, opens120.AddDays(-1)).Vintage != ElectionVintage.Usa2024 || WorldClock.ChamberAt(id, opens120).Vintage != ElectionVintage.Usa2026`
An equivalent check is `WorldClock.SeatingDeviation(id, opens120.AddDays(-1)) != null`. Also print the eve's chamber in the FAIL line. The reviewer's exact `RecordStanding(eve) == "ELECTED 5 NOV 2024 · SEATED 3 JAN 2025"` pin also works, but it repeats the literal already in CongressOfRecordDiagnostic (c). The `dev.Contains` check can stay: which table stands is already pinned by the first conjunct and by (c).

### 19. SeatChamberOfRecord inherited SetSeatsFromElection's W-G1 doc summary

- **Lens:** checks - **reviewer:** note - **skeptic:** minor
- **Where:** Assets/Scripts/Simulation/ParliamentSystem.cs:81

**The scenario.** The new method was inserted between SetSeatsFromElection's summary and its declaration (lines 81-94). SeatChamberOfRecord now carries two <summary> blocks. The first reads 'W-G1: the ONLY thing that changes a chamber — an election result', which is wrong for this method and, since this change, untrue as a general statement. SetSeatsFromElection lost its doc, so IntelliSense shows the W-G1 text on the record's seating and nothing on the election's.

**The fix proposed.** Move the W-G1 summary (lines 81-85) back above SetSeatsFromElection (line 94), and amend 'the ONLY thing', since the record's seating now changes a chamber too.

**The skeptic's evidence.** The finding is real. I could not refute it. The staged copy of Assets/Scripts/Simulation/ParliamentSystem.cs (`git show :path`) reads:
  81  /// <summary>
  82  /// W-G1: the ONLY thing that changes a chamber — an election result, keyed by the same
  83  /// abbreviations `PartySystems` uses. Seats not named by the result are set to zero rather
  84  /// than left at their old value, because a party that won nothing holds nothing.
  85  /// </summary>
  86  /// <summary>PS-6 US-2 (R-US1 (a)): seats the chamber of record ...
  87  /// ... Never `Seated`, which resolves against the epoch.</summary>
  88  public static void SeatChamberOfRecord(Country country, ElectionVintage vintage)
  ...
  94  public static void SetSeatsFromElection(Country country, IReadOnlyDictionary<string, int> wonSeats)   <- no doc comment
At HEAD, lines 81-85 sat directly on line 86 `SetSeatsFromElection`. The hunk `@@ -83,6 +83,14 @@` shows the 8 lines inserted between `/// </summary>` and that declaration. The working tree matches the index (`git diff` on the file is empty), so this is what would be committed.

1) Wrong owner. The W-G1 text describes `SetSeatsFromElection`'s body: the `wonSeats` loop keyed by Abbrev that sets missing parties to zero (lines 96-102). `SeatChamberOfRecord` has no such loop; it assigns `PartySystems.InitialSeats(country.Id, vintage)` directly.

2) Tooltips. The comment now has two `<summary>` elements. Roslyn's doc-comment parser (used by VS Quick Info) keeps only the first one. So `SeatChamberOfRecord` shows the W-G1 text, its own US-2 summary is hidden, and `SetSeatsFromElection` shows nothing.

3) The claim is now false. Before this change, the only runtime writers of `ParliamentSeats` were seeding (WorldFactory.cs:1032, plus `UpdateSeats`, which does nothing once seats exist) and `SetSeatsFromElection` (GameController.cs:6692). Earlier reviews relied on that: Reviews/2026-10-04_s773 line 1022 says "Seats change only through SetSeatsFromElection", and s782 lines 73 and 100 say the same. The staged SimulationManager.cs:4001 (`SeatTheRecordOnItsDate`, in AdvanceDay) now writes the chamber mid-game through `SeatChamberOfRecord`. So "the ONLY thing that changes a chamber" is no longer literally true. It is defensible only in the loose sense that the record's House is also some election's result.

4) Nothing catches it:
- Assets has no csc.rsp and no -doc, so no compiler warning.
- Tools/textcheck/TextCheck.csproj does not compile ParliamentSystem.cs.
- No Editor check inspects doc-comment structure.
- ParliamentSystem.cs is not on the money list in Tools/bar_tier.ps1.

Severity: raised from note to minor. It is a real mistake in where the edit landed, and it leaves a false claim in a comment in a repo whose rules check comment claims. It has no effect on behaviour or the simulation and is trivial to fix.

**The skeptic's corrected fix.** Move the new block (staged lines 86-93: the US-2 `<summary>`, `SeatChamberOfRecord` and its trailing blank line) to just after `UpdateSeats`' closing brace (line 79). The W-G1 summary then sits directly on `SetSeatsFromElection` again. This is the same as moving lines 81-85 down to just above line 94. Then reword the W-G1 sentence so it stops saying "ONLY", without adding any figures. For example: "W-G1: what changes a chamber in play — the game's election result, keyed by the same abbreviations `PartySystems` uses (the record's chamber seated on its own date is the other writer: `SeatChamberOfRecord`, PS-6 US-2). Seats not named by the result are set to zero ...". The file is not on the money list, so the fix needs no ledger row. It is a comment-only change and needs no bar beyond the cheap compile.

## The first pass - refuted by the skeptics

- [hook] The 'who governs' check compares the save's stored Executive name with the record's name text - *The finding describes the mechanics correctly. What it lacks is a trigger: nothing in the staged code, and no path the code can reach today, makes the stored text differ from the record's text for the same president.

How the mechanics work (accurate):
- SimulationManager.cs:4006-4010 compares `country.Government?.Executive != g.President` and calls `TakeOffice(country, GovernmentRecord.AtStart(country, CurrentDate, _world))`.
- AtStart copies `Executive = ofRecord.President` (GovernmentRecord.cs:260).
- `SeatedGovernment.TryAt` makes the US rows Installed with `AsOf = date`, so FormedOn is the day of the install.
- TakeOffice (4022-4028) calls `ResetArrivalBudgetWindow` (1206), which empties `_incomingBudgetWindowUsed`. The arrival branch of TryOpenBudgetProcess (2431-2434 or 2456-2461) then fires.

Why the trigger cannot happen:
1. The two strings `"Joseph R. Biden Jr. (DEM)"` and `"Donald J. Trump (REP)"` (WorldClock.cs:319-320) were added once, in 8efdec1f (s629, format 28). `git log -S` shows no later edit to either.
2. The loader refuses any save whose version differs from `CurrentSaveVersion` (38): SaveGameService.cs:162-166 (Ruling A: "start a new game"). So every save that can load was written by a build holding these exact strings.
3. Nothing else writes Executive for the USA:
   - `ConfidenceProcedure.RulesOf(USA)` is `Rules.Unsourced` (ConfidenceProcedure.cs:34), so Install and InstallSuccessor never run for it. Those only pass `g.Executive` through anyway (3141, 4040).
   - `TryNextPollingDay` returns false for the USA (WorldClock.cs:485), so the election-night FromView at GameController.cs:6429 never runs.
   - `TwoRoundElection.RuleOf(USA)` is null, so HoldPresidentialRound returns at once.
   - WhatIfGoverning is France-only.
   - Every other writer is AtStart, reading the same constant.

So within any build, the stored Executive equals `g.President` for the same president. The misfire needs a future build to edit a US President string without bumping the save format. The finding itself says the current build cannot misfire.

The exposure is also time-limited: US-8 switches off `RecordSeatsExecutive`, which retires this comparison.

The proposed fix only narrows the risk:
- Keying on `TryGovernmentAt(id, FormedOn).From` would misfire the same way after a correction to a row's From/Until dates.
- Storing the row's From on GovernmentRecord would need the save-format step that the DECLARED premise ("no save-format step is owed") avoids.

What remains is a note: the saved Executive string now serves as the president's identity, so the record's text has to keep it.*
- [hook] RecordSeatsExecutive is unconditional, so a president the game elects would be reverted the next day - *The finding's scenario cannot happen with the staged code. The predicate's own summary already names its switch-off, and the stage plan makes that switch-off part of US-7's work, where US-7's own test would catch a revert.

1. No path installs a president the game elects. In the staged tree, the only in-game writes to a US `Government` are the start's `GovernmentRecord.AtStart` (GameController.cs:2188/2230) and the hook's own `TakeOffice` (SimulationManager.cs:4024). Both set `Executive = ofRecord.President` (GovernmentRecord.cs:260/270), so the comparison at SimulationManager.cs:4007 is equal the next day and does not fire again.
   - France's `WhatIfGoverning` is behind `GoverningModeOnly` (France only, GameController.cs:2232).
   - The constructive vote, AI motions and Speaker's rounds are all closed to the USA. `ConfidenceProcedure.RulesOf` returns `Unsourced` for the USA (ConfidenceProcedure.cs:34). The motion path refuses with "THIS COUNTRY'S CONFIDENCE RULES ARE NOT YET MODELLED" (SimulationManager.cs:2833). AI motions run only for Bundestag and Riksdag (3028-3029), and so do rounds (`RoundsApply`, 3232-3234).
   - The USA never votes: `WorldClock.TryNextPollingDay` returns false for every country except Germany, Poland and Sweden (WorldClock.cs:485). So no election, formation or `FromView` install reaches the USA.
   - No Editor diagnostic plants a non-record US president and then steps days with the USA as the player. CongressOfRecordDiagnostic.cs:140 plants `AtStart`. PreStartRecordDiagnostic's planted US governments go only into the static `EconomicVote.TookOffice` and are never stepped.

2. The ruling's condition is a build stage, and the code says so. The plan's US-2 text reads "until US-8 lands, the president of record at noon on 20 January 2025". The predicate's summary (WorldClock.cs:114-115) reads "the USA's president, until the game holds the presidential election (US-8 turns this off; the House stays seated by record after it)". A predicate that is true for the USA until US-8 is built, with that flip written next to it, is the right encoding. The finding's claim that the ruling's condition "is not in the predicate" misreads US-8, which is a build item and not a game event.

3. The plan already makes this US-7's change, and US-7's own test catches a revert. US-2's plan text says "US-7 reuses it for the oath", and US-7 says the winner "takes office at noon on 20 January ... through US-2's dated transition". So US-7 must change this hook itself. US-7's done-when is "a presidency diagnostic forces each outcome and steps to 21 Jan 2025 — each world seats its winner on the oath's date". With the forced non-Trump outcome, the revert the finding describes would be seen on 21 Jan; it could not pass silently. US-8 also brings its own save-format step, which is where any change to a predicate that reads state belongs.

The trap is real only for future code, and it is already written in the predicate's summary, written into US-7's plan, and covered by US-7's done-when. There is nothing to fix in US-2's staged diff.*
- [record] The brief's US clause drops "next" and reads as contradicting the clauses around it - *The quoted text is exactly what the code produces, but the clauses do not contradict each other, and no screen shows this sentence.

1. **The words do leave out "next".** The comment at StartBrief.cs:118 says "the record's next House and president". Lines 119-120 build "the House of record is seated on " + Long(nextHouse.Convened) and "the president of record takes office on " + Long(nextHead.From). The two lookups are WorldClock.TryNextChamberOfRecord, which returns the first sourced chamber convened after the start (Usa2024, 3 Jan 2025), and TryNextGovernmentOfRecord, which returns the first government whose From date is after the start (Trump, 20 Jan 2025). StartBriefDiagnostic.cs:74 pins the whole string exactly as the finding quotes it.

2. **There is no contradiction.**
   - Clause 2 (StartBrief.cs:89) says Biden has been "in office since 20 January 2021". The brief is dated 12 March 2024, so "takes office on 20 January 2025" cannot mean someone who has held office since 2021.
   - "Of record" does not appear in clause 2 or clause 4. It marks a different referent; it does not rename the president or House named before it.
   - The clause opens "No election is held in this game yet -", so it says what replaces the election. That is the next House and president.
   - "Is seated on 3 January 2025" against clause 4's "the election of 8 November 2022" (lines 136-137) describes one House following another. Both statements are true.

3. **The phrase is the project's own.** R-US1 (a) as ruled says "the president of record at noon on 20 Jan 2025" and means the incoming president without saying "next". The plan uses the same wording: USA_STAGE_PLAN.md:124 ("the president of record on 20 Jan 2025") and :174 ("the president of record at noon on 20 January 2025"). The new Parliament slip has the same general form: "THE RECORD'S HOUSE AND PRESIDENT ARE SEATED ON THE RECORD'S DATES".

4. **No screen draws this sentence.**
   - A grep over Assets finds StartBrief.Clauses and StartBrief.Text called only from StartBriefDiagnostic (lines 34, 43, 46, 50, 56, 86, 94).
   - CountrySelectorScreen.BuildBriefLedger draws only Head, Rows and Tagline (:789, :792, :817). The row it draws reads "NONE IN THIS GAME YET · THE RECORD SEATED ON ITS DATES".
   - Precedent: the s782 second pass refuted a finding about how a player might read a line when the text was true for the build.

**What is left** is a wording choice. Adding "next" would make the sentence match the line 118 comment and the TryNext* names, at the cost of re-pinning StartBriefDiagnostic.cs:74. Nothing is false or misleading as it stands.*
- [record] RecordStanding's "NOT ON RECORD" branch tests SeatsSourced alone, not "elected after the record's date" as its doc says - *The finding describes the code correctly, but no input that reaches the method can produce the bad words. The finding admits this itself.

1. The description is accurate. In the staged WorldClock.cs, RecordStanding chooses its branch with `PartySystems.SeatsSourced(c.Vintage)` alone (line 140). Lines 141-142 then say "THE ONE ELECTED .. IS NOT ON RECORD · .. STANDS". SeatingDeviation, by contrast, also tests `c.ElectionDay > RecordDate` (line 257). Called directly, Italy on 2022-07-21 would get "THE ONE ELECTED 4 MAR 2018 IS NOT ON RECORD · THE ONE ELECTED 25 SEP 2022 STANDS". France's XVIe would get "1 JAN 0001".

2. Every caller passes the USA (the working tree matches the index for these files):
- `GameController.ParliamentRows.cs:452`: `if (_playerCountry == null || !WorldClock.RecordSeatsChamber(PlayerCountryId)) { return; }` comes before the `:508` call, which passes `_playerCountry.Id`. `PlayerCountryId => _selectedPlayerCountryId ?? CountryId.USA` (`GameController.cs:106`) could in principle differ from `_playerCountry.Id`, but it cannot in practice. The two are set together in RestoreFromSave (`:898`/`:899`) and SelectPlayerCountry (`:2206`/`:2207`), and cleared together in ResetPlayerCountrySelection (`:2269`/`:2270`). These are the only places either is assigned.
- `SimulationManager.SeatTheRecordOnItsDate`: the `RECORD:` log that calls RecordStanding sits inside `if (Elections.WorldClock.RecordSeatsChamber(id))`.
- `CongressOfRecordDiagnostic:133` and `WorldClockDiagnostic:79` (inside `if (id == CountryId.USA)`) both pass `CountryId.USA`.
- `RecordSeatsChamber(CountryId id) => id == CountryId.USA` (line 112).

3. For the USA the two tests give the same answer on every date:
- SeatsSourced (`PartySystem.cs:733`) returns false only for Italy2018, France2022, Usa2026 and Seated. Its default is true.
- Usa2020, Usa2022 and Usa2024 are sourced and were elected before `RecordDate = D(2026, 9, 24)` (line 413).
- Usa2026, elected `D(2026, 11, 3)`, is the only unsourced US chamber, and it was elected after RecordDate.
- Dates before 2021-01-03 resolve to Usa2020 through ChamberAt's first-chamber fallback, and Usa2020 is sourced.

So `!SeatsSourced` matches `ElectionDay > RecordDate` on every input RecordStanding can receive, and the words on screen and in the log are right. A France world also never sits on a XVIe date: it starts on 2024-07-18, France2024 runs to `Open`, and the clock only moves forward.

4. The doc comment describes "the chamber the record seats", which is RecordSeatsChamber's idea (the USA alone), and it is true there. Under the claim convention it is at most incomplete for countries outside that scope, not wrong. This is a hardening point for the day a second country joins RecordSeatsChamber, not a defect in this change.*
- [record] The slip's first line and the row's doc ignore the executive gate that US-8 will turn off - *The scenario cannot happen in the staged code. It needs US-8 to switch off `RecordSeatsExecutive`, and nothing in this diff does that.

1. In the staged `Assets/Scripts/Elections/WorldClock.cs`, all three predicates are the same: `NoElectionYet(id) => id == CountryId.USA`, `RecordSeatsChamber(id) => id == CountryId.USA` and `RecordSeatsExecutive(id) => id == CountryId.USA`. So for the USA, the row's gate on line 452 implies `RecordSeatsExecutive`, and today "where the game elects neither" means the same thing as `RecordSeatsChamber`.

2. `head` (line 458) is always true in a US game:
   - `head = hasRecord && RecordSeatsExecutive(country)`.
   - `Governments(USA)` has Biden from `D(2021,1,20)` to `D(2025,1,20)`, then Trump from `D(2025,1,20)` to `Open`. `Holds` is `date >= From && date < Until`, so the two terms join with no gap.
   - A US world opens on `StartDate` = `CampaignCalendar(2024-11-05).PreCampaignStart`, which falls in 2024. So `TryGovernmentAt` always succeeds, and `President` is never null.
   - Result: the president part (lines 489-495) is always drawn, and the slip's line 502 ("THE RECORD'S HOUSE AND PRESIDENT ARE SEATED ON THE RECORD'S DATES") matches the row and the `SeatTheRecordOnItsDate` hook in every reachable state.

3. The doc (lines 446-448) and the call-site comment (line 49) are TRACKING claims already marked "until US-8 and US-16". Under CLAUDE.md's claim convention, those go stale only when the work moves.

4. US-8 already owns the rewrite. `docs/specs/USA_STAGE_PLAN.md` US-8 says: "The Parliament page's president row serves the USA", "Congress stays as of record by date (US-2), and `NationalElection.NotHeldReason` says so in place of US-1's line", and "US-1's words now name a polling day that is held". So US-8 has to rework the US president display on this same page, next to `DrawRecordRow`.

5. The rest of the diff uses the same pattern. StartBrief's new clause doesn't gate "the president of record takes office on" on `RecordSeatsExecutive` either, and its fallback "the record's House and president hold" is unconditional. The record row's slip matches how the change words the pre-US-8 state.

6. The proposed fix is incomplete. Building only "House" when `!head` would still leave "NO US ELECTION IS HELD IN THIS GAME YET" ungated, and that line would also be false after US-8.

Unrelated defect, real, outside this finding: in the staged `ParliamentSystem.cs`, the new `/// <summary>` for `SeatChamberOfRecord` was inserted under the existing W-G1 summary of `SetSeatsFromElection`.
- `SeatChamberOfRecord` now carries two summaries, one of which describes another method. In C# XML docs this is a duplicate-tag warning.
- `SetSeatsFromElection` is left with no doc.
- The W-G1 claim "the ONLY thing that changes a chamber — an election result" is now false, because `SeatChamberOfRecord` also changes a chamber.*
- [record] A government installed when an old save is put right gets the put-right day as FormedOn; this reading is not DECLARED - *The mechanics in the finding are right. The conclusion that something is missing or wrong does not hold.

1. The mechanics check out:
   - GovernmentRecord.AtStart's installed branch sets `FormedOn = record.AsOf`.
   - SeatedGovernment.TryAt builds `new Record(Standing.Installed, g.Cabinet, g.Support, ..., date)`, so AsOf is the date passed in.
   - SeatTheRecordOnItsDate calls `TakeOffice(country, GovernmentRecord.AtStart(country, CurrentDate, _world))`.
   - Diagnostic line 142 pins `usa.Government.FormedOn == sim.CurrentDate`.
   - Desk.cs:327-328 reads `since = words == "CARETAKER" ? g.CaretakerSince : g.FormedOn; ... slip.Add("SINCE " + DeskDay(since))`.
   - A v38 save from before US-2 loads unchanged (CurrentSaveVersion is still 38, and RestoreFromSave does not re-seat). So "SINCE 2 MAR 2025" next to the record row's "20 JAN 2025" can happen.

2. This is the standing convention, not something the put-right adds:
   - WorldFactory.cs:1092 seats every country with `GovernmentRecord.AtStart(country, SimulationManager.EpochDate, world)`.
   - So every US game opens with Biden at FormedOn 2024-03-12, and the Desk says "SINCE 12 MAR 2024".
   - On the same day, the new DrawRecordRow prints `Chip(DeskDay(ofRecord.From))`. Biden's row is `D(2021, 1, 20)` (WorldClock.cs:319), so it shows 20 JAN 2021.
   - SINCE is "the government's date" (§685, board 21e): the day the game seated it. The record row prints the record's in-office date. These are two different true facts, and they split from day one of every US game.

3. The doc already declares the premise that produces this. Its DECLARED sentence says "each day the seated House and the president are compared with the record's for the day and seated where they differ - so a save from a build before US-2 ... is put right on its first day ... (DECLARED)". It also says "The president takes office as every government does (TakeOffice ...)". So the put-right day is the declared day the president takes office in the game. FormedOn simply records that day. It is not a separate reading.

4. The stamped day is the true one, and the model does not depend on it:
   - The old save's world was governed by Biden, possibly the player's own DEM government, through 2025-03-01.
   - Other readers treat FormedOn as the game's install day: FinancePartner.cs:72/80/176 use it as the government's identity, and TryAiConstructiveVote acts on `g.FormedOn.Date.AddDays(1) == CurrentDate`.
   - The model's term reader, EconomicVote.TookOffice, does not read FormedOn as the term start for an `Outcome == "of record"` government. It walks to the record's row and returns `rows[at].From` (2025-01-20), so the economic vote is unaffected.
   - The finding's second fix, stamping FormedOn with the record's From, would claim the REP government formed on a day the game's own history shows Biden governing. That would be wrong.*
- [record] The stage plan still describes US-2 as a transition - *The finding is refuted: its main claim is false.

1. The staged diff does touch the plan, and it does have a Built marker. `git diff --cached -- docs/specs/USA_STAGE_PLAN.md` rewrites line 174, which is the paragraph the finding cites. The line now ends with: "**Built: `COMPLETED.md` §783** - held as state, not as a transition: an old save is put right on its first day, and no save-format step is owed." The working tree and the index match for this file. So the plan states the state form in the US-2 paragraph itself.

2. A later session is not left with a design the code no longer has.
- The US-7 and US-15 paragraphs point back to US-2. Line 227 says "through US-2's dated transition" and line 306 says "through US-2's transition". Both lead a reader to line 174, which says the build is state.
- The code names every entry point a later item needs:
  - `TakeOffice` summary: "US-7 calls it with the president the game elects."
  - `WorldClock.RecordSeatsExecutive`: "US-8 turns this off; the House stays seated by record after it."
  - `RecordSeatsChamber`: "until the game elects it (US-16); US-15 adds the Senate."
- The rest of line 174 still matches the code. `ParliamentSystem.SeatChamberOfRecord` sits next to `SetSeatsFromElection` and seats a record. The president is read through `WorldClock.TryGovernmentAt` and `GovernmentRecord.AtStart`.
- §643 asks only that the design live in the repo. It does: in the plan line and in the code summaries.

3. §783 is not missing by mistake. COMPLETED.md ends at §782 at HEAD. US-1's commit (b183a332) added its § (COMPLETED.md +22), its review file and its row edit in the same commit as the code. §783 lands with this commit, after this review.

4. The PS-6 row's "PLANNED 2026-10-04, nothing built" was already stale at HEAD, once US-1 was built. US-1's commit edited only the PS-7 row (`git show b183a332 -- POLISIM_FEATURE_LIST.md`). This diff did not introduce it, and the file is not staged.

5. What remains is wording only, with no failing path. Line 174 keeps "Built as one dated transition" next to the note that corrects it, and lines 227 and 306 still say "transition".

Aside, separate from this finding: in the index, `ParliamentSystem.cs` places `SeatChamberOfRecord` (line 88) between `SetSeatsFromElection`'s existing W-G1 `<summary>` and its signature (line 94). As a result:
- The new method has two summaries. The first describes the other method and claims "the ONLY thing that changes a chamber — an election result", which US-2 now makes untrue.
- `SetSeatsFromElection` has no doc comment.*


## What the author did about the first pass

- **1, 10, 19** (`SeatChamberOfRecord` sat under `SetSeatsFromElection`'s W-G1 summary; "only an election changes a chamber" no longer true) - the method moved above its own summary, W-G1's summary back on `SetSeatsFromElection`; the class summary, `UpdateSeats` and W-G1's summary now name both writers - the game's election, and the record's seated on its date where the game does not elect the chamber - by predicate and method, no figure copied.
- **2, 9** (after a load, the row and the state disagreed until the first day) - the state is put right AS THE SAVE LOADS: `SaveGameService.RestoreInto` calls the hook once at its end, after the campaign's replay, so the row, the Desk, the role gate and the forks read one state; the DECLARED premise says "as it loads"; case (h) loads a save holding the 118th, Biden and the 118th's capital into the game and finds the 119th, Trump and the capital carried before any step.
- **3** (`TakeOffice`'s doc claimed a close every install does) - the doc says what the method does; `CloseBudgetWindowIfNotGoverning`'s "called where a government is installed" names the controller's election-night install as the one that does not - filed as **PF-19** (`POLISIM_FEATURE_LIST.md`), a money-path change of its own.
- **4, 17** (the new game's seeded shadow has no player and keeps the start's House and president; a load's fork seats the record) - DECLARED on the hook, and left for Elias: which of the two a shadow should be is not US-2's to decide (the owed list). (f) now steps by `AdvanceDay` alone, so a hook moved into the player's tick fails it; the fork itself is not stepped (the asymmetry would be pinned, not decided).
- **5** (the impact ledger's except-worlds stepped a period past the live clock) - older than US-2; filed as **PF-18**, to be measured first.
- **6** (the row printed Biden's surname as "JR.") - one surname rule, `WorldClock.SurnameOf`: the last word that is not a generational suffix; the row, the UI's `Surname` and the history reference's head all use it, and `WorldClock.TryPresidentOfRecord` hands the row its name and surname - pinned in (i): BIDEN on the eve and the day before the oath, TRUMP from it, Nawrocki and an unnamed candidate as before.
- **7** (after 20 January 2029 the row still said "of record" for a term the record ends) - `WorldClock.TermOfRecordEnds` (a presidency's four years from the oath, the record's own DERIVED reading of the 20th Amendment) and `ExecutiveStanding`: past it the row reads "TRUMP STANDS · TERM ENDED [20 JAN 2029]" and the slip says no successor is on record, so the last of record stands (R-US1 (a)); Trump's row stays open, so `AtStart` and the hook never lose a government. Pinned in (c).
- **8** (after 3 January 2029 the words named the 2026 election as the sitting House) - the words name no later election: `RecordStanding` past the record reads "ELECTED 5 NOV 2024 · SEATED 3 JAN 2025 · ITS TERM ENDED 3 JAN 2027 · NONE ELECTED AFTER IT IS ON RECORD, SO IT STANDS", true for every later term; `SeatingDeviation`'s branch drops the day; the row reads "STANDS · TERM ENDED [3 JAN 2027]"; the 120th's row stays open and its comment says why (DECLARED). Pinned on the 120th's day and on 1 June 2029, in both diagnostics.
- **11** (the majority mark beside words about the 2026 election) - the words beside the mark are now about the House that stands.
- **12** ((g)'s "the USA's alone" could not fail) - the gates pinned over every `CountryId`; Germany's 2025 Bundestag planted off its record before the steps, so a hook over every country would move it.
- **13** (the budget window's close and reset never asserted) - (b): the DEM player's open arrival window on the eve of the oath; on the oath day none, and the REP government's budget tabled with its whole term to run - which also pins the hook before the day's tick; the eve-of-oath load asserts the same.
- **14** (the capital's carry-over never asserted) - the eve's values captured by value; the 118th's mandate the day before the House's day, carried by 119/118 (computed in code) on it, unchanged at the oath and after; (f) and (h) plant the 118th's capital; the loads assert it.
- **15** ((e)'s s0 and g0 unread) - (d) and (e) compare the House and government OBJECTS where nothing is owed.
- **16** ((c) tested `WorldClock`, not a day) - relabelled as `WorldClock`'s answer, the redundant seat conjunct dropped; (c') adds a game day: a world opened 30 December 2026 keeps the same objects across the 120th's day (seat values cannot tell the 120th from the 119th while its table is unsourced).
- **18** (`WorldClockDiagnostic`'s eve conjunct was vacuous) - it checks which ROW holds each side of the 120th's day, and that the 119th's table stands past the 121st's.

Named run `nus2c` after the fixes (`CongressOfRecordDiagnostic`, `WorldClockDiagnostic`, `StartBriefDiagnostic`, `PlayProtocolCheck`): 4 of 4 clean. An early dry film of the USA at 1280 (`drys783a`): 144 measured, 0 failed, 0 overflows, 0 escapes; its warm-up seated the 119th on 3 January 2025 and Trump on 20 January 2025 and stopped on day 1144, past the 120th's day, so the Parliament frame holds the row past the record - its label table measures the row's end at x = 710 of 1266.

## The second pass - confirmed (verbatim)

Four lenses - the first pass's fixes, the record seated as a save loads, the words and the row on screen, the checks' power - over the staged diff after the first pass's fixes; 13 findings survived their skeptics, none a defect.

### 1. PF-19 says Italy reaches the controller's election-night install, but this build holds no Italian election

- **Lens:** fixes - **reviewer:** minor - **skeptic:** minor
- **Where:** POLISIM_FEATURE_LIST.md:135

**The scenario.** PF-19 says the uncovered install is reached 'where the night forms the government itself (a country without a round - Poland, Italy)'. The controller's night only runs when the manager raises `PollingDayToday` (GameController.cs:6341/6352). That flag comes from TryPlayerPollingDay, and `WorldClock.TryNextPollingDay` returns false for every country except Germany, Poland and Sweden (WorldClock.cs:518-539). Extra elections are scheduled only from Sweden's RF 6 kap paths (SimulationManager.cs:2900, 4065). Germany and Sweden take the `RoundsApply` branch and return before line 6429. So today only Poland reaches the install at GameController.cs:6429. Someone building the owed 'check after ConstructiveVoteDiagnostic (e)' for Italy cannot stage that night. The list of country names is also a DERIVED fact copied into a doc, which the claim convention forbids.

**The fix proposed.** Name the condition, not the countries: a player country whose election the game holds and where `SimulationManager.RoundsApply` is false. Today that is only Poland; Italy joins when PS-7 gives it a polling day. Alternatively point to those two predicates.

**The skeptic's evidence.** I tried to refute the finding and could not. No path takes an Italian game to the install at GameController.cs:6429.

1. Every route into the night needs the flag. `CheckElection` (GameController.cs:6352) and `HoldElectionWithoutTheNight` (6341) return unless `_simulationManager.PollingDayToday` is set. The harness (UiScreenshotDriver.cs:3100/3102, 4132, 4806/4823) only calls them after checking `sim.PollingDayToday`. Its own error says "a country whose election calendar is not modelled has none".

2. The flag has one writer, staged SimulationManager.cs:479: `PollingDayToday = TryPlayerPollingDay(out pollingDay) && pollingDay == CurrentDate`. `TryPlayerPollingDay` (4267-4274) is `WorldClock.TryNextPollingDay`, or `_extraElectionDate` when one is set.

3. `WorldClock.TryNextPollingDay` (staged WorldClock.cs:515-539) returns true only for Germany, Poland and Sweden; `if (id != CountryId.Sweden) { return false; }`. `PollingDayDiagnostic` asserts this for Italy: `!WorldClock.TryNextPollingDay(other, ...) && PollingDayBasis(other) == null` for Italy, USA and France.

4. An extra election can't be set for Italy either. `_extraElectionDate` is written only by `ScheduleExtraElection`, from two callers:
   - `OrderExtraElection` needs `NoConfidenceOn` within its week (2917). The player's motion refuses an Unsourced country (2833: "THIS COUNTRY'S CONFIDENCE RULES ARE NOT YET MODELLED"). `TryAiMotion` returns for anything but the Riksdag or Bundestag (3029). `ConfidenceProcedure.RulesOf` returns `Unsourced` for Italy (ConfidenceProcedure.cs:34).
   - `BreakOff` belongs to the Speaker's round, which only runs where `RoundsApply` is true.

5. Germany and Sweden do reach the night, but they take `if (_simulationManager.RoundsApply(PlayerCountryId))` at 6409 and return before 6429. So today only Poland reaches the install.

6. The repo already says the same. The feature list's own PS-7 row (line 85) says Italy's start "still names a polling day the game does not hold". CLAUDE.md's World Clock rule says "a country whose calendar is not modelled holds none". PF-19 (new in this diff, line 135) contradicts both.

7. The list "Poland, Italy" also breaks the claim convention. It records which countries meet a code predicate, which makes it a DERIVED claim, and POLISIM_FEATURE_LIST.md is not exempt ("Nobody transcribes, anywhere"; only COMPLETED.md is exempt). It is also incomplete as a list of countries without a round: the USA and France are Unsourced too. The bar won't catch it, because DocumentClaimCheck only validates backticked `Type.Member` references.

Why minor: it is a wrong line in an OPEN tracking row and changes no runtime behaviour. Its only effect is to send whoever builds the owed check after `ConstructiveVoteDiagnostic` (e) toward an Italian night that cannot be staged; Poland is the one that can.

**The skeptic's corrected fix.** In POLISIM_FEATURE_LIST.md PF-19, replace "(a country without a round - Poland, Italy)" with "(the player's country on a day `SimulationManager.PollingDayToday` is raised and `SimulationManager.RoundsApply` is false)". Name no countries.

The reviewer's proposed wording, "Today that is only Poland; Italy joins when PS-7 gives it a polling day", is itself a transcribed DERIVED claim that goes stale when PS-7 lands, so don't use it.

Both backticked members exist, so DocumentClaimCheck's reference check passes. If the owed check needs a staging hint, word it as a pointer, for example: "stage it where `WorldClock.TryNextPollingDay` offers a day and `ConfidenceProcedure.RulesOf` is Unsourced".

### 2. The 'nothing moves when a US-2 save loads' claim is not tested: (d)/(e) read s0/g0 after the load's own hook has run

- **Lens:** fixes - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/CongressOfRecordDiagnostic.cs:234

**The scenario.** SaveGameService.cs:245-247 now claims that 'a save from a build with US-2 already holds it, and nothing moves' at load. FromSave takes `s0 = u.ParliamentSeats, g0 = u.Government` only after Adopt has run RestoreFromSave, and so after RestoreInto has already called SeatTheRecordOnItsDate. A wrong install at load would therefore go unseen in (d) on the House's eve. Example: an Executive mismatch makes the hook re-install Biden on 2025-01-02, resetting the arrival window and setting FormedOn to the load day. g0 is then that new object, the step keeps it, and `ReferenceEquals(u.Government, g0)` still passes. (e) catches the government case only indirectly, through FormedOn == OathDay. Adopt already asserts `ReferenceEquals(sim.World, save.World)`, so the objects from before the load are available.

**The fix proposed.** Before Adopt, read `save.World.GetCountry(CountryId.USA).ParliamentSeats` and `.Government`. In (d) and (e), assert those same objects are still held after the load, and keep the existing post-step comparisons.

**The skeptic's evidence.** The finding holds. Every step is traced below against the staged code.

1. **Where s0/g0 are read.**
   - CongressOfRecordDiagnostic.cs:229 runs `Adopt(save, hosts, ...)` first. Only then does :234 read `object s0 = u.ParliamentSeats, g0 = u.Government;`.
   - The call chain from Adopt to the hook:
     - Adopt (:285) runs `restore.Invoke(controller, new object[] { save })`.
     - That reaches GameController.cs:896, `SaveGameService.RestoreInto(_simulationManager, save)`.
     - RestoreInto ends at SaveGameService.cs:247 with `sim.SeatTheRecordOnItsDate();`. The comment at :245-246 says "a save from a build with US-2 already holds it, and nothing moves".
   - So s0 and g0 are always the objects as they stand after the load's hook has run.

2. **Nothing else in the load path replaces those two objects.**
   - RestoreSaveState calls SetWorld(world), which assigns `_world` and seeds history. It clears pending structures but writes neither ParliamentSeats nor Government.
   - RestoreCampaign returns early: the US world has no campaign.
   - In RestoreFromSave, the ShadowBaseline and PolicyImpactLedger work on a serialized copy.
   - Adopt already asserts `ReferenceEquals(sim.World, save.World)` (:294).
   - So the objects read from `save.World` before Adopt are the ones held afterwards, unless the hook replaced them. The proposed fix is feasible.

3. **What each case can and cannot see.**
   - **(d), eve of the House's day (:243).** It checks `Same(seats, house119) && ReferenceEquals(u.Government, g0) && Biden && Carried(u)`. There is no FormedOn check and no window check.
     - A Biden re-install at load goes through TakeOffice (SimulationManager:4026-4029): new record, FormedOn set to the load day, arrival window reset. g0 is then the new record, the step keeps it, and the case passes.
   - **(d), eve of the oath (:246-247).** A load-time Biden re-install is replaced by the oath's install on the step, so it is invisible.
   - **(e) (:250).** It catches a Trump re-install at load only through `FormedOn == OathDay`.
   - **Seats, every case.** A re-seat at load (SimulationManager:4003) that leaves DEM and REP unchanged is invisible:
     - CarryOver at ratio 1.0 leaves the capital untouched (PartyCampaignCapital.cs:100-102).
     - s0 is the object as it stands after the load.

4. **No barred check pins the claim.** SaveLoadRoundTripDiagnostic would catch value changes at load: it re-serializes the restored save, the hook now runs inside RestoreInto, and the USA is the player in two of its scenarios. But it is in neither CheckSuite's Suite nor its Simulation table; the only mention is a comment at CheckSuite.cs:26.

5. **Why this is only a note.**
   - The claim is true today:
     - The load hook makes the same comparisons the day loop made on the save's own date.
     - Both inputs are plain serialized fields: `Executive` (GovernmentRecord.cs:46) and the ParliamentSeats dictionary (Country.cs:893).
     - (a)'s "each change once" check proves the day loop changes nothing on those days.
   - Moves that change values are mostly caught indirectly:
     - A wrong president at load is reverted the next day, which fails the object or FormedOn checks.
     - A wrong table at load is re-seated the next day, which fails `ReferenceEquals(u.ParliamentSeats, s0)` in (d)-oath and (e).
   - What is left unseen is a move at load that replaces an object without a lasting effect: a re-seat to equal DEM/REP seats, or a same-president re-install on the eve of the House's day (which resets the arrival window).
   - It is a gap in the new diagnostic's coverage, not a failing path in the game.

**The skeptic's corrected fix.** In FromSave, read the objects from `save.World` before calling Adopt:
- `Country pre = save.World.GetCountry(CountryId.USA);`
- `object seatsBefore = pre.ParliamentSeats, govBefore = pre.Government;`

Add a flag, `bool loadMovesNothing`: true for (d) and (e), false for (h), whose load is meant to move the House and the president.

When the flag is set, assert straight after Adopt and before any step:
- `ReferenceEquals(u.ParliamentSeats, seatsBefore) && ReferenceEquals(u.Government, govBefore)`
- Optionally also that the arrival-window state is unchanged: `GetPendingBudgetProcess` and `IsIncomingGovernmentBudgetWindow` as they were when the save was cut. Label it e.g. "(d)/(e) loaded with nothing moved: the same House and government objects as the save held".

Keep the existing post-step comparisons against s0/g0 as they are.

If the test is not added, word the SaveGameService.cs:245-246 comment as what the hook compares (it seats only where the state differs from the record's for the save's date), not as a measured "nothing moves". Alternatively, register SaveLoadRoundTripDiagnostic on a bar: its re-serialize equality covers value changes at load for a US player world.

### 3. ChamberSeatedOn's doc is too broad: for Italy 2018 and France 2022 it returns a later chamber

- **Lens:** fixes - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/Elections/WorldClock.cs:134

**The scenario.** The new public member's doc says it returns 'the chamber of record itself, or, past the record, the one that stands'. The code is `ChamberOfVintage(id, SeatedVintage(id, date))`, and SeatedVintage falls back to `LatestSourced` for any unsourced chamber. For Italy on 2022-07-21 (Italy2018, unsourced, E-47) it returns the Italy2022 chamber, which convened 2022-10-13. For France on 2024-03-01 it returns France2024. Neither result is the chamber of record, and neither is 'past the record'. Today's callers (RecordStanding and DrawRecordRow) are USA-only, so nothing displays this yet.

**The fix proposed.** Limit the doc to `RecordSeatsChamber` countries, or name the E-47 case: the latest sourced chamber, which may be a later one.

**The skeptic's evidence.** I could not refute it. The doc is wrong for an input the public method accepts. Nothing displays the error today.

The doc (staged WorldClock.cs:134) says: "the chamber of record whose table is seated on a date - the chamber of record itself, or, past the record, the one that stands." The code (:135) is `ChamberOfVintage(id, SeatedVintage(id, date))`. SeatedVintage (:299-303) returns `PartySystems.SeatsSourced(ofRecord) ? ofRecord : LatestSourced(id)`. LatestSourced (:319-324) walks Chambers(id) from the newest end and returns the first sourced vintage. SeatsSourced (staged PartySystem.cs:733-746) is false for Italy2018, France2022 and Usa2026, and true by default for every other vintage.

Italy on 2022-07-21 (Italy's own StartDate, :208):
- ChamberAt gives Italy2018. Its row (:255) runs 2018-03-23 to 2022-10-13, and that table is unsourced.
- LatestSourced gives Italy2022.
- So ChamberSeatedOn returns the row at :256: ElectionDay 2022-09-25, Convened 2022-10-13. That is a chamber not yet elected on the date asked.

France on 2024-03-01: ChamberAt gives France2022 (:275, unsourced), so the method returns France2024 (:276), convened 2024-07-18.

Neither case fits either of the doc's two cases:
- The result is not "the chamber of record itself", because ChamberAt gives Italy2018 / France2022.
- It is not "past the record". This diff's own meaning of that phrase is `c.ElectionDay > RecordDate` (:310, and RecordStanding's doc at :138). RecordDate is 2026-09-24 (:467). Italy2018 was elected 2018-03-04, and France2022's ElectionDay is MinValue.

The gloss implies the returned chamber had convened by the date. That fails here. SeatedVintage's own doc (:298, "else the latest sourced one") gets this right; ChamberSeatedOn's narrows it to two cases.

There is no runtime effect. ChamberSeatedOn is new in this diff (absent at HEAD), and all three of its callers are USA-only:
- CongressOfRecordDiagnostic.cs:183 passes CountryId.USA.
- RecordStanding (:143) throws unless RecordSeatsChamber, and :146 is its only call.
- DrawRecordRow (GameController.ParliamentRows.cs:455) returns early unless `WorldClock.RecordSeatsChamber(PlayerCountryId)`. Its :494 call sits in the `!onRecord` branch, which for the USA is only Usa2026, giving Usa2024 with Until 2027-01-03. That is correct.

For the USA, which has no E-47 chamber, the doc is exact. So this is doc accuracy on a public, country-generic method only. The repo's claim convention counts it because the claim is false when measured, but the severity is note, not minor.

**The skeptic's corrected fix.** Change only the doc. A throw is not needed: "the chamber whose table is seated" is a meaningful answer for every country. Mirror SeatedVintage's doc, and point to the E-47 test (`PartySystems.SeatsSourced`) rather than listing the countries, per the claim convention. For example: `/// <summary>PS-6 US-2: the chamber of record whose table is seated on a date (<see cref="SeatedVintage"/>) - the chamber of record's own where its table is sourced, else the latest sourced chamber: past the record (a chamber elected after the record's date), the one that stands; for a chamber whose per-list table is not sourced (E-47, `PartySystems.SeatsSourced`), a LATER chamber, which may not have convened by the date (<see cref="SeatingDeviation"/> says so).</summary>`. The other option is to scope it the way RecordStanding is scoped: say "for a country whose chamber the record seats (<see cref="RecordSeatsChamber"/>)" and leave the rest of the doc as written.

### 4. A load-time install keeps the outgoing government's pending budget bill

- **Lens:** load - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/Simulation/SimulationManager.cs:4026

**The scenario.** Take a REP player's save cut between 1 and 21 Oct 2025 by a build before US-2. That build kept Biden, so on 1 Oct his AI ministry tabled the fiscal-year budget as a government bill (TryOpenBudgetProcess, lines 2431-2434), and the bill is saved in PendingBudgetBills. On load the hook installs Trump. TakeOffice (lines 4026-4031) resets the arrival flag and closes any open window, but leaves _pendingBudgetBillByCountry untouched. TryOpenBudgetProcess returns while that bill stands (line 2422). So the House decides Biden's budget under Trump's government with the REP player governing (line 1541 -> ResolveGovernmentBudget; applied if passed), and only after that does the REP player's arrival window open, in late October. An unbroken US-2 game gave the REP player the arrival window on 20 Jan 2025 and the fiscal-year window on 1 Oct 2025. The DEM mirror case behaves the same way: the DEM player's own pending bill is decided under Trump, then the AI's arrival bill follows. Live US-2 play cannot reach this from the AI side, because Biden's last fiscal-year bill resolves in Oct 2024. Only the load-time correction exposes it, and (h) checks no budget state. Install and InstallSuccessor behave the same way, so this is the manager's install rule applied to a state the correction makes reachable.

**The fix proposed.** Either mark as DECLARED on the hook (and on the SaveGameService comment) that a corrected old save keeps the outgoing government's pending bill, or have TakeOffice withdraw a pending GovernmentBill tabled under a different executive and log it. In both cases add a stepped variant of (h) built from a save cut while Biden's 1 Oct 2025 bill is pending.

**The skeptic's evidence.** I traced every step of the scenario in the staged tree and it happens as described. It is not a defect: the outcome follows a premise the project already recorded and kept, and every other install path does the same.

The path (staged SimulationManager.cs unless noted):
- **Old saves reach the hook.** `SaveGameService.cs:162` rejects any save whose version is not `CurrentSaveVersion` (38). The diff does not bump it, so only v38 saves load: those from builds 167b4b8a (§770, 2026-10-04) up to HEAD b183a332.
- **Those builds kept Biden.** US-1 says so: "the start's president and House". With a REP player, `PlayerGoverns` (2631-2643) is false.
- **The AI bill is tabled on 1 Oct.** On that date the AI path at 2428-2434 calls `TableGovernmentBudget`. The US fiscal year starts on (10, 1) per `FiscalYearData`, and the bill gets `BillDurationDays` = 21. A save cut from 1 to 21 Oct holds it in `PendingBudgetBills` (`CaptureSaveState`, 2506).
- **The hook installs Trump on load.** `RestoreInto` calls the hook last, after `RestoreSaveState`. `GovernmentRecord.AtStart` sets `Executive = ofRecord.President`, so the hook's `!=` test fires and `TakeOffice` runs.
- **`TakeOffice` (4026-4032) leaves the bill alone.** It only does `country.Government = formed; ResetArrivalBudgetWindow; CloseBudgetWindowIfNotGoverning`.
- **The bill blocks the window, then resolves.** Line 2422 (`if (_pendingBudgetBillByCountry.ContainsKey(countryId)) { return; }`) holds the REP player's arrival window. Line 1541 then calls `ResolveGovernmentBudget`; the USA's procedure is `Unsourced`, so the bill is voted alone through `ApplyBillResult`. The arrival window opens on the day it resolves, 22 Oct.
- **(h) does not cover it.** Diagnostic lines 252-254 check seats, `Executive`, `FormedOn` and capital only, no budget state.

Why it is only a note:
1. **It is recorded, kept behaviour.** COMPLETED.md §630 lists under "stated and kept": "a pending bill outlives the role that introduced it (the countdown is the chamber's)". §632 ruled that "the window is a GOVERNMENT'S, reset where the election's verdict replaces the record" and that "no window opens while a bill stands". The finding's sequence is exactly these rules together.
2. **Every install does the same.** `Install` (4049-4051) and `InstallSuccessor` (3144-3146) leave the pending bill too. The DEM mirror case is §630's premise word for word, and live US-2 play can reach it. With `GameSettings.HoldOnBudgetWindow` off (`GameController.cs:1387`), a DEM player who introduces a bill in early January has it decided under Trump.
3. **"Biden's budget" overstates it.** The bill is `AiFinanceMinistry`'s US rule applied to the country's own figures ("nothing here reads the player's country"). `ApplyUsRule` cuts only when `UsTriggered` reads the debt ratios. Trump's AI ministry would table the same bill.
4. **No written claim becomes false.** The SaveGameService comment says "what the game does not elect" is put right, which is true. The arrival reset is already marked DECLARED.
5. **The effect is small and narrow.** Only v38 saves from about a day and a half of builds are affected. The worst case is one AI-rule budget event, plus the REP player's window opening up to 21 days late, on a save whose post-oath history cannot be replayed anyway.

**The skeptic's corrected fix.** No behaviour change. Option (b), having `TakeOffice` withdraw a pending government bill, would contradict §630's kept premise ("a pending bill outlives the role that introduced it"). It would also make `TakeOffice` the only install path that differs from `Install` and `InstallSuccessor`. That is Elias's call if it is wanted at all. The most that is worth adding is one pointer clause on the hook's DECLARED sentence about old saves, for example: "a bill pending in the old save is decided as every install leaves one (§630: a pending bill outlives the role that introduced it; §632: no window opens while it stands)". It should point to the record, not copy figures into the comment. A stepped variant of (h) with a pending bill is optional, not owed: the behaviour is the manager's general install rule, not something US-2 adds.

### 5. No screen names the record's coming dates, so the oath flips the player's role unannounced

- **Lens:** words - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/UI/GameController.ParliamentRows.cs:506

**The scenario.** A US game with a DEM player on 19 Jan 2025. The row reads 'OF RECORD, BY DATE · THE HOUSE [REP] SEATED [3 JAN 2025] · THE PRESIDENT [DEM] BIDEN [20 JAN 2021]'. Its slip says the record's House and president are seated 'ON THE RECORD'S DATES' but gives no date. The start card's ledger said only 'THE RECORD SEATED ON ITS DATES' (StartBrief.cs:211). The next day the hook installs Trump: the DEM player no longer governs and its open arrival window is closed (CongressOfRecordDiagnostic (b) pins exactly this). The House change on 3 Jan 2025 is just as silent. The only text naming either date is StartBrief's clause (StartBrief.cs:119-123), and no screen draws it: CountrySelectorScreen.cs:789/792 reads only StartBrief.Head and Rows, and Text/Clauses are read only by StartBriefDiagnostic. TryNextChamberOfRecord and TryNextGovernmentOfRecord have no caller outside StartBrief. TakeOffice only logs.

**The fix proposed.** While a change of record is ahead, put the next of record on the slip (and, if Design agrees, on the row), read from WorldClock.TryNextChamberOfRecord / TryNextGovernmentOfRecord as the brief already does. For example: 'NEXT OF RECORD: THE HOUSE SEATED 3 JAN 2025 · THE PRESIDENT FROM 20 JAN 2025'. Alternatively, carry the brief's dated clause onto the start card's ledger.

**The skeptic's evidence.** I tried to refute this and could not. Every claim matches the staged code (the working tree equals the index for all code files).

1. A new US game crosses both dates. WorldClock.cs:91 fixes LatestElectionDay(USA) = 2024-11-05, so StartDate is the 12 Mar 2024 run-up. The new Usa2026 row does not move it.

2. The record row names only past days before each change. GameController.ParliamentRows.cs:493 chips `chamber.Convened`, and :500 chips `ofRecord.From` while `today < termEnds`. On 19 Jan 2025 the row reads "SEATED [3 JAN 2025] · BIDEN [20 JAN 2021]", and on 2 Jan 2025 it reads "SEATED [3 JAN 2023]". The slip has no forward date either:
   - :506 is the undated "ON THE RECORD'S DATES" line.
   - :512 uses RecordStanding, which on record returns only "ELECTED x · SEATED y" (WorldClock.cs:145) and never the 118th's Until.
   - :513 uses ExecutiveStanding, which before the term ends returns only "IN OFFICE FROM " + g.From (WorldClock.cs:175-176).

3. The start card has no date. CountrySelectorScreen.cs:789/792/817 draws only StartBrief.Head, Rows and Tagline. The row it draws (StartBrief.cs:211) reads "NONE IN THIS GAME YET · THE RECORD SEATED ON ITS DATES". The mode line (StartPoints.cs:126), the folder line (WorldClock.cs:221) and NotHeldReason (NationalElection.cs:415-417) are undated too.

4. The dated words never reach a screen. They exist only in StartBrief.cs:119-123 (Clauses, and Text through it). Grep finds Clauses and Text read only in Assets/Editor/StartBriefDiagnostic.cs. TryNextChamberOfRecord and TryNextGovernmentOfRecord have no caller except StartBrief.cs:119-120.

5. No neighbouring row draws for the USA. DrawReferenceRow is gated to Bundestag rules or Poland (:372) and needs an election the game held. DrawPresidentRow needs TwoRoundElection.RuleOf (:527). A grep of the UI for TERM ENDS, UNTIL, OATH, CONVENES and NEXT finds nothing for the USA.

6. The moment itself is unannounced. TakeOffice (SimulationManager.cs, staged) sets Government, calls ResetArrivalBudgetWindow and CloseBudgetWindowIfNotGoverning, and then only calls Debug.Log. The game's other installer, Install, at least writes round.Log, which a slip shows. The only logMessageReceived listener is Testing/UiScreenshotDriver.cs. No UI code watches for a change of government or role, and GovernmentRecord.AtStart writes no log.

7. The flip is real. CongressOfRecordDiagnostic:173 pins it: demBefore && !demAfter && repAfter. Afterwards the Desk role chip (GameController.Desk.cs:276-288) turns from GOVERNING to IN OPPOSITION. The new state is visible after the fact, but nothing warned of it.

One part of the scenario is overstated. Per the diagnostic's own comment (:151), with the controller's default budget-window hold the DEM player's arrival window would stop the clock, so "its open arrival window is closed at the oath" only happens with the hold off or with a window opened later. The role flip does not depend on that.

Why minor and not a defect: R-US1 (a) requires "said on screen" only for the 119th standing past the record, which the row does. Nothing on screen is false and no state is wrong. The plan gives the PRESIDENT-ELECT and oath-date words to US-7/US-8. Still, US-2 adds a change of the player's role partway through a run, and it wrote the dated warning only into a clause no player sees, so the gap is real and cheap to close.

**The skeptic's corrected fix.** Take the finding's fix, with these refinements.

(1) In DrawRecordRow's slip (GameController.ParliamentRows.cs, after :513), add a line while a change of record is still ahead:
- If `WorldClock.TryNextChamberOfRecord(country, today, out var nextHouse)`, add "NEXT OF RECORD: THE HOUSE ELECTED {ViewDay(nextHouse.ElectionDay)} IS SEATED {DeskDay(nextHouse.Convened)}".
- If `WorldClock.RecordSeatsExecutive(country) && WorldClock.TryNextGovernmentOfRecord(country, today, out var nextHead) && WorldClock.TryPresidentOfRecord(country, nextHead.From, out string nextName, out _)`, add "NEXT OF RECORD: {nextName} TAKES OFFICE {DeskDay(nextHead.From)}".

Both helpers already return false past the record. The House helper filters on SeatsSourced, so it never names the 120th, and Trump is the last government of record. The lines therefore drop out by themselves after 3 Jan and 20 Jan 2025, and they need no special case for the standing words. Build every date at runtime from WorldClock and write no transcribed dates into comments (the claim convention).

(2) Whether the next day also goes on the row as a chip is Design's call, because of the row's width at 1280. The row is IMGUI, so the declared USA@1280x720 dry film can show it.

(3) Optionally put the same dates into the start card's drawn ledger row (StartBrief.Rows :211), for example "NONE IN THIS GAME YET · THE HOUSE OF RECORD 3 JAN 2025 · THE PRESIDENT 20 JAN 2025", built from the same helpers. That is Canvas text, so under the repo's Canvas film rule it needs a REAL 1280 film of the start card, and StartBriefDiagnostic must be re-pinned.

(4) Add a check to CongressOfRecordDiagnostic or StartBriefDiagnostic that the next-of-record words name 3 Jan 2025 and 20 Jan 2025 on the eve and disappear after each date.

Marking the moment of the oath itself (a slip line or the role chip's words) can wait for US-7's PRESIDENT/PRESIDENT-ELECT words, which D-US composes.

### 6. The slip never names the president's party; on the row it is only a mark

- **Lens:** words - **reviewer:** note - **skeptic:** minor
- **Where:** Assets/Scripts/UI/GameController.ParliamentRows.cs:513

**The scenario.** The row shows the president's party only as a 16 px mark (line 498). The slip reads 'THE PRESIDENT: DONALD J. TRUMP · IN OFFICE FROM 20 JAN 2025' with no party, although the same slip names the House's parties by short name (line 511, 'REP 220 · DEM 215'). The §770 sibling slip names the backing party in words ('... · PIS · IN OFFICE SINCE ...', lines 588-589). A player who cannot read the US marks finds the president's party nowhere in words. The verb also differs from §770 ('FROM' against 'SINCE').

**The fix proposed.** Add the short name: 'THE PRESIDENT: ' + name + ' · ' + PartySystems.ShortName(country, ofRecord.Cabinet[0]).ToUpperInvariant() + ' · ' + ExecutiveStanding(...). Optionally use one verb with §770; ExecutiveStanding's words are pinned in CongressOfRecordDiagnostic (c), so changing it means re-pinning.

**The skeptic's evidence.** Traced in the staged tree (git show :path); the finding's line numbers match.
(1) The row, GameController.ParliamentRows.cs:498: `if (ofRecord.Cabinet != null && ofRecord.Cabinet.Length > 0) { Mark(ofRecord.Cabinet[0]); }`. `head` is true for every US date from 2021-01-20: RecordSeatsExecutive(USA) is true, and the governments of record are "Joseph R. Biden Jr. (DEM), president" (Cabinet {"DEM"}, 2021-01-20 to 2025-01-20) and "Donald J. Trump (REP), president" (Cabinet {"REP"}, from 2025-01-20, Open). The US opens in the run-up to 2024-11-05, so the row draws from day one.
(2) The mark has no letters. DrawPartyMarkSlot (GameController.PoliticalRows.cs 105-110) draws the texture when one loads; the lettered fallback `PoliSimWidgets.MeasuredLabel(square, PartySystems.ShortName(country, key), ...)` (line 115) runs only when the texture is missing. Assets/Resources/Art/UI/Emblems/mark_party_us_rep.png and mark_party_us_dem.png are installed, and their SVG sources are a shield with a bar and a torch, with no letters.
(3) The slip, line 513: `if (head) { slip.Add("THE PRESIDENT: " + president.ToUpperInvariant() + " · " + WorldClock.ExecutiveStanding(country, today)); }`.
- WorldClock.TryPresidentOfRecord strips the tag on purpose. Its doc says "the name without the party's tag", and the code is `int tag = tagged.IndexOf(" (", ...); name = tag > 0 ? tagged.Substring(0, tag) : tagged;`.
- ExecutiveStanding returns only `"IN OFFICE FROM " + ViewDay(g.From)`, or the same plus the term-ended words after 2029.
- So the slip reads "THE PRESIDENT: DONALD J. TRUMP · IN OFFICE FROM 20 JAN 2025", and "JOSEPH R. BIDEN JR. · IN OFFICE FROM 20 JAN 2021" at the start, with no party.
- Line 511 names the House's parties by ShortName, but nothing ties REP or DEM to the president's mark, and the slip has no [[TERM]] second level.
(4) Nothing else on the screen carries the president's party.
- DrawParliamentPoliticalBlocks always calls DrawSlips(book, ...) and has no dense-view (DeskProvenance) branch.
- Only PeopleSlipReachabilityCheck and StatsSlipReachabilityCheck exist; no check covers the Parliament tab.
- DrawReferenceRow returns unless the country is under Bundestag rules or is Poland.
- The Desk's role slip says "THE GOVERNMENT IS THE AI'S" with no party.
(5) The page's own pattern names every mark's party on its slip:
- support agreement: `name + " SUPPORT AGREEMENT"`;
- Speaker's round: `Name(round.Asked) + " ASKED FIRST"` and `"THE CABINET " + string.Join("+", ...ConvertAll(Name))`;
- reference row: GovernmentLine = `... + after.Value.Head.ToUpperInvariant()`, i.e. "FRIEDRICH MERZ (CDU)";
- the §770 sibling at 588-589: `president.Name.ToUpperInvariant() + (president.BackingParty != null ? " · " + PartySystems.ShortName(country, president.BackingParty).ToUpperInvariant() : string.Empty) + " · IN OFFICE SINCE " + ...`.
(6) docs/specs/UI_V33_SPEC.md (ruled by Elias 2026-09-29), §1: "marks ... at rest, words ... on demand; every word ... reachable within two tooltip levels". §4.1: "every slip is its own legend entry".
Why minor and not a defect: nothing shown is false, no check fails, and with two parties a player can still guess by elimination. Why above a note: it breaks a ruled UI rule and the uniform pattern of every sibling row, it shows in every US game from day one, and the fix is one line. The "FROM" against "SINCE" verb difference is cosmetic, note level only.

**The skeptic's corrected fix.** Name the party on the slip line, using the same guard the row's mark uses at line 498, in the §770 order (name · party · in office). In DrawRecordRow, replace line 513 with:

if (head)
{
    string party = ofRecord.Cabinet != null && ofRecord.Cabinet.Length > 0 ? " · " + PartySystems.ShortName(country, ofRecord.Cabinet[0]).ToUpperInvariant() : string.Empty;
    slip.Add("THE PRESIDENT: " + president.ToUpperInvariant() + party + " · " + WorldClock.ExecutiveStanding(country, today));
}

PartySystems.ShortName(USA, "REP"/"DEM") returns the abbreviation (no shortName override), so the line reads "THE PRESIDENT: DONALD J. TRUMP · REP · IN OFFICE FROM 20 JAN 2025".

No pin changes. CongressOfRecordDiagnostic (c) pins only ExecutiveStanding's strings (diagnostic lines 186-188) and TryPresidentOfRecord's name and surname (line 192); it does not pin this GameController slip line. Keep the party in GameController rather than in TryPresidentOfRecord or ExecutiveStanding, so the pinned words stay as they are.

Leave the FROM/SINCE verb alone, or align it as a separate note: changing ExecutiveStanding means re-pinning (c) at lines 187-188.

The longer past-term line is fine: the slip box wraps whole lines itself (the §770 comment), so do not pre-wrap it.

### 7. The brief's clause reads as if Biden takes office on 20 Jan 2025

- **Lens:** words - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/Elections/StartBrief.cs:120

**The scenario.** At the US start (12 Mar 2024) the brief's paragraph is: 'The president is Joseph R. Biden Jr. (DEM), in office since 20 January 2021; the House majority is REP, ... No election is held in this game yet - the House of record is seated on 3 January 2025 and the president of record takes office on 20 January 2025, as the record dates them; ...'. The president of record has just been named as Biden, so the sentence says he takes office on 20 Jan 2025. Likewise, the 118th already seated is 'the House of record'. The clause is diagnostic-only today (§782: no screen draws it before US-8) and is pinned whole at StartBriefDiagnostic.cs:74.

**The fix proposed.** Write 'the next House of record is seated on ...' and 'the next president of record takes office on ...', then re-pin StartBriefDiagnostic.cs:74.

**The skeptic's evidence.** The finding holds, at the severity it was given. I could not refute it.

What the paragraph says (G:/UNITY/Projects/PoliSim-captures/logs/nus2c.log:860, the nus2c run's own brief): "United States, 12 March 2024. The president is Joseph R. Biden Jr. (DEM), in office since 20 January 2021; the House majority is REP, with 222 of 435 seats. No election is held in this game yet - the House of record is seated on 3 January 2025 and the president of record takes office on 20 January 2025, as the record dates them; no Senate is modelled. 2 parties sit in the House (435 seats, the election of 8 November 2022)."

How the code builds it:
- StartBrief.cs:89 builds "The president is " + government.President + ", in office since " + Long(government.From). Biden is the only president the paragraph names.
- StartBrief.cs:119-120 build "the House of record is seated on " + Long(nextHouse.Convened) and "the president of record takes office on " + Long(nextHead.From). The code reads nextHead (President = "Donald J. Trump (REP)", WorldClock.cs:374) and nextHouse (ElectionDay 2024-11-05), but never prints either.
- The change's own words call Biden the president of record on the start date. WorldClock.TryPresidentOfRecord(USA, 12 Mar 2024) goes through TryGovernmentAt and returns Biden, and DrawRecordRow prints BIDEN under "OF RECORD, BY DATE" until 20 Jan 2025.
- So "the president of record" with no "next" most naturally points back to Biden. A re-elected president takes the oath again on 20 January, so the reading "Biden takes office again on 20 Jan 2025" makes sense and nothing in the sentence rules it out.
- The House half is less likely to mislead. A House is never seated twice, and the last clause dates the sitting House by "the election of 8 November 2022".

One premise in the finding is slightly off. The text says "The president is ...", not "the president of record is ...". The conclusion still holds through the code's own vocabulary and the plain reading.

Why it is only a note:
- The sentence is true. On 20 Jan 2025 the record's president, Trump, is installed.
- No screen draws the paragraph. StartBrief.Clauses and Text are read only by StartBriefDiagnostic (lines 34/43/46/56/86/94). CountrySelectorScreen.cs:789/792/817 draws only Head, Rows and Tagline. COMPLETED.md §782 says "no screen draws them before US-8".
- USA_STAGE_PLAN.md:243 has US-8 re-pin StartBriefDiagnostic and rewrite US-1's words.
- The pin is confirmed whole at StartBriefDiagnostic.cs:74.

**The skeptic's corrected fix.** The finding's "the next House of record ... the next president of record ..." is an acceptable minimal fix. It breaks the link back to the Biden already named, but it still does not say who takes office: a reader who doesn't know the 2024 result could still read "the next president of record" as Biden's second term.

The record already holds both successors, so the clause can name them (read from the record, not typed in). At StartBrief.cs:119-120:
- house = "the House elected on " + Long(nextHouse.ElectionDay) + " is seated on " + Long(nextHouse.Convened)
- head = nextHead.President + " takes office on " + Long(nextHead.From)

Guard ElectionDay == DateTime.MinValue the way line 137 does. The paragraph then reads: "No election is held in this game yet - the House elected on 5 November 2024 is seated on 3 January 2025 and Donald J. Trump (REP) takes office on 20 January 2025, as the record dates them; no Senate is modelled."

Re-pin StartBriefDiagnostic.cs:74 whole. It is still one clause, so the four-clause check at :35 holds, and the basis still names WorldClock. If the owner does not want a March 2024 brief to name the 2025 president, use the finding's "next" form.

Either way this is only a change to a pinned string: no screen draws it, and no film is owed.

### 8. The slip's first line always names the president, but the row shows him only while the record seats the executive

- **Lens:** words - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/UI/GameController.ParliamentRows.cs:506

**The scenario.** The row draws the president only where WorldClock.RecordSeatsExecutive holds (line 461). That predicate's doc says US-8 turns it off while the House stays seated by record (WorldClock.cs:114-116), and the row's own doc says 'until US-8 and US-16'. In that intended state the row shows only the House, but line 506 still says 'NO US ELECTION IS HELD IN THIS GAME YET: THE RECORD'S HOUSE AND PRESIDENT ARE SEATED ON THE RECORD'S DATES', which is false twice. No state of the current build reaches it.

**The fix proposed.** Build line 1 from the gates (WorldClock.NoElectionYet and `head`): name the president only where `head` holds, and say 'no US election' only while NoElectionYet does.

**The skeptic's evidence.** I could not refute any of the finding's claims. It is a latent inconsistency inside the one function, and no state of the current build reaches it, so it stays a note.

1. The slip's first line is unconditional, but the rest of the row reads the gate. In the staged GameController.ParliamentRows.cs:
   - :454 draws the row on the chamber's gate alone: `if (_playerCountry == null || !WorldClock.RecordSeatsChamber(PlayerCountryId)) { return; }`
   - :461 sets `bool head = WorldClock.RecordSeatsExecutive(country) && WorldClock.TryGovernmentAt(...) && WorldClock.TryPresidentOfRecord(...)`
   - :495 `if (head)` draws THE PRESIDENT on the row.
   - :513 `if (head) { slip.Add("THE PRESIDENT: " + ...); }` gates the slip's own president line.
   - :506 adds "NO US ELECTION IS HELD IN THIS GAME YET: THE RECORD'S HOUSE AND PRESIDENT ARE SEATED ON THE RECORD'S DATES; PAST THE RECORD, THE LAST OF RECORD STANDS" with no gate.

   So within the same slip, the third line honours `head` and the first line ignores it.

2. The state that exposes it is the one the change itself documents.
   - WorldClock.cs:115-116 says "(US-8 turns this off; the House stays seated by record after it)" over `public static bool RecordSeatsExecutive(CountryId id) => id == CountryId.USA;`. RecordSeatsChamber (:112) stays on until US-16.
   - SimulationManager.cs:4010 seats the president under RecordSeatsExecutive alone.
   - After that one-line flip, the row is still drawn (:454) and shows only the House (:495 is skipped). Line 506 would then be false twice: a US election is held, and the president is no longer the record's.

3. Why no current state reaches it:
   - NoElectionYet (:108), RecordSeatsChamber and RecordSeatsExecutive are all `id == CountryId.USA`.
   - The only USA start is the 2024 run-up (StartPoints.cs:83; opens 12 MAR 2024 per StartBriefDiagnostic.cs:75).
   - The USA's two Presidency rows cover every date from then on: Biden from 2021-01-20 to 2025-01-20 (WorldClock.cs:373) and Trump from 2025-01-20, Open (:374). So TryGovernmentAt and TryPresidentOfRecord always succeed, and `head` is always true wherever the row is drawn.

4. Nothing would catch it at US-8.
   - `git grep --cached "NO US ELECTION IS HELD"` finds only :506.
   - CongressOfRecordDiagnostic.cs:108 (g) pins the gates' values, so a flip turns (g) red, but it does not check these words.

5. Sibling wording has the same shape. The row's doc (:446) and the call-site comment (:49) both say "where the game elects neither the chamber nor the president/head of the executive", while the row is drawn on the chamber's gate alone.

StartBrief.cs:115-122 shows the right pattern: its "No election is held in this game yet" clause sits under `if (WorldClock.NoElectionYet(id))`. The finding's fix follows it.

**The skeptic's corrected fix.** The finding's fix is correct. Build line 506 from the gates in a form like this:

`slip.Add((WorldClock.NoElectionYet(country) ? "NO US ELECTION IS HELD IN THIS GAME YET: " : "THE GAME DOES NOT ELECT THE HOUSE YET: ") + (head ? "THE RECORD'S HOUSE AND PRESIDENT ARE SEATED ON THE RECORD'S DATES" : "THE RECORD'S HOUSE IS SEATED ON THE RECORD'S DATES") + "; PAST THE RECORD, THE LAST OF RECORD STANDS");`

This matches the gated president lines at :495 and :513. Also reword the row's doc (:446) and the call-site comment (:49) from "where the game elects neither the chamber nor the president" to say the row is drawn where the game does not elect the chamber (RecordSeatsChamber), and the president is shown with it while RecordSeatsExecutive holds. Today's on-screen words do not change, because `head` and NoElectionYet are both true for the USA.

### 9. Check (g) never steps a world whose player is another country across the record's dates, so a gated loop over every country passes

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/CongressOfRecordDiagnostic.cs:208

**The scenario.** The mutation: change SeatTheRecordOnItsDate (SimulationManager.cs:3993) to keep `if (!PlayerCountryId.HasValue) return;` and then run `foreach (Country country in _world.Countries)` with the same RecordSeatsChamber/RecordSeatsExecutive gates. Every case still passes:
- The planted 2025 Bundestag (line 114) sits in a US-player world, and the gates exclude Germany.
- The bare world (line 209) has no player, so the hook returns.
- The gate pin (lines 107-109) is untouched.
- The trajectory dump sets no player.
In a German game (opens 2024-11-06), and likewise a Polish, Italian or French one, the world opens before 2025-01-03 with the AI USA seeded on the 118th and Biden. Under the mutation, that AI USA would be seated with the 119th on 3 Jan 2025 and Trump would take office via TakeOffice on 20 Jan 2025. That breaks the plan's 'The player's country only; R-US5 owns the rest'. A scan of the other day-stepping checks that read US seats or government found none that crosses those dates with a non-US player (PollingDayDiagnostic's Polish world stops at 2023-10-20).

**The fix proposed.** Add a (g) case: Open a world at Eve with PlayerCountryId = Germany and a German party seated, step it to After in the controller's order, and assert the USA's ParliamentSeats and Government are still the eve's objects (ReferenceEquals: the 118th and Biden).

**The skeptic's evidence.** The finding holds, and I could not refute it from the staged code.

THE HOOK'S SCOPE (staged SimulationManager.cs 3993-4013): `if (!PlayerCountryId.HasValue) { return; } CountryId id = PlayerCountryId.Value; Country country = _world?.GetCountry(id);`. Both gates are then asked of `id`. The plan says the same (USA_STAGE_PLAN.md:174): "The player's country only; R-US5 owns the rest."

WHAT CongressOfRecordDiagnostic COVERS: the `Open` helper (76-88) can only make a USA player (`if (player) { s.PlayerCountryId = CountryId.USA; ... }`) or no player at all. The cases, one by one:
- 107-109: pins the gates. Untouched by the mutation.
- 112/114 and 176-177: Germany's 2025 Bundestag is planted in a world whose player is the USA. `RecordSeatsChamber(Germany)` is false, so it only catches a loop with no gate.
- 209-212: the bare world has no player, so the mutation's kept `return` fires.
- 217 (c'), and the saves at 242-254 ((d), (e), (h)): all have the USA as player.

No world in the diagnostic has a player other than the USA. Under the mutation (keep the no-player `return`, then a gated `foreach` over `_world.Countries`), a USA-player world behaves the same, because the gates admit the USA alone, and a no-player world returns. Every case passes.

NOTHING ELSE CATCHES IT:
- The only checks that step a non-US player across 3 Jan or 20 Jan 2025 from a start before them are GermanCampaignDiagnostic.cs 175-195 and 216-226. They step Germany from its 2024-11-06 start to 2025-02-24/25, but assert only campaign facts: run-up and campaign dates, 16 Länder, the CDU's reach, the polling day, the SSW share bounds.
- No check that sets a non-US player and opens before those dates reads `GetCountry(CountryId.USA)` (grep across Assets/Editor).
- The AI USA's government and seats feed no number. In AdvanceTurn, `AiFinanceMinistry.Apply` reads neither. `FinancePartner.Holder` returns null for a one-party presidency. The manager's seat readers (`MajorityOf`, formation) serve the player's own country.
- Every UI read of `.Government` goes through `_playerCountry`.
- Sweden's start (the 2026 run-up) and `DefaultEpoch` (2026-10-01) both fall after the dates. Those worlds are seeded on the 119th and Trump, so the mutated hook finds nothing to change.
- TrajectorySentinelCheck: "no dump seats a player".
- The mutated path would not throw for an AI USA. `PlayerGoverns` returns false at `PlayerCountryId.Value != country.Id`, `CloseBudgetWindowIfNotGoverning` removes keys that are absent, and `PartyCapital` defaults to an empty list.

THE CONSEQUENCE IN A NON-US GAME: a German game opens 2024-11-06, a French one 2024-07-18, and Polish and Italian ones earlier still. Each is seeded with the AI USA on the 118th and Biden. Under the mutation, the AI USA takes the 119th on 3 Jan 2025 and Trump via TakeOffice on 20 Jan 2025. That breaks the ruled scope (R-US5).

WHY MINOR: the product code is correct. The gap is in the check, whose own comment at line 208 ("the hook is the player's country's") claims a scope it proves on only one axis. Today the mutated behaviour would be latent: no screen and no number reads the AI USA's government or seats. It would show only in non-US saves and in "RECORD: USA" log lines.

**The skeptic's corrected fix.** Add a world to (g) whose player is another country. Use `Open(player: false, Eve)`, then set `PlayerCountryId = CountryId.Germany` and a German party, for example `PlayerPartyAbbrev = "CDU"`. Capture the USA's `ParliamentSeats` and `Government` objects. Step with `Step(...)` to `After`, crossing both days. Then assert:
- `ReferenceEquals` holds for both objects, and `Same(usa.ParliamentSeats, house118)` holds.
- The executive still starts with "Joseph R. Biden".

Afterwards call `EnergyMarket.ResetTurnState()`, as the other cases do. Print it as "(g) a world whose player is another country (Germany) keeps the AI USA's eve House and president across both days - the hook is the player's country's".

A cheaper variant opens one world the day before each date and steps it one day. Under the mutation, the new dictionary seated on 2025-01-03 makes the `ReferenceEquals` assert fail.

### 10. The REP half of check (b) only reads the role; a REP player's arrival budget at the oath is never asserted

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/CongressOfRecordDiagnostic.cs:173

**The scenario.** The mutation: TakeOffice (SimulationManager.cs:4027) resets the arrival flag only when the player leaves office, e.g. `if (!PlayerGoverns(country)) ResetArrivalBudgetWindow(country.Id);`.
- The DEM run still passes. The DEM player leaves office, so the reset runs and the REP AI's bill is tabled on the oath day. (d) is the same DEM world and passes too.
- For a REP player the outcome is wrong. On day one, the Biden AI government's arrival bill takes the flag (TryOpenBudgetProcess's AI branch, line 2433). That 21-day bill resolves on 2025-01-19. On 20 Jan 2025 the governing branch (line 2456) finds the flag used, and 20 January is not the USA's fiscal-year start. So the REP player gets no arrival budget until 1 October, and nothing in the diagnostic fails.
The REP half of (b) is `Governs("REP")` alone (line 150).

**The fix proposed.** Run the eve world once with PlayerPartyAbbrev = "REP" and step it to the oath day. Assert `GetPendingBudgetProcess(USA) && IsIncomingGovernmentBudgetWindow(USA)` there, with no pending bill.

**The skeptic's evidence.** The REP half of (b) checks the role only. CongressOfRecordDiagnostic.cs:86: every player world is opened with `u.PlayerPartyAbbrev = "DEM"`. Line 133's Governs() switches the party only long enough to read PlayerGoverns, then switches it back. Lines 150 and 160 read the role, and line 173 checks it. The budget is asserted only by the DEM run (152, 162, 174) and by (d)'s eve-of-oath save (245-248), which comes from the same DEM world. The other cases never touch the budget:
- (f) and (h) assert the seats, the government and the capital.
- (g)'s bare world has no player, so the hook returns at SimulationManager.cs:3995.
- (c') opens on Trump, so TakeOffice never runs.

**The mutation, traced with a REP player.** The mutation is `if (!PlayerGoverns(country)) ResetArrivalBudgetWindow(country.Id);` in TakeOffice (SimulationManager.cs:4026-4032).
- On 2024-12-29 REP does not govern, so the AI branch runs (2428-2435). It sets the flag (2433) and TableGovernmentBudget sets DaysRemaining to 21 (1180; ParliamentSystem.BillDurationDays = 21).
- AdvanceBudgetBillDay counts down before TryOpenBudgetProcess (tick lines 299 and 308; decrement and resolve at 1534-1541). The bill resolves on 2025-01-19 and ResolveGovernmentBudget does not table another.
- On 2025-01-20 the hook (475) calls TakeOffice. Trump is installed, and PlayerGoverns(REP) is true (the check's own repAfter), so the reset is skipped. CloseBudgetWindowIfNotGoverning returns at 3158.
- In the tick, the governing branch (2456) finds the flag still set. IsFiscalYearStart is false (FiscalYearData.cs:18 gives the USA (10,1)). No window opens until 2025-10-01.

**The DEM run under the same mutation.** PlayerGoverns(DEM) is false after the oath, so the reset still runs and the run behaves exactly as unmutated. windowBeforeOath, budgetOnOath and (d) all pass.

**No other check covers it.**
- The only Editor code that seats a US player party is line 86 above, and it seats DEM.
- BudgetWindowDiagnostic (96-97) and SaveLoadRoundTripDiagnostic (83, 200) run at the default epoch, which is after the oath, so TakeOffice never runs in either. BudgetWindowDiagnostic also seats no party, so PlayerGoverns is always true there (2639).
- The film's state searches (UiScreenshotDriver.cs:1830, 3496) wait for any budget process, so they would find the 1 October window and not fail.

**Why it matters.** REP is the harness's and the fallback's US party. GameController.cs:2215-2226 seats the largest party of the chamber seated at the start (12 Mar 2024), and PartySystem.cs:777 gives the 118th REP 222 and DEM 213. So the unasserted half is the default US path, the one that shows the "YOUR INCOMING GOVERNMENT'S FIRST BUDGET" hold (GameController.cs:5929). TakeOffice's doc also says US-7 will reuse it.

**Why minor and not a defect.** The unmutated TakeOffice resets the flag unconditionally (4029). The DEM half already catches the likely regressions:
- the reset removed;
- the window closed before the install;
- the hook moved after the tick.

Only a reset made conditional on the player's role gets through.

**The skeptic's corrected fix.** The finding's fix is right, but it needs one more assertion or it can pass without testing anything. Add a REP world after the unbroken run:

1. Open it with `Open(player: true, Eve)`, then set `usa.PlayerPartyAbbrev = "REP"`.
2. **On the first stepped day**, assert that the AI government's arrival bill is tabled: `GetPendingBudgetBill(USA)` is a GovernmentBill with DaysRemaining == BillDurationDays, and no process is open. This proves the flag was spent before the oath. Without it, a regression that never set the flag would let the oath-day window open with no reset at all, and the check would pass.
3. **On the eve of the oath**, assert no bill and no open process. The 21-day bill tabled 2024-12-29 resolves on 2025-01-19, which leaves exactly one day of slack. If BillDurationDays grows, this assertion fails and names the broken premise, instead of the failure looking like a missing reset.
4. **On the oath day**, assert `GetPendingBudgetProcess(USA) && IsIncomingGovernmentBudgetWindow(USA) && GetPendingBudgetBill(USA) == null`.
5. Call `EnergyMarket.ResetTurnState()` afterwards, as the bare and (c') worlds do.

### 11. WorldClockDiagnostic's USA block checks the after-the-record deviation only by substring, and nothing pins those words whole, despite the comment

- **Lens:** checks - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/WorldClockDiagnostic.cs:84

**The scenario.** The comment at line 75 says the deviation and the standing words name no later election, 'the words whole are CongressOfRecordDiagnostic's (c)'. That holds only for RecordStanding:
- CongressOfRecordDiagnostic never calls SeatingDeviation.
- This block asserts only `Contains("after the record's date")` on 2027-01-03 and 2029-06-01.
Two mutations of the branch (WorldClock.cs:310-314) would pass both checks: putting the 120th's polling day back (`elected {c.ElectionDay:yyyy-MM-dd}, after the record's date`, which reintroduces the first pass's item 8), or naming the unsourced vintage as the standing table (`the {c.Vintage} table`, i.e. Usa2026).
The block also never reads ChamberAt(USA, 2029-06-01). It infers which row holds the late date from the substring, so it does not check 'which row holds each side of 2029-06-01' as answer (18) states.
Today every caller passes a start date (WorldClock.cs:674, StartBrief.cs:135, GameController.cs:2197), so the branch shows on no screen; the overclaim is in the check.

**The fix proposed.** Pin SeatingDeviation(USA, 2027-01-03) and (USA, 2029-06-01) whole, building the expected text from WorldClock.RecordDate and the Usa2024 vintage name. At minimum, assert that it does not contain "2026-11-03" and does contain "Usa2024 table". Assert ChamberAt(USA, late).Vintage == Usa2026. Narrow the comment to RecordStanding.

**The skeptic's evidence.** The finding is accurate on every point I could check. The weakness only bites if someone edits the words, and those words show on no screen today.

1. The comment points to a pin that does not exist. WorldClockDiagnostic.cs:74-75 (staged) says "the deviation and the standing words say so, naming no later election (the words whole are CongressOfRecordDiagnostic's (c))". CongressOfRecordDiagnostic.cs never calls SeatingDeviation. `git grep --cached SeatingDeviation` finds callers only at WorldClockDiagnostic :51/:80/:82, StartBrief.cs:135, WorldClock.cs:674 and GameController.cs:2197. Case (c) pins only RecordStanding whole (:180 `const string Stands = "ELECTED 5 NOV 2024 · ... SO IT STANDS"`, :184) and ExecutiveStanding whole (:187-188). Nothing in Assets/, Tools/ or docs pins "after the record's date" or "stands in its place" except WorldClockDiagnostic.cs:84. Under CLAUDE.md's claim convention, this is a false REFERENCED claim.

2. The only check is a substring. :84 is `|| !dev.Contains("after the record's date") || !devLate.Contains("after the record's date")`. Tracing it:
   - On 2027-01-03, `Holds` is `date >= Convened && date < Until` (WorldClock.cs:45). So the Usa2024 row (Until 2027-01-03) does not hold that day and the Usa2026 row (Convened 2027-01-03, Until Open) does. Usa2026 also holds 2029-06-01.
   - `SeatsSourced(Usa2026)` returns false (PartySystem.cs:740-741).
   - The election day 2026-11-03 is after RecordDate 2026-09-24 (WorldClock.cs:467), so both dates take the branch at WorldClock.cs:310-314.
   - Any text there that contains the substring passes. That includes `elected {c.ElectionDay:yyyy-MM-dd}, after the record's date`, which on 2029-06-01 names the 120th's 2026-11-03 polling day for a House the 121st has replaced (first-pass item 8 again). It also includes `the {c.Vintage} table`, which wrongly names Usa2026 as standing. The separate `SeatedVintage == Usa2024` conjuncts do not read the words.
   - So the fix to item 8 in SeatingDeviation ("names no later election day") is held by no check. WorldClock.cs:267's "the words (`RecordStanding`, `SeatingDeviation`) name no later election" is true of today's code but proved only for RecordStanding.

3. The 2029-06-01 sub-point is accurate but changes nothing. The block calls ChamberAt only for 2027-01-02 and 2027-01-03 (:79). For the late date it reads SeatedVintage, the deviation substring and RecordStanding equality. So answer (18)'s "which ROW holds each side of ... 2029-06-01" overstates what the block does. Still, a hypothetical extra unsourced after-record row would yield the same correct seating and words, so no failure hides behind this part.

4. Impact is limited (the finding concedes this):
   - StartBrief.cs:135 passes `opens = start.Opens` (:74).
   - WorldClock.cs:674 passes `StartDate(id)`.
   - GameController.cs:2197 passes `start = WorldClock.StartDate(countryId)` (:2166).
   - The new DrawRecordRow (GameController.ParliamentRows.cs) shows RecordStanding and ExecutiveStanding, not SeatingDeviation, so even the US film's frame past the record cannot show the unpinned branch.

That makes this a check-power gap plus a false comment pointer, not a runtime defect.

**The skeptic's corrected fix.** Make the USA block (WorldClockDiagnostic.cs:80-85) hold the property the comment claims, without copying the format string:
- For both `dev` and `devLate`, the only yyyy-MM-dd dates in the text are the queried date and `WorldClock.RecordDate`. A regex over `\d{4}-\d{2}-\d{2}` whose matches are a subset of {date, RecordDate} holds "names no later election" directly and fails on the 2026-11-03 mutation.
- Both contain `"the " + ElectionVintage.Usa2024 + " table"`. This fails on the `{c.Vintage}` mutation.
- Optionally pin each whole against a string built from `WorldClock.RecordDate` and `ElectionVintage.Usa2024`.
- If answer (18)'s wording stays, add `WorldClock.ChamberAt(id, late).Vintage == ElectionVintage.Usa2026`. Otherwise reword (18) to "each side of 2027-01-03, and the table and words on 2029-06-01".

Then fix the comment at :74-75 so the "(the words whole are CongressOfRecordDiagnostic's (c))" pointer names the standing words only. Alternatively, move a whole pin of SeatingDeviation(USA, Day120/Late) into CongressOfRecordDiagnostic (c) so the pointer becomes true.

### 12. Checks (d) and (e) take s0/g0 after the load, so 'nothing moves as a US-2 save loads' is never asserted by identity

- **Lens:** checks - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/CongressOfRecordDiagnostic.cs:234

**The scenario.** s0/g0 are read from the adopted world after RestoreInto has already run SeatTheRecordOnItsDate.
- Suppose the load path re-seats a House with equal values, e.g. SameSeats is bypassed only on RestoreInto's call. The new dictionary exists before s0 is taken, and CarryOver on unchanged seats is the identity, so (d) and (e) pass.
- A re-install of the government at load is caught only by (e)'s FormedOn == OathDay, not by the identity check.
So SaveGameService.cs:245 ('a save from a build with US-2 already holds it, and nothing moves') is read, not proved.

**The fix proposed.** Before Adopt, take `save.World.GetCountry(CountryId.USA).ParliamentSeats` and `.Government`. These objects survive the load: Adopt asserts ReferenceEquals(sim.World, save.World), and RestoreSaveState only calls SetWorld. For (d) and (e), assert ReferenceEquals against them after the load as well as after the step.

**The skeptic's evidence.** The gap is real but narrow, so this stays a note.

**The order is as the finding says.** In CongressOfRecordDiagnostic.cs, FromSave runs `LoadFromFile` (228), then `Adopt` (229). `Adopt` reaches `GameController.RestoreFromSave`:896, then `SaveGameService.RestoreInto`, whose last line (247) is `sim.SeatTheRecordOnItsDate()`. Only after that does line 234 read `object s0 = u.ParliamentSeats, g0 = u.Government;`. Every identity check in (d) and (e) (243, 246, 250) therefore compares against objects that already exist after the load hook ran. The load itself is never checked by identity.

**The finding's mutant does survive.** Take a load-only bypass of `SameSeats`. It writes a new 119th dictionary with the same values. `CarryOver` multiplies by newSeats/SeatsAtLastUpdate, which is exactly 1.0, so the capital is unchanged (PartyCampaignCapital.cs:98-102). After the step, (e) still sees `ReferenceEquals(u.ParliamentSeats, s0)`, because s0 is that new dictionary, and `Same`/`Carried` hold by value. The eve-of-oath (d) passes the same way. A load-only early seating also escapes: the eve-of-House load could seat the 119th on 2025-01-02. The next step changes nothing, and `Same(119)` and `Carried` both pass.

**What already catches the harmful cases, so the claim that SaveGameService.cs:245-246 is only read, not proved, goes too far:**
- A government re-installed at load goes through `AtStart`. `SeatedGovernment.TryAt` sets `AsOf = date` (SeatedGovernment.cs:62) and `GovernmentRecord.cs`:260 sets `FormedOn = record.AsOf`. So FormedOn becomes 2025-01-21, and (e)'s `FormedOn == OathDay` fails.
- Any wrong write at load that the next step has to undo makes the step write a new object, which breaks the identity check against the post-load s0/g0.
- A missing or misplaced call fails (h).
- The two mutants that escape both need load-only code. Today RestoreInto calls the same parameterless function that the day path uses, and the day path is pinned by (a) "each change once" and (c').
- nus2c.log shows nothing moves at load now. The (d) RECORD lines (508, 528) come from Step→AdvanceDay. (e) prints none. Only (h)'s load prints RECORD from RestoreInto:247 (lines 570, 593).

**The fix's premise holds.** `RestoreSaveState` calls `SetWorld` (SimulationManager.cs:2471-2476): it assigns `_world`, runs `SeedPublishedHistory` and `CommitCalendarYear`, and touches only base fields and pending state. On the load path, the only writers of `ParliamentSeats` and `Government` are the hook's (ParliamentSystem.cs:88, SimulationManager.cs:4028). GameController.cs:2188/2234 are on the new-game path. The shadow fork works on its own JSON copy (ShadowBaseline.cs:98-107). `Adopt` asserts `ReferenceEquals(sim.World, save.World)` (294).

**The skeptic's corrected fix.** In FromSave, after `LoadFromFile` and before `Adopt`, take `Country pre = save.World.GetCountry(CountryId.USA); object sPre = pre.ParliamentSeats, gPre = pre.Government;` and pass them to `holds`.

Right after `Adopt`, before any step, assert `ReferenceEquals(u.ParliamentSeats, sPre) && ReferenceEquals(u.Government, gPre)` for all three US-2 saves (eve of House, eve of oath, after both), since none of them owes anything at load. For (h), assert the opposite: both are rewritten at load.

Keep the existing post-step checks against the post-load s0/g0. They still catch a wrong write at load that the step has to undo.

Do not assert the pre-load identity after the step on the side the day owes. On the eve of the House, the House is owed on the step; on the eve of the oath, the government is. The finding's wording "after the load as well as after the step" would fail those two legitimately. This turns the sentence at SaveGameService.cs:245-246 into a proved claim.

### 13. The temp directory's delete catches only IOException

- **Lens:** checks - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/CongressOfRecordDiagnostic.cs:263

**The scenario.** On Windows, Directory.Delete can throw UnauthorizedAccessException ('access denied', e.g. while a scanner holds one of the four saves) as well as IOException. That exception escapes the finally, so Run throws before CheckExit.Finish. The bar then records a check whose assertions all passed as thrown. PlayProtocolCheck catches Exception in the same place.

**The fix proposed.** `catch (Exception) { }`, or catch both IOException and UnauthorizedAccessException.

**The skeptic's evidence.** The mechanism is real, but the finding's example trigger is wrong. The consequence is a spurious red result; the check can never pass wrongly.

Code path (staged file):
- Line 263, inside the outer finally: `try { if (Directory.Exists(dir)) { Directory.Delete(dir, true); } } catch (IOException) { }`.
- Line 257's `catch (Exception ex)` covers only the try block, not the finally. So an exception that is not an IOException escapes Run before lines 266-268 (`Debug.Log(sb)` and `CheckExit.Finish`).
- `dir` holds the four saves (line 132: `Path.Combine(dir, "eve_of_house.json")` and the other three).
- The three sibling temp cleanups all catch Exception: PlayProtocolCheck.cs:84 `catch (Exception) { }`, ReviewLedgerCheck.cs:381 `catch (Exception) { }`, SaveMigrationCheck.cs:103 `catch (Exception e) { sb.Append("    note      the temporary folder stays: ")... }`.

Probed on this machine (a scratchpad program; the Unity Editor was not launched). It ran on .NET Framework and on Unity 6000.5.6f1's own Mono corlib (MonoBleedingEdge/lib/mono/unityjit-win32/mscorlib.dll), with the same results on both:
- `UnauthorizedAccessException.IsSubclassOf(IOException)` is False.
- Directory.Delete on a folder holding a read-only file throws UnauthorizedAccessException.
- Copying line 263's exact shape: the exception escapes, "summary and Finish skipped".

The finding's example ("a scanner holds one of the four saves") does not produce that exception on this Windows 11 build:
- A handle without share-delete gives IOException (sharing violation), which line 263 already catches.
- A handle with share-delete, or even a mapped view of the file, still lets the delete succeed.
- The access-denied path needs a read-only file (SaveGameService.SaveToFile only uses WriteAllText, Replace and Move, so it never makes one) or a filter driver that denies DELETE. The trigger is real but rare and depends on the environment.

Consequence: it fails closed on every route.
- The cheap-table runner (CheckSuite.RunTable) and RunSimulation catch it, log "CHECKS: CongressOfRecordDiagnostic THREW UnauthorizedAccessException" and count the check as failed.
- WarmEditor.RunOne (lines 183-184) does the same with "THREW ... return 1".
- Run directly under `-executeMethod`, Unity exits 1.
- So the cost is a red bar that a re-run clears, plus the lost "ok" lines (`sb` is never logged). No false green is possible.

Note grade stands.

**The skeptic's corrected fix.** Use SaveMigrationCheck's form at line 263, so nothing escapes the cleanup and a leftover folder is reported instead of silent: `try { if (Directory.Exists(dir)) { Directory.Delete(dir, true); } } catch (Exception e) { sb.Append("    note      the temporary folder stays: ").Append(e.Message).Append('\n'); }`. The note goes into `sb`, which is logged after the finally, and it does not raise `failures` or log an error, so a cleanup failure cannot turn a clean run red. A bare `catch (Exception) { }`, as in PlayProtocolCheck and ReviewLedgerCheck, also works but stays silent about the leftover folder.

## The second pass - refuted by the skeptics

- [fixes] COMPLETED.md §783 is cited four times in the staged tree but does not exist - *The facts are right but nothing is wrong yet. This is how every pass looks while it is under review.

Facts confirmed:
- COMPLETED.md's last heading is `## 782.` at line 37385, in the working tree, the index and HEAD (37405 lines).
- COMPLETED.md, Reviews/ and Tools/review_ledger.tsv are all unmodified. `git status` shows only ProjectSettings/ProjectAuditorSettings.asset as an unstaged change.
- The four §783 pointers are where the finding says: USA_STAGE_PLAN.md:174, POLISIM_FEATURE_LIST.md:134 and :135, and TrajectorySentinelCheck.cs:117. Tools/film_scope.tsv:142 also carries `s783`.

Why it is not a defect:
1. The record has to come after the review. CLAUDE.md:67 says a § record is "what changed · the evidence line · the commit". Reviews/README.md says "what was done about its findings is told in COMPLETED.md". §782 (COMPLETED.md:37385-37405) contains a "**The review** (Reviews/2026-10-05_s782_us1_start_words.md ... then a second pass on the fixes ...)" block and a "**Bars** (... on this commit's own tree)" block. Neither can exist until this review and the bars are done. PF-18's own pointer, "(`COMPLETED.md` §783; its verifier confirmed it by reading)", names this review's verifier.
2. Every recent commit adds its record in its own commit. b183a332 added COMPLETED.md (+22) and Reviews/...s782 (+789), and in that same commit the plan's "**Built: `COMPLETED.md` §782.**". cd1fb480, d00b66aa, f03784e7, 22796935, 6d497bc0 and f33891ef each add COMPLETED.md too.
3. The staged tree cannot be committed as it stands. With carriage returns stripped, the staged SimulationManager.cs hashes to 6f5b9bc87a77…, the same as the working tree. That hash appears 0 times in Tools/review_ledger.tsv. So ReviewLedgerCheck fails the cheap bar with "UNREVIEWED" (ReviewLedgerCheck.cs:158), and CLAUDE.md:65 allows one green bar per commit, no exceptions. The bar does not pass until the review report and its row are added, which is the step where the §783 record is written.
4. §783 is not taken. No other work in the tree claims it.
5. The same finding was refuted twice before:
   - Reviews/2026-10-04_s772_e1_factors.md:550: "That is the normal state of a pass while it is under review. The scenario only happens if the session skips a step that every commit takes."
   - Reviews/2026-10-05_s780_pl_declarations_f1f2f7.md:1547: "A pointer to a section not yet appended is therefore the normal state before a commit."

One caveat: no check matches § pointers against COMPLETED.md. ReviewLedgerCheck.cs:517-518 only requires the ledger row's record field to be non-empty. So the protection is the process step, not a mechanical check.*
- [fixes] When an old save is put right, the president is dated and logged as taking office on that day, not on the record's 20 Jan 2025 - *The mechanics are as the finding says. But the dating is deliberate, follows the codebase's existing meaning of FormedOn, moves nothing in the model, and the log line is true under the file's own log convention.

1. The path happens. SaveGameService.RestoreInto restores CurrentDate (RestoreSaveState), then calls sim.SeatTheRecordOnItsDate(). SimulationManager.cs:4013 calls `TakeOffice(country, Elections.GovernmentRecord.AtStart(country, CurrentDate, _world))`. GovernmentRecord.cs:260 sets `FormedOn = record.AsOf`, and SeatedGovernment.cs:62 builds the Record with AsOf equal to `date`. The log at :4031 then prints the load day.

2. The dating is designed and pinned, not an oversight. CongressOfRecordDiagnostic.cs:204 (f) asserts `usa.Government.FormedOn == sim.CurrentDate`, and its caption reads "put right on its next day, {0}". Line 253 (h) asserts `u.Government.FormedOn == s.CurrentDate`. Lines 246 and 250 (d)/(e) pin `FormedOn == OathDay` for the unbroken world.

3. FormedOn already means "the day the game seats it", not the record's day. AtStart dates any start government by the start. Biden at the USA's 2024 start is FormedOn = the start day, and the Desk says "SINCE <start>". His record row is From D(2021,1,20) (WorldClock.cs:373). The finding accepts this for the Desk's SINCE. "takes office on <FormedOn>" in the log makes the same claim.

4. Where the record's oath date matters, it is read from the record, not from FormedOn:
   - EconomicVote.TookOffice (EconomicVote.cs:104-126): for Outcome "of record" it finds the Trump row through TryGovernmentAt(FormedOn) and returns `rows[at].From > opened ? rows[at].From : opened`.
   - PreStartWindowOpens (PublicationSystem.cs:123-133) gives 2024-11-05 for any day after it. The new Usa2026 row is skipped by `day.Year % 4 != 0`. So the term starts 2025-01-20 for a put-right too.
   - The Parliament row prints `Chip(DeskDay(ofRecord.From))` (ParliamentRows.cs:500) and WorldClock.ExecutiveStanding prints `"IN OFFICE FROM " + ViewDay(g.From)` (WorldClock.cs:176).
   - The other FormedOn readers are FinancePartner's identity token and Germany/round-only gates (SimulationManager.cs:3177, 3395, 4091). None has a US effect.

5. The log line is not false:
   - In this file's RECORD: lines, "on {date}" is the game's day. The chamber's sibling line at :4007 prints "seated on {CurrentDate}: " beside the record's own "SEATED 3 JAN 2025".
   - ", of record" names the government's Outcome ("of record"), not a source for the date.
   - In the old save's game, Biden really did govern until the load day, so the line reports this game's history truthfully. Backdating to 2025-01-20 would be the fiction.
   - No tool parses the line. A grep for "takes office" in Tools/ finds nothing, and in Assets/Editor only diagnostic captions.
   - CLAUDE.md's claim convention governs documents and source comments, not Debug.Log strings.

6. The hook's DECLARED list already declares the put-right itself: "put right as it loads ... (DECLARED)". Dating it on the day it happens is AtStart's existing convention, not a new premise.*
- [load] Loading a pre-US-2 save dates Trump's government to the load day, not to 20 Jan 2025 - *The mechanics are as described, but they are not a defect. A load-day FormedOn is the codebase's standing convention for a government of record: FormedOn is the day the game seats it.

What the finding gets right:
- SimulationManager.cs:4013 calls `TakeOffice(country, GovernmentRecord.AtStart(country, CurrentDate, _world))`.
- GovernmentRecord.cs:260 sets `FormedOn = record.AsOf`.
- SeatedGovernment.TryAt builds the Installed record with the date it is passed as AsOf (`new Record(Standing.Installed, g.Cabinet, g.Support, ..., date)`). Trump's row has CabinetSourced = true.
- RestoreInto runs the hook at load, and Desk.cs:327-328 prints `"SINCE " + DeskDay(g.FormedOn)`.
- So a pre-US-2 save loaded on 2025-06-01 reads SINCE 1 JUN 2025.

Why it is not a defect:
1. **The Desk/Parliament split already exists at every US start, with no load.**
   - WorldFactory.cs:1092 is `AtStart(country, SimulationManager.EpochDate, world)` and GameController.cs:2188 is `AtStart(after, start, _world)`. Biden's FormedOn is therefore the US start day, not 2021-01-20.
   - The Desk reads "SINCE <start day>", while the new DrawRecordRow reads `Chip(DeskDay(ofRecord.From))` = 20 JAN 2021 and "IN OFFICE FROM 20 JAN 2021".
   - The diagnostic's own unbroken world dates Biden to 2024-12-28, the eve it opened on.
   - So "the two pages disagree" is the existing meaning of FormedOn (the day the game seated the government), not something the load path adds.
2. **The one reader that needs the real term start is unaffected.** EconomicVote.TookOffice (§752) documents that FormedOn is not the term's start for a government of record seated outside play; it walks the record instead. PublicationSystem.PreStartWindowOpens keeps only USA chambers whose election year is divisible by 4. The new Usa2026 row is skipped and there is no 2028 row, so it returns 2024-11-05 for any FormedOn after that date, and TookOffice gives 2025-01-20 for every load day.
3. **The loaded game's own events start on the load day.**
   - TakeOffice calls ResetArrivalBudgetWindow (1206, 4029). The next day tick (2431-2434 for the AI's bill, 2456-2461 for the player's window) then lays the new government's arrival budget after the load.
   - A load-day FormedOn agrees with that. The proposed `AtStart(country, g.From)` would print SINCE 20 JAN 2025 over a government whose arrival budget is tabled in June.
4. **The claims hold as worded.**
   - The hook's doc says the House and president are "seated where they differ - each day, and once as a save loads".
   - The SaveGameService comment says "what the game does not elect is the record's on the save's date".
   - The plan's Built line says "an old save is put right as it loads".
   - After the load, the House, the president and the capital are all the record's. None of these claims says FormedOn equals an unbroken run's.
   - (f) `FormedOn == sim.CurrentDate` and (h) `FormedOn == s.CurrentDate` pin this meaning on purpose. (d) and (e) pin OathDay for saves from the US-2 build, and (e) shows nothing moves for a save cut after both days.
5. **No other reader reaches the USA.**
   - TryAiConstructiveVote is Bundestag-only (3028).
   - RoundsApply covers only the Riksdag and Bundestag (3233-3235), so the `held > g.FormedOn` readers never run for the USA.
   - DrawPresidentRow is gated on TwoRoundElection.RuleOf.
   - FinancePartner uses FormedOn only as an identity key.
   - Portfolios are trivial for a one-party cabinet.
   - The RECORD: lines are Debug.Log; no check in Assets/Editor or Tools parses them. "Seated on <day>" there is the game's act; "SEATED 3 JAN 2025" is the record's words.

Reach: only a US save from a pre-US-2 build cut after 2025-01-20 (the US start is in 2024). The effect is one line in the Desk role chip's hover slip, with no simulation or money-path effect.*
- [words] The start card's Canvas Election figure changed, but only a dry film is declared and the bar tier will not flag it - *The finding is right that the words changed. It is wrong that no film will show them.

TRUE: StartBrief.cs:211 changes the Election row's figure. CountrySelectorScreen.cs:792-806 (BuildBriefLedger) draws Row.Figure as Canvas text through CanvasChrome.MakeText. bar_tier.ps1:45/52 match Canvas surfaces by file name. Running `bar_tier.ps1 -Staged` confirms that no "canvas: REQUIRED" line prints, because CountrySelectorScreen.cs is not in the staged paths.

REFUTED, "no film will show the new words": the same run prints "BAR TIER: SIMULATION + UI ... per item : ... + the dry film of the SESSIONS THE ITEM NAMES ... + ONE filmed width at the item's end". GameController.ParliamentRows.cs is UI tier, so the commit already owes one real filmed width (CLAUDE.md: "UI -> the cheap bar, the dry film and one filmed width"). The item is the USA's alone (drys783 = USA@1280x720), so that film is a US film.
- Every film passes the brief ledger before any game frame. UiScreenshotDriver.cs:351-360 calls `startScreen.ShowStartPanel(startCountry, null)`, then `Capture("01f2_start_points")`, then `RecordCanvasTextAssert("01f2_start_points", controller)`.
- ShowStartPanel leads to BuildSheet, which calls `BuildBriefLedger(column.transform, points[selected])` (CountrySelectorScreen.cs:503). The new Election figure is drawn there and the canvas text guard asserts on it.
- `-shotload` does not skip this frame: it loads only "once the game has landed on the Desk" (driver line 87).
- The finding names the wrong frame. The ledger is on 01f2_start_points. real782 was only *cut* at 01h (6 captured: 00, 00a, 01, 01f2, 01g, 01h).

REFUTED, "declares only drys783" and "the reason claims the brief's words":
- film_scope.tsv:1-14 is "THE DRY-FILM SCOPE LEDGER". No real film is ever declared in it.
- Its last column is "why these countries and widths, and no others". Row 142's "the brief's words move only in a US game" is that scope reason, used correctly. It does not claim the dry film verifies any Canvas text.

No clip path:
- CountrySelectorScreen.cs is not in the staged diff, so the lane (SheetColumnWidth - LedgerNameWidth - 12f), the face (Display 14 bold) and the row are unchanged.
- COMPLETED.md §782 records real782 reading the old 60-character figure "on one line" in that lane.
- The new figure is 54 characters. Its tail "THE RECORD SEATED ON ITS DATES" has 25 glyphs against the old tail's 31, with no wider letter shapes.

Other Canvas text: nothing else in the diff changes it. ChamberAt at the US start (12 Mar 2024) still returns the 118th, so the start card's caption and its "In the House" row are unchanged. The clause and NotHeldReason are drawn on no screen before US-8 (§782).

What remains: a pre-existing limit. Canvas strings that come from a provider file (StartBrief, StartPoints.ModeLine, WorldClock.StartLine) never trigger the canvas line. US-1 had the same shape and named its real film in its plan. This change did not introduce the limit.*
- [words] The ledger's new Election figure has no subject: 'THE RECORD SEATED ON ITS DATES' - *The scenario happens: StartBrief.cs:211 (staged) emits "NONE IN THIS GAME YET · THE RECORD SEATED ON ITS DATES" under WorldClock.NoElectionYet, which holds only for the USA. The claimed defect does not.

1. The title is wrong: the figure has a subject, "THE RECORD". It uses "the record" to mean what the record holds, and that is the project's standing usage, word for word:
   - CongressOfRecordDiagnostic.cs:266 prints "=== CongressOfRecordDiagnostic: the record seated on its dates ===".
   - USA_STAGE_PLAN.md:169 (US-1's own paragraph, b183a332) says "until US-2 seats the record by its dates".
   - POLISIM_FEATURE_LIST.md:87 (PS-8, recorded with F8 in 99fdcf1c) says "the record seated by date".
   - SimulationManager.cs:3993 names the hook SeatTheRecordOnItsDate.

2. The row never stands alone on screen. For the USA's presidency, Rows() emits "President" (Joseph R. Biden Jr. (DEM)), "Since" and "House" rows before it (StartBrief.cs:180-182) and "In the House" after it (225-228). CountrySelectorScreen.BuildBriefLedger draws exactly those rows in that order (CountrySelectorScreen.cs:792-815). What the record holds is named in the lines directly above, and the slot name "Election" says what the figure replaces.

3. Every reading is true, so there is no path to a false belief:
   - As a short passive ("the record's offices are seated on its dates"), it is R-US1 (a).
   - As an active verb ("the record seated them on its dates"), it is true of the 118th and Biden shown above it, and of the 119th and Trump to come.

4. In game the subject is spelled out. GameController.ParliamentRows.cs:491 and :497 draw "THE HOUSE" and "THE PRESIDENT", and the slip at :506 says "THE RECORD'S HOUSE AND PRESIDENT ARE SEATED ON THE RECORD'S DATES".

5. US-1's figure did name its subject, but what it said (the start's House and president hold) is false after US-2. The new figure has to state a rule, and it does.

6. The proposed fix is weaker, not better:
   - "HOUSE AND PRESIDENT ON RECORD DATES" drops the verb, which carries the meaning, and swaps one compression for another.
   - Its 60-character limit is self-imposed. The figure lane is SheetColumnWidth 1260 - LedgerNameWidth 190 - 12 = 1058 canvas units (CountrySelectorScreen.cs:813, 829, 840).
   - It leaves out the REAL film the start card's Canvas words owe (CLAUDE.md:61; US-1's done-when, USA_STAGE_PLAN.md:171). Re-pinning StartBriefDiagnostic alone does not prove the words on screen.

Side note, outside this finding: US-2 already changed this Canvas row. The only film declared for it is the dry one (film_scope.tsv drys783), which cannot claim Canvas text. CLAUDE.md:61's rule names the files touched, and StartBrief.cs is not among them, so by the letter no real film is owed. By US-1's precedent a REAL 1280 film of the USA start card is owed.*
- [checks] 'Carried once ... not again at the oath' cannot detect a second carry, because CarryOver changes nothing when the seats have not changed - *The arithmetic is right, but no failing path follows from it. An extra carry against the same House is an equivalent mutant: it leaves the state bitwise identical, so no check could ever tell it apart and nothing goes wrong.

1. The no-op (PartyCampaignCapital.cs:97-102). Once the 119th is seated, a second `CarryOver(capital, country.ParliamentSeats)` gives `ratio = newSeats / (double)SeatsAtLastUpdate` = n/n = 1.0 exactly. `strength * 1.0` is exact, `Clamp` changes nothing in [0,100], and `SeatsAtLastUpdate = newSeats` writes back the same int. The finding itself says "It also does no harm." In mutation-testing terms this does not weaken the check.

2. The label is accurate in its own terms. Line 172 defines the carry as "by the 119th's seats over the 118th's", and `Carried()` (line 121) pins exactly one application of that ratio: `SeatsAtLastUpdate == house119[k]` and `strength == Min(100, Max(0, eveStrength[k] * house119[k] / (double)house118[k]))` within 1e-9. The seeded strength is 50.0 (WorldFactory.cs:1074). The ratio is not 1 for either party (Usa2022 is REP 222 / DEM 213 at PartySystem.cs:777; the 119th roster is REP 220 / DEM 215 at :528-529), and 50 is far from the clamp. So a second application of the ratio fails `capitalOnOath`, `capitalAfter` and the "as carried" conjuncts in (d), (e) and (h) by orders of magnitude. That is the only double carry that changes anything, and it needs `SeatsAtLastUpdate` put back to the 118th's first. Read as a statement about the capital's state, which is what the conjuncts test, "not again at the oath" is proved.

3. The realistic way to get a second carry at the oath is caught by the neighbouring checks. The hook's carry sits behind `!SameSeats(...)` and runs right after `ParliamentSystem.SeatChamberOfRecord(country, seated)`. That assigns `PartySystems.InitialSeats(...)`, which builds a `new Dictionary` on every call (PartySystem.cs:714). So the hook's chamber branch firing again changes object identity, and three checks catch it:
- line 137/169: `seatChanges == 1 && seatChangedOn == HouseDay`
- line 246, the eve-of-oath save loaded and stepped across the oath: `ReferenceEquals(u.ParliamentSeats, s0)`
- line 250, (e): the same identity check.

`TakeOffice`, the oath's path, calls no `CarryOver`. Only a hand-planted bare `CarryOver` call with no re-seat goes unseen, and that changes nothing.

4. The proposed rewording would not be more exact. "Carried against other seats" also has no-op versions: lines 97-98 skip a party that holds 0 seats or has no key. So the reworded label would overclaim in the same way the finding says the current one does.*
- [checks] Check (c') runs the real day loop but cannot tell which vintage the hook seats, and no step reaches the president's term end - *The finding's facts are right, but neither half has a path to a failure.

1. The vintage "blind spot" hides nothing, because the two choices give the same result. In the staged PartySystem.cs, SeatsAt(Usa2026) hits `default: return null;` (:782). InitialSeats then falls back to `foreach (PoliticalParty p in For(id)) { seats[p.Abbrev] = p.SeedSeats; }` (:717-720). SeatsAt(Usa2024) is `return Roster(CountryId.USA);` (:778), and Roster (:798-803) builds `list.Add((p.Abbrev, p.SeedSeats))` over the same For(id). So InitialSeats(USA, Usa2026) and InitialSeats(USA, Usa2024) are the same table by construction, created parties included.
   - This holds for every country: each latest sourced vintage's table is `Roster(id)` (Sweden2026, Germany2025, Poland2023, Italy2022, Usa2024, France2024).
   - A hook seating `ChamberAt(id, date).Vintage` would therefore get the same values from `SameSeats` (SimulationManager.cs:4003). It would carry the same capital, and its log line reads RecordStanding, not the hook's vintage. Nothing could observe the difference.
   - The two choices are also the same today and stay the same once the 120th is sourced, so no check can or needs to tell them apart.
   - The diagnostic already declares this equality at lines 29-30: "compared as the same objects, since seat values cannot tell the 120th from the 119th while its table is unsourced". That is the condition the finding's own fix sets for "no change". (c') still has real power: it compares object identity (line 221), so it fails a hook that re-seats on a transition or regardless of equality, and a TakeOffice on 2027-01-03.
   - (c) is labelled "WorldClock's answer, not a stepped day" (lines 27, 179). It pins `SeatedVintage(USA, Day120) == Usa2024` (line 182), the value the hook reads at :4001.

2. 2029-01-20: the hook never reads TermOfRecordEnds. Its only executive input is `TryGovernmentAt(id, CurrentDate, out g) && country.Government?.Executive != g.President` (:4010-4011).
   - Past the term, TryGovernmentAt returns the same open Trump row. `Holds => date >= From && date < Until` with Until = Open (WorldClock.cs:74, :374).
   - (c) pins that answer on 2029-06-01 (line 189: `TryGovernmentAt(CountryId.USA, Late, out stillOfRecord) && stillOfRecord.President == "Donald J. Trump (REP)"`). On the oath day, (a) pins Executive == "Donald J. Trump (REP)" (line 158), so the comparison past the term is the same no-op that (e) steps.
   - TermOfRecordEnds is read only by ExecutiveStanding, which (c) pins on both sides of 2029-01-20 (lines 186-190), and by the UI row (GameController.ParliamentRows.cs:463). There is no hook behaviour keyed on the term's end for a step to run.
   - The finding's "daily re-install" example would need code that does not exist.

The finding's minor facts check out: DaysPerTurn = 365 with an epoch-relative boundary (SimulationManager.cs:216, :484), so no turn falls in the 5-day window from 2026-12-30. TryNextPollingDay returns false for the USA (WorldClock.cs:539). The finding is accurate as an observation, but it is not a defect and asks for no change.*
- [checks] The executive names are typed into the mechanism assertions, though the record holds them and the House tables beside them are computed - *The finding's list of sites is accurate: the names are typed at 125/158/189/204/219/246/250/253, and the "Joseph R. Biden" prefix at 142/146/149/211/243. The rest of its case does not hold, for five reasons.

1. The House is not taken from the record either. CongressOfRecordDiagnostic.cs:69-70 reads `PartySystems.InitialSeats(CountryId.USA, ElectionVintage.Usa2022)` and `(..., ElectionVintage.Usa2024)`. Which House is a typed vintage name; only the seat figures are looked up. The hook resolves the House by date instead (staged SimulationManager.cs:4001, `ElectionVintage seated = Elections.WorldClock.SeatedVintage(id, CurrentDate);`). So the test types the identity of both the House (its vintage) and the president (the name). CLAUDE.md:53 says "a check pins an election by name", and CLAUDE.md:80 says figures are "generated, not transcribed (§419)". A name is an identity, not a figure, and the claim convention covers comments and docs, not check literals. The proposed fix would create the asymmetry the finding describes.

2. The proposed oracle repeats the mechanism's own condition, so it checks itself.
   - The hook (staged SimulationManager.cs:4010-4013): `Elections.WorldClock.TryGovernmentAt(id, CurrentDate, out ... g) && country.Government?.Executive != g.President` then `TakeOffice(country, Elections.GovernmentRecord.AtStart(country, CurrentDate, _world))`.
   - AtStart fills the name from the same lookup: GovernmentRecord.cs:254 `WorldClock.TryGovernmentAt(country.Id, start, out ... ofRecord)`, :260 and :270 `Executive = ofRecord.President`.
   - Comparing `Executive` against `TryGovernmentAt(USA, d).President` only restates the hook's stop condition. It cannot see wrong row content reaching the game.

3. The typed tags are the only pin on the President string's party.
   - `TryPresidentOfRecord` strips the tag (WorldClock.cs:161-163: `string tagged = g.President ?? g.Head; ... name = tagged.Substring(0, tag)`), so (i) never sees "(DEM)" or "(REP)".
   - `PmParty` comes from the Head string's tag (GovernmentRecord.cs:309-317 `HeadParty` parses `government.Head`), not from President.
   - So the exact matches `== "Donald J. Trump (REP)"` and `== "Joseph R. Biden Jr. (DEM)"` are the only check that the President string's party in the world is right. The fix would remove that coverage. Under the checks'-power lens, the typed oracle is the stronger one.

4. Line 189 is not a mechanism check. It is (c), the record's own answer: `WorldClock.TryGovernmentAt(CountryId.USA, Late, out ... stillOfRecord) && stillOfRecord.President == "Donald J. Trump (REP)"`. Taking its expected value from the record would be circular, the same reason the finding gives for keeping (i) typed.

5. The only scenario is a deliberate respelling of the record. That fails loudly and names the cause: lines 126, 168, 212 and 238 print `Government?.Executive`. The same edit would break (i) at 193-194 (kept typed under the finding's own fix) and the older StartBriefDiagnostic pins at :57 and :60 (`"Joseph R. Biden Jr. (DEM)"`, above the staged hunk @@ -61), so the fix would not make a respelling pass anyway. The typed names are also the done-when's own terms: "the 118th and Biden before, the 119th and Trump after".

The typed strings match the record rows at WorldClock.cs:373-374 (`president: "Joseph R. Biden Jr. (DEM)"` and `president: "Donald J. Trump (REP)"`), and run nus2c was clean. No failing path exists today.*


## What the author did about the second pass

- **1** (PF-19 named Italy, which this build holds no election for, and listed countries a predicate decides) - PF-19 now names the condition: the player's country on a day `SimulationManager.PollingDayToday` is raised and `SimulationManager.RoundsApply` is false; no country is listed.
- **2, 12** ("nothing moves as a US-2 save loads" was read, not proved) - `FromSave` takes the save's own House and government objects before the load; (d) and (e) assert the game holds those same objects as it loads, before any step, and keep their after-step checks; (h) asserts both are rewritten as it loads. The sentence in `SaveGameService.RestoreInto` is now a proved claim.
- **3** (`ChamberSeatedOn`'s doc) - mirrors `SeatedVintage`'s: the chamber of record's own where its table is sourced, else the latest sourced - past the record the one that stands, and for a chamber whose table is not sourced (E-47) a later chamber, which `SeatingDeviation` says.
- **4** (a load-time install keeps the outgoing government's pending bill) - no behaviour change, as the skeptic advised: the hook's DECLARED sentence on old saves now points to the rule every install follows - §630 (a pending bill outlives the role that introduced it) and §632 (no window opens while it stands).
- **5** (no screen named the record's coming dates, so the oath flipped the player's role unannounced) - `WorldClock.NextOfRecord` gives the record row's slip a line for the next House of record and, where the record seats the executive, the next president - "NEXT OF RECORD: THE HOUSE ELECTED 5 NOV 2024 · SEATED 3 JAN 2025", "NEXT OF RECORD: DONALD J. TRUMP · REP · TAKES OFFICE 20 JAN 2025" - read from the rows, each dropping away on its day; pinned in (c) on the eve, the day before each date, each date, the 120th's day and 1 June 2029. A chip on the row itself, and the start card's ledger naming the dates, are left to Design (the row's width at 1280; the card is Canvas).
- **6** (the president's party only a mark) - the slip line names it in words, in §770's order: "THE PRESIDENT: DONALD J. TRUMP · REP · IN OFFICE FROM 20 JAN 2025".
- **7** (the brief read as if Biden takes office on 20 January 2025) - the clause names the successors from the record's own rows: "No election is held in this game yet - the House elected on 5 November 2024 is seated on 3 January 2025 and Donald J. Trump (REP) takes office on 20 January 2025, as the record dates them; no Senate is modelled." Each half gated on its predicate; `StartBriefDiagnostic` re-pinned whole.
- **8** (the slip's first line named the president where the row would not) - built from the gates: the president named only while the record seats the executive, and "THE GAME DOES NOT ELECT THE HOUSE YET" once US-8 holds the presidential election; the row's doc and its call site say so. Today's words unchanged.
- **9** ((g) never stepped a world whose player is another country) - a world with a German player (CDU) stepped from the eve across both days keeps the AI USA's House and president, the same objects.
- **10** (a REP player's arrival budget at the oath never asserted) - a REP world: the AI government's arrival bill tabled on the first day and resolved by the eve of the oath (one day of slack, so a longer bill fails there, naming the premise); on the oath's day the REP player's own arrival window opens - the reset as the record installs, apart from the close.
- **11** (`WorldClockDiagnostic`'s deviation held only by substring) - the deviation's dates are only the day asked and the record's, and it names the table that stands, on the 120th's day and on 1 June 2029; the row holding 1 June 2029 is read; the comment's pointer names the standing words only.
- **13** (the temporary folder's delete caught only `IOException`) - any exception is caught and the leftover folder is said as a note, never a failure.

Named run `nus2d` after the fixes (`CongressOfRecordDiagnostic`, `WorldClockDiagnostic`, `StartBriefDiagnostic`, `PlayProtocolCheck`): 4 of 4 clean, every new case among them. The dump re-taken on the fixed tree (`us2b`) is byte-identical to `fp755` on both seeds.
