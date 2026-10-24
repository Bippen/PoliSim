# Review: `wave/fisc`, K60-K64 (A-LOOP's approval and fiscal vote terms), 2026-10-24

**Branch:** `wave/fisc` in `G:/UNITY/Projects/PoliSim-wt/fisc`, base `1768a303`, HEAD `0e1a9e6e` (one commit, 25 files).
**Reviewer:** an independent reviewer. It did not edit code, ran no Unity, and pushed nothing. Probes were perl and dotnet only.
**Files reviewed:** `Assets/Scripts/Elections/FiscalVote.cs`, `Assets/Scripts/Data/FiscalVoteBook.cs`, `Assets/Scripts/Elections/Generated/FiscalBalanceRecord.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `ElectionsData/macro/fiscal_balance.csv`, `Tools/fiscal_balance_prep.pl`; also `Assets/Scripts/Elections/Generated/IncumbentVoteSwing.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, `Assets/Editor/FiscalBalanceRecordCheck.cs`, `Assets/Editor/FiscalVoteDiagnostic.cs`, `Assets/Editor/FiscalVoteFitCheck.cs`, `Assets/Editor/ApprovalVoteDiagnostic.cs`, `Tools/aloop_sigma_fit.pl`, `Tools/audit/aloop_record_check.pl`, `Tools/audit/aloop_fisc.plan`, `ElectionsData/macro/fiscal_balance.md`, `ElectionsData/macro/raw/fiscal_balance/*`, `.gitattributes`.

## Verdict: NOT READY

One finding must be fixed before merge: the new provenance check is never run (M1). It is a one-line fix. Two other items fall due before the bar can be green. The E1 factor refits will fail the bar on the first run: this is the implementer's warning, and it is confirmed (M4). The other Ms are rulings to put to Elias, with the measurements given below. The arithmetic, the data and the save step are correct as far as they could be verified outside Unity.

## What was verified

1. **Raw files.** All four files match `SHA256SUMS.txt` (`sha256sum -c`: OK), both in the working tree and in the committed blobs (`git cat-file`).
   - `.gitattributes` marks `raw/fiscal_balance/**` and the CSV `-text`.
   - The CSV is LF in both the blob and the worktree, and its digest equals the generated header's.
   - The generated `.cs` is CRLF in the worktree and LF in the blob, with no doubled CR anywhere. The check hashes the CSV after CRLF->LF, not the `.cs`.
   - `perl Tools/fiscal_balance_prep.pl --dry` reproduces the CSV digest `724f225b...`.
2. **Values re-read by hand.**
   - **Eurostat JSON-stat.** The index is geo × 16 + time, with the geo order DE, FR, IT, PL, SE. Every figure matches the CSV and the generated table: SE 2012 −1.2, SE 2024/2025 −1.6/−1.6, DE 2010 −4.4, DE 2025 −2.9, FR 2020 −8.9, IT 2010 −4.2 with flag `b` (status key 32), IT 2023 −7.1, IT 2025 −3.1, PL 2023/2024/2025 −5.2/−6.4/−7.2.
   - **OMB xlsx.** Unzipped and dumped with a separate perl probe. Column E (Total, Surplus or Deficit (−)) reads FY2021 −12, FY2024 −6.3, FY2025 −5.9, FY2010 −8.7 (stored as −8.6999999999999993). All match.
   - The sheet's title is "1930 - 2025", so it holds actuals only and no estimates. The TQ row is excluded.
   - **US fiscal-year mapping.** FY n opens on 1 Oct of year n−1, and `PreRunYear`'s overlap loop agrees. At the eve of 5 Nov 2024, the game year [2023-11-05, 2024-11-04) holds 332 days of FY2024, so it is read as FY2024. No tie is possible: a 365-day game year never splits evenly between two fiscal years, so the code's earlier-year tie-break and the diagnostic's later-year one never disagree.
3. **`SeedPreRun`.**
   - The majority-year rule is correct.
   - It stops at the first missing year (`break`, FiscalVote.cs:165).
   - It is idempotent on an existing book (FiscalVote.cs:155), so a save's book stands. SetWorld and Awake both call it through `SeedPublishedHistory` (SimulationManager.cs:6457).
   - **The default epoch 2026-10-01 seeds nothing.** Its newest game year is mostly 2026, which is unpublished. This is DECLARED in `fiscal_balance.md` ("a world opened on the default epoch ... enters with no pre-run year"), though not in FiscalVote's reading (8).
   - Played starts do seed. Sweden's start (2026-01-18) reads 2025, 2024, 2023, 2022.
4. **Tenure gate.** `Judge(country)` and `ShiftOverTerm` read `EconomicVote.TookOffice`. Both the closed-year loop (FiscalVote.cs:284) and the open year (:297) are gated. (7c) plants a government formed today and expects zero years read. Correct as written; see M3 for the consequences.
5. **σ_v.** `perl Tools/aloop_sigma_fit.pl --dry` reproduces the fit: MAD-based 4.388503 pp, IQR-based 4.199471, median −1.7000, MAD 2.9600, kurtosis 6.484, sd 5.899341, n 239.
   - The factors 1.482602218505602 and 1.348979500392163 recompute from the normal's 75 % point.
   - With n = 239 (odd), both medians are exact order statistics, so type 7 is exact.
   - The cap is applied to `GovernmentShift`, the government side's total, before `SplitGovernmentShift`. This is consistent with the plan's §4 (2): shift_gov = σ_v·Δz, then split.
   - The cap is reachable: the support bound alone allows σ_v × 33.412 × 0.10 = 14.66 pp.
6. **Format 61.** The step is CONSERVATIVE, refusing v60 saves. (6a) round-trips `OpenedOn` and `SourceYear`, and `Copy()` carries both.
7. **The sentinel, `form`.** Every `RecordOverTerm` / `RecordPartsOverTerm` call site on the play path is gated on `PlayerCountryId`:
   - `HoldPresidentialRound` (SimulationManager.cs:5734), `HoldUsPresidentialCount` (:5766, which also reaches `HoldUsMidtermCount`), the campaign's opening and count (:5711, :5940), and `HoldPlayerElection` (PlayerElection.cs:21).
   - `TrajectoryBaselineDump` seats no player (TrajectoryBaselineDump.cs:26). **Note:** it opens on Sweden's start (2026-01-18, :101), so the dump's books ARE seeded for all six countries. Nothing in that world reads them, but inertness must still be proved by the dump diff (rule §400). The diff is owed and was not run here.
8. **Rules.**
   - The compile gate: `GATE OK`.
   - Provenance: `fiscal_balance.md` is headed [SOURCED], and the generated table names its record file. `ShareCap` is [AUTHORED-DRAFT].
   - MetaTextCheck: no string literal was added under `Assets/Scripts/UI` or `Data`, and `FiscalVoteBook.cs` adds comments only.
   - UnwiredSubsystemCheck: `FiscalBalanceRecord.For` is called from `FiscalVote.PreRunYear`, so the file is wired.
   - The `<see cref>` to backtick conversions in FiscalVote.cs are comment-only. The new `.meta` GUIDs are unique.

## Findings

### H
None.

### M

**M1. `FiscalBalanceRecordCheck` is never run** (Assets/Editor/FiscalBalanceRecordCheck.cs:28; it is absent from Assets/Editor/CheckSuite.cs, where `FiscalVoteFitCheck` sits at :304).
- A whole-repo grep finds the name only in its own file, the generated header, the `.md` and the prep tool. The `.md` and the generated header both say the check "re-reads the raw files and the CSV against the table".
- **Failure scenario:** someone re-fetches Eurostat after the April 2027 EDP notification and edits the CSV or the table without re-running the prep. The game seeds the stale or hand-typed figures, and no bar fails.
- **Fix:** register it beside `FiscalVoteFitCheck` in the cheap table.

**M2. The first count's BALCH_ey compares the game's own book with the source's** (FiscalVote.cs:166 seeds BAL-1 from the record; :298-300 forms BAL0 from the game's open year).
- Any level gap between the model's balance and Eurostat/OMB becomes a "change". The game's economy is the one mid-2026 seed vintage (`WorldClock.SeedVintageLine`), and its perimeter is the model's. Each point of GDP of gap moves the government side by 22.073 × 4.3885 / 100 ≈ 0.97 pp of share.
- **Concrete case, Poland's reference world (eve of 18 May 2025).** Of the four seeded years (2024, 2023, 2022, 2021) only 2024 opened under Tusk's tenure (from 2023-12-13). So there is no term variable, and the whole fiscal term is BAL0, annualised from ONE day of the game's flow, minus Eurostat's −6.4 %.
- **USA (eve of 5 Nov 2024).** Three years fall under Biden's tenure, so the fallback term is −6.3 − (−6.2) = −0.1 pp of GDP, worth −0.05 pp of share. The rest is the same seam.
- **Sweden's start.** The fallback term is exactly 0 (2025 and 2024 are both −1.6), so the first count is all seam.
- The diagnostic prints the two figures side by side in (8) but bounds nothing.
- **Ask Elias:** accept the seam as declared, or measure each start's model-to-source gap at day one and read BAL0 relative to it.

**M3. Under K63, BALCH_term's four-year form is unreachable in play, and "tenure" resets at each in-play formation.**
- Game years are 365 days from the epoch, and every modelled cycle that holds elections is four years. No government formed after an election can close four whole years under its tenure before the next count:
  - Sweden in play: formed about 2026-10-20 (37 days after the 13 Sept election). The years opening 2027-01-18, 2028-01-18 and 2029-01-17 close before the 2030-09-08 count. That is 3 years, so the fallback applies.
  - The government of record at Sweden's start (2022-10-18) also reads 3 years.
- So every count in every country reads [BD06]'s fallback, BAL-1 − BAL-2, at the four-year coefficient 11.339.
- `TookOffice` returns `g.FormedOn` for a government formed in play (EconomicVote.cs:137). A re-elected player's own previous term is therefore "a predecessor's". Concrete case: M re-forms in October 2026, and at the 2030 count the year that opened 2026-01-18 under M's own Kristersson government is not counted.
- Declared reading (4) cites [BD06]'s narrow sample ("in office for at least two fiscal years") in support of the strict reading. But that filter still measures BALCH_term over the four-year window, predecessor years included, so the citation does not support excluding them.
- The plan's registered pass (A_LOOP_PLAN.md §4 (4): "at the second count each RECORD line prints the fiscal term from four closed years") can no longer hold, and has not been restated.
- **Ask Elias:** (a) should a re-formed government with the same PM's party continue its tenure, as `TookOffice`'s of-record branch does for the same head? (b) is a fallback-only term intended?

**M4. Confirmed: E1's fitted factors go stale, and the readings pinned on them move.**
- K62 seeds the reference worlds that `PresidentialReferenceWorld` builds on the eve: the USA's (5 Nov 2024) and Poland's (18 May 2025). The fiscal term is non-zero in both (M2), so the poll the factors are refit on moves.
- **Will fail at the 1e-6 refit:**
  - `UsPresidentialFactorCheck`. Its stored factors are `PresidencyOfRecord.CandidatesOf(USA, 2024)`; patch them from the FIT lines with `Tools/us_factor_patch.pl`. Its US-6 block in `docs/reference/US_ELECTIONS.md` is pinned in the cheap and document bars.
  - `PresidentialElectionLiveDiagnostic` (Poland's E1 factors, `CandidatesOf(Poland, 2025)`).
- **Expected to move**, since they read those worlds or a played start's first count:
  - `UsPresidentialElectionDiagnostic` (its US-8 block in US_ELECTIONS.md);
  - `PresidentialVoteBacktest` (`docs/reference/PRESIDENTIAL_VOTE.md`);
  - `CongressOfRecordDiagnostic`;
  - `CongressElectionDiagnostic` (its US-16 block);
  - `UsNationalVoteCheck` (its block);
  - `UsMidtermCheck` (its block; it steps a day);
  - `PresidentialStartMeasure`, `PreStartRecordDiagnostic`, `PollingDayDiagnostic`, `TermLimitDiagnostic`, `PolishDeclarationsDiagnostic`, `PolishPresidentialStartDiagnostic`, `UsOwnNomineeDiagnostic`, `UsCampaignDiagnostic`;
  - the film driver's poll captions (`UiScreenshotDriver.cs:1533`, `:5007`);
  - every reader of `NoPolicyCentury.For`. That century seats a player and applies the start (NoPolicyCentury.cs:61-70), so its counts now carry the term. This includes AF-12's "without your policies" world (K50).
- The no-policy dump itself is not on this list (see item 7 above).

### L

**L1.** `ApprovalVoteDiagnostic` (2f) (ApprovalVoteDiagnostic.cs:133-140) and `aloop_record_check.pl` parse `ApprovalVote.Describe` by regex. Both fail closed if the wording changes, which is acceptable. But both read b off the line, so a b of 0.20 would pass. K60 says 0.16: pin `b == ApprovalVote.CoefficientFor(country)` and 0.16.

**L2.** `aloop_record_check.pl`'s K61 sign test reads the 4-decimal printed side. A window that improves by less than about 5e-5 pp of share prints `+0.0000` and FAILS as "improves and the term is +0.0000". Compare on the recomputed `$z`, or allow `abs($side) < 5e-5`.

**L3.** Documents owed, likely the integrator's:
- A_LOOP_PLAN §3/§4 do not yet restate K60's pass or K61's surplus clause ("the surplus poles gain"), and :230 still says σ_v 5.90 pp.
- `ShareCap`'s summary says "On the play-calibration list: ...", but no `.md` carries "q13's share cap" yet; it belongs on §346's list.

**L4.** The majority-year rule can read a calendar year that was still open in reality at the start's date. Italy's [2021-07-21, 2022-07-21) is read as 2022, and Germany's newest year at 6 Nov 2024 as 2024. Reading (8) refuses day-weighting because "no source publishes before the year ends", yet it reads years in hindsight. Declare it.

**L5.** `aloop_fisc.plan`'s `f5_it` (Italy, idle, 120 days) sits under "the first count of every start that holds one". Italy holds none (`WorldClock.NoElectionYet`), so the run measures no count.

**L6.** `NoteDay`'s lazy seed (FiscalVote.cs:202) dates the book from the static `EpochDate`. A world that reaches day N > 0 with no book would get pre-run years ending at the epoch and an open year dated the epoch, holding the current period's flow. It is reachable only by a book-less mid-run world (v60 saves are refused), so it is low risk.

## Owed before the bar

- Register `FiscalBalanceRecordCheck` (M1).
- Refit and patch the E1 factors. Regenerate the US and Polish reading blocks (M4).
- Run the no-policy dump diff against `form` to prove that the seeded books are inert.
- Run `FiscalVoteDiagnostic`, including (8b) for every start that holds an election: Sweden, Germany, Italy, Poland and the USA, all expected to carry a term after day one.
- Run `aloop_fisc.plan` and `aloop_record_check.pl`.
