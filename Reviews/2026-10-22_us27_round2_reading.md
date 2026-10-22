# US-27, the second review's fix round (c6cdd7ac): the integrator's reading (2026-10-22, s896)

**Reviewed:** branch `wave/us27`, `bb1b04a0..c6cdd7ac`, the round that answers the second review (`Reviews/2026-10-22_us27_review2.md`: no H; M1 and M2 to Elias as asks, L1-L4), squash-merged with the whole branch onto main cfaf166b (§895), the branch's own base, with no conflict. Read by the integrator from the diff. The first review is `Reviews/2026-10-22_us27_midterm_review.md` (the implementer's subagent) and the second, independent one is `Reviews/2026-10-22_us27_review2.md`, which read `cfaf166b..bb1b04a0`. This reading covers the delta after it, so that the review ledger's rows for the money paths name a reading of the state being committed.

**Files reviewed:**
- `Assets/Scripts/Simulation/SimulationManager.cs`
- `Assets/Scripts/Simulation/SimulationManager.PlayerElection.cs`
- `Assets/Scripts/Elections/UsCongressElection.cs`
- `Assets/Scripts/Elections/NationalElection.cs`
- `Assets/Scripts/Elections/EconomicVote.cs`
- `Assets/Editor/UsMidtermDiagnostic.cs`
- `Assets/Scripts/Persistence/SaveGameService.cs`

## SimulationManager.cs - the one new call (M1, measurement only)

`HoldUsMidtermCount` now computes, before the count's own prediction, `recordWords = IsCounterfactual ? null : UsCongressElection.RecordInVWords(...)`, and appends it to the midterm's CONGRESS log line. `SimulationManager.PlayerElection.cs` is unchanged by this round.

What `RecordInVWords` runs: `TryRecordInV` reads `EconomicVote.RecordPartsOverTerm` (the economic, approval and fiscal shifts, each computed from the country's stored series; no store) and calls `NationalElection.TryPredictShares` five times (no shift, each component alone, all three) to express each in points of REP's two-party V. Its side effects, read in `NationalElection.cs`:
- `TryPredictShares` ends in `DeriveRegional`, which writes the process-wide regional readout. This is why the call sits BEFORE the count's own `TryPredictShares`: the count's call writes the readout last, so the readout the night and the slips read is the count's, exactly as before. The comment says so.
- No RNG draw: the prediction is deterministic (no `SimulationRandom` in the path), and the tie seed the count uses (`TieSeedOf(master, year)`) is computed after and independently.
- No country field, contest, history or cache is written: `RecordPartsOverTerm` and the predictions return values only.
- Skipped where `IsCounterfactual`: a ledger fork or the no-policy shadow never runs it, so FORK-2's isolation (§890: every counterfactual step keeps the process-wide regional readout) is untouched and a fork's count is the played world's.

So the call changes no figure in any world: it adds a log clause on the one midterm day of a played US game. Its cost is five predictions, once in four years.

## The other files

- `UsCongressElection.cs`: `TryRecordInV` / `RecordInVWords` (above), pure functions.
- `UsMidtermDiagnostic.cs`: reads the components on 3 Nov in its step loop and prints them in (c). L1: on the world restored from the post-count save it calls `HoldMidterm` on 3 Nov and asserts null, "still to count", and exactly one 3 Nov contest. The guard now has teeth: deleting the counted-guard would make the second hold return a contest.
- `SaveGameService.cs`: the format-52 history comment loses its weak clause (L4). Comment only.
- M2's header amendments (`Tools/us_midterm_record.pl`, the regenerated `house_midterms.csv` header and `US_MIDTERM_RECORD.md`, a dated NOTE appended to the fetch log) change no data row. The fit, re-run, is the same (n 20, -3.755, [-4.966, -2.544]), and only its `SourceDigest` moves with the CSV's header bytes. `UsMidtermFitCheck` holds the new digest, and `UsMidtermCheck`'s card block must be regenerated (WriteReadings) in this pass.

## Verdict

No defect. The round is measurement and comments: the new SimulationManager call is placed so that the state it leaves is the count's, skipped in counterfactuals, and writes nothing persistent. The rows for `SimulationManager.cs` and `SimulationManager.PlayerElection.cs` at the merged state rest on the second review for `cfaf166b..bb1b04a0` and on this reading for the rest.
