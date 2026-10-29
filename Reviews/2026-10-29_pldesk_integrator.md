# wave/pldesk: the integrator's reading at the merge (2026-10-29)

**Why this exists.** K197 (COAL-1's ask c) filed the Polish player's desk verbs as a follow-on UI item: leave the government, and move Art. 158's constructive vote. The manager has held both verbs since COAL-1 (§925), but the desk drew neither. The confidence block returned early wherever `TakesMotions` is false, and Poland is such a case.

The item was built on `wave/pldesk` (c9935925, 47c1d408). Review 1, `Reviews/2026-10-29_pldesk_review.md`, was NOT READY:
- H1: the diagnostic was registered in no bar.
- H2: the film frames were unreachable on Poland's default start, where PiS governs alone with no junior partner, so they were skipped as "skipped, not missing".
- M1: K196's quiet days had been applied to the player's Art. 158 motion with no ruling. That was asymmetric with the AI's same-day motion and with Germany's Art. 67.

The rework (83f87db8) stages the KO+TD+NL cabinet with the four frames `Expect()`-declared, drops the quiet-day test from the player's motion (K270), and restores the staging fully. Review 2, `_review2.md` (579ff37e, eda9ea40), was READY with Lows. This file reads the merged state.

**Files reviewed:** `Assets/Scripts/Simulation/SimulationManager.PolishDesk.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/UI/PolishDeskRecord.cs`, `Assets/Scripts/UI/GameController.PolishDesk.cs`, `Assets/Scripts/UI/GameController.ParliamentRows.cs`, `Assets/Scripts/UI/GameController.DocketV35.cs`, `Assets/Scripts/UI/GameController.TopStrip.cs`, `Assets/Scripts/UI/TopStrip.cs`, `Assets/Scripts/Elections/ConfidenceProcedure.cs`, `Assets/Scripts/Testing/UiScreenshotDriver.cs`, `Assets/Editor/PolishDeskVerbsDiagnostic.cs`, `Assets/Editor/UiLeverVerbCheck.cs`, `Assets/Editor/CoalitionExitDiagnostic.cs`, `Assets/Editor/ConfidenceDiagnostic.cs`, `Assets/Editor/CheckSuite.cs`, `Tools/film_scope.tsv`, `docs/RULINGS_2026-10-23.md`.

**The merge.**
- `git merge --squash wave/pldesk` (83f87db8) on f78bedd8 merged without conflict.
- The changed-line sets of all 14 code files on the branch (`git diff f78bedd8 wave/pldesk -- f`) and on main (`git diff HEAD -- f`), sorted with carriage returns stripped, are IDENTICAL.
- The save format is unchanged (68), because no state is added.
- GATE OK.

**The integrator's changes.**
- Registered `PolishDeskVerbsDiagnostic` in the cheap batch after TopStripCrisisDiagnostic, commented s943. This answers H1, the condition of both reviews.
- Added the film-scope row `drypd`.
- Recorded K270 in the rulings: the player's constructive motion (Art. 158, like Art. 67) is not held by K196's quiet days, which rule an AI partner's exit.

**The measurement (this tree, s943).**
- **npd:** PolishDeskVerbsDiagnostic, CoalitionExitDiagnostic, ConfidenceDiagnostic, ConstructiveVoteDiagnostic, TopStripCrisisDiagnostic, UiLeverVerbCheck, MetaTextCheck and ChamberVerdictCacheCheck are 8 of 8 clean.
- **dumppd:** BYTE-IDENTICAL to `k252`. The verbs and the Docket items change nothing in a world with no player, so the sentinel stands.
- **Films:** drypd (Poland 1280) and realpd (Poland 1280) exit 0, with no OVERFLOW, CLIP or ESCAPE, and all four `Expect()`-declared frames filmed. `realpd_pl4_docket_art158.png` reads on the Docket: "Art. 158 · PiS's candidate not elected · today", "200 of 460 deputies for, 231 needed of the statutory 460 · KO · TD govern on 230 of 460 seats, a minority" (Information), with the strip's place 4 reading "Art. 158 · PiS's candidate not elected today". The longer Docket line, the review's N2, fits at 1280.
- **cheappd:** one failure, the ledger. Rows are added at s943 with this file as evidence: `SimulationManager.PolishDesk.cs` a2315d66 and `SimulationManager.cs` 9a8ea3c0.
- **The simulation bar:** recorded in the commit's Bars line.

**Seen on film, a pre-existing Low (not this item's):** the minister alert on the same Docket prints the portfolio's raw identifier, "A leak from InteriorJustice", not its display name "Interior & Justice". Filed.

**Lows left from review 2:**
- N1: the staging leaves the division log's private counter one higher; this is harmless, because EndSweep follows.
- N4: the default start's frames are staged in the 2023 run-up, and the log says STAGED.

**Verdict:** READY. The merged state equals the reviewed branch line for line. The verbs' gates are byte-identical to the inline gates they replace, apart from the Sejm's own rows. The four frames are filmed and the no-player dump is unchanged.
