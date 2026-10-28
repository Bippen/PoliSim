# wave/dnight: the integrator's reading at the merge (2026-10-28)

**Why this exists.** Design answered D-NIGHT in its return of 2026-10-28 (`PoliSim-captures/design/RETURN_2026-10-28/send/D-NIGHT_return/RETURN.md`, boards 31a-31c). The answer redraws the election night's WHO GOVERNS column in v3.5, keeps the long wording whole in slips and a † view, and corrects the tick to where the rule counts.

It was built on `wave/dnight` (0387734a). Its passage to the merge:
- **Review 1** (`Reviews/2026-10-28_dnight_review.md`): NOT READY. Findings: H1, Italy's column overflowing 516 px; M1, art. 64's rule misstated; M2, a refusal by a declining partner contradicting its own bar; M3, no films of the new surfaces; M4, plants missing.
- **Rework** (c7963b4e, 7f60d6cd).
- **Review 2** (`_review2.md`): NOT READY. N1: the decline producer was not in the branch although the commit said it was. The implementer's edit had been refused by its tool and reported as done.
- **Fix** (8086c3fc, 110c3b82).
- **Review 3** (`_review3.md`): READY, with Lows.
- **Lows fixed** (396a6102).
- **After the integrator's first films and cheap bar** (2280a94f): three cheap-bar failures and an 88 px gap seen on film.

**Files reviewed:** `Assets/Scripts/UI/ElectionNightScreen.Governs.cs`, `Assets/Scripts/UI/ElectionNightScreen.cs`, `Assets/Scripts/UI/NightColumnPlan.cs`, `Assets/Scripts/UI/GameController.cs`, `Assets/Scripts/Elections/GovernmentFormation.cs`, `Assets/Scripts/Simulation/SimulationManager.FormationProjection.cs`, `Assets/Scripts/Testing/UiScreenshotDriver.cs`, `Assets/Editor/FormationProjectionDiagnostic.cs`, `Assets/Editor/PartyInkDrawSiteCheck.cs`, `Tools/film_scope.tsv`, `docs/RULINGS_2026-10-23.md`.

**The merge.**
- `git merge --squash wave/dnight` (396a6102) on 0ce03aa3 (§934 landed) merged without conflict. Regional 5a's merge was held aside in a stash ("reg5a-merge-hold-2") while this item was measured.
- The later fix 2280a94f was taken onto main with `git checkout wave/dnight -- <file>` for exactly the five files it touched.
- The changed-line sets of every code file on the branch (`git diff 5c38ce1c wave/dnight -- f`) and on main (`git diff HEAD -- f`), sorted with carriage returns stripped, are IDENTICAL:

  | file | changed lines |
  |---|---|
  | `FormationProjectionDiagnostic.cs` | 221 |
  | `PartyInkDrawSiteCheck.cs` | 11 |
  | `GovernmentFormation.cs` | 92 |
  | `SimulationManager.FormationProjection.cs` | 160 |
  | `UiScreenshotDriver.cs` | 172 |
  | `ElectionNightScreen.Governs.cs` | 1332 |
  | `ElectionNightScreen.cs` | 394 |
  | `GameController.cs` | 9 |
  | `NightColumnPlan.cs` | 72, new |

- GATE OK. FormationProjectionDiagnostic was already registered (cheap), so no CheckSuite line is added.

**What the first measurement found (nnt, the films, cheapnt).**
- **Named checks:** FormationProjectionDiagnostic with every new clause and plant, FormationSweepDiagnostic (the digest UNMOVED: the projection reads the verdict and writes nothing), MetaTextCheck, PartyInkDrawSiteCheck, UiLeverVerbCheck and NumberLocaleCheck were clean. ReviewLedgerCheck owed the money path's row.
- **Films:**
  - The dry film (Sweden, Germany and Italy at 1280) was clean.
  - The real Canvas films showed 0 Canvas-text violations and 0 plan-against-built mismatches: Sweden's night at 1280 and 2560 under `-shotelectionnight` (projected, †, the two staged Italian frames, the model frames), and the Sweden and Germany takeovers at 1280 plus Germany at 2560 under `-shotinterrupts`.
  - Italy's `-shotinterrupts` film reached no night. The game holds no Italian campaign, so after 1,467 days the harness forced its exit. This is not a defect of this item, and Italy's column is filmed through the two staged frames.
- **Three cheap-bar failures:**
  - ConstantProvenanceCheck: `BundestagBallots = 3` was unmarked (now SOURCED, Art. 63 GG).
  - UnwiredSubsystemCheck: Governs.cs's public static planner was called only by the diagnostic. It moved to `NightColumnPlan.cs`, which the night's own code calls.
  - ChamberVerdictCacheCheck: a false positive on a local `k.Support =`, now renamed `SupportRow`.
- **On film:** Sweden's projected column had an 88 px gap above the movers. The act strip's cells force-expanded and the strip took the column's spare height. Its `flexibleHeight` is now 0. The plan check could not see it, because it compares preferred heights and the strip's preferred height was right.

**The measurement after the fix (s935).**
- realntse2 (Sweden's night at 1280) and realntse25b (2560): exit 0, with no OVERFLOW, CLIP, ESCAPE or plan mismatch.
- `realntse2_e6_election_night_projected.png` shows the column as drawn on 31b: Speaker's proposal 1 of 4; a majority government of 198; ✓ 151 against · 175 needed to refuse, with the tick on the refusal line; EXPECTED OUT OF OFFICE for S; the 0.2 pp bridge; the eleven-act strip; the one mover; the budget block. The column now packs top-down with no gap.
- `realntse_e6_election_night_italy_none_staged.png` shows three struck mandates, "The chambers are dissolved", Italy's one vote block with Camera and Senato lanes, each with its own art. 64 tick, and CARETAKER SERVES ON.
- cheapnt2: one failure, the ledger row for `SimulationManager.FormationProjection.cs` at d71c302c. Its changed lines are the branch's, read in the three reviews, and its row is added at s935 with this file as evidence.

**Declared readings** (K-lines at the merge):
- the bar carries the model's own sides;
- refused cells marked with the refused party;
- the Bundestag records no "no" (abstain as cast);
- Italy as one block with two lanes at art. 64's line;
- a decliner or a withdrawing supporter drawn as it stands, with its own reason;
- the derived red-lines note in the outcome slip and †, not at rest;
- the column's shrink order;
- Germany's Fraktion bar retired to †;
- point decimals;
- the Document face at weight;
- slips keep case.

Design's DERIVED split for Sweden's filmed chamber is confirmed by the model: M·KD·V·MP against, SD·C·L abstaining.

**Verdict:** READY. The merged state equals the reviewed branch line for line, the formation sweep is unmoved, both widths film clean, and every defect the measurement found is fixed at its cause.
