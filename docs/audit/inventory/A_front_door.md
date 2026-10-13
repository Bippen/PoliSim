# Gameplay audit, Phase 0: slice A, the front door, the variants and the harness entry points

Read-only inventory of `G:\UNITY\Projects\PoliSim` as it stood on 2026-10-13. Nothing in the repo was edited, no Unity or `dotnet` was run, and git was used read-only (it was not needed).
Paths are repo-relative. `GC` means `Assets/Scripts/UI/GameController.cs` and `SM` means `Assets/Scripts/Simulation/SimulationManager.cs`. A line number is the line as read today.
**INFERRED** marks anything I reasoned out but did not verify by reading. **SUSPECTED** marks a lead for a run, not a finding.
Three read-only forks of this inventory worked in parallel and are named by number: fork 1, save/load and determinism; fork 2, the harness precedents; fork 3, the scenarios and run end. Their line references are their own reading. Where a row says "spot-checked" or "re-read by me", I read the cited lines myself.

**Eight things to read first**
1. **The game cannot be driven headless through its own front door as it stands.** Every takeover screen runs a small state machine, the seam (`AdvanceCanvasSeam`). The seam ticks only inside `OnGUI`, on the Layout event (`GC:2563-2566`). Under `-batchmode`, Unity delivers no `OnGUI` events (`Assets/Scripts/Testing/DryGuiPass.cs:18-23`). Three things go wrong without it:
   - the main menu never appears;
   - a signing ceremony never closes, so the first passed or failed bill holds the clock forever (`GC:747`, `GC:1811-1834`);
   - an election night never closes, so `ApplyElectionVerdict` never runs and the clock holds (`GC:732`, `GC:1900-1908`).

   An audit harness must either pump `OnGUI` (the dry film's `DryGuiPass` does this, through reflection into Unity internals) or reach into private state.
2. **The UI's handlers are all `private`, inside a `MonoBehaviour`.** The `SimulationManager` verbs they call are `public`. One exception: appointing the central bank's head writes the model directly from the IMGUI button (S-12). Many handlers live inline in `OnGUI` with no method at all, such as the speed chips. There are no asmdefs and no `InternalsVisibleTo`, so Editor code reaches `GameController` only by reflection. See section 6.
3. **Which elections a run can hold.** Only Sweden (every fourth September), Germany (one snap election on 23 Feb 2025, then none) and Poland (one per Sejm term) hold an election inside a run (`WorldClock.TryNextPollingDay`, `Assets/Scripts/Elections/WorldClock.cs:548-580`). Italy, the USA and France never vote. Nothing but a scenario verdict ends a run there.
4. **Created parties exist only in Sweden** (`PartyCreationFlow.Offered`, `Assets/Scripts/UI/PartyCreationFlow.cs:25`). The two presidential start cards (Poland 2025, France 2022) are LOCKED. **The USA cannot be played as the president**: no US election is held, and the record seats the president.
5. **There are 2 challenge scenarios, not 3** (`ScenarioLibrary.All`, `Assets/Scripts/Data/ScenarioLibrary.cs:41-45`). The 3 appears only in the audit brief (`docs/audit/AUDIT_PROTOCOL.md:35`). No spec or feature row names any scenario.
6. **Seeding traps.**
   - `-seed=` is read only by `SimulationTestRunner`, and `-skipsimulationtestrunner` returns before the seed is read (`Assets/Scripts/Testing/SimulationTestRunner.cs:80-110`).
   - In the Editor, the runner plays a whole validation run on the same static random streams before the player's game exists.
   - A harness must seed by itself, and must state whether the runner ran.
7. **Real time paces the day loop.** At 1× a day takes one real second; at 3× it takes 0.25 s (`GC:321-330`, `GC:768-772`). The clock also holds on every interrupt until something answers it. An "idle" player who governs stops at the first budget window with default settings (`GameSettings.cs:196-197`).
8. **Determinism and save/load.**
   - **Speed.** Speed changes only real seconds per game day. The one 1× vs 3× route found is a scenario verdict that lets an extra day run (S-05).
   - **Save → load → continue.** Two suspected divergence routes: the cabinet shortlist is unsaved and its Search draws on the simulation's Cabinet stream (S-24), and dictionary order changes after a JSON round trip (S-25).
   - **Statics** are shared by every world in a play session. `EventSystem.Enabled` can be left false by two Editor harnesses (S-26).
   - **Saves** use format 38, with no migration. Every save path writes the player's real saves folder.
   - Details: section 7 and Annexes A-C.

---

## 1. Feature table

Status values: BUILT, PARTIAL, NOT BUILT (planned, named in a spec or row), SPEC-ONLY (specified as built or required but absent in code), CODE-ONLY (in code, named by no spec, feature row or § I could find).

| ID | feature | player-facing entry (screen/control → method) | code (file:symbol) | spec authority | status | countries | notes |
|---|---|---|---|---|---|---|---|
| A-01 | Main menu: Continue, New game, Load, Settings, Quit | First takeover → `MainMenuScreen` button → `GC.ChooseMainMenu` (private, `GC:1293`); applied under the cover by `GC.ApplyMainMenuChoice` (private, `GC:1310-1341`) | `Assets/Scripts/UI/MainMenuScreen.cs:67-174`; seam entry `GC:1752-1773`; exit `GC:1866-1869`, `GC:1884-1893` | SP spec §9.1; rows MM-1 (§614), MM-3 (§626) | BUILT | all | CONTINUE is drawn only when a compatible save exists (`GC:1755`, `GC:1299-1307`). The whole machine ticks from `OnGUI` (`GC:2563-2566`). If the menu fails to build, the selector comes first (`GC:1764-1767`) |
| A-02 | Continue | CONTINUE → `_pendingLoadPath = NewestCompatibleSavePath()` (`GC:1327-1331`) → `Update`'s safe point → `GC.LoadFromPath` (`GC:718-723`, `GC:864-885`) | `GC.NewestCompatibleSavePath` `GC:1299` | SP §9.1 | BUILT | all | INFERRED: autosaves count as "the most recent save". `ListSaves` reads the same directory autosave writes to (`GC:1416`) |
| A-03 | Load game | Main menu LOAD GAME → `OpenSavesMenuFromMainMenu` (`GC:1344-1352`), or the desk's SAVES chip → `OpenSavesMenu` (`GameController.Desk.cs:395-398`, `GC:1098-1106`); Load row → `_pendingLoadPath` (`GC:1179-1191`) → `LoadFromPath` → `RestoreFromSave` (`GC:893-926`) | `Assets/Scripts/Persistence/SaveGameService.cs` (Annex B) | SP §9.1; the item 8 menu pass; MM-1 | BUILT | all | A load always resumes PAUSED (`GC:1075`). In a running game, "Replace unsaved game?" asks for confirmation (`GC:1176-1191`) |
| A-04 | Save (named) | Saves screen Save → `GC.SaveToPath` (private, `GC:840-860`, `GC:1150-1156`) | `SaveGameService.CreateSaveGame` / `SaveToFile` | the item 8 menu pass | BUILT | all | The save row is drawn only in a running game (`GC:1140`) |
| A-05 | Quicksave and quickload (F5/F9) | Keyboard F5/F9 → `DebugSaveGame` / `DebugLoadGame` (`GC:702-714`, `GC:828-836`) | slot path `SaveGameService.DefaultSlotPath` | none (`GC:694-699`: "the TEMPORARY debug entry point") | CODE-ONLY (debug), BUILT | all | Gated off while a takeover is live (`GC:703-704`). Named to the player on the saves screen (`GC:1165`, `GC:1222`) |
| A-06 | Autosave | Per day in `Update` → `GC.AutosaveIfDue` (`GC:801`, `GC:1411-1422`) → `SaveToPath(autosave_N.json)` | `GameSettings.AutosaveDue` / `AutosaveSlot` (`GameSettings.cs:182-187`) | SP §9.2; MM-2 (§615) | BUILT | all | Default OFF: 0 days (`GameSettings.cs:198`); 3 slots (`:199`). Counted in days played, not frames. See section 5 S-07 and S-08 |
| A-07 | Settings screen | SETTINGS (menu) or the masthead chip → `GC.OpenSettings` (`GC:1374-1378`); `DrawSettingsScreen` (`GC:1444`); `CloseSettings` (`GC:1364-1368`) | `PreferenceKeys.All` (`GameSettings.cs:20-33`): volume, mute, † provenance, display mode/width/height, default speed, campaign hold, budget hold, autosave days, autosave slots | SP §9.2; MM-2 (§615), board 17b (§626) | BUILT | all | Stored in PlayerPrefs, so they are machine-global and outside the save. They change the trajectory (the holds, A-34) and so must be pinned by any harness (`Assets/Scripts/Testing/PreferencePins.cs`) |
| A-08 | Display settings | Settings → `DisplaySettings.Set` / `Apply` (`GameSettings.cs:83-105`) | four geometries (`GameSettings.cs:62-65`) | SP §9.2 | BUILT | all | no UI-scale slider, by spec |
| A-09 | Settings named but not built: language, colour-vision modes, key rebinding | — | — | SP §9.2 "Named, not built" | NOT BUILT (by design) | — | — |
| A-10 | The election-night hold as a setting | — | `GameSettings.cs:120-123`: "election night is a takeover and always holds, so it is not offered" | SP §9.2 lists "the campaign opening, election night, budget windows" | PARTIAL (stated deviation) | — | — |
| A-11 | Quit; leaving a game | Menu QUIT → `ApplyMainMenuChoice` (`GC:1332-1338`) | — | SP §9.1; PS §7 ("when the player ends it") | PARTIAL | all | There is no in-game quit, return to menu, or "end the run" verb. `ResetPlayerCountrySelection` exists for the harness only (`GC:2255-2271`) |
| A-12 | Country selector (folders) | seam builds `CountrySelectorScreen.Build(_world, SelectPlayerCountryAndParty, ScenarioLibrary.All, OpenScenarioPartyPick)` (`GC:1775-1805`) | `Assets/Scripts/UI/CountrySelectorScreen.cs:52-183`; a folder → `ShowStartPanel` (`:177`) | SP §1.2; PS §3; SP-1 (§622), SP-2b (§626) | BUILT | all six | The selector's world is built on the default epoch at `Start`. The pickers read the start's own chamber (`WorldClock.PickerViewOf`, `WorldClock.cs:704-722`) |
| A-13 | Start points: cards, PLAYABLE or LOCKED with a reason | the folder's sheet → a card selects → brass SELECT → `ShowPartyPanel(chosen, onSelect)` (`CountrySelectorScreen.cs:388-396`, `:399-530`, SELECT at `:524-529`) | `StartPoints.For` (`Assets/Scripts/Elections/StartPoints.cs:60-96`) | SP §1.1-1.2, decisions 1, 2, 11; SP-1 (§622) | BUILT | all six; LOCKED: Poland presidential, France presidential | SELECT does not pass on which card was chosen. The world always opens on `WorldClock.StartDate(country)` (`GC:2166`). Section 5 S-01 |
| A-14 | The start brief (derived) | the selected card → `BuildBriefLedger` (`CountrySelectorScreen.cs:775+`) | `StartBrief.Head` / `Rows` / `Text` (`Assets/Scripts/Elections/StartBrief.cs:154-243`) | SP §1.3; SP-2 (§623) | BUILT; the tagline is PARTIAL | every playable start | `StartBrief.Tagline` returns null (`StartBrief.cs:243`): "the tagline slot empty until one is reviewed" (§623) |
| A-15 | Party pick | party panel row → `onSelect` = `GC.SelectPlayerCountryAndParty` (private, `GC:2111-2115`) → `SeatPlayerParty` (`GC:2118-2126`) → `SelectPlayerCountry` (`GC:2203-2252`) | Pickable = `PartySystems.IsPlayable` (`Assets/Scripts/Data/PartySystem.cs:813-817`): seated at the start, OR seats at the latest election, OR a CHES position | PS §5.1; §618 ruling 2; CL-2 / R-CL1 | BUILT | all six | PS §5.1 says "The party's leader of record at the start is the player's character". The roster carries the seated election's leaders; leaders by date are not in code (§618, "Deviations stated"). PARTIAL on that clause |
| A-16 | New game: the world opens on the chosen country's start | via A-15 → `GC.OpenWorldAt` (`GC:2156-2201`), which runs `SimulationManager.SetEpoch`, `WorldFactory.CreateDefault`, `SetWorld`, `ResetClockToEpoch`, a new `ShadowBaseline` and `PolicyImpactLedger`, and `GovernmentRecord.AtStart` | `SM:240-264`, `SM:2472-2477`; `Assets/Scripts/Data/WorldFactory.cs:1092` (every country's government at the epoch) | PS §3, §9 stage 1; PS-1 (§618), PS-3a (§628) | BUILT | all six | `OpenWorldAt` refuses a running game (`GC:2158-2164`). One world per `GameController` per play session (section 6) |
| A-17 | IMGUI fallback selector | `DrawCountrySelector` (`GC:2334-2442`). Live only when the Canvas selector fails to build (`GC:1795-1799`, `GC:2614-2620`) | `CountrySelectorScreen.cs:55-59` (a missing folder sprite) | none | CODE-ONLY (the degradation path) | all | It skips the start sheet and offers no CREATE A PARTY. It still reaches the scenario pick |
| A-18 | Create a party: five steps | party panel CREATE A PARTY (only where `PartyCreationFlow.Offered`) → `GC.RequestPartyCreation` (`GameController.PartyCreation.cs:32-36`) → under the cover `OpenPartyCreation` (`GC:1895`; `GameController.PartyCreation.cs:39-47`) → IMGUI steps → START → `CommitPartyCreation` (`:60-69`) → `PartyCreationFlow.TryCommit` (`PartyCreationFlow.cs:179-184`) → `CreatedParties.TryRegister` (`Assets/Scripts/Data/CreatedParty.cs:153-172`) → the world rebuilt (`_worldRosterStale`) → `SelectPlayerCountryAndParty` | `CreatedParty` / `PartyOrigins` / `CreatedParties` (`CreatedParty.cs`) | SP §2.1-2.8, S3, S4; SP-3 (§671, §675, §681), SP-4 (§676), §679 | PARTIAL | Sweden only | Open per the feature list: D-CP, SP-4b, ballot access, blocs, and a Swedish election end to end (row SP-3). Not found in code, each SPEC-ONLY in section 3: the Advanced CHES sliders (SP §2.3), the Splinter's day-one MPs (§2.2; `CreatedParty.cs:50` says "no seats of its own"), and the parent declaring against its Splinter (§2.2) |
| A-19 | Created parties in the other five countries | — | `PartyCreationFlow.cs:25` (`country == CountryId.Sweden`) | SP §3, S5; SP-5 | NOT BUILT | DE, PL, IT, US, FR | — |
| A-20 | The USA: your own nominee (US-33 / SP-6a) and a third-party run (SP-6b) | — | — | SP §2.6, decision 10; PS §6; rows PS-6+SP-6 and SP-6b | NOT BUILT (PS-6 PARKED, §803) | USA | — |
| A-21 | Presidential starts (Poland 2025 S7, France 2022 S8) | the LOCKED card reads "THE PRESIDENTIAL START IS NOT YET BUILT" | `StartPoints.cs:56`, `:75-78`, `:87-90` | SP §1.1, S7-S8; rows SP-7, SP-8 | NOT BUILT | PL, FR | The game holds Poland's 2025 presidential election itself, as the AI's (`SM.HoldPresidentialRound`, `SM:481`; §770) |
| A-22 | Challenge scenarios (2) | selector strip "Scenario: {Name}" (`CountrySelectorScreen.cs:137-156`, `:900-912`) → `GC.OpenScenarioPartyPick` (`GC:2133-2138`) → party panel (no CREATE) → `StartScenarioAsParty` (`GC:2141-2146`) → `StartScenario` (`GC:2038-2068`). Judged at each turn boundary by `CheckScenarioObjectives` (`GC:2076-2089`, called `GC:6137`); verdict screen (`GC:2636-2640`); `DismissScenarioVerdict` → game over (`GC:2094-2104`) | `ScenarioLibrary.cs:41-45`; `Assets/Scripts/Simulation/ScenarioEvaluator.cs:51-170` | `COMPLETED.md` §21, §30 (R-S3a-f), §36, §492; `AUDIT_PROTOCOL.md:35` says "three" | BUILT (2); CODE-ONLY against today's specs | SE ("Inherit the Fund", 12 turns), IT ("Italy Debt Crisis", 30 turns) | Saved through `UiDraftState.Scenario` (`GC:970-971`, `GC:1045-1061`). The scenario's party pick offers every playable party, so the player may not govern (section 5 S-15) |
| A-23 | Who governs, stored; the role chip | Desk masthead role chip (`GameController.Desk.cs:430-434`) | `Country.Government`; `GovernmentRecord.RoleOf` (`Assets/Scripts/Elections/GovernmentRecord.cs:237-244`), `AtStart` (`:252-273`), `WhatIfGoverning` (`:280-288`); `SM.PlayerGoverns` (`SM:2632-2642`) | PS §5.2; PS-3a (§628), PS-3b (§629), PS-3d (§631) | BUILT | all six | `PlayerGoverns` is true for a player country with NO party seated, which is the tools' case (`SM:2638-2640`) |
| A-24 | Levers gated by role | every Introduce* site draws `DrawLeverLock` | `SM.PlayerMayIntroduce` (`SM:2650-2659`, `:2670-2690`) | PS §5.2; PS-3c (§630), PS-3g (§634), §755 | BUILT | all | A junior partner holds its portfolios' levers, and Finance acts through the fiscal-stance dial only (`SM:2679-2683`) |
| A-25 | Junior partner: portfolios, the Finance stance, leaving | Budget page dial → `SM.SetFinanceStanceTarget` (`SM:2695`); LEAVE THE GOVERNMENT → `SM.LeaveGovernment` (`SM:2877`) | `GovernmentRecord.AllocatePortfolios` (`GovernmentRecord.cs:149-201`) | PS §5.2; §634, §706, §755 | BUILT SE/DE | SE, DE (a German start seats Grüne and FDP as partners) | The LEAVE row is drawn only where confidence rules are sourced (fork 3: `GameController.ParliamentRows.cs:702`, `:787-798`) |
| A-26 | Support party: agreement items owed, delivered, broken; withdrawal | `SM.WithdrawSupport` (`SM:2801`) | `SupportAgreement`; `GovernmentRecord.FormAgreements` (`GovernmentRecord.cs:117-121`) | PS §5.3; PS-3h (§635), PS-3i-2a (§644, §655) | BUILT | SE (SD at the start) | The AI supporter's no-confidence motion is unreachable on Sweden's calendar "as ruled" (feature row PS-3i-2a, §655) |
| A-27 | Opposition: alternative budget, motions | `SM.TableShadowBudget` (`SM:1189`); `SM.MoveNoConfidence` (`SM:2828`) | `WorldClock.WeighsAlternative` (`WorldClock.cs:368`) | PS §5.2; PS-3e (§632-633), §636, §698 | BUILT where sourced | alternative budget: SE only; motions: SE, DE | elsewhere refused with the reason (fork 3) |
| A-28 | The AI governs the player's country when the player does not lead | — (`GC.AdvanceTurn` routes `PolicyDecision.None`, `GC:6105`) | `SM.TableGovernmentBudget` (`SM:1170-1185`) | PS §5.2; §628, §632 | BUILT | all | — |
| A-29 | Formation after an election: Speaker's round or chancellor's election, formation sheet, offers | Night CONTINUE · FORM A GOVERNMENT; formation sheet TABLE IT → `SM.SubmitFormation` (`GameController.FormationSheet.cs:366-370`; `SM:4107`); `PassFormation` (`SM:4138`); `AnswerOffer` (`SM:4153`) | `SM.RoundsApply` (`SM:3234-3236`); `FormationHolds` (`GC:1400-1404`) | PS §5.3 premises 1-9; §646, §647, §656, §705 | BUILT SE/DE; PARTIAL PL | SE, DE; PL installs the formation's result that night (`GC:6429`) | — |
| A-30 | Confidence, collapse, caretaker, extra election | Annex C (from fork 3) | `Assets/Scripts/Elections/ConfidenceProcedure.cs:34` (`RulesOf`: Riksdag, Bundestag, else Unsourced) | PS §5.4; §636, §640, §641, §698 | BUILT SE; PARTIAL DE; NOT BUILT PL, IT | — | Germany has no Art. 68 and no dissolution (fork 3) |
| A-31 | The USA as played today | REP or DEM from the picker | `WorldClock.NoElectionYet` / `RecordSeatsChamber` / `RecordSeatsExecutive` (`WorldClock.cs:108-116`); `SM.SeatTheRecordOnItsDate` (`SM:3995-4017`) | PS §6; PS-6 (US-1 §782, US-2 §783) | PARTIAL (the record only) | USA | Read, not run: `SM:4012-4016` with `GovernmentRecord.AtStart`: a DEM player governs from the start (2024-03-12) to 20 Jan 2025, then the record seats Trump and DEM is in opposition; a REP player the reverse. The feature row says "PLANNED 2026-10-04, nothing built", although US-1, US-2, US-3, US-4, US-5, US-10, US-11, US-12 and US-20 have § records (§782-§802). Section 5 S-20 |
| A-32 | The day and turn loop | none (time passes by itself) | `GC.Update` (`GC:692-816`) → `SM.AdvanceDay` (`SM:312-485`) → `SM.AdvanceCountryDayTick` (`SM:296-310`) → `GC.QueueNewlyResolvedDivisions` (`GC:2280-2301`) → at a boundary `GC.AdvanceTurn` (`GC:6098-6138`) → `SM.AdvanceTurn` (`SM:4813`) → on polling day `GC.CheckElection` (`GC:6349-6367`) → `AutosaveIfDue` | `SM.DaysPerTurn = 365` (`SM:216`) | the continuous-time migration (record); PS §3 | BUILT | all | Annex A and section 7 list the per-frame hazards |
| A-33 | Game speed: Pause / 1× / 2× / 3× | masthead chips 1×/2×/3× (`GameController.Desk.cs:364-365`, `:411-421`); rail PAUSE/RUN (`GC:5454-5498`); default speed from settings at a new game (`GC:1381`, `GC:2241`) | `GameSpeed` and `GetSecondsPerDay` (`GC:314-330`): 1, 0.5 and 0.25 real seconds per day | SP §9.2 (the default speed) | BUILT | all | The speed is not restored on load: the recorded value is "data for a future load UI" (`GC:979-983`, `GC:1075`) |
| A-34 | Interrupts that hold the clock | each with its own screen (the list is in Annex A) | `GC:727-766`, re-checked after every day `GC:807-814` | the record (the Fed-chair, cabinet, foreign-policy and budget holds); SP §9.2 (two of them configurable) | BUILT | all | The campaign-opening and budget-window holds are settings, defaulting ON (`GameSettings.cs:196-197`) |
| A-35 | The election on its polling day | none (the calendar) | `SM.PollingDayToday` (`SM:479`, `SM:2620`); `WorldClock.TryNextPollingDay` (`WorldClock.cs:548-580`) | PS §3 and decision 3 ("every country holds its own elections on its own real calendar"); PS-2/CL-4 (§619); PS-8 | PARTIAL | the player's country only, and only SE, DE (once) and PL | AI countries hold no elections (PS-8 is open). Only the player's country votes (`SM:479`) |
| A-36 | Election night takeover | built in `CheckElection` → `ShowElectionNight` (`GC:6492+`, `ElectionNightScreen.Build` at `GC:6617`); the Canvas CONTINUE → `ElectionNightScreen.Dismiss` (public, `ElectionNightScreen.cs:99`, listeners at `:1326`, `:1365`), which `DismissElectionNight` (`GC:6706-6709`) also calls | `ElectionNightFromModel.Available` (`Assets/Scripts/Elections/ElectionNightFromModel.cs:38-39`): Sweden and Germany only | the elections spec; D-DE (§707); D-PL | PARTIAL | SE, DE; PL has no night (row D-PL "not yet built") | Where there is no night the verdict lands at once (`GC:6361-6366`) |
| A-37 | Signing ceremony per resolved division | Canvas takeover → SIGN → `GC.SignPendingDivision` (`GC:2315-2318`) → `SigningScreen.Sign` | queue filled only by the day loop (`GC:787`, `GC:2280-2301`) | the record (the Canvas pilot) | BUILT | all | It holds the clock (`GC:747`) until signed |
| A-38 | Game over and run end | — | Annex C (from fork 3) | PS §7; CLAUDE.md (the world-clock rule) | PARTIAL | — | — |

---

## 2. The variant matrix

**Day-one government.** It comes from `WorldClock.Governments` (`WorldClock.cs:371-426`), read through `SeatedGovernment.TryAt` (`Assets/Scripts/Elections/SeatedGovernment.cs:57-75`) and `GovernmentRecord.AtStart` (`GovernmentRecord.cs:252-273`). A record that names its cabinet is INSTALLED. One that does not is replaced by the formation's provisional stand-in.

**The player's role.** It is `GovernmentRecord.RoleOf` (`GovernmentRecord.cs:237-244`): the prime minister's party, then cabinet members, then support, then everyone else as opposition.

**Pickable parties.** `PartySystems.IsPlayable` against `WorldClock.PickerViewOf(id).Seats` (`CountrySelectorScreen.cs:298-300`).

**Start dates.** From `WorldClock.StartDate` (`WorldClock.cs:223-233`), as §618 measured them: SE 2026-01-18, PL 2023-02-19, US 2024-03-12, DE 2024-11-06, IT 2022-07-21, FR 2024-07-18.

**The instrument for every cell.** `GC:2236` logs `ROLE: <country> at <date> - … the player's <party> is <role>` on every new game. Phase 1 should assert each cell against that line.

| country | start (card) | card | opens | head of government (PM's party) | coalition (junior) partner | support party | opposition | created party | elections inside a run |
|---|---|---|---|---|---|---|---|---|---|
| Sweden | Riksdag election 13 Sep 2026 | PLAYABLE (`StartPoints.cs:66`) | 2026-01-18, run-up | **STARTABLE**: M (Kristersson M+KD+L, SD support, INSTALLED; `WorldClock.cs:379`; §628 measured it) | **STARTABLE**: KD, L | **STARTABLE**: SD | **STARTABLE**: S, V, C, MP | **STARTABLE**: Opposition on day one with 0 seats (`CreatedParty.cs:50`; `RoleOf` returns Opposition for a party outside the record) | 13 Sep 2026, then every fourth year (`WorldClock.cs:572-579`); extra elections by RF 6:5 / 6:7 (fork 3) |
| Germany | Bundestag election (snap) 23 Feb 2025 | PLAYABLE (`StartPoints.cs:69`) | 2024-11-06, the snap trigger | **STARTABLE**: SPD (Scholz SPD+Grüne+FDP, INSTALLED; the record runs it to 2024-11-07, `WorldClock.cs:385`) | **STARTABLE**: Grüne, FDP. The FDP is never removed (section 5 S-04) | **n/a at the start**: the record names no supporter, so no pick yields Support. Not a defect | **STARTABLE**: CDU, CSU, AfD, Linke, BSW (0 seats, pickable through CHES, §618 ruling 2), SSW | **NOT BUILT** (`PartyCreationFlow.cs:25`) | 23 Feb 2025 only, then none (`WorldClock.cs:551-557`); the chancellor's election (§705); the constructive vote (§698) |
| Poland | Sejm election 15 Oct 2023 | PLAYABLE (`StartPoints.cs:72`) | 2023-02-19, run-up | **STARTABLE**: PiS (Morawiecki; cabinet DERIVED as PiS alone, INSTALLED; `WorldClock.cs:392`) | **n/a at the start**: PiS governs alone on record. Reachable after the 2023 election if the night's formation seats the player's party (`GC:6429`) | **n/a at the start**: no supporter on record. Possible after 2023 if the formation's view returns supporters (`GovernmentRecord.FromView`, `GovernmentRecord.cs:297`) | **STARTABLE**: every other pickable party; §618 names PSL, SLD and MN as pickable | **NOT BUILT** | 15 Oct 2023, then each term (17 Oct 2027 …; `WorldClock.cs:558-571`); the AI's presidential election 2025 (§770); no confidence rules (`ConfidenceProcedure.cs:34`) |
| Poland | Presidential election 2025 | **LOCKED**: "THE PRESIDENTIAL START IS NOT YET BUILT" (`StartPoints.cs:76-78`) | — | NOT BUILT | — | — | — | — | — |
| Italy | General election (snap) 25 Sep 2022 | PLAYABLE (`StartPoints.cs:81`) | 2022-07-21, the dissolution | **STARTABLE, PROVISIONAL**: the party leading the formation's stand-in on the seated chamber. Draghi's cabinet is a GAP (`WorldClock.cs:398`). Because of E-47 the seated chamber is the 2022 table (`WorldClock.cs:275-276`, `:319-323`). INFERRED: FdI leads a FdI+Lega+FI+NM stand-in. Not verified; read the ROLE line | STARTABLE, PROVISIONAL: the stand-in's other cabinet parties (INFERRED) | INFERRED: none | **STARTABLE**: every other pickable party | **NOT BUILT** | **none**: `TryNextPollingDay` is false (`WorldClock.cs:572`), although the card says SNAP ELECTION (section 5 S-02) |
| USA | Presidential election 5 Nov 2024 | PLAYABLE, "NO ELECTION YET" (`StartPoints.cs:84`, `:126`) | 2024-03-12, run-up | **STARTABLE as "governing party", not as president**: DEM (Biden's presidency, INSTALLED; `WorldClock.cs:406`). From 20 Jan 2025 REP governs by record (`SM:4012-4016`; read, not run) | n/a | n/a | **STARTABLE**: REP at the start (§629 filmed "the player's REP is Opposition") | **NOT BUILT** (US-33, SP-6b) | **none** (`WorldClock.cs:108`); the House seated by record on 3 Jan 2025, the president on 20 Jan 2025 (`SM:3995-4017`) |
| France | Presidential election Apr 2022 | **LOCKED** (`StartPoints.cs:87-90`) | — | NOT BUILT (S8) | — | — | — | — | — |
| France | Legislative election (snap) 2024 | PLAYABLE, governing mode, "WHAT-IF · YOUR PARTY GOVERNS" (`StartPoints.cs:91`, `:125`) | 2024-07-18 | **STARTABLE for every pickable party**: `GovernmentRecord.WhatIfGoverning` seats the player's party as a one-party cabinet (`GC:2232-2235`; `GovernmentRecord.cs:280-288`) | NOT BUILT: the what-if is one party | NOT BUILT | **NOT BUILT**: no French pick yields opposition | NOT BUILT | **none** (`WorldClock.cs:101`, `:572`) |

**Roles reached after day one.** These apply in countries that hold an election:
- **Sweden.** The Speaker's round can make the player's party PM, partner, supporter or opposition (`SM:3477`, `SM:4071` per fork 3; formation verbs A-29). The four roles are reachable both at the start and after the election. Playing each for a full term is open (row PS-3: "STILL OPEN: play").
- **Germany.** The chancellor's election after 23 Feb 2025 decides the role. The constructive vote can change it mid-term.
- **Poland.** The night installs the formation's government (`GC:6429`). There is no round, no confidence vote, and no LEAVE row (fork 3 #14).

**Length cells for Phase 2:**
- "To the first election": SE, DE, PL only.
- "Two cycles": SE and PL only (DE stops after one).
- "30 years": runs anywhere, but holds elections only in SE and PL.

**Outcome cells:** see Annex C (from fork 3). Term limits are NOT BUILT anywhere for the player.

---

## 3. Specified but missing

Each item is a ledger row of type "spec". Each is NOT BUILT or SPEC-ONLY, with its authority.

| # | what | authority | state in code |
|---|---|---|---|
| M-01 | Every country holds its elections on its own calendar from the start date, so history diverges for all six | PS §3, decision 3; row PS-8; §618 ruling 4 | Only the player's country votes (`SM:479`), and only SE, DE (once) and PL have a calendar (`WorldClock.cs:548-580`). NOT BUILT (PS-8 open) |
| M-02 | The USA: the player as nominee and president; the state-by-state campaign; the Senate; midterms; the term limit (R-US6 (b)); the veto | PS §6; `USA_STAGE_PLAN.md` US-7…US-36; rows PS-6+SP-6, D-US | NOT BUILT; PARKED by H5 (§803). A US run holds no election (`WorldClock.cs:108`) |
| M-03 | Italy: the Rosatellum's sub-national stages, the Senato, confidence in both chambers, an election from the snap start | PS §4, §5.4, §9 stage 7; row PS-7 | NOT BUILT. The card still promises the snap election (section 5 S-02) |
| M-04 | Poland's and France's presidential starts (the player as candidate) | SP §1.1, decision 11, S7, S8; rows SP-7, SP-8 | NOT BUILT: LOCKED cards (`StartPoints.cs:56`) |
| M-05 | Created parties in every country | SP §3 ("a created party works wherever the country's elections work"), S5; row SP-5 | NOT BUILT outside Sweden (`PartyCreationFlow.cs:25`) |
| M-06 | Created party: ballot access as a run-up task; funding by each country's real rule | SP §2.7, decision 9; row SP-3 ("OWED: ballot access (1 500 signatures), blocs, a Swedish election end to end") | NOT BUILT, by the row's own words |
| M-07 | Splinter origin: "may take a number of the parent's MPs into the chamber on day one"; "the parent declares against it" | SP §2.2 (origin 02) | SPEC-ONLY. A created party has "no seats of its own (a created party enters the chamber by election)" (`CreatedParty.cs:50`). The created-party declarations are the created party's own (`DeclaredRedLines.cs:595-620`). No code reads a Splinter's `ParentKey` for a red line, although the origin card's text promises it (`CreatedParty.cs:94`) |
| M-08 | Positions: "an Advanced panel adjusts each" of the other CHES dimensions | SP §2.3 | Not found: grep "Advanced" in the flow's files has no hit. SPEC-ONLY (Design's D-CP / SP-4b are open) |
| M-09 | The player ends the run | PS §7 ("when the player ends it") | No in-game verb (fork 3); the main menu exists only before a country is chosen (`GC:1752`) |
| M-10 | A run as a career, "measured by elections won, seats, years in each role, agreement items delivered, and the country's outcomes" | PS §7 | No career or run-summary code (fork 3's grep). SPEC-ONLY |
| M-11 | Confidence and collapse by each country's rules: Germany's chancellor's confidence question; Poland's constructive no-confidence; Italy in both chambers | PS §5.4 | `ConfidenceProcedure.RulesOf` is Riksdag, Bundestag, or Unsourced for the rest (`ConfidenceProcedure.cs:34`); no Art. 68 (fork 3). NOT BUILT |
| M-12 | Germany's snap start "with Scholz's minority in office" | PS §4 table text; §9 stage 4 | The start seats SPD+Grüne+FDP (the record's cabinet on 2024-11-06, `WorldClock.cs:385`). Nothing seats the minority on 2024-11-07, because record seating is USA-only (`WorldClock.cs:112`, `:116`). Known: §628's reviewer verified the day-one read |
| M-13 | The leader of record at the start is the player's character | PS §5.1 | Leaders by date are not in code; the roster carries the seated election's leaders (§618, "Deviations stated") |
| M-14 | Each of the four roles played for a full term in Sweden | PS §9 stage 3 done-when; row PS-3 ("STILL OPEN: play") | the machinery is BUILT; the done-when is open |
| M-15 | Election night for every country with an election | elections spec; rows D-PL, D-IT, D-US | SE and DE only (`ElectionNightFromModel.cs:38-39`); Poland's waits on D-PL ("not yet built") |
| M-16 | Settings: language, colour-vision modes, key rebinding | SP §9.2 "Named, not built" | NOT BUILT, by design |
| M-17 | The election-night hold as a choice | SP §9.2 lists election night among the holds a player chooses | Deviation stated in code (`GameSettings.cs:120-123`) |
| M-18 | "The three challenge scenarios" | `AUDIT_PROTOCOL.md:35` (Phase 2 variants) | 2 in code (`ScenarioLibrary.cs:41-45`). §36 says "`ScenarioLibrary` stays at two" (fork 3: `COMPLETED.md` 2512-2515). Fork 3 found no re-measurement after §198's return trigger. A brief-vs-code mismatch, for Elias |
| M-19 | The Germany and Poland start cards and briefs promise "the run-up" or "the snap election" | SP §1.2-1.3 | these hold. Italy's does not: section 5 S-02 |

## 4. Present but unspecified

| # | what | code location | note |
|---|---|---|---|
| U-01 | Two challenge scenarios, "Inherit the Fund" (SE) and "Italy Debt Crisis" (IT) | `ScenarioLibrary.cs:41-45`; entry `GC:2133-2146`, `GC:2038-2068` | Named only in `COMPLETED.md` (§21, §30, §36, §492, §598, §718), not by any spec or feature row (fork 3) |
| U-02 | The IMGUI fallback selector, with no start sheet and no CREATE A PARTY | `GC:2334-2442` | The degradation path when the Canvas selector fails (`GC:1795-1799`) |
| U-03 | F5 quicksave and F9 quickload | `GC:694-714`, `GC:828-836` | "TEMPORARY debug entry point" |
| U-04 | Harness-only UI states: the instrument ladder; the campaign stages "(harness only)" | `GC:2658-2665`, `GC:5529`; `GC:2765-2790` | Reachable by the capture driver only. They matter to area 10 (every UI state on the dry film) |
| U-05 | Harness-only controller entries | `ResetPlayerCountrySelection` (`GC:2267-2271`: "Real play never calls this"); `HoldElectionWithoutTheNight` (`GC:6339-6347`: "Invoked by reflection from `UiScreenshotDriver.AdvanceDays`; nothing in play calls it"); `TriggerSigningForNewestDivision` (`GC:2304-2312`) | Precedents of private shortcuts (section 6) |
| U-06 | `SimulationTestRunner`'s own "scenarios" (baseline, stress, … fifteen) and matrix | `SimulationTestRunner.cs:73-74`, `:76-150` | Not the challenge scenarios. A validation harness that, in the Editor, runs at every play-mode entry unless `-skipsimulationtestrunner` (`:80-93`) |
| U-07 | `GameController.PlayerCountryId` falls back to the USA before any selection | `GC:106` (`_selectedPlayerCountryId ?? CountryId.USA`) | INFERRED harmless: the in-game readers run after selection |
| U-08 | A scenario's party pick offers every playable party, and no created party | `GC:2133-2138` (`offerCreate: false`), `GC:2366-2373` | §492 records the pick; no spec says a scenario may be played out of government (fork 3 #8) |

## 5. Suspected discrepancies

Each is SUSPECTED, a lead for a run. The ones marked "fork 3" were found by the scenarios and run-end fork and spot-checked where noted.

| # | what | evidence |
|---|---|---|
| S-01 | **The start card the player selects is not carried into the game.** SELECT opens the party panel with nothing about the card (`CountrySelectorScreen.cs:524-529`), and the world opens on `WorldClock.StartDate(country)` (`GC:2166`). Harmless while a country has at most one playable card (`StartPoints.TryPlayable`'s own premise, `StartPoints.cs:104-110`). It becomes a defect the day a second card unlocks | read |
| S-02 | **Italy promises an election the game never holds.** The card's mode line reads "SNAP ELECTION · OPENS 21 JUL 2022" (`StartPoints.cs:127`, by `IsSnapStart`, `WorldClock.cs:98`). The folder line reads "THE SNAP ELECTION OF 25 SEP 2022" (`WorldClock.cs:243-245`). But `TryNextPollingDay(Italy)` is false (`WorldClock.cs:572`). Already a row (PS-7, "US-1's kind of fix in Italy's own words") | read |
| S-03 | **Germany has no polling day after 23 Feb 2025** (`WorldClock.cs:551-557`; the comment at `:540-541` says so). A German run never meets a second election, so "two cycles" and "30 years" cannot be reached there | read |
| S-04 | **The FDP never leaves Scholz's cabinet in a German run.** The record ends the three-party government on 2024-11-07 (`WorldClock.cs:385-386`), one day after the start. Record seating applies to the USA only (`WorldClock.cs:112`, `:116`; `SM:3995-4017`). The spec names Scholz's minority (section 3 M-12) | read |
| S-05 | **The scenario verdict does not break the day loop.** `_scenarioVerdictPending` is checked once per frame (`GC:739-742`) but is missing from the after-each-day gate (`GC:807-811`). A frame whose accumulated `Time.deltaTime` covers two days (possible at 3×: 0.25 s per day against a 0.333 s cap) simulates one more day after a verdict. At 1× it never does (at most 0.333 days per frame). This is a **1× vs 3× divergence route**, in scenario runs only, and only after the verdict. A harness that scales `Time.timeScale` (section 6.4 step 5) makes multi-day frames, and so this leak, possible at any speed | read; the arithmetic is INFERRED from the 0.333 s cap |
| S-06 | Election night's verdict is applied only at the night's cover-out (`GC:1900-1908`). A save taken while it is pending (only the autosave can be, see S-07) lands it at load (`GC:1041-1043`). The night is never shown, and the asked-first flags `_pendingElectionPlayerAskedFirst` and `_pendingElectionFirstAsked` are not saved (`GC:301-303`; absent from `CaptureUiDrafts` `GC:928-977`) | read. Display only: INFERRED, since `ApplyElectionVerdict` touches only the verdict fields (`GC:6714-6726`) |
| S-07 | **Autosave runs on polling day after the night is built** (`CheckElection` `GC:796-799`, then `AutosaveIfDue` `GC:801`, before the gate at `:807`). So an autosave can capture a pending verdict | read |
| S-08 | **The autosave counters are not reset on load.** `RestoreFromSave` (`GC:893-926`) and `RestoreUiDrafts` (`GC:984-1076`) leave `_daysSinceAutosave` and `_autosaveCount` alone; only a new game resets them (`GC:2242-2243`). This contradicts "a loaded game starts its cadence afresh" (`GC:1406`). The day an autosave writes after a mid-session load depends on the session before it. File side effect only, INFERRED | read |
| S-09 | **A seatless Polish player can survive an election.** On the non-round path, "no government formed" returns (`GC:6431-6435`) before the out-of-parliament test (`GC:6450-6456`) | fork 3 #1, spot-checked |
| S-10 | "No government could be formed … You stay in office until one can" is printed whatever the player's role, opposition included (`GC:6433`) | fork 3 #2, spot-checked |
| S-11 | **"Idle" is not a playable style as built.** With default settings the clock holds until answered at:<br>• every budget window while governing (`GameSettings.cs:197`, `GC:1387`);<br>• the campaign opening (`:196`);<br>• cabinet decisions; foreign-policy meetings;<br>• the chair's term (`GC:3833-3855`);<br>• a scandal; a declaration; the formation sheet;<br>• every signing ceremony (`GC:747`) and every election night.<br>INFERRED: even an opposition player's queue gets the AI government's budget divisions, since `QueueNewlyResolvedDivisions` takes every non-motion division in the player's country (`GC:2282-2299`). "Idle" in Phase 1 must mean "answer each interrupt with a fixed default", or turn the two settings off | read |
| S-12 | **The central bank chair's appointment bypasses the manager.** The IMGUI button writes `Country.CurrentFedChair` directly. It is duplicated in two places (`GC:4248-4252`; `GameController.DocketV35.cs:251-255`) and has no manager verb | read |
| S-13 | Stale comments that still say losing an election ends the run: `GC:6370-6372`, `ScenarioDefinition.cs:39-42`, `ScenarioEvaluator.cs:20-23`; `GC:6447-6449` still calls PS-3 a deviation. Player-visible: `ElectionNightScreen.cs:738` cites "the rule (C-R4, D-5 (a))", which PS §2 marks Replaced. The feature row OP-1 (`POLISIM_FEATURE_LIST.md:163`) still reads "D-5 (a) stands - lose-only" | fork 3 #5-7 |
| S-14 | "NEW ELECTION" is shown by the night and the coalition stage (`ElectionNightScreen.cs:695-699`; `GameController.CampaignCoalition.cs:84`, `:143`), but only the Riksdag's paths schedule one | fork 3 #3 |
| S-15 | **A scenario can be played without governing.** Its objectives are then judged on the AI's book (`GC:6105`; `ScenarioDefinition.cs:35` says "Which country the player governs") | fork 3 #8 |
| S-16 | **A scenario can be left Undecided for good.** If the party leaves the chamber mid-scenario, `_isGameOver` stops the clock (`GC:732`) and `CheckScenarioObjectives` never runs again | fork 3 #9 |
| S-17 | **Two scenario oddities.** `ScenarioEvaluator` compares EndTurn with the ABSOLUTE turn (`ScenarioEvaluator.cs:138`), and `StartTurn` is never read. A save naming a removed scenario is said to be "REFUSED" (`GC:1045-1049`; `SaveGame.cs:176`) but loads as free play with an error (`GC:1053-1059`) | fork 3 #10, #15 |
| S-18 | **A won scenario is stamped "GAME OVER" in the bad ink** (`GC:2099-2101`; `GameController.Desk.cs:1138-1153`) | fork 3 #11 |
| S-19 | **PF-19 confirmed by reading.** The night's install resets the arrival window (`GC:6429`) but does not call `CloseBudgetWindowIfNotGoverning` (private, `SM:3157`), as the manager's own installs do (`SM:4032`) | fork 3 #4; feature row PF-19 |
| S-20 | **The PS-6 row is stale.** It reads "PLANNED 2026-10-04, nothing built" (`POLISIM_FEATURE_LIST.md:88`), while §782-§802 record US-1, US-2, US-3, US-4, US-5, US-10, US-11, US-12 and US-20. Player-visible: the US card "NO ELECTION YET" and the record seating its House and president | read (§ headers) |
| S-21 | **The shadow baseline's "every stream at zero"** (`ShadowBaseline.cs:67-68`) is false whenever anything drew from the static streams before the new game. In the Editor that is `SimulationTestRunner`'s whole validation run, played in its `Start` unless skipped (`SimulationTestRunner.cs:152-189`). The played world then starts on advanced streams while its counterfactual starts at zero, and the ledger's gap absorbs RNG drift. Editor-only: the player build skips the runner unless `-runsimulationtestrunner` (`:84-93`) | read |
| S-22 | **A created party's first election usually ends the run.** It starts with 0 seats, and an idle Grassroots party polls 0.58 % (§681) against Sweden's threshold. INFERRED: most created-party runs end at their first election; needs a ruling | fork 3 #13 |
| S-23 | **A Polish junior partner has no LEAVE THE GOVERNMENT row.** It is drawn inside `DrawConfidence`, which returns early for Unsourced rules (`GameController.ParliamentRows.cs:702`, `:787-798`), although `SM.LeaveGovernment` has no rules gate (`SM:2877-2894`) | fork 3 #14 |
| S-24 | **Save → load → Search diverges.** The cabinet shortlist is not saved (`GC:509`), and Search / Reshuffle draws on the simulation's Cabinet stream (`GameController.PoliticsV35.cs:356`, `:404`). A save → load → Search run draws again, so later cabinet rolls differ from the uninterrupted run | the save/load fork; the shortlist and Search lines re-read by me |
| S-25 | **Dictionary order after a load.** After a minister or pending bill is removed and another added, a loaded game iterates the dictionary in a different order from the uninterrupted game (D-08, INFERRED). That moves cabinet rolls and same-day bill order | the save/load fork |
| S-26 | **Events can be switched off for later runs.** `EventSystem.Enabled` is left false by `ResponsivenessAuditHarness.cs:179` and `ImpactScaleSweep.cs:54`. Every later game in the same process (a warm Editor host) runs with no events | the save/load fork; both lines re-read by me |
| S-27 | **A comment says three streams are never drawn in play; they are** (D-16): `SimulationRandom.cs:76-77`, `:86`, `:94` | re-read by me |
| S-28 | **The harness's saves collide with the player's.** Harness saves (autosave especially) land in the player's real saves folder, and CONTINUE opens the newest save (`GC:1299-1307`, `GC:1416`) | the save/load fork |
| S-29 | **A false "mirrors Update" claim.** `SaveLoadRoundTripDiagnostic` says its loop "mirrors GameController.Update exactly" (its `:25-27`), but it skips the controller's turn wrapper, the divisions queue, the election and the UI layer | the precedents fork, lead 4 |
| S-30 | **The tools disagree on order within a day.** `Update` ticks the day before a boundary's turn (`GC:773-792`). `PlayerSmoke` (`PlayerSmoke.cs:73-74`) and the film's state searches run the turn first | the precedents fork, lead 1 |
| S-31 | **An arrival-window flag lost on load.** `_incomingBudgetWindowOpenNow` (`SM:972`) is cleared on load and never re-derived, although its comment (`SM:969-970`) calls it derivable. Display only: the hold's wording | the save/load fork |

## 6. Harness entry points

### 6.1 The facts that constrain any harness

| fact | evidence |
|---|---|
| The scene holds two scripted objects. `UI_Test` carries `GameController` and `TestRunner` carries `SimulationTestRunner`, both enabled. Nothing else is scripted | `Assets/Scenes/SampleScene.unity:122-148` and `:479-505`; GUIDs `17dadb45…` = `GameController.cs.meta`, `9e829dd4…` = `SimulationTestRunner.cs.meta` |
| `GameController` is `public partial class GameController : MonoBehaviour` (namespace `PoliSim.UI`). Every handler is `private`. The only public surface is `MainMenuLive` (`GC:1290`), `CanvasTransitionSettled` (`GC:1727`) and `CanvasSelectorActive` (`GC:1730`). `internal` members: `CaptureUiDrafts` (`GC:928`), `SetInstrumentLadder` (`GC:5529`), `OnDesk` (`GameController.Desk.cs:48`) and the `SetCampaign*Screen` family | grep of `public`/`internal` in `GameController*.cs` |
| No `.asmdef` exists under `Assets/` and no `InternalsVisibleTo`. Runtime code (including `Assets/Scripts/Testing`) is Assembly-CSharp; `Assets/Editor` is Assembly-CSharp-Editor. **Editor code reaches `GameController`'s private and internal members only by reflection.** A runtime script in `Assets/Scripts/Testing` can call `internal` members directly, but not `private` ones | `find Assets -name "*.asmdef"` (none); grep `InternalsVisibleTo` (none) |
| `SimulationManager` is a `MonoBehaviour` (`SM:179`). Its `Awake` builds a default world when none is set (`SM:4781-4789`). `GameController.Start` adds one to its own GameObject and injects the world (`GC:665-667`) | — |
| **The takeover seam ticks only in `OnGUI`, on the Layout event** (`GC:2563-2566`). Under `-batchmode` nothing delivers `OnGUI`'s events (`DryGuiPass.cs:18-20`: "Under `-batchmode` nothing delivers them, so this delivers them itself"). Without a pump: the main menu and the selector never build; a signing ceremony or election night entered from `Update` never leaves; `ApplyElectionVerdict` never runs | `GC:1752-1917`; holds at `GC:732`, `GC:747` |
| `Update` runs the day loop on real time: `_daySpeedTimer += Time.deltaTime`, at 1, 0.5 or 0.25 s per day (`GC:768-772`, `GC:321-330`). Unity caps a frame's delta at 0.333 s (`ProjectSettings/TimeManager.asset`, Maximum Allowed Timestep). Nothing in `Assets` sets `Time.timeScale`, `targetFrameRate` or `captureDeltaTime` | grep `timeScale\|targetFrameRate\|captureDeltaTime` (none) |
| A new game starts the clock RUNNING at `GameSettings.DefaultSpeed` (default 1×, `GameSettings.cs:195`; `GC:2241`). A load resumes PAUSED (`GC:1075`). The speed chips and PAUSE/RUN are IMGUI-only (`GameController.Desk.cs:411-421`, `GC:5478-5489`) | — |
| A new game can open only once per `GameController`: `OpenWorldAt` returns when a country is already selected or `CurrentTurn != 0` (`GC:2158-2164`). Statics (the epoch, the random streams, `CreatedParties`, the market's turn state, `NationalElection.Last*`) are process-wide (section 7). One run should therefore be one play-mode entry. `ProjectSettings/EditorSettings.asset:29-30` (`m_EnterPlayModeOptionsEnabled: 1`, `m_EnterPlayModeOptions: 0`) means no reload is disabled: INFERRED, domain and scene reload each time | — |
| **Seeding.** `-seed=N` is read only by `SimulationTestRunner.Start`, AFTER its `-skipsimulationtestrunner` early return (`SimulationTestRunner.cs:80-83` and `:103-110`). Without a seed, the first stream use draws one from `Environment.TickCount` (`SimulationRandom.cs:155-158`, `:173-176`). In the Editor the runner, unless skipped, then plays a whole validation run (100 turns by default, its own world, the same static streams) inside `Start` (`SimulationTestRunner.cs:152-189`). The player's game then starts on streams already advanced. `ShadowBaseline(int)` assumes "every stream at zero" (`ShadowBaseline.cs:67-68`). The `Start` order of the two scripts is unspecified: no `DefaultExecutionOrder`, and the metas carry no `executionOrder` | section 5 S-21; section 7 D-01 to D-03 |

### 6.2 The UI's own entry points, per player action

"Editor-callable" means callable from a static class in `Assets/Editor` running in play mode. "Needs OnGUI" means the effect needs the `OnGUI`-ticked seam or IMGUI drawing.

| action | the UI's path (control → method) | accessibility | Editor-callable? | needs OnGUI / lifecycle? | headless note |
|---|---|---|---|---|---|
| Leave the main menu: NEW GAME | Canvas `MainMenu/Column/NewGame` `Button.onClick` → `ChooseMainMenu(NewGame)` (`GC:1293-1296`); applied at CoverOut by `ApplyMainMenuChoice` (`GC:1884-1893`, `GC:1310-1318`) | button public (uGUI); handlers private | `GameObject.Find(...).GetComponent<Button>().onClick.Invoke()` is the click itself, with no reflection | yes: the menu is built by the seam (`GC:1752-1773`) and its choice applied at CoverOut | Without the pump the menu never exists. Skipping it is harmless: the menu exits on its own when a country is seated (`GC:1866-1869`), and in pure batchmode it never started |
| Pick country, start and party | folder `Button` → `ShowStartPanel` (public, `CountrySelectorScreen.cs:388`) → card → SELECT → `ShowPartyPanel` (public, `:242`) → party row `Button.onClick` → `onSelect` = `GC.SelectPlayerCountryAndParty(CountryId, string)` (private, `GC:2111`) | buttons public; the target is private | yes, by clicking the selector's buttons; or by reflection on `SelectPlayerCountryAndParty`, the very delegate the row calls (`GC:1785`) | the selector is built by the seam; **if the selector is live and the seam is not ticked, `_canvasLive` stays true and `Update` never advances a day** (`GC:747`) | In pure batchmode with no pump, the selector is never built and `_canvasLive` stays false. Reflection on `SelectPlayerCountryAndParty` then opens the world (`OpenWorldAt`) and the clock runs |
| Pick a created party (Sweden) | party panel CREATE A PARTY → `OnCreateParty` = `RequestPartyCreation` (private) → CoverOut → `OpenPartyCreation` (private) → IMGUI steps (`DrawPartyCreationScreen`, absolute rects) → START → `CommitPartyCreation` (private, `GameController.PartyCreation.cs:60-69`) | private; `PartyCreationFlow.NewDraft` (public static, `PartyCreationFlow.cs:95`) and `TryCommit` (public static, `:179`) | by reflection: set `_pcDraft` to a `NewDraft` filled in public fields, then invoke `CommitPartyCreation` | the flow's controls are IMGUI | `CommitPartyCreation` itself needs no `OnGUI`. It registers, sets `_worldRosterStale`, then calls `SelectPlayerCountryAndParty` |
| Start a challenge scenario | strip `Button` → `OpenScenarioPartyPick` (private, `GC:2133`) → `ShowPartyPanel(..., offerCreate:false)` → row → `StartScenarioAsParty(definition, abbrev)` (private, `GC:2141`) | private | clicking, or reflection on `StartScenarioAsParty` with `ScenarioLibrary.All[i]` (public) | the strip needs the selector | It must be the first selection of the session (`OpenWorldAt` refuses a running game, `GC:2158-2164`) |
| Advance one day | none: `Update` (`GC:692-816`) calls `SM.AdvanceDay` (public, `SM:312`), `SM.AdvanceCountryDayTick` (public, `SM:296`), then the private `QueueNewlyResolvedDivisions`, `AdvanceTurn`, `CheckElection` and `AutosaveIfDue` | `Update` private; the steps it calls are mixed | let frames run (the UI's own path), or re-implement the loop (a shortcut that misses the private steps) | `Update` is a lifecycle message; holds are re-checked each day (`GC:807-814`) | Real-time paced. Options: `Time.timeScale` (public) multiplies `Time.deltaTime`; INFERRED, `Time.captureDeltaTime` gives a fixed delta per frame. Either keeps `Update` the driver |
| Advance to the next turn | the same loop: `AdvanceDay` returns true on the 365th day (`SM:483-484`) → `GC.AdvanceTurn` (private, `GC:6098-6138`), which builds decisions, advances the impact ledger, `SM.AdvanceTurn`, the shadow baseline, `EnergyMarket.BeginTurn`, `ResetPolicyInputs`, then `CheckScenarioObjectives` | private | frames only; calling `SM.AdvanceTurn` directly skips the shadow, the ledger, the market reset and the scenario check | — | — |
| Set speed / pause | masthead chips (IMGUI) set private `_gameSpeed`; rail PAUSE/RUN (IMGUI, `GC:5478-5489`) | private field | reflection on `_gameSpeed`; the public `GameSettings.DefaultSpeed` before a new game | IMGUI | A new game honours `GameSettings.DefaultSpeed` (`GC:2241`). This is the only public lever |
| Save | saves screen Save (IMGUI) → `SaveToPath` (private, `GC:840`); F5 → `DebugSaveGame` (`GC:828`); **autosave** → `AutosaveIfDue` from `Update` (`GC:801`, `GC:1411-1422`) | private; autosave driven by the public `GameSettings.AutosaveDays` / `AutosaveSlots` | **Autosave is the one save route a harness can drive with public API only**: set `GameSettings.AutosaveDays` (7/30/90) and let days pass. Otherwise reflection on `SaveToPath` | the saves screen is IMGUI; F5 needs `Keyboard.current` (`GC:702-703`), INFERRED null in batchmode | the autosave path is `SaveToPath`, the same as the button's. It writes the player's real saves folder (S-28) |
| Load / Continue | saves screen Load (IMGUI) or menu CONTINUE (Canvas) set `_pendingLoadPath`; `Update`'s safe point → `LoadFromPath` (private, `GC:864-885`) → `RestoreFromSave` (`GC:893-926`) | private | reflection: set `_pendingLoadPath` and let one frame run. That is exactly the UI's path below the button | `Update`; menu CONTINUE also needs the seam | `RestoreFromSave` re-forks the shadow and ledger from the loaded game, re-seats the market, and resumes PAUSED (`GC:908-913`, `GC:1075`) |
| A desk action (bill, dial, law, campaign act, formation, motion) | IMGUI inline: `if (Button(...)) { _simulationManager.<Verb>(PlayerCountryId, Build<X>FromDrafts()) }`. Examples: budget `GameController.BudgetV35.cs:866-869`, `GC:9591-9594`; law `GC:8768-8771`; formation TABLE IT `GameController.FormationSheet.cs:366-370`; no confidence `GameController.ParliamentRows.cs:778-782` | the **manager verbs are public** (`SM:1102`, `:1149`, `:1189`, `:1506-2327`, `:2801-2939`, `:4107-4153`, `:4516`, `:4538`); the draft fields and `Build*FromDrafts` are private | the manager verb directly with the arguments the button passes. The only gap: building the argument through the private draft builders (by reflection) or constructing the public bill type in the harness (a declared shortcut) | no `OnGUI` once the click's logic is lifted out | A refactor exposing one `internal` or `public` method per verb (`IntroduceBudgetFromDrafts()` …) would make "the UI's own entry point" literal |
| Answer an interrupt | cabinet decision → `SM.ResolveCabinetDecision` (public; `GC:9101-9104`); foreign-policy meeting → `SM.ResolveForeignPolicyMeeting` (public, `SM:1149`); scandal → `SM.QueueScandalResponse` (public, `SM:4516`); budget window → `SM.IntroduceBudgetBill` (public); no-confidence declaration → `SM.OrderExtraElection` / `AskToBeDischarged` (public, `SM:2897`, `:2928`); formation → `SubmitFormation` / `PassFormation` / `AnswerOffer` (public); campaign opening → `AcknowledgeCampaignOpening` (private, `GameController.Campaign.cs:132-137`); **central bank chair → the IMGUI button writes the model directly**: `_playerCountry.CurrentFedChair = c; _fedChairCandidates = null;` (`GC:4248-4252`, again at `GameController.DocketV35.cs:251-255`), no manager verb; signing → `SignPendingDivision` (private, `GC:2315`) and then the seam; election night → `DismissElectionNight` (private, `GC:6706-6709`) and then the seam's CoverOut, which runs `ApplyElectionVerdict` (`GC:1900-1908`) | mixed | yes, but the chair, campaign opening, signing and night need reflection | signing and night need the seam (`OnGUI`) to release the clock | **No "idle" run is possible without answering these** (section 5 S-11) |

### 6.3 The precedents: how each tool starts and advances a game

MATCH means the tool's route is the UI's own method. SHORTCUT means it bypasses the UI's method. Line numbers come from the precedents fork; I re-read `BatchSimulationRunner.cs` myself.

| tool | launch | start | advance | save / load | actions | headless? |
|---|---|---|---|---|---|---|
| **BatchSimulationRunner** + **SimulationTestRunner** (the mandated runner) | `-batchmode -nographics -executeMethod PoliSim.EditorTools.BatchSimulationRunner.Run [-turns= -scenario= -runmatrix -matrixturns= -seed=]` (`BatchSimulationRunner.cs:7-15`). Opens SampleScene and enters play mode (`:71-79`). **Exits 15 Editor updates after play starts** through `CheckExit.Finish(0)` (`:28`, `:81-105`). That is safe only because `SimulationTestRunner.Start` is synchronous (`:18-22`) | **SHORTCUT**: `WorldFactory.CreateDefault()` + `AddComponent<SimulationManager>()` + `SetWorld` on the DEFAULT epoch, no player or party (`SimulationTestRunner.cs:152-156`). The scene's `GameController` still runs `Start` beside it and then idles at `GC:732` | `AdvanceDay` ×365, then the manager's `AdvanceTurn`, with a scripted USA decision per `-scenario` (`SimulationTestRunner.cs:169-196`). No day tick, no controller wrapper, no election | none | `cabinetstress` resolves through the public `ResolveCabinetDecision` | yes (synchronous, no frames needed) |
| **UiScreenshotCapture / UiScreenshotDriver** (the film) | real `Run` is windowed and refuses `-batchmode`; `RunDry` requires `-batchmode` and runs one play session per country×geometry, carried in `SessionState` (`UiScreenshotCapture.cs:145-264`). The driver is added after the reload (`:403-433`) and exits itself | pins preferences and `Seed(777)` first; finds the controller (`UiScreenshotDriver.cs:220`, `:247-260`). **MATCH**: `ChooseMainMenu(NewGame)` (`:323`); with `-shotparty=`, `SelectPlayerCountryAndParty` (`:462`); created party: `RequestPartyCreation` → (draft fields written directly, a SHORTCUT past the five steps) → `CommitPartyCreation` (`:414-442`). **SHORTCUT**: by default `SelectPlayerCountry` (the one-argument harness shape, largest party, `:459`); scenarios started MID-RUN with `StartScenario` (`:3828`, `:3918`) | **SHORTCUT** warm-up `AdvanceDays` (`:3079-3138`): the manager's `AdvanceDay` / `AdvanceTurn` only. Its election goes through `HoldElectionWithoutTheNight`. The clock is held by priming `_daySpeedTimer = -3600` (`:468`). **Closest to play**: `-shotinterrupts` / `-shotformation` step in `Update`'s order, then let the controller's own `Update` run at top speed (`:4633-4637`, `:4952-4958`, `:5069-5088`) | `-shotsaves`: `SaveGameService` + `CaptureUiDrafts()` (the same body as `SaveToPath`, not the method). `-shotload`: the private `LoadFromPath` (**MATCH**) | public manager verbs identical to the buttons' bodies (`QueueCampaignDecision`, `SubmitFormation` …). Private handlers by reflection (`CheckElection`, `DismissElectionNight`, `SignPendingDivision`, `BuildBudgetBillFromDrafts` …). Model writes by reflection (SHORTCUT): ministers, re-seated parties, enacted laws. Nothing simulates a click | dry film yes: `OnGUI` pumped through `DryGuiPass` in `LateUpdate` |
| **PlayProtocolStaging** (edit mode) | menu or batch method | seed 777, epoch scoped to Sweden's start, a bare manager. The party and government are seated by copying `GC:2124` / `GC:2188` (SHORTCUT) | `AdvanceDay` / `AdvanceTurn` only | saves with no UI layer; verifies into a second bare manager | — | yes |
| **PlayProtocolCheck** | the cheap bar | adds a `GameController` to a bare object, injects the manager by reflection | — | the private `RestoreFromSave` (**MATCH** for load, §557); asserts paused | — | yes (edit mode) |
| **PlayerSmoke** (player build, `-batchmode -nographics -smoke=`) | attaches itself | — | 30 × `AdvanceDay`, then `AdvanceTurn`, then the day tick (turn BEFORE tick: the reverse of `Update`, SHORTCUT) | the private `LoadFromPath` (**MATCH**) | — | yes |
| **MultiRun** | edit mode | runs static methods; starts no game | — | — | — | yes |
| **TrajectoryBaselineDump**, **NoPolicyCentury** | edit mode, seeded, epoch-scoped | manager only; NoPolicyCentury names a player country with no party (the tools' case, `SM:2638-2640`) | `AdvanceDay` ×365 then `AdvanceTurn(None)` | — | — | yes |

**What no tool reaches** (precedents fork):
- the controller's `AdvanceTurn` wrapper (`GC:6098`);
- `QueueNewlyResolvedDivisions`;
- `AutosaveIfDue` and `SaveToPath`;
- `StartScenarioAsParty`;
- `DismissScenarioVerdict`;
- the menu's `_pendingLoadPath` route.

So **no existing tool has ever played a challenge scenario to its verdict through the UI's route, or saved through the UI's own method.**

**The tools disagree on the order inside a day.** `Update` ticks the day (`AdvanceCountryDayTick`) before the turn at a boundary (`GC:773-792`). `PlayerSmoke` and the film's state searches run the turn first (SUSPECTED, a precedents-fork lead). A state they stage may not be reachable in play.

### 6.4 Recommendation

**Keep BatchSimulationRunner as the entry, and add an audit driver beside the real GameController.** Every change goes in harness files only.

**1. The runner.**
- `BatchSimulationRunner.Run` sets a `SessionState` flag.
- Its `[InitializeOnLoadMethod]` path (`:51-69`) adds an `AuditDriver` MonoBehaviour after the play-mode reload. This is the film's idiom (`UiScreenshotCapture.cs:403-433`).
- It must **not** exit at frame 15 while the driver runs. The driver calls `CheckExit.Finish` itself.
- Launch with `-skipsimulationtestrunner`. This removes the validation run that would otherwise share the random streams (section 7, D-02).
- Test early whether the Canvas screens build under `-nographics` (precedents-fork lead 5). The dry film runs with a graphics device.

**2. One run is one play-mode entry.** Each run then gets a fresh domain, which resets the epoch, the random streams, `CreatedParties`, the market state and `NationalElection.Last*`. `OpenWorldAt` cannot open a second world in a running controller anyway (`GC:2158-2164`). A matrix is a loop of play sessions, carried in `SessionState` as the dry film does.

**3. The driver** lives under `Assets/Scripts/Testing`, so it is in the runtime assembly. That lets it call `internal` members directly and private ones through one loud reflection helper, like `UiScreenshotDriver.cs:2955-2964`. At start-up it must:
1. `SimulationRandom.Seed(seed)` before the new game opens.
2. Pin every preference: speed, both holds, autosave days and slots. `PreferencePins.Pin()` snapshots and deletes every key so readers see the defaults; `Restore()` puts them back (`Assets/Scripts/Testing/PreferencePins.cs:29`, `:52`). Then set the variant's values through `GameSettings`, and log them.
3. **Pump `OnGUI` through `DryGuiPass`** in `LateUpdate`, the dry film's way (`UiScreenshotDriver.cs:2903-2910`). The seam then runs exactly as in play: the menu, the selector, every signing ceremony, every election night and the verdict landing (`GC:1900-1908`). Without the pump, `Update` stalls at the first division or election.

**4. Starting a game, in the UI's order.**
- **Canvas screens:** click the uGUI `Button`s themselves with `Button.onClick.Invoke()`. This is public API and literally the click: main menu NEW GAME, then the country folder, then SELECT on the start sheet, then the party row, CREATE A PARTY, or a scenario strip button. No tool does this yet. It is the most faithful path for every Canvas control.
- **Fallback:** reflection on the very delegates the buttons hold: `ChooseMainMenu`, `SelectPlayerCountryAndParty`, `OpenScenarioPartyPick` → `StartScenarioAsParty`, `RequestPartyCreation` → `CommitPartyCreation`.
- **The created party's IMGUI steps:** fill a `PartyCreationFlow.NewDraft` (public) and commit through `CommitPartyCreation`. This is a declared shortcut past the IMGUI controls.

**5. Advancing time.** Let the controller's own `Update` drive.
- The new game starts at the pinned default speed. Set `_gameSpeed` by reflection for Pause, 1×, 2× and 3× (there is no handler; the chips write the field inline).
- To escape real-time pacing without a private shortcut, measure in Phase 1:
  - `Time.timeScale` (public; it multiplies `Time.deltaTime`, so more days per frame);
  - `Time.captureDeltaTime` (INFERRED: a fixed delta per frame, so a deterministic number of days per frame).

  Both keep `Update`, its holds and its order the driver.
- The alternative is to call `Update`'s private steps in its order (`AdvanceDay`, `AdvanceCountryDayTick`, `QueueNewlyResolvedDivisions`, the controller's `AdvanceTurn`, `CheckElection`, `AutosaveIfDue`). That is faster but reflects into five private methods, and it re-implements the hold logic.

**6. Answering interrupts.**
- Use the public manager verbs where they exist: cabinet decision, meeting, scandal, budget, declaration, formation.
- Use the private handler where the UI has one: `AcknowledgeCampaignOpening`, `SignPendingDivision`, `DismissElectionNight`.
- Copy the inline UI writes where the UI has no method: the central bank chair, `GC:4248-4252`.
- Log every answer as an action with its day.

**7. Saving and loading.**
- Save through `SaveToPath` (reflection) **with a scratch path**. Every built-in path, autosave included, writes the player's real saves folder, which CONTINUE reads (Annex B).
- Autosave is the one fully public save route (`GameSettings.AutosaveDays` and `AutosaveSlots`). It is still worth exercising once, with the folder moved aside and put back afterwards.
- Load by setting `_pendingLoadPath` and letting one `Update` run, which is the menu's own route. Then set the speed again, because a load resumes paused (`GC:1075`).

**8. Desk actions.** Call the public manager verb with what the button passes. Build the argument with the controller's private `Build*FromDrafts` after writing the draft fields (reflection), or construct the public bill type. Either way it is a declared shortcut on the argument, never on the verb.

**What a cleaner Phase 4 refactor would buy** (Elias's call: it is gameplay code):
- one `internal` method per UI verb that today lives inline in `OnGUI`: speed set, chair appointment, each Introduce-from-drafts;
- `Update`'s day body extracted into one method, as `AdvanceCountryDayTick` was (`GC:775-783`).

Together these would make "the same entry points the UI calls" literal, and remove most of the reflection.

**Run-time estimate, for the protocol's Phase 0 post.** Real-time pacing dominates any `Update`-driven run.
- **At 3×:** a game day takes 0.25 s, so a 365-day turn takes at least 91 s.
  - First election from the start: Sweden and Poland are 238 days away (about 60 s); Germany 109 days (27 s).
  - "Two cycles" in Sweden (to September 2030): about 7 min.
  - "30 years": about 46 min, plus every hold.
- **At 1×:** four times those figures.
- **Fixed costs:** a Unity launch costs about 23 s (CLAUDE.md: a host is up in 12-35 s).
- **Scaled time:** with `Time.timeScale` or `captureDeltaTime`, runs fall toward the simulation's own cost. INFERRED from §673's 19.7 s for the runner's 100-turn world: a few tenths of a second per turn for one world, before the shadow baseline and the ledger forks roughly double it.
- **Recommendation:** run the 1× vs 3× determinism comparisons on short spans in real time, and the long cells on scaled time.

## 7. Determinism hazards

**Seeding.** There are 13 streams, each a `System.Random` seeded with the master seed plus the stream number × 7919 (`SimulationRandom.cs:37-122`, `:160`). A save carries the master seed and every stream's draw count, and a load fast-forwards each stream (`SimulationRandom.cs:187-225`).

**Where each stream draws in play** (save/load fork, lines from its reading):
- turn boundary: Cabinet (`CabinetSystem.cs:497-498`, `:535`, `:561`, `:571`) and Event (`EventSystem.cs:447-452`);
- the player's daily roll: ForeignPolicy (`SM:1141`);
- SovereignWealth (`SovereignWealthFundSystem.cs:90-91`) and PublicationRevision (`PublicationSystem.cs:196`);
- the live campaign: CampaignAi, Debate and Scandal (`SM:4355-4356`);
- the run-up: PreCampaign (`SM:4402`);
- UI only: FederalReserve, for the chair's candidates (`FederalReserveSystem.cs:103-135`, via `GC:761`, `:808`, `:2667`).

Parliament, ElectionNoise and ElectionNight are never drawn in play. The Polish veto is a stateless hash of seed, country, date and bill (`PresidentialVeto.cs:91-99`).

| # | hazard | where | play path? | note |
|---|---|---|---|---|
| D-01 | **Real play is unseeded.** The master seed is `Environment.TickCount` at first use (`SimulationRandom.cs:155-158`, `:173-176`). First use in play is `new ShadowBaseline(SimulationRandom.MasterSeed)` in `GameController.Start` (`GC:673`). A new game does not reseed (`OpenWorldAt`, `GC:2156-2201`) | as cited | yes | By design ("real play still varies", `SimulationRandom.cs:28`). The harness must call `SimulationRandom.Seed` itself |
| D-02 | **`-seed=` and the test runner.** `-seed=` is parsed only by `SimulationTestRunner.Start`, after its `-skipsimulationtestrunner` early return (`SimulationTestRunner.cs:80-83`, `:103-110`). Unskipped in the Editor, the runner plays a whole validation run on the shared static streams inside `Start` (`:152-189`). The player's game then begins on advanced streams. The two `Start`s have no defined order. Rows of `-runmatrix` continue each other's streams (`:112-133`, save/load fork) | as cited | yes (Editor) | Launch with `-skipsimulationtestrunner` and seed in the driver |
| D-03 | **The shadow baseline starts at stream zero, the real game may not.** `ShadowBaseline(int)` assumes "every stream at zero" (`ShadowBaseline.cs:67-68`), but the real game may start on advanced streams (D-02). The ledger's gap then contains RNG drift | `GC:673`, `GC:2178` | display (the impact ledger) | Section 5 S-21 |
| D-04 | **Days are paced by wall clock.** `_daySpeedTimer += Time.deltaTime` (`GC:768-773`). An action lands on a day that depends on frame timing unless the clock is held while acting. The film measured this race and holds the clock by priming `_daySpeedTimer` by reflection (save/load fork: §510; `UiScreenshotDriver.cs:443-537`) | as cited | yes, for any run with actions | A harness acts only while paused or held, or on a deterministic frame delta (section 6.4 step 5) |
| D-05 | **1× vs 3×: one extra day after a scenario verdict.** The after-day gate omits `_scenarioVerdictPending` (`GC:807-811`; set at the boundary, `GC:2076-2089`, `GC:6137`). With a 0.333 s frame cap, 3× can run two days in one frame; 1× never does | as cited | scenario runs only | Section 5 S-05. The only speed route either fork found |
| D-06 | **Preferences change the trajectory.** The two hold settings decide whether days pass during the budget window and the campaign opening (`GameSettings.cs:156-166`; `GC:1384-1387`). They are machine PlayerPrefs, outside the save. `PreferencePins.Pin` / `Restore` (`Assets/Scripts/Testing/PreferencePins.cs:29`, `:52`) snapshot them; a process that dies mid-run loses the snapshot | as cited | yes | Pin, then set explicitly per variant, and log them |
| D-07 | **UI actions draw on simulation streams.** Cabinet **Search / Reshuffle** draws a shortlist with `CabinetSystem.GenerateCandidates` (`GameController.PoliticsV35.cs:356`, `:404`) on the Cabinet stream that the turn's cabinet rolls also use. The shortlist `_cabinetCandidatesByPortfolio` (`GC:509`) is not saved, so save → load → Search draws again and every later cabinet roll shifts. The chair candidates draw from FederalReserve, which nothing else uses, once per term window; INFERRED, at the same simulation point at any speed | as cited | yes | Save/load divergence, SUSPECTED (S-24) |
| D-08 | **Dictionary enumeration order after a JSON round trip** (INFERRED from .NET's free-slot reuse). Affected: `Country.CabinetMinisters` (`Country.cs:869`) and the pending-bill dictionaries (`SM:1007-1023`). Removals: `CabinetSystem.cs:508`, `GameController.PoliticsV35.cs:400`, `SM:1648`, `:1708`, `:1932`. Cabinet rolls draw once per minister in enumeration order (`CabinetSystem.cs:493-498`, `:559-571`); same-day bills resolve in that order (`SM:1626`, `:1899`). After a remove-then-add, a loaded game enumerates differently from the uninterrupted one | save/load fork | yes | SUSPECTED (S-25). Repro: vacate a portfolio, save, load, appoint. `SaveLoadRoundTripDiagnostic` never removes a minister |
| D-09 | **Unstable sorts with ties** | `CoalitionFormation.cs:343`; `GovernmentFormation.cs:269-270`; `GovernmentRecord.cs:167` (portfolio order by seats) | yes | Deterministic for an identical input order; fragile under D-08 (INFERRED) |
| D-10 | **The campaign is replayed on load.** The campaign setup is rebuilt at the load date. A replay that lands on the wrong stream counts is logged, not refused (`SM:4579-4650`, `:4592`, `:4613`). The one known moving input (the record shift) is saved; the rest are unverified | save/load fork | yes | Phase 1: save and load mid-campaign and compare |
| D-11 | **Process-wide statics shared by every world in a play session.** Each is reset only by a new play-mode entry:<br>• `SimulationManager.EpochDate` (`SM:233`): set by a new game and by a load (`SaveGameService.RestoreInto`, `SaveGameService.cs:239`);<br>• the streams (`SimulationRandom.cs:124-126`);<br>• `CreatedParties` (`CreatedParty.cs:132-133`), cleared only in `GameController.Start` (`GC:664`) and replaced by a load;<br>• `EnergyMarket`'s turn state (`EnergyMarket.cs:140`, `:147`), written by whichever world ran a boundary last (the ledger forks, then the real world, then the shadow) and put back by `GC:6126-6130`;<br>• `EnergyMarket`'s calibration caches (`:137-139`), never reset;<br>• `NationalElection.LastRegional*` (`NationalElection.cs:69-84`), read by election night (`ElectionNightFromModel.cs:38-51`), never reset;<br>• **`EventSystem.Enabled`** (`EventSystem.cs:442`), set false by `ResponsivenessAuditHarness.cs:179` and `ImpactScaleSweep.cs:54` and never restored | as cited | yes | **A warm Editor host that ran one of those two harnesses runs every later game with no events.** Run the audit only in fresh processes (one play-mode entry per run) |
| D-12 | **Diagnostic knobs the game never sets**, which persist if a check leaves them set | `TradeCosts.PassThroughMeasurementScale` (`:70`), `EntrantLayer.Active` (`:63`), `PensionParticipationResponse.ProbeSuspended` (`:65`), `EnergyLedger.SupportShareOverride` (`:187`), the fiscal-reaction statics (`SM:533-535`) | yes, if left set | the same rule as D-11 |
| D-13 | **Film code reseeds the whole process** | `ElectionNightFilm.cs:58`; `UiScreenshotDriver.cs:251`, `:5229` | film runs only | Never share a process with the film |
| D-14 | **Manager-only loops never hold the player's election.** The controller runs it (`GC:796-799`, `GC:6349`). Such trajectories split from a UI run at the first polling day | both forks | yes, for comparisons | Never compare a manager-only dump with a UI run past polling day |
| D-15 | **Clean by grep.** No `UnityEngine.Random`; no `new System.Random` outside the film driver (`UiScreenshotDriver.cs:5627`, `:5732`); no threads, `Parallel` or `Task`; no `DateTime.Now` in the model (a save's `SavedAtUtc` is the one wall-clock field, `SaveGameService.cs:94`); `GetHashCode` only in the UI preview fingerprint (`GC:4739-4745`); `Stopwatch` only in diagnostic timing (`ChamberVerdicts.cs:214`, `:227`). **Looking at a screen does not draw:** `PreviewTurn` runs on a clone with no event roll (`SM:5187-5340`); `PreviewFormation` freezes a proposal (`SM:4125-4132`); the shadow and the forks swap the global generator in a `finally` (`ShadowBaseline.cs:96-116`, `:128-148`) | as cited | — | read by the save/load fork; Phase 1 should still compare runs with and without the `OnGUI` pump |
| D-16 | **A comment contradicts the code.** `SimulationRandom.cs:76-77`, `:86`, `:94` say "NOTHING IN THE LIVE GAME DRAWS" from CampaignAi, Debate and Scandal. The live campaign draws from all three (`SM:2604-2607`, `:4355-4356`) | as cited | — | S4 |

---

## Annex A: time, the day loop and its holds (item 3)

**The loop.** It runs in `GC.Update`, every frame (`GC:692-816`). The frame-level gates come first:
- the F5/F9 debug keys (`:702-714`);
- a load queued by the menu runs (`:718-723`);
- **holds that return before any day:** saves or settings screen open (`:727`); no country, game over, or election night up (`:732`); scenario verdict pending (`:739`); a Canvas takeover live or in transition, or a signing queued (`:747`); Paused (`:751`);
- the HQ opens itself on a pending campaign opening or story (`:758`). This is UI state only, INFERRED;
- **interrupt holds** (`:761-763`): the chair's term candidates; the campaign opening (setting); a scandal answer; pending cabinet decisions; a pending foreign-policy meeting; the budget window (setting); a no-confidence declaration in the player's government (`GC:1391-1397`); the Speaker's round waiting on the player (`GC:1400-1404`).

**One day.** `_daySpeedTimer += Time.deltaTime`, and while it covers a whole day:
- `SM.AdvanceDay()` (returns the turn boundary);
- `SM.AdvanceCountryDayTick(player)`;
- `QueueNewlyResolvedDivisions()`;
- `AdvanceTurn()` if a boundary was crossed;
- `CheckElection()` if `PollingDayToday`;
- `AutosaveIfDue()`;
- then the holds are re-checked (`:807-814`; `_scenarioVerdictPending` is missing, S-05).

**Inside `SM.AdvanceDay`** (`SM:312-485`), for every country in order:
- publication; the rating review;
- sector and infrastructure; demographics; labour, crime and the society statistics;
- fiscal accrual; the macro block; history;

then, for the player's country only:
- `SeatTheRecordOnItsDate` (the USA's record);
- `AdvanceCampaign`;
- the `PollingDayToday` flag;
- `HoldPresidentialRound` (Poland's).

The turn boundary is every 365th day from the epoch (`SM:483-484`, `SM:216`).

**Speed.** Pause stops `Update` at `:751`. 1×, 2× and 3× are 1, 0.5 and 0.25 real seconds per game day (`GC:321-330`). Speed changes only how many days run per real second. Every day runs the same sequence, and every hold is re-checked after each day, so a hold raised by day *d* stops the loop before day *d+1* at any speed. The one known exception is S-05.

**Routes by which 1× and 3× could diverge on one seed.**
1. S-05 (a scenario verdict lets an extra day run at 3×; at 2× a frame holds at most 0.833 s against a 0.5 s day, so one day at most).
2. Anything that runs per FRAME and draws from a simulation stream or writes the model. On the frame, outside the day loop, there are: `OpenLiveCampaign` (`GC:758`, UI state); `UpdateFedChairSelectionState` (`GC:761`, `GC:3833-3855`); and everything `OnGUI` does (previews, `RecomputePolicyPreview`). See section 7 for what draws.
   - `UpdateFedChairSelectionState` draws the candidates from the FederalReserve stream (`FederalReserveSystem.GenerateCandidates`, `GC:3850`) once per term turn, cached by `_fedChairCandidatesForTurn`. The after-each-day re-check (`GC:808`) generates them right after the boundary day at any speed, so INFERRED the draw happens at the same simulation point. Fork 1 found that nothing else draws the FederalReserve stream (`FederalReserveSystem.cs:103-135`), so even a timing shift could move no other consumer.
3. The settings that release the two holds: a released hold lets days pass where a held one stops them. They are PlayerPrefs, so they are machine-global and must be pinned.

The Canvas seam's real-time envelopes (`Time.unscaledTime`, `GC:1747`, 0.18 s and 0.24 s) only delay when the clock resumes; they change no state. INFERRED.

## Annex B: save and load (item 4)

| step | path | evidence |
|---|---|---|
| Write | `SaveToPath(path)` → `SaveGameService.CreateSaveGame(_simulationManager, _world, PlayerCountryId, CaptureUiDrafts())` → `SaveToFile` (the old file kept as `.bak`) | `GC:840-860` |
| Named save | saves screen → `SaveToPath(DefaultSaveDirectory/<sanitized>.json)` | `GC:1150-1156` |
| Quicksave | F5 → `SaveToPath(DefaultSlotPath)` | `GC:828-831` |
| Autosave | every N days played → `SaveToPath(autosave_<slot>.json)`, slot rotating 1…N | `GC:1411-1422`; default off (`GameSettings.cs:198`) |
| Load | `LoadFromPath` → `SaveGameService.LoadFromFile` → `RestoreFromSave`, which: `SaveGameService.RestoreInto(manager, save)` adopts `save.World`; re-resolves the player's country; sanitises the fleets; `EnergyMarket.BeginTurn(world)`; sets `PlayerCountryId`; `RestoreUiDrafts`; **re-forks the shadow baseline and the impact ledger from the loaded game**; clears the preview cache and the signing queue; zeroes `_daySpeedTimer`; closes the campaign map | `GC:864-926` |
| UI state carried | the draft fields; the game-over flag and reason; the pending election verdict, its ends-game flag and date; the acknowledged campaign opening; the scenario progress and verdict-pending flag; the recorded speed (ignored); the chair candidates and their turn; the signing high-water mark | `CaptureUiDrafts` `GC:928-977` |
| UI state NOT carried | `_pendingElectionPlayerAskedFirst`, `_pendingElectionFirstAsked` (S-06); the autosave counters (S-08); the live speed (a load resumes PAUSED, `GC:1075`); `_lastRunningSpeed`; the map markers `_mapEventMarkers` (`GC:428`, written at `GC:6153`). Nothing under `Assets/Scripts/Persistence` names them, and `RestoreFromSave` does not clear them, so a mid-session load keeps the previous game's markers on the maps (display only); open screens and pins | `GC:928-977`, `GC:984-1076` |
| Who loads like the player | `PlayProtocolCheck` (the private `RestoreFromSave`, §557), `PlayerSmoke` and the film's `-shotload` (the private `LoadFromPath`). `SaveLoadRoundTripDiagnostic` and `PlayProtocolStaging` load into a bare manager only, which by CLAUDE.md §557 is not the game | precedents fork |

**From the save/load fork** (line numbers from its reading; the version and migration lines re-read by me):
- **Format.** `SaveGameService.CurrentSaveVersion = 38` (`SaveGameService.cs:53`). Any other version is refused; there is no format migration (`:26-32`, `:149-177`). New fields inside a version are ignored on read (`MissingMemberHandling.Ignore`, `:71-81`).
- **`SaveMigration` is a folder and preferences move, not a format migration** (§693). It runs `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` once per process (`SaveMigration.cs:41-48`) and touches the real user folder and registry.
- **What a save carries** (`SaveGame.cs:22-186`):
  - the player country, `MasterSeed` and the streams' draw counts;
  - the turn, the date and the epoch (since format 25);
  - the currency zones;
  - the whole `World` (the support-agreement templates are re-scored on load, `World.cs:19`);
  - the manager's pending state: fiscal periods, budget process and arrival flags, every pending bill type, extra-election dates, meetings, cabinet decisions, last reports and events;
  - the player's campaign as a replay record;
  - the UI layer (`UiDraftState`);
  - the created parties.

  A save written by a batch tool has no UI layer.
- **Not carried, beyond the UI items above:**
  - the cabinet shortlist (`GC:509`), whose loss is a divergence (D-07);
  - `_incomingBudgetWindowOpenNow` (`SM:972`), cleared on load and never re-derived; display only, the hold's wording (`GC:5929`, `:9728`);
  - the shadow baseline and impact ledger, re-forked from the loaded game (`GC:908-913`). After a load, "without your policies" means since the load, not since the seed. The code calls which of the two is right undecided (`SM:3990-3993`);
  - the energy market's turn state, recomputed at the load day; display only, INFERRED.
- **The order of `RestoreInto`** (`SaveGameService.cs:230-248`):
  1. the created-party registry replaced;
  2. the streams restored;
  3. `SimulationManager.SetEpoch(save.EpochDate)` (`:239`), which stays set for the process;
  4. `RestoreSaveState`;
  5. the player country set;
  6. `RestoreCampaign` replays the campaign (`SM:4579-4650`);
  7. `SeatTheRecordOnItsDate` (internal).
- **Hazard for the harness: every save path writes the player's real saves folder.** The folder is `Application.persistentDataPath/saves`, i.e. `%USERPROFILE%\AppData\LocalLow\DWELOP Games\Incumbent\saves`. CONTINUE then opens whatever the harness saved last (`GC:1299-1307`). Autosave's path is fixed to that folder (`GC:1416`). The harness should call `SaveToPath` with a scratch path, or move its files out and restore the folder afterwards.
- **Gap: nothing proves save → load → continue through the controller.** The election, the shadow and the drafts all live there. `PlayProtocolCheck` proves only the load itself, not the continuation.

## Annex C: run end and outcomes (item 5)

From the scenarios and run-end fork, condensed; its code references were spot-checked at `GC:6379-6465` and `GC:6714-6726`.

| outcome | how it is reached | code | run continues? | status |
|---|---|---|---|---|
| **Game over: out of parliament** | the player's party wins no seat at an election | `GC:6409-6418` (round path), `GC:6450-6456`; lands in `ApplyElectionVerdict` (`GC:6714-6726`) at the night's cover-out or at once | no (Load still works) | BUILT SE, DE, PL (S-09: a Polish exception) |
| **Game over: scenario verdict** | the verdict is dismissed | `GC:2094-2104` | no | BUILT SE, IT |
| The player ends the run | — | no verb | — | SPEC-ONLY (M-09) |
| **Loss: out of office after an election** | another party is installed: SE, DE by the round or Art. 63; PL that night | `GC:6458-6464` | yes | BUILT SE, DE, PL; NOT BUILT IT, US, FR (no elections) |
| Win | "There is no single win" (PS §7). Night "IN OFFICE"; "SCENARIO COMPLETE" | no career or summary code | yes | the career measure is SPEC-ONLY (M-10) |
| Hung / no government | SE: rounds, four rejections then an extra election; DE: Art. 63 to Abs. 4; PL: the previous record stands (`GC:6431-6435`) | `SM:3477`, `SM:4058-4071` (fork 3) | yes | BUILT SE, DE; PARTIAL PL |
| Coalition / minority / support | the formation's kinds; `RoleOf` | `GovernmentRecord.cs:237-244` | yes | BUILT SE, DE, PL |
| No-confidence | SE: player or AI motion, the week, an extra election or discharge; DE: the constructive vote | `SM:2828-2874`, `SM:3026-3059` (SE AI), `SM:3135-3193` (DE) | yes | BUILT SE, DE. **SE AI motion unreachable on the real calendar, as ruled** (§655, row PS-3i-2a). NOT BUILT PL, IT |
| Early / extra election | SE: `OrderExtraElection` or `BreakOff` → `ScheduleExtraElection` | `SM:2897-2952` | yes | BUILT SE; NOT BUILT DE (no dissolution or Art. 68), PL, IT |
| Term limit | — | only the AI Polish president's limit (`PresidentialElection.cs:335-357`, fork 3) | — | NOT BUILT for the player (US-28; R-US6 (b) ruled) |
| Caretaker | SE discharge | `SM:4180-4198` | yes | BUILT SE |
| Never ends | IT, US, FR: no election, no motion. Only the Italy scenario's verdict ends an Italian run | `WorldClock.cs:548-580`; `ConfidenceProcedure.cs:34` | — | by construction |
