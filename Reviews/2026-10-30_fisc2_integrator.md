# wave/fisc2 (K269 (a) R1, F4, K269 (d), K218's bands; K292-K298, K310, K313): the integrator's reading at the merge (2026-10-30)

**Files reviewed:** `Assets/Scripts/Data/SpendingDrivers.cs`, `Assets/Scripts/Data/SpendingLine.cs`, `Assets/Scripts/Simulation/EnergyLedger.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/Data/Country.cs`, `Assets/Scripts/Data/HealthFamily.cs`, `Assets/Scripts/Simulation/LawCatalog.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, `Assets/Scripts/UI/FiscalRules.cs`, `Assets/Scripts/UI/GameController.Energy.cs`, `Assets/Scripts/UI/GameController.EnergyV35.cs`, `Assets/Scripts/UI/GameController.LawsV35.cs`, `Assets/Scripts/UI/GameController.Statistics.cs`, `Assets/Scripts/UI/RangeCaptions.cs`, `Assets/Scripts/UI/StatsSlips.cs`, `Assets/Editor/ParticipationCohortDiagnostic.cs`, `Assets/Editor/EnergyLedgerDiagnostic.cs`, `Assets/Editor/TrajectorySentinelCheck.cs`, `Reviews/2026-10-29_fisc2_prereg.md`, `docs/RULINGS_2026-10-23.md`, `Tools/film_scope.tsv`.

**The path here.**
1. K269 (Elias) asked for one spending-line rule in every book (a), and the debt chart's own framework figure (d).
2. The prereg was committed alone on the branch before code. Review round 1: NOT READY (H1, M1-M3). Round 2: READY on conditions (R2-M1 and the K-lines).
3. R2-M1 was resolved under option (a) at a092233b: the levy stays continuous, and the page prints the pass-through the rule computes.
4. The branch was rebased onto main 54c23ca9 (§951, §952, §953). One new check line failed out of the engine: ParticipationCohort (c) Poland 2050, +3.03 % against ±3 %.
   - It was ruled K313 (Code's recommendation, the standing instruction): registered, bounded, after a dated prereg amendment 2 committed alone (3c5aa98c), then the code (f3e8e945).
5. K292-K298 and K310 were recorded on main alone before the merge (54c23ca9). K313 and K310's corrected figure (Poland +3.42 / +3.42 at year 50) are recorded with the merge.

**Round 2's conditions, checked at the merge.**
1. **R2-M1, option (a).** The commit (rebased as 9d9b16ea) adds read-only helpers to `EnergyLedger.cs`: `LevyPassThrough`, `PassesOneForOne`, `PassThroughShort` and `PassThroughNote`.
   - No line of the levy's own computation changes; the rest is doc comments.
   - The page calls those helpers. EnergyLedgerDiagnostic (3b) holds four mixed histories (1.0000 / 0.9228 / 0.8722 / 0.9088) against the closed form 1 / G_AI, the helper and the printed words. It is clean in this tree's Unity run (nfisc2).
   - K297 is recorded with the resolution.
2. **The K-lines:** recorded (above).
3. **The sim bar's named checks:** clean (below).
4. **The usual steps:** ledger rows, the `fisc2` family re-pinned, play saves re-staged for format 70, the dry film. All done (below).
5. **The uspop merge (M4):** the rebase kept main's whole K299 note above `70: fisc2` (`SaveGameService.cs`). SimulationManager.cs and GameController.LawsV35.cs replayed cleanly with §952's display names kept.

**The merge.**
- The prereg, with amendments 1 and 2, was committed alone on main first (b9d3fecb), before any Unity run.
- `git merge --squash wave/fisc2` (f3e8e945) applied cleanly on b9d3fecb.
- Film scope row `dryfisc2`: Sweden, Germany, France, the USA at 1280.

**The measurement (this tree).**
- **nfisc2:** 15 of 15 clean (DebtPathHold, LongRunAnchors, ParticipationCohort, EnergyLedger, SpendingIndexation, LineIndexation, HospitalWaits, UnemploymentFloorPhillips, PreviewParity, TaxSchedule, GeneratedCatalog, DryFilmScope, CommentImmunity, EnumDisplayName, MetaText).
  - `DEBT HOLD: PASS`, 1 REGISTERED MISS (Germany (h2), K295). Sweden (h2) −11.2 against the DSM's −5.8, band ±5.6, holds (K295 lifts it). USA (h3) 2026-2036: +13.3 against +19.6 (margin 3.7). 2026-2056: +64.7 against +74.5.
  - `LONG-RUN ANCHORS: PASS`, 3 REGISTERED MISS: K210's (d2) and K298's US (d3) marks within their bounds.
  - `PARTICIPATION COHORTS: PASS`, 1 REGISTERED MISS (K313, Poland 2050, its attribution holding).
  - `FLOOR PHILLIPS: PASS`.
  - PreviewParity clean with the new `Country.EnergyLevyRealGrowth` field.
- **dryfisc2:** 4 sessions (631 frames), 0 failed, 0 overflow, 0 escape.
- **stagefisc2:** the play saves re-staged at format 70. **slrtfisc2:** 12 scenarios (6 countries × 2 seeds) round-trip clean.
- **dumpfisc2** against dumpuspop (`Tools/traj_diff.pl uspop fisc2`):
  - Every EU country first differs at turn 1 (ApprovalRating), the USA at turn 2. F4 alone moves the all-AI world; R1, M3 and R2-M1 leave it byte-identical (the implementer's frf_play on the rebased tree, both seeds).
  - Debt at turn 100, seed 777: Poland +223.6 %, Germany +212.6 %, Sweden +37.0 %, Italy +6.7 %, France +6.5 %, the USA +5.4 %. GDP is within 0.5 % everywhere. Seed 424242: Germany +484 %, Poland +46 %, Sweden +18.6 %, Italy +6.2 %, France +4.2 %, the USA +5.3 %.
  - As a share of GDP at turn 100 (seed 777): Germany 76 → 236 %, Poland 99 → 321 %, Sweden 153 → 210 %, France 235 → 250 %, Italy 237 → 252 %. The AI world's century already ran France and Italy past 230 % on main; F4 brings Germany and Poland into that range.
  - No acceptance in the prereg bounds the AI world past the DSM's 2035 (F4's landing rule was the asserted (h1)/(h3) bands, all holding). **Filed:** a hold on the all-AI world's century debt, or a reading of the AI reaction that lets it pass 200 %, as its own item.
- **The sentinel**, re-pinned to the family `fisc2`: seed 777 `4e5611c454ba4a7e204b480620ede9e632f0d502c0528c769a926b01a89f7bb1`, seed 424242 `d3aaee0a3e9e02a8bcf6b6bbd9d9e0f315cb21f2ac8e425ed6690a0fdeacceb5`.

**The bars' own rules, after the re-pin** (nfisc2l, cheapfisc2, simfisc2):
- FT-10's bounds pass found three LEVEL ceilings slack, more than a point above their measurement: France household 4.854 % at t26 against 6.5, and Poland household 1.520 % and industry 1.549 % at t56 against 3.0. Each is lowered by the check's rule (the measurement plus half a point, up to the next half point): 5.5, 2.5 and 2.5. The breach count is unchanged (4 against the recorded 4).
- `ReviewLedgerCheck` (`Assets/Editor/ReviewLedgerCheck.cs`) found SpendingLine's grandfathered row stale. The row is removed and `GrandfatheredCeiling` lowered 8 → 7; the new state has its reviewed row.

**Verdict:** READY. The merged state equals the reviewed branch's final HEAD (f3e8e945): round 2's state, plus R2-M1's resolution and the K313 registration, each checked above, the sentinel's re-pin and the two rule-driven ceilings.
