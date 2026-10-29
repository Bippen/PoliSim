**Files reviewed:** `Assets/Scripts/Data/TaxBaseTable.cs`, `Assets/Editor/CollectionEfficiencyBasisDiagnostic.cs` (`git diff` on main's working tree, 2026-10-29, after the K299 squash-merge).

# uspop: the integrator's re-derived USA income row (K299), independent review

Read-only. No Unity run, no code edited. Also read: `Assets/Editor/TaxScheduleDiagnostic.cs` (section 8, lines 222-275), `Assets/Scripts/Data/TaxSchedule.cs`, `Assets/Scripts/Data/WorldFactory.cs`, `Assets/Scripts/Data/TaxBases.cs`, `Assets/Scripts/Simulation/CohortDemographics.cs` (staged), `Assets/Editor/RevenueBaseDiagnostic.cs`, `Assets/Editor/PriceLevelDiagnostic.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, and the logs `simnames.log` (before K299), `simuspop.log` (K299, before the row), `nuspop3.log` and `nuspop4.log` (after the row).

**State reviewed:** `TaxBaseTable.cs` SHA-256 (carriage returns removed) `61dde835666a5301` (full: `61dde835666a5301f38ad8d07d01c6465b7114f2f1a7296aeb02dec06522cedf`). Both files are CRLF on every line.

## The change

- `TaxBaseTable.cs:67`: `"USA/IncomeTax"` 0.543803f to 0.543675f.
- `TaxBaseTable.cs:65-66`: a two-line comment on why.
- `CollectionEfficiencyBasisDiagnostic.cs:41`: the doc line naming the row now names 0.543675 over 17.1060, and keeps F4-5's 0.543803 over 17.1020 as history.

## What I checked

**1. The seed is computed from the cohorts, and K299 moves the cohorts.**
- `WorldFactory.cs:1182-1188` (`SeedIncomeLeverLevel`) sets `line.Rate` and `line.RateSeed` to `TaxSchedule.AverageRateOnTaxedIncome(country)`. It runs at `WorldFactory.cs:1119`, after the per-country loop.
- `TaxSchedule.cs:829-835`: that rate is the AER over the taxed share, both from `AverageEffectiveRateCore` (`TaxSchedule.cs:470-497`). That sums each band's log-normal income weighted by `country.Cohorts.Counts[cohort]` (line 480).
- `WorldFactory.cs:1090` calls `CohortDemographics.WalkToEpoch` before the seeding. It walks the 2024 seed pyramid to 2026 along `Target(id, year)` (`CohortDemographics.cs:259-268`). `Target` now reads `Projection` (`CohortDemographics.cs:74-79`), and `Projection` returns CBO's path for the USA (`CohortDemographics.cs:93-95`). So K299 changes the USA's opening `Counts`, so it changes the headcount weights in the seed.
- The band incomes do not move. `CohortIncomeSeeds.Apply` runs before the walk, and section 1's "40-44 median 56,760 USD" is the same in both logs.
- The logs agree. The last bar before K299 (`simnames.log`, 22:07) and the five earlier simulation bars of 2026-10-29 I checked (`simk275`, `simk274`, `simdit`, `simk276`, `simk311`) read 17.1020 (taxed share 0.753957, seed AER 12.894). `simuspop.log` reads 17.1060 (0.753934, 12.897). In section 8 only the USA's lines differ between the two logs. Germany, France, Poland and Sweden are the same to the printed digit.
- Conclusion: the move from 17.1020 to 17.1060 comes from K299, not from another defect.

**2. The value and the tolerance.**
- 9.300111 / 17.1060 = 0.5436754, so 0.543675 at 6 decimals is right.
- The exact seed is 17.105952 (9.300079 / 0.543675, from the post-fix log). From it the row would be 0.543677. That is the same 4-decimal-seed convention the other four rows use (for example Germany 10.425679 / 26.9164 = 0.387336).
- After the fix, `nuspop3.log` and `nuspop4.log` read 9.300079 against 9.300111, a gap of -0.000032 points. The tolerance (`TaxScheduleDiagnostic.cs:269`) is 0.002, so the gap is 1.6 % of it. The rounding does not matter. TaxScheduleDiagnostic, RevenueBaseDiagnostic and TrajectorySentinelCheck all PASS on the new row.
- The seed check (`TaxScheduleDiagnostic.cs:262`) passed even before the fix. So the seeding step ran, and only the row was stale.

**3. Nothing else derived from the USA's seed needs re-deriving.**
- The bridge, `WorldFactory.cs:289-292`. `usa.CollectionEfficiency = 0.7531f` was solved as 18.0 / 23.8701 on income revenue of 9.300139 (17.1020 x 0.543803).
  - Without the fix, income revenue is 9.302268. The implied total becomes about 23.8723 and 18.0 / 23.8723 = 0.75402, which rounds to 0.7540.
  - With the fix, income revenue is 9.300079. The implied total is about 23.87004 and 18.0 / 23.87004 = 0.75408, which rounds to 0.7541, less 0.0010 = 0.7531.
  - **The fix restores the bridge's premise:** seeded rate x base = the sourced 9.300111, to within 0.00003. The constant stands as typed.
- `CollectionEfficiencyBasisDiagnostic` prints and asserts nothing about the USA's sourced figure. It reads the row through `TaxBaseTable.BaseShareOfGdp`, and its local USA table is the general-government one (0.3077 etc.), which is unaffected.
- `RevenueBaseDiagnostic.cs:59`, `PriceLevelDiagnostic.cs:71`, `ResponsivenessAuditHarness.cs:211`, `TaxBases.cs:99` and `PolicyWebRenderer.cs:1355` all read the accessor live. None holds a typed USA figure.
- The seed's other anchors (`BaselineTaxRates`, `BaselineIncomeTaxRate`, `IncomeTaxSeedAer`) are taken from the seeded line, so they follow it.
- A search of `Assets/`, `Tools/` and `docs/` for 0.5438, 17.102, 23.870 and 0.7541 finds no other constant. The only other hits are comments (Low 1) and the history in `COMPLETED.md`.
- The sentinel re-pin (`4dbae24d...`, `6f367ce3...`) matches `traj_uspop_s{777,424242}_t100.csv`. Those were written by the dump launched at 23:08:46, after the row was edited at 23:07:03, so the pins were taken on the re-derived tree. (`traj_uspopv1_*` hash to the earlier `39c69f7d...` / `aa457173...`, the pre-fix tree.)

**4. The comments are accurate.**
- `TaxBaseTable.cs:65-66`: right on the numbers (17.1060 was 17.1020; 9.300111 over it; was 0.543803). "Moves the opening income distribution" is slightly loose. The band incomes are unchanged, and what moved is the age mix that weights them. That is acceptable.
- `CollectionEfficiencyBasisDiagnostic.cs:41`: right on all four numbers. "K299 moved the opening pyramid" is exactly the mechanism (`WalkToEpoch`).

## Findings

- **High:** none.
- **Medium:** none.
- **Low 1 (stale comments, outside the diff).** Two comments still name the old pair.
  - `WorldFactory.cs:227` lists "the USA 17.10" among the seeded levels. It is now 17.11 to two decimals.
  - `WorldFactory.cs:289-290` derives the bridge from "the lever's new level 17.1020 = 0.543803".
  - The constant at `:292` is still right (see 3). A clause such as "K299: 17.1060 = 0.543675; implied moves by -0.00006, CE unchanged" would keep the bridge's derivation true. Not blocking.
- **Low 2 (save note, outside the diff).** The staged K299 note at `SaveGameService.cs:113-116` says what an older save keeps. It was written before this row moved, so it does not mention the row.
  - A v69 save cut before K299 carries the USA's `RateSeed` 17.1020 and now reads 0.543675. Its anchored income revenue is 9.29793 % of GDP, -0.0022 points against the sourced figure (-0.024 % of the line).
  - That is far below the scale at which F4-3 (60 %) and F4-5 (up to 2.5x) bumped the format, so no bump is needed. One clause would record it.

**Verdict:** READY. The row is K299's re-derivation and is correct to the diagnostic's precision. It restores the bridge's premise, and the two comment edits are accurate. Low 1 and Low 2 are optional comment clauses in other files.
