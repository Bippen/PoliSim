# SEAT-1 review: Italy's XVIII Camera and XIX Senato by their groups, AVS at SI's and EV's mean, France's XVIe by nuance

**Reviewed:** branch `wave/seat1` (worktree `G:/UNITY/Projects/PoliSim-wt/seat1`), commit `32213a4f`, base `b06ab5b5`. Read-only and static: I ran no Unity. Every figure below was re-derived with perl from the stored files or read off the source at the line given.

**Files reviewed:** `CLAUDE.md` (main repo), `docs/RULINGS_2026-10-19.md` (J2), `COMPLETED.md` §869, `POLISIM_FEATURE_LIST.md` (row 8, SEAT-1), `Assets/Scripts/Data/PartySystem.cs`, `Assets/Scripts/Elections/WorldClock.cs`, `Assets/Scripts/Elections/StartBrief.cs`, `Assets/Scripts/Elections/CoalitionFormation.cs`, `Assets/Scripts/Elections/GovernmentFormation.cs`, `Assets/Scripts/Elections/SeatedGovernment.cs`, `Assets/Scripts/Elections/NationalElection.cs`, `Assets/Scripts/Data/PartyFamilies.cs`, `Assets/Scripts/Data/CreatedParty.cs`, `Assets/Scripts/Data/Country.cs`, `Assets/Scripts/Simulation/ParliamentSystem.cs`, `Assets/Scripts/Simulation/SimulationManager.cs` (AI motion, majority, AI budget note), `Assets/Scripts/Simulation/SimulationManager.AiVoteMemo.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, `Assets/Scripts/UI/CountrySelectorScreen.cs`, `Assets/Scripts/UI/HemicycleRenderer.cs`, `Assets/Scripts/UI/GameController.cs`, `Assets/Scripts/UI/GameController.PoliticsV35.cs`, `Assets/Scripts/Testing/UiScreenshotDriver.cs` (the chamber guard), `Assets/Editor/ItalySeatingDiagnostic.cs`, `Assets/Editor/AuditInvariants.cs`, `Assets/Editor/D18MarkAssignment.cs`, `Assets/Editor/WorldClockDiagnostic.cs`, `Assets/Editor/FormationSweepDiagnostic.cs`, `Assets/Editor/CheckSuite.cs`, `Assets/Editor/UnwiredSubsystemCheck.cs`, `Assets/Editor/ConstantProvenanceCheck.cs`, `Assets/Editor/TrajectoryBaselineDump.cs`, `Assets/Editor/StartBriefDiagnostic.cs`, `Assets/Editor/PlayerRoleDiagnostic.cs`, `Assets/Editor/VoteShareBacktest.cs`, `Assets/Editor/ReBacktest.cs`, `Assets/Editor/GateReRun.cs`, `ElectionsData/italy/records_by_date.md` (the SEAT-1 section), `ElectionsData/italy/raw/reseat_2022/dati_camera_leg18_adesioni_gruppi.csv`, `ElectionsData/italy/raw/camera2018/*.html`, `ElectionsData/france/raw/legislatives2022/wayback_interieur_FE_20220705224641.html.gz`, `ElectionsData/positions/raw/CHES_2024_final_v2.csv`.

## Findings

### H1 - FormationSweepDiagnostic now silently drops Italy and France, and its pinned digest has to move
`Assets/Editor/FormationSweepDiagnostic.cs:100` skips any country with `parties.Count > 16`. Before this commit no roster went over 16. Now Italy has 19 (14 + LeU, MinLing, CP, NcI, Misto) and France has 17 (15 + NUP, DSV), so the "seated" sweep for both countries (four `== Italy|France | seated` blocks of the 250 in today's `formation_sweep.txt`) is skipped. The text loses those blocks, so the digest cannot equal `PinnedDigest` (`:129`, which this commit leaves alone), and the check fails with `CheckExit.Finish(1)` (`:119-123`). It is in the bar (`CheckSuite.cs:209`).
- **Failure scenario:** the simulation bar on this tree goes red on `FormationSweepDiagnostic`.
- **The worse fix to avoid:** re-pinning as things stand would lock in a sweep that no longer covers the two chambers this item changes.
- **Fix:** raise the cap to 20, or better count only seated parties after M1's fix. Then re-pin with the text diff shown. Italy's and France's blocks will show only longer power arrays carrying zeros for the appended units, with the same masks, governments and viable lists. Keep the old text beside the new, as §646 requires.

### H2 - ItalySeatingDiagnostic is in no bar, so the hand-typed figures are held by nothing
`grep` finds no `ItalySeatingDiagnostic` in `CheckSuite.cs` or `Tools/`. Two comments claim enforcement:
- `PartySystem.cs:498`: "`ItalySeatingDiagnostic` holds each to its CHES row on every field".
- `PartySystem.cs:1054`: "re-derived ... by `ItalySeatingDiagnostic`, which fails while one of them differs".

Neither holds in the bar. The CHES rows are typed (`:502-503`), and so are the 630/200/577 tables. The claim convention allows a typed figure only with a check holding it to its file.
- **Failure scenario:** someone edits SI's `galtan`, or one Camera seat moves, and every bar stays green.
- **Fix:** register it in `CheckSuite`, in the simulation tier, since it builds a world on Italy's start.
- **Verified by hand:** both CHES rows match the file on all 14 fields at 2 dp (SI `party_id` 838, EV 850; EV `nationalism` .875 rounds to 0.88 under ToEven). The Camera table, the Senato sums and the FE table also match (see Checked and clean).

### M1 - Performance: the formation enumerates subsets of seatless parties, and Italy is one party short of the hard throw
`CoalitionMath.NegotiatingPower` (`CoalitionFormation.cs:189-209`) does about n · 2^(n−1) · n bit-tests, because `Seats()` is O(n):

| n | bit-tests |
|---|---|
| 14 | 1.6 M |
| 15 | 3.7 M |
| 17 | 18.9 M |
| 19 | 94.6 M |

That is roughly 59x for Italy and 5x for France, and `Prepare` and `TryAsResult` (`SeatedGovernment.cs:123`) call it every time. Pass 1 (`:432`) also walks all 2^n masks: 524k for Italy, against 16k before. It allocates `double[2^n]` (4 MB) plus `bool[2^n]` per `Prepare`, and `FormationSweep` calls `Prepare` twice. This fits the measured 101 → ~600 ms. What made the jump is the zero-seat units the commit added.

Two further costs:
- `CreatedParties.Extend` adds the player's party, so an Italian game with one created party has n = 20: about 4x more again, an estimated ~2 s per formation.
- `NegotiatingPower` throws at n > 20 (`:192`), so one more Italian roster unit makes every Italian formation throw.

**Verdict:** a 600 ms main-thread stall on every world build and picker open is a defect, not a cost to accept. The project cut startup from 20 to 3 s, and this adds back a visible hitch. Its growth with the roster also sets up a crash.

**Least invasive fix, provably byte-identical:**
1. Compute the Banzhaf sweep over the seated parties only, and give seatless parties 0. A zero-seat party is never pivotal. For every other party it exactly doubles both the pivot count and the total, so each share is (a·2^k)/(b·2^k) = a/b. Scaling by a power of two is exact in IEEE doubles, so `Power` is bit-identical.
2. In `Prepare`, walk the submasks of `seated = all & ~SeatlessMask` in ascending order (`c = (c - seated) & seated`) instead of `1..all` with `continue`. The masks skipped are exactly the ones the `(cabinet & seatless) != 0` test already skips. The order of `Admissible` is unchanged, which matters because the later `List.Sort` is unstable.

**Can this change a formation result?** No. A seatless party:
- is never in a cabinet (§683; `TryFindInternalRedLine` reads members only);
- is never a supporter (`SupportersOf`, `:787`);
- adds 0 seats to `opposeMask`;
- has `bestOwn` = −∞;
- is in no `Joint` group.

Its compatibility and red lines therefore cannot move a seated outcome.

With the fix, Italy's start (12 of 19 parties seated) costs 2^12, below the base. The 2022 table seats 14 of 19 and costs the base's 2^14. Prove it with `FormationSweepDiagnostic` unchanged in text (after H1) and the trajectory digest unchanged.

### M2 - `DeclaredSize` is computed from the very count it is meant to check, which weakens the film's chamber guard
`PartySystem.cs:1143-1150` returns `seated` whenever `seated` equals the size of any sourced chamber of record for the country. `HemicycleRenderer` (`:159`, `:504`) feeds it the drawn total, and `UiScreenshotDriver.cs:3128` fails a film when `chamber != declared`.
- **Failure scenario:** a German game after its 2025 count wrongly seats 736, or an Italian path wrongly seats 630 past the XIX. `DeclaredSize` returns 736 or 630, and the guard passes where it failed before.
- **Fix:** key it on the vintage actually seated: `ChamberSizeAt(id, WorldClock.SeatedVintage(id, date))` while the start's chamber stands, `ChamberSeats` once the game has counted. Do not key it on the count.
- **The German side effect** (20th Bundestag declaring 736, so the Politics slip no longer says "DECLARES 630"): correct as a fact. No German figure or check reads it besides these two UI sites and the film guard.

### M3 - The keying rule is narrower than J2's words "seat each component as its list"
J2 seats each named component "as its list". The implementer's DECLARED rule is "a component bearing a 2022 list's name sits as that list". That is an added reading.

The case where the two differ: **Minoranze linguistiche** (Gebhard, Plangger, Schullian, Rossini; checked on the stored component page) is the SVP-PATT deputies. Its list is SVP-PATT, and the roster holds `SVP` ("Südtiroler Volkspartei - PATT", with a mark). The commit seats it instead as a new unit with no mark (`PartySystem.cs:549`).

The rule also carries over without comment that MAIE-Sogno Italia's six include five deputies elected on the M5S list. Name-keying seats them as MAIE, which J2's words allow but do not settle.

**Fix:** put the reading to Elias (ruling-first), recommending MinLing → SVP. Record NcI → NM (NcI is a member of the 2022 Noi Moderati joint list) as an open lineage question, not decided by name.

### M4 - An Italian game now plays its whole run on the 630-seat XVIII, and the audit was loosened to allow it
No Italian election is held (`NationalElection.cs` `NotHeldReason(Italy)`), and nothing re-seats Italy by date (`WorldClock.RecordSeatsChamber` is the USA's alone). So from 13 Oct 2022 the game sits on a chamber that `WorldClock.ChamberAt` no longer names (the XIX, 400 seats, which matched the old stand-in).

`AuditInvariants.cs:137-140` changes the SEATS invariant to read the start's chamber, precisely so that this divergence passes.

This is J2 applied together with PS-7-interim, not a code fault. But it is a player-visible change, and the record does not state it:
- M5S, with 222 seats, is the largest party for the whole run;
- the formation's stand-in for Draghi runs on 19 parties;
- "the snap election of 25 Sep 2022" is never held.

**Fix:** add one line to §869 or the PS-7 row saying the XVIII stands past 13 Oct 2022 until PS-7, and tell Elias.

### M5 - "Seat both chambers": the Senato is data only, and its Misto components were not read on the page that names them
J2 and the row's done-when say "both chambers seated". `ItalySenato2022` and `SecondChamberGroupsAt` (`PartySystem.cs:1101`, `:1115`) are read only by the Editor diagnostic, and nothing seats them.
- `UnwiredSubsystemCheck` judges by file, and `PartySystem.cs` is wired, so it does not flag them. `DeadStateCheck` sees private members only. The bar is silent either way.
- "The page names no component of the Misto group" is asserted from the groups page alone (`namesComponents` = the substring "omponent"). For the Camera, the components came from the Misto group's own page; the Senato's Misto page is behind the WAF (BILLED in the record).

**Fix:** state on the SEAT-1 row that the Senato is filed for PS-7 and the done-when is partly met. Bill the Senato's Misto components as unread, rather than asserting there are none.

Separately, for Elias: J2 pairs the XVIII Camera (2018) with the XIX Senato (2022). No start falls inside both, so the ruling's pairing is worth a sentence back to him.

### M6 - No bar or trajectory evidence is reachable for this branch
The worktree's `../PoliSim-captures` holds only `textcheck`, and the main captures hold nothing tagged seat1. Inertness is "proved by the dump diff ... never by reading the code" (CLAUDE.md), so the dump owes a run.

My static reading says the Sweden-start dump should be byte-inert:
- The dump has no player country, and every national election and AI motion is for the player only (`SimulationManager.cs:4347`).
- Italy's 2026 government is installed (Meloni), so no Italian formation runs.
- France's formation (Lecornu, provisional) runs on n = 17, but the two units are appended at the end, seatless and invariant (M1's argument).
- New roster units are appended at the end, so no existing index moves.
- `ParliamentSystem`'s random stream is declared and never drawn in roster loops.
- AI budgets are a no-op (`SimulationManager.cs:1077`).
- `MeasuredSeatShare`, which would give 595/400 at Italy's start, has no caller.

**Fix:** run the family dump and `traj_diff.pl` before merging. Expect 0 movers.

### L1 - AVS's runtime point does not match the backtests' typed point
`EqualWeightList` averages the 2-dp-rounded member rows: lrecon (1.38 + 2.21)/2 = 1.795. The mean of the raw CHES values rounds to 1.80. `VoteShareBacktest.cs:166`, `ReBacktest.cs:316` and `GateReRun.cs:323` type AVS at (1.80, 1.70), and Italy's electorate (`PartySystem.cs` `TryElectorate`) was fitted on that typed point. A 0.005 gap is small, but it is two transcriptions of one figure held by nothing. **Fix:** the backtests read `RealRoster(Italy)`'s AVS, or the gap is DECLARED.

### L2 - Changing AVS from NaN to a position moves one path, the created-party family
`PartyFamilies.OfCreated` (`PartyFamilies.cs:51-58`) takes the nearest positioned real party. A created Italian party near (1.8, 1.7) used to land nearest PD (Socialist) and now lands nearest AVS (no family, its own nest). This matches the TD precedent, but it is a behaviour change, and `EntrantLayerDiagnostic` should be confirmed green.

Nothing else on the play path read AVS's NaN:
- Italy holds no election.
- The compass returns null for Italy, which has no history.
- AVS holds 0 seats in the XVIII, so it changes no formation there.

### L3 - Wording and data
- The brief says "12 parties sit": the 1-seat Misto remainder is counted as a party, and it is a group, not a list.
- The D18 comment "raised it 3 -> 10 with its seven rows" (`D18MarkAssignment.cs:170`) and the reason strings' seat counts are counts in source (CommentClaimCheck's class; the Poland rows are the precedent).

### L4 - The Canvas caption owes a real film (§578)
"THE CAMERA DEI DEPUTATI OF 4 MAR 2018 · 630 SEATS · BY ITS GROUPS OF 20 APR 2018" runs about 80 characters at 12 pt, a little shorter than the US card's House-plus-Senate line, which is filmed at 1280. It probably fits, but it is unproven until the real 1280 and 2560 films reach Italy's card.

### L5 - France at the other starts
At Italy's, Poland's and the US starts, France's government is the formation's stand-in (Borne and Attal are GAP G4, provisional). The implementer's ENS+RN+LR result appears on base and branch alike, and it is not the record's: Borne governed as an Ensemble minority. That predates SEAT-1.
- AI budgets are a no-op, so France's cabinet moves no French fiscal figure, and through it nothing in the euro zone or the US checks.
- No Editor check pins France's 2022 seats or its government at those starts. Only `PlayerRoleDiagnostic` checks that the government is provisional.

## Checked and clean
- **J2's day:** 20 Apr 2018 is the day MAIE-SI formed. The table is identical from 20 Apr to 7 May, and differs on 19 Apr (no MAIE component) and on 8 May. Perl over the CSV gives 222/125/111/105/32/14, Misto 21, total 630 on 20 Apr; 27 Mar gives FI 104 and Misto 36; 8 May gives a total of 629 after Fedriga left. Nothing is forced to make 630.
- **Components on the day, from the stored pages:**

  | component | seats |
  |---|---|
  | +Europa-CD | 3 |
  | Civica Popolare | 4 |
  | MAIE-SI | 6 |
  | Minoranze linguistiche | 4 |
  | NcI | 3 |
  | in no component (Sangregorio) | 1 |

  These account for all 21 Misto members.
- **France:** the stored gzip's nuance table sums to exactly 577 and matches `FranceAssembly2022` key for key.
- **Senato:** the elected senators sum to 200.
- **AVS:** the mean is taken over all 14 fields `JointList` carries, and every field the positions use is covered.
- **Save format 55:** both load paths refuse `version != CurrentSaveVersion` (`SaveGameService.cs:219`, `:410`). Whether the play saves were re-staged could not be checked from here.
- **ChamberVerdictCacheCheck:** no `.Cabinet`, `.Support` or `PmParty` assignment in the diff.
- **MetaTextCheck:** no `{0}'s` on a value.
- **ConstantProvenanceCheck:** it scans `Assets/Scripts/Simulation` only, and nothing new lands there.
- **Majorities** are computed from seat sums (`CoalitionMath.Majority`, `MajorityOf`), not from `ChamberSeats`, so the 630-seat chamber is handled correctly.
- **The epoch:** the diagnostic runs under `EpochScope`.

## Verdict
**Not ready to merge.**
- H1 alone turns the simulation bar red.
- H2 leaves every typed SEAT-1 figure held by no bar.

The data itself is right: every seat table and both CHES rows match the stored files.

To merge:
1. Register `ItalySeatingDiagnostic` in the bar.
2. Apply M1's seated-only enumeration, which is byte-identical and removes the 600 ms stall and the throw one party away.
3. Fix the sweep cap and re-pin it with the text diff.
4. Key `DeclaredSize` on the seated vintage.
5. Run the dump and show 0 movers.

Then put M3, M4 and M5 to Elias as readings of J2.
