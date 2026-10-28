# D-NIGHT review 2: the rework (wave/dnight, eaa16b13..7f60d6cd)

This is an independent delta review of c7963b4e (the column's plan, Italy's two lanes, art. 64, M2, Lows, diagnostic) and 7f60d6cd (films), checked against the first review (H1, M1-M4, L1-L7), the integrator's rulings and Design's RETURN.md. I did not run Unity. `Tools/cgate/gate.sh` on the worktree printed `GATE OK`. I read the full diff, the producer side it leans on (`SimulationManager.FormationProjection.cs`, `ConfidenceProcedure.Article94`, `ItalianFormation.Run`, `ElectionNight.At`) and the draw helpers (`Gap`, `Rule`, `Px`, `BoardPx`).

**Files reviewed:** Assets/Editor/FormationProjectionDiagnostic.cs, Assets/Scripts/Elections/GovernmentFormation.cs, Assets/Scripts/Testing/UiScreenshotDriver.cs, Assets/Scripts/UI/ElectionNightScreen.Governs.cs, Assets/Scripts/UI/ElectionNightScreen.cs (the five files the rework touched). I also read, unchanged: Assets/Scripts/Simulation/SimulationManager.FormationProjection.cs, Assets/Scripts/Elections/ConfidenceProcedure.cs, Assets/Scripts/Elections/ItalianFormation.cs, Assets/Scripts/Elections/ElectionNight.cs.

## 1. The column's plan (H1)

**Plan against draw.** I checked every literal in `ColumnHeight` (Governs.cs:982) against the block that draws it:

| block | px |
|---|---|
| head HRow | 28 |
| round | Gap 6 + HRow 24, only when the view has cells, or while counting |
| outcome | Gap 8 + Rule 1 + HRow 30 |
| reason rows | 20 each, `NoneLines` length (1 or 2) |
| support | HRow 26 |
| vote | Gap 8 + count row 26 + per lane (Gap 4 + 20), Italy's lane rows also 20 |
| key | Gap 6 + 18 |
| You | Gap 8 + Rule 1 + 30, K119 + 20 |
| Why | Gap 12 + head 26 + Gap 4 + bridge + strip 18 + (Gap 4 + 20 per mover) |
| Why, no ledger / fault | 12+26+4+24 / 12+26 |
| budget | 12 + 26 + 4 + 22 per row of three; no budget: 12+26+20 |

`Px` is `RoundToInt(x * 1.5)` and every height used is a whole number of canvas units, so units = 1.5 x px exactly. `CaseOf` derives each flag from the same predicates the blocks test: `VotesOf`, `NeedsKey`, `NoneLines`, `TryBridge`, `Movers`. The bridge draws `min(plan.BridgePx * 1.5, natural)`, so it is within a unit of the plan. The plan and the draw agree today.

They do not share constants, though. `ColumnHeight` repeats the draw sites' literals, and nothing measures the built AtRest against `plan.Final`. See N5.

**Worst cases by hand**, with the key line, K119, a 40 px bridge, two movers and seven outcomes:

- **Italy, none expected:** 28 + 30 + 39 + 40 + 82 + 24 + 59 + 144 + 108 = **554**. The shrink steps go: bridge to 24 gives 538, one mover gives 518, no movers gives **494**. This matches the implementer's 554 -> 494.
- **Sweden, none expected:** 28 + 30 + 39 + 40 + 58 + 24 + 59 + 144 + 108 = **530**. The bridge to 24 gives **514**. This matches 530 -> 514.
- **The doc comment's other figures also check:** Italy with support at 6 outcomes is 518 -> 502, Italy none at 6 is 532 -> 516, Sweden 494 and 508.

**Termination and bound.** `PlanColumn` makes six guarded one-shot steps and has no loop, so it terminates. With every step taken, the largest possible column is Italy none expected: 28+30+39+40+82+39+(12+26+4+24+18)+(12+26+4+44) = **428**. Reason rows are at most 2, lanes at most 2, and effects fold to 2 rows whatever the count. **No case can exceed 516.** The 516 x 1.5 = 774 units are also conservative against Design's 516 px measured on the 1064-unit 1280x699 canvas (785 units).

**H1: answered.** Ruling (a) is honoured: the derived note is in the outcome slip and the † view for both the expected and the none-expected case (Governs.cs:~337 and :~356), and no row is drawn for it.

## 2. Italy's two-lane block (ruling (b), M1)

- **The tick.** Each lane's tick is drawn by `LaneTick(Needed / Members)` from the for end. Italy's `Needed` is `Against + 1` (`Article94`). The for block spans [0, For/Members], so it reaches the tick exactly when For >= Against + 1, which is art. 64. The lane is correct per chamber.
- **The counting lane** (L6) now draws no tick, and its words say why ("set by the votes cast"). Sweden and Germany keep the members' majority line, which is right because it is known before the count.
- **The shared words line,** "Confidence · for, more for than against", states the rule and not a verdict. Each lane carries its own ✓/✗ and its own count. When the Camera carries and the Senato refuses, the lanes show ✓ and ✗ and the outcome row says "No government expected", so the shared line stays true.
- **The legend and the slip** (M1) now read "the tick is N for, more for than against (art. 64)" and "CONFIDENCE IN THE CAMERA: ... it carries with more for than against (art. 64)".

**M1: answered.** One small thing: `LaneWidth` (Governs.cs:454) ignores the +2 units each `FixedCell` adds (:552), so the drawn lane is 4 units narrower than the width the words were fitted to. The words' 18-unit margin absorbs this.

## 3. M2: the decliner. NOT ANSWERED

`NightVote.Decliners` and `NightVote.Fell` are declared (GovernmentFormation.cs:249). The board reads them (Governs.cs:523, :579), and so does the diagnostic (FormationProjectionDiagnostic.cs:353). **Nothing writes them.** `SimulationManager.FormationProjection.cs` is not in either commit. `VoteOf` still sets `Carried = verdict.Passes` (:203) and leaves a declining drafted partner `InCabinet` on the for side.

So the questions this review was asked cannot be checked against code, because none exists:

- whether `VoteOf` reads only the verdict's answers;
- whether moving a decliner to abstain keeps the sides summing to the chamber;
- whether against still equals `OpposedSeats`.

The original failure stands unchanged. A refused Riksdag proposal whose drafted partner declined still draws ✗ beside "140 against ... it takes 175 against to refuse it", with no reason given. Commit c7963b4e's message ("a proposal that falls on a decline says so") claims a fix that is not in the branch. See N1.

For whoever builds it:

- Read the decliners from `verdict.Answers` (`InCabinet && !Accepts`). Do not re-run `Answer`.
- Per the ruling, keep the decliner's side as it stands (do not move it to abstain), so the sums and the against clause are untouched.
- If it is moved instead, change `Sides[p]` and the counts together. `VoteFault` then still sums. Against is unaffected because the against clause applies only where a government is expected, and a fallen proposal never is.

## 4. The films (M3)

`CaptureNightColumn` (UiScreenshotDriver.cs:~5139):

- **The seat swap** is synchronous: there is no `yield` inside the `try`. It is restored in `finally` in its original insertion order. Nothing can observe the swapped chamber between frames.
- **The † view** is opened with `SetDense(true)`, asserted through `DenseShown`, then reset by `SetDense(false)`, and the screen is destroyed.
- **A view lacking a vote or a round** logs `SHOT: ... NOT filmed` and increments `_failed`. A projection that throws is logged, then falls into the same null check, so it fails loudly as well.
- **Italy:**
  - The stems carry `_staged`.
  - The staging rule is stated in the doc comment.
  - Each lane's line is `Against + 1`.
  - The staged count is declared at minute 0, so the night is `Complete` and the column is not the counting state.
  - The surface is reset on each build.
  - Every screen is destroyed after its shot.

  I found no leak into later shots.

**Partly answered.** No frame from this code exists yet: no `e6_election_night_projected`, `_dagger` or `_italy_*_staged` capture is on disk, so none has been looked at, at 1280 or at 2560. The 1280 slack concerns from the first review (the round row, three marks with "Minority, with support") remain unseen. Also see N7 on how the staged frames read.

## 5. The diagnostic's clauses and plants (M4)

- **Round plant** [Current, Refused, NotReached...] with a government expected. The old clauses accept it. Only the new "a step after the current one is reached" refuses it, so the plant isolates the new clause.
- **The new rule matches the model:**
  - `ProjectRound` returns as soon as a cell is current.
  - `FirstBallot` makes one cell.
  - `ItalianFormation.Run` breaks at the first mandate both chambers grant.

  So nothing after a current cell is ever reached.
- **Carried plant:** a government expected, `OpposedSeats` equal to the vote's against, and the vote not carried. Only the carried clause fails.
- **Against plant** (Riksdag only): `OpposedSeats = Against + 1` and carried. Only the against clause fails.
- **Italy:**
  - `ItalyVotesFault` now passes `sidesSumToMembers: true`. That is safe because `Article94` puts every seated party at ±1 and `Members` is their sum.
  - The over plant (For + 1), the swapped-chambers plant and the members'-majority line plant (stepped off `Against + 1` where the two coincide) each break one clause.
- **Column heights:**
  - Every country's tallest static cases go through `PlanColumn`, plus `WorstCaseOf` per projected view.
  - The unshrunk Italian tallest (532) is checked to exceed 516, so the clause bites.
  - It bites on the *plan* only, not on the drawn layout (N5).

**M4: answered.** One weakening: `NightFault` exempts any carried Bundestag vote without a government (FormationProjectionDiagnostic.cs:96). See N8.

## 6. The Lows and K-c

- **L1, partly.** "Elected at the first ballot / No government drawn on its party" is now said, and `RoundFault` tolerates the current cell. But see N3.
- **L2, answered.** The marks fold to "+N" by measured room, and the support row falls back to "N seats".
- **L3, answered.** A zero draws no arrow and is muted, and the 0.005 threshold matches the two-decimal figure.
- **L4, answered.**
- **L5, answered.** The Fraktion line has the Union as one block, largest first. Its legend still describes the retired bar (N6).
- **L6, answered.**
- **L7, answered.** One sprite per (ink, paper) pair.
- **K-c, answered.** "The ballot records no "no": a party that votes for no candidate abstains" is in the German slip and the † view.

## 7. Regressions and traps

The gate is OK and all five files are CRLF throughout (408/408, 592/592, 7931/7931, 1446/1446, 1071/1071). The added lines show:

- no `FontStyle.Bold` (the §648 guard);
- no `{0}'s` or `"'s` possessive (MetaTextCheck);
- every new number through `InvariantCulture` or `F()` (NumberLocaleCheck);
- no party-ink accessor, since the hatch ink is GOOD.

`Verdict`'s new size parameter compiles at every caller. The † view still holds every line the slips hold.

## New findings

### Medium

**N1. M2's producer is missing, so the decliner path is dead code** (GovernmentFormation.cs:249; Governs.cs:523, :579; FormationProjectionDiagnostic.cs:353; SimulationManager.FormationProjection.cs:203 unchanged).

Failure scenario: SD declines KD's drafted cabinet, the proposal falls with 140 against, and the night draws ✗ under a refusal line of 175 with no reason. That is exactly the first review's M2. The diagnostic's "fell on ..." fragment never prints, so no check would notice.

Fix: in `VoteOf`, fill `Decliners` from `verdict.Answers` (drafted and not accepting) and set `Fell = verdict.Reason` when `!verdict.Passes`. Draw the decliner as it stands. Add a diagnostic clause, with a plant, that a vote with `Investiture.Wins && !AllAccept` carries decliners.

### Low

- **N3. The elected-without-government slip still says "THE BALLOTS GO ON"** (Governs.cs:336, with `projection.Partial = true` at FormationProjection.cs:167 for both German outcomes). The row says "Elected at the first ballot" while the slip and the † view say the ballots go on. Key the NoneWords on the elected case, for example "A CHANCELLOR WITHOUT A GOVERNMENT DRAWN". Separately, `RoundFault`'s message when the elected case has no current cell reads "no government is expected and a cell is current", which inverts the fault.
- **N4. "Not drawn in its block" repeats per chamber** (Governs.cs:596). It sits inside the per-vote loop but iterates the whole `keys` list. For Italy with the key folded, the Camera's section lists the Senato's entries too, and both sections repeat in the slip and in †. The key cells are also not labelled by chamber ("against 150", "against 70"). Iterate only that lane's entries and prefix the chamber.
- **N5. There is no runtime parity check between the plan and the draw** (Governs.cs:982 against the draw sites). The literals are duplicated. A later change to any `Gap`/`HRow` height silently desynchronises the plan, the diagnostic stays green, and `CanvasTextGuard` cannot see it. After the build, compare `LayoutUtility.GetPreferredHeight(atRest)` with `plan.Final * BoardPx`, log a `CANVAS:` error on a mismatch, and have the films assert it.
- **N6. The Fraktion legend describes a bar that is not drawn** (Governs.cs:193): "a block per Fraktion, largest first · the tick is 316 of 630". The † view is text, so neither the blocks nor the tick exist. Keep the line, but drop "a block per Fraktion" and "the tick".
- **N7. The staged Italian frames do not read as staged at rest, and they carry Sweden's Why** (UiScreenshotDriver.cs:5205). "(STAGED)" appears only in the round's slip, and the none-expected view has no `ProjectionStep` at all. The ledger passed in is the Swedish player's, so the board says "Why <Swedish party> moved" on an Italian night. As a layout fixture that is tolerable, but a reader of the frame cannot tell. Pass `ledger: null`, or a ledger of the same size, and add a visible fixture mark to the frame or the log line.
- **N8. The Bundestag exemption hides a lost government** (FormationProjectionDiagnostic.cs:96). Any carried first ballot with `HasGovernment` false passes, so a regression where `ElectedGovernment` wrongly returns null goes green. Require the view's `Reason` to be the "no government can be drawn on its party" case before exempting it.

## Prior findings, summary

| finding | status |
|---|---|
| H1 | answered |
| M1 | answered |
| M2 | **not answered** (N1) |
| M3 | partly (code in place, no frame yet captured or seen) |
| M4 | answered |
| L1 | partly (N3) |
| L2-L7 | answered |
| K-c | answered |

The ruled M2 item is absent from the branch, and its commit message says otherwise. That alone keeps this from integration.

**Verdict:** NOT READY
