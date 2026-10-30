# wave/art155 (K302: Poland's Art. 155; K303-K307): the integrator's reading at the merge (2026-10-30)

**Files reviewed:** `Assets/Scripts/Elections/SejmFormation.cs`, `Assets/Scripts/Elections/WorldCalendar.cs`, `Assets/Scripts/Elections/WorldClock.cs`, `Assets/Scripts/Elections/FormationProjection.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/Simulation/SimulationManager.FormationProjection.cs`, `Assets/Scripts/Simulation/SimulationManager.Sejm.cs`, `Assets/Scripts/Simulation/SimulationManager.WorldCalendar.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, `Assets/Scripts/UI/ElectionNightScreen.Poland.cs`, `Assets/Scripts/UI/GameController.SejmVerdict.cs`, `Assets/Scripts/Testing/UiScreenshotDriver.cs`, `Assets/Editor/SejmFormationDiagnostic.cs`, `Assets/Editor/WorldCalendarDiagnostic.cs`, `docs/RULINGS_2026-10-23.md`, `docs/ASKS_FOR_ELIAS.md`, `Tools/film_scope.tsv`.

**The path here.**
1. K302 (Elias): Art. 155 is built as its own item, once K300 had landed with (iii)(b) read chamber-matched.
2. The branch was built on 553aadb8. Review round 1: findings M1, L1-L9. Round 2 (on d4773c30): READY after a real film of the Polish night (R2-M1), with R2-L1-L3.
3. ed2aba91 answered R2-M1 (the foot on one line: `PolandFooter`, `FitPolandVerdict`; the staged refusal frame `e7e2`), R2-L1 (the failure line `RefusedPresidentsCabinet` requires) and R2-L2 (the saves loaded and stepped).
4. The integrator's three readings (the President's choice, the abstention model, Konf-on-PiS) are recorded as K303 and K306.

**The merge.**
- `git merge --squash wave/art155` applied cleanly on 030d8d17 (after §951-§954).
- K303-K307 are written to RULINGS from the implementer's drafts with the review's additions (the caretaker in both paths, the last Sunday and the Wednesday edge DECLARED, the lapsed-save re-order DECLARED, repeated shortened terms unlimited).
- docs/ASKS_FOR_ELIAS.md:244 ("Art. 155 and the shortened term are not modelled") is marked ANSWERED (R2-L3).
- Film scope row `dryart155`: Poland at 1280.

**The measurement (this tree).**
- **nart155:** 8 of 9 the first time. `SejmFormationDiagnostic` failed two (g) lines, the continuous run's at 2027-12-27 and 2027-12-28.
  - **The cause is in the check, not the engine.** The loop stepped on to the vote day (2028-01-11), and the two lines then read the live records. By then the outgoing record's procedure had concluded, and the President's refused cabinet was the caretaker.
  - The run's own log shows the right sequence: the President's step due 2027-12-28; PiS appointed; refused 230-230 on 2028-01-11; elections on 2028-02-20. The two loaded-save lines, which read right after one step, pass.
  - The loop has run to the vote day since d4773c30; this diagnostic had never run in Unity before.
  - The integrator's fix reads each state on its own day (`beforeOk4`, `atAppointmentOk4`), with the same conditions.
- **dryart155:** Poland at 1280, 159 measured, 0 failed, 0 overflow.
- **realart155** (1280, `-shotinterrupts`) and **realart155w** (2560): 23 captured, 0 failed each, no OVERFLOW line.
  - `e7c` (the ordinary branch) and `e7e2` (the staged PiS 230 / KO 230 refusal, planted and put back) each show the verdict on one line, cut with " …" before CONTINUE: 179 of 483 characters at 1280, 212 at 2560. The whole sentence is in the verdict's slip.
  - **Filed (Low):** the foot leads with the first sitting's date, so the cut falls on the outcome ("NO GOVERNMENT IS EXPECTED TO FORM FROM …"). Leading with the outcome would show it.
- **dumpart155** against dumpfisc2: **byte-identical on both seeds.**
  - In the `fisc2` family no Polish Sejm ends without a government. Both dumps hold no caretaker line, every in-run Sejm installs by Art. 154's steps, and Art. 155 never runs.
  - The review's prediction (seed 777 moving from 2059-12-02) was made on the k311 family, before F4 moved Poland's century. The sentinel stands at `fisc2`.
- Ledger rows `s955` for the four money-path states, from the review: `SimulationManager.cs` 46eefe3b, `.FormationProjection.cs` 12fbda13, `.Sejm.cs` 6fca1e15, `.WorldCalendar.cs` d82a9689.

**Verdict:** READY. The merged state equals the reviewed branch (ed2aba91) plus the RULINGS lines, the ASKS annotation, the film scope row and the diagnostic's read-on-the-day fix.
