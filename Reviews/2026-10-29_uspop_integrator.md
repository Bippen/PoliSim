# wave/uspop (K299, K290's follow-up): the integrator's reading at the merge (2026-10-29)

**Files reviewed:** `Assets/Scripts/Simulation/CohortDemographics.cs`, `Assets/Scripts/Data/Generated/UsDemographicOutlook.cs`, `Assets/Editor/ParticipationCohortDiagnostic.cs`, `Assets/Editor/CohortAgingStepDiagnostic.cs`, `Assets/Editor/GeneratedCatalogCheck.cs`, `Assets/Editor/NoPolicyCentury.cs`, `Assets/Editor/PopulationProjectionCatalogGenerator.cs`, `Assets/Editor/TrajectorySentinelCheck.cs`, `Assets/Editor/CheckSuite.cs`, `Assets/Editor/UsMidtermCheck.cs`, `Assets/Editor/CollectionEfficiencyBasisDiagnostic.cs`, `Assets/Scripts/Data/TaxBaseTable.cs` (the integrator's re-derived row), `docs/reference/US_ELECTIONS.md`, `Assets/Scripts/Data/WorldFactory.cs` (a comment), `Assets/Scripts/Persistence/SaveGameService.cs` (a comment: no field and no stored meaning changes), `Tools/uspop_prep.pl`, `ElectionsData/usa/raw/uspop_cbo_demographics/`, `Reviews/2026-10-29_uspop_prereg.md`, `docs/RULINGS_2026-10-23.md`.

**The path here.**
1. K290 registered K276's (c) for USA 2040 as a miss (−3.43 %), wholly the population's, and filed the US population update.
2. The prereg was committed alone (86bee07c). The four-group recalibration was measured, and review round 1 was READY on conditions.
3. Amendment r2 (cohort split, (c) keyed by the pyramid's own year) was committed alone (6c801815). Amendment r3 (CBO's by-age table, found per r2-4) was committed alone (a7308a54). Each came before the code it governs.
4. Review round 2: READY at `33057a0c`, on conditions.

**The merge.**
- The prereg, with r2 and r3, was committed alone on main first (7a3e1ec8), before any Unity run.
- `git merge --squash wave/uspop` applied cleanly on 579f1c9b.
- K299 is written to RULINGS, and K290 is marked LIFTED by K299.
- CheckSuite's ParticipationCohortDiagnostic comment is updated (the registration is gone).

**Which rule governed (L-r2-1).** The EU model is untouched. r2-5's "byte-identical" holds for every EU model field, apart from the currency channel named below. Sweden's and Poland's (c) printouts moved by the instrument's re-key under r2-2 and r3-3, not by the model.

**The measurement (this tree).**
- **nuspop:** 8 of 8 clean (GeneratedCatalogCheck, ReviewLedgerCheck, CommentImmunityCheck, CohortSubstrateDiagnostic, CohortAgingStepDiagnostic, LongRunAnchorsDiagnostic, ParticipationCohortDiagnostic, DebtPathHoldDiagnostic).
  - `PARTICIPATION COHORTS: PASS`, no REGISTERED MISS.
  - USA (c): 2030 −1.24 %, 2040 −2.17 %, 2055 −2.56 % (band ±3 %).
  - USA (d1): every group inside ±1.0 %, the largest the 65+ at 2055, +0.21 %. The whole population at 2055: −0.01 % on CBO's total.
  - USA (d2): the 16+ against CBO's civilian 16+, −1.10 / −1.12 / −1.13 % (band ±1.5 %).
  - USA (d3): the 15+ and 16+ against CBO's own, within 0.05 % (band ±1.0 %, generous by design).
  - Sweden (c) 2050: +0.31 %. Poland (c): 2030 +1.67 %, 2040 +2.25 %, 2050 +2.47 %, 2070 −2.31 % (the re-key, all inside ±3 %).
  - `LONG-RUN ANCHORS: PASS`, 0 REGISTERED MISS.
  - `DEBT HOLD: PASS`. USA (h3) 2026-2036 +10.4 points against CBO's +19.6, band ±10.0 (9.2 of 10, the margin the review asked to watch). 2026-2056: +52.0 against +74.5, band ±25.0. The two registered K218 misses (Sweden, Germany (h2)) are unchanged.
- **dumpuspop** against dumpnames (`Tools/traj_diff.pl names uspop`):
  - France, Germany and Italy identical at every turn on both seeds.
  - Sweden and Poland first differ at turn 2, only in CurrencyStrength and TradeBalance (the Fed's rate in the partner averages, the review's probe). At turn 100 every field is under 0.01 %.
  - The USA moves from its first record (L1: readings at day 0 of turn 0, the pyramid from the first step).

**What the engine bars caught, which the branch's headless runs could not.** The first cheap and simulation bars (cheapuspop, simuspop) each failed one check:
1. **`TaxScheduleDiagnostic`** (sim): "USA's seeded rate × its base reads 9.3023 % of GDP against the realised 9.300111 % - the row was not re-derived for the rate it is read against."
   - The USA's income lever is seeded at its schedule's own average rate on taxed income. CBO's pyramid moves the opening income distribution, so that rate reads 17.1060 (it was 17.1020).
   - Under F4-5's rule the row is the realised revenue over that rate. The integrator re-derived `TaxBaseTable`'s `USA/IncomeTax` row from 0.543803 to 0.543675 (9.300111 / 17.1060). This restores the premise WorldFactory's collection-efficiency bridge was solved on.
   - The doc comment in `CollectionEfficiencyBasisDiagnostic` that states the row is updated.
   - `TaxBaseTable.cs` is a money path. Its new state is reviewed independently in `Reviews/2026-10-29_uspop_taxbase_review.md`.
   - nuspop3: the row reads 9.3001 against 9.300111, TaxScheduleDiagnostic and RevenueBaseDiagnostic clean.
   - The review: READY (digest 61dde835). Its two optional Lows were taken as comments: `WorldFactory.cs` names the USA's seed 17.11 and says the bridge stands as typed (seed × row is still 9.300111); `SaveGameService.cs`'s K299 note adds that a save made before K299 loads its US income revenue 0.0022 points of GDP low (−0.024 %).
2. **`UsMidtermCheck`** (cheap): the economic vote's m moved −0.01 → −0.03 with the verdict unchanged (verdict (a), government divided, L −8.97, U +2.26). It is RE-REGISTERED in the check's own form, naming K299 as the cause, and the US card's US-13 block is regenerated (`UsMidtermCheck.WriteReadings`).

**The dump with the re-derived row** (dumpuspop, second run) against the first:
- France, Germany and Italy are identical at every turn on both seeds.
- Sweden and Poland differ from turns 4-8, in CurrencyStrength only at first, and are under 0.01 % at turn 100.
- The USA differs from turn 1 in Budget, the effective debt rate and debt: 0.0022 points of GDP less revenue, its debt +0.03 % at turn 100.
- Against dumpnames, at turn 100 on seed 777, the USA's GDP is −3.71 % and its debt +3.66 %.

- **The sentinel**, re-pinned to the family `uspop` on the re-derived tree: seed 777 `4dbae24d2d8df3052f114c71031a989cc3502d30041c52c6af51385f0294996d`, seed 424242 `6f367ce364454ec78f4f3cad7798850251723e3192423a20e8550dfeed74f370`.
- The branch's own diff touches no money-path file. The integrator's re-derived row does, and it is reviewed separately.

**Verdict:** READY. The merged state equals the reviewed branch (`33057a0c`) plus the RULINGS lines, the CheckSuite comment, the re-derived USA income row (with its comment), the midterm re-registration and the card, and the sentinel's re-pin.
