# Delta review: wave/usdata - K110, the K112/K113 prereg and wiring, M-2 and the lows

**Branch:** `wave/usdata` in `G:/UNITY/Projects/PoliSim-wt/usdata`. Range `f1decf76..ae15e957`, four commits: `fff66ddd` (K110), `f8065952` (the prereg alone), `2011547d` (K112/K113 wiring, L-5) and `ae15e957` (M-2, L-3, L-4, L-6, L-7). The rulings are read in main's `docs/RULINGS_2026-10-23.md` (K110-K113, lines 162-165), the first review in `Reviews/2026-10-24_usdata_review.md`.

**Reviewer:** independent and read-only. No Unity was run, nothing was pushed, and the only file written is this one. What was run:
- the compile gate, `bash Tools/cgate/gate.sh G:/UNITY/Projects/PoliSim-wt/usdata`: `GATE OK`;
- `perl Tools/us_campaign_spending_prep.pl`: it passes and rewrites `campaign_spending.csv` and `UsCampaignSpending.cs` byte-identically (`git status` clean after);
- two mutants of the prep tool (scratchpad copies): the REP former name planted as "... 2020, INC." gives `1 mismatch(es) - nothing written`; a former name planted on the DEM committee gives 2 mismatches, nothing written. L-3's new identity test has teeth;
- `sha256sum -c` on `senate_specials_2026/`: 5 of 5 OK after L-7's fetch-log line (`fetch_log.txt` is in no SHA256SUMS and no generator digests it);
- a perl line-ending and encoding audit of every changed blob (below);
- the K110 ratio by hand: 347,027,210.14 / 2,400,000 = 144.594671 dollars a unit, so a rally (300,000 units) is $43,378,401, as K110 says.

**Money-path files reviewed (K110):**
- `Assets/Scripts/Elections/LiveCampaignSetup.cs` (`UsDollarsPerUnit`, `PursePerUnit`, the US staging's chest at line 311, the note at 333-335, `TryCreatedDayZero`, the Swedish and German stagings at 426 and 513);
- `Assets/Scripts/Elections/PreCampaignRun.cs` (`LetterTariff`, `RevenuePerLetter`, `FundraiseLine`, `FundraiseOpen`, `Price`, `StepDay`'s FUNDRAISE case, `Outcome`, the digest);
- `Assets/Scripts/Elections/Generated/UsCampaignSpending.cs`, `ElectionsData/usa/campaign_spending.csv`, `Tools/us_campaign_spending_prep.pl`, `ElectionsData/usa/raw/fec_spending/*` (the two history JSONs read for L-3);
- `Assets/Scripts/Elections/LiveCampaignSnapshot.cs` (the run-up HQ's notes, the queue, the resource pool);
- `Assets/Scripts/Elections/CampaignRun.cs` (the internal poll's charge, the pools), `Assets/Scripts/Elections/CampaignActions.cs` (the price table), `Assets/Scripts/Data/EnergyLayer.cs` (`NationalPerUsd`, 1 for the USA);
- `Assets/Scripts/UI/GameController.Campaign.cs` (`Kronor` and its call sites 541, 639, 667, 678, 857, 1065; the run-up verbs 903-954), `GameController.CampaignAction.cs:136`, `GameController.CampaignPolling.cs:241/261`;
- `Assets/Scripts/Simulation/SimulationManager.cs` (`QueueCampaignDecision`, 6168-6179), `Assets/Scripts/Persistence/SaveGameService.cs` (format 63, the refusal, `RestoreCampaign` by replay);
- the Editor side: `UsCampaignSpendingCheck.cs`, `UsCampaignDiagnostic.cs` (f) and (l), `UsCampaignGeographyCheck.cs`, `CampaignPersuasionDiagnostic.cs` (the mailing check), `CreatedPartyCampaignDiagnostic.cs`.

**Read for tracing (K112/K113, M-2, L-4):** `UsGroundGame.cs` (`Effects`, `EffectsOnCurve`, `Frame`), `GotvTurnout.cs` (`Coverage`, `PartyTurnout`, `RegionVotes`, the band), `ElectionDay.cs` (the 5b `Count` overload, 120-175), `SpatialBudgetVote.cs` (`Applies`, `Alignment`), `Congress.cs` (`Applies`), `UsMajorityReconciliation.cs` (`TryPartyRate`, `Book`, `Applied`), `FinancePartner.IsHouseholdRate`, `WorldFactory.cs` (the US seed: VAT not implemented), `SimulationManager.MajorityReconciliation.cs`.

## Verdict: NOT READY

One finding blocks: **H-1**. K113's own words are "the clause stays failing by name as the record". The prereg and the wiring do the opposite. When the live path cannot flip, the diagnostic appends a line and does **not** fail. The fix is a few lines. It needs one prereg amendment, committed before any run, and that amendment only makes the rule stricter. With H-1 fixed, and M-1 (the run-up HQ's "YESTERDAY" note) fixed or put to Elias, the branch is READY.

K110's arithmetic is right on every path that charges or credits the purse. The prereg was committed alone, before the check that reads it, and it was not touched after. Its closed form is 5b's arithmetic exactly. M-2's unplanted line restores the law, and its DEM threshold is right. The no-policy dump stays byte-identical. The encoding is clean.

## 1. K110: the purse in model units

**The conversion is derived, not typed.** `UsDollarsPerUnit => UsWarChest / WarChest` (LiveCampaignSetup.cs:56). `PursePerUnit(country)` is that ratio for `CountryId.USA` and exactly `1.0` otherwise (:60). For every non-US country, every `/ PursePerUnit` and `* PursePerUnit` is a division or multiplication by 1.0, which is exact in IEEE arithmetic. So Sweden and Germany are bit-identical, both in the purse and on the HQ.

**Every place money enters or leaves the purse:**

| path | where | units | verdict |
|---|---|---|---|
| the US chest at staging | LiveCampaignSetup.cs:311 | `WarChest` (2.4 M units = K19's $347,027,210.14) | right |
| a carried run-up chest | `brought.Value.Money`, from `PreCampaignRun.Outcome(s.Money)` | units (the run-up starts at `p.StartingMoney`, PreCampaignRun.cs ~238) | right |
| the act prices, offices, staff, the internal poll | `CampaignActions.Spec`, `CampaignOffices`, `CampaignStaff`, `PollingHouse(..., 120_000)` | the model's table read as units, so ×144.6 in dollars, as K110 rules | right |
| FUNDRAISE affordability | PreCampaignRun.cs:119 | `letters × tariff / perUnit` | right |
| FUNDRAISE price (the queue, the UI's dimming) | PreCampaignRun.cs:252 | `/ PursePerUnit` | right |
| FUNDRAISE cost and revenue | PreCampaignRun.cs:361-366 | both divided once; `s.Money += raised - cost`; `entry.Cost`, `entry.Raised` and `s.Raised` in units | right |
| a queued mailing | `QueueCampaignDecision(..., PreCampaignRun.Price(pre, kind))`, re-priced on the day by `StepDay` | units, re-derived on the day | right |
| the next mailing's line | `FundraiseLine` (PreCampaignRun.cs:102-111) | national dollars: the sourced postage and revenue, never converted | right: it is a national figure, labelled with the purse code |
| the HQ's money figures | `Kronor` (GameController.Campaign.cs:1338-1341) × `PursePerUnit(PlayerCountryId)` | dollars | right |
| **the run-up's "YESTERDAY" note** | **LiveCampaignSnapshot.cs:287-294** | **units, labelled USD** | **missed: M-1** |

- **No double conversion.** Each sourced national price is divided exactly once, where it enters the purse. Each purse figure is multiplied exactly once, in `Kronor`. Nothing that `Kronor` prints was already converted: the queue's `MoneyCost`, the option costs, the poll offers, the payroll and the office upkeep are all units.
- **The created party** (`TryCreatedDayZero`, Sweden only) is `WarChest × share`, in units, as before.
- **Save and load.** No purse is stored. A campaign restores by replaying its queue (`RestoreCampaign`), and each run-up price is re-derived on the day. A v62 save is refused by the plain `version != CurrentSaveVersion` inequality, and the format-63 comment names K110. See L-2 for a dev-only v63 caveat.
- **The checks.**
  - `UsCampaignSpendingCheck` asserts `perUnit == WarChestUsd / WarChest`, `perUnit > 1`, and Sweden and Germany at 1.0.
  - (f) asserts every US chest at `WarChest` and `WarChest × perUnit` = K19's chest within 1e-6.
  - `CampaignPersuasionDiagnostic`'s mailing check expects the net divided by `PursePerUnit(c)`.
  - (l) prints spending in dollars (units × ratio).

## 2. The prereg and K112/K113's wiring

**The ordering holds.**
- `f8065952` touches one file, `Reviews/2026-10-24_usdata_prereg.md`.
- Its parent is `fff66ddd`. `2011547d`, the first commit whose code reads it, follows two minutes later.
- `git diff f8065952 ae15e957 -- Reviews/2026-10-24_usdata_prereg.md` is empty.
- No Unity run has measured anything.
- The rules are concrete and falsifiable: every state, either party, within 1e-3 points of a closed form; unworked states within 1e-3; flip, bit-for-bit others, exact electors.
- **Caution for the integrator:** a squash merge would erase the commit-order evidence. Keep the commits, or record the prereg in the plan's US-31 block before the run, per IMPL_BRIEF's `plan_prereg` convention.

**The closed form is 5b's arithmetic.**
- `GotvTurnout.RegionVotes` gives each column's supporters `eligible × s / Σs` a turnout of `base + Coverage(doors, supporters) × cace/100`, where `Coverage = 1 - e^(-doors/supporters)` (GotvTurnout.cs:120, 134-137).
- `UsGroundGame.Effects` passes per-state `BaseTurnoutAt(r)` and `DoorCacePp(USA, base)` (UsGroundGame.cs:175-176).
- `ElectionDay.Count` (5b) shares are `expected / regionCast`. With sigma 0 the noise is the identity, and whole-vote rounding is at most about 4e-4 points in the smallest state.
- With 40 doors per eligible voter, `doors / supporters = 40 / s_p`, so `k = 1 - e^(-40/s_p)`.
- The worked column's votes are `s_p(b + kc)` and every other column's `s b`. The total is `b + s_p k c`. The prereg's `D` follows exactly.
- `ClosedFormMovePp` (UsCampaignDiagnostic.cs:388) implements it on the normalised shares, with `k` from `GotvTurnout.Coverage` on `eligible × s_p`, which is the same supporters figure.
- Its sign is REP less DEM; the band takes absolute values.

**Rule 1's wiring matches the text.**
- `FiveBBand` (:587) works each of the 51 states for each party, on US-9's map and on the staging map, noise off (`Effects(map, c, 0, 0.0)`).
- It counts any departure over `UnworkedPp` = 1e-3 and prints the band and the worst departure.
- `Sweep` still holds unworked states within 1e-3.
- The planted tooth (:643) is now `idleMost < liveBand.Low`, a NaN band failing it. That closes the first review's L-5.

**Rule 2 / K113: see H-1 and M-2.** The flip branch is as registered. The K113 branch does not fail, against K113's words. Its guard also does not read the live path's move, so it can label a broken live path as a ruled outcome.

A prereg nit, for the reader only: "up to about 7 points" in the 30-50 % band. The closed form gives about 9.1 points at b = 0.31 and 6.4 at b = 0.45. No US state's 2024 base is under .50, so nothing reads it.

## 3. M-2's unplanted K72 lines (UsMajorityReconciliationDiagnostic.cs ~110-132)

- **The law is restored.** Both planted rates are saved (`plantedIncome`, `plantedCorporate`) and restored in a `finally`. `Book` and `RatesOf` are pure: they build bills and mutate no country. So (c) onward still sees the planted world.
- **REP's rate:** `TryPartyRate` and `TryUsScale` read the seed (`BaselineTaxRates`) and the statute, never the standing `Rate`. So `repRateOf`, computed on the planted world, is still REP's rate on the unplanted one.
- **The two -1 readings are right.**
  - REP's standing distance is about 0, the bill's distance equals the move, and `Alignment = (√s - √b)/√m = -1`.
  - `Book(REP)` is null because the only household rates are income and VAT (`FinancePartner.IsHouseholdRate`), the US VAT is seeded not implemented (WorldFactory.cs:227), and income stands at REP's rate within `MinPoints`.
  - That fits K111's "other lines may still go": none can go on the default seed.
- **The DEM threshold is right.** With gap g = 18.30 - 17.10 = 1.2, a rise of t gives `Alignment = (g - |t - g|)/t`. That is positive exactly for 0 < t < 2g = 2.4. The probes at 2.15 and 2.65 read about +0.12 and -0.09. The ±0.25 margin is ample against float error.

## 4. The lows

- **L-3: closed.** The prep tool now asserts `name`, plus `former_committee_name` and `former_candidate_id` for the converted committee, or `designation_full` "Principal campaign committee" otherwise. The saved JSONs carry exactly those values, and both mutants are caught.
- **L-4: closed in substance, but the comment overclaims (L-1 below).**
  - `TryPartyRate` now also needs `SpatialBudgetVote.Applies(country)`, which is `Congress.Applies`: the USA with a sworn Senate. It is not a player test.
  - Every caller (`Book`, `Applied`, both from `SimulationManager.MajorityReconciliation.cs`, behind `Due`/`TryOutPartyMajority`, which need seats) already ran only where a sworn Congress exists, and only on the player's day tick. So no non-player behaviour changes, and no player behaviour either.
  - The gate only adds a `false` where no Senate is sworn.
- **L-6: closed.** No `<see cref` is added anywhere in the range.
- **L-7: closed.** The fetch log names the page as corroboration only. The digest folder still verifies.

## 5. The no-policy dump: unchanged

- **K110** touches only the player's staging and run-up (`LiveCampaignSetup.TryFor`/`PreCampaignRun`, reached only from `PlayerCountryId`'s campaign) and the UI.
- **L-4's gate** can only return false, and only on player-tick paths.
- **M-2 and K112/K113** are Editor code.
- Non-US purses are bit-identical (×/÷ 1.0).

The first review's section 6 argument stands, so TrajectorySentinelCheck should be unchanged.

## 6. Encoding

- Every changed blob is LF in the index (perl count: zero CRLF and zero CR-CR in every blob).
- In the work tree, every `.cs` file and the prereg are uniformly CRLF (autocrlf). `fetch_log.txt` (`-text`) and the prep tool are uniformly LF. No file is mixed.
- Non-ASCII byte counts are unchanged in every file, and there is no `\xC3\x82` / `\xC3\x83` double-encoding.
- No `//` sits inside a C# string literal. The only hits are perl's `//` defined-or operator in `us_campaign_spending_prep.pl`.
- No gendered text is added, and no new `<see cref`.

## Findings

### High

**H-1. K113 says the clause "stays failing by name as the record". The prereg and the wiring make it pass.**
- **Where:** `Reviews/2026-10-24_usdata_prereg.md:37` ("K113's reading - not a failure ... and does not fail"). `Assets/Editor/UsCampaignDiagnostic.cs:670-678`: in the no-flip case it calls `sb.Append("REGISTERED OUTCOME CHANGED (K113) ...")` and never calls `Check(false, ...)`.
- **What the ruling says:** RULINGS_2026-10-23.md:165: "US-31's registered outcome is recorded as changed by K18 and the sourced 5b count is kept; **the clause stays failing by name as the record**." K112 adds "a miss comes back to Elias". The project rule is that a check fails while its statement is false, and the codebase already keeps a check RED as a named gap (the AreaIconCoverageCheck convention).
- **Failure scenario:** The integrator's named run cannot flip the closest state, which is likely at 5b's 0.4-1.2-point moves (the first review's M-1). UsCampaignDiagnostic then prints PASS, and the bar is green. "Able to flip it" is false in play, yet no failing line carries it. A later reader of the bar's summary sees nothing owed.
- **What to do:**
  - Make the K113 branch a failing, named check: `Check(false, "(m) REGISTERED OUTCOME CHANGED (K113): ...")`. Keep the line's text, which is good.
  - Amend the prereg's K113 paragraph to "fails by name (K113)" in a commit before any run. This only makes the rule stricter, so it is not tuning.
  - The integrator records the red line as the ruled record, with the AreaIconCoverageCheck convention.
  - If Elias meant "recorded by name, not failing", he says so and the code stands. The ruling's words read the other way.

### Medium

**M-1. K110 missed one display: the run-up HQ's "YESTERDAY" note prints purse units labelled as dollars.**
- **Where:** `Assets/Scripts/Elections/LiveCampaignSnapshot.cs:287-294`. It formats `e.Cost`, `e.Raised` and `e.Raised - e.Cost` with `{0:N0} USD`. Since `fff66ddd` those are units (PreCampaignRun.cs:362-366), and they are not passed through `PursePerUnit`.
- **Failure scenario:** On the USA's run-up, a FUNDRAISE day:
  - the FUNDRAISE line reads `POSTAGE 82,000 · RAISES 40,100 · NET -41,900 USD`;
  - the next day's note reads `YESTERDAY · FUNDRAISE NATIONAL · 567 USD · RAISED 277 USD · NET -290 USD`.

  A television buy reads "300,000 USD" in the note while the queue (via `Kronor`) priced it at 43,378,401. The chest (hero, `Kronor`) falls by $43 M while the note says $300 k. Sweden and Germany are unaffected (factor 1).
- **What to do:** Multiply the three figures by `LiveCampaignSetup.PursePerUnit(country.Id)` in that note. Also film-check the US run-up HQ (the Canvas film rule) or add a check. `CampaignPersuasionDiagnostic.cs:786` prints `mail.Raised` and `Log[0].Cost` beside a dollar postage the same way. That is a diagnostic print (unlabelled units), so convert or label it in the same pass.

**M-2. K113's guard does not read the live path, and it skips two of the prereg's own failure conditions. It can label a broken count as the ruled outcome.**
- **Where:** `Assets/Editor/UsCampaignDiagnostic.cs:663-666`. `asClosed` compares the closed form with a **separate** noise-off call, `UsGroundGame.Effects(noisy, worked, 0, 0.0)`. It does not compare it with the live move `Margin(after[t]) - Margin(noisy[t])` from `UsPresidentialElection.Count`, which is printed but never judged.
- **What the prereg says** (rule 2): the named line requires "the worked state's move equals rule 1's closed form", and "the electors wrong" fails.
- **What the K113 branch checks:** neither `gained == 0` nor `flipped.GroundGame.Count == noisy.Length`, both of which the flip branch asserts.
- **Failure scenario:** A regression in the live path, for example `UsPresidentialElection.Count` dropping or mis-routing the campaign's doors, leaves the worked state unmoved. The call does not flip, the other states are bit-for-bit the same, and the independent noise-off `Effects` still equals the closed form. So the diagnostic prints "REGISTERED OUTCOME CHANGED (K113)" instead of a defect. No other check drives `UsPresidentialElection.Count` with a worked campaign (this is the only call that does, :656).

  H-1's fix makes this branch fail either way, but the record would then name the wrong cause.
- **What to do:** In the K113 branch, also require:
  - `gained == 0` and `flipped.GroundGame.Count == noisy.Length`;
  - that the live move equals the count's own function on the live inputs, bit for bit: `UsGroundGame.Effects(<the live derived map>, worked, noiseSeed, UsGroundGame.NoiseSigmaPp)` against `none` at the same seed. Alternatively, require the live move within the closed form ± a stated noise-scaling allowance.

  Keep the noise-off closed-form comparison as it is. A strict 1e-3 on the noisy move alone would be wrong: the scaled noise's √(s(1-s)) term shifts by about 5e-4·z points.

### Low

- **L-1. L-4's comments overclaim "the player's".** SpatialBudgetVote.cs:69 ("Its callers gate it to the player's US Congress") and UsMajorityReconciliation.cs:84 ("reaches the player's US Congress only") describe `Applies`, which is `Congress.Applies`: any USA with a sworn Senate, not a player test. The player property still rests on the callers (the day tick). Say "a US Congress (sworn Senate); the player's by its callers".
- **L-2. Format 63 now carries K110's meaning too, with no bump inside the branch.** A dev save cut on this branch between `6cd482fd` and `fff66ddd` is v63 and loads. Its campaign queue's `Spend` figures and run-up mailings replay against a 2.4 M-unit chest where they were made against $347 M, which `AssertReplayLandedOn` only logs. Nothing was released at 63, so this is harmless. But the integrator renumbering against wave/fed's 63 should bump past both.
- **L-3. K110's consequence at the per-act level is ruled but stark.** In the USA the model's table now reads $43.4 M a rally, $17.4 M an internal poll, about $723 a door (5 units) and $289 k a day per office's ground operation. K110 rules "every act's price the model's times it", so this is not a defect. But it should sit on the play-calibration list beside K19's BILLED unit prices, so a reader does not take the HQ's per-act dollars as sourced.
- **L-4. The prereg lives in `Reviews/`, not in the plan's US-31 block.** IMPL_BRIEF's convention is that `plan_prereg` is committed by the integrator into `docs/specs/USA_STAGE_PLAN.md` before the run. Do so at integration (with H-1's amendment), and keep `f8065952` unsquashed.
- **Carried from the first review, not in this range:** M-3 (declare the played-US family), L-1 (the special's winner sits from 3 January), L-2 (the AI's national door band) and L-8 (DEM's roll-call anchor). They belong in the integrator's draft records.

## What a passing named run must print (after H-1, M-1 and M-2)

- `US CAMPAIGN SPENDING: PASS`, including the K110 line (144.5947 dollars a unit; a TV buy and a rally in dollars).
- UsCampaignDiagnostic:
  - (f) with `2,400,000 units ... 144.5947 dollars a unit, 347,027,210.14 dollars each`;
  - both `(m) K112, THE REGISTERED 5b CEILING` lines `ok`, with their bands and `worst off` at or under 1e-3;
  - the planted tooth `ok`;
  - then either `(m) THE DONE-WHEN, able to flip it` `ok` (FLIPS), or the named failing `REGISTERED OUTCOME CHANGED (K113)` line, which is the ruled record and goes to Elias per K112.
- UsMajorityReconciliationDiagnostic: `(b) THE REVIEW'S M-2, K72 UNPLANTED` `ok` (REP -1.00 and -1.00, no book, DEM + below 2.40 and - above).
- CampaignPersuasionDiagnostic's mailing line `ok` for Germany and the USA.
- TrajectorySentinelCheck unchanged.
- A US run-up HQ film showing the YESTERDAY note in dollars (M-1).
