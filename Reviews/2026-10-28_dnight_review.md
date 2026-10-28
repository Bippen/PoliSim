# D-NIGHT review: WHO GOVERNS redrawn to boards 31a-31c (wave/dnight, 5c38ce1c..0387734a)

Independent adversarial review. Unity was not run. I ran the compile gate (`Tools/cgate/gate.sh` on the worktree), which printed `GATE OK`. I read the diff in full, the touched helpers (`CoalitionFormation.Evaluate`, `FormationProposal.Passes`, `ConfidenceProcedure.Article94`, `ItalianFormation.Run`, `CanvasRows`, `CanvasTextGuard`, `WordFit`, the film driver's night captures), Design's `RETURN.md`, and the ask.

**Files reviewed:** Assets/Editor/FormationProjectionDiagnostic.cs, Assets/Editor/PartyInkDrawSiteCheck.cs, Assets/Scripts/Elections/GovernmentFormation.cs, Assets/Scripts/Simulation/SimulationManager.FormationProjection.cs, Assets/Scripts/UI/ElectionNightScreen.Governs.cs, Assets/Scripts/UI/ElectionNightScreen.Governs.cs.meta, Assets/Scripts/UI/ElectionNightScreen.cs, Assets/Scripts/UI/GameController.cs

## What holds

- **The sides come from the procedure.** In the Riksdag, `VoteOf` reads `verdict.Investiture.Sides`/`Reasons`, the arrays `CoalitionFormation.Evaluate` fills, including the joint-group override. The Bundestag reads the `cast` list from the same `Tally` call that was already made with `out _`, so it is the same call and has no new side effect. Italy reads `MotionVote.Sides` from `Article94`. No side is invented. In Italy every party is +1 or -1 and the abstain count is 0, so the sides fill the chamber.
- **The projection stays side-effect free.** The new code only reads verdicts and motions. It draws no RNG and writes nothing outside the fresh `View`. The existing `Moved(...)` clause still wraps `ProjectFormation`, so the sweep and the baselines are untouched by construction.
- **The ticks are right.** The negative rule puts the tick at `(members - needed) / members`, so the refusal line sits 175 from the against end (174/349 from the left). The Bundestag counts `needed / members` from the for end (316/630). Italy's `Needed` is `Against + 1`, which is art. 64's "more for than against", per chamber.
- **Removed methods.** No caller of `BuildGovernment`, `BuildGovernmentBar`, `BuildChamberGermany`, `BuildExpectedGermany`, `ProjectionFoot`, `BuildLedger`, `BuildEstimate`, `Paint` or `Label` remains in `Assets/` (the gate compiles). No film caption or check reads the retired wordings ("A PROJECTION - YOUR ANSWER CAN CHANGE IT", "the tick a majority"). "INVESTITURE BY THE NEGATIVE RULE" survives only in the new slip, word for word as in the RETURN's table.
- **The wording table is honoured.** The step line (`ProjectionStep`), the foot (`Foot()`, with the K119 tail), the investiture sentence and the corrected legend are all kept whole in the slips and in the dagger view. `SlipWrap` breaks at word boundaries and keeps case.
- **Lifecycle and leaks.** The night is built once at its final minute and destroyed through `Root`. The dagger toggle only calls `SetActive` and rebuilds layout, so repeated toggles create nothing. The old German chamber bar's `Texture2D` was never destroyed and is now gone. The hatch sprite is one static `HideAndDontSave` object.
- **Traps.** No `FontStyle.Bold` on the night. Numbers use `InvariantCulture` with point decimals. Possessives go through `UiFormat.Possessive`. No party-ink accessor appears in Governs.cs, so the PartyInkDrawSiteCheck reason text is justified. `ToggleDense`/`SetDense` live on `ElectionNightScreen`, not on `SimulationManager`, so UiLeverVerbCheck does not apply. Every changed file is CRLF, and the meta guid is unique. The new GameController log lines run inside the existing try, use invariant formatting and only read.

## Findings

### High

**H1. Italy's column can run past its 516 px rect into the CONTINUE row** (`ElectionNightScreen.Governs.cs:131-139`, `:338`, `:370`). Every block has min height equal to preferred height (HRow, Gap, Rule, bar, bridge), so the column's VerticalLayoutGroup cannot compress AtRest, and nothing masks it. Summing the code's own heights in board px for an Italian night with a government expected:

| block | px |
|---|---|
| head | 28 |
| round | 6 + 24 |
| outcome | 8 + 1 + 30 |
| derived row (Italy is not `DeclaredRedLines.IsSourced`) | 20 |
| two vote blocks | 2 x 58 |
| you | 39 |
| why | 38 |
| bridge | 4 + 24 |
| strip | 18 |
| movers | 44 |
| budget head and six outcomes | 38 + 48 |

That totals 486. The tallest bridge adds 16 (502). A support row adds 26 (528, which is 12 over). A key line under either bar adds 24.

With no government expected, the two reason rows replace the derived row (+20). That gives 506, and 522 with a tall bridge.

Failure scenario: an Italian game whose mandates grant a government with outside support, or fail with a bridge taller than 30 px. The bottom rows (the budget outcomes) draw over the footer. Design's constraint was "nothing moves under CONTINUE". `CanvasTextGuard` will not report this, because each text keeps its own rect, so it is neither CLIP nor ESCAPE. No Italian night is filmed in `UiScreenshotDriver.cs` (only Sweden's staged and model nights, Germany's counting night, and `-shotinterrupts` takeovers), so nothing in the branch shows it fits.

Fix: drop the derived row on the night to the outcome slip, which already carries the sentence, or merge Italy's two bars into one block with two 20 px lanes (saving 34). Then film an Italian night with support and with none expected.

### Medium

**M1. Italy's legend states a false rule** (`Governs.cs:481`, also `:464`). For a for-counting vote the slip and the dagger view print "the tick is N for, a majority of the M members". In Italy N is `Against + 1` (art. 64, a majority of those present), for example 164 of a 400-member Camera, which is not a majority of the members. Failure scenario: every Italian night's slip and dagger view state a threshold the code does not use. Fix: on `v.Chamber != null`, print "the tick is N for, more for than against (art. 64)".

**M2. A Riksdag proposal refused by an invited party's answer draws as a vote it would have won** (`SimulationManager.FormationProjection.cs:203` `Carried = verdict.Passes`, and the none-expected path's `lastRefused`). `Passes` is `AllAccept && Investiture.Wins`. When a drafted cabinet party declines, `Evaluate` still marks it `InCabinet`, so it counts "for". The night then shows the cross with, say, 140 against beside the slip "it takes 175 against to refuse it", and nothing says why it fell. The game's own round (`Investiture`, SimulationManager.cs ~5030) counts that refusal the same way, so the data is faithful, but the board's grammar says the opposite of its verdict. Fix: carry `verdict.Reason` and the refusers into the `NightVote` and print them in the slip and the dagger view. Or mark the block "refused before the vote" when `Investiture.Wins && !AllAccept`.

**M3. No real film covers the new surfaces** (THE CANVAS FILM RULE). The staged Swedish model night passes `GovernmentFormation.ViewOf`, which has no `Votes` and no `Round`. It therefore films the aggregate fallback, with no round row and no per-party sides. The German staged night films the counting state. No film calls `SetDense(true)`, so the dagger view's ScrollRect, its wrapping and its head rules have never been seen. The 1280 round row (about 483 of 495 units on a mono-advance estimate) and a three-mark "Minority, with support" outcome row (about 475) have about 12-20 units of slack, which small-pixel hinting has eaten before (PF-16). Fix: add frames for a played Swedish night (per-party bar, round cells), the dagger view, and an Italian night, each at 1280 and at 2560.

**M4. The diagnostic's teeth cover one clause of four** (`FormationProjectionDiagnostic.cs:284`). Only `VoteFault`'s sides-to-counts branch is planted, using For+1. `RoundFault`, the carried-vs-HasGovernment clause, the against-equals-OpposedSeats clause and the Italian chamber clause have no plant. The Italian call (`:252`) passes `sidesSumToMembers: false`, which skips the sum checks, so in Italy `VoteFault` checks only "a seatless party", "no party on any side" and the line it already reads from the vote itself (`v.Needed`), and can barely fail. Failure scenario: a regression that pads the round wrongly, or swaps Camera/Senato sides, passes green. Fix: plant a round with a Refused cell after the current one, and plant an Italian vote one seat over its sides with `true`. Italy's sides do sum to its counts.

### Low

- **L1.** Germany, elected but with no government drawn (`FormationProjection.cs:149`, `:160`): the cell is `Current`, `Carried` is true and `HasGovernment` is false. The board shows a tick and an outlined current cell under "No government drawn on the elected". `RoundFault` would also fail ("no government is expected and a cell is current"). Rare, but contradictory. Mark the cell `Refused`, or give the round its own state.
- **L2.** An outcome row with four or more cabinet marks and "Minority, with support" (`Governs.cs:316`) exceeds about 495 units at 1280. The row has no flexible shrink, so it spills past the column edge. The same applies to a support row with four supporters.
- **L3.** A budget estimate of exactly 0 shows the down triangle (`Governs.cs:734`). Draw a dash or nothing.
- **L4.** Stale comment at `ElectionNightScreen.cs:368`: "(BuildEstimate, below)" names a method that no longer exists.
- **L5.** K-i: the old German "UNION 208 · AFD 152 · ..." Fraktion line is not in the dagger view as such. Only per-list seats in the vote's party lines survive, with CDU and CSU apart. If "retired to the dagger view" is the reading, carry the Fraktion line itself.
- **L6.** In the counting state Italy draws one empty lane at `members/2+1`, "a majority of the members". That is neither of the two per-chamber lines nor art. 64's rule. Low, because it says "owed".
- **L7.** `HatchSprite` captures the theme colours once per domain. A theme change later in the session keeps the old inks.

## The declared readings K-a..K-i

- **Defensible against the RETURN:** K-a (Design asked to "confirm or replace" its derived split, and the procedure's own sides replace it), K-b (cells known, so marked and faded rather than markless), K-d (Italy per chamber, as asked), K-e (point decimals, §568 and NumberLocaleCheck), K-f (Document weight, the §648 guard), K-g (slips keep case, word for word).
- **K-c (Bundestag "abstain" as cast)** follows the game's premise, since the tally casts no "no". It deserves a slip line saying the ballot records no "no".
- **K-h (derived note at rest)** is honest but costs 20 px that Design did not budget, and it is the row that tips Italy over (H1).
- **K-i** is only partly true (L5).

**Verdict:** NOT READY
