# wave/q28: the integrator's reading at the merge (2026-10-28)

**Why this exists.** MF q28 (ruled 2026-10-18: "hospital waits read spending, as coverage and mortality do"; the audit's GA-7-1) was built on `wave/q28` (cfd6a866), reviewed once (`Reviews/2026-10-28_q28_review.md`, READY with M1 and L1-L6), reworked (41441688), and measured on main before this reading. The first named run on the merged tree failed the reworked AI-book case for Italy, and the case was rewritten on the branch (d3677e87). This file reads the merged state, records the measurements, and names what the integrator changed.

**Files reviewed:** `Assets/Scripts/Data/HealthFamily.cs`, `Assets/Editor/HospitalWaitsDiagnostic.cs`, `Assets/Editor/NoPolicyCentury.cs`, `Assets/Scripts/UI/PeopleSlips.cs`, `docs/data/SOCIETY_STATS.md`, `Assets/Editor/HealthFamilyDiagnostic.cs`, `Assets/Editor/CheckSuite.cs`, `Assets/Editor/TrajectorySentinelCheck.cs`, `Tools/film_scope.tsv`, `docs/RULINGS_2026-10-23.md`, `ElectionsData/macro/raw/q28_waits/SHA256SUMS.txt`.

**The merge.** `git merge --squash wave/q28` on 5c38ce1c (K219/K220 committed; nothing else had landed since the branch was cut) merged without conflict. The changed-line sets of each code file on the branch (`git diff 5c38ce1c wave/q28 -- f`) and on main (`git diff HEAD -- f`), sorted with carriage returns stripped, are IDENTICAL for `NoPolicyCentury.cs` (8), `PeopleSlips.cs` (2) and `HospitalWaitsDiagnostic.cs` (309, at d3677e87). `HealthFamily.cs` has 30 changed lines on both sides and differs in exactly one: the M1 note's last sentence, which on the branch still said "HospitalWaitsDiagnostic (c) measures it against that path". The integrator changed it to "(c2) decomposes it, and holds it where the AI moves no line", because the case was rewritten after that note was written. The change is in a comment only. GATE OK.

**The integrator's changes.**
- Registered `HospitalWaitsDiagnostic` in the simulation batch after `HealthFamilyDiagnostic`, commented s932.
- The HealthFamily comment above.
- The sentinel re-pinned to the family `mf28`, by its own rule: lead paragraph, BaselineLabel, digest doc and both expected digests (777 bc618b3d…, 424242 18b5b6a3…), as computed by `dig.pl` over the header and turns 1-20 of `traj_mf28_s{seed}_t100.csv`.
- A film-scope row for the dry film `drysq28`.
- K221 (the derived elasticity stands) and K222 (the AI book's real-wage drift kept and DECLARED) appended to the rulings.

**Why c2 was rewritten (the first named run).** The reworked c2 held the AI's book to the declared real-wage path plus 5 %. On the merged tree Italy failed it: after ten idle years its spending per age-cost unit was x0.7546 against a real wage index of x0.9654, so the waits ran 23.1 % above their seeds against a declared 4.4 % plus 5 %. The cause is not the q33 crisis path. The AI finance ministry applies the EU rule (`AiFinanceMinistry.ApplyEuRule` -> `CutLinesUniformly`) and cuts every line by one percentage, which shows as the Health portfolio's yearly allocation ratio. The premise that the AI's health line follows the wage path was therefore false in general, and the case now states only what is true:
- (i) every year the waits sit at the model's step, with the factor from the measured spending ratio, to 1e-3 relative;
- (ii) the drift is printed, decomposed into the wage index, the AI ministry's line moves and the rest;
- (iii) where the AI moved no line, spending follows the wage index to 1 %.

**Honest note on (iii):** on this tree the AI ministry moved the Health lines in all three countries (Sweden x1.0016 net, Poland x0.8411, Italy x0.7816), so (iii) judged none of them. It prints `--` lines saying so. The M1 declaration is therefore carried by the decomposition (ii), not by an assertion. (iii) stays in the check for the day an AI book holds its lines.

**The measurement (this tree, s932).**
- nq28: HealthFamilyDiagnostic, EffectivenessDiagnostic, AverageCabinetDiagnostic, ConstantProvenanceCheck and MetaTextCheck clean; HospitalWaitsDiagnostic failed c2 as above.
- nq28b: HospitalWaitsDiagnostic PASS after the rewrite. The decomposition at year 10:
  - Sweden: wage x1.0888, line moves x1.0016; cataract 57.17 / knee 134.19 d against seeds 60.2 / 141.3.
  - Poland: wage x1.3056, line moves x0.8411; 45.38 / 271.81 against 47.0 / 281.5.
  - Italy: wage x0.9654, line moves x0.7816; 84.92 / 110.76 against 69.0 / 90.0.
- dumpq28 (`mf28`) against `mf32`: 6 of 468 field-readings moved, only WaitCataractDays and WaitKneeDays, in Italy, Poland and Sweden; France, Germany and the USA byte-equal. Nothing downstream reads the waits. At turn 100: Sweden -55.7 %, Poland -86.0 %, Italy +128.6 %. These are the AI books' century-long drifts (K222), large enough to sit on the calibration list.
- drysq28: Sweden 1280 dry film clean (the People slips are IMGUI).
- cheapq28: clean.
- simq28: 85 of 85 simulation checks clean, the sentinel at `mf28`.

**Review rows.** No money path was touched (`bar_tier` lists none). The baseline digests moved, so both seeds' rows are added at s932 with this file as evidence.

**Verdict:** READY. The merged state equals the reviewed branch line for line, except one comment the integrator corrected. The first measured failure was a false premise in the check, not a model defect, and it is fixed and recorded. Only the two wait fields move. Both bars are clean.
