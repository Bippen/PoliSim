# Review: PN-1's other half - the participation response of the bands a pension age crosses (2026-09-23, s596)

**What was reviewed.** The §596 change: `Assets/Scripts/Data/PensionParticipationResponse.cs` (new), `ParticipationRateTable.StructuralRate(Country)` and `PensionResponsePoints`, the four model readers of the structural rate (`Assets/Scripts/Simulation/MacroSystem.cs` twice, `WorldFactory`, `Country`), and - after the first read - the level shift (`Country.PensionParticipationApplied`, `MacroSystem.SyncPensionParticipation`, the preview clone's copy). The family moves: `pp3` against `ft14` (= `ft16` = `pn1d2`), explained per country in COMPLETED.md §596. The review is owed because the change books money and moves the baseline (the tier's clause).

**The form.** One independent read-only reader, told the change, the source and eight areas to hunt; then the same reader VERIFIED the fixes. The report is below VERBATIM - the first read, then its verification section - and what was done with each finding follows it.

---

# Adversarial review - §596 PN-1's other half (participation response to the pension age)

Read-only. Files read whole: PensionParticipationResponse.cs, PensionParticipationDiagnostic.cs, PensionParticipationProbe.cs, PensionAgeStatute.cs, pp1.diff; relevant spans of ParticipationRateTable.cs, MacroSystem.cs (418-447, 858-893, 2030-2043), SimulationManager.cs (267-273, 2053-2068, 2536-2542, 2734-2760, 2822-2830, 3144-3163, 3485-3489, 3585-3586), Country.cs (150-250), PotentialOutput.cs, AiFinanceMinistry.cs (80-140), UnemploymentRateByAgeTable.cs, SpendingDrivers.cs. Evidence re-read: rja.txt Table B.1; the probe's own output `PoliSim-captures/logs/s596_probe6.log` (line 429 on).

## Summary

Two defects, both at the level of how the response ENTERS the model rather than its arithmetic:

- **D1**: the FT-8 split produces the unemployment drop at a statute step, and that same drop drives the fiscal attribution. At France's 2028 step, participation did not move at all (dLFP 0.0000), yet unemployment fell 0.14 pts and potential rose 0.152 %. The ministry's "potential growth" read at that step is entirely this artefact. The shipped explanation, that the ministry reads a level step in labour as growth, is not what the probe shows.
- **D2**: `RateAtAge` looks up one five-year band, so the dial's LOWERING side jumps at a band edge in every country. France at 61 y 0 m → 60 y 11 m moves about 290 k people, roughly 0.9 % of the labour force, in one month of dial.

The arithmetic otherwise holds: the overlap, the sign, the cap, the units, the seed reference and numeric inertness are all clean. The sourcing checks out: Table B.1 gives eight increases with a median of 0.425, and Vestad is excluded.

---

## D1 - DEFECT: the step enters as an anchor jump, so the FT-8 split reads it as a negative cyclical shock. That shock, not labour, drives the ministry attribution
**Where:** MacroSystem.cs:422-446 (the split), used through ParticipationRateTable.cs:80-91. It combines with MacroSystem.cs:878-892, where the state rate reverts to the anchor at 0.15 a year, applied daily.

**Mechanism (verified in code).** The response is added to the ANCHOR only. At the boundary where a statute step is in force, `deltaTrend = structural − StructuralParticipationAtLastBoundary` carries the whole step, but `deltaP` (the state's rate) carries only what the reversion has closed so far. On a Jan-1 boundary that is one day's worth, and after about a year only ~15 %. So `impact = (deltaP − deltaTrend)·(100−U)/p < 0`, and unemployment falls on impact. In later years the rate catches up with deltaTrend = 0, so the impacts turn positive. These sum to zero over time, but the 0.4 absorption decays the early negative excess first.

**Measured (s596_probe6.log, France, on − off):**
| turn | year | dLFP (pts) | dU (pts) | potential off → on |
|---|---|---|---|---|
| 2 (2028 step, boundary 2028-01-01) | 2028 | **0.0000** | **−0.1399** | 3218.785 → 3223.691 (+0.152 %) |
| 4 (2029 step) | 2029 | 0.0438 | −0.1946 | 3235.953 → 3245.430 (+0.29 %; U ≈ 0.21 of it, LF ≈ 0.08) |
| 6 / 8 | 2031 / 2033 | 0.127 / 0.209 | −0.24 / −0.22 | |
The same artefact appears at the other countries' first steps: turn 1 gives USA −0.029, Germany −0.017 and Italy −0.017 in dU, with dLFP 0.

**Why this is the attribution too.** The ministry's expenditure excess at turn 2 moved from 0.0215 to 0.0153, a 0.62-pt difference, with dLFP exactly 0. Two paths account for it:
1. `PotentialGrowthRate` is finalised in ApplySectorGrowthEffect (MacroSystem.cs:2037) after the supply shock (SimulationManager.cs:2826). `LabourInput` = LF × (1−U), so the U drop alone raises potential growth by about 0.15 pt, and the benchmark rises with it.
2. The spending lines are resolved after the shock (SimulationManager.cs:2828). France's `IncomeSecurity` (M, UnemploymentRate driver, ~21 % of the lines) and `LaborMarket` lines index on U. U fell 1.7 % (8.2076 → 8.0677), which takes about 0.4 pt off the lines' growth.

Together these are about 0.55-0.6 pt, which is the whole turn-2 move. So the family's "the ministry reads the step as potential growth and cuts less" is, at the step turns that matter, **"the ministry reads the FT-8 artefact's lower unemployment"**. The century debt's seed-dependent sign (+3.5 % / −2.3 %) rides on it. The code comment (PensionParticipationResponse.cs:26-28) says the added participants "meet the country's unemployment like any other", which means the INTENDED effect on U is neutral. The observed effect is negative on impact.

**Against the source.** The mechanism in Table B.1 is "people stay in their pre-age state" and happens at once. The studies find unemployment/UI inflow RISING: Staubli & Zweimüller, and the paper's own NLD table B.1, where unemployment is +3.6 to +4.0 pp at cutoffs 3-6. The model gives a transient FALL. It also spreads a mechanical, same-year effect over years: France's labour force has realised about 0.31 of the ~0.43 structural pts by 2037.

**Whose defect.** The split (FT-8 §398) and the reversion are pre-existing. They were built for a smooth demographic drift, where deltaP ≈ deltaTrend in steady state. This change is the first thing to feed a DISCRETE anchor step through them, so the defect is this change's integration choice.

**Suggested fix.** Apply the response as a LEVEL shift to the state's rate as well as to the anchor. Keep `Country.PensionParticipationExtraAppliedPts` (saved; 0 at seed). In `ApplyLaborForceParticipationRate`, before the reversion, add `(currentExtraPts − applied)` to `state.LaborForceParticipationRate` and store it. Then deltaP and deltaTrend carry the step together, the FT-8 impact is zero (neutral U, the code's stated intent) and the labour force responds in the step's year (the source's mechanism). This stays inert while the delta is exactly 0f. Re-run the probe afterwards: the France fiscal story will change, and the "level read as growth" line (D4 below) then becomes the whole remaining ministry effect. If the ruling is instead that retained workers should raise U (the source's substitution direction), exclude the pension component from `deltaTrend` so that it arrives as a supply shock. That is a different model and needs its own ruling.

## D2 - DEFECT: the lowering side is discontinuous at band edges. `RateAtAge` reads one five-year band
**Where:** PensionParticipationResponse.cs:69 (`RateAtAge(rates, lower − 1f)`) and :86-90.

For a raise, `lower` is the seed age (fixed), so the step is constant and the response is continuous. For a LOWERING, `lower` is the dial's age, and the step jumps whenever `lower − 1` crosses a multiple of 5, that is at lower = 61, 66 or 71. Every country has one such edge inside ±2 years of its seed, except France, whose edge sits 1 y 9 m below its seed (at 61) and also falls inside the 60-70 track. Worked cases:
- **France** (seed 62.75, h 0.50). At 61 y 0 m: step = 0.452 × 0.5 = 0.226, share 1.75/5, loss **0.079 × band 60-64**. At 60 y 11 m: step = 0.816 × 0.5 = 0.408 (band 55-59), share 0.367, loss **0.150 × band 60-64**. One month of dial moves about 0.071 × 4.1 M ≈ 290 k people, roughly 0.9 % of the labour force.
- **Sweden** (seed 67, h 0.425). At 66 y 0 m: step 0.312 × 0.425 = 0.133, share 0.2, loss 0.027 × band 65-69. At 65 y 11 m: step 0.744 × 0.425 = 0.316, capped at the band rate 0.312, share 0.217, loss 0.068 × band 65-69, a 2.5× jump. At 65 the cap binds: ages 65-67 lose their whole band-average participation, although the hazard says 42.5 %.
- **Germany** (seed 66 y 4 m): 66 y 0 m vs 65 y 11 m gives a loss of 0.0027 vs 0.0108 × band 65-69, a 4× jump.

This also drives the asymmetry that sec596 explains as "a lowered age removes more than a raised one adds"; part of it is this band edge. The player's dial moves in months (GameController.cs:10646), so the jump is reachable in one click.

**Fix.** Make `RateAtAge` continuous: linearly interpolate between band midpoints, so that the rate at age a uses bands ⌊(a−2.5)/5⌋ and the next. Consider capping against the interpolated age-specific rate rather than the band average. The same interpolation also answers L2.

## Limitations (stated or worth stating)
- **L1** (PensionParticipationResponse.cs:69 with Table B.1). The model takes the BUNCHING (rate × hazard) as the effect. In Table B.1 effect/bunching for the eight increases is 9.8/14, 11/14, 6.3/14, 10/15, 20.9/23, 13.5/12, 4.5/8 and 20.3/20, a median of about 0.75 (range 0.45-1.13). The paper says the product holds "in the absence of active substitution". Participation versus employment covers substitution into unemployment but not into disability or inactivity. So the step is an upper-leaning read. This is stated only partly (lines 26-28).
- **L2** (ParticipationRateTable bands). The pre-age rate is a five-year band average that includes post-age ages, so it UNDERSTATES the rate just before the age where the band straddles it. Germany at 65 y 4 m reads the 65-69 band's 21.6 %. The USA reads the single BLS 65+ rate of 19.5 % for 65 y 10 m, where the real rate at 65-66 is about twice that. Germany and USA steps are therefore low. The interpolation in D2 only helps partially, because the USA's 65+ has no shape at all.
- **L3** (the hazards). Germany's 0.19 is a women's EARLY-retirement-age hazard (Geyer & Welteke). Choosing it over the table median for the Regelaltersgrenze is a judgement call. It is stated, but a reviewer could argue the median is the better proxy for an SRA. France's 0.50 (Rabaté & Rochut, the âge légal 60→61) matches L161-17-2's kind of age. That is correct.
- **L4** (the seed reference; stated). The tables are 2024 figures and the reference is the 2026 age. France's 2024 age in force was about 62 y 3 m to 62 y 6 m, so about 0.25-0.5 y of the schedule's move is embedded in neither the tables nor the response.
- **L5** (a save from before §596; no version bump). A v22 save taken with a statute step already past the seed (France from 2028, Germany/USA/Italy from 2027) or with a passed §590 override carries `StructuralParticipationAtLastBoundary` without the response. On load, the whole accumulated response arrives at the next boundary as a single anchor step. D1's artefact then fires at full size: France in 2031 gets a structural jump of ~0.35 pts and a U drop of ~0.5 pts. This is a model change, not a field misread, so no bump is strictly needed under the save-version convention. Any staged play save (CL-3/CL-4) should be re-staged after this lands.
- **L6** (composition). The extra older participants are not in `UnemploymentRateByAgeTable.CompositionRate`, which weights by the table's participation. Older workers' lower unemployment rates would lower the natural rate slightly. This is an omission, not a double count.

## D4 - LIMITATION (pre-existing): the ministry reads the one-year potential growth
AiFinanceMinistry.cs:100-108 builds the benchmark on this turn's `PotentialGrowthRate`, which is derived from last turn's labour growth (PotentialOutput.cs:74-79, MacroSystem.cs:2037). The real rule (Reg. 2024/1263) uses a medium-term potential-growth estimate. After D1 is fixed, a France step of ~0.086 pts of participation is about 0.155 % labour growth in one year, and so a benchmark ~0.155 pt higher for one year. That happens five times over 2028-2033. The cumulative permitted level is about the same as under a ten-year average; only the timing differs. The ministry does respond to a pension reform's potential in the direction the real framework does. This is a pre-existing design issue and not this change's defect. Its size is small beside D1's U-driven line effect.

## Nits
- **N1** (PensionParticipationDiagnostic.cs:81-83). Assertion (4) is tautological. `SpendingDrivers.Level(StatutoryPensionAge, c)` (SpendingDrivers.cs:91) is `PensionersMillions(c, AgeInForce(c, c.CalendarYear))`, and with the override set that is the same expression as `heads`. It cannot fail from anything this change touches. The real guarantee is that the diff leaves the driver path alone, which is true. Say so rather than asserting it.
- **N2** (PensionParticipationDiagnostic.cs:49-51). Assertion (1) sets CalendarYear to the seed and the override to −1, which forces `now == reference` and the early return. It is also tautological. The live seed path (WorldFactory.cs:1057, Country.cs:240) is the one worth asserting: `CalendarYear == SeedYear` at `CaptureStructuralBases`. It holds today; see Clean.
- **N3** (pre-existing, now with one more reader). CalendarYear commits on Jan 1 in AdvanceDay (SimulationManager.cs:270), but boundaries are every 365 days. From turn 3 the boundary lands on Dec 31 and then drifts earlier. The probe shows `year 2028` at both turn 2 and turn 3. Statute steps therefore land on the boundary for turns 1-2 and the day after it later. `AiEnergyMinistry` uses `SeedYear + CurrentTurn + 1`, a different convention. This came with §520.
- **N4** (preview). The clone copies CalendarYear (SimulationManager.cs:3585) and so reads the CURRENT year. At the turn-1 and turn-2 boundaries (Jan 1) the real turn reads the next year, so the preview misses that turn's statute step and the FT-8 U drop (France −0.14 at turn 2). The same mismatch has existed for the pension line's driver since §520. It is invisible for Sweden, whose statute holds, so PreviewParityDiagnostic cannot catch it.

## Areas checked and clean
- **Band overlap arithmetic** (lines 72-81): `[start, start+5)` against `[lower, upper)` is correct at the edges. An upper of exactly 65 gives overlap 0 in band 13, which is skipped. The open band (index 20, `end = MaxValue`, share 0) is unreachable anyway because PensionAgeMax is 70. The loop starts at band 3 like the base.
- **Sign and cap**: raising gives +, `room = 1 − rate`; lowering gives −, `room = rate`. Each band's rate stays in 0..1. A −0f from a zero-room band compares equal to 0f.
- **Units**: `extra` is in count units over the same Σ counts[3..20] base as the pyramid (ParticipationRateTable.cs:66-70, 88-90). `PotentialOutput.Population15Plus` = `InAgeRange(15, 999)` is the same population. `100·extra/base` is in percentage points, consistent with `StructuralRate`.
- **"Year before the lower age"** is the right age for both directions: the people crossing were in their pre-age state at lower−1. Only its band granularity is a problem (D2).
- **Source figures**: Table B.1 (rja.txt line 105) matches the XML doc line for line. The eight increases' hazards are 0.50, 0.25, 0.25, 0.35, 0.50, 0.19, 0.50, 0.70, whose sorted median is (0.35+0.50)/2 = **0.425**. Vestad NOR 64→62 (0.46) is a decrease, and Atalay & Barrett report no hazard. Both are correctly excluded.
- **Seed reference**: `Country.CalendarYear` initialises to SeedYear (Country.cs:162). No WorldFactory code writes it before WorldFactory.cs:1057 or the `CaptureStructuralBases` loop at :1079, so the response is zero at both seed readers. It is written only by `CommitCalendarYear` (SetWorld, RestoreSaveState, AdvanceDay), the clone and the diagnostic, which restores it. AI countries get the same commit, and the AI never sets `PensionAgeOverride`. The trajectory dump goes through SetWorld. The probe confirms dLFP = 0 for every country at turn 1.
- **Numeric inertness**: `StructuralRate(Country)` returns the same float local when `extra == 0f`. NaN handling matches the old `Cohorts != null ? … : NaN` at all four sites (MacroSystem.cs:422, :878, WorldFactory.cs:1057, Country.cs:240). Sweden's statute is 67 throughout (carried after 2032) and Poland's is fixed, with no override, so their own `ExtraActive` is 0 every day. The probe shows both at exactly 0 through turn 3 and in the 4th decimal afterwards: spillover only.
- **Double counting**: the only labour channel is the anchor, feeding PN-3's LabourForce/LabourInput and PN-3b's wage bill, which is intended. The pension line's driver path is untouched by the diff. TaxSchedule reads counts only. Nothing else reads ParticipationRateTable except CompositionRate (L6).
- **ProbeSuspended**: set only in the probe, inside try/finally (PensionParticipationProbe.cs:31-38), and read only by `ExtraActive`. A process kill takes the static with it. No bar or runtime code sets it.
- **Preview and drafts**: `PolicyDecision` carries no pension age. A DRAFTED age (in a BudgetBill) reaches `PensionAgeOverride` only on passage (ParliamentSystem.cs:512), so neither the participation arrow nor the pension line previews a draft. That is consistent. A PASSED override is copied to the clone (SimulationManager.cs:3586), so the preview carries its response, which is also consistent. `StructuralParticipationAtLastBoundary` rides the clone (:3489).

---

## Verification of fixes

Read-only. Files re-read: MacroSystem.cs 858-900, ParticipationRateTable.cs 60-100, PensionParticipationResponse.cs 55-100, Country.cs 160-176, SimulationManager.cs 3485-3489 and 3585-3587, PensionParticipationDiagnostic.cs 88-100, PreviewParityDiagnostic.cs 262-333, GameController.cs 770-800 (the day order), ParliamentSystem.cs 510-512. Evidence: the probe section of `PoliSim-captures/logs/s596c_diag.log` (line 675 on).

**Verdict:** D1 and D2 are fixed. There is one leftover edge in D1: a bill that passes on a boundary day, and the preview during the signing pause after a bill. The fix is small. The new attribution is right for turn 2 and about half of turn 4.

### (a) The level shift, path by path
- **The step itself.** Probe, France turn 2: dLFP +0.0952 and dU +0.0001. Potential off 3218.785 against on 3224.295 is +0.171 %, which is exactly 0.0952/55.6. The artefact is gone. The shift carries the same points the anchor gains through `StructuralRate`, so the gap to the target is unchanged. **No double application.**
- **Daily step.** `ApplyLaborForceParticipationRateDaily` → MacroSystem.cs:864-869 runs after `CommitCalendarYear` (SimulationManager.cs:270). A statute step on Jan 1 is shifted the same day. **Clean.**
- **Boundary FT-8 split.** For turns 1-2 the boundary is on Jan 1: AdvanceDay commits the year, the daily step shifts the rate, then AdvanceTurn runs the split, so deltaP and deltaTrend both carry the step. From turn 3 on the boundary is on Dec 31: the step lands the next day and both deltas carry it at the following boundary. **Clean.**
  - Residual nit: the cohorts commit at the top of AdvanceTurn. The split then reads `points` on the new counts, but the shift for that pyramid-driven part only happens on the next day's daily step. That is a one-year lag of a change of about 1-2 % of `points`, on the order of 0.005 pts, and it cancels in steady drift. Negligible.
- **A bill that passes. LIMITATION (the remaining edge).**
  - In play the day runs AdvanceDay (the daily shift) → `AdvanceCountryDayTick` (bills resolve; ParliamentSystem.cs:512 sets `PensionAgeOverride`) → `AdvanceTurn` if it is a boundary (GameController.cs:775-792).
  - A pension-age bill that resolves on a boundary day therefore reaches the split with the anchor already moved but the rate not yet shifted. D1's artefact fires once: a U drop of about 1.6 × the response's points. The next boundary reverses it.
  - The **preview** hits the same stale state whenever it runs between passage and the next day's step. The clone copies `Applied` stale, and `ApplySupplyShockToUnemployment(previewCountry)` (SimulationManager.cs:3162) runs before the clone's turn-form participation step (:3246). The clock stops on `_signingQueue` right after passage, so a preview in the signing pause of a pension-age bill shows the spurious U drop.
  - **Fix:** extract the four lines into `MacroSystem.SyncPensionParticipation(country)` and call it at the top of both `ApplySupplyShockToUnemployment` (before `p` is read) and `ApplyLaborForceParticipationRate`. It is idempotent and a no-op at `points == Applied`. Alternatively, call it at ParliamentSystem.cs:512 right after the override is set.
- **Save load.** Saves are Newtonsoft world-as-is (SaveGameService.cs:135/165), so the new public field round-trips and an old save reads 0.
  - A save from **before §596** (anchor without the response) takes the whole standing response as one shift on its first day. The saved `StructuralParticipationAtLastBoundary` lacks the response, so the next boundary sees it in both deltas → zero impact. The old L5 is fixed.
  - Nit: a save written by the **pre-fix §596 build** (the pp1 tree, whose rate had partly reverted toward the response-bearing anchor) also reads Applied = 0. It gets the full shift on top of what reversion already added, overshooting by up to that part, which then decays. This only matters if such a save was kept, for example a staged play save made today. Re-stage it.
- **AI countries.** The daily step runs for every country and `Applied` is per country. AI governments never set the override, so they only move at statute steps. **Clean.**
- **Numeric inertness.** With `points == 0f` and `Applied == 0f` the branch is skipped. `StructuralRate` returns the same `pyramid` float. `PensionResponsePoints` returns a literal 0f when `extra == 0f`, which also catches −0f. Nothing else is written. **Clean.** The probe's "off" world (ProbeSuspended) holds `Applied` at 0.
- The diagnostic's check (6) exercises the shift with the reversion at 0 and restores the state. **Clean.**

### (b) Hand-lists
- **Clone:** covered. SimulationManager.cs:3587 copies the field, and PreviewParityDiagnostic's clone audit enumerates every public instance field of `Country` by reflection (PreviewParityDiagnostic.cs:270-310), so it would have flagged an omission anyway.
- **Save:** no hand-list, because the serialisation is reflective. No version bump is needed, since a 0 read is the intended "apply on first day" behaviour; see the pre-fix-build nit above.
- **Other copies:** no other code copies `Country` fields. A grep for `PensionAgeOverride = country` / `CalendarYear = country` finds only the clone. JobsLagDiagnostic.cs:40/59/62 overwrites the participation state directly, but on worlds where `Applied` = 0 = points. **Clean.**

### (c) RateAtAge interpolation (PensionParticipationResponse.cs:89-99)
- **Continuity.** Midpoints at b·5+2.5, linear between them, flat outside [band 0 midpoint, band n−1 midpoint]. `step`, `share` and `overlap` are continuous in the age, and `min(step, room)` with a per-band constant `room` is continuous, so `ExtraActive` is continuous in the dial now. The coordinator's month sweep (largest move 0.03-0.06 pts) agrees. **D2 fixed.**
- **Edges.**
  - The track is 60-70, so `lower − 1` lies in 59-69 and `position` in about 11.3-13.3: always between bands 11-14.
  - The zero bands 0-2 (ages 0-14) and the EU 75+ zeros are unreachable, so the interpolation never blends toward them.
  - Were a track ever to reach about 72.5+, the EU tables' 75+ zero would blend in (a statement about the survey frame, not people), but nothing reaches it.
  - The flat ends are right.
- **Raising reads sensibly.** Recomputed from the tables: the pre-age rates now sit between the band before and the band after, nearer the age.

  | Country | Reads at age | Rate | Hazard | Step | Before |
  |---|---|---|---|---|---|
  | France | 61.75 | 0.507 | 0.50 | 25.3 pp | 22.6 |
  | Germany | 65.33 | 0.419 | 0.19 | 8.0 | 4.1 |
  | USA | 65.83 | 0.324 | 0.425 | 13.8 | 8.3 |
  | Italy | 66 | 0.262 | 0.425 | 11.1 | 7.0 |
  | Sweden | 66 | 0.442 | 0.425 | 18.8 | 13.3 |
  | Poland | 64 | 0.344 | 0.425 | 14.6 | 18.7 |

  This largely answers L2, since the band averages were diluted by post-age years.
- **Note:** France's step is now 25.3 pp against Rabaté & Rochut's 20.9. The diagnostic's check (5) tolerance is ±5, so it passes with 0.6 pp to spare. Given L1 (bunching > effect, median ratio ~0.75), France now leans further above its own study. State this in the row. Don't widen the tolerance silently.

### (d) The attribution of France's residual ministry effect
- **Turn 2 (the 2028 step): right, and complete.** Excess 0.0215 → 0.0198, a change of 0.17 pt. Potential growth rose 0.171 pt (3218.785 → 3224.295 with dU ≈ 0 and dLFP +0.0952 = +0.171 %). The benchmark is (1+g)(1+π)−1, so it rises by about 0.174 pt. That is the entire move.
- **Turn 4 (the 2029 step): about half.** Excess 0.0182 → 0.0130, a change of 0.52 pt. Potential growth from turn 3 to 4 is −0.201 % off and +0.047 % on, a difference of 0.25 pt:
  - participation ratio −0.256 % off against −0.088 % on, a difference of +0.17 %;
  - (1−U) differs by about +0.08 %, because U rose 0.741 off against 0.668 on.

  The U-driven lines (IncomeSecurity + LaborMarket, about 23 % of the lines, U −0.36 % lower on) take about 0.08 pt off the lines' growth. That leaves about 0.19 pt, which the potential-growth reading does not explain. It is most likely the ministry's own path: at turn 2 the on-world asked −3.054 % against −3.228 %, so the line base the turn-4 ratio reads differs. Prices differ by only 0.007 pt, so they are not it.
- **The label.** "The labour step read as potential growth" is correct as the first-order cause and is the whole of it at the first step. From the second step on, part of the residual is the ministry's path dependence on its own earlier, smaller cut, plus a little from lower unemployment (the higher GDP's Okun effect, not the old artefact). The accurate family-row wording would be: "the ministry's benchmark reads the step's labour as potential growth (all of turn 2's 0.17 pt; about half of turn 4's 0.52 pt, the rest its own smaller earlier cut and the U-driven lines)".
- Checking the rest would need one more probe column printing `PotentialGrowthRate` and Σ `LastYearAmount` per turn.

**Remaining open after the fixes:**
- (a) the boundary-day / signing-pause edge, fixed by one helper;
- the pre-fix-build save nit;
- France's step now 25.3 vs 20.9 pp, to be stated;
- L1, L3, L4, L6 and D4 as before.

---

## What was done with each finding

- **D1 (the step as an anchor jump; the unemployment artefact drove the ministry) - FIXED.** The state's rate carries the response as a level shift (`Country.PensionParticipationApplied`, `MacroSystem.SyncPensionParticipation`), copied on the preview clone. Verified: France's turn-2 unemployment +0.0001 pts, participation +0.095; the family re-dumped (`pp2`, then `pp3` with the sync at both readers), labour force and output consistent in sign on both seeds.
- **The verification's edge (a bill passing on a boundary day; the preview's order) - FIXED** as the verification proposed: the sync is idempotent and called at the top of BOTH readers of the gap, `ApplySupplyShockToUnemployment` and `ApplyLaborForceParticipationRate`.
- **D2 (the lowering side jumped at band edges) - FIXED.** `RateAtAge` reads by single year of age, linear between the bands' midpoints; the diagnostic sweeps every dial month by month and asserts no month moves participation by 0.1 pts (the largest now 0.03-0.06).
- **The attribution (d) - TAKEN as the verification worded it:** the potential-growth reading is all of turn 2's 0.17 pt fall in the expenditure excess and about half of turn 4's 0.52 pt, the rest the ministry's own smaller earlier cut and the unemployment-driven lines.
- **Limitations - STATED in §596 and PN-1's row, not built:** the product rate × hazard leans high (measured effect a median 0.75 of it across Table B.1) - France 25.3 pp against the 20.9 measured, 0.6 pp inside the check, stated rather than the tolerance widened; Germany's 0.19 is a women's ERA hazard; part of France's schedule sits in neither the 2024 tables nor the response; the extra older workers are not in the natural rate's age mix; the ministry reads a level step as potential growth (older than this change).
- **Nits:** the diagnostic's checks (1) and (4) could not fail - (4) now compares the driver with the response on against suspended; (1) compares the country overload with the pyramid's own form, which fails if the response is non-zero at the seed. A play save written by today's pre-fix build would take the shift twice - none was staged. The calendar-year drift after 2028 is §520's, recorded there.

- **The baseline this record covers:** `traj_pp3`, seed 777 digest 45ef1055382d9ef31c9b779e932f4cabbdab46f33cea68d1d9402252732a49ca and seed 424242 digest f9e2af13e4fa24985798baf7102c7e5d259b3c57a425e0c7ada76d196af28b42 (turns 1-20, the sentinel's text). The reader read `pp1` and verified the fixes behind `pp2`; `pp3` is `pp2` with the verification's own proposal applied (the sync at both readers of the gap) - it moves the USA's first boundary in the fifth decimal place and the century figures by at most 0.1 %, stated per country in COMPLETED.md s596.
