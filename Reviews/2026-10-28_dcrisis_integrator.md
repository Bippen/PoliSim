# wave/dcrisis: the integrator's reading at the merge (2026-10-28)

**Why this exists.** Design's return of 2026-10-28 (`PoliSim-captures/design/RETURN_2026-10-28/`, boards 32a-32e and 33a-33b) answered sections 1 and 2 of D-WORKLIST: the debt crisis on screen and the top strip in every country (K178).

The build went through these steps on `wave/dcrisis`:
- Built at 6a66723d.
- Reviewed NOT READY (`Reviews/2026-10-28_dcrisis_review.md`, M1 and M2 plus nine Lows).
- Reworked at 3b8b2b18, then reviewed READY (`_review2.md`, eight Lows).
- Given a last pass on those Lows at e15a0eb4.
- Fixed at 6962e47f after the integrator's first films found text rects sized too short.

This file reads the merged state.

**Files reviewed:** `Assets/Scripts/UI/TopStrip.cs`, `Assets/Scripts/UI/GameController.TopStrip.cs`, `Assets/Scripts/UI/GameController.Crisis.cs`, `Assets/Scripts/UI/CalendarFit.cs`, `Assets/Scripts/UI/GameController.cs`, `Assets/Scripts/UI/GameController.Desk.cs`, `Assets/Scripts/UI/GameController.BudgetV35.cs`, `Assets/Scripts/UI/GameController.Statistics.cs`, `Assets/Scripts/UI/FiscalRules.cs`, `Assets/Scripts/Testing/UiScreenshotDriver.cs`, `Assets/Editor/TopStripCrisisDiagnostic.cs`, `Assets/Editor/UsCongressViewDiagnostic.cs`, `Assets/Editor/CheckSuite.cs`, `Tools/film_scope.tsv`.

**The merge.** `git merge --squash wave/dcrisis` (e15a0eb4) was made on ed689788 (§933 landed). It merged without conflict; q26 touched only `GameController.EnergyV35.cs` among the UI files. Regional 5a's uncommitted merge was set aside in a stash ("reg5a-merge-hold") while this item was measured. The one later fix (6962e47f, 2 files) was taken onto main by `git checkout wave/dcrisis -- <file>` for exactly the files that commit touched.

The tier is UI only (`Tools/bar_tier.ps1 -Staged`: 14 UI paths, 3 tooling, 2 documents, no simulation path). No money path is touched, so no review rows are owed and no baseline can move. GATE OK.

**The integrator's changes.**
- Registered `TopStripCrisisDiagnostic` in the cheap batch after UsCongressViewDiagnostic, commented s934.
- Added film-scope rows for `dryscr` and `dryscr2` (Sweden, the USA and Germany at 1280x720).

**The first films (dryscr, realcrse, realcrus) and why they failed.** The named run was clean: TopStripCrisisDiagnostic with its (a2) and (g) sections, UsCongressViewDiagnostic, MetaTextCheck, UiLeverVerbCheck, CommentClaimCheck, ChamberVerdictCacheCheck, PlayerRoleDiagnostic and NumberLocaleCheck, 8 of 8. The cheap bar was 144 of 144. All three films still exited 1, on OVERFLOW lines from the new crisis surfaces:
- The target chart's labels ("3.0", "3 %", "1.2") needed 17.5 px tall in 16 at 14 px.
- "1.2 %" and "US$1.81T" needed 37.4 in 36 at 30 px.
- The verdict line needed 21.3 in 20 at 15 px.
- "2 y 10 m left" needed 27.4 in 26 at 22 px.
- "US$1.37T" needed 43.3 px wide in 31.8, at 8 px. That figure was drawn inside a bar below the 14 px floor.

The cause was rects sized as the face's px plus two, where IMGUI needs about 1.25 times the face. The fix sizes every crisis text rect from `CalcSize`. A bar's figure that won't fit at 14 px moves to the caption below. The hits filed under 01f_desk_gameover were the staging's frames still under the previous shot's label, and that is fixed too.

**The measurement (this tree, s934).**
- ncr: 8 of 8 clean.
- dryscr2 (Sweden, the USA and Germany at 1280), realcrse2 (Sweden 1280) and realcrus2 (the USA 1280) all exit 0. They have no OVERFLOW, CLIP or ESCAPE lines, no `CALENDAR FIT` error, no `CRISIS PLATE ON FILM` line, and every staging restore logged (`SHOT: 01g-01g4 restored ... put back (None)`).
- cheapcr2: clean.
- Read on film:
  - `realcrse2_01g_crisis_moment.png` shows board 32b: the ladder with CCC filled, debt-to-GDP and the deficit, the programme column (EU + IMF, three years to 30 Jan 2032, no premium), the target chart with the dashed 3 % line, the Met / Missed / First review rows, and Continue as the only control. The strip reads "Paused · 1 decision waiting · Programme · year 1 of 3".
  - `realcrse2_01c_desk.png` shows the seven-place strip outside the USA (the speed chip, polling day 13 Sep 2026, "Minority, with support", "In opposition") with the calendar card whole under the shortened first row.

**Seen on film, a Low left for later:** at 1280 the chart's x labels "year 1" and "year 2" touch without a gap. It is legible, and it is not an OVERFLOW, because each label fits its own cell. It goes on the follow-on list beside the RETURN's boards.

**Declared readings** (recorded as K-lines at the merge): the plate is IMGUI over the page, not a Canvas takeover; every country's first card row, the USA's included, loses the strip's height; the holds fold into one chip; the running deficit is the trailing year to the last closed quarter, dated; the rating tile keeps the standing rating with the branch word; standing words beyond the board; the speed chip; GOOD "restored" on EndsOn + 1; met/over in ink and words; one shared clock predicate (`ClockHeld()`); the calendar card's fit order; France's censure and cohabitation absent for want of model data.

**Verdict:** READY. The UI tier is clean, the films show the boards, and the one class of defect the films found is fixed at its cause.
