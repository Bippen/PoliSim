# Review - §800, PS-6 G1: R-US18's instrument `UsHouseCountCheck` (2026-10-06)

One adversarial reviewer (a single agent) read `Assets/Editor/UsHouseCountCheck.cs` against the whole of R-US18's block (`docs/specs/USA_STAGE_PLAN.md`, as amended in §797), the card it wrote (`docs/reference/US_ELECTIONS.md`), and the catalogs and CSVs it reads. It edited nothing. Below is its report, wording kept and layout lightly reformatted, followed by what was done.

## The report

**Bottom line: nothing changes the registered outcome `(a) + S2, S5, S4 [(ii)]` or the decisive digest `2df09e4c…`.** The findings are two latent bar defects (checks that can never fail), some verdict wording, and nits.

**Checked against the block and found faithful:**
- `Held` reads `InForce` for the years after B up to T, plus the seat test.
- The shares N, U and one-zero.
- The held-state district swing, with ties going to the base winner.
- Missing shares fall back to F-old.
- F-old's quantile interpolation (checked at M=1 for Montana 2022) and its half rule.
- F-prop counts through (b0).
- The (b), (b0) and (b U-blind) formulas, the P clamp and rounding, and n_T=1.
- **(b'):**
  - its fit set;
  - its Newton step, a correct 2x2 inverse, starting at (0, 1) and stopping at 1e-12 or after 100 iterations;
  - k <= 0 is not fitted.
- **(c):** its window, rho, and the half going to B's majority.
- No change.
- **The scores:** W, G, H over `Held(cfg)`, signed N, and K over the resolutions.
- **Steps 1-5:**
  - the tie-breaks;
  - S5's exception, including a printed S2;
  - S3's forward-cycle rule;
  - the after-S1 listings.
- **Reading (vii):** the sums in steps 2 and 5 run over the swing cycles only, and step 1 stays over D.
- **Reading (ii):** compares the step-2 verdict and S5, and S2/S3/S6 only where its D has two swing cycles. S5 really moves under (ii) (W 10 against 11, where the run over D has 14 against 11), so S4 [(ii)] is correct.
- **Reading (vi):** run, printed, and kept out of S4 and out of the digest.
- **The digest's contents are exactly the block's list:**
  - W per redrawn state-cycle;
  - H/G/N/K of (a) F-old, (b) and (b') per cycle of D;
  - whether (b') is fitted;
  - (c) and no change only where D has two forward cycles;
  - all of this over the base run and readings (i)-(v) and (vii).
- The block's digest is taken over the LF text between the markers.

1. **RULED is not implemented (latent, a vacuous pass).** With the state RULED, only the mirror check runs. Nothing compares the outcome or the digest, and nothing checks that the option can be built.
2. **No C5 guard, and S7 is hard-coded (latent, a vacuous pass).** If 2026 House returns landed in the catalog, the instrument would ignore them and still pass.
3. **The S4 figures always print the reading's S5 figures.** That is right for (ii) today. It would be wrong if a reading moved the step-2 verdict, S2, S3 or S6.
4. **(c)'s comparisons are missing from the not-decided verdict (ambiguous).** Step 4 says "Its comparisons print in the verdict line".
5. **The decided verdict's wording (latent).** The block quotes "code recommends (a): ...", but the code emits "recommends (a) with F-old: ...". The code also sums only H and G, where the block asks for H, G, K and N.
6. **(b') is counted from the last iterate when its fit fails (ambiguous, latent).** The block does not say what an unfitted (b') counts. It is fitted in every cycle today.

**Nits, none affecting this data:**
- the `!HasVotes` u fallback;
- n_T = 1 with a base of several districts;
- `Worse` returns false over an empty set of swing cycles, where the block's "every" over nothing is vacuously true;
- the (c) window ignores readings (iii)-(v) (the block defines the window from `house_maps`, and nothing changes);
- the state check tests a constant;
- amendment entries are not checked to name a ruling;
- the card's source digest omits `HouseYearSourceDigest`.

## What was done

- **1:** RULED is now held. The ruling must name one of the block's buildable options:
  - (a) with F-old;
  - (a) with F-prop;
  - (b);
  - (b) as (b')'s curve;
  - (c) reconciled to (a) with F-old, to (a) with F-prop, or to (b).

  The outcome must equal the ruling's; a changed outcome asks again and is never re-pinned. The decisive digest must equal the ruling's; a moved digest is re-pinned only by a commit naming the old digest, the new one and "outcome unchanged". The plan's line mirrors "RULED (§N): <the ruling>".
- **2:** a check fails once the catalog's House returns run past 2024 ("C5 BILLED ... not built"). Step 7's line is now computed from the cycles.
- **3:** the S4 figures name what moved, each with the reading's own figures: the step-2 verdict, S5, and S2/S3/S6 where compared.
- **4:** the not-decided verdict ends "; (c), printed: <its comparison> - asked."
- **5:** the decided verdict reads "code recommends (a): with F-old; ..." and sums H, G, K and N.
- **6:** carried to the record as an ambiguity for Elias. Today's figures do not depend on it.
- **Nits:**
  - `Worse` now uses the block's "every" without the empty-set guard. That is harmless: with fewer than two swing cycles, S2 is printed only.
  - The card's source digest now includes `HouseYearSourceDigest`.
  - The rest are unchanged and declared.

The rerun (house800d) gives the same outcome and the same decisive digest, and the instrument holds. The card was regenerated (house800w2).
