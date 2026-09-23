# Review: the measurement is the source - France and Germany read their measured effects (2026-09-23, s599)

**What was reviewed.** The s599 change to `Assets/Scripts/Data/PensionParticipationResponse.cs` (the per-country hazards replaced by `MeasuredEffect` - France 20.9 pp, Germany 13.5 pp, from Table B.1 of Atav, Jongen & Rabate 2021 - and `Step`), the diagnostic's checks, and the family `pp4` (seed 777 digest 6f6788f1c3f6cb5a7618d7fc8229dba013e4465c9e10da8d28b482ed1cecb684, seed 424242 digest 7ca9d665354f196082377526816c179d22975b6d6c185b3ec30b66669d322db5) against the landed `pp3`. Owed because the change books money and moves the baseline.

**The form.** One independent read-only reader, told the ruling, the source and six areas. The report is below VERBATIM, then what was done with each finding.

---

# Review: §599, measured effects in PensionParticipationResponse (family pp4 vs pp3)

Reviewer: adversarial, read-only. Nothing in the project was edited.
Scope: the working-tree diff of `Assets/Scripts/Data/PensionParticipationResponse.cs`, `Assets/Editor/PensionParticipationDiagnostic.cs` and `Assets/Editor/TrajectorySentinelCheck.cs`. `GameController.Statistics.cs` (PF-15) and `Tools/film_scope.tsv` (dry600) are also modified in the tree but belong to a different change and were not reviewed.

**Verdict: no code defect.** The code does what the ruling says. The seed stays inert. The four unmeasured countries' step is unchanged bit for bit. The sentinel digests match the pp4 dumps. There are **two record defects**: one false sentence in the sentinel's baseline note, and doc comments that still describe the old formula. There are also four limitations to state in the record.

## (1) The two figures against Table B.1: CORRECT

From rja.txt, the appendix Table B.1 ("Comparison with results of related studies"):
- `Rabate & Rochut (2019) FRA NRA 60 ! 61 DID +20.9 {47.8 45 0.50 23`. The employment effect is +20.9 pp and the code has France at 0.209f. ✓
- `Geyer & Welteke (2019) GER : ERA 60 ! 63 RDD +13.5 {27.6 62 0.19 12`. The employment effect is +13.5 pp and the code has Germany at 0.135f. ✓ The dropped glyph before "ERA" is the paper's female sign, so "women's ERA" is right.
- MedianHazard 0.425 is still correct. The eight increases give 0.19, .25, .25, .35, .50, .50, .50, .70, and the median is (0.35 + 0.50) / 2.
- **Nit (PensionParticipationResponse.cs:47):** "the measured effect is a median 0.75 of the product". Recomputed from Table B.1, the ratio of measured effect to (employment rate × hazard) runs .458, .600, .664, .700, .772, .929, 1.000, 1.146, and the median is **0.736**. Against the bunching column the median is 0.743. **Fix:** write "about 0.74".
- **Limitation, mis-stated in the record (PensionParticipationResponse.cs:36-37, 46-47; Diagnostic:122-125):** "the formula overshoots the measurement by a fifth (25.3 vs 20.9)". That 25.3 is the model's Eurostat **participation** rate at **61 y 9 m** (50.7 %) × 0.50. The measurement is **employment** just before **60** (45 %). Table B.1's own like-for-like product is 45 % × 0.50 = 22.5 pp, which is **+8 %** over 20.9. The "fifth" mixes the formula's overshoot with the participation-vs-employment gap and with the age gap. The ruling still stands, but the sentence overstates the case for it. **Fix:** say "the paper's own product 22.5 (+8 %); on the model's participation rate at 61 y 9 m, 25.3 (+21 %)".

## (2) Raising vs lowering, the room cap, and continuity: CONSISTENT, one limitation

- The code (PensionParticipationResponse.cs:73-90) is a constant step times the band's overlap share, capped by `room` (1 − rate when raised, rate when lowered). Because the step is constant for FR/DE, the response is piecewise linear in the dial age and exactly 0 at the seed. It is continuous across the seed and across band edges.
  - The diagnostic's sweep confirms this. The largest one-month move is 0.0262 pts for France (at 60 y 2 m) and 0.0205 pts for Germany (at 60 y 4 m), against the 0.1 bar.
- **The cap never binds in the dial range (60-70) for either country:**
  - France lowered only crosses band 12 (60-64, rate 0.452), and 0.452 > 0.209. Raised, the room is 0.548 and 0.884.
  - Germany lowered to 60 crosses band 12 (0.684) and band 13 (0.216), both above 0.135. Raised, the room is 0.784 and above.
  - France's ±2 figures (+0.628 / −0.628) are exactly symmetric, and that is correct, not a bug. Both windows (62.75 to 64.75, and 60.75 to 62.75) lie wholly inside band 12, each with share 0.4.
- **Lowering below the measured band:** it cannot happen for France, because the dial floor of 60 is the bottom of the measured window. For Germany, dial 60 is exactly the reverse of the measured 60→63 window.
- **Limitation (PensionParticipationResponse.cs:55-56):** under pp3, a lowering read the rate before the dial age (`lower` = the dial), which is the mechanism's own reading: the people freed to retire at 60 were in their age-59 state. The measured constant drops that.
  - France at dial 60: pp3 step 0.707 × 0.50 = **0.353**, pp4 **0.209** (−41 %). The raise side falls only 17 % (0.253 → 0.209).
  - The one lowering in Table B.1 (Vestad, NOR 64→62, −33.2 pp at 65 % × 0.46) is bigger than any increase in it. Applying a measured raise to a lowering may understate the lowering.
  - State this. It is not a defect under the ruling ("the measurement is the source").
- **Nit, no current effect:** nothing checks that a measured step stays at or below the pre-age rate, `RateAtAge(rates, lower − 1)`. That check is the implied hazard ≤ 1: under the mechanism, no more people can "stay" than were active before the age. Today the implied hazards are FR 0.209 / 0.507 = 0.41 and DE 0.135 / 0.419 = 0.32, with lowering always lower, so it does not bind. A future measured entry at a high seed age (for example a US figure against its flat 19.5 % 65+ rate) could exceed it, and `room` would not catch it. **Fix, optional:** `Mathf.Min(measured, RateAtAge(rates, lowerAge − 1f))` in `Step`, or an assert in the diagnostic.

## (3) A measured effect carried to a different age window: CORRECTLY APPLIED, a stated limitation

- The code applies the measured pp as an **absolute share of the people in the crossed ages**, the same unit as the old rate × hazard. The measurement is not re-expressed as a hazard.
- For France (60→61, measured on a 45 % base) at 62 y 9 m → 64 (base 50.7 % participation), the implied hazard falls from 0.50 to 0.41.
- For Germany (women's ERA 60→63, measured on a 62 % base, hazard 0.19) at 66 y 4 m → 67:
  - The interpolated base is 41.9 %, so the implied hazard **rises to 0.32**.
  - The step is +70 % over pp3's 0.0796.
  - The family figure moves +0.14 % → +0.24 %. The t100 LFPR ratio in the dumps is 1.000985, consistent with that.
- Further mismatches to record:
  - Germany's measurement is women-only and at an early-retirement age. It is applied to both sexes at the statutory age, whose bunching differs.
  - Both measurements are employment and enter as participation. The class header already says so.
  - The code does exactly what the ruling asks. **Limitation, not a defect.**
  - The class doc at :35-36 names Germany's ERA caveat. It does not name the sex and base-rate mismatch. Add one clause.

## (4) FR/DE independent of the sourced rate, and the four bit-identical: VERIFIED

- In code, `Step` returns `measured` without reading `rates` for FR/DE. For the other four it computes `RateAtAge(rates, lowerAge - 1f) * MedianHazard`, the same float operations in the same order as pp3's `RateAtAge(rates, lower - 1f) * Hazard(id)`, where `Hazard` returned `MedianHazard` for them. That is bit-identical.
- `pp3` and `pp3b` are byte-identical on both seeds (cmp), so the harness is deterministic and every pp3→pp4 difference comes from this change.
- pp3 vs pp4, by perl, on both seeds:
  - **France:** every field is identical at turn 1. The first difference is at turn 2, the year the statute steps (2028).
  - **Germany:** differs from turn 1. Its statute steps in 2027, and the t1 LFPR delta of +0.0146 matches the diagnostic's 2027 step of 0.034 pts (measured) against about 0.020 (pp3). This is not an inertness break.
  - **Sweden, Italy, Poland, USA:** identical at turn 1, but **they do move from turn 2** by coupling: Italy through `Zone.InterestRate`, the others through `CurrencyStrength`. Largest relative differences:

| Country | LFPR | GDP | Other |
|---|---|---|---|
| Italy | 8.8e-7 | 9.6e-6 | Unemployment 4.8e-5, GovernmentDebt 1.2e-4 |
| Poland | 1.3e-7 | 2.5e-6 | |
| Sweden | 8.8e-8 | 1.3e-6 | |
| USA | 1.0e-7 | 3.0e-7 | |

- **Record defect (TrajectorySentinelCheck.cs:78-79):** the sentence "the four unmeasured countries ... move not at all against 'pp3'" is false as written for the world. It is true only of their **step** and their structural response. Every earlier note in that stack is careful to say "move in late digits by spillover" (for example §596's "Sweden and Poland by spillover alone").
  - **Fix:** "keep their step bit for bit; they move against 'pp3' only by spillover, from turn 2, in the fifth to eighth digit (Italy through the zone's rate, the others through their currencies)".
- Sentinel digests: SHA-256 of the header plus turns 1-20 of `traj_pp4_s777` is 6f6788f1…cb684, and of `traj_pp4_s424242` is 7ca9d665…22db5. **Both match** the `Expected` table.
- The note's France debt-ratio figures reproduce from the dumps as 100 × Debt / (GDP × PriceLevel) at t100:
  - seed 777: 24.117, which is −0.08 against the response-off 24.20;
  - seed 424242: 33.908, which is +0.70 against 33.21.
  - pp3's own values (24.185 / 33.625) match the old note.

## (5) Numeric inertness at the seed: HOLDS

- `ExtraActive` returns 0f exactly when |now − reference| < 1e-4, before `Step` is reached (:75). `StructuralRate(country)` then returns the pyramid's figure untouched (ParticipationRateTable.cs:86).
- The diagnostic asserts atSeed == pyramidSeed with float equality for all six, and all pass.
- The dumps agree: France and the four are identical in every field at turn 1.

## (6) Anything else

- **Doc rot, nit/record:** these still describe "the sourced rate before the lower age × the hazard" as the step for every country, with no measured branch.
  - PensionParticipationResponse.cs:19-22, the class "The model" paragraph.
  - PensionParticipationResponse.cs:65-67, `ExtraActive`'s summary.
  - PensionParticipationDiagnostic.cs:12, (2) "the step (the sourced rate before the lower age × the hazard...)".
  - PensionParticipationDiagnostic.cs:17, (5) "against Rabaté & Rochut's +20.9 pp". It is now an identity, not a comparison.
  - PensionParticipationDiagnostic.cs:32, the printed `source:` line, "employment effect = the rate just before the age x the hazard at it". This one appears in s599_diag.log above the MEASURED lines.
  - **Fix:** add "where no study measured the country; France and Germany read their measurement (§599)".
- **Nit (Diagnostic:126-127):** the two "the step IS the measured effect" asserts only re-read the table constant, so they are tautological. That is harmless. The substantive guard for FR/DE is now the sweep and the ±2 checks, and those only check sign and continuity, not size. Consider asserting the 2033 France figure (+0.393 pts) as a pinned value if you want a size check.
- **Ledger owed (the cheap bar):**
  - `Tools/review_ledger.tsv` row 75 holds pp2's state 1bd7dcb7… for PensionParticipationResponse.cs. The file's CR-stripped state is now **db6b2f90a299125e790803335453d5802a5a9084ece94cf8a2f5334aef1602ed**, which needs a reviewed row.
  - The new baseline digests (777: 6f6788f1…, 424242: 7ca9d665…) need baseline rows.
  - This report can be the evidence under Reviews/.
- Nothing outside the two Editor tools and ParticipationRateTable/MacroSystem reads the class. No UI text quotes the hazard. The removed `Hazard` / `HazardIsCountrys` have no remaining callers, and the diagnostic compiled and passed in s599_diag.log.

## Summary table

| # | Severity | Where | What | Fix |
|---|---|---|---|---|
| A | record defect | TrajectorySentinelCheck.cs:78-79 | "the four ... move not at all against 'pp3'" is false: they move from turn 2 by spillover (up to 1.2e-4 relative, Italy's debt) | reword to "keep their step bit for bit; move only by spillover" |
| B | nit (record) | Response.cs:19-22, 65-67; Diagnostic.cs:12, 17, 32 | the step is still described as rate × hazard for all six | add the measured branch |
| C | limitation | Response.cs:36-37, 46-47; Diagnostic.cs:124 | "overshoots by a fifth" compares participation at 61 y 9 m with employment at 60; the paper's own product overshoots by 8 % | restate both numbers |
| D | nit | Response.cs:47 | median ratio is 0.736 (0.743 on bunching), not 0.75 | "about 0.74" |
| E | limitation | Response.cs:55-56 | a lowering now uses the raise's constant, not the rate before the dial age (France at 60: 0.353 → 0.209); Vestad's lowering is larger than any increase | state it |
| F | limitation | Response.cs:38-42 | Germany's measurement (women, ERA, 62 % base) carried as absolute pp to 66 y 4 m (41.9 % base) gives an implied hazard of 0.32 (the study's was 0.19); France's falls to 0.41 | state it (sex and base-rate clause) |
| G | nit | Response.cs:55-56 | no implied-hazard ≤ 1 clamp for a future measured entry | optional `Min(measured, RateAtAge(...))` or a diagnostic assert |
| H | owed | Tools/review_ledger.tsv | file state db6b2f90…, baselines 6f6788f1… / 7ca9d665… | add rows before the bar |

---

## What was done with each finding

- **A (the sentinel said the four unmeasured countries do not move) - FIXED:** they move against `pp3` only by spillover (Italy through the zone rate, the others through their currencies; 1.2e-4 at most); their step is bit for bit.
- **B (the class, the extra-active and the diagnostic docs still described rate × hazard for all six) - FIXED** in `PensionParticipationResponse.cs` and `PensionParticipationDiagnostic.cs`, including the `source:` line the diagnostic prints.
- **C (the "overshoots by a fifth" compared the model's rate at 61 y 9 m with employment at 60) - FIXED by stating both:** the model's formula gives 25.3 pp (+21 %); the paper's own product for that study, 45 % × 0.50 = 22.5 pp, +8 % - in the class doc and printed by the diagnostic.
- **D (0.75) - FIXED** to 0.736.
- **E (a measured country's lowering reuses its raise's constant), F (Germany's measurement is women-only, at an ERA, on a 62 % base, carried at the statutory age where the base is 41.9 %, an implied hazard of 0.32), (3) (measured at other ages, carried as absolute points at the current ages) - STATED** in the class doc and COMPLETED.md s599.
- **G (nothing caps a measured step at the pre-age rate) - STATED;** neither entry comes near it.
- **H (the ledger rows) - ADDED:** the file's state after these comment fixes, and the two `pp4` digests named above.
