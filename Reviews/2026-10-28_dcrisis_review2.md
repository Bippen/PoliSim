# Review 2: wave/dcrisis, the rework 3b8b2b18 (delta on review 9e66f1cb)

Reviewer: independent adversarial review, 2026-10-28. Delta `git diff 9e66f1cb..3b8b2b18` read against review 1 (M1, M2, L1-L9), Design's RETURN (`RETURN_2026-10-28/send/D-WORKLIST_return/RETURN.md`), the model (`SovereignCrisis.cs`, `SimulationManager.AdvanceDay`, `DebtLedgerRecorder`) and CLAUDE.md. Unity was not run, so neither `TopStripCrisisDiagnostic` nor any film has run on this commit. `Tools/cgate/gate.sh` on the worktree gives GATE OK, which is a compile check only. All ten touched files are CRLF on every line, and the new `.meta` matches the repo's two-line form with a unique guid.

**Files reviewed:** Assets/Editor/TopStripCrisisDiagnostic.cs, Assets/Scripts/Testing/UiScreenshotDriver.cs, Assets/Scripts/UI/CalendarFit.cs, Assets/Scripts/UI/CalendarFit.cs.meta, Assets/Scripts/UI/FiscalRules.cs, Assets/Scripts/UI/GameController.Crisis.cs, Assets/Scripts/UI/GameController.Desk.cs, Assets/Scripts/UI/GameController.TopStrip.cs, Assets/Scripts/UI/GameController.cs, Assets/Scripts/UI/TopStrip.cs

## The prior findings

**M1 (the clock chip under the budget window): ANSWERED.** GameController.TopStrip.cs:163-166 now reads `CollectHolds(includeBudgetProcess: true)`. The banner's filtered list is no longer used, so on the Budget page during the window the chip reads Paused and its slip lists every hold. I compared that list with Update's gate (GameController.cs:745-768, :813-817) and with `isTimePaused` (:2726):
- **Hold for hold, they agree.** Each hold Update tests is in the list: crisis plate, campaign opening, story, central-bank head, cabinet decision, foreign meeting, declaration, formation, debt limit, budget window.
- **Where Update differs, the strip is not drawn or the state lasts one frame.** The extra gates are the Canvas, the signing queue, game over (the strip is not drawn under any of them) and the one-frame `PresidentialNightOwed` / `UsHouseChoiceOwed` / `PresidentBetweenAsked` openings, which open their own surfaces.
- **Election night** is missing from `isTimePaused` but present in the list. That is harmless: the election board holds the clock and draws its own Continue.

So the false "1×" state is gone. It is still a third hand-kept enumeration, not one shared predicate (see N1).

**M2 (the calendar card can clip silently): ANSWERED in code, NOT YET ON FILM.**
- **The decision uses the drawing's own heights.**
  - `CalendarFit.GridHeight` is the same expression as the `GetRect` in `DrawCalendarMonthGrid` (GameController.cs:3654-3657), from the same accessors: `CalendarDayNumberLineHeight`, the new `CalendarGap`, and the fitted dot row.
  - The dense line's need (Desk.cs:1105-1115) is `DrawV35DenseLine`'s own arithmetic: text plus 4, plus the 4 above (V35.cs:507-516).
  - The weekday row is `CalcHeight` plus both vertical margins. Its style has `wordWrap = false`, so the width does not matter. Counting both margins over-states the GUILayout collapse, so the estimate is conservative. It can floor early but never under-reserve.
- **The fallback order is honest.** The dense line is dropped first (its text is the card's head slip, word for word: the same `SlipContent`). Then the cell drops to its floor; the day number never shrinks, so the 14 px floor holds. Then `Debug.LogError("CALENDAR FIT: ...")`.
- **The error is countable.** The driver's `OnRunLog` (UiScreenshotDriver.cs:195-207) counts every `LogType.Error`.
- **No silent clip is left on this card.** The only remaining clip is the not-fits case, and that case logs an error.
- **The dotY change** (GameController.cs:3782) is identical for the non-fitted sheet caller, where `rect.height` = number line + 10.
- **(g) is derived arithmetic, not a measurement.** Its 22/10/1.3/22 and the 330-39-4 body come from a film's 33 px pitch, as its comment says, but the Check message presents "at 1280 in 227 px" as if measured. The margin it shows is only 6.5 px (220.5 needed in 227). The real guard fires only when even the floor cell does not fit. Films pin the preferences, so provenance is off, and no film is known to show a six-row month at 1280. See N2.

**L1 (keyboard under the plate): ANSWERED.**
- **What changed.** Crisis.cs:66-70 clears `keyboardControl` and `hotControl` on the plate's rising edge and swallows KeyDown and KeyUp.
- **No global shortcut is lost.** `CrisisPlateInput` runs after the rail and the strip have handled the event (GameController.cs:2736-2755), so their input is untouched. The project has no IMGUI pause key: pause is the rail chip, which is disabled while anything holds. F5/F9 are read through the Input System in `Update` and are unaffected.
- **The only IMGUI key that becomes inert** is the slips' ESC-unpin (Slip.cs:81) while the plate is up, which is correct.

**L2 (the sources slip; the stock before and after): ANSWERED, with two notes.**
- **The citations are the model's.** Borensztein and Panizza 2009 for the 2.6 %, Gelos, Sahay and Sandleris 2011 for the 2.9 years, Cruces and Trebesch for the 37 % SZ haircut, Regulation (EC) No 332/2002 for the BoP facility, and the Portugal MoU 2011 for the three years and the 3 % end. No ESM source is invented for the euro countries.
- **Note 1 (N5).** The model holds Cruces and Trebesch and Gelos et al. through Tomz and Wright 2013; the C&T article is BILLED, not stored. The slip cites them as if primary.
- **The arithmetic.** before = stock now + written off, which is exact if nothing moved the stock after the haircut.
- **Note 2 (N4).** The crisis step runs before the day's fiscal flows (SimulationManager.cs:428 against :513), so "before" carries one day's flow.

**L3 (the verdict's "so far"): ANSWERED.** It now reads "the year to <d MMM>" (Crisis.cs:645-646). It repeats the date already printed on the line above it, without the year; that is cosmetic.

**L4 (labels outside their columns): ANSWERED.**
- **The ladder.** It now caps its cell at the column: 40 px at the reference, 7 x 40 + 18 = 298 in 300.
- **The chart.** It reserves the right end for the reference label and the running figure, and clamps the running label inside the area.
- **One residual overlap (N6).** The reference label and the running label can overlap when the running bar sits 20-27 px above the 3 % line.

**L5 (the staging): ANSWERED.**
- **The restore is exact, before any day can run.** `GovernmentDebt` is written down only for a default, by exactly the model's write (`max(0, debt) x 0.37`, the same as SovereignCrisis.cs:142-144). It is put back in `finally` by assigning the float saved before the `try`. `_daySpeedTimer` = -3600 s, so no play day runs on it, and nothing on the debt ledger is touched.
- **The ledger stays consistent.** `DebtLedgerRecorder` writes only on model calls (`RecordDay` / `RecordEvent`), so it never sees the transient stock.
- **The precedent agrees.** Other stagings that set debt and restore it (`AiFinanceMinistryDiagnostic` :82/:97, `EnergyLedgerDiagnostic` :482) record nothing either. CLAUDE.md's money-path rule is about reviews of runtime money files, not about harness writes. The driver is outside `$moneyRoots`.
- **Caches and scroll.** `_hasCachedPreview` is cleared, so a preview priced on the staged world cannot survive. The Budget scroll is zeroed for 05u and restored.

**L6 (a real crisis on film): ANSWERED, weakly.** Every `Capture` outside the staging now logs a `SHOT: CRISIS PLATE ON FILM` warning (UiScreenshotDriver.cs:3156-3167). `_realCrisisPlateFrames` appears only inside that warning. It reaches no summary or exit code, and `OnRunLog` ignores warnings (N7).

**L7 (diagnostic gaps): PARTLY.**
- **Done.** (a2) now drives the model's own `SovereignCrisis.AdvanceDay` on a planted CCC review day. It checks `StartedOn == day`, checks that the plate is owed on that day, and pins the manager's order in the source: `CurrentDate` advances before the crisis step. The hand-written "13 May 2033" and "14 Jun 2031" are correct for d = 14 May 2031. (f) now pins the `isTimePaused` term, the strip's full read, the row and the fit wiring.
- **Still open.** The controller's load reset (`ReferenceEquals`) and `!_isGameOver` remain untested. The reflective `ClockHeldBy` probe tests `a > 0 || b`, which is a tautology (N1). (a2) and (g) have not been run.

**L8 (France): ANSWERED.** "Relative majority" is gone. The doc at TopStrip.cs:104 declares the censure motion (art. 49) and the cohabitation ABSENT FOR WANT OF MODEL DATA. Review 1 also noted Germany sitting in `TakesMotions`; that is unchanged and still harmless.

**L9 (wording): ANSWERED, one phrase.**
- The hold chip counts only holds answered at a cell. The plate and election night show their own words through `HoldWords`.
- The run-cumulative cut says so after a second crisis in all three places (FiscalRules.cs:207, Crisis.cs:91, :655).
- The plate figure "US$X in every crisis" reads as per crisis (N8).

## K-b (the USA's first row)

- **No existing US check breaks.**
  - `UsCongressViewDiagnostic` pins only `deskYields` and `frameCarries` (:320-321), and neither touches `rowWorld`.
  - The only Desk geometry pin is TopStripCrisisDiagnostic's own (f).
  - `AssertDeskState` asserts only that the page is the Desk.
  - 01c/01d have no geometry captions.
  - `AssertMapChips` (driver :600) now runs on a 287 px map for the USA as well. Any overlap it finds is a counted error, not a silent one.
- **Nothing is double-counted.** `_topStripHeight` is subtracted once, from the content. The view was already shortened by the frame. The two together keep the Desk's scroll extent what it was before the strip, which is Design's "comes out of the first row".
- **The reading goes past the RETURN's letter.** RETURN line 101 names Sweden's boards (32b, 32d) only. The USA's change is the rework's reading ("one geometry"). It is declared in a comment but not as a K-line Elias can strike (N3).

## New findings

### High

None.

### Medium

None.

### Low

**N1. Three hold enumerations remain.**
- **Where.** GameController.TopStrip.cs:163-166 against GameController.cs:745-768 and :2726. The rework fixes the reported state by switching to `CollectHolds(true)`, but Update, `isTimePaused` and `CollectHolds` are still three hand-kept lists.
- **Failure scenario.** A future hold is added to Update and `isTimePaused` but not to `CollectHolds`. The chip reads "1×" again while no day moves.
- **What would catch it.** The (f) probe of `ClockHeldBy` (TopStripCrisisDiagnostic.cs:245) cannot: it exercises `a > 0 || b` and not the set.
- **Fix.** Have `isTimePaused` and the strip both read `CollectHolds(true).Count > 0`, or add a (f) clause that every hold predicate called in Update appears in `CollectHolds`.

**N2. The calendar fit's margin at 1280 is derived, not shown.**
- **Where.** TopStripCrisisDiagnostic.cs:96-115, Desk.cs:1123-1136.
- **Failure scenario.** The true number line or weekday row at 1280 is 2-3 px taller than (g)'s 22. A six-row month then silently floors its cells, which is an info line nobody counts. Nothing contradicts (g)'s "fits whole", because no film shows August 2026.
- **The check message.** It should say the heights are derived from the film's pitch.
- **Fix.** Log a `SHOT:`-readable `CALENDAR FIT: need N in M` once per Desk frame whatever the outcome, so the film measures the margin. Owed: one 1280 Desk frame on a six-row month, Sweden and the USA.

**N3. The USA's default card is now on the shorter row.**
- **Where.** Crisis.cs:606.
- **The risk.** `DrawDeskDefaultBody` drops the haircut row with no report when `y + rowH > body.yMax + 1`. The USA, the only default branch, now draws it in about 223 px of body against about 200 px of need at the reference: it fits by about 23 px, but this is unfilmed.
- **Failure scenario.** At a height where `V35.Px` rounds up, the dated haircut row disappears from 01g2 with no log line.
- **Fix.** Report the skip as the ledger cards do. Record K-b's USA extension as a K-line for Elias.

**N4. The haircut's "before" is reconstructed.**
- **Where.** Crisis.cs:329.
- **The gap.** `before = GovernmentDebt + writtenOff`, and the opening day's fiscal flows run after the crisis step. "Before" is therefore the pre-haircut stock plus one day's deficit (about US$5 bn for the USA). That is mostly invisible at the T tier, but it is not the stock the model wrote off.
- **Fix.** The exact figure is the `before` on the ledger event the model records (SovereignCrisis.cs:146). Read it, or store it on `DatedAmount`.

**N5. The sources slip cites secondary-held works as primary.**
- **Where.** Crisis.cs:230.
- **The gap.** The model's own docs say Cruces and Trebesch and Gelos et al. are quoted "as Tomz and Wright (2013) report them". The C&T article is billed, not stored.
- **Fix.** Use "Cruces and Trebesch 2012, as Tomz and Wright 2013 report …", or name Tomz and Wright as the source.

**N6. The reference and running labels can overlap on the Desk chart.**
- **Where.** Crisis.cs:490-501.
- **The cause.** `crowded` is a symmetric 20 px band, but the reference label sits above the line (refY-18..refY-2) and the running label is centred on ry (ry-9..ry+9). With ry = refY-24 they overlap by 3 px vertically, and both sit in the reserved right end.
- **Fix.** Widen the band above the line to 27 px.

**N7. Two driver blemishes.**
- **The displaced doc block.** CaptureDeskCrisis's `<summary>` now sits above `_stagingCrisis`'s own `<summary>` (UiScreenshotDriver.cs:4933-4948). The field carries two summaries and the method none.
- **The uncounted frames.** `_realCrisisPlateFrames` never reaches the run's summary or exit code, so a run that films a plate over pages still ends clean.

**N8. Wording.**
- **The plate figure.** "US$X in every crisis" (Crisis.cs:91) reads as X per crisis; "in all crises so far" or "across the run's crises" would not.
- **Two Continue holds.** With the plate and election night both up, the hold chip names only the first (TopStrip.cs:183). The slip lists both.

## Traps

- **MetaTextCheck.** No `{0}'s` on a value. The possessives are on literal names ("Cruces and Trebesch's"). No banned token appears in a player string: "No 332/2002" matches no register pattern, and the arrow is the glyph. The CALENDAR FIT strings are log lines.
- **NumberLocaleCheck.** No culture is built. The fit's key and message use `InvariantCulture`, and `DateTimeFormatInfo.CurrentInfo` is the installed culture.
- **CommentClaimCheck.** `ConfidenceProcedure.RulesOf`, `SovereignCrisisState.CutBillions`, `SovereignCrisis` and `TopStripCrisisDiagnostic` all exist.
- **UiLeverVerbCheck.** The UI writes no model field. The rework only reads `GovernmentDebt` and the crisis state.
- **The 14 px floor.** No new face is below `V35.Floor`. The fit floors the dot row and the gap, never the day number.
- **CRLF.** Clean.

## Verdict

M1 and M2 are fixed in code. The fit decides from the drawing's own heights, falls back in an honest order, and the only clip left is a counted error. K-b breaks no check and double-counts nothing. L1-L9 are answered, L7 only in part. What remains is Low:
- a third hold enumeration that can drift;
- a fit margin and a USA default card that no film has shown at 1280;
- citation and wording polish.

None of it shows the player a false state today. The owed six-row-month film (N2) and the K-line for the USA's row (N3) should land with this branch's first film run, not block integration.

**Verdict:** READY
