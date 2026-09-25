# Review — §640 the caretaker loop (2026-09-25)

One independent reader, read-only (no build, no Unity run), on the working-tree diff of
`Assets/Scripts/Simulation/SimulationManager.cs` (`ResumeAppointmentAfterElection`, called from `AdvanceConfidenceDay`),
`Assets/Scripts/Elections/GovernmentRecord.cs` (`ProcedureResumedAfter`) and `Assets/Editor/ConfidenceDiagnostic.cs` (part 4).

## Checked and found correct

- The ordering in `AdvanceConfidenceDay`: on an extra election's polling day the resume waits (the pending date); the game's `CheckElection`
  runs after the tick and `ResolveElectionVerdict` installs a formed government at once, so the day after, the record the resume reads is
  final. The ordinary-serves branch does not re-fire. The caretaker's early return skips nothing the old code did.
- The held-election scan: a same-day record is excluded; a discharge on an ordinary polling day resumes the next day; a NotImplemented record
  is skipped; earlier elections are excluded by the discharge date; `Caretaker` and `CaretakerSince` arrived together (format 33).
- Reach: the day tick runs for the player's country only (and from the harness and diagnostics); a caretaker needs a carried motion; the
  warm-up calls no tick; the no-player trajectory cannot move.
- The motion and extra-election guards; 3 kap. 11 § rightly not applied to 6:5's extra election; the extra election's eight-week campaign.
- The diagnostic's timeline, derived by hand: two extra elections ordered (19 April, 19 July 2026), then the ordinary of 13 September serves.
- The doc claims against the statute's text, but for one wording (finding 6).

## Findings, and what was done

1. **defect (low)** — the day after a held election, the ordinary election's run-up re-begins before the resume orders the next extra election,
   and is left orphaned; if the next extra election falls in the ordinary campaign's window, the campaign's opening adopts the orphaned run-up
   with the extra election's record. **Fixed:** the opening adopts a run-up only with its own election's record (the run-up's own guard, §636);
   `ScheduleExtraElection` drops a run-up and a record dated to another polling day. **Stated:** the run-up's one day, drawn before the order
   lands, stays drawn.
2. **diagnostic depth** — the "once per election" guard the field exists for was never exercised (the pending date guarded it). **Fixed:** two
   more days after the ordinary-serves break add nothing.
3. **vacuous assertion** — "nothing resumes" passed on an empty history. **Fixed:** an election before the discharge is recorded first.
4. **vacuous assertion** — "the caretaker serves on" cannot fail in a diagnostic that holds no game election. **Dropped**.
5. **stale doc** — `GovernmentRecord.Caretaker`: "it orders no extra election". **Fixed:** it cannot itself decide one (3 kap. 11 §); the
   resumed procedure can (6 kap. 5 §).
6. **wording** — "it resumes once an election has been held". **Fixed:** "only after".
7. **save convention** — no bump; the field round-trips and an older save's caretaker reads "not yet resumed", its true state. **Stated** in §640.
8. **player-facing** — the night's no-government verdict ("You stay in office until one can") does not name the loop. **Deferred** to the
   formateur's night line (its ninth premise).
9. **process** — the ledger row for `SimulationManager.cs`. **Added.**
10. **scope** — an extra election the government ordered itself that forms none leaves a non-caretaker government, and no procedure runs:
    the ruling's case is the caretaker. **Stated** in §640.
