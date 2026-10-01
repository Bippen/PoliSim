# Review — s716 the Finance partner holds its levers in fact (2026-10-01)

Reviewed: the uncommitted change against HEAD 0a30212. I read:
- `Assets/Scripts/Simulation/FinancePartner.cs` (new, whole);
- the working `Assets/Scripts/Simulation/SimulationManager.cs`:
  - the diff: `IntroduceBudgetBill`'s strip 1294-1310; `PlayerMayIntroduce`, `FinancePartnerOfPlayer` and `FinancePartnerRuns` 2457-2493; `AdvanceTurn`'s per-country loop 4445-4495; the cabinet roll 4725-4755;
  - `ResolveCabinetDecision` 1098-1117 and `TableGovernmentBudget` 1167-1176;
  - `PreviewTurn` 4782-4990 (no partner) and `ApplyTaxRateChanges` 5430-5463;
  - `BuildEffectiveDecisionForDetailedSpending` 6246-6265;
- `Assets/Scripts/UI/GameController.cs` (the diff; `BuildPlayerDecision` 6704-6727; `RecomputePolicyPreview` 5075-5110; `DrawTaxScheduleRows` 12001-12030);
- `Assets/Scripts/Simulation/AiFinanceMinistry.cs` 100-260 (`AsBudgetBill`, `Decide`, the EU and US rules, `RaiseHouseholdRates`);
- `TaxSchedule.cs` 425-440, 536-570, 600-609 and `TaxBases.cs` 135-152 (every `RateSeedOf` reader); `Country.cs` 250-256; `WorldFactory.cs` 1140-1150; `TaxLine.cs` 134-188;
- `MacroSystem.cs` 2144, 2279, 2356 (the tax-hike approval term) and `CabinetSystem.cs` 170-215 (the Finance decisions);
- `Assets/Editor/FinancePartnerDiagnostic.cs` (new, whole), and the diffs of `CheckSuite.cs`, `TrajectorySentinelCheck.cs` and `ElectionsData/portfolios/portfolio_salience.md`;
- `PreviewParityDiagnostic.cs` (the ministry switch at 210) and `Tools/bar_tier.ps1:37` (`$money`);
- the dumps `traj_fp716_s{777,424242}_t100.csv` against `traj_ps3k_*`, compared row by row with a scratch perl script.

One independent reading, read-only; Unity not run. **Verdict NOT READY: four small fixes and one process fix, and none of them moves the fp716 family**, so the baseline rows can cite fp716 as measured.

## Defects

1. **The ministry's "first claim" fails in the player's own country under an AI head.** In every AI country the ministry writes into the boundary's decision, and the partner skips a rate the ministry wrote (FinancePartner.cs:108, after AdvanceTurn's `AiFinanceMinistry.Apply`). In the player's country the AI government is `ministryByBill`:
   - **The ministry's side:** its rule goes to the chamber as the government's budget bill (`TableGovernmentBudget`, `AiFinanceMinistry.Decide` → `AsBudgetBill`, both household rates included) and applies on the adoption day.
   - **The partner's side:** the boundary decision is `PolicyDecision.None()` (GameController.cs:6533), so the partner steps on both rates every year, whatever the government's bill set.
   - **Failing scenario:** a player in opposition in Germany 2026 (Merz, the SPD at Finance). In a deficit year the government's bill raises VAT, and at the boundary the SPD steps VAT again. In the dump's Germany, the same year leaves VAT to the ministry.
   - **Fix:** in `TableGovernmentBudget`, run `FinancePartner.Apply` into the ministry's decision before `AsBudgetBill`, so the government's bill carries the partner's step where the ministry wrote none, and skip the boundary step where `ministryByBill`. This keeps reading (3) everywhere, and puts the partner's step to the chamber the player sits in.
   - **Scope:** player path only; the dump is untouched.
2. **VAT's anchor is the rate when a partner first appears, not the seed.** `Target` anchors on `TaxSchedule.RateSeedOf(line)`. The income tax's seed is captured at the seed (Country.cs:256, WorldFactory.cs:1147). VAT's is not, so `RateSeedOf` sets it to VAT's current rate on the first read (TaxSchedule.cs:544), and the partner rule is VAT's only reader.
   - **Where it bites:** where a partner exists from the start (every partner in the dump, and Germany's FDP at the 2024 start), the first read is the seed. Where a partner first appears after a government change in play (an election, a constructive vote), VAT anchors on the rate after years of budgets, while the income tax anchors on the seed. That contradicts the doc's "the rate the line was seeded at".
   - **Fix:** capture both household rates' `RateSeed` at the seed. It is inert elsewhere, since revenue reads `RateSeedOf` only for the income tax (TaxBases.cs:149) and the UI only in the income-tax schedule rows (GameController.cs:12027-12029). Keep the first-read fallback for old saves.
3. **`Choose` reads backwards on a spending decision.** It takes `BudgetImpact` as revenue raised (a party for services takes the highest, a party for lower taxes the lowest). Several Finance options are spending.
   - **Failing scenario:** "Legacy System Failure Risk" (CabinetSystem.cs: "Fund the maintenance" −80, "Push it another year" 0). The services party (`spendvtax` below 5) postpones the maintenance, and the lower-taxes party funds it.
   - **Pure revenue choices read right:** the pilot, the loophole package and the sweep.
   - **"Audit Staffing Shortfall" is right only by luck:** its −150 is lost collections, not spending.
   - **Fix:** tag each Finance option by what it does (raises or forgoes revenue; spends or saves), since one signed number mixes the two. This is [AUTHORED-DRAFT], on the calibration row.
   - **Scope:** cabinet decisions roll only for the player's country, so this is the player path only.
4. **The player's preview leaves out the partner's step.** `PreviewTurn` runs neither the ministry nor the partner, so a governing player with an AI Finance partner sees a next year without the two rate moves and without their approval cost (−0.75 in Germany's and France's first year).
   - **Why it matters:** §694's rule is that the preview carries a rate lever where the turn will, and here the turn will move it at the boundary with no bill to show it.
   - **A second gap:** the budget draft's preview (`PreviewTurnWithBudgetDraft`) still counts a household-rate draft left from before the partner arrived, which `IntroduceBudgetBill` then strips.
   - **Fix:** apply `FinancePartner.Apply` and `Withdraw` on the preview's decision; it is deterministic and draws no random number. Leave the household rates out of `BuildBudgetBillFromDrafts` under a partner.
   - **Unaffected:** `PreviewParityDiagnostic` turns the ministry off, which turns the partner off, so the parity bar is unaffected.
5. **Process: the new file is not a money path.** `FinancePartner.cs` writes household tax rates into the turn's decision, but `$money` (`Tools/bar_tier.ps1:37`) does not match it. `AiFinanceMinistry` and `SimulationManager` do. The same gap §546 closed for `EnergyFleet`. **Fix:** add `FinancePartner` to `$money`, with its ledger row from this review.

## Decisions owed (Elias)

- **A. France moves on a cabinet that is not of record.** France's start government is the formation's PROVISIONAL stand-in (the record names no French cabinet's parties, GAP G4). It seats the RN at Finance under ENS, which no French government of record has done.
  - **The effect:** the rule moves France's whole family: turn-1 approval −0.75, and at turn 100 GDP −0.22 / −0.26 % (seeds 777 / 424242).
  - **My recommendation:** `Holder` returns null where `Government.Provisional`. A lever "run from its own positions" by a party in no government of record is not the record's. At the least, the family note should state it as a finding.
- **B. Poland's partner is the model's, not the record's.** The salience allocation gives TD Finance under Tusk, while the record's Finance minister (Andrzej Domański) is KO's (`poland/records_by_date.md:231`, the KO club). TD has no CHES position, so nothing moves today. If TD gains one, Poland would move by a portfolio the record never gave TD. Germany's SPD and Italy's Lega match the record.
- **C. Levers the partner holds only by refusal.** Under a player head with an AI partner:
  - **Refused to the player:** the tax programme (every tax's implement and remove), the fiscal, monetary and electricity-tax laws, and the Finance chair's appointment and reshuffle.
  - **Run by no rule:** none of these is run for the partner, so they are frozen in the player's country. The partner runs only the two household rates and the Finance cabinet decision "from its own positions".
  - **Ask:** say whether that is the reading of "in fact", or whether the frozen levers are owed rules. An empty Finance chair also stays empty, which is neutral (efficiency 1).
- **D. The partner's claim is per rate, not per fiscal stance.** The treaty rule mostly consolidates through spending lines (only a shortfall reaches the rates, `RaiseHouseholdRates`), so in a consolidation year done through spending the partner still moves either rate. Today's partners mostly raise rates (Germany, France). A cutting partner works against the rule's year: Italy's Lega asks −0.16, small. State it.

**Notes:**
- **The "first claim" ordering is correct where it applies.** In AI countries the ministry runs before the partner, and a rate the ministry wrote is skipped. A caller's own override (a harness, the film driver's `+8`) wins likewise. `BuildPlayerDecision` carries only the interest-rate change, so the player's boundary decision never holds a household rate. `BuildEffectiveDecisionForDetailedSpending` shares the dictionary by reference, but only within `ApplyDomesticPolicy`, so the withdrawal reaches it.
- **The withdrawal is complete.** `Withdraw` removes exactly the types `Apply` wrote, and the early return on a missing position returns what was written. It has no try/finally, the same as the ministry's: an exception in `ApplyDomesticPolicy` aborts the turn, so it is not a new exposure.
- **The cabinet roll is unchanged in its random draws.** `TryRollDecisions` runs exactly as before; the gate is read after it, and `Choose` draws nothing. The partner's resolution lands after `ApprovalLedgerRecorder.CloseAtBoundary`, so it counts in the next period's ledger, as a player's later answer does. The ledgers record it (`Cabinet: …`), and `RemoveAll` on pending is a no-op.
- **The strip touches only what it should.** It fires only under `FinancePartnerOfPlayer`, which needs `PlayerGoverns`. `TableGovernmentBudget` does not go through `IntroduceBudgetBill` (it sets the pending bill directly), so the AI government's bill in the player's country is never stripped. A shadow budget is not stripped either (the chamber's alternative), which is right.
- **Edge cases hold:**
  - **The joint group:** the CSU at Finance under a CDU head is excluded (`SameGroup`), and so is the reverse.
  - **A caretaker head** keeps its record and portfolios, so the partner keeps running, consistent with "the caretaker governs meanwhile".
  - **A seatless head** is fine, since positions do not need seats.
  - **No head party, a presidency (the USA), or a government with no portfolios** gives null.
  - **A missing position** (Poland's TD) moves nothing.
- **The diagnostic covers the rule's mechanics.** It asserts the holder, the target, the step, the withdrawal, the gate, the strip, both turns and `Choose` on a revenue decision. Its turn with no player passes only if the ministry wrote no household rate that year; it was right in n716c, but the check would read a ministry year as a failure.

## The trajectory family (fp716)

- **The digests reproduce.** By the stated method (header plus turns 1-20, as raw bytes), 777 gives `d7a8dbee…6984` and 424242 gives `b4efe1e8…5f30`. The same method gives ps3k's `438e1148…9446` and `fc163628…e905`, so the sentinel's new pins are right.
- **The first divergence is turn 1, on approval.** Germany 49.33 → 48.58 and France 46.73 → 45.98 on seed 777 (46.33 → 45.58 and 49.73 → 48.98 on 424242). Nothing else moves at turn 1.
  - **It is the existing term:** `TaxHikeApprovalSensitivity` (1.5, MacroSystem.cs:2144) × the turn's total hike of 0.5 points (two +0.25 steps, counted by `ApplyTaxRateChanges` because they are rises) = 0.75. Nothing new.
  - **Italy:** its partner cuts, cuts carry no hike penalty, so Italy first moves at turn 2 (approval +0.006, by growth).
- **The signs are plausible.**
  - **Germany and France (rises):**
    - GDP at turn 2 is −0.064 / −0.077 %, at turn 10 −0.30 / −0.23 %, and at turn 100 −0.38 to −0.47 %.
    - Debt is lower early (Germany at turn 10, 54.37 → 53.73 on 777).
    - By turn 100 the ratios converge, because the treaty rule gives a surplus back to the lines (`RestoreLinesUniformly`). Germany 777 ends at 33.04 → 33.06, the smaller GDP showing in the denominator.
  - **Italy (a cut):** GDP up (+0.09 / +0.11 %), debt up (38.76 → 38.90).
  - **Inflation and unemployment** move in the second decimal.
  - **Sweden, Poland and the USA** move from turn 2 in late digits through `CurrencyStrength` and trade, as stated.
  - **These match the coordinator's figures exactly** for both seeds.
- **France's movement is a finding (decision A).** It rests on the provisional stand-in's RN at Finance. Say so in the family note, or exclude provisional cabinets and re-dump.
- **Italy moves again at item 8, confirmed.** With the MEF weighed as the sum of its posts (3.94), the Meloni cabinet (FdI 119, Lega 66, FI 45, NM 7) gives FdI Finance (hand-computed in the §706 review's latent note). `Holder` then returns null, Italy's partner rule stops, and its +0.09 / +0.11 % reverts. That is a new family at item 8.
- **The defects above don't touch the dump.** Defects 1, 3 and 4 are player-path only. Defect 2 fires only where a partner appears after the start, and no dump election re-forms an AI government. fp716 stands as the family; only decision A, if Elias excludes provisional cabinets, would re-dump it.

**Money path:** yes: `SimulationManager` by name, and `FinancePartner` in substance (defect 5). The rule writes household tax rates every year in up to four countries, strips two rates from the player's budget, and resolves a Finance cabinet decision's budget impact. The arithmetic of each piece is right (target, step, clamp, withdrawal, approval term). The defects are in where it runs: the player's AI-governed country (1), the VAT anchor (2), the decision's direction (3), and the preview (4).

**Verdict: NOT READY, four small fixes plus defect 5's pattern line.** None changes the fp716 family, so the ledger's baseline rows can cite it now. Decisions A-D go to Elias with the record. A is the one that would change the family if he rules provisional cabinets out.

## Second pass (2026-10-01, the uncommitted tree after the fixes and decision A)

Reviewed: `git diff` against HEAD 0a30212 (nine files) plus the two new files. I re-read:
- `FinancePartner.cs` (whole: `Holder`'s provisional skip at 52, `Apply`'s `claimed` parameter at 105-112, `Choose` 135-144);
- the working `SimulationManager.cs`:
  - `TableGovernmentBudget` 1167-1180 and its three callers (`TryOpenBudgetProcess` 2215-2227, `UiScreenshotDriver.cs:1955-1960`, the diagnostic);
  - `PartnerStepsAtBoundary` 2498-2503 and `AdvanceTurn` 4477;
  - `PreviewTurnOnClone` 4854-5011, with no return or throw between the partner's Apply (≈4898) and its Withdraw (5009), and the two-clone caller at 5090/5094;
- `Country.cs:224-258` (`CaptureStructuralBases`, called only from `WorldFactory.cs:1081` at creation, never on load);
- the diffs of `CabinetDecision.cs`, `CabinetSystem.cs`, `GameController.cs`, `Tools/bar_tier.ps1`, `TrajectorySentinelCheck.cs` and `FinancePartnerDiagnostic.cs` (new check (j));
- the logs `n716d.log` (5 of 5 clean: FinancePartner, PreviewParity, PortfolioAllocation, GovernmentBudgetBill, PlayerRole) and the dumps `traj_fp716b_s{777,424242}_t100.csv`, compared against ps3k and fp716 with the same script.

**Verdict READY.**

### The defects

1. **CLOSED.** In the player's country under an AI head, the partner's step now rides the government's own bill, after the ministry's rule (the decision's existing entries are skipped), and `PartnerStepsAtBoundary` keeps the boundary out.
   - **`TableGovernmentBudget`'s callers:** `TryOpenBudgetProcess` (2227) reaches it only for the player's country when `!PlayerGoverns`; the film driver guards on `!PlayerGoverns` (1955); the diagnostic does so by construction. All three are exactly the case where `PartnerStepsAtBoundary` is false, so no path steps twice.
   - **With the ministry switched off:** `FinancePartnerRuns` is false, so neither the bill nor the boundary steps, which is consistent.
   - **The arrival and fiscal-year windows** table the government's bill once each, so there is at most one partner step a year, and the chamber decides it. A rejected bill means no step that year.
2. **CLOSED.** Both household seeds are captured in `CaptureStructuralBases`, which runs only at world creation, so no load resets them.
   - **Other readers:** VAT's seed has no other reader.
   - **Old saves:** they keep `RateSeedOf`'s first-read fallback.
   - **The dump:** every partner is present at turn 1, so the anchor's value is unchanged.
3. **CLOSED.** `StateLean` is tagged on ten options of the five tradeable Finance decisions, and the windfall is untagged (the first option for every party).
   - **The rule:** `Choose` takes +1 below 5 on `spendvtax` and −1 above; at 5, or with no tagged option on that side, it takes the first. "Fund the maintenance" now goes to the services side.
   - **Old saves:** pending decisions carry `StateLean` 0, but those are the player's own decisions, never the partner's.
   - **Proof:** the diagnostic reads the real pool by reflection.
4. **CLOSED.** The preview applies the partner's boundary step on the real country's lines before `ApplyTaxRateChanges` and withdraws it after `RecordApprovalAttribution`. The effective decision shares the dictionary by reference, so withdrawing any earlier would have dropped the step from the approval term.
   - **The claimed set mirrors the boundary's order.** At the boundary the ministry writes first and the partner skips what it wrote. In the preview, `claimed` is the key set of `AiFinanceMinistry.Decide` (pure, the same last report), taken only where the ministry would run (`!PlayerGoverns`).
   - **The player's own country never uses it:** `PartnerStepsAtBoundary` is false exactly when `!PlayerGoverns` and the ministry is on, so `claimed` is always null there. It matters only for a previewed AI country, where it gives the same skip as the boundary.
   - **`PlayerGoverns` on the real country is safe.** It returns false before the null-government throw for every country but the player's. The player's always holds a government in a factory-built world. `RecomputePolicyPreview` already calls it on the same country.
   - **The stale-draft case:** the budget draft drops a held household rate, and the row shows the standing rate.
   - **Proof:** PreviewParity is clean (n716d), after failing on sim716a with −0.75 against 0.
5. **CLOSED.** `FinancePartner` is in `$money`. `CabinetSystem.cs` and `CabinetDecision.cs` change only tags and are outside the money pattern by name.

**Decision A, taken:** `Holder` returns null on a provisional government. France's stand-in still seats the RN at Finance, the diagnostic asserts it, and no partner runs it. B, C and D are recorded as owed.

### The family fp716b

- **The digests reproduce** by the stated method: 777 gives `bd910875…1e0b` and 424242 gives `6a3e305b…bacf`, and both are pinned in the sentinel.
- **Against ps3k:** Germany moves first, with turn-1 approval −0.75 on both seeds. At turn 100, Germany's GDP is −0.384 / −0.471 %, Italy's +0.087 / +0.108 %, and France's −0.003 / −0.007 % (late digits).
- **Against fp716, a correction to the message:** Germany and Italy are equal to rounding, not to the byte. From turn 2 they move through `Zone.InterestRate` (777: 3.0324 → 3.0350), the euro-zone rate France's removed partner no longer pushes. France itself moves only at turn 1 (approval +0.75 back) and then through the same zone.
- **The sentinel note is right as written.** It claims no byte-identity with fp716. Its "through trade" for France would be more exact as "through the euro-zone rate and trade".

**Notes (none blocking):**
- **Check (j) proves "never two steps", not that the bill's step lands.** It advances with `AdvanceDay` only, without the country day tick that counts a bill down, so the government's bill can never be adopted inside it. Its 0.00 / 0.00 is that, not a rejection. A run with the day tick, asserting the adopted bill's −0.25 / −0.25, would close it.
- **On an old save the preview can fix VAT's seed.** The preview reads the real country's lines, so on an old save (VAT seed 0) its first `Target` read sets VAT's seed to the rate at that moment, not the boundary's. A budget adopted between the preview and the boundary would then anchor VAT on the earlier rate. This is old saves only.
- **Stale drafts stay in the draft dictionary.** The held rates' drafts stay in `_taxRateInputs`, so they would reappear if the partner left office. That is harmless (they are visible and editable again then), but clearing them when the partner arrives would be tidier.

**Money path:** sound. One partner step a year in every path: at the boundary where the player governs or the country is AI-governed, and on the government's bill in the player's AI-governed country. The ministry's claim is kept in all three: at the boundary, on the bill, and in the preview. The anchor is fixed at creation, the cabinet choice follows the party's side of the state, the preview carries the step, and the file is now a money path.

**Verdict (second pass): READY.** The ledger's baseline rows cite fp716b. Check (j)'s adoption run is a note for the next pass on this code, not a condition.

## Third pass (2026-10-01, the step memory)

Reviewed the delta:
- `Country.FinancePartnerSteppedOn`;
- `FinancePartner.StepIntervalDays`, `StepDue`, `Record` and `Apply(…, asOf, …)` (FinancePartner.cs:39-48, 120-124);
- the three callers (`TableGovernmentBudget` 1173, `AdvanceTurn` 4481-4482, the preview 4902) and `ComingBoundaryDate` (2498-2501);
- `AdvanceDay`'s boundary test (SimulationManager.cs:480);
- `PreviewParityDiagnostic`'s clone audit (276-330, `Mark`);
- every path that clones or copies a country or a decision (`ClonePreviewCountry`'s five callers, `PolicyImpactLedger.Strip`, the film driver's save at `UiScreenshotDriver.cs:1275`).

The defect my note pointed at was real (n716e: three tabled government bills, −0.75 in one year), and it is fixed. **Verdict READY.**

- **The boundary arithmetic is right.** `AdvanceDay` returns true where `daysSinceEpoch % DaysPerTurn == 0` (480), and `AdvanceTurn` runs before `CurrentTurn++`. So at a normal boundary `CurrentDate == EpochDate + 365 × (CurrentTurn + 1) == ComingBoundaryDate`, the same clock the bill path records on (`CurrentDate`). Boundaries are exactly 365 days apart, so `>= 365` always passes at the boundary, which is why fp716b is unchanged.
  - **A harness that calls `AdvanceTurn` without the days:** `ComingBoundaryDate` follows the turn count, not the date, so consecutive off-schedule turns are still 365 days apart and the step stays due each turn. That is the right reading for a dump.
  - **The one mismatch:** a harness that both tables government bills and calls `AdvanceTurn` off-schedule. It would compare a bill's real `CurrentDate` with a notional boundary date. No harness does both today; (j) advances real days.
- **A role change mid-year never gives two steps; it can defer one.** The memory spans both paths, so no two steps fall within 365 days, whichever path wrote them.
  - **Why a step can be deferred:** the bill path steps on the arrival day or the fiscal-year date (1 January in Germany), and the boundary path on the epoch's anniversary (6 November for the 2024 start). Crossing between those calendars can push one step up to about ten months.
  - **An example:** a bill-path step on 1 January 2025, then the player takes the chancellery. The 6 November 2025 boundary is 309 days on and not due, so the next step comes on 6 November 2026.
  - **The bill path alone** does the same in its first year (arrival 6 November 2024, then 1 January 2025 not due, then 1 January 2026).
  - **Effect:** benign, since the partner reaches its target a step later, and consistent with "at most once a year". State it in the record.
- **A rejected government bill uses up the year's step.** `Record` runs at tabling (`TableGovernmentBudget`), not adoption, so a bill the chamber rejects leaves no step and no second chance within 365 days. That is consistent with the second pass's "a rejected bill means no step that year", by design.
- **No save or clone listing is needed for the field.** It is a public field of `Country`, so it rides the Newtonsoft save, and an old save loads `MinValue` (due at the next opportunity, at most one early step after an upgrade).
  - **No cloned country runs the rule.** The preview judges due-ness on the real country (`previewedReal`) and never records. `ClonePreviewCountry`'s other callers are previews too. `PolicyImpactLedger` copies decisions, not countries. The film's save serializes the real world.
  - **The clone audit cannot see this field.** `Mark` returns null for a `DateTime` (it marks primitives, enums, strings and `float[]` only), so the field is neither listed nor checked. That is harmless today, because the clone's `MinValue` is never read. If any future preview reads a `DateTime` off the clone, the audit would not catch it. Marking `DateTime` in `Mark` (e.g. `new DateTime(2000 + index, 1, 1).AddDays(pass)`) would close the class for every such field.
- **The checks:** n716f is clean. (j) now proves the adopted bill's single step (−0.25 / −0.25 across three divisions in the year, the memory on the first bill's day), which closes the second pass's note. (f) still steps at the boundary, PreviewParity is clean, and the sentinel is unchanged on fp716b.

**Money path:** sound. One step in any 365 days on every path: the boundary, the government's bill, and the preview (judged, never recorded). The memory is saved, and nothing clones it into a turn.

**Verdict (third pass): READY.** State the role-change deferral and the rejected-bill rule in §716's record. The `DateTime` mark in the clone audit is a note for the next pass on PreviewParity, not a condition.
