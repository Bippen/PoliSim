# Review: K31, K33, K53, K56, K58, K59 and save format 57 (wave/form)

**Branch:** `wave/form` in `G:/UNITY/Projects/PoliSim-wt/form`, base `140aab81`, HEAD `ace6a9e0` (9 commits)
**Reviewer:** independent, read-only. No Unity was run. The compile gate was run (`GATE OK`), one dotnet probe was run, two pages were fetched again with curl to compare bytes, and the pages under `ElectionsData/sweden/raw/declarations2026/` and `ElectionsData/sweden/2026/raw/declarations/` were read with perl (tags stripped).
**Files reviewed:** `Assets/Scripts/Simulation/FederalReserveSystem.cs`, `Assets/Scripts/Simulation/CentralBankTerms.cs`, `Assets/Scripts/Simulation/CabinetSystem.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/Simulation/SimulationManager.PlayerElection.cs`, `Assets/Scripts/Simulation/SimulationManager.Sejm.cs`, `Assets/Scripts/Elections/NationalElection.cs`, `Assets/Scripts/Elections/WorldClock.cs`, `Assets/Scripts/Data/PartySystem.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, `Assets/Editor/AverageCabinetDiagnostic.cs`, `Assets/Editor/AverageMinisterLeakMeasure.cs` (+ `.meta`), `Assets/Editor/CentralBankTermCheck.cs`, `Assets/Editor/ItalySeatingDiagnostic.cs`, `Assets/Editor/NoPolicyCentury.cs`, `Assets/Editor/SwedenBlocsDiagnostic.cs` (+ `.meta`), `Assets/Editor/WorldClockDiagnostic.cs`, `ElectionsData/france/records_by_date.md`, `ElectionsData/sweden/2026/coalition_declarations_2026.md`, `ElectionsData/sweden/raw/declarations2026/*` (7 HTML pages, `SHA256SUMS.txt`, `fetch_log.txt`). Also read, unchanged, as readers of what changed: `PublicationSystem.cs`, `EconomicVote.cs`, `SimulationManager.PlayerPresident.cs`, `GameController.cs` (the start and the cabinet option card), `CheckSuite.cs`, `SimulationRandom.cs`, `CountingRandom`, `ElectionsData/.gitattributes`.

## Verdict: NOT READY

There are two H findings. The first is about sourcing: K53 reads C as "neither side", and the register supports that reading by describing an SVT page in a way the page does not bear out. The second is a one-line fix: the two new diagnostics are not registered in `CheckSuite`, so no bar runs them. The M and L findings are smaller. Everything else checked holds (listed first).

## What was verified (and holds)

- **K56: no draw moves, and every AI pick has no lean.** The probe at `G:/Claude/tmp/claude/C--Users-elias/1fd816b7-8c12-47d8-ae29-269db5403b26/scratchpad/form_review/k56probe/` replicates `GenerateCandidates` at base and at HEAD. It also replicates `SimulationRandom.CentralBankShortlistOn` (the SplitMix key, seed 777) and the pool. It runs every bank (Fed 4 y, Riksbank 6 y, NBP 6 y, ECB 8 y) through every chained term end for 120 years, from six epochs.
  - 486 AI lists. Lists without a no-lean candidate: 0. AI nominees that lean: 0. Generator positions that differ after the draw: 0.
  - 346 of the 486 AI picks change, 187 by an appended candidate and 159 by a replaced one.
  - Typical first change: the Fed at epoch + 4 years (Anand +0.25 to Voss; from the Italy epoch, Delacroix -1.25 to Voss). That falls in turn 5, because the seeded heads' terms begin at the epoch (`WorldFactory.cs:1012-1016`). The "from turn 5" claim is consistent.
  - The player's counted stream (seed 777 + 2 x 7919, 1200 lists): the stream's state is equal after the draws at base and at HEAD.
  - `UsFedConsent.Vote` reads the country and the day, never the nominee, so the Senate's count does not move with the name.
- **K56 against the other K items in the no-policy run.**
  - `NoPolicyCentury` and the sentinel seat no party (`PlayerPartyAbbrev` stays null), so `ChairIsThePlayers` is false. K58 and K59 therefore seat and roll nothing there.
  - K53's tactical layer runs only on the player's staged campaign (`SimulationManager.PlayerElection.cs:122-131`). The shadow worlds swap in their own RNG streams (`ShadowBaseline` Capture/Reinstate).
  - On reading, K56 is the only mover of the Sweden/Germany/USA/France centuries. For Italy and Poland see L1. I could not see a family diff, so the claim is checked by reading the code, not by a dump.
- **K58: the means come from the table.** `PoolMeanOf` sums the `Attributes` table: 1390/18 = 77.22, 1430/18 = 79.44, 1075/18 = 59.72. The static initialisers run in textual order (table at :426, means at :475-481), and no earlier initialiser calls `AverageMinister`.
- **K58: what changes for the player's seated average cabinet.**
  - Leaks and resignations now roll under pressure at disloyalty 0.228 (`CabinetSystem.cs:663`). Before, the `disloyalty <= 0` short-circuit took no draw, so a played game's Cabinet stream now takes draws it did not take before. That is intended, and the format step covers it.
  - Knowledge 79.44 is at or above the floor of 70, so disclosure is unchanged.
  - An option's approval lands at x0.597 (see M2).
  - The dismissal cost does not change. `ReshuffleMinister` removes the minister before `PopularityFactor` reads the chair, so it reads the vacancy (x1) as before.
  - Nothing that used to test for 100 meaning neutral is broken: `NeutralAttribute` is gone from the code, and only `COMPLETED.md` and an old review mention it.
- **K59: the install sites.**
  - The search for `.Government =` assignments outside `Testing/` finds seven sites. Five are the manager's installs, and each is bracketed (`PlayerElection.cs:226`, `Sejm.cs:167`, `SimulationManager.cs:4465, 5442, 5469`).
  - The rest are the start's (`WorldFactory.cs:1111`, `GameController.cs:2205/2250/2254`, followed by `SeatAverageCabinet` at :2262) and a view (`CoalitionFormation.cs:359`).
  - A chair the player already held is in `before` and is skipped, so its minister or its vacancy is kept. `AverageCabinetDiagnostic` holds both cases and the install-site guard.
  - Loads keep the save's chairs. A fork seats its own copy.
- **K53: the pages.** `sha256sum -c` passes on all seven. Each page's blob is byte-identical to the working file. `ElectionsData/.gitattributes` `**/raw/**` sets `-text` (`git check-attr`: `text: unset`). A curl refetch of MP's 2026-08-24 page today is byte-identical (`2dd8d519...`). C's 2026-09-09 page differs only in a script nonce and stylesheet cache paths, with CRLF as served.
- **K53: every quoted line is on its page under its date.**
  - C 2026-09-07 (`NYHET 2026-09-07`): "Det finns de som påstår ... Det stämmer inte."
  - C 2026-09-09: "Det finns mer än två alternativ" and "Tidösamarbetet finns inte efter valet".
  - C 2026-09-12: "går till val på en regering i mitten av svensk politik, utan ytterkantspartierna".
  - MP: "Publicerad 2026-08-24 08:55:47"; the manifesto 2026-08-10, Helldén 2026-06-26 and Lind 2026-05-28. None of the four names a partner or a prime minister, so the GAP holds.
  - [MP-I4] (Nyheter24/TT, 2026-04-04T05:18) carries "den tilltänkta regeringspartnern är Socialdemokraterna" in the newsroom's voice, and K53 declares it as such.
- **K53: the layer reads the dated sides only on its polling day.** `on.HasValue ? SwedenBlocsOn(keys, on.Value) : SwedenBlocs2022(keys)`. The only live caller passes `on: CurrentDate`. An undated call, or 2022-09-11, reads 2022's table.
- **K31.** `IsPlayable` refuses `IsGroupRemainder(Italy, "Misto")`, which is SEAT-1's single test. The picker (`CountrySelectorScreen.cs:282`, `GameController.cs:2420`) is the only reader, and the brief and the seats still count the remainder.
- **K33.** The stored FE page (SHA256SUMS OK) reads `<div class="span12">12 et 19 juin 2022</div>`. The row is dated 2022-06-19 and both days are on its basis. `FormationDays(France)` is unchanged: Borne's row begins before the poll, and Attal's is more than 365 days after it.
- **Format 57.** `CurrentSaveVersion = 57`, and the loader refuses any version other than the current one (`SaveGameService.cs:219, 410`). No fixture pins 56.
- **Diagnostics that fail while their statement is false:**
  - `CentralBankTermCheck` (f): fails if any list lacks the no-lean candidate.
  - `WorldClockDiagnostic` K33: fails if the row is reverted to MinValue, if any chamber is undated, or if the digest fails.
  - `ItalySeatingDiagnostic` K31: fails if Misto is offered or anything else is left out.
  - `AverageCabinetDiagnostic`: means read off the drawn candidates; the K59 entry; the install-site guard by line.
- **Rules.** The gate is OK. The two new `.meta` files have unique guids. Working-tree CRLF is consistent with the base, and new text is in no MetaTextCheck surface. The new public statics are wired (`UnwiredSubsystemCheck` is unaffected).

## Findings

### H1: K53 "C neither side" rests on a misdescribed source

**Where:** `ElectionsData/sweden/2026/coalition_declarations_2026.md:869-872` and `Assets/Scripts/Elections/NationalElection.cs:355` (C's basis string).

**The problem:** The register says SVT's 2026-06-09 page [C-I11] carries the leader's words refusing Kristersson, "and DN [C-I2] the same day reports Andersson as C's mest sannolika partner ... a newspaper's report, which dates nothing (F2)".

**What the page shows:** SVT's own stored page (`2026/raw/declarations/svt_20260609_c_andersson_mest_sannolika.html`) carries the leader's words verbatim, at the party's press conference:
- "Centerpartiet har gett besked i regeringsfrågan. Svaret: S-ledaren Magdalena Andersson."
- "– Därför är Magdalena Andersson och Socialdemokraterna den mest sannolika samarbetspartnern för oss i Centerpartiet, säger Thand Ringqvist."

Under F2 (§780), that is a dated declaration on a broadcaster's own page. The register's own PM table (:564) already says "from 9 June 2026 it names Andersson as its most likely candidate". C's later pages declare "a government in the middle without the edge parties" and "more than two alternatives". They name no prime minister and do not withdraw Andersson. Read with SVT 2026-09-08 [C-I1] ("hellre extraval än V i regering"), they exclude V, not S.

**The honest reading:** On the binary side the table defines ("0 the side of S's candidacy"), the reading closest to the dated record is C = 0 from 2026-06-09 (conditional, without V). "Neither" may still be Elias's to choose, but it must be put to him with the SVT words quoted, not with DN standing in for them.

**Failure scenario:** The reading decides whether C survives. `SwedenBlocsDiagnostic` (d) shows that on a planted poll with C at 4.0 %, C is lent votes on 2022's sides and 0 pp on 2026's. In Sweden's 2026 count C polls near the 4 % threshold, so the game drops C from the Riksdag more often, on evidence the record contradicts.

**Fix:** Correct the register paragraph. Then either date C's side 0 from 2026-06-09 with [C-I11], or carry "neither" as an explicit ask with both readings in `docs/ASKS_FOR_ELIAS.md`.

### H2: the two new diagnostics run in no bar

**Where:** `Assets/Editor/CheckSuite.cs`, next to `:205`.

**The problem:** Neither `SwedenBlocsDiagnostic` nor `AverageMinisterLeakMeasure` is registered in `CheckSuite`. `git grep` finds no reference to either outside its own file and one comment. The suite does no discovery by reflection.

**Failure scenario:** K53's sources, timeline and ruled sides are held by nothing in the bar. A later edit to `SwedenSides`, a moved page or a broken SHA256SUMS in `raw/declarations2026/` stays green. The leak measure's "fails while no event rolls" never runs either. (The US-35 review rated the same class H.)

**Fix:** Add both to the table. The measure needs the no-policy century, so it belongs in the simulation tier.

### M1: K56 removes a drawn candidate from the player's list in a third of draws

**Where:** `FederalReserveSystem.cs:128-131`.

**The problem:** When the draw is a full hawk/moderate/dove list with Anand or Kade as the moderate, Voss replaces the moderate. In the probe's 1200 player lists, 401 lose a drawn candidate (Anand 183, Kade 218). The ruling says "carries a no-lean candidate". It does not say to remove the mild leans.

**Failure scenario:** A player holding the bank is offered fewer mild +0.25/-0.25 options than before. In a full list they now choose only between the strong leans and none.

**Why it matters:** `MaxCandidateCount` is a CONVENTION (a presentation choice), so a fourth slot was open. The [DECLARED] note covers only the draws, not this reading.

**Fix:** Ask Elias (replace, or allow four), or declare the reading as an ask.

### M2: the cabinet card shows an approval figure the average cabinet does not land

**Where:** `GameController.cs:8945`; `CabinetSystem.ApplyDecisionOption`.

**The problem:** The card's arrow shows `option.ApprovalEffect` unscaled. The average minister now lands it at x0.597.

**Failure scenario:** This mismatch already existed for named ministers. K58 makes it the default for every new game's cabinet: a card saying "Approval -1.5 pts" lands -0.90. Also, a vacant chair (x1) now lands an option's approval more strongly than the average minister, in both directions.

**Fix:** Either draw the scaled figure on the card or say so on it. Elias's call on the vacancy asymmetry.

### L1: K33 has a consequence nobody declared

**Where:** `PublicationSystem.cs:88`.

**The problem:** Before K33, `PreStartWindowOpens(France, start)` returned MinValue for any start before 2024-07-07, and `SeedPreStartRecord` seeded nothing. Now France's published unemployment and inflation are seeded from June 2022 in the Italy-start and Poland-Sejm-start worlds. `EconomicVote.TookOffice` and `VintageOfElection(France, ...)` also now see the 2022 day.

**Why it is low:** The only reader is a count (`EconomicVote`), and France holds none, so this is display and save content, not the trajectory.

**Fix:** State it in the K33 record, and confirm it with the family diff for the Italy and Poland starts.

### L2: K59 misses one way of entering government

**Where:** `SimulationManager.PlayerPresident.cs:54`.

**The problem:** When a Polish player-president's term ends, `PlayerHoldsPresidency` falls and the party's chairs become the player's, with no `Government =` assignment. Any chair the party gained during the presidency is vacant, and `SeatOnEntry` is not called. The install-site guard cannot see this, because it scans only assignments.

### L3: SwedenBlocsDiagnostic (d) matches source text

**Where:** `SwedenBlocsDiagnostic.cs:107`.

**The problem:** It checks that `SwedenBlocsOn(keys, on.Value)` appears in the source. It does not check that the live call passes a date. Dropping `on: CurrentDate` at `PlayerElection.cs:131` would put the game back on 2022's sides, and the check would stay green.

### L4: an F2 source for MP may exist

**Where:** `2026/raw/declarations/svt_hellden_karnkraft_regering.html`.

**The finding:** This SVT page carries Helldén's own words about sitting in a government with S ("Jag hoppas att Socialdemokraterna ändrar sin analys. Men det är Socialdemokraterna som s..."). It may be an F2-grade source for MP's side to stand on instead of the paraphrase. It is worth a line to Elias.

### L5: two small record gaps

- The format-57 comment (`SaveGameService.cs:104`) does not name K53. A v56 save cut inside Sweden's campaign would count C on the new sides; it is refused anyway.
- The "asked" items exist only in code comments and the register, not in `docs/ASKS_FOR_ELIAS.md`: `BlocOf` staying on 2022's table, and C's reading.

### Owed at integration (not findings)

The review-ledger row for the money paths (`Tools/review_row.ps1`), the simulation bar, and the BASELINE family `form` with its `traj_diff` read for every start.
