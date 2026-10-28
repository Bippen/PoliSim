# wave/refit: the integrator's reading at the merge (2026-10-28)

**Why this exists.** Regional 5a (§936) made every campaign result move. Since then Gate18Diagnostic has reported the stored record factors (`Assets/Scripts/Elections/RecordFactors.cs`) DUE A REFIT, the largest relative move being 2.8e-2. PS-8 (§938) carries those same factors into every AI country's count (K160). The refit was built on `wave/refit` (52ad1640). Its review, `Reviews/2026-10-28_refit_review.md` (23848445), was READY, with one condition on merge order: the branch adds a clause that fails while a refit is owed, but does not refit, so the refit has to land with it.

**Files reviewed:** `Tools/record_factor_refit.pl`, `Assets/Editor/Gate18Diagnostic.cs`, `Assets/Scripts/Elections/RecordFactors.cs`, `Assets/Editor/CheckSuite.cs`, `docs/RULINGS_2026-10-23.md`.

**The merge and the refit.** `git merge --squash wave/refit` on c2cc6610 (§938 landed) merged without conflict, and GATE OK. The review's procedure was then run on main:

1. **g18r1** (Gate18 named): the new clause failed all four elections by name, as intended.
   - Sweden2026: 2.1e-2 (M)
   - Germany2025: 8.7e-4 (SPD)
   - Poland2023: 2.8e-2 (PiS)
   - Italy2022: 3.0e-3 (AVS)
2. `perl Tools/record_factor_refit.pl g18r1.log --record s940` moved 31 factors and found 12 already at the fit, over 4 elections. Every row is commented "[FITTED] refit s940 from g18r1, the fit's fixed point". The result has sha256 db4987c1... Italy2022 FdI, for example, went from 2.75561 to 2.76376.
3. GATE OK.
4. **g18r2:** all four read "the stored factors are the fit (the largest relative move 0.0e+0)", and the run ends `=== Gate18Diagnostic: the gate holds ===`. `--check` on g18r2.log gives CHECK OK (43 rows, 4 elections, the same sha256).

The refit settles in one pass, as the review verified from the code: the fit starts from the factors-free shares, and no table election's Gate18 window reaches another's polling day.

The integrator also corrected Gate18's CheckSuite comment, which named three elections instead of four (the review's L4), and added that the gate now fails a refit owed.

**The measurement (this tree, s939).**
- **dumprf** (family `refit`) against `ps8`: Sweden first differs at turn 33, Poland and the USA at turn 34 through trade. By turn 100, 146 of 468 readings have moved.
  - Turns 1-20 are byte-identical, so the sentinel's 20-turn digest is unchanged and is not re-pinned.
  - Sweden's 2026 count, which uses the refit Sweden2026 factors, lands the same seats and government. A 2.1 % move in one party's factor shifts its share by well under a seat.
- **cheaprf:** 144 of 144 clean.
- **simrf:** 88 of 88 clean.
- No money path is touched, so no ledger rows are owed, and the baseline did not move.

**Lows left from the review** (filed, not blocking):
- L1: a leftover table row passes the gate but blocks the tool.
- L2: nothing asserts that the fit converged.
- L3: `--check` exits 255 on a structural mismatch.
- L5: the one-pass property rests on the start dates and is unguarded.
- L6: every refit rewrites all 43 comments.
- L7: the tool's LF line endings.

**Declared:** K248. GATE-18's refit drift now FAILS the gate where any stored record factor moves more than RefitReportFloor (1e-6 relative) from the run's fit, or where the fit prints a list the table has no row for. This supersedes K107, because AI countries count with these factors. Setting `RefitFails = false` restores K107.

**Verdict:** READY. The refit is a fixed point, both bars are clean, and the 20-turn baseline is unchanged.
