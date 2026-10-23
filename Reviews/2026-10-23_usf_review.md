# Review: K38-K44, the US oath dates, the Congress view's rows, the top masthead, the veto's ten days, funding restored, PRESIDENT-ELECT (wave/usf)

**Branch:** `wave/usf` in `G:/UNITY/Projects/PoliSim-wt/usf`, base `140aab81`, HEAD `2cd72aff` (7 commits)
**Reviewer:** independent and read-only. No Unity was run. I ran the compile gate (`Tools/cgate/gate.sh`, result `GATE OK`) and `Tools/textcheck`: 4 of 5 checks are clean. UpstreamCheck fails because the branch has no upstream. That is where the work stands, not a defect in the code (L6).
**Files reviewed:** `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/Simulation/UsBudgetProcedure.cs`, `Assets/Scripts/Simulation/Congress.cs`, `Assets/Scripts/Data/Country.cs`, `Assets/Scripts/Data/DivisionRecord.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, `Assets/Scripts/Elections/UsNight.cs`, `Assets/Scripts/Elections/PresidentialElection.cs`, `Assets/Scripts/UI/GameController.cs`, `Assets/Scripts/UI/GameController.Desk.cs`, `Assets/Scripts/UI/GameController.CongressV35.cs`, `Assets/Scripts/UI/GameController.ParliamentRows.cs`, `Assets/Scripts/UI/GameController.DocketV35.cs`, `Assets/Scripts/UI/GameController.Slip.cs`, `Assets/Scripts/UI/SigningScreen.UsPath.cs`, `Assets/Scripts/UI/LapseMemo.cs` (deleted), `Assets/Editor/UsCongressViewDiagnostic.cs`, `Assets/Editor/UsBudgetProcedureDiagnostic.cs`, `Assets/Editor/UsDocketDiagnostic.cs`, `Assets/Editor/UsNightDiagnostic.cs`, `Assets/Editor/PolishPresidentialStartDiagnostic.cs`. I also read, for context, `GameController.PoliticsV35.cs` (the Congress page's draw order and its two `DrawSlips` calls), `UsPresidentialElection.cs`, `UsCongressElection.cs` and `UsCongressView.cs`.

## Verdict: NOT READY

There is one H, and it is in the UI only. On the Politics page's Congress tab, the USA's main page, three sets of slips cannot be opened by the pointer: the new nominee line's slips, the Congress view's own slips, and the top masthead's slips. K39 and K40 each require these slips to be reachable. The fix is small (see H1). There are also two Ms: real films are owed at 1280 and 2560, and neither is evidenced on the branch. The rest are Ls.

The money paths are clean, and the format step is clean.

## What was verified (and holds)

- **(1) The money paths only store dates.** `SimulationManager.cs:1750`: `presentedOn` is a `DateTime`. It is read from `docket.PresentedOn` before `docket = null`, or it is `CurrentDate.Date`, and it is passed to `Congress.RecordVeto` (`:1759`). `Congress.cs:902-911` writes it to `PresentedOn` on the first record and on the second record, after `RecordOverride`. `SimulationManager.cs:2196`: `LapseRestoredOn = CurrentDate.Date`, guarded by `LapseSince != MinValue`. It is written before `LapseSince` is cleared. `FundedByLaw` has no float expression anywhere in its body, and neither does `UsVetoGate`. `:7384`: the preview clone copies the new value field. `UsBudgetProcedure.cs:176` adds a pure static predicate. No booking, ordering or float store changed, so by reading `us24dl` cannot move. Under the numeric-inertness rule, the proof is still the simulation bar's dump diff, not this reading.
- **(2) The format step.** `CurrentSaveVersion = 57`. `Deserialize` refuses any `version != CurrentSaveVersion` with the plain message (`SaveGameService.cs:219-222`), and the slot header does the same (`:410`), so a v56 save is refused. Both new fields are public instance fields on `[Serializable]` types, so Json.NET carries them under `BuildSettings()`. The forks (`PlayerLever.Copies`), the ledger worlds and the shadow world (`ShadowBaseline` `_carried`) clone through that same serializer, so the fields ride every world a save carries. `ClonePreviewCountry` lists `LapseRestoredOn`. `DivisionRecord` is not cloned by hand.
- **(3) K38.** `UsNight.DatesToTheOath` reads `ElectorsMeet`, `CountedOn`, `Contingent` and `TakesOffice` from `UsPresidentialElection.TryLatest`, and `Convenes` from `UsCongressElection.TryLatest`. Both take the latest contest whose polling day is on or before today, so nothing is listed on the eve, everything from the night, and each date through its own day (`>= day`). A midterm lists the new Congress alone. The Docket draws each date with `holdsTime: false`, the stamp "Information", and never sets `anyPending`. Nothing reaches `CollectHolds`. UsNightDiagnostic (f) plants contests from the statute-day helpers and checks the eve, the night, the day after the electors meet, the oath, the day after the oath and a midterm. It is not a tautology.
- **(4) K39.** `NEXT CHOICE` in the strip calls the same `StepUsNominee` the row calls (`ParliamentRows.cs:431-437`). `TryUsNomineeToday` is the row's own cached reading, factored out unchanged, so the effect is identical. The rows are gated on `UsCongressView.Applies`, which is `Congress.Applies` (USA only). `UsCongressView.Read` returns null only where `Applies` is false, so wherever the rows are hidden the view is drawn, and the nominee's action is never lost. Other countries' rows are untouched.
- **(5, in part) K40 for other countries.** For any country but the USA, `MastheadStripApplies()` is false. The banner returns 0 with no holds exactly as before, and with holds the `used` arithmetic is the same as before (both new widths are 0). The Desk keeps the role chip beside the year. In a Congress the Desk's LIVE caption is restored (`lapse = strip ? null : ...`).
- **(6) K41.** Only the US veto gate writes `PresentedOn`. `PolishDocketGate` returns before it, so Poland's veto records keep `MinValue`, and `VetoedDays` draws nothing for them. The vetoed plate's "today" is `record.Date`, the day of the votes on the veto. `ActOnDocketAnswers` acts on an answer the same day (`SimulationManager.cs:1697`), so that is the answer's day. `answeredOnDay` is the last numbered day on or before it. UsDocketDiagnostic (c) holds a veto answered days after presentment (`PresentedOn < Date`, day N > 0, N = 10 - days left).
- **(7) K42.** `RestoredToday` is true only on `LapseRestoredOn.Date == today`, and only while no lapse has started since. UsBudgetProcedureDiagnostic (c) checks the field before the lapse (initialised to `MaxValue`, so it fails if never read), the day the lapse ends (true) and the next day (initialised `true`, so it fails if never read). It is non-vacuous.
- **(8) K44.** `PlayerElectOn` covers the span from the deciding vote (`RunOffHeld ? RunOffOn : FirstVote`) up to the day before `TakesOffice`, and requires `PlayerWon()` (decided). Between the rounds, `Decided()` is false, so no chip shows. On the oath day, `PlayerHoldsPresidency` shows PRESIDENT first. The diagnostic checks the run-off's eve, the run-off, the oath's eve and the oath, and reads the Desk's chip through `RoleChip`. If the reflection breaks, the chip reads "NO CHIP" and the check fails.
- **(10) The rules.** CRLF is uniform in every touched file (counts of CRLF lines equal the line counts). No gendered words appear in new player text. I found no possessive built by hand on a value. The new docket and slip words are statute days and the model's own dates (provenance holds). The fonts sit at `V35.Floor` or above, under `V35.FloorGuarded`. The vetoed plate's head is 18 px. `LapseMemo` has no remaining reference, and its `.meta` is deleted with it.

## Findings

### H1 - On the USA's Congress page, the pointer cannot open the nominee line's slips, the Congress view's slips or the top masthead's slips (K39, K40)

`GameController.PoliticsV35.cs:128` draws `DrawCongressV35`: the who-governs strip, which now carries `SlipAnchor(..., "congress:nominee")` and `"congress:nominee:next"` (`GameController.CongressV35.cs:64, 73`). It then calls `DrawParliamentKept`, which calls `DrawParliamentPoliticalBlocks` (`PoliticsV35.cs:201`). That method opens with `BeginSlipAnchors()` (`GameController.ParliamentRows.cs:43`), which on Repaint runs `_slipAnchors.Clear()`. Every anchor the Congress view registered earlier in the same Repaint is erased, and the new nominee anchors with them. Their slips stay in `_politicsSlipBook`, but no anchor is ever hovered. The retired row's slip ("usnominee") was registered after the clear, in the parliament book, and did open. So K39's move is a regression: the who-governs strip's slips explaining the nominee, YOUR PICK and the Twenty-second Amendment are no longer reachable by the pointer. The NEXT CHOICE button itself still works, because it is a `Button`, not a slip.

The same clear also re-adds `_mastheadAnchors` (`GameController.Slip.cs:55`) inside the scroll view. The page then runs two `DrawSlips` passes in one Repaint:
- The inner pass, `DrawSlips(book, _parliamentSlipBounds)` (`ParliamentRows.cs:63`), compares the masthead's rects (in page coordinates) against a mouse position in scroll-content coordinates.
- The outer pass, `DrawSlips(_politicsSlipBook, ...)` (`PoliticsV35.cs:90`), compares them in page coordinates.

With the pointer on the masthead's role chip, the inner pass finds `hovered = null` (or a parliament row, depending on the scroll), and the outer pass finds `"role"`. `_slipHoverId` therefore flips on each call, and `_slipHoverSince` is reset to `now` (`Slip.cs:113`), so the delay never elapses. The role and lapse slips never open on this page, though they do on the Desk, the Docket, Budget, Laws, People, Energy and Statistics.

Concrete failure: a US player is on Politics → Congress during a lapse and hovers "Funding lapsed · day 12" on the masthead, or hovers YOUR NOMINEE. No slip ever opens.

Films cannot catch this. `DrawSlips` returns before pointer slips while `CaptureIdentity.Armed`, and a pin falls back through `TrySlipOf` to its `at` point with no anchor rect.

The Congress cards' own slips (`congress:president`, `house`, `senate`, `divided`) were already dead on the base, for the same reason. This branch extends the defect to the nominee line and the masthead.

Fix: do not clear in `DrawParliamentPoliticalBlocks`. Let the Politics page's `BeginSlipAnchors` stand, and rely on the page's `MoveScrolledAnchors`. If the inner `DrawSlips` is still needed, it should not see the masthead's anchors (and not two passes per Repaint).

### M1 - The vetoed plate grows on a Canvas surface, and no real film is evidenced (K41)

`SigningScreen.UsPath.cs:732-741` adds `6 + DayHeight` to every vote-on-veto plate whose record carries `PresentedOn`. The old declared departure ("the ten days are drawn only on the desk ... so the plate keeps its height at 16:9") was about exactly this height. Under the Canvas film rule (§578), a commit touching `SigningScreen` owes a REAL film that reaches a vetoed plate at 1280 and 2560. A dry film cannot claim Canvas text. Neither film is on the branch.

What to check in the film:
- whether the override strip and the ten days fit at 1280×720;
- whether the 200 px head "THE TEN DAYS / ANSWERED ON DAY 10" fits at 18 px in the real-weight face;
- that the 0.45 dim leaves the slip on the head reachable (`blocksRaycasts = true` with `interactable = false` should leave it reachable).

### M2 - The top masthead is now on every US page, so every US page's height changes; films are owed at 1280 and 2560 (K40)

For the USA, `DrawFoldedInterruptBanner` no longer returns 0 when nothing holds (`GameController.cs:5916`). Every US page, the Desk included, loses the banner's height from `tabContentHeight` at all times. `inner = Max(inner, StatsUnit(20f))` (`:6020`) can also make the strip taller than the old held banner. At 2560, `StatsUnit(20)` is likely to exceed the caption's line height.

Pages that were only ever checked unheld at 720 (the Desk's fixed rows, Campaign HQ) can now clip at the bottom. The checklist's "no overlap with existing chrome at 1280/2560" cannot be closed by reading. It owes a real film of each US page at both widths, with a lapse chip, a long hold list (the "+N MORE" fold against the reserved role width) and nothing held.

### L1 - UsCongressViewDiagnostic (f) checks only the source's shape, so it is close to a tautology (K39, K40)

`UsCongressViewDiagnostic.cs:125-148` asserts that the source text, comments stripped and whitespace flattened, contains the implementation's own statements, for example `if (!congressView) { DrawRecordRow(book); }` and `DrawHoldChips(holds, columnWidth, _bannerIsMasthead)`. It does fail when those statements are removed. But it fails just as readily on a harmless rename or reformatting, and it passes with H1's defect in place, because it says nothing about what draws, where, or whether a slip opens.

It should be reported as a pin on the code's shape, not as a check of the surfaces. The real check for K39 and K40 is the film (M2). Every other extended check is behavioural.

### L2 - FUNDING RESTORED can be skipped at speed (K42)

`RestoredToday` matches the saved day exactly. The retired memo showed the chip on the first frame within three days of the lapse's end. At a speed that steps more than one day per frame, a frame may never land on the restoration day, and the green chip is never seen. The ruling ("a saved field") is met. This is a side effect worth declaring.

### L3 - K44's outright first-round win is not exercised

The diagnostic covers the run-off branch only (Poland's real 2025 went to a second round). The `RunOffHeld ? RunOffOn : FirstVote` choice for a first vote won outright is correct by reading, but no check holds it. A planted contest with `RunOffHeld = false` and `Elected = PlayerCandidate` would close it in a few lines.

### L4 - Every veto from the AI's rule reads "ANSWERED ON DAY 0" (K41)

When the president of the day is not the player, `presentedOn = CurrentDate.Date` (`SimulationManager.cs:1750`), the same day as the votes on the veto, because the rule answers on the day. Every such plate draws its ten days with day 0 marked, and the slip says "DAY 0 OF 10". This is true to the model, but it is the most common vetoed plate, and K41's check covers only the docket path (N > 0). Declare it, or add a case.

### L5 - On pages with no slips, the masthead's chips have no slip

The campaign stages (HQ, map, polling and the rest) and the dense view (`DeskProvenance.On`) never call `DrawSlips`, so the masthead's role and lapse chips are drawn there with no slip. If that is intended, it should be declared.

### L6 - UpstreamCheck fails

`Tools/textcheck` reports that the branch has no upstream. I did not push, as instructed. This belongs to the integrator.

## Owed before the bar

- H1 fixed.
- Real films for M1 and M2.
- `Tools/review_row.ps1` adds this report's row to the ledger, because `SimulationManager.cs` and `UsBudgetProcedure.cs` are money paths.
- The simulation bar's sentinel confirms `us24dl` unmoved.
