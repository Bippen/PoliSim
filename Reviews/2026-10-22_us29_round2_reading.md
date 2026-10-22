# US-29, the second fix round (6a441c8c): the integrator's reading (2026-10-22, s893)

**Reviewed:** branch `wave/us29`, `b399da17..6a441c8c`, the round that answers the second review (`Reviews/2026-10-22_us29_fix_review.md`, verdict MERGE, Lows only), squash-merged with the whole branch onto main 0244e796 (§892) with no conflict. Read by the integrator from the diff, hunk by hunk. The first review is `Reviews/2026-10-22_us29_review.md`; this reading covers only the delta after the second, so that the review ledger's row for the money path names a reading of the state being committed.

**Files reviewed:**
- `Assets/Scripts/Simulation/SimulationManager.cs`
- `Assets/Scripts/Testing/UiScreenshotDriver.cs`

## SimulationManager.cs - the one behaviour line (the second review's L5)

`ConsentsToAiNominee`'s `logDay` gains 20 January beside the term's own day and 3 January:

```
- bool logDay = outgoing.TermEnds == CurrentDate.Date || (CurrentDate.Month == 1 && CurrentDate.Day == 3);
+ bool logDay = outgoing.TermEnds == CurrentDate.Date || (CurrentDate.Month == 1 && (CurrentDate.Day == 3 || CurrentDate.Day == 20));
```

`logDay` is read in exactly two places, each the condition of a `Debug.Log`/`Debug.LogError` call: the null-count defect line and the refusal line. Neither branch's control flow depends on it otherwise: the count is taken before it, the return value (`count != null && count.Confirms`, as before) does not read it, and nothing is stored. So the only effect is one more day a year on which an ongoing holdover is written to the log. The method is reached only for a USA whose chair's term has ended and whose president is not the player (US-29's AI-nominee path); every other world never enters it, so no figure, saved field, hash or RNG draw anywhere can move. The summary is reworded to name the oath day and why (a player-president leaving office inside a holdover hands the nomination to the AI that day); the claim it makes is the code's.

Note for the record: the comment's justification is honest about its reach - in a two-party Senate an incoming president is nearly always confirmed, and confirmations always log - so this is diagnostics, not behaviour.

## UiScreenshotDriver.cs - the film's guards (L1, L2, L3)

- **L1.** Both staging blocks (93a-93c and the new 90h/90i) now snapshot the day and `CentralBankHeadAwaitsPlayer` before staging and fail (`_failed++`, "THE HOLD DID NOT RETURN") when either differs after the closing `Settle`; 90h/90i also fail unless `player.SecondChamberSeats` is by reference the dictionary found. A staged frame that let a day tick would now fail the film instead of silently moving every later US frame to another day. The restored log line prints the before/after values, so the failure names itself.
- **L2.** `_onDesk` and `_docketOpenItem` are snapshotted beside the tab and category (the missing-field guard widened so an absent field fails as MISSING, not clean) and written back in the `finally`, so they return on every exit path including a throw inside the staging.
- **L3.** After the swap, where `UsFedConsent.Confirms` still holds (an equal division the president's Vice President decides), one more seat moves from the president's party to the other, on a copy (`new Dictionary<string, int>(...)`): the original dictionary `senate0` is never mutated, and the `finally` puts `senate0` back by reference, which L1's reference check then asserts. The log line states the move and the split.

The driver is Testing code: it runs only under the film harness and is not compiled into a player's path through the simulation.

## Verdict

No defect. The SimulationManager change is log-only by construction and reachable only on US-29's own path; the driver changes make the film fail loudly where it previously could pass on a moved day or a leaked flag. The trajectory proof (M4) is the integrator's dump diff against `aloopsal2`, recorded in §893.
