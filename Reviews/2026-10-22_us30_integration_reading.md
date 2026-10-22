# US-30 on main after US-29: the integrator's reading of the merge (2026-10-22, s894)

**Reviewed:** branch `wave/us30` at a8e25007 (rebased on §892 0244e796 as 31b85b8f..7092ce2b, then the rebase review's fixes a8e25007), squash-merged onto main a7eeddf3 (§893, US-29), which moved `Assets/Scripts/Simulation/SimulationManager.cs` after the branch's base. The rebase review (`Reviews/2026-10-22_us30_rebase_review.md`, verdict SHIP, Lows only) read the money path as the branch holds it, on 0244e796. This reading covers only what the merge onto a7eeddf3 could change, so that the review ledger's row for the money path names a reading of the state being committed.

**Files reviewed:**
- `Assets/Scripts/Simulation/SimulationManager.cs`
- `Assets/Scripts/Persistence/SaveGameService.cs`
- `Assets/Scripts/Elections/LiveCampaignSetup.cs`

## Method

For each file, the set of changed lines (`+`/`-` lines, the file headers excluded) of `git diff 0244e796 wave/us30 -- <file>` was compared with that of the staged squash (`git diff --cached -- <file>` on a7eeddf3), by digest. All three are identical: the merge applied the branch's own change line for line and introduced nothing of its own. Git merged `SimulationManager.cs` with no conflict.

## Where the two items meet in SimulationManager.cs

US-29 (§893) changed the Fed chair's consent path: `ConsentsToAiNominee` and its neighbours in the central-bank appointment region (the term's day, the AI president's keyed nominee, the hold, the log days). US-30 changes the US presidential count and the campaign stagings: `HoldUsPresidentialCount` (the campaign's final shares first, `TryPlayerCampaignShares`, then the prediction with `played:`), the openings of the run-up and the campaign, and `RestoreCampaign`. The staged hunks sit in the elections region only (the hunk headers read four ranges between the count and the restore); none touches the appointment region, and neither item reads a field the other writes:

- US-29's consent count reads the seated Senate (`SecondChamberSeats`) or the Senate of record by date, and the president of the day. US-30 writes neither: its count writes the presidential and Congress contests through the existing `UsPresidentialElection.Hold` and the Congress count, whose seating on 3 January and oath on 20 January were already on main before both items and are unchanged.
- US-30's stagings read `ElectionHistory`/the Congress contests (`PartySystems.OwnCounts`) and the campaign record. US-29 writes neither.
- Neither adds a field to the save: US-29 adds no state, and US-30's format step (51) is a refusal of v50 saves with no field added. The two do not compose in `SaveGameService`.

So the merged file's behaviour is the union of the two reviewed changes, each as its own review read it.

## What the bars must still show

The no-policy dump holds no US player and no campaign, so the trajectory sentinel must hold on `aloopsal2` (US-29 was measured byte-identical on both seeds at a7eeddf3; US-30's count returns early without a player country). The named run must show `UsCampaignDiagnostic` clean, including (h) the done-when and (j) the 2028 staging on the history rolled forward, and `HistoryRolledForwardDiagnostic` (a) bit for bit for the USA's staging as well as Sweden's and Germany's, plus `FedChairConsentDiagnostic` still clean beside it.

## Verdict

No defect from the merge. The ledger row for `SimulationManager.cs` at the merged state rests on the rebase review for US-30's change, the US-29 reviews (§893) for the other, and this reading for their union.
