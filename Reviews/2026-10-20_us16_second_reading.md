# US-16: second reading of the staged Congress count (2026-10-20)

**Reviewed:** the staged index on `main` (`git diff --cached`, 25 paths), read-only, by a second independent reader, after `Reviews/2026-10-20_us16_congress_count.md`.
The money path `Assets/Scripts/Simulation/SimulationManager.cs` was read at staged blob `716cfda7` (SHA-256 without CR: `696b0965dd1d75e8...`).
No Unity was run. Each finding below was traced in the code; anything that could not be substantiated was dropped.

**Verdict.** The arithmetic, determinism and save paths hold. Two defects reach play in the R-US22 branch, which is the branch US-16 makes reachable:
- an acting president is treated as an incumbent nominee (M1);
- an acting president of the player's own party is not the player's (M2).

The rest are false or stale words (L3 to L8). Of the first reading's findings, 1, 2, 4, 8 and 11 are fixed in the staged tree. Finding 6 is still open: no `reviewed` row for this `SimulationManager.cs` state is in `Tools/review_ledger.tsv`, so `ReviewLedgerCheck` fails until the integrator adds it. Finding 5 is fixed only in part (L3).

---

## Findings

### M1 - An acting president stands for re-election as "the Speaker of the House"
**Where:**
- `Assets/Scripts/Elections/UsTermLimit.cs:282-287`: the third branch of `TryPresidentOn`.
- `UsTermLimit.cs:489-509`: the private `NomineeOf`.
- `UsTermLimit.cs:576-577`: `EligibleFor`.
- `UsTermLimit.cs` `Pick` and `PartyPeople`.
- `Assets/Scripts/Elections/UsPresidentialElection.cs:386`: the ballot name is taken from `n.Person`.

**What goes wrong:**
1. Start with a contingent contest the House cannot choose and a Senate split 50-50 (or any Senate where no ticket has 51). At the oath, `ActingGovernmentOf` seats `"the Speaker of the House (DEM)"` with `Outcome = "acting"`.
2. The model declares that the House votes once, so the acting government holds to the next oath. On the next polling day (for example 7 Nov 2028):
   - `UsPresidentialElection.TryInOffice` is false, because the latest contest whose oath has come is undecided.
   - `RecordSeatsExecutive` is false.
   - `TryPresidentOn` therefore falls to `country.Government` and returns person `"the Speaker of the House"`, party DEM.
3. `NomineeOf(DEM)` sees an incumbent with 0 elections and returns `Person = "the Speaker of the House"`, Basis "the president, who may be elected again - never elected". The count puts that title on the ballot (`UsPresidentialElection.cs:386`).
4. If it wins:
   - `ExecutiveOf` gives an *elected* president named "the Speaker of the House" (the Desk shows "HOUSE").
   - `ElectionsOf` counts every future "the Speaker of the House" as one person, so the Twenty-second Amendment would eventually bar a title.
5. For a DEM player:
   - `EligibleFor` returns no choices (`holds && Eligible`).
   - `Pick` refuses with "the party's president may be elected again".
   - `PartyPeople` lists "the Speaker of the House" as one of the party's people.

In the Vice President case, the VP-elect (for example "JD Vance") stands as "the president ... never elected". That is plausible, but the basis words are wrong and US-28b's pick is closed for that party.

**Fix:** in `NomineeOf` (or through a flag out of `TryPresidentOn`), treat a sitting presidency with `Outcome == UsPresidentialElection.ActingOutcome` as no incumbent:
- If the Speaker acts, the party takes its unnamed default with the pick open (`OutParty`-like).
- If the Vice President acts, use the `ViceDefault` path, with the pick open.

Exclude the Speaker's title from `PartyPeople`. Pin this in PresidencyDiagnostic (f'): `NomineesFor(usa, 2028 polling day)` must not contain "the Speaker of the House".

### M2 - An acting president of the player's party is not the player
**Where:**
- `Assets/Scripts/Simulation/Congress.cs:595`: `playerPresident` requires `UsPresidentialElection.TryInOffice`.
- The readers that refuse: `SimulationManager.cs:1648` (the docket) and `SimulationManager.cs:1726` ("you do not hold the presidency today").

**What goes wrong:**
- `TryInOffice` is now false whenever someone acts. A player whose party's Vice President was chosen by the Senate, and who therefore governs (`PmParty` is the player's), cannot sign or veto any bill for the whole acting term (up to four years).
- For that same term, the president row says ACTING, the player's party holds the presidency, and the budget and role gates treat the player as governing. Only the bill gate disagrees.

**Fix:**
- Decide the reading. R-US7 says "the player is the president" when the president is the game's own and of the player's party, and an acting presidency seated by the game's own contest qualifies. The suggested change: `playerPresident = (TryInOffice(...) && sworn.WinnerParty == player) || (TryActingOn(...) && acting.ActingParty == player)`.
- Otherwise, add it to the R-US22 card line as DECLARED.

### L3 - The president row reads "HOUSE" again after the next polling day
**Where:** `Assets/Scripts/UI/GameController.ParliamentRows.cs:710`.

**What goes wrong:**
- `speakerActs` tests `last.ActingAs`, where `last` is `TryLatest`, the latest contest *held*.
- From the next polling day until its oath, `last` is the new contest, whose `ActingAs` is null. The row falls back to `Surname("the Speaker of the House")`, which gives "HOUSE". This is the first reading's finding 5 coming back for about 75 days of every Speaker-acting term.

**Fix:** read the acting contest with `UsPresidentialElection.TryActingOn(_playerCountry, today, out var acting)` and test `acting.ActingAs`.

### L4 - Player-facing and log words that go false
1. **`GameController.ParliamentRows.cs:861`** says "THE SEATS NOT UP STAY THE RECORD'S, BY DATE", and **`GameController.PoliticsV35.cs:252`** says "THE SEATS UP ON <latest> WERE ELECTED IN THE GAME; THE OTHERS SIT AS THE RECORD SEATS THEM".
   - From 3 Jan 2029 two game classes sit: the 2024 class I seats are the game's, not the record's.
   - Fix: count the game's seated contests and say "the seats the game elected on <days>", or "the rest of record".
2. **`Assets/Scripts/Simulation/SimulationManager.cs:5334`.** When a later Congress count fails (for example 2028), the warning says "the record's House and Senate stay seated by date".
   - Once a game Congress has sat, `UsCongressElection.TryInOffice` keeps the game's previous House. `WorldClock.RecordSeatsChamber(Country, DateTime)` stays false, so the 2024 House sits on.
   - Fix: branch the words on `TryInOffice(country, CurrentDate, out _)`.
3. **`UsPresidentialElection.ActsAsVicePresident`** still reads "acting as President until the House chooses". The House never votes again (DECLARED in `ActingGovernmentOf`), and the string goes into the contest line and the government's `Basis`.
   - The first reading's finding 9 was fixed in the slip only.
   - Fix: "... acting as President to the next oath (the House votes once - DECLARED)".
4. **`Assets/Scripts/UI/GameController.cs:6597-6611` (`RunNationalElection`, not in the diff).**
   - It still adds an `ElectionMethod.NotImplemented` record and logs "ELECTION: the chamber is not elected for USA ...". The comment says "(the USA's House, until US-16) the polling day elects no chamber", but on that same day the game now elects the House.
   - No live reader is hurt: the readers that skip `NotImplemented` serve the parliamentary rounds, which do not apply to the USA. The log and the comment are still false.
   - Fix: reword both (for example "the chamber is elected on the day loop, beside the president").
5. **`Assets/Scripts/Simulation/ParliamentSystem.cs:93`** says "A second chamber the game elects (US-16; ...) seats through a sibling of its own". US-16 now seats through this very method (`SimulationManager.SeatTheSecondChamberOfRecord` passes `UsCongressElection.SenateOn`).

### L5 - The record seats its own 2024 presidential result's Senate consequences in a world that elected someone else
**Where:**
- `Assets/Scripts/Elections/UsCongressElection.cs:415-427` (`SeatSides`), over `UsSenate.On`.
- The record rows: `UsSenateRecord.cs:139-140` and `214-215`.

**What goes wrong:**
- The card's own eve world elects the DEM ticket. Yet on 10 Jan 2025 the record still vacates Vance's Ohio class III seat (he resigned to become Vice President), and on 20 Jan Rubio's Florida seat; both appointees sit from 21 Jan.
- So the game's Senate seats 99 from 10 to 19 Jan and 98 on 20 Jan 2025 (REP 52, then 51), in a world where neither man left. Sides are unchanged, so nothing flips today.
- "100 after every election" is measured only on 3 January (the diagnostic's (d) and (f)). A Senate choice made at the oath (`ActingGovernmentOf` calls `ChooseVicePresident` when a world opens after the count day) reads a 98-seat Senate against a majority of 51 of the whole 100. Similarly, California's unexpired special is folded into the full term by design, yet the record seats Schiff on 9 Dec 2024, before the game's own seat sits.
- Under R-US1 (a) this is "whatever the game does not elect"; it is not stated.

**Fix:** DECLARE it on the card's US-16 block (holdover seats of record keep the record's resignations and appointments, including those that follow from the record's own presidential result), or measure the sum over 3 to 21 January as well.

### L6 - `ActingGovernmentOf` re-seats every day if the chosen running mate has no name (hardening; unreachable today)
**Where:** `Assets/Scripts/Elections/UsPresidentialElection.cs:259-270`.

**What goes wrong:**
- If `vice.Candidate` is null (`RunningMateOf` returns null for a party not on the roster), `contest.Acting` stays null.
- `ActingGoverns` is then false every day, so `SeatTheRecordOnItsDate` calls `TakeOffice` daily. That resets the arrival budget window each time and appends to `contest.Line` each day.
- Today REP and DEM always resolve a mate, so this is not reachable.

**Fix:** fall through to the Speaker when `vice.Candidate` is null, or set `Acting` to a non-null label.

### L7 - `ChooseVicePresident` orders tallies with an unstable sort
**Where:** `UsPresidentialElection.cs:313`.

**What goes wrong:** `List.Sort` on `Votes` alone leaves equal tallies (a 50-50 Senate) in an order the sort algorithm decides. The choice does not change (`Find(Votes >= 51)`), but `SenateChoice`'s order, the contest line and the slip's order can differ between runtimes (Editor Mono vs a player build).

**Fix:** add a tie-break on `string.CompareOrdinal(a.Party, b.Party)`.

### L8 - `TryVicePresidentOn` has no answer under an acting president
**Where:** `UsTermLimit.cs:296-322`.

**What goes wrong:** the first reading's finding 7 is still true. The third branch can no longer match, so under a Speaker acting, `UsSenate.VicePresidentParty` falls back to the Speaker's party. That party then breaks Senate ties (`UsCongressElection.CountSenate`'s cascade, `UsSenate.MajorityOf`, the economic vote's "divided"), although no Vice President is in office.

**Fix:** state it on the R-US22 card line, or return no party under `ActsAsSpeaker`.

---

## Checked, no defect found
- **Determinism.**
  - No count draws a stream. The tie draw is pure (`ElectoralCollege.TieBreak`, SplitMix64 over seed, year and place).
  - `CountedPoll` is called twice, but `SimulationRandom.ElectionNoiseOn` is keyed on the master seed, country and day, so both calls give the same poll. σ is 0 in play anyway.
  - `TieSeedOf(MasterSeed, year)` is kept on the contest and the master seed is saved.
  - Every Dictionary iteration reduces order-free (sums, max, keyed overwrite). The House iterates a `SortedDictionary`; the Senate iterates a `SortedSet` plus catalog order.
  - A no-player world never enters `HoldUsPresidentialCount` or `SeatTheRecordOnItsDate`, so the `traj_*` baselines (seeded, no player) do not move.
  - Shadow and fork worlds (`ShadowBaseline`) are JSON deep copies through the save, so the shared-list hazard is limited to `ClonePreviewCountry`. Nothing on the preview path calls `Hold`, `ActingGovernmentOf` or `ChooseVicePresident`.
- **Dates.**
  - The order is polling day < 3 Jan (`ConvenesAfter`) < 6 Jan (count) < 20 Jan (oath) in every year; the dates are built explicitly, so leap years do not matter.
  - On 3 Jan both the record's 119th and the game's convene, and the game branch wins in `SeatTheRecordOnItsDate`.
  - Chaining: `Count` takes the latest game contest strictly before the day, with `VBase = prior.V`. `ClassUp` gives 2028→III, 2032→II, 2036→I.
  - `house_maps.csv` carries 2026 rows (50 states, 435). `SeatsInForce(2028+)` reads them, and they still sum to 435.
  - A game opened after 5 Nov 2024 swings 2028 from the record's 2024 House.
- **Save and clone.**
  - `Country.UsCongressElections` is saved (public fields; lists are field-initialised and empty, so Newtonsoft's auto creation appends to an empty list).
  - The new `Contest` fields `TieSeed`, `SenateChoice`, `Acting`, `ActingParty` and `ActingAs` are saved.
  - Format 49 refuses v48 (the diagnostic's (e)). The clone shares the list read-only.
- **Seats.**
  - The House count asserts 435 and builds `HouseSeats` from the roster.
  - The specials are only rows of another class on the day, and California's unexpired race is excluded because it is the same class.
  - `SeatSides` keys on (state, class), so a special cannot double-count the class. A vacant record seat is absent and is filled by the game's seat.
  - The Twelfth Amendment's delegations read `UsCongressElection.TryInOffice` through `DelegationsOn(Country, DateTime)`.
- **Static guards.**
  - The added backticked claims all name existing members (`SimulationManager.HoldUsPresidentialCount`, `UsCongressElection.Contest/Hold/SenateOn/TryInOffice`, `UsSenate.On`, `WorldClock.TryReference`).
  - No numeric const is added under `Assets/Scripts/Simulation`, which is ConstantProvenanceCheck's scope; the Elections consts carry SOURCED and CONVENTION anyway.
  - No `.Cabinet`, `.Support` or `.PmParty` assignment appears in the diff.
- **UI null safety.**
  - Every new row returns early for a non-US player or before any count (`TryLatest`).
  - `WorldClock.Reference.Seats` is always set when `TryReference` succeeds.

## Files reviewed
- `Assets/Scripts/Elections/UsCongressElection.cs` (whole).
- `Assets/Scripts/Simulation/SimulationManager.cs`: 495-545, 3835-3850, 4318-4330, 4925-5060, 5092-5105 and 5300-5340.
- `Assets/Scripts/Elections/UsPresidentialElection.cs`: 49-330, 400-450, 520-720.
- `Assets/Scripts/Elections/ElectoralCollege.cs` (diff); `Assets/Scripts/Elections/WorldClock.cs`: 115-310, 740-800.
- `Assets/Scripts/Elections/UsTermLimit.cs`: 130-340, 440-640.
- `Assets/Scripts/Elections/UsSenate.cs`: 20-240.
- `Assets/Scripts/Elections/StartBrief.cs` and `Assets/Scripts/Elections/NationalElection.cs` (diffs).
- `Assets/Scripts/Data/Country.cs` (diff); `Assets/Scripts/Persistence/SaveGameService.cs` (the version line and the restore order).
- `Assets/Scripts/Simulation/Congress.cs`: 580-625; `Assets/Scripts/Simulation/ParliamentSystem.cs`: 60-120; `Assets/Scripts/Simulation/ShadowBaseline.cs` (the fork); `Assets/Scripts/Simulation/SimulationRandom.cs`: 165-290.
- `Assets/Scripts/Elections/GovernmentRecord.cs` (`OfPresidency`).
- `Assets/Scripts/UI/GameController.ParliamentRows.cs`: 450-530 and 660-890; `Assets/Scripts/UI/GameController.PoliticsV35.cs`: 200-260; `Assets/Scripts/UI/GameController.cs`: 6585-6620.
- `Assets/Scripts/Elections/Generated/UsHouseDistricts.cs` (`HouseMaps`); `UsSenateRecord.cs` (OH, FL and CA rows; `SenateOn`); `UsSenateRaces.cs` (2016-2024 classes).
- `Assets/Editor/CongressElectionDiagnostic.cs`: 1-420.
- Diffs of `Assets/Editor/PresidencyDiagnostic.cs`, `UsPresidentialElectionDiagnostic.cs`, `UsPresidentialFactorCheck.cs`, `UsHouseCountCheck.cs`, `UsSenateRaceCheck.cs`, `UsMidtermCheck.cs`, `StartBriefDiagnostic.cs` and `CheckSuite.cs`.
- `Assets/Editor/ConstantProvenanceCheck.cs`, `CommentClaimCheck.cs` and `ReviewLedgerCheck.cs` (rules); `Tools/review_ledger.tsv` (tail and hash search).
- `docs/RULINGS_2026-10-18.md` (I1, I2, I3, I6); `docs/RULINGS_2026-10-19.md` (J4); `docs/specs/USA_STAGE_PLAN.md` (US-16, R-US18 *In play*, R-US19 (a) *in play*, R-US22); `docs/reference/US_ELECTIONS.md` (US-8 and US-16 blocks).
- `Reviews/2026-10-20_us16_congress_count.md`.
