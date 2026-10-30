# pbr review: the pension benefit ratio in every book, K318 and K319 (`git diff 030d8d17..03a80d6c`, branch `wave/pbr`)

**Files reviewed:** `.gitattributes`, `Assets/Editor/CheckSuite.cs`, `Assets/Editor/DebtPathHoldDiagnostic.cs`, `Assets/Editor/Generated/PensionBenefitRatioAnchors.cs`, `Assets/Editor/Generated/PensionBenefitRatioAnchors.cs.meta`, `Assets/Editor/GeneratedCatalogCheck.cs`, `Assets/Editor/LineIndexationDiagnostic.cs`, `Assets/Editor/NoPolicyCentury.cs`, `Assets/Editor/PensionBenefitRatioDiagnostic.cs`, `Assets/Editor/PensionBenefitRatioDiagnostic.cs.meta`, `Assets/Editor/SpendingIndexationDiagnostic.cs`, `Assets/Scripts/Data/Country.cs`, `Assets/Scripts/Data/Generated/PensionBenefitRatioPath.cs`, `Assets/Scripts/Data/Generated/PensionBenefitRatioPath.cs.meta`, `Assets/Scripts/Data/PensionBenefitRatio.cs`, `Assets/Scripts/Data/PensionBenefitRatio.cs.meta`, `Assets/Scripts/Data/PensionPayment.cs`, `Assets/Scripts/Data/SpendingDrivers.cs`, `Assets/Scripts/Data/SpendingLine.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `ElectionsData/usa/raw/pbr_social_security/SHA256SUMS.txt`, `ElectionsData/usa/raw/pbr_social_security/fetch_log.md`, `ElectionsData/usa/raw/pbr_social_security/wayback_20260919143154_cbo_62556-2026-Additional-Info.xlsx`, `ElectionsData/usa/raw/pbr_social_security/wayback_20260919143154_cbo_62556-2026-Additional-Info.xlsx.headers.txt`, `ElectionsData/usa/raw/pbr_social_security/wayback_20260921152701_cbo_62556-2026-Long-Term-Social-Security-Projections.xlsx`, `ElectionsData/usa/raw/pbr_social_security/wayback_20260921152701_cbo_62556-2026-Long-Term-Social-Security-Projections.xlsx.headers.txt`, `Reviews/2026-10-30_pbr_prereg.md`, `Tools/headless_hold/FrfPlay.cs`, `Tools/headless_hold/PlayerBook.cs`, `Tools/pbr_prep.pl`, `docs/reference/MODEL_REFERENCE.md`

Independent adversarial review, 2026-10-30. HEAD 03a80d6c, base main 030d8d17, ten commits. Read-only on the repo except this file. No Unity run. Everything ran out of the engine (.NET, not Mono) on scratch copies made with `git archive` of each commit. I did not use the implementer's worktree or trust its outputs. I reproduced them, and where I ran the same command they match byte for byte.

## What I ran

1. `bash Tools/cgate/gate.sh <pbr copy>`: GATE OK.
2. `perl Tools/pbr_prep.pl --check` in the pbr copy: both generated files "is what this tool writes", exit 0. All five sources matched their folders' SHA256SUMS.txt (the script dies otherwise). 45 IP279 cross-checks held to the printed tenth. By hand: `sha256sum` of the two CBO workbooks and their header files equals `SHA256SUMS.txt`; each file's size equals both `Content-Length` and `x-archive-orig-content-length` (109264 and 36896).
3. A hand check of source cells with my own xlsx reader (not the generator's code), on unzipped copies of the stored workbooks.
4. Neutrality, (A0) 4, PBR_PROBE=1 on pbr against main:
   - `frf_play.sh <root> on none 100 777`: byte-identical (601 lines).
   - `player_book.sh <root> unchanged` (six players, seed 777): identical on all 19 columns main prints (1465 lines). `benefit_index` reads 1 on every row.
   - My own full-precision dump: every country, every year, in R format. It covers GDP, nominal GDP, the debt ratio, the price level, the real wage, unemployment, approval, potential GDP, inflation, and each spending line's amount, seed amount and driver reference. I ran six worlds of 100 years: all-AI 777; play Italy 777; play Germany 424242; unchanged Poland 424242; unchanged Sweden 777; nofrf USA 777. All six are byte-identical, main against probe.
   - Teeth: the same dump on pbr without the probe differs from main.
5. `diag.sh <pbr> PensionBenefitRatioDiagnostic`: PASS, with 2 registered lines. The figures match amendment 3.
6. `hold.sh <pbr>`: PASS, with 2 registered lines (Sweden under K319; Germany as before).
7. Mutation tests. Each scratch tree carries only Assets/Scripts, Assets/Editor and Tools. An unmutated control tree printed the hold byte-identical to the full tree. Results are under hunt item 4.
8. Save/load, my own probe. It saves Italy's world at year 11, day 100 (mid-turn), through `SaveGameService.CreateSaveGame`, `Serialize`, `Deserialize` and `RestoreInto`, into a fresh SimulationManager, then runs to year 30. Years 11-30 at full precision are identical to the continuous run. `PensionBenefitRatioYear` reads 2032 for all six countries both before the save and after the load, and the JSON carries the field. The same probe on main is also identical, which shows the round trip itself is deterministic.
9. `SpendingIndexationDiagnostic` PASS. `LineIndexationDiagnostic` PASS.
10. `player_book.sh <pbr>` in modes `nofrf`, `unchanged` and `play`, six players, seeds 777 and 424242. All three CSVs are byte-identical to the implementer's run2.
11. `LongRunAnchorsDiagnostic` PASS (K298's three lines registered). `ParticipationCohortDiagnostic` PASS (K313's Poland 2050 registered, +3.03 %).
12. `Tools/bar_tier.ps1`'s own rule over the full 32-path diff list (a scratch copy of the script that reads the list instead of `git diff-tree`).

## Hunt answers

### 1. Sources: match, reproducible, byte-exact

- The generator reads sheet `cross-country_tables`. Through the workbook's rels this is `xl/worksheets/sheet4.xml` (rId4), as the prereg says.
- Cells I checked against `PensionBenefitRatioPath.cs` (element i is 2022 + i for the EU five, 2026 + i for the USA). Table II.1.79 sits at rows 2732-2764: Sweden 2023 39.073996 (file 39.074), Sweden 2026 37.643113 (37.64311), Sweden 2070 30.430403 (30.4304); Germany 2030 41.611936 (41.61194), 2050 35.998024 (35.99802), 2070 35.000971 (35.00097); France 2026 47.091205 (47.0912); Italy 2023 70.933034 (70.93303), 2050 56.598932 (56.59893); Poland 2026 46.391746 (46.39175), 2070 24.708659 (24.70866). All match to float precision.
- Anchors file: Table II.1.64 Italy 2022 15.566584 / 2070 13.675122 and Sweden 2022 7.391906 match `PensionBenefitRatioAnchors.cs`.
- The USA's derived ratio, by hand from CBO's cells:
  - 2030: outlays 5.609 % (Table 3, row 54) x GDP 37.743 tn (Table 2, row 54) / 77,600 k beneficiaries = $27,281.0. Covered earnings 15.983 tn / 188,800 k workers = $84,655.7. B = 32.22581. The file reads 32.22581.
  - 2070: 6.378 % x 156.705 tn / 106,600 k = $93,758.4, against 65.524 tn / 194,800 k = $336,365.5. B = 27.87396. The file reads 27.87396.
- Minor text slip: the prereg's Part 1 and `fetch_log.md` give the USA's 2050 ratio as 29.24. The file reads 29.23467 (29.23). The code is right; only the prose rounds wrong (L7).

### 2. The rule in SimulationManager: correct, applied once per boundary

- `SimulationManager.cs:8515-8516` computes `PensionBenefitRatio.Ratio(id, PensionBenefitRatioYear, CalendarYear)` once, before the line loop. It then sets the stored year to `CalendarYear`. `:8531` multiplies the pension line's real term (and only the pension line's) by it. `:8532` forms the factor. `:8533` passes that same real term to the MF-14 guarantee.
- So B(now)/B(last) is applied exactly once per index. A second call in the same boundary would read B(y)/B(y) = 1. It cannot double-apply.
- `IndexSpendingLines` has two callers: the turn (`:6941` via `ResolveSpendingForTurn`) and the preview clone (`:7374`, its own object).
- **MF-14.** Germany's floor is prices x wages x B >= 1 (`:8559`). The USA's floor is on prices only, with B still applied (`:8558`). Both are as the prereg's Part 2 (iii) worded them.
- **NEXT YEAR.** `ProjectedRealTerm` (`:8584-8587`) multiplies by `ProjectedRatio` = B(stored year + 1) / B(stored year) (`PensionBenefitRatio.cs:61`). After a boundary at year y, the next boundary indexes into y + 1. Before the first boundary, the stored year is the epoch's year. Consistent.
- **Preview clone.** It copies the field (`:7890`) beside `CalendarYear` (`:7882`).
- **First boundary.** `Country.cs:323` captures `EpochDate.Year` at `CaptureStructuralBases`. `GameController.OpenWorldAt` calls `SetEpoch` before `WorldFactory.CreateDefault`, and so does every headless harness through `ApplyStart`. `CommitCalendarYear` runs at the top of `AdvanceDay`, before the boundary. The first boundary therefore reads B(y0 + 1)/B(y0). `SpendingIndexationDiagnostic` asserts exactly this against the table. The diagnostic's B index at Sweden 2030 is 0.9679 = 36.43598 / 37.64311.
- **Skipped or repeated year.** The stored value is a year, not B. Each boundary multiplies by B(y_k)/B(y_{k-1}), and the product telescopes to B(y_n)/B(y0). A repeated calendar year gives 1 that boundary, and the next one catches up. A skipped year is carried in one step. No country's start lies near New Year, so with 365-day turns the drift (about 24 days a century) never repeats or skips a year inside 100 turns.
- **Change of government.** The B term reads no role flag (`aiBudget` is not consulted). The year advances in every book, so a change of government cannot freeze or jump it. The full-precision runs went through AI elections and changes of government with no difference.
- **Save/load.** The field is a public int that Newtonsoft serializes by default (`BuildSettings` has no contract resolver). My mid-turn round trip is identical to the continuous run. The shadow baseline and the impact ledger fork through the same save round trip (`ShadowBaseline.cs:158-165`), so they carry it too.
- **Never applies?** Only where the stored year is 0 (a pre-71 save, which is refused) or the probe is set. The probe is set only by `FrfPlay.cs` and `PlayerBook.cs`, never by the game or a check.
- **Pinned line.** Its seed amount carries B and its reference advances, as the prereg says.

### 3. Neutrality (A0): byte-identical, reproduced

See "What I ran" item 4. This is stronger than the prereg asked: six worlds at full precision, both seeds, all four books. The harness has teeth: without the probe the dump differs.

### 4. K318 and K319: coded as worded, and both have teeth

**K318** (`PensionBenefitRatioDiagnostic.cs:102-121`):
- (i) is `a2 >= -0.05 && a2 <= 0.075`. (ii) is |ln coverage| > |ln rest|; the benefit ratio's ln is 0. This is amendment 3 word for word.
- The key set holds only "Italy 2070" (`:42`). Any other (A1) miss whose (A2) misses still STOPs (`:123-125`). A missing year FAILs (`:82`). An (A1) miss with no (A2) STOPs, as amendment 2 says.
- Measured (seed 777): (A1) +30.2 %, (A2) +6.0 %, coverage x1.2288, rest x1.0597. Seed 424242, from my player_book: (A1) +30.8 %, (A2) +6.5 %, rest x1.0647.
- Teeth, mutation M1: ceiling 0.075 to 0.055 gives `FAIL - registered under K318 but its bound broke: (i) ... BROKEN`.
- Teeth, mutation M2: ceiling opened to 1.0, and Italy's source coverage at 2070 planted at 100.0. Coverage then reads x1.0578 against a rest of x1.2310, giving `(ii) ... BROKEN` and FAIL.

**K319** (`DebtPathHoldDiagnostic.cs:81, 89, 122, 133-167, 294`):
- (i) the outside term is (model -pb less the pension line's own part) less (DSM -pb less its cost of ageing). The other terms are the pension line against the cost of ageing, interest, growth, inflation, and the rest against the stock-flow. `largest` is a strict |x| > max |others|.
- (ii) the excess beyond the edge must be <= 2.5.
- A broken bound becomes `Check(false, ...)` and FAILs (`:122`). This is as amendment 3 words it.
- Measured: outside -6.6 (-8.5 less -1.5, against -1.1 less -0.7). Next largest: the pension line -0.9; interest +0.9, growth -0.5, inflation -0.1, rest 0.0. The excess is 1.5 points (unrounded 1.55; amendment 3 says 1.6, L7).
- Teeth, mutation M3: excess bound 2.5 to 1.0 gives `(ii) 1.5 ... at most 1.0 - BROKEN` and FAIL.
- Teeth, mutation M4: bound opened to 100, and Sweden's B halved from 2027 in the runtime table. The pension line's part then reads -31.6 against an outside term of -2.2, giving `(i) ... BROKEN` and FAIL.

**Is K319's arithmetic right, and does the outside term really dominate?** The arithmetic is right. I recomputed the model's Swedish pension part from the player_book: -1.54 over 2027-2035. The Ageing Report's own Swedish pension expenditure, rebased on 2026, gives -1.46 over the same years. So the pension line now tracks its source, and the outside term dominates by a factor of seven.

The comparison has two asymmetries (L1):
- The DSM's cost of ageing is pensions plus health plus long-term care, measured against 2025 (its 2022-2025 entries are 0). The model's part is the pension line only, measured against its 2026 opening.
- Rebasing the DSM's cost of ageing on 2026 changes the two terms to outside -5.1 and pension -2.3. Bound (i) still holds, so the verdict is robust. The printed split is base-dependent.

**Is registering a newly missing band honest?**
- fisc2's prereg rule was: "no band is widened and no band is newly registered", and "F4 lands only if no band asserted today misses after it. If one does, F4 is withdrawn." Under that rule, pbr would be withdrawn.
- pbr's own prereg chose a different path. A newly missing asserted band "stops the item" and holds it "for a ruling". The ruling (K319) was dated after the measurement, marked "not preregistered", and committed alone (a13e24b7) before its code (03a80d6c).
- The miss was predicted before measurement (-13.6 predicted, -13.0 measured). The decomposition supports the re-attribution.
- Before pbr, the band held by only 0.2 points. It held because the unsourced pension drift (+13 to +35 % over the Ageing Report) masked the primary-balance gap. Withdrawing pbr to restore that offsetting error would be worse than registering the band.

So I judge the registration defensible. It is not what fisc2's standing allowed, and it is not the preregistered outcome. The record must say so in plain words (M2):
- it reverses fisc2's lift of the same band (K295);
- it turns a band pbr's prereg Part 3 asserted into a registered one;
- both bounds were chosen with the measurement in hand: K319 allows 2.5 against 1.55 measured; K318 allows 7.5 % against 6.0 / 6.5 %;
- K318's seed-424242 line is checked out of the engine only.

Amendment 3's "No band is widened" is literally true, but alone it reads as "nothing was relaxed", which is not the case.

### 5. Save format 71: correct

- `SaveGameService.cs:119` steps to 71. The v71 note is prepended. The full v70 note and every earlier note follow it verbatim: I compared the line's suffix with main's line 119, and the notes at 120-127 are unchanged.
- A v70 save is refused (`:240`, `version != CurrentSaveVersion`). The list view shows "format 70 - this build reads 71" (`:431-433`).
- If such a save ever loaded, the absent field would read 0, and `Ratio` returns 1 once (`PensionBenefitRatio.cs:54`). The refusal is right.
- The note misdescribes the consequence, though (L2). A v70 game never carried B, so it would miss B's whole move from the epoch to the save, not one year's move.

### 6. Prereg promises against the build

Kept:
- every code site Part 2 named;
- the generator's discipline;
- the `.gitattributes` lines;
- (A0) 1-4;
- (A1)/(A2) and the disposition, with amendment 2's readings;
- the preview clone copy;
- NoPolicyCentury's read-only fields;
- the save format and the CRLF line endings;
- the FRF doc updated with (B)'s measurement (`SimulationManager.cs:708-713`).

I reproduced (B) and every figure the doc states:
- Sweden nofrf: -142.4 % at year 100 on 777, below -100 % from year 67; -109.4 % on 424242. (B3) +3.62 / +3.65.
- Italy peaks at 255.5 / 240.2 %.
- Poland ends at -14.8 / -16.0 %. Its seed 777 misses (B3) by 0.05 (+3.05).
- B1 holds everywhere.

So (B) fails, the reaction stays, and no code moved for (B). This is mechanical, as the prereg says.

Different or missing:
- (a) Part 3 promised "the largest move of any other line's share of GDP at years 10, 30, 50 and 100 in `frf_play on none`" (reported, not banded). The committed `FrfPlay.cs` prints no line columns, so it cannot produce that figure. Amendment 2's full-precision state hash also lives only in scratch. Both belong in the record with the tool that made them (L9).
- (b) (A0) 2 does not assert the USA's pension line with B, because `SpendingIndexationDiagnostic` pins the USA's SocialSecurity line in its test. The USA's B is covered by `LineIndexationDiagnostic` (index 0.9317 at 20 years, well past its 2 % tolerance) and by the (A1) check (L8).
- (c) The `DebtPathHoldDiagnostic` table gained a registration where Part 2 expected one only "if a registration lifts". That is amendment 3's ruling, and it is disclosed.
- (d) The rulings K314, K315, K318 and K319 are cited in code and in `MODEL_REFERENCE.md` but are absent from `docs/RULINGS_2026-10-23.md` on the branch and on main (M1).
- Expected and owed by the integrator (amendment 1.2): the TrajectorySentinel family re-pin to `pbr`, and the FT-10 ceilings. The branch fails the sentinel until then.

### 7. Money paths

By bar_tier's rule (name matches `$money` and the path is a simulation path), ten files. The first 16 hex of SHA-256 of the final state with carriage returns removed (`perl -0777 -pe 's/\r//g' <file> | sha256sum`). The value is the same from the 03a80d6c blob, the implementer's worktree, and my scratch copy:

| file | digest |
|---|---|
| `Assets/Editor/DebtPathHoldDiagnostic.cs` | `8efeaa904e882d79` |
| `Assets/Editor/Generated/PensionBenefitRatioAnchors.cs` | `d17e63c71c4f677c` |
| `Assets/Editor/PensionBenefitRatioDiagnostic.cs` | `e4b03036e5072003` |
| `Assets/Editor/SpendingIndexationDiagnostic.cs` | `b93543b2ddb035c4` |
| `Assets/Scripts/Data/Generated/PensionBenefitRatioPath.cs` | `51d578220dfdc186` |
| `Assets/Scripts/Data/PensionBenefitRatio.cs` | `c7720e07c780ec7a` |
| `Assets/Scripts/Data/PensionPayment.cs` | `c5757baad711410e` |
| `Assets/Scripts/Data/SpendingDrivers.cs` | `4f0101aebd4c3fb9` |
| `Assets/Scripts/Data/SpendingLine.cs` | `b02c2440b8a5e094` |
| `Assets/Scripts/Simulation/SimulationManager.cs` | `739d1fcefccf95ef` |

The prefixes agree with the ledger states bar_tier printed (51d57822, c7720e07, c5757baa, 4f0101ae, b02c2440, 739d1fce). The four Editor files sit outside the ledger's rows, and the baseline pass reaches them. `PensionPayment.cs`, `SpendingDrivers.cs` and `SpendingLine.cs` change comments only.

## Findings

### High

None. I found no defect in the rule, the sources, the neutrality, the save path or the two registrations' code.

### Medium

**M1. The rulings the code relies on are not recorded.**
- Where: `PensionBenefitRatioDiagnostic.cs:36-45` (K318), `DebtPathHoldDiagnostic.cs:79-89` (K319: the ruling comment at 79-81, the bound at 89), `PensionBenefitRatio.cs:27` (K315), `SimulationManager.cs:8515` (K314). `docs/RULINGS_2026-10-23.md` ends at K313.
- Scenario: the branch merges. A later reader, or the next item's reviewer, meets "K319" in the hold's output and finds no ruling that states its bounds, its date, or that it was set after measurement. fisc2's review flagged the same gap (R2-L2), and fisc2's rulings were recorded on main before its merge (54c23ca9).
- Fix: record K314, K315, K318 and K319 on main before the merge.

**M2. The record must state plainly what K318 and K319 relax.**
- Where: `Reviews/2026-10-30_pbr_prereg.md` amendment 3 (lines 293-317); `DebtPathHoldDiagnostic.cs:81`.
- What the record must say:
  - K319 re-registers Sweden (h2), a band fisc2 lifted (K295) and pbr's prereg Part 3 asserted.
  - Under fisc2's rule ("no band is newly registered"; F4 withdrawn on a new miss), pbr would not land.
  - Both bounds were set after the measurement, with about one point of headroom: 2.5 against 1.55, and 7.5 % against 6.0 / 6.5 %.
  - K318's second seed is out of the engine only.
- Scenario: the § entry repeats "no band is widened". A reader then believes no assertion was relaxed. They miss that the hold now passes with Sweden 1.5 points outside a band that, a day earlier, was asserted.
- Fix: say it in those words in the ruling text and the § entry. No code change.

### Low

**L1. K319's split compares unlike quantities.**
- Where: `DebtPathHoldDiagnostic.cs:145, 151, 159`.
- The DSM's cost of ageing covers all ageing (health and long-term care included), against 2025. The model's part is the pension line only, against the 2026 opening.
- Scenario: a reader takes "the pension line -0.9" as pbr's overshoot of the source. Rebased on 2026, it reads -2.3, and the outside term reads -5.1. The verdict holds either way.
- Fix: state the base and the perimeter beside the printed bound, or in the record. The name "primary balance before ageing" includes the model's own health ageing.

**L2. The v71 note understates a v70 save's error.**
- Where: `SaveGameService.cs:119`.
- It says the save would "skip the benefit ratio's move over the year the save was cut in". A v70 game never carried B, so it would miss B's whole move since the epoch. For a Polish game cut in 2040, that is about +27 % on the line (B 2023-2040 = 0.784). The refusal is right.
- Fix: correct the wording.

**L3. The policy preview lags B for part of each turn.**
- Where: `SimulationManager.cs:7374, 7882, 7890, 8515`.
- `PreviewTurn` indexes the clone at its own `CalendarYear`. Before New Year inside a turn, the ratio is B(y)/B(y) = 1. Meanwhile `ProjectLineNextYear` already shows B(y + 1)/B(y).
- Scenario: in Poland in autumn, the effects panel's pension line omits up to 2.3 % of the line's move (2037), about 0.2 point of GDP, while the NEXT YEAR figure includes it.
- This is the same idiom as PN-1's statutory driver, and the preview's price term is partial-year too. Note it; no change needed now.

**L4. Some (A1) lines pass with (A2) well outside its band.**
- Where: `PensionBenefitRatioDiagnostic.cs:94`.
- The lines: Poland 2030 / 2040 / 2050 / 2070 at -7.0 / -10.9 / -14.6 / -7.5 %; Sweden 2070 -5.1 %; France 2070 -5.2 %.
- The prereg makes (A2) decide only misses, so these are ok. But the prereg predicted Poland at -8 to -9 %, and Poland's "rest" term (x0.85 at 2050) is the largest residual in the table. The (A1) pass there comes from offsetting errors.
- Fix: print these in the record.

**L5. Dead code.**
- Where: `PensionBenefitRatio.cs:37` (`Has`) and `:65` (`IndexOnOpening`) are never called. The doc of the second says "the diagnostics' printout", but the diagnostics call `Ratio`.

**L6. Germany (h2)'s reason text is stale.**
- Where: `DebtPathHoldDiagnostic.cs:77`.
- It still says "the pension line's ageing runs near the DSM's cost of ageing". After pbr, the model's German pension line adds +2.7 over 2025-2035 (my player_book), against the DSM's whole cost of ageing of +6.8.
- It is unbounded, so nothing fails. The reason should be re-read.

**L7. Small figure slips in the prose.**
- The USA's 2050 ratio is 29.24 in prereg Part 1 and `fetch_log.md`; the file reads 29.23.
- Sweden is "1.6 beyond the edge" in amendment 3; the code prints 1.5 (unrounded 1.55).

**L8. (A0) 2 does not cover the USA's pension line.**
- Where: `SpendingIndexationDiagnostic.cs:118`.
- The USA's pension line is the pinned line in that test, so the year-five B check never touches the USA. It is covered elsewhere (see 6 (b)).

**L9. Two reported figures have no committed instrument.**
- The other lines' largest share move (Part 3) and amendment 2's state hash have no committed tool. The record should carry the figures and name the scratch tool.

## Verdict

**Verdict:** READY on conditions.

The mechanism is right and sourced. It is neutral to the bit with B held flat. It survives save/load, changes of government and the first boundary. K318 and K319 are coded as worded and both fail when broken.

Before the merge:
1. Record K314, K315, K318 and K319 in `docs/RULINGS_2026-10-23.md` on main (M1).
2. The ruling text and the § entry must state, in plain words, that K319 re-registers a band fisc2 lifted and pbr's prereg asserted, against fisc2's "no band is newly registered". They must also state that both bounds were set after measurement with about one point of headroom, and that K318's second seed is checked out of the engine only (M2).
3. The integrator re-pins the trajectory sentinel to `pbr` and applies the FT-10 rule after the Unity run, as amendment 1.2 says.

L1-L9 can follow. The L2 wording and the L4 / L9 figures are cheapest to fix in the same record.
