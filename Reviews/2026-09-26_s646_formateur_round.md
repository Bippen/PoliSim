# Review — §646 the formateur's evaluator and the Speaker's round (2026-09-26)

Independent readers, read-only, on the staged diff: `Assets/Scripts/Elections/CoalitionFormation.cs` (`Form` split into `Prepare` and
`Evaluate`; `SupportRefusal`, `SharedSupport`, `Holding`), `FormationProposal.cs` (new: the proposal, the answers, `Formateur.Answer`),
`SpeakerRound.cs` (new), `GovernmentRecord.cs` (`Round`, `GamsonPosts`, `FromProposal`), `SupportAgreement.cs` (`KeyOf`),
`Assets/Scripts/Simulation/SimulationManager.cs` (the round's engine; the discharge opens a round; §640's resumption replaced), `GameController.cs`
(election night installs nothing where the round runs; `FormationHolds`; `DrawSpeakerRound`), `SaveGameService.cs` (format 35), and the checks
(`FormationSweepDiagnostic`, `FormateurDiagnostic`, `SpeakerRoundDiagnostic` new; `ConfidenceDiagnostic`, `AiMotionReachDiagnostic`,
`ChamberVerdictCacheCheck`, the suite).

## The evaluator, first reading (before the round)

The refactor is exact - proved by the sweep's digest, identical on the code before the split and after it, now pinned. Found and answered:
the cheap bar red on the who-governs guard's false positives (the evaluation's own `Support` field renamed; the Gamson allocation moved into
`GovernmentRecord`) and on `UnwiredSubsystemCheck` (the evaluator's types reached by no game code - **it lands with the round, its first
caller**); supporters had no walk-out test (**given the partners'**); the posts factor unbounded (**capped at one: offering more buys nothing**);
`SupportRefusal` let refusing supporters count against each other (**split: the individual conditions, then `SharedSupport` among the survivors**);
demands matched by name (**by `KeyOf`**); the vintage defaulted (**required**); validation; the sweep's file handling; stale comments.
**Stated:** the sweep prints each blocked cabinet's line by its two parties, not its basis (the pinned text is the proof; widening it would move
the pin for no formation change).

## The round, first reading

1. **defect D1** — a round open when an election is held was never replaced. **Fixed:** the day after an election the open round ends and the
   election's round opens; tested (`SpeakerRoundDiagnostic` (4)).
2. **defect D2** — the desk's draft cache keyed on the turn alone. **Fixed:** on the round and the turn.
3. **defect D3** — a supporter's demands re-derived on the vote day could flip its acceptance. **Fixed:** the tabled demands frozen on the proposal
   (drafted or submitted) and read by the vote and the installed government; tested (a demand enacted during the wait).
4. **defect D4** — the player's party answered by the model after accepting an offer. **Fixed:** the player's party answers by the player.
5. **risk** — the election night board still describes a formed government. **Stated:** premise 9's commit (§648) cuts the board; until then the
   foot's verdict is the round's.
6. **risk (films)** — the film's warm-up election opens a round later in the film. **Checked:** the dry and the real film clean, the round
   consulting M with the film's S in opposition.
7. **risk** — contexts that drive the manager alone stall where the player is asked or offered a place. **Stated.**
8. **risk (premises)** — R2/R6 open the round the day after polling and vote on the eleventh day; the Riksdag of record convenes later and a prime
   minister is chosen after its opening. **Stated for Elias:** the convening day for an in-game election is not sourced on disk (RF 3:10 is not quoted).
9. **risk** — a pass where every holding government is the player's leads other parties to propose themselves alone, to rejections. **Stated.**
10. **defect (K-1f)** — the candidacy check unreachable, and S or M could sit under a non-candidate. **Fixed:** a declared-candidacy party refuses a
    cabinet it does not lead (K-1f's own premise); tested (`FormateurDiagnostic` (h)).
11. **risk (cost)** — `Evaluate` allocates per cabinet. **Measured:** the bars' times (the record).
12.-20. **nits** — `DrawConfidence`'s doc displaced, a doubled suite comment, a decline printing the motion's basis, stale docs (the confidence
    header, the discharge's doc, `ProcedureResumedAfter`, the diagnostic's class doc, the verdict's summary), an unread log, claim slips, an empty
    order, the draft read from the pre-defection list. **Fixed** each (the log now on the desk; the draft from `Holding`; `AskNext` breaks off
    with no party).
21.-23. **tests** — **added** the mid-round election, the tabled round's save, the demand drift, the pinned decline branch, (g)'s reason, K-1f;
    part 4's once-per-election guard restored.

## Second reading (on the fixes)

Every answer verified in the code (D1-D4, K-1f, `Holding` for the drafts as for the answers, the refactor's equivalence read line by line). Found:

1. **risk** — a declined offer was wired as a support-blocking line, which the evaluator counts as a vote AGAINST every proposal from that
   formateur; premise 5 says the party stays in opposition. **Fixed:** a decline keeps the player out of that formateur's cabinet and support,
   and its vote at the investiture is the model's, as any opposition party's; no line is added. (On the start's chamber M still cannot form
   without KD's seats: the Speaker moves on - `SpeakerRoundDiagnostic` (2).)
2. **risk (a regression)** — an ordinary election held after an extra election was ordered, and before it, opened no round: the government
   governed the new chamber undischarged until the extra election, and the night's line was false. **Fixed, a premise stated:** the election
   held opens the round and the order lapses; tested (5).
3. **risk** — the round's log lines unwrapped on the desk. **Fixed:** wrapped.
4. **nits** — the K-1f doc, the draft's "formation sheet" promised before the sheet exists, a one-way line reported as the refused party's
   refusal, the no-party break-off's log, a stale doc on the record. **Fixed.** Stated: the round's strings carry party keys (Sweden's keys are
   its short names); frozen dial demands keep the draft day's standing level; the sweep's pin was taken on the code before the split, which
   is what makes "reproduced exactly" true.

VERDICT (second reading): no blocking defect; the two risks fixed.
