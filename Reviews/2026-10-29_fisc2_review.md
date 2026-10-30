# fisc2 review: K269 (a) R1, F4, K269 (d), K218's bands (`git diff cf6bd007..wave/fisc2`, HEAD 4ac720bb)

Independent adversarial review, 2026-10-29. Read-only on the repo: no Unity run, no code edited. I ran the gate and the branch's own loop instruments out of the engine (.NET, not Mono: each figure is a prediction to the printed decimal at best, the instruments' own caveat).

**Files reviewed:** `Assets/Editor/BudgetPremiseDump.cs`, `Assets/Editor/DebtPathHoldDiagnostic.cs`, `Assets/Editor/EnergyLedgerDiagnostic.cs`, `Assets/Editor/HospitalWaitsDiagnostic.cs`, `Assets/Editor/LineIndexationDiagnostic.cs`, `Assets/Editor/LongRunAnchorsDiagnostic.cs`, `Assets/Editor/NoPolicyCentury.cs`, `Assets/Editor/SpendingIndexationDiagnostic.cs`, `Assets/Editor/UnemploymentFloorPhillipsDiagnostic.cs`, `Assets/Scripts/Data/Country.cs`, `Assets/Scripts/Data/HealthFamily.cs`, `Assets/Scripts/Data/SpendingDrivers.cs`, `Assets/Scripts/Data/SpendingLine.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, `Assets/Scripts/Simulation/EnergyLedger.cs`, `Assets/Scripts/Simulation/LawCatalog.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/UI/FiscalRules.cs`, `Assets/Scripts/UI/GameController.Energy.cs`, `Assets/Scripts/UI/GameController.EnergyV35.cs`, `Assets/Scripts/UI/GameController.LawsV35.cs`, `Assets/Scripts/UI/GameController.Statistics.cs`, `Assets/Scripts/UI/RangeCaptions.cs`, `Assets/Scripts/UI/StatsSlips.cs`, `Reviews/2026-10-29_fisc2_prereg.md`, `Tools/headless_hold/PlayerBook.cs`, `Tools/headless_hold/diag.sh`, `Tools/headless_hold/player_book.sh`.

Also read: `docs/RULINGS_2026-10-23.md` K218, K222, K269, K271, K275, K287 (and K288-K302); `COMPLETED.md` §944, §947, §948; `Assets/Scripts/Data/WorldFactory.cs` (the transfer residual), `Assets/Scripts/Data/TaxSchedule.cs` (`LiftThresholds`), `Assets/Scripts/Data/MortalitySchedule.cs`, `Assets/Scripts/Simulation/MacroSystem.cs` (`RealWageGrowthPerTurnPercent`), `Assets/Scripts/UI/GameController.BudgetV35.cs:811-812`; the sources: CBO LTBO 2026 (`ElectionsData/usa/raw/k274_fiscal_reaction/wayback_20260923101450_cbo_62044-2026-LTBO.xlsx`, Tables 4 and 7), the Ageing Report 2024 and DSM 2024 texts (`ElectionsData/macro/raw/k276_pension_age/`, `q32_debt/`), `ElectionsData/rules/fiscal_rules.md` with its raw pages.

## What I ran, and whether I reproduce the numbers

| Instrument | Result | Reproduces the implementer? |
|---|---|---|
| `Tools/cgate/gate.sh` on the worktree | **GATE OK** | yes |
| `frf_play.sh <root> on none 100 <seed>` on `git archive cf6bd007` and `git archive dd47efce` (R1 alone), seeds 777 and 424242 | `cmp`: **byte-identical** on both seeds; HEAD differs on both (F4) | yes - same SHA-256 as the implementer's `ai_base_*`, `ai_r1_*` and `after/ai_*` |
| `hold.sh` on HEAD and on cf6bd007 | HEAD: PASS, 1 registered (Germany (h2) +20.8 against +3.0 ±7.6); Sweden (h2) -11.2 against -5.8 ±5.6, France +36.2, Italy +25.1, Poland +45.3, USA (h3) +15.2 / +63.6, all inside | yes - both files byte-identical to the implementer's `hold_final.txt` / `hold_before.txt` |
| `player_book.sh` HEAD `play`, `unchanged`, `nofrf`, six players, 777 and 424242 | see H1 and the hunt list (4) | yes - all three CSVs byte-identical to the implementer's `after/*.csv` |
| `player_book.sh` cf6bd007 `play`, seed 777 | lines 41.5 / 41.8 / 46.5 / 40.3 / 41.8 / 19.9 % of GDP to 12.25 / 15.12 / 16.84 / 16.41 / 12.73 / 6.64 %; debt -255 / -112 / -79 / +96 / -209 / +5 % | yes - the prereg's "before" figures |
| `diag.sh` LongRunAnchorsDiagnostic; `anchors.sh` | PASS with 3 REGISTERED (d3) marks, all USA (years 50 / 60 / 70: +6.03 / +6.94 / +7.78 against +5.68 / +6.58 / +7.38 ±0.3; without the treatable excess +5.81 / +6.70 / +7.54); ParticipationCohort PASS, K290 at -3.33 % | yes |
| `diag.sh` LineIndexation, SpendingIndexation, HospitalWaits, IncomeTaxCreepCap, EnergyLedger | all PASS | yes - byte-identical to the implementer's `diag/` outputs |
| UnemploymentFloorPhillipsDiagnostic | does not run out of the engine (SecurityException, an ECall) | not reproducible here (see L5) |

Timeline (commit dates, and the implementer's scratch files): `player_book.sh` 18:16:36; the prereg 32ade1a9 18:16:45, **alone** (one file); the first after-measurements 18:19-18:25; the R1 commit 18:37:53. The prereg was committed alone and before the code it governs.

## The hunt list, answered

1. **R1 and its sources.** In direction, yes; exactly, no (L2).
   - DSM 2024 (IP306): "the structural primary balance (SPB) before costs of ageing is assumed to remain constant at its value forecast for T+1", ageing costs added - every non-ageing item at a constant share. R1's driverless lines do that. Its non-ageing *caseload* lines (population, 20-64, 0-19 family, and F4's income security) do not: they grow with headcount × the real wage, which outgrows GDP when employment per head falls.
   - AR 2024 (IP279): education "expenditure per capita growing in line with labour productivity" ✓. The health baseline carries unit costs with GDP *per capita*; GDP per worker appears only in the labour-intensity scenario. R1 (real wage ≈ productivity) is the labour-intensity scenario. "Wages are assumed to evolve in line with prices and labour productivity" ✓.
   - CBO LTBO 2026, Table 4: discretionary is 4.641 % of GDP every year 2040-2056 ✓. But it *falls* 5.894 % (2026) → 4.641 % (2040), because in-window appropriations follow inflation - the prices-only rule R1 retires. "Other mandatory" falls 2.994 → 2.285 %. Table 7: health without ageing or excess cost growth 6.684 → 6.476 %, Social Security 5.222 → 4.780 % ✓ (as the prereg quotes).
   - **A3 is true in code.** `IndexSpendingLines` computes the same float expressions for an AI book: `realGrowth = 1f + PotentialGrowthRate / 100f` and the wage ratio under `RealWageIndexAtLastIndex > 0f`, with the factor expression unchanged. `TaxSchedule.LiftThresholds` still returns for `!aiBook`. The one changed static, `ProjectLineNextYear`, has only UI readers (`GameController.BudgetV35.cs:812`, `GameController.Statistics.cs:98`) and diagnostics; `ProjectedRealTerm` and `RealWageGrowthPerTurnPercent` are pure. Reproduced byte-identical on both seeds, to the CSV's printed precision.
2. **F4.**
   - **The residual: yes.** The EU five's `IncomeSecurity` is the solved residual (`WorldFactory.cs:953-961`, `SeedMandatoryTransferLines`; Sweden's at `:1535-1538`). The USA's is function 600 (700 bn seed, 2.41 % of GDP).
   - **The driver: declared, not sourced, and the citation does not fit** (M2).
   - **Removing the unemployment driver removes a double count and drops nothing material.** The cycle stays in `GetUnemploymentBenefitCost` (0.10-0.25 % of GDP per point, the country constant) and in the LaborMarket lines. The old driver added about 1.2 % of GDP per point in Sweden (9.58 / 8.0) and 3.4 % in Germany (11.8 / 3.5), five to fifteen times the benefit term. Germany's residual does hold Bürgergeld and SGB II/III (55.5 bn of about 520 bn), so a small cyclical share goes, inside the benefit term's own size.
   - **Defect or calibration: both.** Removing the rate is a defect fix. Choosing the population is a calibration choice.
3. **Prereg discipline.**
   - **Bands fixed before measurement: yes.**
   - **A1 missed and is not reported (H1).** A2, A3 and Part 4 hold as written. B fails as predicted, and the FRF stays.
   - **The DebtPathHold registrations were handled by the rule:** four lifted where the band holds, Germany kept and re-attributed, none widened, none new.
   - **The (d3) registration does not break Part 4's letter; it does break the prereg's spirit (M1).** Part 4 governs "K218's five registered bands (DebtPathHoldDiagnostic)". But (d3) is a check that FAILED after measurement (scratch `diag/LongRunAnchorsDiagnostic.txt` 18:45, 3 FAIL). It was then given a new exemption (19:04) that no prereg line names and no K-line rules. That is the re-choice "What follows a miss" forbids.
4. **The century tables** (`play`, y1 → y100, seed 777).
   - **Revenue:** Sweden 42.4 → 49.5, Germany 40.2 → 49.4, France 44.6 → 53.1, Italy 42.0 → 55.2, Poland 36.7 → 58.7, USA 17.7 → 24.8.
   - **Spending:** 42.9 → 50.3, 42.1 → 51.7, 49.1 → 56.4, 44.6 → 60.6, 42.9 → 61.5, 23.5 → 29.0.
   - **So "8-18 points" is loose:** revenue rises +7.1 to +22.0 (Poland above the range), spending +5.4 (USA) to +18.6.
   - **"Creep × the multiplier" holds for Sweden, France, Germany and Poland. For Italy it is the multiplier alone.** Italy's PIT is flat (10.8 → 10.7) while the multiplier reaches 1.33-1.41.
   - **The FRF's revenue has no economic effect in Italy.** Italy's GDP, real wage, price level and unemployment are *identical* to the digit in `play`, `unchanged` and `nofrf` at y50 and y100. The multiplier scales the taxes only (K213), so the reaction collects 13 points of GDP that move nothing else.
   - **Spending's rise is mostly the pension line**, at the statutory headcount and the going wage with no benefit ratio (DECLARED): Poland 10.4 → 21.7 % of GDP, Italy 13.7 → 20.6 %.
   - **A2 holds as worded,** reproduced 12/12. No year is at a bound; the multiplier stays within 0.92-1.41. Debt at year 100 is within ±50 (largest Italy +22.4). That is "not pinned", not "plausible": Poland's 58-59 % revenue and 60-62 % spending are outside every OECD state's range (L8).
5. **Save format 70: no field is added.** Only the stored `DriverReference` of one line changes meaning. A v69 save is refused with the plain message (`SaveGameService.cs:234-238`, ruling A). Nothing new needs cloning for previews. The comment's magnitude is wrong (L3).
6. **The debt line is sourced.**
   - `fiscal_rules.md` quotes skr. 2025/26:76 3.2 ("Skuldankaret är satt till 35 procent av BNP och gäller tills vidare"); the string is in `raw/se_riksdagen_skr_2025-26-76_HD0376.html`.
   - It also quotes Protocol (No 12) Art. 1: `raw/eu_tfeu_protocol12_cellar.xhtml`, SHA-256 c5cf78e7…fb643, matches.
   - Poland art. 216 ust. 5 and 31 U.S.C. 3101(b) are there too.
   - The slip texts match the file.
7. **UI strings: all IMGUI** (GUILayout/GUI; `CanvasPaint.Bridge` is a texture painter, not uGUI). A dry film suffices; the Canvas film rule does not apply. The shots to name:
   - Statistics · Domestic · the debt chart's threshold line and label, and its slip `chart:debt/comfortable` ("anchor 35 %" for Sweden, "EU 60 %" for Germany, France, Italy and Poland, no line and no slip for the USA): Sweden@1280x720, Germany@1280x720, USA@1280x720.
   - Energy · Policy (v3.5) · the Retail intervention tile's slip (`GameController.EnergyV35.cs:395-396`) and the kept why/reaches plate row "× THE SEEDED LEVIES · CUT IN PROPORTION BY THE SUBSIDY" (`GameController.Energy.cs:290`, drawn by `DrawEnergyPlate` from EnergyV35).
   - Laws · a sector's Subsidy dial row census (`GameController.LawsV35.cs:548-549`).
   - Budget · the IncomeSecurity tile's slip, now "DRIVEN BY POPULATION · NEXT YEAR …" with the real term (`GameController.BudgetV35.cs:811-812`; indirect).
   - Shoot Sweden@1280x720 plus one country with a policy levy, and Germany for "no energy line".
8. **The money paths.**
   - (a) `SimulationManager.IndexSpendingLines`: `realGrowth` (`:8468`) and `wages` (`:8484`) no longer read `aiBudget`, so the player's lines carry real factors. `aiBudget` now decides `LiftThresholds` alone. The AI arithmetic is unchanged.
   - (b) `SimulationManager.ProjectLineNextYear` (`:8537-8545`) multiplies by `ProjectedRealTerm` (new, `:8551-8554`) in every book, through `PensionNominalGuarantee` with the real term. UI readers only.
   - (c) `SpendingDrivers.Of` (`:71-77`): IncomeSecurity → Population, in every book. This is F4, the only code change that moves AI books.
   - (d) `EnergyLedger`: comments only. Its player-book result changes through R1's G (M3).
   - (e) `SaveGameService.CurrentSaveVersion` 69 → 70.
   - (f) Docs only: `GetFiscalReactionMultiplier`, `SpendingLine.ProjectNextYear`, `Country.ComfortableDebtToGdpPercent`, the LawCatalog P4-C3 block, `HealthFamily`.
   - (g) `FiscalRules.DebtReferencePercent / Label / Lines` (UI).
9. **Vacuous checks and CRLF.** No check passes vacuously:
   - LineIndexation (3) now requires the player's factors ≠ 1 *and* on the rule.
   - EnergyLedger (3b) requires G > 1.001 and the two forms apart.
   - (d3) keeps `marks > 0`.
   - DebtPathHold fails any unregistered miss.
   - Three checks were weakened: L4, L5, L6.
   - CRLF: every changed `.cs` is i/lf, w/crlf, with CR count = line count. The new `Tools` files and the prereg are LF. No mixed endings.
10. **Interaction (trial merges with `git merge-tree`, read-only).**
    - main: clean.
    - wave/k273 (K300): clean, and no shared file.
    - wave/names: clean.
    - **wave/uspop: one textual conflict** at `SaveGameService.cs:113` (its four comment lines sit right above the version line fisc2 rewrites), plus a semantic coupling (M4). `NoPolicyCentury.cs` merges cleanly, since uspop adds `Pyramid` / `SubstrateYear` away from fisc2's hunks.

## Findings

### High

**H1 - The prereg's acceptance (A1) is missed, and the branch reports it nowhere.** `Reviews/2026-10-29_fisc2_prereg.md:39-41, 112`; `Assets/Scripts/Simulation/SimulationManager.cs:701-708`; commit dd47efce's message.

- **The miss:** A1 asks the player's total lines to stay within ±3.0 points of the no-policy-change book at years 10, 30, 50 and 100, for six players on both seeds. Reproduced, with my own judge and with the implementer's `judge.awk` (which prints "A1 FAILS"):

| Book | y50 | y100 |
|---|---|---|
| Sweden 777 / 424242 | +0.88 / +0.88 | **+3.86 / +3.88** |
| Poland 777 / 424242 | **+3.42 / +3.41** | **+3.79 / +3.71** |

- **Every other book is inside** (France 2.2-2.4, Germany 2.2, USA 0.3, Italy 0.00).
- **Attribution, read off the LINE and MACRO rows:**
  - At y100 the played economy is smaller and its real wage higher. Sweden: GDP 2474.6 against 2583.5, real wage 371.6 against 349.2. Poland: 2902 against 3001, 566 against 542.
  - So the caseload lines at the going wage take a larger share. The largest movers are SocialSecurity (+1.21 / +1.63) and IncomeSecurity on F4's population driver (+1.10 / +1.28).
  - That is the mechanism the prereg's [AUTHORED-DRAFT] band was set for, and the band was exceeded.
- **What is missing:** dd47efce says "the prereg's A1-A3 measured" without the outcome. The FRF doc says only that the idle book "holds near its opening ratio with the reaction inside its bounds" (A2). B3 fails in the same three books for the same reason (Sweden's B fails on B3 alone), which the code does not mention either.
- **Scenario:** the integrator records R1 as landing on a passed prereg, and the record claims an acceptance that was missed - the outcome preregistration exists to prevent.
- **Fix:** report A1's miss in the item with this measurement and attribution, the band unchanged. Rule by a K-line whether R1 lands with A1 missed. Under the standing instruction Code may recommend landing: the miss is the declared revenue difference, and the alternative, the driver-only player book, is far worse.

### Medium

**M1 - The (d3) registration was added after the check failed, outside the prereg, with no K-line and no bound.** `Assets/Editor/LongRunAnchorsDiagnostic.cs:35, 107-110, 429-434`; `Assets/Editor/NoPolicyCentury.cs:70-73, 246-251`.

- **The evidence:** R1 moved the player's idle book. The USA's (d3) marks at years 50, 60 and 70 failed (+0.05 / +0.06 / +0.10 years past the band), and 15 minutes later a rule was written that registers any (d3) mark whose gain *without the treatable excess* is inside the band.
- **It is sound as an attribution.** `WithoutTreatable` is `LifeExpectancyAtBirth` with a zero excess, the same schedule, calibration and multiplier as `MortalitySchedule.LifeExpectancy`. It is also consistent with K222's declared drift.
- **But it differs from every registration before it (K210, K217, K290):**
  - It is not ruled.
  - It is not keyed: any country and any year qualifies.
  - It is unbounded: any size of treatable excess passes while the rest is in band. K290, for contrast, holds only "while the population part lies within a point of the measured".
- **Scenario:** a later change, say a health-line bug, that drives US treatable mortality far down leaves (d3) green and REGISTERED.
- **Fix:** a K-line (Code's recommendation, standing instruction), keyed to the measured marks (USA, years 50-70) with a bound on the treatable part (e.g. ≤ 0.5 year). Add a dated amendment to the prereg saying it was written after the measurement. Correct the PASS line's "every country's life-expectancy gain inside its band" (`:137`).

**M2 - F4's replacement driver is presented as the DSM's convention, but it is a calibration choice that drifts over the century.** `Assets/Scripts/Data/SpendingDrivers.cs:71-77` ("A caseload of residents at the going wage holds the residual's share of output, the DSM's non-ageing convention"); prereg Part 4 ("where the real wage follows output per head").

- **The model's real wage follows productivity per worker,** as the AR says it does ("in line with prices and labour productivity"), not output per head. A population caseload at the going wage therefore grows with population ÷ employment.
- **Measured (777, % of GDP, y0 → y100):**
  - In the `unchanged` book: Poland 13.35 → 15.77, Germany 11.80 → 12.81, Sweden 9.58 → 9.90.
  - In `play`: 17.04, 13.75 and 11.00.
- **The DSM's convention for a non-ageing item is a constant share,** which a driverless line (prices × real potential growth) gives exactly.
- **What it does not affect:** the hold's window, where the line stays within 0.5 point to 2035.
- **What it does affect:** it adds 0.3-3.7 points of ageing-like drift to a line the item calls non-ageing, and about a third of H1's miss.
- **Fix:** do not re-choose the driver now (that would be re-choosing after measurement). Declare it in the code and the save comment: tag it [AUTHORED-DRAFT] and state the drift. File the driverless alternative.

**M3 - R1 silently changes the energy subsidy's pass-through in the player's book from one for one to 1/G. The page is re-labelled, not ruled.** `Assets/Scripts/Simulation/EnergyLedger.cs:24, 26-30, 381-387`; `GameController.Energy.cs:290`; `GameController.EnergyV35.cs:395-396`; `GameController.LawsV35.cs:548-549`; `RangeCaptions.cs:310`.

- **Before R1:** with the player's path on prices alone, P-A's share-of-path form was exactly a billion of extra support off a billion of levy.
- **Now:** the path carries G, so a billion displaces 1/G billion. EnergyLedgerDiagnostic (3b)'s France: G = 1.084 at year 10, so 7.7 % of an extra subsidy reaches no bill, a share that grows with G over the run.
- **The class doc still says otherwise:** "The scheme's cost is one sum ... support moved above its indexed path takes the levy down one for one" (`:24`, unedited). That is now false in every book.
- **The page now says "IN PROPORTION",** which does not tell the player that a billion moves less than a billion.
- **Scenario:** a player raises the subsidy late in a run and pays G times the levy relief in budget money.
- **Fix:** a K-line that extends P-A's form to the player's book knowingly (and fixes `:24`), or carry G in the levy's path.

**M4 - wave/uspop: one textual conflict, and a new semantic coupling through F4.**

- **The conflict:** `SaveGameService.cs:113` (trivial: keep uspop's "Nor for K299" note above `70: fisc2`).
- **The coupling:** F4 makes the population drive the USA's function-600 line (2.4 % of GDP) in every book. uspop's CBO-2025 population path will therefore move:
  - the dump (both families);
  - the USA (h3) hold (+15.2 / +63.6 against +19.6 / +74.5 today);
  - (d3)'s USA marks, through the age-cost index of the health lines;
  - ParticipationCohort's K290 line, which uspop lifts on a -2.69 % measured without fisc2.
- **Fix:** whichever lands second re-runs `hold.sh`, `anchors.sh` / `diag.sh LongRunAnchorsDiagnostic` and ParticipationCohort on the merged tree, and names the dump's family after both.

### Low

**L1 - Sweden (h2) is lifted on a 0.2-point margin.** It reads -11.2 against the band's floor of -11.4 (`DebtPathHoldDiagnostic.cs:68-76`). Out of the engine is "a prediction to the printed tenth at best". If the integrator's Unity hold prints -11.5, the lifted band FAILS. Its decomposition still shows the primary-balance gap the old registration named (model -pb -6.7 against the DSM's -1.1 to 2035). The Unity run decides; do not re-register on a Mono difference without a K-line.

**L2 - R1's sources support it in direction, not exactly** (hunt list 1). CBO's in-window discretionary falls 5.894 → 4.641 % on prices alone. The AR's health baseline is GDP per capita; R1 is the labour-intensity scenario. The DSM's constant share holds for R1's driverless lines, not its non-ageing caseload lines. `SimulationManager.cs:8451-8457` should say "in direction" and name the three gaps.

**L3 - The save comment's magnitude is wrong.** "A one-time jump of a third or more" (`SaveGameService.cs:113`): `DriverReference` held a rate of 3.5-8, and the population level is in millions (Sweden 10.7, Germany 83.5, USA 340.1). A v69 line would index by about ×1.3 (Sweden) to ×85 (USA) before the seed band's clamp. This is moot, since v69 is refused, but the record should say so. The play saves must be re-staged at integration.

**L4 - SpendingIndexationDiagnostic's driverless tolerance is 20× looser** (1e-4 → 2e-3, `:32`). Over five years it cannot see a dropped year of real growth for Italy, whose seeded potential growth is 0.119 % a year. Read `PotentialGrowthRate` at the index's own moment instead, or keep 2e-3 only where the reading genuinely differs.

**L5 - UnemploymentFloorPhillipsDiagnostic (g) reads its expected real term from the production function.** It takes `ProjectedRealTerm` (`:407`), so it checks the composition, not the value. It does not run out of the engine, so the edit is unverified until the Unity sim bar.

**L6 - LongRunAnchors' PASS line overclaims.** It still says "every country's life-expectancy gain inside its band" (`:137`) while it prints REGISTERED (d3) marks.

**L7 - Two docs are stale.** `EnergyLedger.cs:24` says "one for one" (M3). `docs/reference/MODEL_REFERENCE.md:3607` says the debt chart draws `ComfortableDebtToGdpPercent` ("Comfortable").

**L8 - The century is held by revenue the economy does not feel** (hunt list 4). This is K251's critique, now the reaction's documented job. It is DECLARED and filed (the pension line's benefit ratio), so it is not this item's to fix, but the record should not call the century plausible.

**L9 - The three framework laws no longer move anything the player sees.** A player who enacts the Debt Anchor Act sees no line move: the chart now draws the framework's fixed figure, and the law moves only the reaction's neutral point. This is DECLARED at `LawCatalog.cs:2051-2054` and filed with K269 (a); a one-line note on the law card would remove the surprise.

## Conditions for integration

1. H1: A1's miss reported in the item with its measurement; a K-line on landing R1 with A1 missed (band unchanged).
2. M1: a K-line for the (d3) registration, keyed and bounded; a dated prereg amendment; the PASS line corrected.
3. M2: the population driver declared ([AUTHORED-DRAFT], the drift stated) in `SpendingDrivers.cs` and the save comment.
4. M3: a K-line on the player's energy pass-through, and `EnergyLedger.cs:24` corrected.
5. K-lines also owed for: R1 itself (answering K269 (a)), F4, Part 2's outcome (the FRF stays) and K269 (d).
6. The usual integration steps:
   - ledger rows for `SimulationManager.cs`, `SpendingDrivers.cs`, `SaveGameService.cs`, `EnergyLedger.cs`;
   - the dump family `fisc2` re-pinned (the sentinel's digests);
   - play saves re-staged for format 70;
   - a dry film of the item 7 surfaces;
   - the Unity sim bar, for DebtPathHold (Sweden (h2), L1), LongRunAnchors, HospitalWaits, UnemploymentFloorPhillips (L5), EnergyLedger, LineIndexation and SpendingIndexation.

## Verdict

**Verdict: NOT READY.** The code does what it claims, and every measured figure I re-ran reproduces byte for byte. But the prereg's acceptance A1 failed in four books with no report (H1), and a failing check was turned into a registration after measurement with no ruling (M1). Both are fixable without new code: report, rule, bound. Once conditions 1-4 are met, the branch can land on its bars.

## Round 2 (2026-10-29): the rework, `git diff 4ac720bb..e5202d60` (six commits)

Read-only again, no Unity run. **Files reviewed in this round:** `Assets/Editor/EnergyLedgerDiagnostic.cs`, `Assets/Editor/LongRunAnchorsDiagnostic.cs`, `Assets/Editor/SpendingIndexationDiagnostic.cs`, `Assets/Scripts/Data/Country.cs`, `Assets/Scripts/Data/SpendingDrivers.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, `Assets/Scripts/Simulation/EnergyLedger.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/UI/GameController.Energy.cs`, `Assets/Scripts/UI/GameController.EnergyV35.cs`, `Assets/Scripts/UI/GameController.LawsV35.cs`, `Assets/Scripts/UI/RangeCaptions.cs`, `Reviews/2026-10-29_fisc2_prereg.md`, `docs/reference/MODEL_REFERENCE.md`. For the new field I also read `ClonePreviewCountry`, `PreviewParityDiagnostic`'s clone audit, `ShadowBaseline`'s fork (a save round trip) and the save's Newtonsoft settings.

### What I ran on e5202d60

| Instrument | Result |
|---|---|
| `gate.sh` | **GATE OK** |
| `frf_play.sh <wt> on none 100`, seeds 777 and 424242 | **byte-identical** to round 1's HEAD 4ac720bb on both seeds: the all-AI world does not move |
| `player_book.sh play`, six players, 777 and 424242 | **byte-identical** to 4ac720bb: the idle player book does not move. At no policy the support's deviation is 0, so what the rule carries multiplies 0 |
| `hold.sh` | byte-identical to 4ac720bb (the no-policy-change book never touches the field) |
| `diag.sh EnergyLedgerDiagnostic` | PASS. Played France at year 10: G 1.0840, carried 1.0840, share-of-path = one-for-one (apart by 1.2e-7), pass-through **1.0000**. Under its ministry: carried 1, pass-through 0.9228 = 1/G. Both reproduce the commit's figures |
| `diag.sh LongRunAnchorsDiagnostic` | PASS, 3 REGISTERED (K298): USA at years 50 / 60 / 70, with treatable parts +0.22 / +0.24 / +0.25 (the doc's "0.24" for year 70 rounds the printed gains) and the rest inside. Year 40 passes outright |
| `diag.sh SpendingIndexationDiagnostic` at 1e-4 | PASS. An instrumented scratch copy (not the repo's) prints the widest relative miss: **2.35e-7 on driverless lines**, 3.63e-7 on driver lines |
| A scratch probe on the out-of-engine runtime (`player_book.sh`'s build with my own entry; not in the repo) | France, seed 777, 20 years, the government switched every five years by `PlayerCountryId`. Results below |

### Each finding

| Round 1 | Status | Evidence |
|---|---|---|
| **H1** A1 missed, unreported | **Fixed** | Prereg note 1 (8f617112, alone) reports Sweden +3.86 / +3.88 and Poland +3.42 / +3.41, +3.79 / +3.71 against the unchanged ±3.0 band. Its attribution matches my round-1 reading: the creep's participation term shrinks the played economy. Italy, which has no creep, reads 0.00, which supports it. B3's failure in the same books is stated. The FRF doc (`SimulationManager.cs:702-705`) now names the miss and the revenue "the economy does not feel". The disposition is K310, a draft not yet on main |
| **M1** (d3) registered after the fact | **Fixed** | See "the (d3) bound" below |
| **M2** the population driver mis-cited | **Fixed** | `SpendingDrivers.cs:75-80`: [AUTHORED-DRAFT] (K294). It says plainly that this is not the DSM's convention and why, gives the measured drift (Poland 13.35 → 15.77, Germany 11.80 → 12.81, Sweden 9.58 → 9.90, my round-1 figures), says the driver was not re-chosen, and files the driverless alternative |
| **M3** energy pass-through 1/G | **Fixed for an unbroken player tenure; open after any AI spell** | See R2-M1 below |
| M4 the wave/uspop conflict | unchanged (not this rework's) | Re-checked: still one conflict at `SaveGameService.cs:113`. main has moved (K311 and the K300 prereg) and still merges cleanly |
| L1 Sweden (h2)'s 0.2-point margin | open, for the Unity run | The hold is unchanged at -11.2 against a floor of -11.4 |
| L2 the sources only "in direction" | **Fixed** | `SimulationManager.cs:8457-8461` names CBO's window (5.894 → 4.641 %), the Ageing Report's GDP-per-head health baseline, and the non-ageing caseload drift |
| L3 the save comment's magnitude | **Fixed** | `SaveGameService.cs:113`: "about x1.3 (Sweden) to about x85 (the USA) before the seed band's clamp", plus the new additive field |
| L4 the loosened tolerance | **Fixed** | See "the 1e-4 tolerance" below |
| L5 the Phillips (g) check | open, for the Unity bar | unchanged |
| L6 the PASS line | **Fixed** | `LongRunAnchorsDiagnostic.cs:142`: "every country's life-expectancy gain inside its band but the marks K298 registers" |
| L7 stale docs | **Fixed** | The `EnergyLedger.cs:24-27` class doc and `MODEL_REFERENCE.md:3607` |
| L8, L9 | L8 is now stated in the FRF doc; L9 is still declared only | — |

### M3's new field, `Country.EnergyLevyRealGrowth`

- **Additive: yes.** It is a public float, default 0, read as 1 (`EnergyLedger.cs:344`).
- **Saved: yes.** The save is Newtonsoft over public fields, so it is saved like `IncomeTaxThresholdLift`. The format stays 70, with the comment updated; v69 is refused anyway.
- **Cloned for previews: yes**, at `SimulationManager.cs:7873`. `PreviewParityDiagnostic`'s reflection audit walks every public field of `Country`, so a dropped clone line would fail it.
- **The shadow's fork:** it is a save round trip, so it carries the field.
- **Applied once per boundary: yes.** It is multiplied once per boundary in the one `IndexSpendingLines` call (`:8493`), only where `!aiBudget`, by the same `realGrowth` the driverless energy line takes (`SpendingCategory.Energy` has no driver). It is read in the one `LevyScaleWith`, which both `Compute` and the dial's preview use, and the line's path and the field move at the same boundary. There is no double application, and nothing moves between boundaries.
- **Reset when an AI government takes over: no.** It is frozen: "an AI book leaves it where it stands" (`Country.cs:189-193`). This keeps the levy continuous at a change of government, but it means the rule is neither of the two the docs state.

**R2-M1 (Medium) - one for one holds only if the player has governed every boundary since the seed.** `EnergyLedger.cs:24-34, 344, 391-393`; `Country.cs:189-194`; `GameController.Energy.cs:290`; `GameController.EnergyV35.cs:395-396`; `GameController.LawsV35.cs:548-549`.

- **The arithmetic:** the levy moved per billion of support is 1 / G_AI in *either* book, where G_AI is the real growth over the boundaries an AI government indexed. Measured with the probe (France, 777):

| History, five-year spells | Who governs at year 20 | Carried | Path's G | Pass-through | 1 / G_AI |
|---|---|---|---|---|---|
| player throughout | player | 1.2424 | 1.2424 | **1.0000** | 1.0000 |
| AI 1-10, player 11-20 | player | 1.1464 | 1.2424 | **0.9228** | 0.9228 |
| player 1-10, AI 11-20 | AI | 1.0840 | 1.2427 | **0.8722** | 0.8722 |
| AI, player, AI, player | player | 1.1297 | 1.2431 | **0.9088** | 0.9088 |

- **Who it reaches:** a player who starts in opposition, or who loses an election and later wins one back. The page then says "ONE FOR ONE" while a billion moves 0.92 of a billion: the same 7.7 % gap round 1 measured at year 10, now after a ten-year AI spell. After a player spell, the AI book's doc ("a billion over G", `:393`) is also false: 0.872 where the P-A rule gives 1/1.243 = 0.805. The bound P-A was built for still holds, because what is carried is constant in an AI spell.
- **Why the probe hides it:** EnergyLedgerDiagnostic (3b) probes only an unbroken player tenure and a world with no player, so it cannot see this.
- **Fix, either:**
  - **(a)** rule the continuity form by K297's text ("one for one over the boundaries the player indexed; after an AI spell a billion moves 1/G of that spell's growth, in either book"), correct the three docs, and add a mixed-history case to (3b) asserting 1/G_AI; or
  - **(b)** carry the path's whole growth every boundary and apply it only where the player governs, through a role flag stored at the boundary (a preview's clone carries no government). This makes the page true at the price of a jump in the levy at a change of government, K × share × (G - 1) on a standing deviation, which itself needs a ruling.
- **Recommendation:** (a). It needs no new arithmetic, but the page's "ONE FOR ONE" should then carry the condition, or the K-line should accept the gap as declared.

### The (d3) bound and its amendment

- **The amendment came before its code.** Amendment 1, a04361c8 at 19:44:42, is alone (one file, the prereg) and precedes its code, 60441609 at 19:45:32. It is labelled "Not preregistered ... written after R1 was measured, and it is dated so". That is the honest form.
- **The bound is real in code** (`LongRunAnchorsDiagnostic.cs:113-115, 438-442`):
  - It is keyed: a `HashSet` of exactly (USA, 50), (USA, 60), (USA, 70).
  - It is bounded: it registers only while the gain without the treatable excess is inside the band **and** `|gain - plain| <= 0.5`.
  - Everything else fails, with its treatable part printed.
- **It has room:** the measured parts are 0.22-0.25 against the 0.5 bound, so the treatable term can roughly double before the check fails.
- **What remains:** K298 is a draft, to be recorded on main.

### SpendingIndexation's 1e-4

- **It is sound.** The widest driverless miss is 2.35e-7 (reproduced, against the commit's 2.4e-7), about 400 times inside the tolerance.
- **It can see the case L4 worried about.** A dropped year of Italy's seeded potential growth (0.119 % a year, a relative 1.19e-3) fails by an order of magnitude.
- **The one blind spot:** a country-year whose growth rate sits within 0.01 point of zero, where any reading would be blind. That is negligible.
- **Round 1's 2e-3 was never needed:** the diagnostic's reading before the turn is the figure the boundary indexes with.

### New in this round (Low)

- **R2-L1:** `SaveGameService.cs` is now LF in the working tree (`w/lf`), where round 1 left it CRLF. The index is LF either way, so the commit is clean. This is cosmetic, but the next CRLF edit should not double the CRs (count with perl, not grep).
- **R2-L2:** K294, K297, K298 and K310 are drafts that main does not yet hold. Main now has K311 (Elias), with no collision. They must be recorded on main before the merge; K297's text should carry R2-M1's resolution.

### Round 2 conditions for integration

1. R2-M1 resolved by (a) or (b), with K297 recorded to match, the three docs corrected, and a mixed-history case in EnergyLedgerDiagnostic (3b).
2. K294, K297, K298 and K310 recorded on main, along with round 1's owed K-lines (R1 itself, F4, Part 2's outcome, K269 (d)).
3. The Unity sim bar for: DebtPathHold (Sweden (h2)'s margin, L1), LongRunAnchors, EnergyLedger, SpendingIndexation, LineIndexation, HospitalWaits, UnemploymentFloorPhillips (L5).
4. The usual steps, as round 1 listed them: the ledger rows, the `fisc2` dump family re-pinned (unchanged by this rework: the all-AI world is byte-identical), the play saves re-staged for format 70, and a dry film of the energy strings, which are now back to round 0's "ONE FOR ONE".
5. The wave/uspop merge handled as in M4.

**Verdict (round 2): READY**, on condition 1 (R2-M1 ruled, documented and probed) and condition 2 (the K-lines recorded). H1, M1, M2, L2, L3, L4, L6 and L7 are fixed, and every byte-identity claim reproduces on both seeds. The one open item is M3's residual after an AI spell. Option (a) settles it with a ruling, docs and a probe case; option (b) would need a small code change.
