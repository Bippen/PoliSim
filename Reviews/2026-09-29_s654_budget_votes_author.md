# Review — §654 PS-3i-2b, the breach by budget votes and the author's vote (2026-09-29)

Reviewed: `Assets/Scripts/Simulation/SimulationManager.cs` (the §654 diff: `CountBudgetVoteOnAgreements`, the budget day), with
`SupportAgreement.cs` (`BudgetVotesToBreak`, `AgreementItem.BudgetVotesOwed`, `CountBudgetVote`, `Demand` clearing the author), `StanceModel.cs`
(`BillConcern.Author`, the author's vote first in the per-party loop), `ParliamentSystem.cs` (`Authored`, every bill concern's author),
`ChamberVerdicts.cs` (the author in the key), `SaveGameService.cs` (format 37) and `SupportAgreementDiagnostic`. One independent reading,
read-only. Verdict READY WITH FIXES; every fix taken.

## Defect, fixed

- **Two verdict rows read the vote without its author**: the tax and welfare programme rows scored through the legacy direction (no author), while
  the vote itself carries the player's party as author - in a close chamber the row could read WOULD FAIL for a bill its author's seats carry.
  **Fixed**: both rows score the bill's own concern.

## Confirmed sound, stated

- **Every budget vote counts once**: the government's bill (the contest or the bill alone) and the player's bill both resolve on the budget day,
  and it counts once on each; a failed budget counts (a vote is a vote); the arrival window counts, being the same path. AI-governed countries'
  books have no budget vote and count nothing (the day tick is the player's country's).
- **Stated for calibration (Elias)**: an agreement formed after an election can meet the arrival budget and the fiscal-year one within a few
  months and break inside that window; a bill tabled by one government and resolved under the next counts against the new government's
  agreements, and a caretaker or an open round is not skipped; on a day holding a second budget vote, the budget counts before the day's tracker, so
  a dial delivered that same day is broken first.
- **The author's reach**: only the bill concerns carry an author, and their callers are real bills or previews of the player's own draft (which
  now agree with the vote); `SupportAgreement.Demand` clears it on the laws it scores and builds its dial concerns with none; no formation,
  motion, confidence or economic-vote path builds a bill concern. The author's forced vote enters the seat-weighted alignment at full weight, so
  the tie-break, the lean bar and the division's recorded alignment move with it - consistent with "votes for".
- **Money path: yes** - the author's vote can flip a bill's passage, and a passed bill books its money (`ApplyBillResult`); that is the ruling's
  consequence, and the sentinel's no-policy century seats no player, so no author reaches it.
- Save: the count is a public field, serialised and copied; format 37 refuses any other.

Minor, taken: two summaries stacked on the wrong members moved to their own; the verdict cache check's "member" copy carries the author.
