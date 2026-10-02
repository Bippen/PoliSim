# Review — s755 nothing frozen: a partner holding Finance acts through the fiscal stance only, the same for the player and the AI (Elias's ruling A2) (2026-10-02)

Reviewed: the staged diff against HEAD 19a3366 - `Assets/Scripts/Simulation/FinancePartner.cs` (rewritten; a money path by name), `Assets/Scripts/Simulation/SimulationManager.cs` (a money path by name), `Assets/Scripts/Data/Country.cs`, `Assets/Scripts/Data/CabinetDecision.cs`, `Assets/Scripts/UI/GameController.cs`, `Assets/Scripts/UI/GameController.BudgetV35.cs`, `Assets/Scripts/UI/RangeCaptions.cs`, `Assets/Editor/FinancePartnerDiagnostic.cs`, `Assets/Editor/PreviewParityDiagnostic.cs`, `Assets/Editor/RangeCaptionCheck.cs`, `Assets/Editor/TrajectorySentinelCheck.cs`; the family `fp755` against `fp717` - its sentinel digests 777 `2f70e004111451927f85fb1d0743f4c1d872b7f449e9449dae97bdd07fdb1b04` and 424242 `35b65eca7bfda2c64c410cedc7eb135178634c022b43ab5c585afde1b1bf6886`, the dumps behind them diffed by both readers (`traj_diff fp717 fp755`, both seeds). One independent read-only reader; its pass verbatim, then what the author did about it.

## The pass (verbatim)

Reviewed: the staged diff (11 files) against HEAD 19a3366. FinancePartner.cs:1-246 in full; AiFinanceMinistry.cs:1-252; SimulationManager.cs:1160-1275, 1298-1360, 2195-2225, 2405-2505, 4505-4560, 4820-4830, 4925-5080, 6235-6285; GameController.BudgetV35.cs:255-330, 465-535; GameController.LawsV35.cs:1160-1240; LedgerRow.cs:317-345; RangeCaptions.cs:1-80, 390-407; Country.cs:179-192, 262-268; SaveGameService.cs:50-90; ParliamentSystem.cs:452-458; FinancePartnerDiagnostic.cs, and the PreviewParity, RangeCaption and TrajectorySentinel diffs; COMPLETED §716; the logs n755, n755b, n755c, probe755, sim755 and dump755; traj_diff fp717→fp755 for both seeds, including `first`, plus per-turn extracts.

Verdict: NOT READY

## Defects

1. **SimulationManager.cs:1173 (with 1242-1243 and 1258-1262): the stance count is credited when the bill is tabled, not when it is adopted.** `TableGovernmentBudget` records the step right away. The chamber can still reject the bill: Germany's procedure is Unsourced, so the government's bill is voted alone, and `ApplyBillResult(passed:false)` applies nothing. In Sweden the alternative can be adopted instead. Under §716 the target was an absolute anchor, so a lost step corrected itself. Now the count is the partner's only memory.
   - Scenario: a German player in opposition, an AI CDU head and the SPD at Finance. The government's bill fails, and the SPD's count still reads +0.25 although nothing moved. A few failed bills and the SPD "has moved" its +0.78 without moving anything.
   - FinancePartnerDiagnostic (j) only checks that the bill was voted, not that it was adopted.

2. **FinancePartner.cs:140-141, 154-155: the count is kept per holder only, but the target is measured against the current head.** No code resets the count when the government changes. It also survives a period when the head's own party holds Finance, because `Holder()` returns null then and nothing is recorded.
   - Scenario (lrecon SPD 3.47, FDP 7.58, CDU ≈6.59): the FDP moves −1.03 under an SPD head. A new government is formed with a CDU head and the FDP still at Finance. Its target is now −0.25 against a count of −1.03, so it steps **+0.25 a year for three years**. A fiscal hawk expands.
   - Player version: a junior partner that has moved +2.5 can never move again while it holds Finance, through any number of governments. The dial's ±2.5 limit becomes a lifetime cap.
   - Meanwhile a different holder taking over resets the count. The rule's own reason ("a new holder starts from the book as it stands") applies just as much to a new government.

3. **TrajectorySentinelCheck.cs:109-115: the family's account does not match the dumps.**
   - probe755 matches seed 777: Germany's debt ratio is 51.9 / 50.6 / 50.3 / 31.6 % at t12 / t15 / t16 / t100.
   - It shows **four** steps: 2027 +0.25, 2038 +0.25, **2041 +0.25** and 2042 **+0.028**. The account says three, with "+0.28 in 2042". The author's description repeats this.
   - The account also misses a jump at turn 83 (details under question 5).

4. **RangeCaptions.cs:398 and GameController.BudgetV35.cs:325: player-visible text overstates the tightening.** "Hard squeeze: … taxes rise too" and "(A TIGHTENING ALSO THE INCOME TAX AND VAT)" are both wrong. The rates rise only on what the line clamps leave, which is in practice the years the ministry has claimed the lines. How far the dial is set has nothing to do with it.

## The questions asked

(1) **Mostly faithful.**
- Nothing is frozen for the head: the refusal, the budget stripping, `Choose`, the tile hold and the schedule-row hold are all gone.
- The stance is read as the holder's own cumulative push, not a balance target. Elias should confirm that reading.
- The partner still moves a player head's book with no vote: every unpinned line (mandatory included) and, on a tightening with lines pinned, the income tax and VAT. The ruling grants this. An expansion never cuts taxes.
- lrecon as the axis is defensible as [AUTHORED-DRAFT]: CHES has no deficit item, and the sign holds for the FDP and SPD. It replaces §716's `redistribution` (the SPD's ask goes from 0.875 to 0.78), which should be stated.

(2) **The code is symmetric**: one `Apply`/`StepDue`/`Record`, one instrument, and only `Target` reads a different source. Defect 2 breaks the symmetry in practice.
- Refusing a player partner Finance's other levers is the literal reading of "acts through the fiscal stance only". But it removes levers §634 gave the player, so it is a reading for Elias to confirm.
- Edge case: a CSU player at Finance under a CDU head, or a player in a PROVISIONAL government, has `Holder` null. It gets all of §634's Finance levers and no dial, while an AI party in the same seat gets nothing.

(3) **Money correctness.**
- Expand and Tighten are correct as written: the moved amount is never more than the amount asked, and `StancePoints` has the right sign and is nominal over nominal GDP.
- The count records what was asked, not what landed. The applier moves `(Amount − dial cost)` and clamps to the seed band, and the tax remainder uses a static base. The error is small.
- No double counting: the boundary and bill paths exclude each other, the 365-day rule holds across mixed dates, and preview, boundary and bill all skip lines the ministry claimed.
- Withdraw runs on every path, except if an exception is thrown between Apply and Withdraw (there is no try/finally).
- `Record` never runs on a preview.
- On the bill path the arrival window plus the fiscal-year date skips one year after each new government.

(4) **Yes, it can ratchet without bound when different holders alternate.** Each holder's time at Finance adds up to its own target. In AI governments the ministry's EU rule pushes back; under a player head only the player does. That is acceptable as a stated reading only if the reset happens per government (defect 2).

(5) **The other countries are explained**: France and Italy diverge first at t2 through the euro-zone rate, Poland, Sweden and the USA through currency and trade. Germany is not:
- The step timeline is wrong (defect 3).
- Seed 777's t100 figures come mostly from a one-turn jump at t83, 41 years after the last step. The government-consumption gap goes from −1.79 % to −7.52 % and the debt gap from +0.09 % to −4.82 % in one turn. Through t82 the debt gap agreed in sign with seed 424242's +0.58 %.
- The account never says why a partner that expands ends with lower government consumption on both seeds. The gap is +0.61 % at t2 and negative from t3 as the deficit gap widens. The likely cause is the EU rule cutting the lines to close a deficit widened both by the expansion and by §716's rate rises being gone.

(6) **Saves.** They use Newtonsoft (`SaveGameService.BuildSettings`), not JsonUtility. The three public fields are serialized. The save version stays 37, and an older v37 save loads them as 0 / null / 0, keeping `FinancePartnerSteppedOn`. A v37 German save made under §716 keeps the SPD's raised rates and starts its stance from zero on top of them. That is acceptable but should be stated.

(7) **UI.**
- The tile shifts the control IDs of the sliders after it. It appears or disappears only at a government formation, which runs in Update, so the only risk is a drag in progress at that moment.
- Germany's default start (SPD at Finance) now draws the tile in every German game, and no film was cited.
- The grep for "HELD BY", "household rates" and "partner's lever" finds no stale player-visible text.
- Wording:
  - "AT MOST 0.25 A YEAR" has no unit.
  - "NOTHING ELSE OF YOURS IS HELD" reads oddly to a player in opposition.

(8) **Stale comments:**
- FinancePartner.cs:33 says "asks about +0.9"; it is +0.78.
- FinancePartner.cs:95 says "(the page reads it)"; the page no longer reads it.
- SimulationManager.cs:2482-2483: `FinancePartnerOfPlayer` still has §716 wording, and only the diagnostic calls it now.
- CheckSuite.cs:211 (not staged) still describes §716.
- Country.cs:267 calls VAT's seed "the Finance partner's anchor"; nothing reads it now.
- PreviewParityDiagnostic.cs:166 tests "has ever stepped", while its comment says "at this boundary".

## Notes for the record
- The constant was renamed (`PointsPerChesPoint` → `StancePointsPerChesPoint`) and `PlayerTargetLimit` added. The §755 record should re-cut the 26th play-calibration entry.
- PreviewParity no longer asserts the preview's partner step. Comparing the preview's Written to the boundary's would restore that.
- Review-ledger rows are owed for FinancePartner.cs, SimulationManager.cs and both new digests.
- The cheap bar was not run on this state, only named subsets.
- sim755.log shows the sentinel passing with the fp755 digests, but the 57-check bar log stops after about 15 checks, with no CHECKS summary line.

## What was done about the pass (the author's)

1. **Defect 1, fixed**: on the government's bill path tabling spends the year's step (§716's premise kept: a rejected bill spends it) and the step rides the bill (`BudgetBill.FinanceStanceHolder`, `FinanceStancePoints`, `FinanceStanceAppliedBefore`, `FinanceStanceGovernment`); the count moves only when the chamber adopts the government's bill (`FinancePartner.CreditAdopted`, called in both branches of `ResolveGovernmentBudget`) and the government that tabled it still sits. The diagnostic's (j) now asserts it on the reviewer's own scenario: Germany's government bill fails (*the old budget stands - this country's procedure is not yet sourced*) and the count stays 0, the year's step spent (`n755d`).
2. **Defect 2, fixed**: the count is kept per holder AND per government (`Country.FinanceStanceGovernment`, the government's FormedOn); the same holder in a new government starts from zero, its target read against the head that sits - the hawk no longer expands, the player's dial is no lifetime cap. Asserted (`n755d`: *the same FDP's count from a government before this one reads zero*). The family is byte-identical after the fix (the dump's one partner sits in one government all century; `fp755b` dumped and compared, deleted).
3. **Defect 3, fixed**: the account rewritten - four steps on 777 (+0.25 in 2027, 2038 and 2041, +0.03 in 2042); why consumption ends lower (§716's rate rises gone, the early deficit wider, the EU rule's cuts harder later); and t83 - the two paths on opposite sides of the Fiscal Compact's 1 % deficit limit (debt near 33 %, below 60: the ministry cut fp755's lines and not fp717's - a threshold of the treaty's rule, 41 years after the last step).
4. **Defect 4, fixed**: *Hard squeeze: Two points tightened. Lines cut deep.*; the tile's census: *AT MOST 0.25 PP OF GDP A YEAR, THROUGH THE LINES - THE INCOME TAX AND VAT ONLY FOR WHAT THE LINES' LIMITS LEAVE*, and for an AI partner *THE HEAD OF GOVERNMENT KEEPS EVERY OTHER LEVER*; *MOVED SO FAR IN THIS GOVERNMENT*.
5. **The stale comments**, fixed (the constant's example and the `redistribution` → `lrecon` change stated; `AiHolder`'s reader; `FinancePartnerOfPlayer`; `CheckSuite`'s line; VAT's seed comment; the parity comment).
6. **Stated, not changed**: the stance read as the holder's own cumulative push and the player partner's other Finance levers refused - both owed to Elias as readings; a CSU player at Finance under a CDU head, or in a PROVISIONAL government, holds no partner seat (the group's and decision A's rules) and keeps §634's levers; the count records what was asked, not what the applier lands after the dial cost and the seed band (small); the bill path's skipped year after a new government (§716's, kept); a v37 save made under §716 keeps the SPD's raised rates, its stance from zero; saves are Newtonsoft (the record corrected); no film session reaches a partner's tile (Germany's start seats the SPD's own Finance; the tile draws after the 2025 formation seats Merz with the SPD at Finance) - owed a film when such a session exists; the parity check no longer asserts the preview's partner step (a comparison of the preview's Written to the boundary's is noted for later).

## The rework pass (verbatim) - a second reader on the four fixes

Reviewed: the staged diff against HEAD 19a3366, a rework pass on the four defects in Reviews/2026-10-02_s755_fiscal_stance.md. I read FinancePartner.cs in full; BudgetBill.cs and Country.cs (diffs); SimulationManager.cs:1160-1345 (Table, Resolve, Introduce, AdvanceBudgetBillDay) and 2450-2510, 4545-4565; every site that assigns or mutates a government (GovernmentRecord.cs:85-115 and 200-305, SimulationManager.cs:2890-2905 and 3735-3752, GameController.cs:2183-2190 and 6417); SaveGameService.cs:55-80 and SaveGame.cs:88; FinancePartnerDiagnostic (j) and (k); the RangeCaptions, BudgetV35, PreviewParity, GameController and CabinetDecision diffs; `traj_diff fp717 fp755` on both seeds; the per-turn CSVs for t78-t86; probe755, n755d, sim755b and dump755b.

Verdict: READY

### Defects
None found.

### What was verified
1. **The step is credited only on adoption.**
   - `CreditAdopted` runs only when the government's frames are adopted (SimulationManager.cs:1252) or when the government's bill passes on its own (SimulationManager.cs:1264). Those two branches never both run, and each bill is resolved once and then removed (SimulationManager.cs:1333).
   - When Sweden's chamber adopts the alternative, nothing is credited.
   - No path withdraws or replaces a pending bill. `TableGovernmentBudget` and `IntroduceBudgetBill` both refuse while a bill is pending, and only resolution or a load clear removes one.
   - A step cannot be counted twice. `StepDue` blocks the boundary step for 365 days after tabling, and the bill lives 21 days.
   - If the government changes while a bill is pending, `FormedOn` no longer matches, so nothing is credited.
   - The new BudgetBill fields are public, so the save serializer writes them with `PendingBudgetBills`. Older saves load them as null or default, which credits nothing.
2. **The count resets with a new government.**
   - Every place a government is constructed sets `FormedOn` (GovernmentRecord.cs:210, 231, 260, 283, 293).
   - Installs and successors use `CurrentDate`, and the post-election government uses the election date, so two different governments do not share a date except in a theoretical double formation on one day.
   - `Record` always goes through `Credit`, which sets `FinanceStanceGovernment`.
   - `LeaveCabinet` keeps `FormedOn` but changes the holder, which already resets the count.
3. **The family's account matches the data.**
   - Four steps on seed 777: 2027-01-18, 2038, 2041 and 2042, the last one +0.028.
   - At t100: consumption -6.08 / -1.22 % and debt -5.27 / +0.58 % on 777 / 424242.
   - At t83, fp755's consumption falls 983 to 930 while fp717's rises 1001 to 1006. The debt gap goes from +0.08 % to -4.8 % that turn, with debt at 32.4 %. The SPD under the CDU holds Finance all 100 turns.
4. **The wording is fixed.** The bands no longer say "taxes rise", and the census says the rates move only for what the lines' limits leave. Band indexing puts 0 in "Neutral".
5. **The diagnostic assertions are real.** (k) moves the government date back 400 days and asserts the count reads zero. (j) asserts the bill carries -0.25 with nothing counted. After the failed vote it asserts the count is still +0.000, the step was spent once (2024), and the expected count follows the actual outcome.
6. **The dumps.** sim755b compiled the final state: the sentinel passes against fp755 and 57 of 57 checks are clean.

### Notes
- The tile's census line also shows on an expanding dial. Its "THE INCOME TAX AND VAT ONLY FOR WHAT THE LINES' LIMITS LEAVE" reads as if rates could fall. `Expand` never touches rates: what the clamps leave is simply not given. Adding "IN A TIGHTENING" would make it exact.
- The adoption branch of `CreditAdopted` is not asserted in this run, because (j)'s German bill was rejected. Only the rejection path is proven. A planted passing case would close that.
- FinancePartnerDiagnostic (n755d, 12:33) ran before the last Country.cs edit (12:35). That diff holds only fields already used at 12:31 plus comments, so the risk is low. It is not in the 57-check group that ran later.
- The tile is drawn whenever `Holder` is not null, including when the player is in opposition. Its doc says "in the player's government". The text it shows stays true either way.
- The previous review's open items still stand: no ledger rows, no cheap-bar run on this state, and no film reaching the tile.

## What was done about the rework pass (the author's)
The census reads *IN A TIGHTENING THE INCOME TAX AND VAT ONLY FOR WHAT THE LINES' LIMITS LEAVE*; the adoption branch asserted directly (`n755e`: *adopted, the bill's step is counted (-0.250 pp, the FDP's, this government's); tabled by a government that no longer sits, it is not*); the ledger rows added (`FinancePartner.cs`, `SimulationManager.cs`, `BudgetBill.cs`, both baselines), the cheap bar run on the final tree; the tile drawn for any partner holding Finance is said on the record (its words true in opposition too); no film session reaches the tile (stated, owed).
