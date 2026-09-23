# Review: FT-16 - the euro zone's rate weighted by HICP country weights (2026-09-23, s585)

**What was reviewed.** The FT-16 change: `Assets/Scripts/Simulation/EurozoneRateSystem.cs` (the blend's weight, real GDP → household consumption at current prices), the comment in `CurrencySystem.cs`, two on-screen strings in `GameController.cs`, `docs/reference/MODEL_REFERENCE.md`; and the BASELINE family it makes, `traj_ft16`, against `pn3b` - seed 777 digest `d11ce4c2…`, seed 424242 digest `19324156…`. No money path under the ledger's rows changed; the review is owed because the sentinel moved.

**The form.** One independent read-only reader, told the ruling, the cited source and the family. The report is below VERBATIM, then what was done with each finding.

---

# Adversarial review - FT-16 (euro-zone rate weighting), baseline `ft16`

**VERDICT: SIGN WITH FIXES.** The baseline is sound and the diff has a single cause. The digests are verified: seed 777 `d11ce4c2`, seed 424242 `19324156`. Three things are owed before the commit. (F1) A regression at day 0: before the first day, the blend falls back to the member's own reading. (F2) The on-screen sentence and the code comments claim more than the model does. (F3) Stale documentation lines.

Scope reviewed: the working tree against HEAD 7486f98. The changed files are `Assets/Scripts/Simulation/EurozoneRateSystem.cs`, `CurrencySystem.cs` (a comment), `Assets/Scripts/UI/GameController.cs`, `docs/reference/MODEL_REFERENCE.md`, `Assets/Editor/TrajectorySentinelCheck.cs` (label and digests) and `Tools/film_scope.tsv` (`dry585`).

## Evidence checked

- **Digests.** The sha256 of header + turns 1-20 of `traj_ft16_s777_t100.csv` is `d11ce4c293c0...447c`. For `traj_ft16_s424242_t100.csv` it is `193241569609...09be1`. Both match `TrajectorySentinelCheck.Expected`. The whole-file sha256 differs, as expected, because the digest spans only turns 1-20.
- **Control.** `traj_ctl585_s{777,424242}` is byte-identical to `traj_pn3b_*` (`cmp`, and the sha256 values match: 14a28e62... / ecf5a60b...). The whole diff is therefore this change.
- **Eurostat citation, verified by a fetch of `prc_hicp_esms` (metadata last update 4 February 2026).** It says, verbatim: "The country weights are derived from National Accounts data for the HFMCE expressed in euros." It also says HFMCE "is adjusted to exclude narcotics, imputed rentals for housing, prostitution, life insurance, FISIM, purchases abroad, and pension contributions are price updated to December of the previous year (y-1)". On coverage: "all products and services purchased in monetary transactions by households, both resident and non-resident (i.e 'domestic concept')". The weights are republished every year with the January data. The model recomputes them once per turn (one year), which matches that cadence.
- **Published weights, for comparison.** Eurostat `prc_hicp_cow`, COWEA, per mille:

  | Year | DE | FR | IT |
  |---|---|---|---|
  | 2022 | 282.78 | 205.01 | 164.28 |
  | 2023 | 276.91 | 195.77 | 166.03 |
  | 2024 | 270.82 | 195.66 | 165.84 |
  | 2025 | 275.92 | 191.08 | 158.68 |

  Normalised over the three members, 2023 gives **DE .434 / FR .307 / IT .260**.

## Findings, ranked

### F1 (fix before commit): a regression at day 0, where the blend becomes one member's own reading
`EurozoneRateSystem.cs:74-75` sets `HicpCountryWeight = max(0,C)*max(1e-4,P)`. `EconomyState.Consumption` is 0 at the seed: the constructor defaults it to `consumption = 0f` (`EconomyState.cs:394`), and `WorldFactory.cs:146-183` passes no `consumption:` for any country. The only writers are `MacroSystem.ApplyNationalAccounts{,Daily}` (`MacroSystem.cs:214, 643`). Until the first `AdvanceDay`, every member therefore weighs 0, and `GetBlendedSuggestedRate` (`EurozoneRateSystem.cs:68`) returns `TaylorRule.GetSuggestedInterestRate(zoneMember)`. That is the caller's own reading, not a blend.

- **Where it shows.** On a new game before the first day ticks, the central-bank page's "Blended rule reading this year" row (`GameController.cs:3421-3422`) prints the same figure as the "own reading" row directly below it. `SimulationManager.PreviewTurn`'s `blendedAtOpen` (`SimulationManager.cs:3159`) previews the member's own reading plus its push.
- **Why this is new.** Under GDP weighting the branch could not be reached, because `GDP >= MinGdp = 1`. The new comment at `EurozoneRateSystem.cs:72-73` says the fallback happens "as it always has". That is untrue: the fallback was dead code until this change.
- **The boundary and the harness are unaffected.** Every `AdvanceTurn` caller runs a full period of `AdvanceDay` first: `SimulationTestRunner.cs:176-185`, `ShadowBaseline.cs:138-139`, `PreviewParityDiagnostic.cs:79/211`, and the UiScreenshotDriver loops. The preview clone is also fine: `ClonePreviewCountry` (`SimulationManager.cs:3439`) uses `State.Clone()` = `MemberwiseClone`, so the clone carries both `Consumption` and `PriceLevel`.
- **Partial zero.** Consider one member at C=0 while the others are positive. That member loses its voice entirely, and a C-weight mixed with a fallback weight would mix bases. The case is practically unreachable after day 1. The rate factor is at least `1 - 15/100*0.5 = 0.925`, and the disposable-income delta would need a household burden about 0.83 of GDP above the seed. Still, the degenerate branch exists and is silent.
- **Fix.** Keep one basis per blend. In `GetBlendedSuggestedRate`, if any member (the caller included) has `Consumption <= 0`, weight every member by `State.NominalGdp` instead. `NominalGdp` already exists at `EconomyState.cs:22` with the same 1e-4 price floor. The two bases agree to within 0.0007 on every measured turn (see F2), so the fallback is continuous. Then correct the comment at `:72-73`. The alternative, seeding `Consumption` in `WorldFactory`, would move the seed and the baseline; the fallback does not.

### F2 (fix the wording; the weight itself may stand): "household spending" is a label, not a separate quantity in this model
`MacroSystem.cs:21` sets one `BaseConsumptionRate = 0.60f` for every country, and C = priorGDP x 0.60 x the rate factor x confidence + the disposable-income delta. Measured from the dumps (seed 777; seed 424242 agrees):

| turn | real GDP DE/FR/IT | nominal GDP (GDP x P) | C x P (new) | C/GDP DE/FR/IT |
|---|---|---|---|---|
| 1 | .4608/.3177/.2215 | .4605/.3182/.2214 | .4601/.3184/.2215 | .599/.600/.600 |
| 25 | .5034/.3135/.1832 | .5105/.3112/.1783 | .5107/.3113/.1780 | .594/.594/.593 |
| 100 | .6188/.2763/.1050 | .6264/.2701/.1035 | .6262/.2708/.1030 | .593/.594/.590 |

- The "HICP country weight" is nominal GDP x 0.6 to within 0.0007 per member. It does NOT reproduce Eurostat's weights. At the seed the model reads DE .460 / IT .222, while Eurostat's 2023 COWEA reads DE .434 / FR .307 / IT .260. Italy is under-weighted by about 3.8 pp and Germany over-weighted by about 2.6 pp, because the real Italian HFMCE-to-GDP share is higher than the German one. The model has no per-country consumption share, and no split between households, NPISH and imputed rent (all excluded or not separable in HFMCE).
- **Question (1): is C x P the right reading, or would nominal GDP be closer to the ruling?** Conceptually, C x P is the closer reading of the cited sentence ("HFMCE expressed in euros"). The ruling's own words, "nominal shares", are met by either basis, and in this model the two are the same number. Nominal GDP would be the more honest name for what the code computes.
- **The overclaim.** The UI sentence (`GameController.cs:3416`) ends "- the weights Eurostat builds the euro area's inflation index from". The comment at `EurozoneRateSystem.cs:10-13` says the weight is the member's "HICP COUNTRY WEIGHT". Both claim more than the model does.
- **Fix (minimum).** Say "the basis" rather than "the weights". Suggested wording: "weighted by its share of the three countries' household spending at current prices - the basis Eurostat weights the euro area's inflation index on". In the code comment, name the deviation: "the model's C is a uniform 0.60 share of GDP, so this reads as nominal GDP; Eurostat's published 2023 shares are DE .434 / FR .307 / IT .260".
- **Fix (optional; a ruling, not a review fix).** Scale each member's C x P by a sourced HFMCE/GDP share, or by the COWEA seed weight over the model's seed weight, if the published weights themselves are wanted.

### F3 (fix): stale documentation and records
- **`docs/reference/MODEL_REFERENCE.md:2767-2768`.** The paragraph under the new ⚠ still says "a simplified version of the real ECB's 'capital key' concept ... recomputed fresh every turn since GDP changes". "Capital key" was already wrong for a rate blend, and the ECB targets the HICP. The ⚠ says the paragraph was written for real GDP, but that sentence contradicts the current reading.
- **`MODEL_REFERENCE.md:2805`.** It still says "the explanatory text was rewritten to describe the GDP-weighted Taylor-Rule blend". It is historical, but has no ⚠.
- **`POLISIM_FEATURE_LIST.md:35`.** It still reads "FT-16 ... OPEN ... Elias's to rule". Close it in the same commit.
- **`COMPLETED.md` §585.** The code cites §585, but the entry does not exist yet (grep finds only §584's "FT-16 filed"). Write it in the commit.
- Question (3) is otherwise clean. No second computation of the zone weight exists. `GetBlendedSuggestedRate` has three readers: `EurozoneRateSystem.cs:111` (boundary), `SimulationManager.cs:3159` (preview) and `GameController.cs:3422` (UI). `TaylorRule.cs:28` and `MODEL_REFERENCE.md:45/96` name the blend without stating a weight. `docs/archive/CLAUDE_LESSONS_2026-08.md:8615` ("ECB blend at GDP weight") is archive and is left alone.

### F4 (pass): on-screen fit
- **Row caption.** "weighted by household spending" is 30 characters, down from 37 for "GDP-weighted across the three members". `LedgerRow`'s trailing column is sized to its content (`LedgerRow.TrailingNeed`, `LedgerRow.cs:338-340`), so a shorter string fits wherever the old one did.
- **Paragraph.** It is about 40 characters longer. It is drawn with `_labelStyle` (`wordWrap = true`, `GameController.cs:2246`), so it wraps rather than clips. The only cost is one more line on the central-bank page.
- **Film.** The page is IMGUI (`GUILayout.Label`), so the `dry585` dry-film scope is the right class under the Canvas film rule. The wording change asked for in F2 would need the same dry film.

### F5 (pass): the diff has one cause
- Every country first diverges at **turn 1**. For Germany, France and Italy the first field is `Zone.InterestRate` (seed 777: 2.7153995 -> 2.71554542). For the USA, Sweden and Poland it is `CurrencyStrength` (USA 100.440666 -> 100.440605).
- `CurrencySystem.ApplyCurrencyStrength` (`CurrencySystem.cs:115-137`) pulls each independent currency toward its rate differential against the average of its trade partners' zone rates. Since P6-D1 every pair of countries is linked, and the euro members are partners. The euro rate therefore enters the USA's, Sweden's and Poland's currency on the same boundary, then their trade balance and the rest.
- The USA's own `Zone.InterestRate` and Taylor rows do not move at t100 (4.1241, 3.6386). Only trade-borne fields move, by 1e-5 relative. That fits a single euro-rate cause.
- Totals: seed 777 moved 281 of 462 field-readings. The `traj_diff` for 424242 prints 286 of 462 (Zone rate 4.2521 -> 4.2525). The note in the brief quoted only seed 777's count.

### F6 (pass, noted): numerics
- The 1e-4 price floor copies `NominalGdp`'s own floor. `PriceLevel` compounds multiplicatively from 1 and cannot reach zero, so the floor is inert.
- Magnitudes stay small. At t1000 (`traj_p6pap2`) Germany is C 4.6e5 x P 4.96e8, about 2.3e14. The weighted sum is about 1e15, far from float overflow, with about 1e-7 relative precision. That is ample for a weight, and GDP-based code had the same shape.
- An infinite `PriceLevel` would make the weight NaN (inf/inf), but that fails the same way through `NominalGdp` everywhere else.
- Summation order: the caller goes first, which is Germany at the boundary and the player's clone in the preview. It differs only in the last bit between boundary and preview, and did before this change too.
- Worth knowing, but not this change's defect: on the millennium path Italy's weight falls to about 0.002 (C 820 vs Germany's 460,722). That is a GDP-path matter.

## Summary of required changes
1. **F1.** Nominal-GDP fallback for the whole blend when any member's `Consumption <= 0` (`EurozoneRateSystem.cs:50-75`), and correct the "as it always has" comment. Cheap bar only. The baseline does not move, because the harness runs days before every boundary.
2. **F2.** Reword `GameController.cs:3416` and `EurozoneRateSystem.cs:10-13/71-73` from "the weights" to "the basis", and state the uniform-0.60 deviation with Eurostat's 2023 shares.
3. **F3.** Correct `MODEL_REFERENCE.md:2767, 2805`, close `POLISIM_FEATURE_LIST.md:35`, and write `COMPLETED.md` §585.


---

## What was done with each finding (the author)

- **F1 (day 0: every member's consumption is 0, the blend fell to one member's own reading): FIXED.** One basis per blend - until every member's consumption is written, the zone blends on nominal GDP. The family re-dumped with the fix (`traj_ft16b`) is byte-identical to `traj_ft16` at both seeds: the harness always runs a day before a boundary, as the reader said, so the digests `d11ce4c2…` and `19324156…` stand.
- **F2 ("household spending" is a label: the model's consumption is a flat share of output, so C × P reads as nominal GDP within a thousandth, and it does not reproduce Eurostat's published weights): TAKEN AS STATED.** The ruling's "nominal shares" is met either way; the weight is kept on consumption at current prices because that is the cited basis and it becomes the right quantity the day consumption shares differ by country. The on-screen sentence now says "the basis Eurostat weights the euro area's inflation index on"; the code comment states the flat share and that the published weights are not reproduced. Sourcing the members' consumption shares is filed as its own ruling (FT-16b).
- **F3 (stale records): FIXED** - `MODEL_REFERENCE.md`'s two lines carry the change, the feature-list row is closed and moved to `COMPLETED.md` §585, and §585 is written in the same commit.
- **The count:** seed 424242 moves 286 of 462 field-readings, seed 777 281 - both stated in the record.
