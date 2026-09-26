# Review — §644 PS-3i-2a, AI no-confidence motions reachable in play (2026-09-26)

Two independent readers, read-only, on the working-tree diff to `Assets/Scripts/Simulation/SimulationManager.cs` (`AiWithdrawals`,
`AdvanceConfidenceDay`, `TryAiMotion`, `AiMotionCandidates`, `TrackAgreements`' doc), `Assets/Scripts/Elections/SupportAgreement.cs`
(`BrokenShareTolerated`, `PastTolerance`), `Assets/Scripts/Elections/GovernmentFormation.cs` and `ConfidenceProcedure.cs` (an optional
date on the sitting round, the red-lined set and the vote), `Assets/Scripts/Elections/DeclaredRedLines.cs` (comments), and the checks
(`AiMotionReachDiagnostic` new, `ConfidenceDiagnostic` part 5, `SupportAgreementDiagnostic`, the suite's registration).

## First reading — on the version that read the dated declarations in the game

No code defect; two ruling-level risks and stale comments.

1. **risk (ruling-level)** — the round and the vote read the timeline on the day (`ForDate`), which brings in SD's refusal of the support
   role, scoped by SD's own words to the formation after the next election (K-1i open), and reverses §636's reviewed reading and §607's
   rule. **Measured on its word** (`AiMotionReachDiagnostic` (1)): without the election platforms' rules the round seats SD only from KD's
   September lift - so SD's refusal is exactly what made the April window. **Taken:** the dated reading is REVERTED in the game; the
   optional date stays as the measuring instrument only; the question is filed as PS-3i-2c.
2. **risk** — an extra election before 13 September 2026 forms on the previous chamber's vintage while a dated round would read the
   timeline. With the revert the mismatch is gone; the vintage fallback itself is pre-existing - **stated** in PS-3i-2c.
3. **ok** — `DeclarationsDate` in consistent states (moot after the revert; the helper removed).
4. **ok** — the start's discharge round unchanged in effect.
5. **ok** — daily tracking: `Track` returns only newly broken items; deliveries land on the day; nothing numeric reads item states;
   `EconomicVote` reads support membership, so an AI withdrawal moves the player's election - intended. AI countries tick daily only where a
   diagnostic ticks every country; no trajectory dump calls the day tick.
6. **nit** — `SupportAgreementDiagnostic`'s dial-delivery check had become vacuous (the daily tracker delivers the dial the day the barrier
   law applies). **Fixed:** asserted on a fresh owed copy - one short of the target stays owed, at the target it delivers.
7. **ok** — `AiWithdrawals`: ordering, no double withdrawal, the player's party excluded.
8. **ok / nit** — `TryAiMotion` after the withdraw-to-move step went; the re-tally guard dead. **Fixed:** removed.
9. **risk** — optional dates reopen the vintage path to a later caller. **Moot:** the game passes none by ruling (§607, §636).
10. **test** — the chain's breaks were not sized from the share; the "moves nothing while the round would not seat it" line printed its
    premise without asserting it. **Fixed:** part (3) sizes the breaks from the share; part (1) asserts the premise PS-3i-2c rests on.
11. **risk (overstatement)** — the diagnostic's summary claimed the breach was made through the verbs. **Fixed:** rewritten - (2) is a
    measurement, (3) stands the breach in and says so.
12. **nit** — stale comments and transcribed figures (`TrackAgreements`' doc, ConfidenceDiagnostic's §641 wording, the feature rows'
    measured figures, a count in `SupportAgreement`). **Fixed.**
13. **scope** — the TL-1 files in the working tree were not §644's. **Separated** (stashed while §644 was barred and committed).

## Second reading — on the final diff


1. **ok** — no game path passes a date; `DeclarationsDate` gone; CLAUDE.md untouched. **stale** — the suite's registration ("the dated
   round"), case (b)'s "on the day" and "in January", `ViewOfSitting`'s summary ("today's declarations"), no summary saying the date is for
   measuring only. **Fixed.**
2. **ok** — ordering, no double withdrawal, exclusions, the candidate order; from the start as M nothing can break (the dials move in play
   only by laws, and the crime-and-justice bill is retired from the desk), as S nothing can either; an older save carrying a broken item
   withdraws on its first day - the rule. **nit** — `AiWithdrawals` is not gated on the Riksdag's rules: a withdrawal elsewhere changes the
   support and no procedure follows - **stated**, consistent with the ruling's words.
3. **ok** — numeric inertness; the dumps byte-identical by construction (no dump ticks the day); daily tracking in every country a
   diagnostic ticks, state only. **risk (film)** — the driver stages a crime-and-justice bill with unset fields read as zero; if it passed
   on Sweden, SD's dial item would break and SD withdraw mid-film. **Checked:** neither Sweden dry film on the tree (`dryps3n`, `drytl1`)
   logs a withdrawal; **stated**.
4. **risk** — part (1) sampled too few dates and could miss a window (notably 2-5 September, KD's line against S standing before its SD
   lift). **Fixed:** sampled on every day a declaration starts or ends between the start and polling day, and whether the platformless
   round ever seats SD before the no-motion week is printed - it does not; the full timeline's window narrows to the days between M's lift
   and MP's rule. **test** — nothing pinned that the withdrawal reads the tolerance. **Fixed:** (b0, live) - one broken item within the
   share on a widened agreement, SD stays and moves nothing. **nit** — part (2) printed totals only. **Fixed:** each party's vote.
5. **rows** — accurate; the nits (the day's tracking is the player's country's; the breach is stood in; the retired crime bill; a derived
   relation written into a comment and the calibration entry). **Fixed.**

VERDICT (second reading): READY - no code defect; the dated reading is gone from the game; the fixes above taken.
