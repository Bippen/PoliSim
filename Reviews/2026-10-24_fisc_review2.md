# Review 2: `wave/fisc`, the delta since the first review (K98, K100, K101 and the fix round), 2026-10-24

**Branch:** `wave/fisc` in `G:/UNITY/Projects/PoliSim-wt/fisc`. The delta is `549810a1..e02cc682`: K98 `e2cc078c`, the prereg file `e8769b4b` and the fix round `e02cc682`.
**Reviewer:** an independent reviewer. It edited no code, ran no Unity and pushed nothing. The probes were perl and dotnet only, in the session scratchpad (`fisc_review2/probe.pl`), and they re-implement `SeedPreRun` and `Judge` from the CSV.
**Files reviewed:** `Assets/Scripts/Elections/FiscalVote.cs`, `Assets/Scripts/Data/FiscalVoteBook.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/Elections/EconomicVote.cs`, `ElectionsData/macro/fiscal_balance.csv`, `ElectionsData/macro/fiscal_balance.md`, `Tools/fiscal_balance_prep.pl` (not changed in the delta), `Assets/Scripts/Persistence/SaveGameService.cs`; also `Assets/Editor/FiscalVoteDiagnostic.cs`, `Assets/Editor/ApprovalVoteDiagnostic.cs`, `Assets/Editor/PresidentialReferenceWorld.cs`, `Assets/Editor/CheckSuite.cs`, `Assets/Editor/TrajectoryBaselineDump.cs`, `Assets/Scripts/Elections/WorldClock.cs` (the governments of record, `StartDate`), `Assets/Scripts/Elections/CampaignClock.cs`, `Tools/audit/aloop_record_check.pl`, `Tools/audit/aloop_fisc.plan`, `Reviews/2026-10-24_fisc_prereg.md`, `docs/RULINGS_2026-10-23.md` (K60-K64, K98-K101).

## Verdict: NOT READY

The branch is not ready, for three reasons:
- **H1:** the USA's E1 reference world reads the same source year as both BAL0 and BAL-1.
- **M1:** a campaign's opening judges BAL0 by the opening day's own year, which breaks K100 for Germany.
- **M2:** the first review's M1 is still open. `FiscalBalanceRecordCheck` is never run.

All three are small fixes. M3 and M4 are rulings to put to Elias, with the figures given below.

## What was verified

- **Gate and text checks.** `gate.sh`: `GATE OK`. `Tools/textcheck` on the worktree: 4 of 5 clean. The one failure is UpstreamCheck, and only because the worktree branch has no upstream yet (main is 5 of 5).
- **Line endings.** All 11 changed files are CRLF in the worktree (the `.pl` is LF) and LF in their blobs. No doubled CR anywhere (counted with perl).
- **Comment rules.** No `<see cref` was added. MetaTextCheck is not touched: `FiscalVoteBook.cs` adds two public fields and comments, and no string literal was added under UI or Data. No new file, so UnwiredSubsystemCheck is unaffected. `fiscal_balance.md` keeps its [SOURCED] header.
- **K100's pre-run years** (FiscalVote.cs:106-125). Only source years whose `closes <= start` are read, newest first, and a gap ends the run. The probe reproduces each start:
  - Germany (6 Nov 2024) reads 2020-2023.
  - Sweden (18 Jan 2026) reads 2022-2025.
  - The USA's played start (`PreCampaignStart`, 12 Mar 2024) reads FY2020-FY2023.
  - The USA's eve world (4 Nov 2024) reads FY2021-FY2024.
  - The default epoch (1 Oct 2026) reads 2022-2025, and FY2022-FY2025 for the USA. FY2026 would qualify (it closes 1 Oct), but the record does not hold it.
- **K100's BAL0** (`SourcedYear`, :204-216). It matches the record's year number to `judgedOn.Year`, so for the USA that is the federal FY of that number.
  - Sweden's 2026 count reads no BAL0 and no BALCH_ey, because 2026 is unpublished. This is correct.
  - Germany's count (judged 2025-02-22) reads 2025 at −2.9.
  - Poland's presidential eve reads 2025 at −7.2.
- **The K98 switch.** `BeforeFirstClose` (:217) stays true until `CloseYear` (:176-184) appends the first game year with `SourceYear` 0. From then on `OpenRatio` is read. The checker's K98 lines (aloop_record_check.pl:77-78) agree.
- **K101's walk** (`PartyTenureOfRecord`, :147-160). `HeadParty` matches the stored `PmParty` keys for every country it touches:
  - Sweden: "Ulf Kristersson (M), caretaker" walks back to 2022-10-18.
  - Germany: "Olaf Scholz (SPD), minority" carries SPD back to 2021-12-08, so the minority government of 2024-11-07 does not restart the tenure.
  - Poland: KO from 2023-12-13; PiS from 2019-11-15, the record's first row.
  - The USA: "(DEM), president" and "(REP), president". Trump's oath on 2025-01-20 restarts the tenure through `NoteDay`, where `TookOffice` reads the of-record day.
  - Italy's Draghi row and France's rows name no party. `GovernmentRecord.AtStart` (:412) falls back to the largest cabinet party, so the walk returns null and `TookOffice` decides, as before.
- **K101 in play** (`NoteDay`, :191-206). The tenure restarts only where `PmParty` changes:
  - A junior partner entering or leaving, a caretaker or a re-formation under the same PM party continues it.
  - A government with no `PmParty` leaves the book as it is.
  - Sweden in play keeps 2022-10-18 if M re-forms. Its game years close on 2027-01-18, 2028-01-18, 2029-01-17 and 2030-01-17, so the 2030 count reads the four-year form. If S forms on about 2026-10-20, the 2030 count reads 3 years and the fallback, as K99 requires.
- **The save and the clone.** Newtonsoft serialises public fields, so `TenureParty` and `TenureFrom` ride the save. `Copy()` carries both.
- **The sentinel, `form`.** Only `FiscalVote`, the clone (SimulationManager.cs:7358) and the save touch `Country.FiscalVoteRecord`. `TrajectoryBaselineDump` seats no player, and `NoteDay` draws nothing random. The books are therefore inert in the dump as read. The dump diff itself is still owed.
- **The first review's L items.**
  - **L1 is fixed.** ApprovalVoteDiagnostic (2f) pins `b == 0.16 == ApprovalVote.CoefficientFor`, and the checker's line :61 pins 0.16.
  - **L2 is fixed,** but see L2 below.
  - **L5 is fixed.** `f5_it` was removed, and `#` comments are the plan's own syntax.
  - **L6 is fixed.** `NoteDay` now seeds from `today`.

## Findings

### H

**H1. In the USA's E1 reference world, BAL0 and BAL-1 are the same source year (FY2024).**
- **Where:** `SeedPreRun` (FiscalVote.cs:119-123) together with `Judge` (:286, :291).
- **What happens:**
  - `PresidentialReferenceWorld.Hold` opens the epoch on 4 Nov 2024. FY2024 closed on 1 Oct, so K100 seeds it as the newest pre-run year (BAL-1).
  - The count on 5 Nov 2024 then reads BAL0 as `SourcedYear(USA, 2024)`, which is FY2024 again.
  - So BALCH_ey ≡ 0, and the term is FY2024 − FY2023 when it should be FY2023 − FY2022.
- **What the probe gives** (the values in parentheses are BAL-1 − BAL-2 and BAL0 − BAL-1, in pp of GDP; the last figure is the resulting shift in pp of share):

  | World | Term | Election year | Shift |
  |---|---|---|---|
  | The eve world (4 Nov 2024) | −0.10 | +0.00 | **−0.050 pp** |
  | The played US start (12 Mar 2024), same election | −0.80 | −0.10 | **−0.495 pp** |

- **Failure scenario:** E1's US factors are refit on the eve world (the first review's M4, still owed). The refit would absorb a fiscal term about 0.45 pp smaller than the one the played game applies to the same election.
  - The diagnostic's own invariant would fail on this world. (8c) asserts `r.Source1 < judged.Year` (FiscalVoteDiagnostic.cs:426), but section 8 opens only the `ApplyStart` starts, never the eve world.
  - The same coincidence hits any US world opened between 1 Oct and polling day of an election year.
- **Fix:** while `BeforeFirstClose`, read BAL-1..BAL-4 only from sourced years older than BAL0's year (skip any closed year with `SourceYear >= Source0`). Add the eve world to (8c).

### M

**M1. A campaign's opening reads BAL0 from the opening day's calendar year, not the election's.**
- **Where:** SimulationManager.cs:5946 calls `RecordPartsOverTerm(recordCountry, CurrentDate)`. That reaches EconomicVote.cs:204, then `ShiftOverTerm` (:297) with `judgedOn = asOf`, then :286.
- **Germany's case:**
  - The opening falls on 2024-12-29, 8 weeks before 2025-02-23 (`CampaignClock.DefaultCampaignWeeks`). It reads BAL0 = **2024 (−2.7)**, a year that had not closed by the start.
  - This breaks K100 ("the first count's current year is the election's calendar year"). It also contradicts `fiscal_balance.md` and FiscalVote's reading (8) ("no count reads 2024").
  - The probe gives the opening's term as −0.634 pp, against the count's −0.827 pp. The opening's shift drives the campaign's polls for eight weeks.
- **Affected runs:** the registered runs `q13_de_deficit`, `q13_de_surplus` and `f3_de_spd` will all print it.
- **Also:** `Judge(country)` defaults `judgedOn` to `book.OpenedOn` (:254, :286), which is the start's year (2024 for Germany). Only the diagnostic calls it (:334).
- **Fix:** pass the election's date (`calendar.ElectionDate`, or `record.ElectionDate`) as the BAL0 day, separately from the responsibility vector's `asOf`.

**M2. The first review's M1 is still open.**
- `FiscalBalanceRecordCheck` is still absent from `Assets/Editor/CheckSuite.cs`. `FiscalVoteFitCheck` sits at :304, and no commit in the delta touches the file.
- **Failure scenario:** the April 2027 EDP re-fetch edits the CSV or the generated table without the prep, and no bar fails.
- **Fix:** one line in the cheap table.

**M3. Poland's tenure floor (2019-11-15) flips the sign of BALCH_term at Poland's own start.**
- **Where:** `PartyTenureOfRecord` stops at the record's first row (FiscalVote.cs:170).
- **What happens at the Sejm start** (about 2023-02-19; count on 2023-10-15; registered run `f4_pl_pis`):
  - It reads 2020-2022: three years, so [BD06]'s fallback −1.70, a shift of **−2.59 pp** for PiS.
  - Under K101's own rule, PiS governed from 2015-11-16 (Szydło, Morawiecki I). The whole of 2019 then counts, and the four-year form is **+1.25**, a shift of **−1.12 pp**.
- The floor is declared in reading (4), but its consequence is not stated.
- **Ask Elias:** source the two PiS rows of record (2015-11-16 to 2017-12-11, and 2017-12-11 to 2019-11-15), or accept the floor as declared.

**M4. Germany's BALCH_ey spans two years.**
- BAL0 is 2025 and BAL-1 is 2023. 2024 is read by no count: it is neither a pre-run year (it was open at the start, so K100 excludes it) nor the election's year.
- The first count's election-year change is therefore 2025 − 2023 = −0.70, against a one-year change of −0.20.
- After the first game year closes ([2024-11-06, 2025-11-06)), the series skips 2024 for good.
- K98 says the first count reads the source "for both years it compares". K100 forbids a pre-run year that was open at the start. Together they leave the two-year change.
- **Ask Elias:** may a year that closed after the start but before the count (DE 2024) stand as BAL-1? If not, declare the two-year change in reading (8b).

### L

**L1. The new fields' round trip is not asserted.**
- (6a)'s `SameBook` (FiscalVoteDiagnostic.cs:515-517) compares no `TenureParty` or `TenureFrom`, and neither does (6b).
- Format 61 is folded. A save cut by `0e1a9e6e` or `e2cc078c` loads with `TenureParty` null, falls back to `TookOffice`, and loses the walk. Only saves internal to the branch exist, so re-stage them.

**L2. The checker's K61 sign test can no longer fail on its own** (aloop_record_check.pl:103-104).
- `$z` is recomputed from the same printed BAL-i with positive betas, so "improves" implies z > 0 by construction.
- The game's sign is constrained only by |side − z| ≤ 0.01 pp and the index check (≤ 1e-4). That is adequate, but the prereg's K61 clause is falsified through those checks, not through this one. Say so.

**L3. The checker asserts nothing of K100 or K101.** The prereg's K100 and K101 clauses therefore have no falsifier in the registered run. Add three assertions, which would have caught H1 and M1:
- BAL0's source year equals the year of the count (or the election, on an opening line);
- BAL0's source year is newer than BAL-1's;
- a line reading 4 years prints the four-year form.

**L4. The default epoch now seeds 2022-2025.** The probe's day-one readings:

| Country | Day-one reading | Why |
|---|---|---|
| Poland | −0.398 pp | the fallback 2025 − 2024, under KO from 2023-12-13 |
| Italy | +0.149 pp | under Meloni |
| Sweden | exactly 0 | 2025 = 2024 = −1.6 |
| Germany | 0 | Merz from 2025-05-06: no whole year |
| The USA | 0 | see below |

- **The USA reads 0, not the non-zero the brief expected.** Trump's tenure runs from 2025-01-20, and FY2025 opened 2024-10-01. So the 2026 midterm reads nothing, and the 2028 count reads only game years.
- **Poland's 2027 Sejm count** reads 2025 and 2024 as BAL-2 and BAL-3.
- **Diagnostics that will move:** those that read `RecordOverTerm` on a default-epoch world for Poland or Italy. France reads every year where its government carries no head party, through `TookOffice`'s day; this was not measured.

**L5. K100's "no figure from after the election" is built as the ruling's own words.** The figure is the election year's full-year one, so for Germany (Feb) and Poland (May) it is a figure closed and published after polling day. Declare it as hindsight in `fiscal_balance.md`.

**L6. The prereg's K101 clause depends on the formation.** "The four-year form applies after a full term" is reachable in the Swedish 1760-day poles only if M re-forms in 2026. The run should print the 2026 formation's PM party beside the 2030 line, so the clause is read and not assumed.

## The prereg file against the code

- Its K60, K61 (as restated), K64 and K98 text matches the code and the checker.
- Its K100 text ("BAL0 is the source's figure for the election's calendar year") is contradicted by the opening lines (M1) and by the eve world (H1).
- Its K101 text matches the code, apart from M3's floor.
- Its pass criteria are falsifiable only as L2 and L3 describe. Commit it after H1 and M1 are fixed, with L3's assertions added.

## Owed before the bar

- Fix H1, M1 and M2. Put M3 and M4 to Elias.
- The E1 refits and the US and Polish reading blocks (the first review's M4) are still owed. They must follow the H1 fix.
- The no-policy dump diff against `form`, `FiscalVoteDiagnostic`, `aloop_fisc.plan` and `aloop_record_check.pl` have not been run.
