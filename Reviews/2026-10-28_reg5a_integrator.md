# wave/reg5a: the integrator's reading at the merge (2026-10-28)

**Why this exists.** Regional 5a (rulings K146-K149, unblocking K83) was built on `wave/reg5a` (abad7ac4).

Its path to this merge:
- `Reviews/2026-10-28_reg5a_review.md`: READY, with M1 (registration), M2 (the swing map's "too close to call" margin treating two shares of one poll as independent samples) and nine Lows.
- Reworked at 1d809333.
- `_review2.md`: READY with Lows.
- A final pass at 7ed9628b, after the integrator's first merged run found five failures in the new diagnostic.
- One more at c3e5f2c0, after the second run found the poll's charge read at the wrong time.

This file reads the merged state.

**Files reviewed:** `Assets/Scripts/Elections/CampaignPressureRegional.cs`, `Assets/Scripts/Elections/RegionalCampaign.cs`, `Assets/Scripts/Elections/CampaignRun.cs`, `Assets/Scripts/Elections/CampaignPersuasion.cs`, `Assets/Scripts/Elections/CampaignAi.cs`, `Assets/Scripts/Elections/CampaignActions.cs`, `Assets/Scripts/Elections/LiveCampaignSnapshot.cs`, `Assets/Scripts/Elections/LiveCampaignSetup.cs`, `Assets/Scripts/Elections/UsGroundGame.cs`, `Assets/Scripts/Elections/ParliamentaryCount.cs`, `Assets/Scripts/Elections/NationalElection.cs`, `Assets/Scripts/Elections/VoteAttribution.cs`, `Assets/Scripts/Elections/Generated/PolishOkregElectorates2023.cs`, `Assets/Scripts/Simulation/SimulationManager.PlayerElection.cs`, `Assets/Scripts/UI/GameController.Campaign.cs`, `Assets/Scripts/UI/SwingRegions.cs`, `Assets/Editor/RegionalCampaignDiagnostic.cs`, `Assets/Editor/CampaignPersuasionDiagnostic.cs`, `Assets/Editor/Gate18Diagnostic.cs`, `Assets/Editor/CheckSuite.cs`, `Tools/okreg_electorates_prep.pl`, `Tools/film_scope.tsv`, `docs/reference/US_ELECTIONS.md`.

**The merge.**
- `git merge --squash wave/reg5a` (c3e5f2c0) was made on 8718776a (§935 landed) and merged without conflict.
- Earlier merges of this branch were held twice in a stash while §934 and §935 were measured, then dropped and re-merged fresh from the branch.
- The changed-line sets of all 19 code files on the branch (`git diff 5c38ce1c wave/reg5a -- f`) and on main (`git diff HEAD -- f`), sorted with carriage returns stripped, are IDENTICAL.
- GATE OK.

**The integrator's changes.**
- Registered `RegionalCampaignDiagnostic` in the simulation batch after EnergyReliabilityDiagnostic, commented s936 (the review's M1).
- Regenerated the US model card's US-9 block with `UsCampaignGeographyCheck.WriteReadings`. Every campaign result moves by design (K147), so the card was stale.
- Added the film-scope row `drysreg4`.

**What the merged runs found, in order.**
- **The first run (7ed9628b's predecessor, 1d809333) failed five clauses.**
  - The count's "two votes" tolerance was wrong. A point laid on Stockholms län moved it +0.995 and every other region up to 0.004, because the swing's re-solve spreads part of the point. The clause now runs the re-solve's own arithmetic on the shifted priors, to two votes a region. A stated bound keeps at least 0.9 of the point in its region and moves no other region by a tenth.
  - Sweden's staging carried no valkrets offsets, so no regional poll was ever on sale. That caused the next three failures. **This was a real game bug.** The offsets were staged only off the 2022 vintage, but the chamber seated before the 2026 election IS 2022's, so a player in the real Swedish campaign could never buy a regional poll. Every staging now carries its regions' offsets: Sweden 29, Germany 16, the USA 51.
- **The second run's one failure** read "0 off the chest" for a bought poll. The poll was not free: `BuyRegionalPoll` debits the purse before it draws. The clause compared the twin runs' money at the campaign's close, where a rally every day runs both to the same end. It now steps the twins day by day and reads day 5's close.
  - On this tree: 60,000 off the chest against the twin, exactly the price.
  - The filed sample is 600, the staged party fields no pollster that day.
  - Stockholms län reads TOO CLOSE TO CALL on the one-sample lead formula, index 83.

**The measurement (this tree, s936).**
- **nreg4:** 11 of 11 clean.
  - RegionalCampaignDiagnostic PASS.
  - CampaignPersuasionDiagnostic, its tolerances unchanged.
  - UsCampaignGeographyCheck, on the regenerated card.
  - GermanCampaignDiagnostic, GroundGameCountDiagnostic, CreatedPartyCampaignDiagnostic, Gate18Diagnostic, PlaySheetCheck, UiLeverVerbCheck, ConstantProvenanceCheck and MetaTextCheck.
- **Gate18 passes,** with every list within its bound. It reports the stored record factors as DUE A REFIT, largest relative move 2.8e-2. That is expected, since every campaign result moves by design (K147). The gate reads the shares and refitting is its own step: recorded as a follow-on.
- **dump (`reg5a4`) against `mf28`:** BYTE-IDENTICAL on both seeds. No campaign is staged in the no-policy run, so the sentinel stays.
- **Films:** drysreg4 (Sweden, the USA and Germany at 1280) and realreg4 (Sweden 1280) are clean, with no OVERFLOW, CLIP or ESCAPE.
- **Bars:** simreg4 87 of 87. cheapreg4 has one failure, the ledger row for `SimulationManager.PlayerElection.cs` at c92cf880. That file is the money path that passes the regional shift to the count. Its 5 changed lines are the branch's, read in both reviews, and its row is added at s936 with this file as evidence.

**The campaign harness family `reg5a`.** The four campaign harnesses the implementer named (VoteAttributionHarness, CampaignAiHarness, ChainBandHarness, CampaignStrategyHarness) are not registered checks, and RunNamedBatch knows only registered ones. They are run by hand, and their declared digests are re-declared the next time each is run, with this explanation:
- local acts now saturate their own region;
- national acts are numerically equivalent;
- Germany's absorption is region by region (K235).

**Verdict:** READY. The merged state equals the reviewed branch line for line, and the no-policy dump is unmoved. The one real game bug the measurement found (no regional poll on sale in Sweden) is fixed at its cause. The poll is charged its price on the day it is bought.
