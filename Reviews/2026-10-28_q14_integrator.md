# wave/q14: the integrator's reading at the merge (2026-10-28)

**Why this exists.** q14 was reviewed twice on its branch (`Reviews/2026-10-28_q14_review.md`, NOT READY: H-1 the floor above the natural rate, M-1 the supply excess, M-2 France's seed; `_review2.md`, READY at 67e7e497 with N-1..N-4 Low). The last round, cc7f2e94, answered N-1..N-3 after that review; this file reads that delta and the merged state.

**Files reviewed:** `Assets/Scripts/Simulation/MacroSystem.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/UI/GameController.cs`, `Assets/Scripts/UI/GameController.BudgetV35.cs`, `Assets/Scripts/UI/GameController.Statistics.cs`, `Assets/Editor/UnemploymentFloorPhillipsDiagnostic.cs`.

**The last delta (6808bf6b..cc7f2e94).** `MacroSystem.EffectiveNaturalRateChange(country, delta)` and `SimulationManager.ProjectLineNextYear(country, line, inflation)` are pure reads: each computes a figure from the country's state and returns it, writing nothing (no field, no ledger, no stream). The first serves the law card's EXPECTED EFFECTS line (the room above the floor a natural-rate act delivers under K199); the second serves the Budget row's and the Statistics total's NEXT YEAR projection through the same `PensionNominalGuarantee` the boundary applies (K201), returning `SpendingLine.ProjectNextYear`'s figure unchanged wherever the guarantee does not bind. Neither is reachable from the no-policy path. The diagnostic's section (g) drives the real `IndexSpendingLines` by reflection on a 2 % fall in the price level and fails if the guarantee's call is removed (N-3).

**The merge.** `git merge --squash wave/q14` on 266308ae conflicted only in `.gitattributes` (Italy's sourcing and q14 each appended `-text` lines); both blocks were kept and `git check-attr` reads `text: unset` for both trees. `MacroSystem.cs` and `SimulationManager.cs` on main equal the branch's tip. `perl Tools/unemployment_floor_prep.pl` regenerates `UnemploymentFloorData.cs` byte-identical (419389de...); each raw folder's SHA256SUMS passes. The compile gate printed GATE OK.

**The named run (n928):** 12 of 12 clean - UnemploymentFloorPhillipsDiagnostic PASS, NaturalRateCompositionDiagnostic, JobsLag, the two indexation diagnostics, PreviewParity, PriceLevel, ExpectationsAnchoring, AggregationEquivalence and the three text checks.

**The family `mf14` against `mf13` (dump928; `Tools/traj_diff.pl mf13 mf14 <seed> 100 first`).** Every country first differs at turn 1, through the convex Phillips slack. France moves most: its seed is 7.45 (K200) and its natural rate is floored at 7.36, so at turn 1 unemployment reads 7.79 (7.33 in mf13) and at turn 100 7.37 (7.32 - below the floor in mf13); a century of slightly higher unemployment compounds into France's debt at turn 100 9419.9 against 7626.3 (+23.5 %, seed 777). Elsewhere at turn 100 on 777 the moves are small: Germany debt +0.4 %, Italy +1.9 %, Poland +2.7 %, Sweden +0.6 %, the USA -4.4 % (its inflation path lower). Unemployment at turn 100 moves by under 0.04 points outside France.

**The Jensen drift (the q14 implementer's ask 4).** Mean inflation over turns 1-100, mf13 -> mf14, seed 777: France 2.027 -> 1.946, Germany 2.126 -> 2.127, Italy 2.099 -> 2.107, Poland 2.695 -> 2.724, Sweden 2.140 -> 2.148, the USA 2.080 -> 2.029; seed 424242 the same pattern (France -0.077, the USA -0.064, the rest within 0.03). No systematic upward drift: the log slack's lower mean under fluctuation is outweighed by where each country sits against its natural rate.

**N-4 (bit-identity of the float local, by reading).** Declared, not measured: the control dump the review proposed (France back at 7.3 against the pre-fix family) was not run; the merged tree's own family dump is the measurement this commit pins.

**The sentinel:** `BaselineLabel` = mf14; turn-20 digests 777 ac504769..., 424242 8367215c... (the perl reproduction checked against form's pins earlier today).

**Verdict:** READY, subject to the films (the law card and the Budget screen) and both bars on this tree.
