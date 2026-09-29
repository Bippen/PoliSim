# Review — §653 PS-3i-2c, which declarations a formation reads (2026-09-29)

Reviewed: `Assets/Scripts/Simulation/SimulationManager.cs` (the §653 diff: `RoundReading`, the round's reading, the motion's and the vote's day),
with `DeclaredRedLines.cs` (`DeclarationReading`, `HasTimeline`), `GovernmentFormation.cs` (`SittingPollingDay`, `SittingReading`, the reading
overloads of `ViewOf` / `Form` / `TryFormSeats` / `TryFormChamber`, `ViewOfSitting` and `RedLinedFrom` on a day), `FormationProposal.cs`
(`Formateur.ChamberOf` / `Answer` on a reading), `SpeakerRound.cs` (`ReadsOn`, `MidTerm`, save format 36), `GameController.cs` (election night on
`DeclarationReading.OfElection`), the film driver's staged round, and the checks (`SpeakerRoundDiagnostic` (6)-(7), `ConfidenceDiagnostic`,
`AiMotionReachDiagnostic`). One independent reading, read-only. Verdict READY WITH FIXES; every fix taken.

## Defects, fixed

1. **`SittingPollingDay` returned MinValue on a start's chamber with no election held in the game**: it asked `WorldClock.ElectionDayOf` for the
   `Seated` sentinel, which no chamber of record carries - a mid-term reading would have read no platforms at all. On today's Sweden start the
   two coincide (no platform is dated before 2025), which is why the checks passed. **Fixed**: the sentinel resolved to its chamber of record first.
2. **The check could not catch (1)**: part (6) compared the reading's platform day with the function's own answer. **Fixed**: it asserts the
   sitting polling day is 2022-09-11.
3. **The player's own motion projection read the vintage** while the vote it projects reads the day (`MoveNoConfidence`). **Fixed**: the
   Parliament tab's projection passes the day.
4. **Fixture text**: two basis strings carried a literal `\xC2\xA7` (C# reads it as other characters). **Fixed**: the section sign itself.

## Confirmed sound

Every game path that forms a government or weighs a round reads by the ruling (election night and the round after an election on the election's
own day; the discharge's round, the AI motion's successor and the confidence vote on today's mid-term reading); the start's chamber of record
(`AtStart`) reads its vintage, as it should. A mid-term round fixes its order at opening and answers on later days; the two can differ only
across a declaration dated inside the round (2 and 8 September 2026 on today's timeline) - stated, consistent with the Speaker's list fixed at
opening. The loader refuses any format but 36. The vintage paths are unchanged, so the formation sweep's pin stands. **Money path: no** -
nothing here books money; the choice of government can move, for Sweden, before 13 September only. The fixtures run under `EpochScope`; the
year-32 fixtures open on 1 October 2026 and install Kristersson's government through `GovernmentRecord.FromProposal`.

## Stated, not changed

`ConfidenceDiagnostic`'s (e+) positive control is gone - before the 2026 election no day outside the no-motion week seats SD under the ruled
reading - and its role is (c)'s, after the election; (e) still asserts both its premises on 8 September. `TryFormSeats`'s `asOf` fallback
(`DeclarationReading.AllOn`, everything on one day) now serves only the measuring instrument.
