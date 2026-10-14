# Review - §816, the gameplay audit's Phase 4: GA-8-1 and GA-8-2, the carbon statute's calendar clock, the passed figure, and the bill that names only what moved (2026-10-14)

Two passes by independent read-only agents. **The first** read the patch prepared outside the tree while §815's bars ran - `Assets/Scripts/Data/TaxLine.cs`, `Assets/Scripts/Data/CarbonRateStatute.cs`, `Assets/Scripts/Simulation/ParliamentSystem.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/UI/GameController.cs`, `Assets/Scripts/Data/BudgetBill.cs`, `Assets/Editor/CarbonTaxUnitDiagnostic.cs` - against the tree, told the defects, Elias's ruling verbatim, §464, the save rule (ruling A) and the numeric-inertness rule, and asked to break the clock at every start, the passed-figure rule, the builder change against every reader of a bill's tax lines, the no-policy path, the diagnostic, the compile and the comments. **The second** read the revisions the first one's findings caused, in the tree, with the first report beside it. Neither edited anything or ran anything that writes. Both reports follow verbatim, then what was done.

## The first pass

HIGH 0 / MEDIUM 1 / LOW 7. The clock, the gate and GA-8-2's builder are correct and look compile-clean on reading. One real break: a per-tonne line is stamped whenever a bill names it, so a bill that only restates the current rates now changes something, and C-C1's untouched-draft check fails for Germany.

Checked and correct:
- **The clock.** `ComingBoundaryDate` = EpochDate + 365·(CurrentTurn+1) (SimulationManager.cs:2719). That is the day AdvanceDay signals. ApplyDomesticPolicy runs before `CurrentTurn++` (4884/4907), and the preview reads the real CurrentTurn.
- **Readings by start** (traced):

| Start | First boundary | Figures read |
|---|---|---|
| Germany | 2025-11-06 | 55, 55, then carried |
| Sweden | 2027-01-18 | 55×r at turn 0 |
| Poland | 2024-02-19 | 45, 55, 55, carried (byte-identical to the old path) |
| Italy | 2023-07-21 | 30, 45, 55, 55 |
| USA | 2025-03-12 | 55, 55, carried |
| France | 2025-07-18 | 55, 55, carried |
| Default | 2027-10-01 | 55×r |

- **Leap days** only pull boundaries earlier, so no calendar year is ever skipped. The only repeated year within 120 turns is Sweden's 2096 (01-01 and 12-31, turns 70–71). After that a figure passed in January skips the December boundary and stands about 23 months. That is outside play.
- **Load, resume and forks.** The save restores CurrentTurn, CurrentDate and EpochDate (SaveGameService.cs:239, SimulationManager.cs:4655). Json.NET serializes PassedForYear as a public field. `TaxLine.Clone` and the ShadowBaseline fork both carry it. `currentTurn > 0` reads correctly after a load and in the preview.
- **Budget timing.** Windows open Jan 1 for Sweden and Germany (FiscalYearData.cs:18-19) or on arrival, and pass 21 days later. Sweden's Jan 22 passage stamps the year its figure is for.
- **Other writers.** Only ApplyBillResult stamps. The AI ministry and the Finance partner write IncomeTax and VAT only (AiFinanceMinistry.cs:246, FinancePartner.cs:92). Nothing in play overrides carbon at the boundary. The ceiling carries before the gate, as it should.
- **GA-8-2's builder.** Lines named at their standing rate added 0 to the direction, and `BillConcern.Add` drops zeros (StanceModel.cs:57), so votes are unchanged. BracketRateMove's two branches agree at HEAD (TaxBases.cs:125). Poland loses only the phantom tax act, which was the same reversion. Nothing treats an empty TaxLines specially.
- **Numeric inertness.** The dump passes no bill, so only Germany's path moves.
- **Compile.** All five callers are updated and nothing reaches them by reflection. Unity 6000.5 gives C# 9. The using-declaration, the tuple `params` and the named-then-positional arguments are valid. The new backticked names resolve.
- **The diagnostic.** PriceLevelAtLastIndex is seeded at Country.cs:256 and advanced only by IndexSpendingLines (6344), so the ratios telescope. Day index 100 is 2025-02-15. The expected values hold by trace; I did not run it.

Findings:

1. **MEDIUM, CONFIRMED** — ParliamentSystem.cs:491 with CarbonRateStatute.cs:139.
   - **Mechanism:** the line is stamped even when named at its standing rate. The turn-0 clause `PassedForYear >= LastScheduledYear` then reads the seed's 30 as the law's figure in force.
   - **Scenario:** BudgetDraftEstimateDiagnostic's RestatingBill (lines 133-139) names every line. On the default epoch at turn 0 the proposed clone is stamped 2026 (SimulationManager.cs:5492). At the previewed 2027 boundary Germany carries 30×r against the standing clone's 55×r. That is about 3.5 bn EUR (1.68 t × 84 M × 25) against an Epsilon of 0.5, so "an UNTOUCHED draft does not estimate to zero" fails (lines 70-78). The same happens on Sweden's start.
   - **Also:** its +1 raised bill lowers Germany's rate by 24 per tonne. This contradicts the patch's own BudgetBill doc ("a line named at its current rate is no change").
   - **Reach:** not in CheckSuite and unreachable in play.
   - **Fix:** stamp only on a move, with `moved = !Mathf.Approximately(clampedRate, line.Rate)` taken before the write. Drop the turn-0 clause too; it is unreachable in play.

2. **LOW, CONFIRMED** — CarbonRateStatute.cs:123. The zero-rate guard runs before the gate, so a passed 0 (the slider's minimum) is never moved again. On Germany's start, a 0 passed in 2025 stays 0 at 2026-11-06, while a passed 10 becomes 55. Fix: skip on `!IsImplemented` alone, or on `Rate <= 0 && PassedForYear == 0`.

3. **LOW, CONFIRMED (code)** — CarbonTaxUnitDiagnostic.cs:229-230. `RunStatutes(Years)` uses whatever epoch is set. The Germany assertion (172-176) now depends on the epoch, so a leaked or played start fails it spuriously. Fix: `SetEpoch(epoch ?? SimulationManager.DefaultEpoch)`.

4. **LOW, CONFIRMED by trace** — "no bump" leaves two leftover effects in old saves.
   - An old Sweden-start save made at CurrentTurn 1 (2027-01-18 to 2028-01-18) holds the old 45. The new rule carries 45×level for ever and never reaches 55.
   - An old save made after a carbon bill passed in the current year loads with PassedForYear 0, so the statute overwrites that figure once more.
   - Not bumping is defensible under EN-4e's precedent. Either record both effects in CurrentSaveVersion's comment or bump to 39.

5. **LOW, PLAUSIBLE** — SimulationManager.cs:5200, 5492 and GameController.Desk.cs:1102. The preview stamps CurrentDate.Year, but the vote comes 21 days later. A December tabling therefore previews the statute moving a figure that the real stamp keeps. Fix: stamp `CurrentDate.AddDays(DaysRemaining or BillDurationDays).Year`.

6. **LOW, a reading to confirm with Elias** — CarbonRateStatute.cs:139-140. Past the schedule, a bill's figure passed for 2026 or later is carried for ever, not only "for the year it was passed for". Inside the schedule it yields to the BEHG figure. Related: the builder cannot re-affirm an unchanged figure, so Germany's 80 passed in 2025 becomes 55 at 2026-11-06 unless moved.

7. **LOW, CONFIRMED (code), pre-existing** — GameController.BudgetV35.cs:520-522 writes every levied line's draft on every frame. Only carbon is dropped at a boundary (GameController.cs:6315). IncomeTax or VAT moved by the Finance partner's boundary step therefore stay drafted at the old rate and are named and reverted. GA-8-2 only fixes bills tabled before the boundary. Fix: write the draft only when the slider moves.

8. **LOW, CONFIRMED — stale or loose comments:**
   - CarbonRateStatute.cs:56 says "the schedule counts from it", but SeedYear is now unreferenced.
   - Lines 35-36 say "a year or two behind the law". Italy's start read a year ahead; the default and Sweden starts read three behind.
   - "A game begun after the schedule" (lines 37-38, 136-138, diagnostic line 28) is wrong: both games began in 2026. It is the first boundary that falls after the schedule.
   - The brief says fp755 runs on the default epoch, but TrajectoryBaselineDump.cs:100-101 opens it on Sweden's start. The baseline rows should say so.

## The second pass

HIGH 0 / MEDIUM 0 / LOW 3. The revisions close findings 1, 2, 3, 7 and 8 and break nothing in play. `PassageYear` (finding 5) is right only for the government's pending bill and a draft inside an open budget window, and finding 4's save note misses two residues.

Checked and correct:
- **F1 addressed.**
  - `moved` is taken before the write (ParliamentSystem.cs:491-493). The restating bill stamps nothing, and C-C1 reads 0.00 for all six countries (ga8diag.log:1268-1279).
  - The kept turn-0 clause (CarbonRateStatute.cs:143) is test-only, as claimed. A German player's world opens on 2024-11-06, so its first boundary (2025-11-06) lies inside the schedule (GameController.cs:2166-2173). In play, no bill ever names an AI Germany's carbon line.
  - Its cost still shows in C-C1: the +1 bill takes Germany from 55×r to 31×r on the default epoch. Five other lines mask it (rev +63.8).
- **`moved` edge cases are sound.**
  - It compares the clamped figure, so a bill clamped onto the standing figure stamps nothing.
  - The builder never names a sub-epsilon move, because it applies the same Approximately test.
  - A fallen Polish tax act empties TaxLines (BudgetBill.cs:114). `PartOf` acts are voted only, never applied.
  - Carbon is a single dictionary key, so it cannot be named twice. The hike accounting is untouched.
- **F2 addressed.**
  - No seed has an implemented zero carbon line (WorldFactory.cs:223-241).
  - A zero levied by the switch belongs to Italy, Poland or the USA, all of them nominal.
  - Sweden indexes a passed 0 to 0. Germany's passed 0 yields to the BEHG inside the schedule and stays 0 after it.
- **F3 addressed.** CarbonTaxUnitDiagnostic.cs:229-230 pins the epoch inside the scope, before the world is built.
- **F5: the government's pending bill is right.**
  - The bill counts down after the date advances and resolves that day (GameController.cs:773-783; SimulationManager.cs:299, 1535). So CurrentDate + DaysRemaining is the passage day, with no off-by-one.
  - A bill resolving on the boundary day is stamped before AdvanceYear reads it.
  - Polish acts are voted the same day. The three real passages keep CurrentDate.Year.
- **F6** is asked as row 36. The row names only the builder, but `moved` is a second gate on re-affirmation, so either ruling must change both.
- **F7 addressed.**
  - An untouched slider returns its draft unsnapped (LedgerRow.cs:341), so no grain artefact appears.
  - The change count, DraftFingerprint, the if-passed panel and the People page all read the built bill or test Approximately.
  - Sub-row drafts already write only on a move (GameController.cs:10003).
  - The carbon cleanup at line 6315 still holds.
  - The film driver restores its draft (UiScreenshotDriver.cs:1152).
  - The audit harness's DraftTax still passes through the builder's filter.
- **F8 addressed.** SeedYear is now referenced only by its declaration. The fp755 baseline-row note is not in the tree; it is a ledger matter.

Findings:

1. **LOW, CONFIRMED (trace).** SimulationManager.cs:5192 with GameController.BudgetV35.cs:872 and GameController.cs:9585.
   - **Mechanism:** Introduce is enabled only while a budget process is open. That is on arrival, then on each fiscal-year start: 1 Jan, or 1 Oct for the USA (SimulationManager.cs:2457-2468; FiscalYearData.cs:18-19). Yet an untabled draft is stamped CurrentDate+21.
   - **Scenario:** a German player drafts 80 on 2024-12-01, after the arrival bill.
     - The estimate stamps 2024, so at the 2025-11-06 boundary the clone reads the BEHG 55 and shows a nil carbon effect.
     - In reality the bill can only be tabled 2025-01-01. It passes on 01-22 stamped 2025, so 80 stands.
     - The estimate therefore omits about 3.5 bn EUR (25 × 140.65 Mt).
     - The same happens from 7 Nov to 10 Dec 2025. Elsewhere (Sweden, and Germany past the schedule) the error is about 2%.
   - **Stale caches:** the estimate caches are keyed on turn and draft, not on date (GameController.cs:4664-4677, BudgetV35.cs:1166, Desk.cs:1100). That estimate is still the one shown when the bill is tabled in January.
   - **Fix:** run the course from the next tableable day plus BillDurationDays, and add PassageYear to the cache keys.

2. **LOW, CONFIRMED (code); effect PLAUSIBLE.** SimulationManager.cs:1200 and 1535.
   - **Mechanism:** a tabled alternative's DaysRemaining is copied once at tabling and never counted down. The UI itself reads the government bill's count (BudgetV35.cs:887).
   - **Effect:**
     - The Desk's preview of the alternative (Desk.cs:1097-1102) stamps a date past the decision day, overshooting by the days elapsed.
     - An untabled opposition draft gets 21 days instead of the government bill's remainder.
     - Only Sweden is affected. The year changes only for a late-December decision (an arrival budget) once the preview recomputes, for example after a load.
     - The size is about 2% of the carbon line.
   - **Fix:** while a government bill is pending, use its DaysRemaining for any other bill.

3. **LOW, CONFIRMED (code/trace).** The note at SaveGameService.cs:53-56 is incomplete.
   - **(a) Stale drafts survive.**
     - An older save carries a draft for every levied line, because the old tile wrote them every frame.
     - RestoreUiDrafts brings them back (GameController.cs:1000).
     - A draft made stale by a later move persists and is named in the next bill. Such moves are the Finance partner's VAT or income-tax step, or an adopted AI budget.
     - So finding 7's reversion survives in such a save.
   - **(b) More old-clock residues.** The old clock started the post-schedule carry late, so the Sweden turn-1 figure of 45 is not the only residue:
     - Sweden-start saves from turn 2 onward carry Germany two to three price ratios low.
     - Germany-, USA- and France-start saves from turn 3 onward carry it one ratio low.
     - Italy-start saves from turn 4 onward carry it one ratio high.
   - **Fix:** record both in the note. Nothing marks the drafts in (a), so they cannot be cleaned on load.

## What was done

**The first pass:**
- **1 (MEDIUM): fixed.** `ParliamentSystem.ApplyBillResult` stamps a per-tonne line only when the bill MOVES it (`Mathf.Approximately` against the standing figure, before the write): a restating bill passes nothing. `BudgetDraftEstimateDiagnostic` passes on it (the untouched draft estimates to zero for all six). The turn-0 clause in `CarbonRateStatute.AdvanceYear` is kept, not dropped: stamping only on a move, it can no longer misread a restatement, and where a bill moved the line in the schedule's last year or later before a game's first boundary (tests, never play), carrying the bill's figure is the statute's own rule - the last legislated figure carried by the level.
- **2 (LOW): fixed** - the zero guard skips an unimplemented line, and an implemented zero only where no bill stamped it (a save from before keeps it as it was); a passed zero yields to the statute after its year.
- **3 (LOW): fixed** - `RunStatutes` pins `SetEpoch(epoch ?? SimulationManager.DefaultEpoch)` inside its scope.
- **4 (LOW): not bumped, written down** - ruling A; the residues an older save keeps are recorded beside `SaveGameService.CurrentSaveVersion` (completed after the second pass's finding 3).
- **5 (LOW): fixed** - the two preview sites stamp `PassageYear`, the year the bill would pass in (refined after the second pass's findings 1 and 2).
- **6 (LOW): asked** - question 36 in `docs/audit/AUDIT_FOR_ARCHITECT.md` (a passed figure past the schedule is the base the statute carries; a passed figure cannot be re-affirmed before its year ends - and, as the second pass adds, the `moved` gate is a second reason).
- **7 (LOW): fixed** - the Budget page's tax slider writes a draft only off the standing rate (`GameController.BudgetV35.cs`, the spending lines' idiom), so GA-8-2's "names only the lines the player moved" holds after a boundary too.
- **8 (LOW): fixed** - the comments ("a game whose first boundary falls after the schedule"; Italy's start a year ahead, a 2026 start three behind; `SeedYear`'s remark). The baseline rows say the dump's epoch (Sweden's start, `TrajectoryBaselineDump.Build`).

**The second pass:**
- **1 and 2 (LOW): fixed with the pass's own remedies.** `PassageYear(countryId, bill)`: while the government's bill is before the chamber every bill is decided on its day; a draft not yet tabled is tabled when a budget process is next open (now, or the next fiscal-year start) and decided a bill's whole course later. The caches keyed on turn and draft are left: the stamp is now the predicted passage year, the same in December as in January once the bill is tabled.
- **3 (LOW): fixed** - the note beside `CurrentSaveVersion` names both further residues: the stale drafts every levied line holds in an older save, and the old clock's lag by start.
- The second pass's changes are a preview-only helper and a comment; no third pass was run.

**The family** (`TrajectorySentinelCheck`, the BASELINE): `ga81`, `traj_ga81_s{777,424242}_t100.csv`, on the dump's epoch - Sweden's start - with the sentinel's digests `c393135b` (777) and `9ba38c16` (424242), explained per country in the check's own summary: Germany moves from its first boundary (2027-01-18, the law's last 55 carried by the level where the turns' count set 45), the rest from turn 2 in late digits.
