# Review - §825, the gameplay audit's Phase 4: GA-0-14 and GA-0-40, the challenge scenarios retired (2026-10-14)

Two read-only agents. **The plan** (before any code): a map of everything the two scenarios touched - the core types, the save layer, the game's and the selector's screens, the film's frames, the harness's key, the plans and the docs - and two ways to retire them, dormant or deleted; it recommended deleting the machinery (dormant code would read as live, the class `DeadStateCheck` and `UnwiredSubsystemCheck` exist to catch), and the record follows it. **The review** (of the change in the tree while its recheck ran) emulated the project's own checks in Perl on the working tree and on a `git archive HEAD` copy - its report, verbatim:

## The report

HIGH 0 / MEDIUM 0 / LOW 7

I found nothing that breaks the build or fails a check. All seven findings are stale wording, missing coverage, or work still owed with the commit. Where a check can't be run without Unity, I re-implemented it in Perl (same regexes, same comment-stripping rule) and ran it on the working tree and on a `git archive HEAD` copy in my scratchpad. Nothing in the repo was written, staged or run.

**Checked and correct**

- **Compile safety.** I extracted all 90 names declared in the removed code (the diff's `-` lines plus the six deleted files) and grepped each across `Assets/`.
  - The only matches are unrelated names in other classes: `EnergyMarket.EndTurn`, the `Undecided` tuple field in `StanceModelDiagnostic`, `RunLine`, `RunTrajectory`, `RunFromMenu`.
  - Every caller was updated:
    - `CountrySelectorScreen.Build(_world, SelectPlayerCountryAndParty)` at `GameController.cs:1796`.
    - `ShowPartyPanel` is called with two arguments at `UiScreenshotDriver.cs:384` and `CountrySelectorScreen.cs:495`.
    - No `offerCreate:` argument is left anywhere.
  - Nothing else can still reference the deleted files: there is no asmdef or `csc.rsp`, `Tools/textcheck/TextCheck.csproj` compiles none of them, and no .unity/.prefab/.asset references their GUIDs.
  - In `SaveGameService.Deserialize`, the new pattern variables `ui` and `scenario` don't collide with anything, and `retiredScenario` is definitely assigned (the catch block throws).
- **Reflection by name.** No string literal names `_scenario*`, `StartScenario`, `DismissScenarioVerdict`, `OpenScenarioPartyPick`, `ScenarioStrip` or `Scenario_` anywhere in `Assets/` or `Tools/`.
  - No `-shotstop` value or `Expect()` call names a removed frame.
  - The `bar_tier.ps1:46` entry for CountrySelectorScreen now lists only `01_country_selector, 01g_party_picker`.
- **The project's checks**, emulated on the working tree and HEAD:
  - **DeadStateCheck:** 0 unreached and 0 write-only (2,867 private declarations, down from 2,894). No private member of the four classes lost its last caller:
    - `SeatPlayerParty` is still called at `GameController.cs:2037`.
    - `CtaWidth` is still called at 9239 and 9260.
    - `DrawColoredLabel`, `DeskCaption`, `_neutralActionButtonStyle`, `InvokeNoArg` and `FolderName` all still have callers.
    - `InvokeOneArg` is down to one caller, `UiScreenshotDriver.cs:3790` (see finding 1).
  - **UnwiredSubsystemCheck:** 2 of 2 unwired and 2 of 2 unreachable, the same files as HEAD.
  - **PhantomGuardCheck:** no comment names any of the three deleted diagnostics.
  - **CommentClaimCheck:** 0 missing on both trees. The new comments' only dotted claim, `SaveGameService.Deserialize`, resolves.
  - **DocumentClaimCheck:** 0/0 on the three live root documents, same as HEAD.
  - **RatchetSlackCheck:** no ratchet's measurement fell below its ceiling.
    - ConstantProvenanceCheck reads only `Simulation/`, and ScenarioEvaluator had no constants.
    - MojibakeCheck stays at 1/1 (the known line in COMPLETED.md).
    - No money-path file was touched (`bar_tier.ps1:37`, `:40`), so ReviewLedgerCheck owes no row.
  - **MetaTextCheck:** no remaining literal needed the two removed allow rows. The new warning text at `GameController.cs:906` matches none of the 19 banned patterns, and it sits on a logging line, which the widened patterns skip.
  - **Encoding:** every edited file is valid UTF-8 with its BOM state unchanged, and `bar_tier.ps1` contains no byte above 0x7F.
- **(4a) `ForeignPolicyCadenceMultiplier` stays 1 everywhere.**
  - Nothing in `Assets/` or `Tools/` writes it, in code or by string. It keeps its initializer `= 1f` (`SimulationManager.cs:1055`) and is read once (`:1141`).
  - Both retired scenarios set it to 1 as well, so no trajectory moves.
- **(4b) The selector's title block height is consistent.**
  - 62 + 8 + 1 + 8 + 18 = 97 units: wordmark, the rule pair (preferred height 1, `CountrySelectorScreen.cs:972-988`), subtitle, and two spacings.
  - This matches HEAD's branch with no strip.
- **(4d) The audit harness still reaches `Phase.PartyPanel`.**
  - It goes there from StartSheet (`AuditDriver.cs:426`).
  - The `Scenario` field fed only `Describe`, the main-menu fallback, `Selector` and the SOFTLOCK message, and all four were updated.
  - A plan that still says `scenario=` is refused whole at parse (`AuditDriver.cs:197`, `AuditHarness.cs:107-112`).
  - Commented lines are skipped (`AuditHarness.cs:106`).
  - `phase1_smoke.plan` still has one Italy run.
- **(5) The save load is sound.**
  - Format version stays 38.
  - `MissingMemberHandling.Ignore` (`SaveGameService.cs:128`) skips both dropped keys, including a nested object carrying `$id` (nothing `$ref`s it).
  - `"Ui": null` is a JValue (not a JObject), so no warning; a missing `Ui` or missing `Scenario` gives null.
  - HEAD's free-play shape `"Scenario": null` (seen in a real save, `p4_ga12b/a1_de_sl.day110.save.json:44724`) gives no warning.
  - `RetiredScenarioId` is `[JsonIgnore]`, so it is never written.
  - A save taken with the verdict pending loads with `IsGameOver` false, as free play, paused, with no exception.
  - `SaveLoadRoundTripDiagnostic` is unaffected: it saves with `ui = null` and its JSON comparison doesn't change.
- **(4c) No later `-shotstates` frame fails.**
  - 88n, 88b/88c, 93a and 93b are not in `Expect()`.
  - Starting the signing ceremony doesn't depend on `_onDesk` (`GameController.cs:1820-1822`).
  - The "GAME OVER" banner line is drawn on every page (`GameController.cs:5736-5741`).
  - The e* frames belong to other film modes and are untouched.

**Findings**

1. **LOW, CONFIRMED - the `-shotstates` frames after 93c now show different states.**
   - **Where:** `UiScreenshotDriver.cs:3786-3794`, `:3818`, `:3845-3862`, `:1459`.
   - **Surface:** HEAD's Italy block seated Italy, so the country re-seat guard ran `SelectPlayerCountry`, which sets `_onDesk = true` (`GameController.cs:2144`). That guard can no longer fire. `_onDesk` therefore stays false as the sweep leaves it (`:602`), with Budget/Tax left by 93b (`:3745-3746`). In all six films, 89b_signing_restored and 88b/88c are now drawn over the Budget page. HEAD showed the Desk there in the five non-Italy films.
   - **Clock and world:** HEAD's verdict loops drove the clock to turn 12, then turn 30 (one turn = 365 days; HEAD `:3879`, `:4030`). They applied Sweden's debt at −50% of GDP with a fresh fund at 60% of GDP, and Italy's debt at 165%. They also pointed the manager's `PlayerCountryId` at Sweden, then Italy, during those loops. Now `AdvanceToElectionTurn` (`:3845`) stops at the first polling day after the state pins. So 88n films a different election on a different world.
   - **93a/93b:** `CaptureDeskByRole` reads the player's role after that election, so 93a/93b may switch between filmed and "skipped".
   - **Fix:** re-film and re-baseline these frames. If the Desk is the intended surface, set `_onDesk` explicitly before the signing pins. If the guard is deleted as dead code, delete `InvokeOneArg` in the same change, or DeadStateCheck (ceiling 0) fails.

2. **LOW, CONFIRMED - the load warning is wrong for a save taken after a scenario verdict was closed.**
   - **Where:** `GameController.cs:903-907` vs `:1037-1038`.
   - **Why:** HEAD's `DismissScenarioVerdict` (HEAD `:2123-2133`) set `_isGameOver = true` with a "Scenario complete/failed - …" reason but kept the scenario progress. Such a save carries both `Ui.Scenario` and `IsGameOver: true`.
   - **Failure:** the log says "loads as free play", but the run restores as finished: the clock is held by the gate at `:734` and the controls are disabled. Its GAME OVER line names a scenario the game no longer has.
   - **Fix:** word the message from `save.Ui?.IsGameOver`. Whether such a run should reopen as free play is Elias's call.

3. **LOW, CONFIRMED - the `ScenarioId` cast can throw past the catch.**
   - **Where:** `SaveGameService.cs:179`; the try only catches `JsonException` (`:181`).
   - **Failure:** if `ScenarioId` is a number, object or array (only possible in a hand-edited file; the game always wrote a string or null), it throws ArgumentException. That reaches LoadFromPath's general catch and shows "Load FAILED" for a key the game no longer reads.
   - **Fix:** `(scenario["ScenarioId"] as JValue)?.Value as string`.

4. **LOW, CONFIRMED - the new load path has no test, and the save-format comment lacks its entry.**
   - **No test:** `SaveLoadRoundTripDiagnostic.cs:224/258/273/330` always save with `ui = null`, so the retired-id branch never runs. The deleted ScenarioSliceDiagnostic's save-crossing runs were the only coverage of a save carrying a scenario.
   - **Missing entry:** `SaveGameService.cs:59-80` records every Phase 4 change that didn't bump the format (GA-8-1 §816 through GA-1-2 §821), each with what an older save keeps. §825 adds no entry.
   - **Fix:** add a probe to `ExerciseFileIoAndVersionGate`: inject `Ui.Scenario.ScenarioId` and assert `RetiredScenarioId`; assert null for `"Ui": null` and `"Scenario": null`.
   - **Fix (version comment):** add a "Nor for GA-0-14/GA-0-40 (§825)" entry saying what such a save keeps:
     - the scenario's start changes baked into the world (Sweden's debt and replaced fund; Italy at 165%);
     - a pending verdict is dropped;
     - a closed verdict's game over is kept.

5. **LOW, CONFIRMED - dead public setter and stale present-tense comments.**
   - **Setter:** `SimulationManager.cs:1050-1055` still says the multiplier is "set by GameController when a scenario starts and re-derived from the scenario id on load". Nothing writes it now. `ForeignPolicySystem.cs:69-71` still calls the parameter the "per-scenario pacing lever".
   - **Other stale prose:** `DebtLedgerRecorder.cs:40` and `:117`, and `DebtAttribution.cs:8` (a scenario seed as a live debt writer); `AuditInvariants.cs:274`; `InkContrastCheck.cs:94`; `PoliSimWidgets.cs:341`; `StatTracePanel.cs:100`.
   - **Why no check catches it:** CommentClaimCheck only reads backticked `Type.Member` claims, and DeadStateCheck only looks at private members.
   - **Fix:** delete the property and drop the argument at `:1141`. `TryRollMeeting` defaults to 1, and multiplying 0.01 by `Mathf.Max(0, 1)` gives exactly 0.01, so results stay identical. Or reword these docs as history.

6. **LOW, CONFIRMED - audit documents and plans still treat the scenarios as live.**
   - **Audit protocol:** `docs/audit/AUDIT_PROTOCOL.md:35` ("every start point and the three challenge scenarios") and `:53` ("Events and the challenge scenarios"). These contradict "out of the matrix".
   - **Inventory:**
     - `docs/audit/inventory/F_ui_states.md:89` and `:211-213` (rows F-14, F-111 to F-113).
     - `D_campaign_cabinet_events.md:603`: row INV-S4 says "an unknown id is refused", but it is now read past with a warning.
     - `D_campaign_cabinet_events.md:722` (row SD-49).
   - **Plan headers:** `Tools/audit/phase1_smoke.plan:1` ("both scenarios") and `area1b_resume.plan:5`.
   - **Tracked plans still using the key:** `p4_ga1_1.plan:49-50,54-55`, `p4_ga1_2.plan:51-52,56-57` and `p4_ga1_3.plan:49-50,54-55` still have live `scenario=inherit_the_fund` lines. COMPLETED.md §818/§820/§821 cite them as reproducible measurements, but re-running any of them is now refused whole (exit 2).
   - **Leftover diagnostics:** `ScenarioCandidateMeasurementDiagnostic.cs` and `ItalyDebtMeasurementDiagnostic.cs` only measured scenario candidates; the decision doesn't mention them.
   - **Fix:** comment out those plan lines as was done for the three standing plans; reword the protocol, inventory and header lines; get a ruling on the two diagnostics.

7. **LOW, CONFIRMED - owed with the commit.**
   - **COMPLETED.md §825:** COMPLETED.md ends at §824 (line 38295), but 15 new code comments, 7 plan lines and `POLISIM_FEATURE_LIST.md:272` cite §825.
   - **Untracked plan:** `Tools/audit/p4_scen.plan` (the change's verification plan) is untracked.
   - **Real film:** CountrySelectorScreen is on bar_tier's Canvas list (`bar_tier.ps1:45-46`), so a real film of 01_country_selector and 01g_party_picker is owed.
   - **Layout:** `TitleTop = 28` (`CountrySelectorScreen.cs:836`) was moved up from 40 by §718 only to make room for the strip. The frame now has an empty band of about 67 units between the title block (ends 125 down) and the grid (starts 192 down on the 1064-unit canvas).
   - **Fix:** write §825, add the plan, film both frames; whether `TitleTop` goes back to 40 is Design's call.

## What was done

- **1 (LOW): fixed in the film driver** - the signing and election pins stand on the desk again, set explicitly where the re-seat used to put it (`SetPrivateField(controller, "_onDesk", true)`); the re-seat stays as a guard, so `InvokeOneArg` keeps its caller. The -shotstates world after 93c is the film's own now - no scenario's turns or deltas - and its frames are this commit's dry film (`dry825`, with `-shotstates`).
- **2 (LOW): fixed** - the load's warning reads the save's game-over: a run whose verdict was closed "had ended, and stays over"; any other loads as free play. Whether such a run should reopen is Elias's, if it ever matters (no player holds such a save).
- **3 (LOW): fixed** - the id is read as a string or nothing (`as JValue ... as string`), never cast.
- **4 (LOW): fixed** - `SaveLoadRoundTripDiagnostic`'s file-and-version exercise plants a retired scenario's id under the UI layer and asserts it is named, and that a free-play save's null scenario and a batch save's absent layer name nothing (passing: `rt825`); the "Nor for GA-0-14 / GA-0-40" note stands beside `CurrentSaveVersion` with what such a save keeps.
- **5 (LOW): reworded as history** - the multiplier's doc (`SimulationManager.cs`, a comment-only change - the review's own fix, given its row: the ledger check walks committed history, and an uncommitted state has none to inherit from), the pacing lever's (`ForeignPolicySystem.cs`), the invariants' restart and the contrast check's caller list. **Left**: `DebtLedgerRecorder.cs` and `DebtAttribution.cs` are grandfathered money files whose first change of any kind owes a review - their prose waits for one; `PoliSimWidgets.cs:341` and `StatTracePanel.cs:100` already read as history. The property itself stays (the planner's advice: a money-path file, cited by `TradeCosts.cs`).
- **6 (LOW): fixed** - the protocol keeps Elias's words with a bracketed note at both lines; the inventory's rows (F-14, F-111 to F-113, INV-S4, SD-49) carry the retirement; the two plan headers say so; Phase 4's own plans' scenario runs are commented out with the reason, as the standing plans' were. **Left**: `ScenarioCandidateMeasurementDiagnostic` and `ItalyDebtMeasurementDiagnostic` measure candidate economies, depend on nothing retired and sit in no bar - kept, named in the record.
- **7 (LOW): done** - §825 written, `Tools/audit/p4_scen.plan` committed, the real film taken (`film825`, frames 01 and 01g on the Canvas selector). `TitleTop` stays 28: the band above the grid is Design's to judge.
