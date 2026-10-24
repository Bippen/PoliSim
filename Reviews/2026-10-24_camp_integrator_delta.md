# wave/camp: the integrator's reading of the money-path delta after the second review (2026-10-24)

**Why this exists.** `Reviews/2026-10-24_camp_review2.md` (independent, NOT READY: H1 the absorption's weights cancelling the shares, M1 an attack lifting idle parties, M2 K88's net missing; L1-L10) reviewed `wave/camp` at 1bd70568. The fix round b9322df2 followed: Elias ruled H1 (K91, final: the equal losses kept after the share-aware form was proved impossible) and M1 (K92: an attack's loss goes to the attacker only), and the Mediums and Lows were fixed. Of the money paths, only `SimulationManager.cs` changed after the review, and the recipe asks the integrator to read that delta before the ledger row is written.

**Files reviewed:** `Assets/Scripts/Simulation/SimulationManager.cs` (`git diff 1bd70568 b9322df2`: one added line at the campaign's close), `Assets/Scripts/Elections/CampaignPersuasion.cs` (the corrected comments and the attack path, for context), `Assets/Scripts/Elections/CampaignRun.cs` (where `AbsorptionFallback` is set), `Assets/Editor/CampaignPersuasionDiagnostic.cs` (the closed-form check and the fallback check).

**What the delta does in the money path.** One line: `if (PlayerCampaignResult.AbsorptionFallback != null) { Debug.Log("CAMPAIGN: " + PlayerCampaignResult.AbsorptionFallback); }` - when the balanced absorption cannot be met and the run falls back to the proportional form (review 2's L2), the game names it at the campaign's close. It reads a string the run already holds and writes a log line; it books nothing, draws no random number and changes no figure. The sentinel cannot move through it: no campaign is staged in the no-policy dump (the merged tree's `dump908` was byte-identical to `us24dl` before this line, and the line adds no path the day loop reaches).

**The merged state.** On main this file also carries §906's date stores and §907's debt-limit lines (each read in its own review and delta reading); the three-way apply of 1bd70568..b9322df2 placed this one line without conflict.

**Verdict:** the delta is a log line on a declared fallback; READY subject to the named run, the harnesses and both bars on the merged tree.
