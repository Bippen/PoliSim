# Review — §647 the formation sheet (2026-09-29)

Two independent readers, read-only, on the uncommitted diff: `Assets/Scripts/UI/GameController.cs` (the sheet: `DrawFormationSheetStage`,
`FormationDraft`, `FormationVerdict`, `AutoOpenFormationSheet`; the desk's PlayerAsked case opening it), `Assets/Scripts/Testing/UiScreenshotDriver.cs`
(the staged frames), `Assets/Scripts/Simulation/SimulationManager.cs` (`PreviewFormation`, `HoldsTreasury`, `SubmitFormation`) and
`Assets/Editor/SpeakerRoundDiagnostic.cs` part (3).

## The first reading (the sheet and the film's staging)

Three defects confirmed, all fixed before the second reading:

1. **A control-count mismatch.** The rows mutated the draft inside the event that was still drawing the rows below them - ticking a supporter added its
   demand rows (and leaving a partner out added a support row) on a non-Layout event, which throws "Getting control N's position in a group with only N
   controls" and leaves the scroll view unbalanced. **Fixed:** a click is queued and applied after `EndVertical`; every row draws from the unchanged draft.
2. **The sheet's verdict was not the tabling's.** The sheet called `Formateur.Answer` with the round's refusals AND the player's declines turned into
   refusal lines, and no player's party; `SubmitFormation` reads the round's refusals only and names the player's party - so a decline could make the
   sheet show a refusal the tabling would not reach, or the reverse. **Fixed:** `SimulationManager.PreviewFormation` is the call the tabling makes;
   `SubmitFormation` calls it.
3. **The formateur could give away the Treasury or every post** - the prime minister's party left without the head of government's portfolio, which
   `AllocatePortfolios` never produces and the spec's §5.3 rules out. **Fixed:** the Treasury row and the formateur's last post draw fixed on the sheet;
   `SubmitFormation` refuses a proposal whose formateur does not hold the Treasury (`HoldsTreasury`).

Minor, taken: the tabled demands read from the proposal as frozen (`FreezeTabled` when a supporter is asked) instead of `SupportAgreement.Demand`
on every event; a post cycle with one cabinet party draws fixed (it re-formed the chamber for nothing); a refusal line is cleared by any revision.
Stated, not changed: a partner offered no post refuses only where its best alternative clears the formation's margin (premise 2: acceptance is
*lowered*), so the film's premise-3 check is a measurement of the film's chamber, not a law of the model; the driver's restore was read adequate
(after the warm-up election the government is already a caretaker, so `OpenSpeakerRound` adds no discharge, break or version bump).

## The second reading (`SimulationManager.cs`, the final diff)

Reviewed: `Assets/Scripts/Simulation/SimulationManager.cs` (the §647 diff: `PreviewFormation`, `HoldsTreasury`, `SubmitFormation`). **No defect.**
`PreviewFormation` computes what `SubmitFormation` computed before on every path that reaches it (the tabling's guards run first; the stage is checked
by the sheet's only caller); `FreezeTabled` is idempotent and every reader walks `Supporters` through `TabledOf`, so a stale frozen entry is inert and
`AcceptedDemands` keys come from the same frozen items; no AI proposal passes through `SubmitFormation`, every draft gives the formateur the Treasury
first, and every fixture that tables builds its proposal with `DraftProposal`. **Money path: no** - nothing here touches a budget, a tax or the book;
the row is owed because the file is on the ledger's path. Verdict READY.

Minor, taken on the reader's word: the preview now applies the Treasury rule itself (it could otherwise promise what the tabling refuses);
`HoldsTreasury` guards a null formateur; `SpeakerRoundDiagnostic` (3)'s "same answers" check compared a call with itself once the tabling called the
preview, and is replaced by the Treasury case asked of both the preview and the tabling.
