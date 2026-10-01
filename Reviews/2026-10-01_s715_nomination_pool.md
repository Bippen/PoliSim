# Review — §715 the nomination pool: any qualifying party; the President's order phase 1's alone (2026-10-01)

Reviewed: `git diff HEAD`, against HEAD e894a20. The change is staged, not unstaged. Both files were read: `Assets/Scripts/Simulation/SimulationManager.cs` and `Assets/Editor/GermanFormationDiagnostic.cs`. In the staged `SimulationManager.cs` I read:
- `RoundsApply`, `SpeakerOrder` and `PmOf` 2915-2965;
- `OpenSpeakerRound` 3034-3066 and `AskNext` 3091-3125;
- `PersonBallot`'s leader 3255-3262;
- `PluralityBallot` 3395-3440, its doc included;
- `Tally` 3520-3570 and `Nominated` 3570-3625;
- `PassFormation`, `SubmitFormation` (`PlayerStood`) and `AnswerOffer` 3760-3795.

Also read:
- `PartySystem.cs` 264 (`HasPosition`) and 385-397 (Germany's parties);
- every remaining reader of a round's `Order` (scripts, UI and the film driver);
- `ConfidenceProcedure.ConstructiveVote` (from §713's review), for question 2;
- the log `n715c.log` (4 of 4 clean; e2 at 1615-1623).

One independent reading, read-only; Unity not run (a bar is running). **Verdict READY WITH ONE DECISION** (defect 1), the rest notes.

## Defects

1. **A player's party is kept out of a final-ballot nomination nobody ever offered it.** `Nominated` still skips the player's party in the final ballot unless `round.PlayerStood` (3600). That is set only when the player tables (`SubmitFormation`). §705 made it a consent rule: "a pass is not a candidacy". §715 now gives every seated party that qualifies a nomination, but a player's party is asked only in two cases:
   - in phase 1, if it is first in the Bundespräsident's order (phase 1 ends at that party's ballot);
   - in phase 2, if its own nomination reaches a quarter.

   **Failing scenario:** (e2)'s chamber with the player playing the Linke. The Linke is outside the order and never reaches a quarter, so it is never asked, never stands, and is left out of "AfD, SPD, Grune, Linke". The AI Linke on the same chamber is in it (n715c:1620).
   - **The asymmetry already existed** (since §705) for a party in the order that phase 1 never reached; §715 widens it to every seated party. "Any qualifying quarter may nominate" now holds for every party but the player's.
   - **Fix (a design choice, state it):** either ask the player's party before the final ballot whether it nominates where it qualifies, which keeps §705's consent rule; or exclude it only where it passed (a `PlayerPassed` flag set by `PassFormation` in any phase), so a party never asked nominates as the AI's do.

**Notes:**
- **Text still naming the order.** `PluralityBallot`'s doc reads "the most votes elect, a tie to the earlier in the order" (3420). The candidates now come in the pool's order, so a tie goes to the larger Fraktion, then the earlier party in the chamber. `AnswerOffer`'s log, *"… cannot form without X's seats - the next party in the order stands its candidate"* (3789), is wrong in phase 2. That log line reaches the player through the round's slip, which shows the last four log lines. `ParliamentRows.cs:212`'s doc is stale too.
- **(Not §715's.)** The final ballot's log says "the Bundespräsident has seven days to appoint him" when the elected candidate is Weidel (n715c:1621); "the elected candidate" would do.

## The questions asked

1. **No path still reads the order for later nominations or the final ballot.** `round.Order` is read only by `AskNext`'s opening assignment (3095), which is phase 1's ask and, in phase 2 or later, only the non-empty gate before the block replaces it. It is also printed in `OpenSpeakerRound`'s log and used by the film driver's phase-1 staging (`UiScreenshotDriver.cs:1010`).
   - **Later phases:** nominations (`Nominated`), the fourteen days' candidates (`FourteenDaysCandidates`), the sheet's projection and the final ballot all read the new pool.
   - **The text above** is the only remainder.
2. **The SSW skip can't change phase 1, Sweden or Art. 67.**
   - **Where `Tally` runs:** only from Bundestag paths: `PersonBallot` (phases 1 and 2), `PluralityBallot`, `Nominated`'s signing count and `ProjectPersonBallot`.
   - **Sweden:** the Riksdag goes through `Investiture` alone and never reaches `Tally`.
   - **Art. 67:** counted by `ConfidenceProcedure.ConstructiveVote` from the investiture, not `Tally`.
   - **Phase 1:** its one candidate comes from `SpeakerOrder`: the formation's PM party, a declared candidacy, or the largest party. The SSW, Germany's only party with no surveyed position (BSW and FDP have positions), can be none of these.
   - **The final ballot:** the SSW's nominee appears only under Abs. 3's "every member may nominate" fallback (its 1 seat is below 5 %, so it is not among the Fraktion nominations). There it now draws its own vote, as it should.
   - **Before §715** the SSW was never in a pool, so the bug the skip fixes could not fire. The skip matches the SSW's own abstention ("holds no surveyed position").
   - **Note:** in a one-candidate ballot every party that doesn't refuse the candidate votes for it, whatever the compatibility. That is §713's rule. The skip makes a positionless nominee draw only own, bound and group votes in any ballot, which is coherent; the record should state it as a premise.
3. **The pool's order matters in three places, and nowhere wrongly.**
   - **`PersonBallot`'s leader:** a tie to the first candidate cannot decide anything, since two candidates cannot both hold a majority.
   - **`PluralityBallot`'s winner:** a tie goes to the first candidate, now the larger Fraktion, then the earlier in the chamber. The old order was by size too (CDU, AfD, SPD, Grüne), so the effect is the same; only the doc line is stale.
   - **The elimination's "a tie to the later":** among equally short nominations, the smaller Fraktion falls first.
   - **And one new place, `AskNext`:** after the player, `standing[0]`, the largest standing Fraktion, is asked. That decides the formateur whose coalition binds in the one ballot (§712's notes A and B), so the largest nomination now always holds that role. Under the old order it rotated. That fits "any qualifying quarter may nominate"; state it.

## Confirmed sound, stated

- **The pool matches the text.** It is every party seated (`SeatsOf > 0`), a parliamentary group standing one (its larger member's), with GO-BT § 4's thresholds unchanged. The order applies to phase 1 alone ("The President's order applies only to phase 1"). (e2) proves a party outside the order nominating: the Linke, 64, a Fraktion.
- **`AskNext` in phase 2 or later:** the player first where its nomination stands, else the first standing nomination, else the no-nomination state (`standing` empty gives `Asked` null). The removed order loop and `tries` leave nothing dangling. `Turn` still increments, so the sheet's per-turn draft refreshes. Stood and passed parties are still removed first.
- **Outcomes are unchanged where the order already held every qualifying party.** In (e), the SPD's and the Linke's members still pool behind Habeck one nomination at a time; a Linke candidate in the pool falls before it can split them, and the SSW falls first.
- **Nothing else moves.** The sweep digest 86fbf292… is unchanged, and SpeakerRound, Confidence and German checks are clean (n715c). No random stream is drawn, no saved field is added, and the cost is negligible: about eight tallies over seven parties.

**Money path:** yes by name (`SimulationManager`), so the ledger wants this review's row. No arithmetic is touched.

**Verdict: READY WITH ONE DECISION.** Defect 1, the player's party and the final ballot, is either a short fix (a pass flag) or a prompt before the final ballot; whichever is chosen goes in the record. The stale order wording in `PluralityBallot`'s doc and `AnswerOffer`'s log is a one-line fix each.

## The author's note (2026-10-01)

- **The decision, taken (the second of the two fixes offered):** only an actual pass keeps the player's party off the ballot the most votes win - `SpeakerRound.PlayerPassed` (saved; an older round loads false), set by `PassFormation`; a player's party never asked stands there as an AI party in its seat would ("any qualifying quarter may nominate"). Checked: (e2)'s CDU, which passed, stands no candidate; new (e6) - the Linke, the player's, on a planted chamber where no party reaches a quarter and every party refuses every other, is never asked and stands among the Abs. 3 nominations (AfD, CDU, SPD, Grune, Linke, BSW).
- **Stale wording, fixed:** the final ballot's tie "to the larger Fraktion" (the pool's order); the decline log phase-aware (*the Bundespräsident proposes the next party's candidate* / *the next nomination standing is asked*); the final ballot's log says "appoint the candidate".
- **Stated in COMPLETED §715:** `AskNext` now always asks the largest standing Fraktion first after the player, so it is always that party's coalition the one ballot binds.
