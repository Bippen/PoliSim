# Review - §828, the gameplay audit's Phase 4: GA-2-12, a sub-row a bill has set can be put back (2026-10-14)

One adversarial reviewer, a single read-only agent, read the change three times, each pass on the form the last one left: (1) the first form, prepared outside the tree while its control ran - the dial's draft at the statute's figure kept where the line carries its own rate, within 0.005 (`Assets/Scripts/UI/GameController.cs`, `Assets/Scripts/Simulation/ParliamentSystem.cs`, the harness); (2) B2, rebuilt on the first pass's high - the knob's step, never the 0.005 a drag cannot reach (adding `Assets/Scripts/UI/LedgerRow.cs`, `Assets/Scripts/Data/TaxLine.cs`, `Assets/Scripts/Data/BudgetBill.cs`); (3) the delta to B3 - the knob's detent and the track's reach. Their reports follow verbatim; then what was done with each finding.

## The first pass (the first form)


**Verdict: NOT READY.** The merge, the builder and the null and determinism handling are sound. But the restore cannot be reached from the dial the player drags in the ledger's own case (Sweden's flat layer back to 32.38), or for any row once the lever's shift is off the dial's grid. The dial snaps to a grid of 0.1 or coarser, and the restore test is ±0.005. X-02R hides this because it passes the exact statute figure.

Line numbers: "B/" is the proposed copy; Editor files are the working tree; everything else is HEAD.

### Findings

**1. HIGH: the restore can't be reached by dragging the dial (it snaps to a grid).**
- The sub-row dial runs through `LedgerRow.Draw` and then `Track`. A moved knob snaps to `Round(result/step)*step` (LedgerRow.cs:343), and the step is never finer than `SnapStep` = 0.1 (LedgerRow.cs:54, 66-71, 322). Film logs record 0.21-0.84 units/px on Budget rows, which means a 0.5 step at 1280.
- `DragSubRow` then needs |result − draftShift − StatuteRate| < 0.005 (B/GameController.cs:9661, 9676).
- With the lever untouched (shift 0):
  - Sweden's flat layer is 32.38 (TaxSchedule.cs:141). The nearest grid values are 32.4 / 32.5, which are 0.020 / 0.120 off.
  - Germany's first ramp end is 23.969988 (TaxSchedule.cs:131, 144). The nearest grid value is 24.0, which is 0.030 off.
  - Neither is ever "at the statute". The drag leaves an own rate of 32.4 or 24.0, the bill carries it, and the merge keeps it (B/ParliamentSystem.cs:179). `OverridesKey` stays non-empty, and since 5a600bb8 the burden term reads the leftover. That is the ledger's own symptom, still there after the fix.
- Rows with integer statutes restore only while statute + draftShift sits on the grid:
  - Any lever move in Germany, France, Poland or Sweden pushes the shift off the grid. Their seeds are 26.92, 17.39, 12.36 and 32.38, and the lever's own dial snaps to the same 0.1/0.5 grid.
  - A Finance partner's boundary step sets the rate to an arbitrary figure (GameController.BudgetV35.cs:525-527).
  - After either of these, no sub-row can be restored. In Sweden, a lever moved on its grid makes the flat layer reachable and the state band unreachable.
- This is the same class of problem as PN-1's "§590 review F2" (B/GameController.cs:10217: 62 y 9 m could not be reached on the 0.1 grid).
- **Remedy:** decide "at the statute" at the dial's own resolution, as PN-1 does with half a month (B/GameController.cs:9515, 10220).
  - Pass the step the track snapped to into `DragSubRow`, and treat |wanted − statute| ≤ step/2 as the statute. The draft is then written as the exact `(float)r.StatuteRate`, so the bill and merge tests at 0.005 hold.
  - `Draw`/`Track` must hand back that step. `LedgerRow.LastStep` is written only on Repaint, and only for the last row drawn (LedgerRow.cs:290).
  - Optionally, draw a ghost tick at the statute on rows that carry their own rate.
  - Add a check: floor Sweden's flat layer, drag it to the nearest grid value, pass the bill, and confirm `BracketRates` becomes null. Repeat with the lever drafted off the grid (e.g. 33.0).

**2. MEDIUM: X-02R tests a path the player can't take, and the planned recheck can't catch finding 1.**
- AuditDriver.Styles.cs:189 passes `(float)rows[i].StatuteRate` straight into `DragSubRow`, so `Track`'s snap is skipped. p4_ga2_12.plan will report the restore working in all five countries.
- Two comments overstate the match with the real dial:
  - `DragSubRow`'s summary says "so the two cannot drift apart" (B/GameController.cs:9670).
  - The harness summary calls X-02R "a player's restore" (AuditDriver.Styles.cs:173).
- After a restore passes, the line is null and `back` is false (Styles.cs:180), so the next window floors again. The run alternates floor and restore. A 730-day run with a third window ends floored, which the plan's "returns to the seed's" expectation does not allow for.
- A restore is a large tax rise: `BracketRateMove` is positive and the hike penalty applies. It may lose its vote under M, PiS or RN. In the TAXES line a lost vote looks exactly like the bug, and the introduce event (AuditDriver.cs:1010) does not print the bill's sub-row figures.
- **Remedy:**
  - Drag through the dial's rule: wanted = snap(statute + shift, step) − shift, at 0.1 and 0.5, plus the step-aware `DragSubRow` from finding 1.
  - Print `bill.BracketRates` in the introduce event.
  - Read the division for each restore bill.
  - Either state the floor/restore alternation in the plan or stop X-02R after one restore.

**3. LOW: dragging back to the statute while the player's own sub-row bill is pending is silently dropped.**
- The dials stay live while a budget bill is pending: `live` checks only the tax-programme bill (B/GameController.cs:9561). `DragSubRow` decides on the line as it stands now (9677-9678).
- Sequence: a floor bill is pending, the player drags back to the statute, and the draft is removed because the line has no own rate yet. When the floor passes, the restore intent is gone.
- **Remedy:**
  - Have `DragSubRow` always keep the exact statute figure and never drop it.
  - Leave "asks for nothing" to `BuildBudgetBillFromDrafts` (9470-9471). It runs at build time, and only one budget bill can be pending at once (SimulationManager.cs:1509).
  - The dial's cue stays quiet: hasDraft compares statute + shift on both sides.

**4. LOW (pre-existing, next to this change): kept drafts at the standing own figure still count as changes.**
- After a floor passes, its drafts stay (only `RestoreUiDrafts` clears them, B/GameController.cs:993-995). The builder converts only drafts at the statute (9471).
- So every later bill carries the own figure again, and `BudgetDraftChanges` counts each r ≥ 0 (GameController.BudgetV35.cs:120). The strip prints changes the bill would not make.
- `ChangesTaxRates` (BudgetBill.cs:97-98) and `TaxActTitle` (SimulationManager.cs:1426) already ignore them.
- **Remedy:** the builder also writes −1 where the draft equals the line's own figure (≤ 1e-6). This is the lever's GA-8-2 rule (BudgetV35.cs:528).

**5. LOW: docs now stale or inaccurate.**
- TaxSchedule.cs:562-565: "every entry −1, the form a bill leaves" is now wrong (a bill leaves null). "A sub-row carries a figure, even the statute's own" can no longer come from a bill.
- TaxLine.cs:136-138: "Null on every line but the income tax's with a statute" is wrong. The income line is null as seeded, and null again once every sub-row is put back.
- BudgetBill.cs:49: "−1 where the statute's figure stands" should now read: −1 keeps the standing figure; the statute's own figure puts the statute back.
- B/ParliamentSystem.cs:167: "within the dial's 0.005" is misleading. The dial's resolution is its snap step; 0.005 is only a float tolerance.
- **Remedy:** reword all four.

**6. LOW: the tolerance appears as three separate literals.**
- 0.005f at B/GameController.cs:9471, 9676 and B/ParliamentSystem.cs:179. They agree today: each compares a float against `(float)StatuteRate` with `<`.
- **Remedy:** one named constant (e.g. on `TaxSchedule`) used by the dial, the bill and the merge.

### Verified OK
- **Tabled and passed:** `ChangesTaxRates` reads a restore request (statute vs own 0) as a change, and a stale draft converted to −1 as no change. So in Poland the tax act is split out for a restore and not for a stale draft (`BudgetActOf`). The merge returns all −1, then null (B/ParliamentSystem.cs:179-183). The line then reads exactly as seeded:
  - `OverridesKey` is "" (TaxSchedule.cs:535).
  - Germany's tax uses § 32a again (TaxSchedule.cs:279).
  - `YieldNow` equals `YieldNowAtStatute` to the bit, so the burden gap from the sub-rows is zero.
- **Weighed:** `BracketRateMove` measures the sub-rows' move at the bill's lever (B/ParliamentSystem.cs:187-195). A restore of a cut adds positively to the direction (200) and pulls Redistribution toward 0 (938). `ApplyBillResult` measures it after the lever is applied, adds it to `totalHike`, and the approval penalty follows (505-511). That matches the lever's convention (rises are penalised, cuts are not rewarded), so it is right.
- **Act title:** a restore is named "income tax's schedule" (SimulationManager.cs:1418-1429); a converted stale draft is not named.
- **After a restore passes:** the stale draft becomes −1. That means no change, no count, no Polish act, no cue on the dial, and no visible curve split. Germany's draft side reads the ramps against the formula, which differ by about a euro, under the 0.5 px threshold.
- **Flooring again after a restore:** the clamp replaces the draft. Dragging back with no own rate drops it.
- **Builder timing:** only the player's budget bill writes `BracketRates` (B/ParliamentSystem.cs:509). Only one budget bill can be pending per country (SimulationManager.cs:1509). AI and partner bills carry none (AiFinanceMinistry.cs:116, SimulationManager.cs:2736). So the line at build time is the line at passage, and no draft is wrongly converted while an own rate stands.
- **Null-tolerant everywhere:**
  - `TaxLine.Clone` (TaxLine.cs:194) and `ClonePreviewTaxLines` (SimulationManager.cs:5789); the preview runs `ApplyBillResult` on the clone (5219, 5511).
  - The PreviewParity clone audit (Country fields only).
  - Newtonsoft with the default `NullValueHandling.Include` (SaveGameService.cs:95-104).
  - `TaxSchedule` `RowRate`, `EffectiveRowRates`, `Tax`, `HasActiveOverride` and `OverridesKey`; `ChangesTaxRates`; `TaxActTitle`.
  - The harness's TAXES readout (AuditDriver.cs:634) and `DraftSubRows` (180, 188).
  - B/GameController.cs:9470, 9611, 9642, 9677, 9688.
  - Nothing assumed a non-null array after a merge.
- **Determinism:** no AI or partner path writes or requests `BracketRates`. Without sub-row drafts the direction, concern and apply loops are empty, so the no-policy dumps cannot move. X-02S drafts are identical to the old harness rule (the no-own-rate branch is the old rule, and a floor never sits within 0.005 of a statute); only the STYLE event's wording changed.
- **Tolerance and rows:**
  - The three tests agree.
  - Exempt and levy rows get no dial, and `RowRate` ignores any entry there.
  - Restoring a ramp END restores the next ramp's start (`BandRates`).
  - Germany's ramp-2 end is 41.99975, so a dialled 42.0 reads as the statute, which is harmless.
  - No grid value lies within 0.005 of a statute without being it, so no deliberate figure is lost.
- **Checks:**
  - No new string literal under MetaTextCheck's roots (Scripts/UI, Scripts/Data); "X-02R" sits in Editor files.
  - § appears only in a comment.
  - The one backticked Type.Member, `ParliamentSystem.MergedBracketRates`, exists.
  - No name collisions in the builder.
  - `MergedBracketRates` has no other callers.

## The second pass (B2)

**Verdict: ready to land once one MEDIUM gap is fixed or recorded.** A real drag now restores every adjustable sub-row in all five countries. This holds at steps 0.1, 0.5 and 1, with the lever unmoved or moved off-grid, ramps included, wherever the statute's figure plus the lever's shift lies on the dial's 0-50 track. The merge, the builder, `LastStep`, null handling, determinism and both checks are sound. The gap: when the lever's shift pushes a statute off the track, a drag cannot restore it.

`B2/<file>:n` points to the proposed copy; other references are HEAD.

### Findings

**1. MEDIUM: a statute pushed off the 0-50 track by the lever's shift cannot be restored by a drag.**
- The dial runs 0-50 in dial units (rate + `draftShift`) (B2/GameController.cs:9658). The slider and Track clamp the knob to that range (B2/LedgerRow.cs:343). The half-step test is the only reach (B2/GameController.cs:9680).
- So a row restores only while −step/2 ≤ S + shift ≤ 50 + step/2. Thresholds use the printed seeds (DE 26.92, FR 17.39, PL 12.36, US 17.10, SE 29.159), at step 0.1:
  - Top bands are lost above a lever of about: DE 45 % at 31.97; FR 45 % at 22.44; FR 41 % at 26.44; PL 32 % at 30.41; US 37 % at 30.15.
  - Bottom rows are lost below about: DE ramp-1 end at 2.90; FR 11 % at 6.34; PL 12 % at 0.31; US 10 % at 7.05; SE state band at 9.11.
- Raising France's lever by about five points is enough. Tabulated examples: DE at lever 33 gives the 45 % band T = 51.08 (fails at all three steps); US at lever 31 gives the 37 % band T = 50.9.
- The drag then lands on the track's end, more than half a step from the statute. The bill writes that end as the line's own rate, which is exactly GA-2-12's leftover. X-02R leaves the lever where it stands, so the planned runs will not see this.
- **Workaround:** bring the lever draft back into range, drag the sub-row, then move the lever back. The sub-row draft is stored without the shift, so it survives the lever moving.
- **Remedy (fix):** in `DragSubRow`, compare against the statute clamped into the track in rate terms, i.e. `Mathf.Clamp(S, 0 − draftShift, 50 − draftShift)`, and store the exact S on a hit. Pass `draftShift` from the dial and from the harness. Add an X-02R run with the lever drafted about +6 (DE or FR).
- **Remedy (record instead):** put the limit and the workaround in the ledger row.

**2. LOW: the slider's step tick repeats while the knob is held on the statute's cell.**
- The magnet pins the draft at S (B2/GameController.cs:9681), so the knob value S + shift sits off the grid. Every later MouseDrag in that cell, vertical motion included, snaps to a grid point that differs from the draft. That fires `AudioDirector.FireStep` (B2/LedgerRow.cs:343-344), up to once per 40 ms (AudioDirector.cs:125).
- In HEAD an off-grid knob value lasted one event. Runs are muted, so no plan will hear it.
- **Remedy:** give `Track` an optional detent (S + `draftShift`) and return it when the snapped value is within half a step. The knob then equals the draft, so there is no tick and no re-pin.

**3. LOW: the half-step test is an exact `<=` on float arithmetic.**
- A target exactly halfway between two grid points could fail on both sides by float error.
- Germany's first ramp end comes close. With the 26.92 seed as printed and any lever on the 0.1 grid, T sits 1.2e-5 inside the half step (lever 30: T = 27.049988 against 27.0). Float32-emulated, it holds by about 6 ulps.
- **Remedy:** use `<= step * 0.5f + 1e-4f`. An exact halfway then catches both neighbours, never neither.

**4. LOW (existing problem, now hit more often because drafts are never dropped): Germany's draft curve can draw "draft" dashes where nothing changes.**
- `DraftBracketRates` puts every kept draft into the draft overrides (B2/GameController.cs:9685-9692). A draft at the statute is now kept even with no own rate, and stays after every restore.
- That turns `HasActiveOverride` on, so the draft curve reads the ramps while the standing curve reads § 32a (TaxSchedule.cs:279).
- With indexed thresholds (after the first boundary), the formula's marginal rate over 100 € jitters by up to ~1 % of the rate because incomes are floored to whole seed-euros. That is ~0.4 points at 42 %, against a 0.5 px gate of 0.31 points at 1440p (StatsUnit = H/720).
- **Remedy:** build the draft side the way a bill would leave the line, e.g. `MergedBracketRates(taxLine.BracketRates, <the builder's requested row>, rows)`. A draft that asks for nothing then draws no draft curve, and a full restore draws § 32a.

**5. LOW: the doc on `TaxLine.BracketRates` contradicts itself.**
- B2/TaxLine.cs:138-140 keeps "Null on every line but the income tax's with a statute" and adds "carries null again". In fact the income line is null as seeded (nothing seeds it) and null again after a full restore.
- **Remedy:** reword to "null until a passed bill sets a sub-row, and again once every sub-row is put back".

**6. LOW (optional; HEAD behaves the same): the sub-row dial lacks the pension dial's pull back to the standing figure.**
- PN-1 also clears a draft that lands within half a month of the standing figure (B2/GameController.cs:10222). The sub-row dial does not.
- So with the shift off-grid, an own figure cannot be dragged away and back exactly. The bill then names a change of about 0.03 points, which in Poland is a separate tax act.
- **Remedy if wanted:** after the statute test (statute first), treat a landing within half a step of the line's own figure as that exact figure. The builder (9471-9472) then asks nothing.

### Verified OK
- **(a) Reachability.** Rows checked: DE ramp ends 23.969988 and 41.99975 and bands 42/45; FR 11/30/41/45; PL 12/32; US 10-37; SE 32.38/20.
  - For any target on 0-50, a grid point lies within half a step at 0.1, 0.5 and 1, since 0 and 50 sit on every grid.
  - Track can rest on every grid point (the film's 1 unit/px bound).
  - All passed at the seeds and at off-grid levers: DE 30, FR 20, PL 15, US 20, SE 29.159 (seed) and 30.
  - SE's 32.38 is reached via 32.5 at step 0.5 (0.12 off), 32.4 at 0.1 (0.02) and 32 at 1 (0.38).
  - A ramp's dial is its END plus the shift, and restoring an end restores the next ramp's start.
  - At the seed, no statute sits exactly halfway between grid points.
- **(b) Magnet.** It takes away only the one grid value nearest the statute (e.g. 32.5 at 1280) and shows the statute's own figure ("32.38%") in its place, so nothing is hidden. Acceptable. It is PN-1's half-step idea, but PN-1's statutes sit on its month grid, so PN-1 loses no value (see finding 6 for the other half).
- **(c) `LastStep`.**
  - It is written on every event (B2/LedgerRow.cs:221-223) from the same rect and grain Track snaps with (322, 343). It is read straight after that sub-row's own Draw (B2/GameController.cs:9658-9662), so it is the step of that row on the event the knob moved.
  - On Layout the rect is a dummy (step 1), but the slider does not move on Layout, so `DragSubRow` is not called.
  - Each sub-row lays out its own columns, so steps can differ per row, and `LastStep` follows each.
  - The income lever is drawn by Track (BudgetV35.cs:521), which does not write `LastStep`. Nothing else reads `LastStep` or `LastGrain`.
- **(d) Builder (B2/GameController.cs:9470-9472).**
  - A kept draft equal to the own rate gives −1. S over an own X gives S (the restore). S with no own rate gives −1. Anything else carries the draft.
  - The strip (BudgetV35.cs:120) now counts only real changes, which fixes old finding 4.
  - `TaxActTitle` (SimulationManager.cs:1418-1429) and `ChangesTaxRates` (BudgetBill.cs:97-98) name only the restore, so Poland's Rates act splits off only for that.
  - The preview builds the same bill and applies it to a clone. `DraftFingerprint` is value-based, so the magnet's re-pin triggers no recompute.
  - A restore dragged while a floor bill is pending survives (old finding 3 fixed).
- **Merge.** `IsStatuteFigure` uses `Approximately` against the same float the drag stores (B2/ParliamentSystem.cs:189). All −1 becomes null (180-184). The rise, the bill's direction and the redistribution reading behave as reviewed for B.
- **(e) Null handling and determinism.**
  - Null handling is as in B, and `DragSubRow` no longer reads the line.
  - No AI or partner path writes `BracketRates`.
  - No-policy runs cannot move.
  - X-02S drafts (snap 0) are identical to before. Its later bills now carry −1 where HEAD repeated the line's own figure, but the merged array, `BracketRateMove` (0) and `ChangesTaxRates` (false) come out the same, so trajectories match. Only event wording differs.
- **Harness.**
  - Snap matches Track: round, clamp to 0-50, take the shift off (B2/AuditDriver.Styles.cs:193).
  - The tabling event prints the sub-rows (AuditDriver.cs:1014), and divisions are already logged PASSED/FAILED (AuditDriver.cs:674).
  - `snap=` is parsed with the invariant culture.
- **(f) Checks.**
  - Every backticked Type.Member in the new comments exists: `ParliamentSystem.MergedBracketRates`, `LedgerRow.Draw`, `LedgerRow.StepFor`, `TaxLine.BracketRates`. Private members are not backticked.
  - No new string literal under Scripts/UI or Scripts/Data, and § appears only in comments.
  - TaxSchedule.cs:562-565 stays stale until GA-2-13's commit.

## The third pass (the delta to B3)

**Verdict: READY.** B3 deals with every B2 finding: 1-5 are fixed and 6 is declined and recorded. I found no regression, loop or visual change for rows inside the track. Three LOW items remain, all optional:
- a narrow numeric corner that still ticks;
- one doc example that is misstated;
- one gap in what the planned runs cover.

The tree equals B3 for all seven files (checked with a CRLF-normalised diff). Line numbers below are B3's.

### Findings

**1. LOW: the knob and its detent are computed by two different float paths, and they can disagree near the bottom of the track.**
- The knob comes from `draftRates`, which `EffectiveRowRates` computes in double: `(float)(S + shift)` (GameController.cs:9650).
- The detent is computed in float: `(float)S + draftShift` (9651, and `onTrack` at 9675).
- For a statute that float cannot hold exactly, the two differ by about the rounding error of `(float)S`: 1.5e-7 for Germany's first ramp end, 1.07e-6 for Sweden's 32.38. `Mathf.Approximately` (relative 1e-6) absorbs that everywhere except where S + shift is close to 0.
- I emulated it in float32. Germany's ramp-1 end with the lever at 3.0 (seed 26.92 as printed) gives knob 0.049987942 and detent 0.049987793. Those are "different", so while the knob is held on the statute, every mouse event re-snaps, ticks and calls `DragSubRow`, with no change of state. At lever 3.5 and above the two agree.
- The corner is only reachable with Germany's lever cut to about 3-3.2.
- **Remedy:** compute the detent on the knob's own path, `Mathf.Clamp((float)(r.StatuteRate + draftShift), 0f, SubRowTrackTop)`. For a draft at the statute, the knob and detent are then the identical float.

**2. LOW: the example in the `DetentSlack` doc is misstated (LedgerRow.cs:659-660).**
- It says "Germany's first ramp end with the seed's lever … falls inside by 1.2e-5". At the seed's lever the shift is 0, so T = 23.969988, which is 0.030 from the nearest 0.1 grid point and nowhere near half a step.
- The 1.2e-5 margin happens with the lever on the 0.1 grid (seed 26.92 as printed, e.g. lever 30: T = 27.049988).
- **Remedy:** reword to "with the lever on the 0.1 grid".

**3. LOW (test coverage): `leverdelta=6` gives an integer shift.**
- The shift is (float)(seed + 6) − seed = 6 ± 2e-6, so every whole-number band's restore target stays on the grid.
- The planned DE/FR runs still exercise:
  - the off-track end (the 45 % bands at 51);
  - Germany's off-grid ramp ends;
  - Sweden's 32.38 at shift 0.
- No run exercises a whole-number band with an off-grid shift, which is the PL/FR/US/DE case after a lever move.
- **Remedy:** make one run fractional, e.g. `leverdelta=6.37`. That gives DE 45 at 51.37 (off the track) and FR 30 at 36.37 (off the grid).

### Verified OK
- **(a) The detent stays local.**
  - `Snap` (LedgerRow.cs:653-657) captures only a grid point within step/2 + 1e-4 of the detent. Grid points are a full step apart, so that is one cell, or two only when the detent sits within 1e-4 of a cell boundary.
  - The comparison uses the already-clamped grid value, and a hit returns the clamped detent. So a detent at or past an end captures only the end cell. A detent outside [min, max] can only return the end itself.
  - With the default NaN, `Snap` evaluates `Mathf.Clamp(Mathf.Round(v / step) * step, min, max)`, the exact old expression in the same order. The new parameter is last and optional. So the other `Track` callers (BudgetV35:521/705, LawsV35:343/1223) and every other `Draw` call behave identically.
- **(b) No visual change from the clamped knob.**
  - `Draw` uses its draft only to hand it to `Track` (LedgerRow.cs:295).
  - `DrawTrackFurniture` already clamps both markers with `Clamp01` (LedgerRow.cs:497-498), so the standing tick, the hatch and the thumb draw exactly as before.
  - The figure texts and `hasDraft` use the unclamped rate (GameController.cs:9646-9655).
  - There is no phantom move: the slider returns the value it was given on Layout and Repaint, and on a click on the thumb.
  - B3 also removes a latent B2/HEAD phantom. Unity's slider starts a drag from the clamped value, so in B2 a click-and-wiggle on a past-the-end knob (say 51.08) wrote the end value. Now the knob is that clamped value, so nothing moves.
- **(c) `DraftBracketRates` through the merge (9687-9688; `RequestedSubRows` 9693-9703, shared with the builder at 9463).**
  - **No drafts:** returns the line's own array (or null), so the draft curve and `draftRates` match the standing except for the lever.
  - **Pending restore:** −1 at that row, so the dial prints S + shift and shows the cue. A full restore draws § 32a against the standing ramps, which is a real difference.
  - **Pending floor:** prints 0 + shift and shows the cue.
  - **Kept stale drafts after a restore:** returns null, so § 32a is drawn on both sides, with no dashes and no cue. B2's finding 4 is fixed.
  - **Lever moved:** every row shifts and shows the cue, as before.
  - The two consumers, `EffectiveRowRates` and `MarginalRate` (9602, 9633), both accept null.
- **(d) No loops.**
  - `DragSubRow` runs only when the snapped result differs from the knob, which happens only on mouse events.
  - Held on the statute, `Snap` returns the detent, which equals the knob within `Approximately` (exactly, after finding 1), so there is no `FireStep` and no call.
  - Entering the statute's cell ticks once.
  - A past-the-end draft at the statute has knob = detent = the end.
- **(e) The harness matches the dial.**
  - It uses the same detent formula with the same shift (drafted lever − seed), and the same `Snap`. Rounding before the clamp gives the same end value as the slider clamping first.
  - `DragSubRow` gets the same `(landed, step, shift)`. The reflection cache key becomes "DragSubRow/5".
  - The unsnapped path clamps to the track.
  - `leverdelta=` drafts seed + delta at floor windows only. The draft persists, so restore windows see the same shift. The bill names the lever only while it differs from the line's rate.
- **(f) Checks clean.**
  - The only new backticked Type.Member names are `LedgerRow.Snap` and `ParliamentSystem.MergedBracketRates`, and both exist.
  - The new string literals are in Editor files only, outside MetaTextCheck's roots.
  - The `TaxLine` doc is now accurate (TaxLine.cs:136-140).
- **Unchanged from B2:** `BudgetBill.cs`, `ParliamentSystem.cs`, the `LastStep` behaviour, null handling and determinism.

## What was done

**The first pass:**
- **1 (HIGH, the knob's grid): fixed.** B2 judged "at the statute" at half the knob's own step, and B3 made it a detent in the knob (`LedgerRow.Snap`). The grid point nearest the statute's figure returns that figure exactly, as half a month stands for the statute on the pension-age dial.
  - The second control, `p4_ga212pre2`, ran the old rule with the harness's drags on the knob's grid. It measured the finding as a player meets it: Sweden's flat layer came back to 32.5 and stayed a rate of the line's own, and its state band's drag, landing exactly on 20, was dropped and left the band at 0.
  - The first form was measured once (`p4_ga212`: exact figures, where the 0.005 test holds). The restore bills passed, the line went back to null, and area 2 was byte-identical to `p4_ga211`. That form is superseded, so its plan is withdrawn and not committed.
- **2 (MEDIUM, the harness bypassed the snap): fixed.**
  - `snap=` lands every drag where the knob does, and the plans run the 1280 step (0.5) and the widest film's (0.1).
  - The budget window's tabling event prints the bill's sub-row figures, and each restore's division is in the run's events.
  - The plans state the alternation: X-02R floors at one window and, once the floor has passed, restores at the next.
- **3 (LOW, a restore dragged while a floor bill is pending): fixed.** `DragSubRow` never drops a draft.
- **4 (LOW, kept drafts counted as changes): fixed.** The bill names a sub-row only where its draft moves the line (one helper, which the draft curve reads too).
- **5 (LOW, stale comments): fixed** in `TaxLine`, `BudgetBill` and `ParliamentSystem`. `TaxSchedule.cs`'s comment is GA-2-13's file and goes with that commit.
- **6 (LOW, one tolerance written three times): done.** There is now half the knob's step at the knob and the draft, and exactness at the merge, since the drag stores the statute's figure exactly.

**The second pass:**
- **1 (MEDIUM, a statute the shift carries off the track): fixed.** The statute is taken as far as the track reaches it, in the knob's detent and in `DragSubRow`. The plan runs X-02R with the lever drafted 6 points up in Germany and France (`leverdelta=`).
- **2 (LOW, the tick repeating on the statute's cell): fixed.** The detent returns the statute's figure, the knob rides the track (the draft is clamped to it, the printed figure stays the rate), and the dial compares against that knob. Held on the statute, the knob is the draft.
- **3 (LOW, the half step's float edge): fixed.** `LedgerRow.DetentSlack` is 1e-4.
- **4 (LOW, Germany's draft curve dashing a difference no bill would make): fixed.** The draft overrides are the line as the bill would leave it (`ParliamentSystem.MergedBracketRates` over the requested figures).
- **5 (LOW, the `TaxLine.BracketRates` doc contradicting itself): fixed.**
- **6 (LOW, optional, no pull back to the line's own figure): declined.** It is pre-existing, and HEAD behaves the same. A second detent on the shared knob is a design question. A drag away and back can name a change of a few hundredths of a point.

**The third pass:**
- **1 (LOW, the knob's figure in double against the detent in single precision): fixed.** The detent is summed as the dial's own figure is, `(float)(statute + shift)`, in the dial, in `DragSubRow` and in the harness.
- **2 (LOW, the slack comment's example): fixed.**
- **3 (LOW, no fractional lever): done.** `p4_ga2_12c` drafts the lever 6.37 points up in Germany and France.
