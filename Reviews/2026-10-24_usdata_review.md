# Review: wave/usdata - K14, K18, K19 and K72, PS-6's US data (save format 63)

**Branch:** `wave/usdata` in `G:/UNITY/Projects/PoliSim-wt/usdata`. The range is `f0977344..8756a283`, four commits: `4214b1d6` (K14), `497c9dd4` (K18), `6cd482fd` (K19, format 63) and `8756a283` (K72). The rulings are read in `docs/RULINGS_2026-10-23.md`, with US-31's PLAN_PREREG in `docs/specs/USA_STAGE_PLAN.md` (the US-31 block).

**Reviewer:** independent and read-only. No Unity was run, nothing was pushed, and the only file written is this one. What was run:
- the compile gate, `bash Tools/cgate/gate.sh G:/UNITY/Projects/PoliSim-wt/usdata`: `GATE OK`;
- `bash Tools/us_senate_mutations.sh`: **51 caught, 0 missed or broken**. The control passes, exits 0 and reproduces the repository's bytes. The five K14 cases are among the 51. The run took 6 m 46 s;
- `bash Tools/us_senate_races_mutations.sh`: **37 caught, 0 missed or broken**, the control passing (a sanity control: that tool is unchanged by the branch);
- `sha256sum -c` on all three new raw folders. `senate_specials_2026/` passes 5 of 5, `q16_scale/` 6 of 6 and `fec_spending/` 8 of 8. `git ls-files --eol` shows every file in them stored `-text`, so a checkout cannot rewrite their bytes;
- an independent perl re-derivation of K19's window from the saved OpenFEC JSON (my own script, not the tool's);
- the Ohio writ, read by eye. Its JPEG was extracted from the PDF's single DCTDecode stream. It carries every word the tool transcribes: Vance's resignation "effective January 10, 2025", Husted appointed "on January 17, 2025", "a special election on Tuesday, November 3, 2026 to fill such vacancy", and the signature date of 29 October 2025;
- a line-ending and encoding audit of every changed blob. All are LF in the index, with no doubled CR and no `\xC3\x82` / `\xC3\x83` double-encoding. The generated `.cs` files are written CRLF and normalised by autocrlf, which is the existing convention. Each file's non-ASCII byte count matches its base, except `LiveCampaignSetup.cs`, which loses one ⚠ (3 bytes).

**Money-path files reviewed:**
- `Assets/Scripts/Elections/Generated/UsCampaignSpending.cs`, `ElectionsData/usa/campaign_spending.csv`, `Tools/us_campaign_spending_prep.pl`, `Assets/Editor/UsCampaignSpendingCheck.cs`, `ElectionsData/usa/raw/fec_spending/*`;
- `Assets/Scripts/Elections/UsGroundGame.cs`, `Assets/Scripts/Elections/GotvTurnout.cs`, `Assets/Scripts/Elections/LiveCampaignSetup.cs`, `Assets/Scripts/Elections/CampaignRun.cs`, `Assets/Scripts/Elections/PreCampaignRun.cs`;
- `Assets/Scripts/Simulation/SpatialBudgetVote.cs`, `Assets/Scripts/Simulation/UsMajorityReconciliation.cs`, `Assets/Scripts/Elections/Generated/UsRateAnchors.cs`, `ElectionsData/usa/q16_rate_anchors.csv`, `Tools/us_rate_anchors_prep.pl`, `Assets/Editor/UsRateAnchorCheck.cs`;
- `Assets/Scripts/Elections/UsCongressElection.cs`, `Assets/Scripts/Elections/Generated/UsSenateRecord.cs`, `ElectionsData/usa/senate_specials.csv`, `Tools/us_senate_prep.pl`, `Tools/us_senate_mutations.sh`, `Assets/Scripts/Simulation/UsCongressView.cs`;
- `Assets/Scripts/Persistence/SaveGameService.cs`;
- the Editor side: `UsCampaignGeographyCheck.cs`, `UsCampaignDiagnostic.cs`, `GroundGameCountDiagnostic.cs`, `UsMajorityReconciliationDiagnostic.cs`, `UsMidtermDiagnostic.cs`, `GeneratedCatalogCheck.cs`, `CongressElectionDiagnostic.cs`.

**Read for tracing, not changed by the branch:**
- `ElectionDay.cs` (both `Count` overloads), `UsPresidentialElection.cs` (`Count`), `SimulationManager.cs` (the US count's gate near line 5771, `AdvanceCountryDayTick`, the staging), `SimulationManager.MajorityReconciliation.cs`;
- `ParliamentSystem.GetBudgetBillConcern`, `StanceModel` (term 1), `UsDebtLimit.ConcernOf`, `Congress.Applies`, `WorldFactory` (the US seeds);
- `UsGps2019.cs`, `NoPolicyCentury.cs`, `TrajectorySentinelCheck.cs`, `UnwiredSubsystemCheck.cs`.

## Verdict: READY

This is conditional on the named runs below and on two questions going to Elias.

There is no code defect that blocks the merge, and no H finding. The two Mediums that matter most are not bugs:
- **M-1:** US-31's registered "able to flip it" now runs through the new 5b count, and may print DOES NOT FLIP.
- **M-2:** K72's sourced scale has a consequence the desk worlds plant around, so no bar shows it. Under the real seed, every Republican reads -1 on every income-tax or corporate-tax cut, and a Republican majority never has a reconciliation book.

Both must reach Elias as asks. Neither may be quietly re-tuned. The no-policy dump stays byte-identical (section 6).

## 1. K14: Ohio's and Florida's specials

- **The sources hold.** The writ matches its transcription (read by eye, above). Ohio's schedule must give the writ's day, and does. Florida's candidate list is the 2026 general's (`elecid` 20261103-GEN). Its Senate table has exactly one incumbent, Moody. The DERIVED step is sound: Florida holds Class 1 and Class 3, the class regularly up in 2026 is Class II, so Florida's Senate race that day is the special for Rubio's Class 3 seat.
- **The generator's checks hold.** The vacated holder must have resigned on the writ's day. The appointee must be appointed and still serving. The state must hold no seat of the regular class. The term end comes from the class page and must fall after the polling day. Each failure path has a mutation case, and all five are caught.
- **`SeatsUp` counts each seat once.** A special joins only off the regular class (`s.Class != cls`), and only where no (state, class) is already up, so a later catalog race for the same seat replaces it. Oklahoma's and South Carolina's Class 2 appointees cannot be counted twice: they are the regular class, and they are not in the CSV.
- **The seat's next election is right.** Its term is "unexpired". ClassUp(2028) is Class 3, from the 2022 races, so the game elects the full term in 2028. That fits `term_ends` 2029-01-03. No runtime reads `TermEnds`, which is consistent.
- **The seating date is 3 January**, under R-US18's NY-22 convention (DECLARED). The record's appointees sit until then. In fact a special winner is seated on certification, in November or December, so the game holds Husted and Moody about six weeks too long. The convention is declared, so this is L-1 only.
- **The board edge works.** `UsCongressView` marks `Special = game && Term != "full"`. UsMidtermDiagnostic (k) asserts exactly FL/3 and OH/3 in brass on 3 Jan 2027.

## 2. K18: the re-cut card and the count on 5b (the registered-outcome risk)

- **The US-9 guard is real.** `UsCampaignGeographyCheck` re-measures (c)'s ceiling on the registered inputs: UF's VEP, its own base turnout, the same seed, the same idle map and neutral attributes. It formats both readings through one `Printed(...)`: range `0.00`, calls, electors and names. That is the card's precision, the same `{0:0.00}` the (c) line prints. The check fails by name ("THE RE-CUT CHANGED A REGISTERED OUTCOME ... put to Elias") on any difference, on a jurisdiction not at mobilisation 100, or on a base that clamps. The salience enters no figure of (c). The check asserts the staging's salience label is Gallup's August 2024 wave, not fielded after the campaign. The digest stamp now carries the CVAP table and `UsCampaignSpending.SourceDigest`.
- **The "measured amount" clause is held honestly, but on a function no count calls.** `EffectsOnCurve` is W-B11's curve, kept verbatim. The code says plainly "Called by no count", and only `UsCampaignDiagnostic` (m) reads it. UnwiredSubsystemCheck judges per file and `UsGroundGame.cs` is wired, so the check does not object. DeadStateCheck scans private declarations, and this function is public. So nothing flags it. That is legitimate only because the PLAN_PREREG says the function is "never tuned on the result". But the prereg names it twice: "the US count's own ground-game function (`UsGroundGame.Effects`)". After K18 those two descriptions point to different functions. The branch held the letter (the curve) and moved the count. The plan's done-when, "a player's ground game in one state moving it by US-9's measured amount", is no longer true of play: 5b's ceiling moves a margin by roughly 0.4 to 1.1 points against the card's 2.90 to 9.00. The branch prints that beside the registered reading ("K18, READ"), and says "a pass rule for 5b's figure is Elias's to register". That is honest, but it should be a named ask in the integrator's record. See M-1.
- **`UsGroundGame.Effects` on 5b is as stated.**
  - Each state's base is `Audience / Eligible`, the prior presidential votes of record over the staging's CVAP.
  - The door CACE is GG14's band of that base: 4.6, 6.2, 1.4, 0.6 by the floors 0, .30, .50, .70. Its upper limits, 6.9, 8.2, 4.0, 2.8, are held as SOURCED constants. The phone CACE is [GG14] p. 19's mean, 1.15, with 1.4 as the bound.
  - The clamp guard is base + (band upper + phone upper)/100 < 1.
  - The new `ElectionDay.Count` overload is the one `ParliamentSystem`-style counts use. It always draws the scaled noise, zeroes a column with no share and renormalises by `kept`.
  - With zero doors, a state's shares cancel the base exactly, as the curve's did. The vote rounding differs, though (see M-3).
- **The AI's door value is declared, but less like Europe's than the declaration says.** `doorCacePp` is the band of the NATIONAL base, about .65, so 1.4. The count reads each state's own band, and every state at or above .70 is at 0.6. "One figure a staging, as Europe's" (DECLARED) is a weak analogy: in Europe the count's CACE really is one figure. See L-2.

## 3. K19: the war chest

**Re-derived independently.** My perl script decodes the saved JSON, keeps `most_recent` reports and pro-rates by inclusive days inside 10 Sep to 4 Nov 2024. It gives:

| party | report | coverage | disbursements | days in window | share |
|---|---|---|---|---|---|
| DEM | OCTOBER MONTHLY | 1-30 Sep | $269,813,057.32 | 21 of 30 | |
| DEM | PRE-GENERAL (A) | 1-16 Oct | $165,857,675.15 | 16 of 16 | |
| DEM | POST-GENERAL (A) | 17 Oct-25 Nov | $277,057,928.27 | 19 of 40 | |
| **DEM** | **window** | | **$486,329,331.20** | | |
| REP | OCTOBER MONTHLY | 1-30 Sep | $77,579,173.28 | 21 of 30 | |
| REP | PRE-GENERAL (A) | 1-16 Oct | $99,689,404.21 | 16 of 16 | |
| REP | POST-GENERAL (A) | 17 Oct-25 Nov | $113,116,344.38 | 19 of 40 | |
| **REP** | **window** | | **$207,725,089.09** | | |
| **mean** | | | **$347,027,210.14** | | |

The generated constants are these, to the cent. Both responses have `pages` = 1, so nothing was paginated away. The superseded originals (amendment N) are correctly excluded by `most_recent`.

**C00828541 is Trump's committee.** Its 2024 history row reads `former_committee_name` "DONALD J. TRUMP FOR PRESIDENT 2024, INC.", `former_candidate_id` P80001571, `first_f1_date` 2022-11-15 (the announcement) and `convert_to_pac_flag` true. Every window report is a Form 3P; the two Form 3X reports are post-conversion and fall outside the window. The tool's own identity test is weaker than the evidence: a sponsored leadership PAC would also list the candidate id. See L-3.

**The chest is generated, not transcribed.** `LiveCampaignSetup.UsWarChest` is `=> Generated.UsCampaignSpending.WarChestUsd`. `UsCampaignSpendingCheck` derives it again from the saved responses, holds the window to `CampaignCalendar.FromWorldStart(...)`, compares the CSV's digest over LF text and has two teeth. The act prices stay the model's table read in dollars, [AUTHORED-DRAFT], with a unit price BILLED. That is declared on the note and on the HQ line.

## 4. K72: the sourced scale

**The anchors hold.**
- 26 U.S.C. §1, before (f): the top bracket is 39.6. In 1(j) it is 37, and the check also asserts no 39.6 inside 1(j).
- §11(b): "shall be 21 percent of taxable income".
- House roll 699 of 2017: H R 1, "On Motion to Concur in the Senate Amendment", passed, Democrats 0 yea. This is the final House vote.
- Senate vote 323 (115-1): "Motion to Recede ... and Concur with Further Amendment". The parties split whole.
- The Democratic platform's 28 % sentence is found verbatim in the American Presidency Project's copy (the party's own PDF is BILLED).
- I searched that platform for an income-tax top rate. It states none (only the 25 % billionaire minimum), so reading DEM's 39.6 from the roll call is the declared fallback, not a missed platform figure.

**The translation is as declared.** The seed is `BaselineTaxRates`, which only `WorldFactory` writes, so it is fixed at the seed. The game's line is seed × party / in-force:
- income: the effective seed 17.10 gives DEM 18.30 and REP 17.10;
- corporate: the seed 21 gives DEM 28 and REP 21.

The line runs through (pos_REP, rate_REP) and (pos_DEM, rate_DEM). `ScaleAt` is unbounded and `RateAt` clamps the position to 0-10 and the rate to the dial.

**Only the income and corporate lines move.** `TryUsScale` keys on `Generated.UsRateAnchors.Lines`. UsRateAnchorCheck proves that payroll and Sweden's income tax keep the dial's map.

**"Only for the player's Congress" holds by the call graph, not by a guard.**
- `TryUsScale` tests only `country.Id == USA`.
- Its readers are `RatesOf`, which `GetBudgetBillConcern` reaches under `SpatialBudgetVote.Applies`, and `UsMajorityReconciliation.TryPartyRate`.
- Every caller that votes a rate move is on the player's day tick (`AdvanceBudgetBillDay` and `AdvanceMajorityReconciliationDay` from `AdvanceCountryDayTick`) or a UI preview.
- The only path in a no-player world is the debt-limit raise, and it moves no rate (section 6).

See L-4.

**The consequences are computed correctly, and both are reproduced by hand.** GPS 2019 gives REP 8.23 and DEM 3.73.
- **Income line:** 3.745 scale units per point, against the dial's 10/70 = 0.143, about 26 times steeper.
- **Corporate line:** 0.643 units per point, against the dial's 0.2.
- **DEM against the 70 % ceiling** (the (b) case): DEM's own rate is 18.30, and the ceiling lies far past it, so DEM reads AGAINST. True, and the diagnostic asserts it.
- More broadly, DEM reads AGAINST any income-tax rise of more than about 2.4 points from the seed's 17.10: twice the 1.2-point gap to its own rate.
- **REP has no book:** REP's rate equals the law in force, exactly. `(float)(seed*37.0/37.0)` is exact in double, so its distance from the standing law is 0.

**The desk worlds' plant is legitimate for testing the mechanism. It does weaken what the bar can see.**
- `DemPresidentWorld` now sets the standing income rate to (DEM+REP)/2.
- So (b)'s "a cut halfway toward REP's rate reads +1 for REP" and (c)'s REP-majority book exist only on that planted world.
- On the real seed, every pure income-tax or corporate-tax cut reads **-1 for REP** on term 1. The bill loads only Redistribution, and Alignment = (0 - d)/d.
- A Republican majority facing a Democratic president never tables a reconciliation while the law stands at the TCJA. US-23's F8 behaviour is then inert on the default world.
- The comment declares the no-book half. The "REP against every cut" half is stated nowhere, and no check prints either half on the unplanted world. See M-2.

## 5. Save format 63

`CurrentSaveVersion = 63`. The loader refuses any `version != CurrentSaveVersion` with the plain Ruling A message (SaveGameService.cs:228-233), so a v62 save is refused, which K8 requires. No field is added. The comment's reasons, one per ruling (K14, K18, K19), are accurate. wave/fed also takes 63, so the integrator renumbers one of them. This branch's step needs nothing else, because the refusal is a plain inequality.

## 6. The no-policy dump: it stays byte-identical

My argument, path by path:
- **K14 / K18 (the US count):** `SeatsUp`, `CountSenate`, `UsGroundGame.Effects`/`NoCampaign` and the midterm count are reached only through the presidential/midterm hold, which returns at once unless `PlayerCountryId` is a US-presidential country (SimulationManager.cs ~5771). The dump seats no player.
- **K18 / K19 (the staging):** `LiveCampaignSetup.TryFor`, `PreCampaignRun.Begin` and the AI's door value are reached only from the player's campaign (SimulationManager.cs 5937/5992/6009, all on `PlayerCountryId.Value`). No AI country stages a campaign.
- **K72:**
  - `UsMajorityReconciliation.Due`/`Book` run from `AdvanceMajorityReconciliationDay`, called only from `AdvanceCountryDayTick` and `ActOnDocketAnswers`, both the player's.
  - The one Congress vote in the dump is the AI's debt-limit raise (`UsDebtLimit.ConcernOf` to `GetBudgetBillConcern` to `RatesOf`). That bill moves no rate, so `RatesOf` skips every line at `PointsOf(...) ≈ 0` before any `ScaleAt` is evaluated, and `concern.Spatial` stays null as before.
- **Caveat:** `NoPolicyCentury.For(CountryId.USA)`, if any diagnostic asks for it, is a PLAYER world (`sim.PlayerCountryId = player`). It can move: the 2026 specials, and the 5b no-campaign count's rounding (M-3). That is not the sentinel's dump.

## Findings

### Medium

**M-1. US-31's PLAN_PREREG is split across two functions. "Able to flip it" now runs on 5b and may fail.**
- **Where:** `Assets/Editor/UsCampaignDiagnostic.cs` ~560-614, and `Assets/Scripts/Elections/UsGroundGame.cs` `Effects` / `EffectsOnCurve`.
- **What happens:** "Measured amount" is held on `EffectsOnCurve`. "Able to flip it" goes "through the count's own path", which is `UsPresidentialElection.Count` and so `Effects` (5b) since K18.
- **Why it may fail:** At 5b's ceiling a margin moves by about 0.5·c/b points:
  - about 1.1 points in a 50-70 % state (c = 1.4);
  - at most about 0.43 points in a state at or above 70 % turnout over CVAP (c = 0.6). Minnesota, Wisconsin, New Hampshire, Maine and Michigan are all in that band.

  So the done-when check passes only if the noisy map's closest winner-take-all state sits within that distance. The §898 pass was at 2.90 to 9.00.
- **Failure scenario:** The integrator's run prints "the call DOES NOT FLIP" and UsCampaignDiagnostic fails. This branch could not run it, and its commit message does not warn of it.
- **What to do:** Either way, record that K18 changed the registered outcome of US-31's done-when for play. "Measured amount" is true only of a function no count calls. Ask Elias for 5b's pass rule, as the code comment says, rather than re-registering or tuning it. Also, the "planted tooth" (`idleMost < cardLow`) compares a 5b sweep against the curve's 2.90. Under 5b it bites trivially (L-5).

**M-2. K72's real-world consequence is planted around and never printed.**
- **Where:** `Assets/Editor/UsMajorityReconciliationDiagnostic.cs` (the `DemPresidentWorld` plant, line 243), `Assets/Scripts/Simulation/SpatialBudgetVote.cs` `TryUsScale`, `Assets/Scripts/Simulation/UsMajorityReconciliation.cs` `Book`.
- **What happens on the default seed:**
  - (i) REP's position stands for exactly the law in force on both anchored lines. Every income or corporate cut reads Alignment -1 for REP. A REP player-president's own caucus is maximally against a pure tax cut on term 1. Only the cohesion and opposition terms can carry it.
  - (ii) `Book(REP)` returns null: "already stand at the party's position". The F8 out-party reconciliation never fires for a Republican majority until the law moves.
  - (iii) DEM reads AGAINST any income-tax rise over about 2.4 points.
- **Why it matters:** Each follows from the declared anchors, and none is a code error. But only (ii) is declared, in a diagnostic comment, and the bar asserts all three only on a planted law.
- **What to do:** Add a printed, unplanted line to (b) or (c) on the seed world: REP's alignment on a 1-point cut, and REP's `Book` null with its reason. Put (i)-(iii) to Elias as K72's consequences: the 2025 OBBBA's further cuts are exactly what this scale makes Republicans oppose. Then a reader can see the behaviour the ruling produced.

**M-3. The played US world's count moves numerically with no campaign at all, and the branch declares no family.**
- **Where:** `Assets/Scripts/Elections/UsGroundGame.cs` `Effects` (via `NoCampaign`, SimulationManager.cs ~5793).
- **What happens:** On every US polling day no campaign ran up to, a played world (or a counterfactual or shadow fork of it) is counted through the 5b overload. Before, it went through the curve overload.
- **Why it differs:** With zero contacts the shares cancel the base mathematically. But the votes are `Math.Round(noisy/kept * regionCast)` on a per-state `regionCast`, where before they were `Math.Round(noisy * regionCast)` on the national base. So every state's effect moves at rounding level.
- **Also moving:** K14 adds two Senate seats to every played 2026 midterm, and so to the 2027+ Senate and every vote it takes.
- **Failure scenario:** A US-player family, an `elec` backtest or a film's numbers move, and the integrator reads it as unexplained.
- **What to do:** Name it in the draft records as an expected family for played US worlds (not the sentinel).

### Low

- **L-1. The special's winner sits from 3 January** (`UsCongressElection.cs` class doc, DECLARED). In fact a winner is seated on certification, so the appointee of record holds about six extra weeks of votes. This is acceptable as declared. List it on the play-calibration or known-deviation list.
- **L-2. The AI's door value reads the national band** (1.4 at about .65). The count reads each state's own: 0.6 at or above .70 (`LiveCampaignSetup.cs`, the `doorCacePp:` line). So the AI overvalues doors in the high-turnout states by about 2.3 times against the count's own arithmetic. That is the property GroundGameCountDiagnostic (5) asserts for Europe. Either per-state or declared as a known divergence. The analogy "as Europe's" does not hold, because Europe's count CACE is uniform.
- **L-3. `us_campaign_spending_prep.pl` identifies the committee by `candidate_ids` alone,** plus Form 3P on the window reports. Assert `former_committee_name` and `former_candidate_id` from the saved history as well. They are present, and they settle the committee's conversion to NEVER SURRENDER, INC.
- **L-4. `SpatialBudgetVote.TryUsScale` has no player or Congress guard of its own.** Its "only the player's Congress" property rests on its callers. A future AI-country budget vote through `GetBudgetBillConcern` would inherit the sourced scale silently. The ruling says "US only", which is met, but say so on the method.
- **L-5. UsCampaignDiagnostic (m)'s planted tooth compares a 5b sweep with the curve's card low (2.90).** Under 5b the widest no-contact move is near 0 and every 5b ceiling is under 2.90, so the tooth no longer tests the 5b assertion beside it.
- **L-6. A new `<see cref="EffectsOnCurve"/>`** in `UsGroundGame.cs`'s `Campaign` summary (line 56). The brief says no new `<see cref`. Make it a backticked name, as elsewhere.
- **L-7. `wayback_fldos_offices_up_for_election_20261009194659.html` is saved and listed in SHA256SUMS, but the generator never reads it.** It is not in `SenateRawSources`. That is harmless, but the fetch log should say it is held as corroboration only.
- **L-8. The DEM income anchor (39.6) is a roll-call inference: "every Democrat against".** It rests on a 2017 vote, while the 2024 nominee's own budget position (the $400k restoration) is in no saved source. It is declared; noted for Elias beside M-2.

## What a passing named run must print (owed to the integrator)

- `US CAMPAIGN SPENDING: PASS`; `US RATE ANCHORS: PASS`; GeneratedCatalogCheck with "K14's scheduled specials FL/3 2026-11-03, OH/3 2026-11-03".
- UsCampaignGeographyCheck: the "K18: the re-cut does not change US-9's registered outcome" line, `ok`.
- UsCampaignDiagnostic: (f) on the K19 chest $347,027,210.14; (m)'s registered line `ok`; and the "able to flip it" line. If that last line FAILS, the cause is M-1, a change to a registered outcome, and it goes to Elias.
- `US MIDTERM: PASS` with (k) FL/3 and OH/3, both elected and in brass.
- UsMajorityReconciliationDiagnostic (b)/(c) on the planted world, with M-2's unplanted readings added.
- TrajectorySentinelCheck unchanged (section 6). PreviewParityDiagnostic unchanged, since no `Country` field is added.

Note on `us_senate_races_mutations.sh`: neither the races tool nor its inputs change on this branch, so the run is a sanity control only: **37 caught, 0 missed or broken**, the control passing and reproducing the repository's bytes (7 m 35 s).
