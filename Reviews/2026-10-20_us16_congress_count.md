# US-16: the House and a Senate class elected on the same day - one code reading, and what was done about it (2026-10-20)

**Reviewed:** branch `wave/us16`, `7cfae549..bc1f8492` (the money path `Assets/Scripts/Simulation/SimulationManager.cs` at blob `776f5a06`), read-only, by one independent reader; the
compile gate run on the worktree (GATE OK). **After it:** `271092db` applies findings 1, 2, 5, 7, 8, 9 and 11 (it changes `SimulationManager.cs` in a doc comment only - blob
`716cfda7`); findings 3, 4 and 6 are the integrator's (WriteReadings runs, the CheckSuite registration, this row); finding 10 stands as a stated limit. What was done is told in the
record, not here.

---

**Verdict: not ready to integrate.** The runtime arithmetic is sound: the House matches the instrument line by line, and the Senate selection, the c_j fit and the 435/100 sums all hold. But two existing Editor checks will FAIL that the diff does not update, the US-8 card block is stale, and the new diagnostic is registered in no bar. The compile gate on the worktree prints GATE OK. CRLF is intact in all 21 changed files, and both new .meta GUIDs are unique.

**What I confirmed correct (traced, not assumed)**
- **House = `UsHouseCountCheck` kind "b".** These match the instrument exactly:
  - u: votes over non-U districts with votes, else R_B/m_B.
  - U_R/U_D: from the `Contains("U")` flag.
  - c_B = m_B−U_R−U_D and c_T = n_T−U_R−U_D, with c_B=0 giving P=0.
  - The one-seat rule: one district → its winner, else R_B*2>m_B.
  - The half rule: the state's R_B vs D_B, then the national base majority over all base states.
  - P's rounding and the min/max clamp.
  - n_T: the `house_maps` row of the year (2024 → 2024).
  - V(B): own-line, as `RecordV`.

  At the record's own swing it gives 222, which is what `CardC4B` parses from the card (`US_ELECTIONS.md:407-410`, the "(b) REP 222" row; the parse works on the actual text).
- **Senate seats up.** `ClassUp` gives 2024→1, 2028→3, 2032→2. `SeatsUp(2024)` = 33 Class I states + NE-2 (CA's unexpired race correctly dropped) = 34. `SenateTruth` on 2025-01-21 also gives 34 (no class-1 vacancy that day).
- **c_j fit.** It is identical to `UsSenateRaceCheck.CLines`: FinalR/FinalDc with final_none_top standing in, the same clamp, and p from `FromNationalShares(2020,2024, 2024 FEC)` = `Derive(2020,2024,0)`.
- **Senate sums.** On 2025-01-03 the record has 1 vacancy (WV-1, per `SenateOn` "C1Vacant 1"), which the game's seat fills, so 100. At the reach there are no vacancies, so (f) gets 100 in 2029, 2033 and 2037.
- **PresidencyDiagnostic (f).** The planted House gives REP 25 / DEM 23 / tied 2, so no choice. The Senate on 6 Jan is REP 53 (≥51), so Vance acts. (f') gives the Speaker as DEM (planted House 197/238). These pass.
- **Save path.** Newtonsoft (public fields, `R` round-trip on doubles, field-initialised lists) and the Adopt/ReferenceEquals pattern match CongressOfRecordDiagnostic's.
- **Project rules.** No `.Cabinet/.Support/.PmParty =` outside GovernmentRecord. Every backticked `Type.Member` added exists. No MetaTextCheck token in the new UI literals.

**Findings**

1. **H – `Assets/Editor/UsPresidentialElectionDiagnostic.cs:416` will fail.** It asserts `unlistedLine.EndsWith("nothing is decided")`. `ChooseByStates` now returns "...; nothing is decided (R-US22: the Senate chooses the Vice President, who acts from the oath)" (`UsPresidentialElection.cs:713`). Line 405's `Contains` still passes.
   - Fix: change the pin to `Contains("nothing is decided")`, or end the runtime string at "nothing is decided" and put the R-US22 words in `HoldHouseChoice`'s appended line.

2. **H – `Assets/Editor/UsPresidentialFactorCheck.cs:265-276` (`seatedLater`) will fail.** Through `PresidentialReferenceWorld.cs:85-112`, the reference world is the USA as player, epoch 4 Nov 2024, stepped by `AdvanceDay` to 3 Jan 2025 (`laterChamber`). It now holds its own Congress count on 5 Nov and seats the game's House on 3 Jan. `SeatsThen` therefore no longer equals `InitialSeats(USA, Usa2024)` (220/215); the instrument's (b) at a near-zero swing already gives 222.
   - Fix: expect `world.Country.UsCongressElections[0].HouseSeats(USA)` (and reword "the record by date, US-2"), or set the count aside as UsMidtermCheck does. Note that `SeatsThen` is only a copy of the seated House.

3. **H – the card's US-8 block (`docs/reference/US_ELECTIONS.md:852-868`) is stale.** `UsPresidentialElectionDiagnostic.cs:572` changed its "Not modelled, stated" card line, which is part of the block's digest. The on-disk block still has the old line, so `UsPresidentialElectionDiagnostic` (cheap bar and documents bar) fails "STALE" until `UsPresidentialElectionDiagnostic.WriteReadings` is run. Separately, the new US-16 block is a placeholder ("source-digest: none"), so `CongressElectionDiagnostic.Run` fails until its own `WriteReadings` runs; that one is by design, but the integrator must run both.

4. **M – `CongressElectionDiagnostic` is not registered in `Assets/Editor/CheckSuite.cs`.** It is in neither the cheap list (around 225-298) nor the documents list (around 776-786). Its header claims it is "pinned in the cheap bar and the documents bar", but no bar runs it.
   - Fix: add `("CongressElectionDiagnostic", CongressElectionDiagnostic.Run)` to both lists, as was done for UsPresidentialElectionDiagnostic.

5. **M – the Speaker's acting row reads "HOUSE".** `GameController.ParliamentRows.cs:709` shows `Surname(name)`. For the Speaker, `Executive` = "the Speaker of the House (REP)", and `WorldClock.SurnameOf` returns "House", so the row reads "THE PRESIDENT [REP] HOUSE ACTING". The same happens anywhere `SurnameOf(Government.Executive)` is shown (Desk, headlines).
   - Fix: special-case `Outcome == ActingOutcome && ActingAs == ActsAsSpeaker` to read "THE SPEAKER", or have `SurnameOf` keep "the Speaker of the House" whole, as it does for "'s candidate".

6. **M – review ledger.** `SimulationManager.cs` is a money-path file. `ReviewLedgerCheck` fails the bar until `Tools/review_ledger.tsv` has a `reviewed` row for its new hash, which this diff does not add. That row should be added at integration, citing this review's report.

7. **L – stale words after the `TryInOffice` change.**
   - `UsTermLimit.cs:265-266` still says an undecided contest "leaves him there".
   - The branch at `UsTermLimit.cs:312-316` (the sitting president of record past an undecided contest) can no longer match, because the acting government is not of record. So `TryVicePresidentOn` returns false under an acting president, and `UsSenate.VicePresidentParty` falls back to the acting party for tie votes. That is DECLARED-worthy, not wrong.
   - `SimulationManager.cs:4928` still says "latest decided contest".
   - `UsVetoDiagnostic.cs:450-453` still says the 119th of 3 Jan 2025 is the record's. It is not in Run2024's USA-player world from 12 Mar 2024, though that run only reports, so nothing fails.

8. **L – `ChooseVicePresident` line (`UsPresidentialElection.cs:315`).** It formats "a majority of the whole number (100 of 100) is 51", because `members` is passed twice. Drop "({2} of {3})" or pass the seated count.

9. **L – "UNTIL THE HOUSE CHOOSES" (`ParliamentRows.cs:764`).** The model DECLARES that the House votes once, so the acting president serves to the next oath. Either word the slip that way or note it as a deviation from I6's "until the House chooses".

10. **L – `WorldClock.cs:102-103`.** `NextOfRecord` drops the House-of-record line before 5 Nov 2024 because a game count is expected. If that count fails (no prediction), the record's 119th is still seated on 3 Jan but was never announced.

11. **L – checks that cannot fail, and card wording.**
    - `CongressElectionDiagnostic.cs:372`: `same == 1000` is always true (`TieBreak` is pure).
    - The card's "{7} races" prints 34 (seats with c_j≠0), while R-US19's instrument line says 35 fitted races (CA's unexpired race fitted, not seated). Say "seats" or explain the difference.

12. **Checked, no defect found.**
    - Polish, Swedish and AI-USA games, and worlds with no player: every new hook returns early.
    - The preview clone shares the list read-only; ActingGovernmentOf runs only on the real day loop.
    - The other affected Editor checks were each updated correctly: StartBriefDiagnostic, CongressOfRecordDiagnostic, SenateChamberDiagnostic, UsCongressDiagnostic, PresidencyDiagnostic (b)–(e), UsMidtermCheck, and the TermLimit/UsOwnNominee 2028 worlds.
    - Not checked individually: the generic economy diagnostics, which step from default epochs or for a few turns only.

**Files read**
- Diffs of all 21 changed files.
- `UsCongressElection.cs`, `CongressElectionDiagnostic.cs` and `UsHouseCountCheck.cs` (to line 400, plus 570-582 and 766) in full.
- `UsSenateRaceCheck.cs` (188-245, 465-476, 660-720).
- `UsSenateRaces.cs` (header, 2024 rows, row counts); `UsSenateRecord.cs` (SenateOn rows); `UsHouseDistricts.cs` (2024 delegations computed).
- `ElectoralCollege.cs` (1-30, 120-240); `UsPresidentialElection.cs` (130-330, 340-540, 600-723); `UsSenate.cs` (40-240); `UsTermLimit.cs` (130-210, 255-320); `WorldClock.cs` (100-260, 290-310, 740-800).
- `SimulationManager.cs` (500-545, 4925-5060, 5290-5340, 6805-6815); `ParliamentSystem.cs` (106-115); `GovernmentRecord.cs` (370-393); `EconomicVote.cs` (42, 61-77); `SimulationRandom.cs` (269-275).
- `SaveGameService.cs` (100-235); `GameController.cs` (6560-6620); `GameController.ParliamentRows.cs` (640-790 plus the new row); `GameController.PoliticsV35.cs` (200-275).
- `PresidencyDiagnostic.cs` (1-660); `UsPresidentialElectionDiagnostic.cs` (236-470, 560-600); `UsPresidentialFactorCheck.cs` (200-290, 420-440); `PresidentialReferenceWorld.cs` (80-140); `CongressOfRecordDiagnostic.cs` (headers and pins); `SenateChamberDiagnostic.cs` (grep); `UsVetoDiagnostic.cs` (176-215, 440-520); `PlayerRoleDiagnostic.cs` (258-300); `ConfidenceDiagnostic.cs` (USA world); `StartBriefDiagnostic.cs` (60-115); `PreviewParityDiagnostic.cs` (280-395).
- `MetaTextCheck.cs` (60-110); `CommentClaimCheck.cs` (1-80, 183-188); `ReviewLedgerCheck.cs` (header); `CheckSuite.cs` (grep plus 255-275); `Tools/cgate/gate.sh`; `Tools/review_ledger.tsv` (tail).
- `docs/reference/US_ELECTIONS.md` (GENERATED markers, 407-421, 519, 842-890); `docs/specs/USA_STAGE_PLAN.md` (US-16 entry, R-US15 line); `docs/RULINGS_2026-10-18.md` (I6); `docs/RULINGS_2026-10-19.md` (J3, J4).
