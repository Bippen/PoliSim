# Review - §818, the gameplay audit's Phase 4: GA-1-3, the shadow hands back the real game's stream objects (2026-10-14)

Two read-only agents. **The investigation** (before any code, Elias's decision "investigate, then fix what is wrong") found the cause; its findings are in the record (§818), checked in the code (`SimulationRandom.RestoreState` builds new objects; `ShadowBaseline` restored the real generator with it) and the saves. **The review** read the fix in the tree - `Assets/Scripts/Simulation/SimulationRandom.cs` (`LiveState`, `CaptureLive`, `Reinstate`) and `Assets/Scripts/Simulation/ShadowBaseline.cs` (the real game's live state taken and put back; the shadow's own kept as a live state) - and was asked to break the equivalence for streams nothing holds, the aliasing between the real game and a shadow, the shadow's readings, saves and loads, the bars and the comments. It edited nothing and ran nothing that writes. Its report follows verbatim, then what was done.

## The report

0 HIGH / 1 MEDIUM / 4 LOW. The fix is correct. Non-holder streams draw the same numbers in the same places, no path lets the real game and a shadow share a stream object, and saves now record live counts. What's missing is a check that would catch the fix being undone.

**Checked and correct:**
- **Equivalence (item 1).** Every system draws through `RandomSource => SimulationRandom.For(...)` and caches nothing. The only holders are `CampaignRun.State` (CampaignRun.cs:313, 494) and `PreCampaignRun.State.PollRandom` (PreCampaignRun.cs:94, 250). The old rebuilds fast-forwarded to the same counts, so the next draw was the same number (grep finds no NextBytes and no literal int.MinValue/MaxValue ranges). `CaptureLive` reads the `MasterSeed` getter once, exactly as `int realSeed = SimulationRandom.MasterSeed` did. In the initializer, `MasterSeed = MasterSeed` binds the right-hand side to the static getter. One assembly with no asmdef, so the internal fields compile.
- **Aliasing (item 2).** `RestoreInto` calls `RestoreState` before anything can create a stream (SaveGameService.cs:243-248), and otherwise the constructor throws, so `_own` never holds a real object. `Reinstate` copies from a separate dictionary, so it never clears its own source. Every path puts the real state back in a `finally`. Ledger forks and turns run one after another (PolicyImpactLedger.cs:137-148), and no simulation code builds a shadow, so nothing nests. Each fork's `RestoreState` builds its own objects. A stream first made during a shadow run is dropped, as before.
- **Shadow readings (item 3) cannot move.** A shadow's campaign or run-up feeds nothing in its world: the result is read only by the controller's election (GameController.cs:6646). `LinesFor` and `EconomyState` are therefore unchanged. Nothing reflects on `_masterSeed`/`_drawCounts`; ShadowBaselineDiagnostic reflects only `_sim`.
- **Saves and loads (item 4).** In every runtime path the holders now are the table's objects: on load, `RestoreState` plus the replay's `For`; the post-load fork and every shadow/ledger operation put them back. So `CreateSaveGame` (:107) records live counts, and `AssertReplayLandedOn` lands on the second and third loads. Forks stop logging the alarm too. `RestoreCampaign`'s second rebuild (SimulationManager.cs:4625) orphans only the finished run-up's object, which is never drawn again.
- **Real trajectories move only** where a run-up or campaign is begun again in the same session after an old detachment (the next cycle, e.g. a1_fund_* at 4500 days, or an extra election); those now draw fresh numbers. Side benefit: no fast-forward over every draw so far on each shadow operation (CampaignAi had reached 166 491 draws).
- **Bars (item 5).** TrajectoryBaselineDump and TrajectorySentinelCheck build no shadow, so the sentinel cannot move. On the bar, only CongressOfRecordDiagnostic (CheckSuite.cs:222) and PlayProtocolCheck (:337) fork a shadow through `RestoreFromSave`, and neither advances it. ShadowBaselineDiagnostic and PolicyImpactLedgerDiagnostic are not registered anywhere, and their real games hold no player.
- **Comments (item 6).** `SimulationRandom.LiveState` resolves in CommentClaimCheck (a member that is a type name, CommentClaimCheck.cs:131). The new docs are accurate.

**Findings**

1. **MEDIUM, CONFIRMED** – ShadowBaselineDiagnostic.cs:63-83 and 292-345; CheckSuite.cs.
   - Nothing asserts the property this fix creates. The diagnostic's counter and finally checks compare counts only, and they passed under the bug. Neither proof gate is in CheckSuite.
   - Scenario: a later refactor back to (seed, counts), or a new swapper using `RestoreState`, passes every bar. Only an audit re-run's CAMPAIGN REPLAY line would show it.
   - Fix: add an edit-mode check to the bar.
     - Take `held = For(CampaignAi)`, run `shadow.AdvanceTurn()` and a fork, then `held.Next()`.
     - Assert `ReferenceEquals(For(CampaignAi), held)` and that the count rose by one.
     - Assert that no object in `_own` is in the real table.

2. **LOW, CONFIRMED** – ShadowBaseline.cs:129-141.
   - If a shadow turn throws, `_own` keeps its old dictionary, but its objects were already advanced in place. Streams first made in the failed turn are lost and start again at zero next turn. Before the fix, this was a clean rewind to the turn's start.
   - Fix: move `_own = CaptureLive()` into the `finally`, before `Reinstate(real)`.

3. **LOW, CONFIRMED** – SimulationRandom.cs:76-77, 86, 94.
   - These lines still say "NOTHING IN THE LIVE GAME DRAWS FROM IT" (and "the only caller is `CampaignAiHarness`"). That contradicts the new doc at :228-232 and SimulationManager.cs:4355-4356 and 4626-4627 (inventory D-16/SD-59).
   - The `Seed` doc (:128), "cannot leave some consumers on stale sequences", is also false for holders.
   - Fix both in this commit.

4. **LOW, PLAUSIBLE** – places outside the fix that still detach holders.
   - `Seed` called mid-run clears the table under any live holder: UiScreenshotDriver.cs:5229, and ElectionNightFilm.cs:58 via UiScreenshotDriver.cs:4498 and 5279. Film-only.
   - Saves written before the fix keep their frozen counts:
     - The first load of one cut during a run-up or campaign still logs the alarm once.
     - A frozen PreCampaign count, restored at SimulationManager.cs:4620-4625, makes the next cycle's run-up draw old numbers again.
   - Neither should be read as a regression; only a save-version bump would fix the old saves.

5. **LOW, PLAUSIBLE** – SimulationRandom.cs:245-250.
   - `Reinstate` clears the table before reading `live.Streams`. `LiveState` has a public implicit constructor, so `Reinstate(new LiveState())` empties the table, sets the seed to 0 and throws.
   - Unreachable today. Fix: make the constructor internal, or validate before clearing.

## What was done

- **1 (MEDIUM): asserted in the proof gate, not added to a bar.** `ShadowBaselineDiagnostic` (the shadow's proof gate, run on demand, in no bar) gains its sixth check: a stream object the real game holds is still the table's after a shadow turn and a fork, and a draw on it moves the table's count - the property counts cannot see. Proved both ways on this tree and on HEAD's generator (the record gives the readings). A bar check is declined under **R-N5**: a holder detached from the table has cost once.
- **2 (LOW): fixed** - the shadow takes its own live state in the `finally`, once its state is in (`swapped`), so a turn that throws keeps the streams it made, and a failure before the swap can never hand the real game's objects to the shadow.
- **3 (LOW): fixed** - `CampaignAi`'s, `Debate`'s and `Scandal`'s docs say the live campaign holds and draws them (they said nothing in the live game did); `Seed`'s says a holder keeps its object across a reseed. `ElectionNight`'s stands - only the film draws it.
- **4 (LOW): written down** - beside `SaveGameService.CurrentSaveVersion`: a save cut before the fix during a run-up or campaign, in a session that had loaded, carries the frozen counts (its first load logs the alarm once; a frozen run-up count re-draws old numbers). The films' mid-run reseeds are film-only and left.
- **5 (LOW): fixed** - `LiveState`'s constructor is internal.
