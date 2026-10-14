# Gameplay audit, Phase 0 inventory — slice D

**Slice D covers:** campaigning; approval and polls between elections; events and the challenge scenarios; cabinet ministers; parliamentary gating; statutes and laws; the central bank; foreign policy.

- **Repo:** `G:\UNITY\Projects\PoliSim` at HEAD `1dd5b43f` (s802), read on 2026-10-13.
- **Read-only.** No file in the repo was touched, and nothing was run (no Unity, dotnet or git writes).

## How this was read

Six read-only sub-readers each read one part of the slice in full:
1. the campaign engine;
2. the campaign HQ UI, with the campaign spec's coverage;
3. approval, events and scenarios;
4. cabinet, central bank and foreign policy;
5. parliamentary gating;
6. laws and statutes.

I re-read every load-bearing fact myself. They are listed in §0.4.

Every citation below was read by me or by one of those readers:
- A code fact is given as `file:line` (or file + symbol).
- A spec fact is given as document + section, a `POLISIM_FEATURE_LIST.md` row id, or a `COMPLETED.md` § number.
- Anything reasoned out but not checked by reading the exact lines is marked **INFERRED**.
- Every item in §5 is **SUSPECTED**: a lead for Phase 1 runs, not a finding.

### Paths and abbreviations

Code paths are relative to `Assets/Scripts/`. Editor files are given as `Assets/Editor/…`.

| Short | File |
|---|---|
| SM | `Simulation/SimulationManager.cs` |
| GC | `UI/GameController.cs` |
| GC.X | `UI/GameController.X.cs` (e.g. GC.Campaign, GC.DocketV35, GC.LawsV35, GC.PoliticsV35, GC.BudgetV35) |
| CR | `Elections/CampaignRun.cs` |
| PCR | `Elections/PreCampaignRun.cs` |
| LCS | `Elections/LiveCampaignSnapshot.cs` |
| LSET | `Elections/LiveCampaignSetup.cs` |
| PS | `Simulation/ParliamentSystem.cs` |
| CS | `Simulation/CabinetSystem.cs` |
| FRS | `Simulation/FederalReserveSystem.cs` |
| FPS | `Simulation/ForeignPolicySystem.cs` |
| LC | `Simulation/LawCatalog.cs` |

| Short | Document |
|---|---|
| CAMPAIGN SPEC | `docs/specs/ELECTIONS_CAMPAIGN_SPEC.md` |
| POLSYS | `docs/specs/POLITICAL_SYSTEM_SPEC.md` |
| MR | `docs/reference/MODEL_REFERENCE.md` |
| FL | `POLISIM_FEATURE_LIST.md` |
| §N | a `COMPLETED.md` section |

**Caution: duplicate low-numbered headings in `COMPLETED.md`.**
- §2 means COMPLETED.md:48 (Political Systems Overhaul Part A, Cabinet).
- §5 means COMPLETED.md:100 (Part B, Parliament).
- §583 also contains a "## 2." (COMPLETED.md:31690) and a "## 5." (COMPLETED.md:31763, "Closing the loop for a player (CL-1..CL-4)"). These are cited by line number.

---

## 0. Slice D at a glance (inputs to the variant matrix)

### 0.1 Per country

| Feature | Sweden | Germany | Poland | Italy | USA | France |
|---|---|---|---|---|---|---|
| Playable start (`Elections/StartPoints.cs` For :59-96) | Riksdag 2026 | Bundestag 2025 snap | Sejm 2023. The presidential 2025 start is LOCKED (:76-78) | general 2022 snap | presidential 2024 | legislative 2024 snap, governing mode. The presidential 2022 start is LOCKED (:87-89) |
| Polling day the game holds (`Elections/WorldClock.cs`:548-580) | 2nd Sunday of September every 4 years from 2026-09-13 (:573-579) | 2025-02-23 only, then none (:551-557) | 2023-10-15, then the first Sunday of each Art. 98 window (:558-571) | none (:572) | none | none |
| Run-up + campaign + HQ (`LSET.TryFor` :144-178, SE/DE only at :148) | yes, every election | yes, once. Run-up cut by `FromWorldStart` (`Elections/CampaignClock.cs`:104-110); length INFERRED about 7 weeks | **no**: polling day with no campaign; count on the prediction (GC.RunNationalElection :6638-6700) | no | no | no |
| Approval (`EconomyState.ApprovalRating`) | yes | yes | yes | yes | yes | yes |
| Random events (EventSystem, all six, no country filter) | yes | yes | yes | yes | yes | yes |
| Challenge scenario (`Data/ScenarioLibrary.cs`:41-45) | Inherit the Fund | — | — | Italy Debt Crisis | — | — |
| Cabinet ministers and decisions | player's country only, any of the six | " | " | " | " | " |
| Laws offered (competence, LC:2514-2517) | 140 | 130 (no monetary regime) | 140 | 130 | 130 (no electricity tax) | 130 |
| Second chamber / veto | — | — (no Bundesrat anywhere) | presidential veto + 3/5 override (§761) | Senato NOT BUILT | Senate and veto NOT BUILT (PS-6 parked) | president's powers NOT BUILT |
| Budget alternative weighed (`WorldClock.BudgetProcedureOf` :356-368) | frame decision (§632/§633) | government bill alone | budget act + vetoable statute acts (§768); no alternative weighed | alone | alone | alone |
| Central bank | Riksbank head (appointed) | ECB, national push ±0.75 | NBP head | ECB push | Fed head | ECB push |
| Foreign-policy meetings | player's country, where the player may act for Foreign (SM:298) | " | " | " | " | " |
| Trade bill and overrides | yes; EU members get no base dial (GC.LawsV35:748-776) | " | " | " | yes, with a base dial | " |

**Who governs at the start.** This decides which levers the player has. The role is read from `Country.Government` (SM.PlayerGoverns :2632-2642).

- **Sweden:** the government of record, M+KD+L with SD's support (§628).
- **USA:** a Presidency record; the president's party is the "prime minister's party" (`Elections/GovernmentRecord.cs`:43-46).
- **France:** a what-if. The player's party is seated as the prime minister's (§631).
- **Italy:**
  - Draghi's cabinet parties are a GAP, so "the formation's result stands in, PROVISIONAL" (WorldClock.cs:398).
  - Italy holds no election in a run.
  - Only the USA seats the record by date (`WorldClock.RecordSeatsChamber` / `RecordSeatsExecutive` :112, :116).
  - So an Italian player governs only if their party is that provisional formation's PM party. INFERRED from those lines.

### 0.2 Per role: the lever matrix

✓ = may use. ✗ = refused. ✓P = only if the junior partner's party holds the portfolio. References are SM unless stated.

| Lever | PM party | Junior partner | Support | Opposition | Decided at |
|---|---|---|---|---|---|
| Budget bill (own) | ✓ | ✗ | ✗ | ✗ | 1508 → 2650-2659 |
| Alternative ("shadow") budget | n/a | ✗ (refused at 1197; the UI still draws a live button, §5) | ✓ in Sweden only; recorded as a break | ✓ in Sweden only | 1189-1204 |
| Tax-programme bill | ✓ | ✓P Finance, but ✗ for the player-partner holding Finance | ✗ | ✗ | 1601; 2679-2684 |
| Welfare-programme bill | ✓ | ✓P Health | ✗ | ✗ | 1661 |
| Labour bill | ✓ | ✗ | ✗ | ✗ | 1721 |
| Crime & justice bill (no UI) | ✓ | ✓P Interior | ✗ | ✗ | 1811 |
| Law bill | ✓ | ✓P by category (`PortfolioOfLaw` 2753-2763) | ✗ | ✗ | 1865 |
| Sector bill | ✓ | ✗ | ✗ | ✗ | 2174 |
| SWF drawdown bill | ✓ | ✗ | ✗ | ✗ | 2239 |
| Trade bill / "Set override" | ✓ | ✓P Foreign | ✗ | ✗ | 2329; GC.LawsV35:785 |
| Cabinet decisions (roll and answer) | ✓ | ✓P | ✗ | ✗ | roll gate 5154-5157; the answer is not gated (1102-1118) |
| Foreign-policy meetings | ✓ | ✓P Foreign | ✗ | ✗ | roll gate 298; the answer is not gated (1149-1160) |
| Appoint or reshuffle ministers | ✓ | ✓P | ✗ | ✗ | UI only: GC.PoliticsV35:375, 397-405, 453-459 |
| Cabinet shortlist "Search" | ✓ | ✓ | ✓ | ✓ | **ungated**: GC.PoliticsV35:354-357 |
| Central-bank head appointment | ✓ | ✗ | ✗ | ✗ | UI only: GC:3836; GC.DocketV35:209, 251-256 |
| Rate push (euro members) | ✓ | ✗ | ✗ | ✗ | GC:4031; the decision is used only if PlayerGoverns (GC:6105) |
| Pension age (rides the budget bill) | ✓ | ✗ | via the alternative (Sweden) | via the alternative (Sweden) | GC:9843-9855 |
| Finance partner's stance dial (±2.5) | ✗ | ✓ if it holds Finance | ✗ | ✗ | 2695-2701 |
| Withdraw support | ✗ | ✗ | ✓ | ✗ | 2801-2811 |
| Leave government | ✗ | ✓ | ✗ | ✗ | 2877-2894 |
| Campaign HQ (queue, answers) | ✓ | ✓ | ✓ | ✓ | no role test on the path (SM:4298-4374, 4456-4462) |
| The party's own chamber votes | automatic | automatic | automatic | automatic | StanceModel (`Simulation/StanceModel.cs`:281-425). No player control |

**Notes on the matrix.**
- A caretaker PM keeps every lever: `PlayerGoverns` has no caretaker test (SM:2641).
- An empty `PlayerPartyAbbrev` makes `PlayerGoverns` true: "the instrument's hand" (SM:2640).

### 0.3 The day path a Phase 1 harness must reproduce

The play path is `GameController.Update`, GC:700-815. It returns early, in this order, when:
- the saves or settings screen is open (727);
- there is no country, the game is over, or it is election night (732);
- a scenario verdict is pending (739);
- a Canvas takeover or signing queue is open (747);
- the game is paused (751).

It opens the HQ when the campaign's opening or a story is pending (758).

Then it returns while any **hold** stands (761-763):
- `UpdateFedChairSelectionState()` (3833-3855);
- `CampaignOpeningHolds()` (1384, only if `GameSettings.HoldOnCampaignOpening`);
- `HasPendingScandalAnswer()` (GC.Campaign:109-113; no setting);
- a pending cabinet decision;
- a pending foreign-policy meeting;
- `BudgetWindowHolds()` (1387, only if `GameSettings.HoldOnBudgetWindow`);
- `DeclarationHolds()` (1391);
- `FormationHolds()` (1400).

Each day it then runs, in order:
1. `SM.AdvanceDay()` (773). This steps the campaign at SM:477 and raises `PollingDayToday` at SM:479.
2. `SM.AdvanceCountryDayTick(PlayerCountryId)` (783). This is the foreign-meeting roll, every bill countdown, the budget process and the confidence day (SM:296-310).
3. `QueueNewlyResolvedDivisions()` (787).
4. `AdvanceTurn()` on a boundary (GC:6098-6137). This builds the decisions (the player's only where `PlayerGoverns`, 6105), advances the impact ledger, SM and the shadow, then `CheckScenarioObjectives` (6137).
5. `CheckElection()` when `PollingDayToday` (796).
6. `AutosaveIfDue()` (801).
7. Every hold is re-checked (807-814).

**What the batch runner does not cover.**
- `Assets/Editor/BatchSimulationRunner.cs` runs `Testing/SimulationTestRunner.cs`.
- That never sets `PlayerCountryId`, so no campaign, no polling day and no election run.
- It never calls `AdvanceCountryDayTick`: no bill countdowns, meetings or budget process.
- It drives only the USA's decision (SimulationTestRunner.cs:171).
- Its `-scenario=` flag selects **validation stress scenarios** (`baseline`, `stress`, … `parliamentstress`; SimulationTestRunner.cs:20, :73). These are **not** the challenge scenarios.
- So a Phase 1 harness for slice D has to replicate the controller's loop, as `Testing/UiScreenshotDriver.cs` does, or drive `GameController` itself.

**Player actions with no public SimulationManager entry point.**

These are private controller methods or direct field writes. A harness can reach them only by reflection, or by repeating the write:

| Action | Where |
|---|---|
| Appoint a central-bank head | GC.DocketV35:251-256, GC:4250 |
| Appoint, reshuffle or search ministers | GC.PoliticsV35:354-357, 397-405, 453-459 |
| "Set override" on a trade partner | GC.LawsV35:868-874 |
| Start a scenario | `StartScenario` / `StartScenarioAsParty`, GC:2038-2068, 2141-2146 |
| Dismiss a scenario verdict | GC:2094-2104 |
| Acknowledge the campaign's opening | GC.Campaign:132-137 |
| Pick the campaign region | GC.Campaign:189-196 |
| Sign a division | GC:2315-2318 |

### 0.4 Facts I re-read myself (the load-bearing ones)

**Calendar and polling days**
- The calendar comes from the real polling day (SM:4279-4289) and `WorldClock.TryNextPollingDay` (WorldClock.cs:548-580). CLAUDE.md:88 still says "turn boundary"; §619 is quoted below.
- Only Sweden and Germany stage a campaign (LSET:148, :175-177).

**Campaign engine**
- `QueueCampaignDecision` does not validate region < 0, spend, issue or kind executability (SM:4538-4562).
- `TrySpend` throws on a negative spend and lets NaN through (`Elections/CampaignResources.cs`:99-113, ctor :78-88).
- Campaign-phase legality is a deny-list (`Elections/CampaignClock.cs`:155-163). `CampaignActions.Spec` throws for anything outside the eight (`Elections/CampaignActions.cs`:142-143). `ToDecision` calls `Spec` (`Elections/PlayerCampaignRecord.cs`:102-109).
- The automatic poll runs for the scripted (player) party before the script is read (CR:680-702). The first unaffordable queued item ends the day's queue (CR:801), with at most 64 a day (CR:774).
- The HQ chips' calls and arguments are at GC.Campaign:797-877.

**Laws and statutes**
- There are 140 laws in 6 categories (grep of LC).
- The enact/repeal guard and the approval charge are at SM:1957-1997. The null-LawId path is SM:1863-1887 with LC:2514-2533.
- `ui_duration_cut_act` is named "Extend unemployment insurance" (LC:1951-1958).
- Carbon: `ComingYear = 2023 + turn + 1` and the schedule overwrite (`Data/CarbonRateStatute.cs`:95, 105-129). A passed budget sets `line.Rate` (PS:481-488). The player's turn decision carries only `InterestRateChange` (GC:6277-6301).

**Parliament**
- `BillDurationDays = 21`, `BillFailedApprovalCost = 1.5`, and an unused `RandomSource` (PS:40, 49, 51). `WouldBillPass` is at PS:342-353.

**Scenarios and events**
- Two scenarios (`Data/ScenarioLibrary.cs`:8-46). The selector's wiring is at GC:1786 and 2346-2376. The party pick offers every playable party (GC:2365-2373).
- `EventChancePerTurn = 0.40` and 50 events (`Simulation/EventSystem.cs`:73, 440-454; grep).
- The approval formula is at `Simulation/MacroSystem.cs`:2263-2326. The paid-leave sensitivity is `Simulation/LaborCouplings.cs`:169.

**Cabinet, central bank and docket**
- The docket iterates the live pending-decision list and invokes the body synchronously (GC.DocketV35:65-76, 192-198). The resolve methods are at SM:1096-1160.
- The reshuffle removes the minister before reading `PopularityFactor` (GC.PoliticsV35:398-403; CS:461-463).
- Central bank: candidates only while the player may introduce (GC:3833-3855). The rate rule is at FRS:149-157 and `Simulation/CurrencySystem.cs`:48-55.

**Determinism and the shadow**
- The shadow's RNG swap is at `Simulation/ShadowBaseline.cs`:101-146.
- The stale "NOTHING IN THE LIVE GAME DRAWS FROM IT" comments are at `Simulation/SimulationRandom.cs`:76, 86, 94.
- The day loop is at GC:727-801.
- Seat-the-record applies to the USA only (WorldClock.cs:112-116).

---

## 1. Feature table

Status values:
- **BUILT**
- **PARTIAL**
- **NOT BUILT** (planned in a spec or row)
- **SPEC-ONLY** (specified as built or required, but absent)
- **CODE-ONLY** (in code, named by no spec, row or § I could find)

Campaign spec sections read "REFERENCE, not a work list" (CAMPAIGN SPEC head, §356). A spec section with no code is therefore NOT BUILT, not SPEC-ONLY, unless a record claims it built.

### 1a. Campaigning

| ID | Feature | Player-facing entry (screen/control → method) | Code (file:symbol) | Spec authority | Status | Countries | Notes |
|---|---|---|---|---|---|---|---|
| D-01 | Campaign calendar: 26-week run-up, 8-week campaign, polling day | automatic; HQ masthead and strip (GC.Campaign:327-393, 1054-1123) | `Elections/CampaignClock.cs` CampaignCalendar (`DefaultCampaignWeeks` :72, `DefaultPreCampaignWeeks` :75, `PhaseOn` :117-125, `FromWorldStart` :104-110); SM.CurrentCampaignCalendar :4279-4289 | CAMPAIGN SPEC §3; §477 (CL-1); §619 (CL-4); §697 | BUILT | SE every election; DE once | Polling day is never stepped (CR:351-353, 488; SM:4310). An extra election has no run-up and at most 8 weeks (SM:4283-4287) |
| D-02 | Polling day per country | none | `WorldClock.TryNextPollingDay` :548-580; SM.TryPlayerPollingDay :4269-4276; `PollingDayToday` SM:479 → GC.CheckElection (GC:796) | POLSYS §3, §4, decision 3; §619, §697, §762, §767 | PARTIAL | SE, DE (once), PL; none for IT, US, FR | PS-8 (every country's calendar) NOT BUILT. The IT and US starts name a polling day the game does not hold (FL PS-7; §782 for US-1) |
| D-03 | Live campaign staging | none | `LSET.TryFor` :144-178 (SE and DE :148; refusal :175-177; vote-model refusal :156-160) | §203, §697; FL PS-2, PS-4 | BUILT (SE, DE) | SE, DE | Poland votes with no campaign |
| D-04 | Rail CAMPAIGN cell (open/close the HQ) | rail cell → `OpenLiveCampaign`/`CloseLiveCampaign` (GC.Campaign:248-266; `RailCampaignCellPresent` :239-245) | GC.Campaign | §205, §477 | BUILT | SE, DE | Absent on the run-up's last day (GC.Campaign:244, 785-786) |
| D-05 | The campaign's opening interrupt | the HQ auto-opens (GC:758; on load GC:1040); TAKE THE TRAIL → `AcknowledgeCampaignOpening` (GC.Campaign:398-408 → 132-137) | `HasPendingCampaignOpening` GC.Campaign:120-126; `CampaignOpeningHolds` GC:1384 | §219 (P2-0.3); §615 (MM-2 setting) | BUILT | SE, DE | Holds only when the setting is on. The acknowledgement is saved (GC:969) |
| D-06 | HQ read-outs: chest, hours, staff, offices, the race, the queue, legality | HQ sheet (GC.Campaign:149-160, 431-1123) | `LCS.Build` :23-149, `BuildPreCampaign` :215-287 | CAMPAIGN SPEC §3, §9, §10, §20, §22; §52 (W-E1); §205; §477; §492; §752 | BUILT | SE, DE | Display lags, see §5 SD-30–34 |
| D-07 | Run-up verbs: OFFICE, POLL, TV BUY | run-up chips → `SM.QueueCampaignDecision(kind, -1, null, PreCampaignRun.Price(pre, kind), out _)` (GC.Campaign:797-815, call :810) | PCR (priced :49-52, `Apply` :209-272); SM:4542-4552 | CAMPAIGN SPEC §3 (pre-campaign); §477 | PARTIAL | SE, DE | FUNDRAISE, POLICY, TRAIN and STRATEGY are drawn disabled (`PCR.Refusal` :55-65). "Identify voter groups" has no verb. TV BUY is free and uncapped (PCR:255-262) |
| D-08 | Run-up hires (5 roles) | HIRE MANAGER/MEDIA/POLLSTER/FIELD/DIGITAL → `QueueCampaignDecision(RecruitStaff, -1, null, 1800, role)` (GC.Campaign:817-831) | `Elections/CampaignStaff.cs` (TheFive; `SalaryPerDay` 1,800); PCR:231-241 | CAMPAIGN SPEC §9; §68 (W-B5: 5 of 9 roles); §477 | PARTIAL | SE, DE | 4 roles missing (§37 deferred, FL F-1). Field organiser inert, media advisor lost on interviews (§5 SD-21, 22). Hiring is run-up only |
| D-09 | The eight campaign actions | RALLY, TOWN HALL, DOORS, TV AD, DIGITAL, POST, POLICY, INTERVIEW? → `QueueCampaignDecision(kind, region or -1, null, Spec(kind).MoneyCost)` (GC.Campaign:853-872, call :866-868) | `Elections/CampaignActions.cs` `Spec` :112-145, `Resolve` :169-203; CR.StepDay :612-891 (queue :774-858) | CAMPAIGN SPEC §12, §3; §207; §210; §492 | PARTIAL | SE, DE | Fixed spend and hours (table in §6). No issue, targeting or strategy. The first unaffordable item ends the day's queue (CR:801) |
| D-10 | CLEAR the next day's queue | CLEAR chip → `SM.ClearCampaignQueue` (GC.Campaign:832-836, 873-877; SM:4565-4570) | SM | §207 | BUILT | SE, DE | — |
| D-11 | Live map / region picker | THE MAP chip (Sweden, campaign phase) → tile → `PickCampaignRegion` (GC.CampaignMap:220; GC.Campaign:163, 170-179, 189-196) | GC.CampaignMap; LCS:155-206 | CAMPAIGN SPEC §24-§25; §63 (W-E2); §492; §697; FL K-1c | PARTIAL | SE only | The pick is UI state and not saved (GC:922-925). The swing index is never live (LCS:200-201). The Länder map is a Design row (D-DE) |
| D-12 | Scandals: live stories, the player's answer, the clock hold | story panel DENY/APOLOGIZE/EXPLAIN/ATTACK SOURCE/IGNORE/SACRIFICE STAFF (RESIGN disabled) → `SM.QueueScandalResponse` (GC.Campaign:928-1000, call :968; SM:4516-4528) | `Elections/Scandals.cs` (`LiveRatePerPartyDay` 1/80 :241; table :137-174); CR:540-571, 913-923 | CAMPAIGN SPEC §17; §65 (W-B8); §492 (CL-2); §503 (RS-1, T-9); FL 21st calibration entry | BUILT | SE, DE | The hold is UI-only (GC:761, 808). The engine falls back to the party's instinct (CR:1084-1095) |
| D-13 | Debates | announced on the HQ strip (GC.Campaign:1101-1122) | `Elections/Debates.cs` `Resolve` :190-233; CR:933-958 (days {20, 41} :161) | CAMPAIGN SPEC §15-§16; §64 (W-B7); §492 | PARTIAL | SE, DE | No player input. Preparation is a fixed, uncharged 8 h (CR:1099); spec §9 says 6 h. The debate screen W-E5 exists in films only |
| D-14 | Media coverage, bookings, interviews | INTERVIEW? is a request the outlets may book (§210) | `Elections/MediaSystem.cs` (half-life 3 d :89; 6.5 pp per unit :102; `BookingLedger.Allocate` :461-504) | CAMPAIGN SPEC §13-§14; §57; §194 (C-N1); §208; §210 | PARTIAL | SE, DE | No outlet choice. `eventSalience` is always 0 (CR:588). C-N8 open (FL) |
| D-15 | Campaign strategy (5 sets) | none for the player | `Elections/CampaignStrategy.cs`:105-156; the cast strategy `CampaignAi.cs`:338-382 | CAMPAIGN SPEC §11; §56 (W-B6) | PARTIAL (AI BUILT, player NOT BUILT) | SE, DE | SetStrategy is refused in the run-up (PCR:62) and crashes in the campaign (§5 SD-01) |
| D-16 | Campaign AI: 5 personalities, softmax choice | none | `Elections/CampaignAi.cs` `PersonalityCatalog` :316-388, `Choose` :656-689; `CampaignCasts.cs`:33-39 (CHES rule) | CAMPAIGN SPEC §32-§33; §55; §69; §695 | BUILT | SE, DE | Chaotic is never cast in a live game (CampaignCasts.cs:19-21) |
| D-17 | Reactivity (defend a region, answer an attack) | none | `Elections/CampaignReactivity.cs`; `CampaignAi.Reactions` :892-927; CR:709-772 | §69 (W-C2) | BUILT (AI only) | SE, DE | — |
| D-18 | Campaign offices | run-up OFFICE chip (auto-sited, SM:4549) | `Elections/CampaignOffices.cs` (`OpenCost` 100,000 :38); CR:453-469 | CAMPAIGN SPEC §10; §67 (W-B4); §134 (D-1(c)) | PARTIAL | SE, DE | The player cannot site an office. Local polling and staff capacity are never read (CampaignOffices.cs:40, 45) |
| D-19 | GOTV / ground game | DOORS chip | `Elections/GotvModel.cs`:82-142 | CAMPAIGN SPEC §26-§27; §58; §193 | PARTIAL (no live effect) | SE, DE | Mobilisation is read only by `ElectionDay.Count`, which is Editor-only (`Assets/Editor/ElectionDayHarness.cs`:92). `GetOutTheVote` is legal only on the never-stepped polling day and has no `Spec` |
| D-20 | Polls inside the window: public tracker, internal polls, run-up poll | HQ race plate (GC.Campaign:584-702); run-up POLL chip | `Elections/Polling.cs` `Conduct` :125-168; CR:574-580, 679-696; PCR:242-254 | CAMPAIGN SPEC §20-§21; §54 (W-E4); §477 | PARTIAL | SE, DE | The player's party auto-buys internal polls (§5 SD-20, CODE-ONLY). No breakdowns. The polling screen W-E4 is film-only |
| D-21 | Momentum | HQ race (GC.Campaign:662-673) | `Polling.cs` MomentumTracker (half-life 7 :106) | CAMPAIGN SPEC §22, §39 | BUILT | SE, DE | Reaches polls only, not the vote (CR:576, 686) |
| D-22 | War chest / funds | HQ resources plate (GC.Campaign:431-496) | `ResourcePool` (`Elections/CampaignResources.cs`:74-125); `WarChest` 2,400,000 (LSET:47-48) | CAMPAIGN SPEC §9; §134 (D-1(c)); FL S-14 (separate purse) | PARTIAL | SE, DE (Germany in kronor, LSET:189-190) | No income at all. A fresh 2.4 M at every election, extra elections included |
| D-23 | Long-term party capital | none | `Elections/PartyCampaignCapital.cs`:84-104; carried over at GC:6699 and SM:4008 | CAMPAIGN SPEC §38; §115 (C-D4) | PARTIAL (inert) | all (stored) | No campaign reads it |
| D-24 | DECLARED page (who will govern with whom) | DECLARED chip (GC.Campaign:305) → GC.CampaignDeclared | `DeclaredRedLines.ForDate`; GC.CampaignDeclared:34-35, 190, 241 | FL D-PS; §657; §685; §705; §776 | BUILT | SE, DE (Poland's timeline exists but Poland has no HQ) | Read-only |
| D-25 | Campaign screens reachable only from the harness: Action W-E3, Polling W-E4, Debate W-E5, Results W-E7, Coalition W-E8 | none in play; films only (`Testing/UiScreenshotDriver.cs`:5250, 5318, 5372, 5450, 5545) | GC.CampaignAction / Polling / Debate / Results / Coalition; `Elections/*ScreenSnapshot.cs` | §52-§73 (the W-E series) | PARTIAL (built, unreachable) | — | Headers say "HARNESS ONLY — R-N2 holds until W-G1"; R-N2 is retired (§5 SD-58) |
| D-26 | Campaign save and replay | Save / Load | `Elections/PlayerCampaignRecord.cs`; `SM.RestoreCampaign` :4579-4632; `AssertReplayLandedOn` :4638-4650 | §204; §207; §477; §492 | BUILT | SE, DE | A replay mismatch is only a LogError |
| D-27 | Government record shift (the economic vote) in the campaign | HQ "Economy over the term" (GC.Campaign:153, 486-488) | SM:4343-4351 (opening), 4229-4246 (count day); `EconomicVote.RecordOverTerm` | CAMPAIGN SPEC §19; POLSYS §5.5; §638 (PS-3k); §752 (A1); FL :170, :174 | BUILT | SE, DE; Poland's presidential first round (SM:4259) | Judged once and stored on the record |
| D-28 | Created party / entrant awareness in the campaign | party creation flow (another slice) | `EntrantLayer`; CR:1167, 1200-1218; `LSET.TryCreatedDayZero` :123 | §675; §681; FL SP-3, SP-4 | BUILT (SE); PARTIAL (DE) | SE; DE has no created-party branch (§5 SD-26) | — |
| D-29 | Campaign → count hand-off | election night | `CR.Finish` :1019-1021; `GC.RunNationalElection` :6638-6700 (`SharesFromCampaign` :6655) | §206 (C-R4b step 5, D-21) | BUILT | SE, DE | The live count uses no turnout, GOTV or election noise |
| D-30 | Extra-election campaign | none | SM.CurrentCampaignCalendar :4283-4287 | §636 (a premise) | BUILT | wherever an extra election can be ordered (INFERRED: Sweden, whose confidence rules are sourced) | Debates are still announced for days 20 and 41 (§5 SD-35) |

### 1b. Approval and polls between elections

| ID | Feature | Player-facing entry | Code | Spec authority | Status | Countries | Notes |
|---|---|---|---|---|---|---|---|
| D-31 | Approval: one stat, `EconomyState.ApprovalRating`, 0-100, seed 50 | Desk hero numeral and ledger (GC.Desk:554-567); Statistics (GC.Statistics:124); graph (GC:5667) | `MacroSystem.ApplyApprovalRating` :2263-2326, once per turn per country (SM:5119-5122) | MR "## Political Layer" :606-622; §119 (R-CL1) | BUILT | all six | Reaches no vote, confidence motion or game over (§637, COMPLETED.md:34322). Read by cabinet pressure and scenario objectives only |
| D-32 | Approval attribution ledger | Desk ledger rows (`UI/StatTracePanel.cs`:203-264) | `Data/ApprovalAttribution.cs`; `Simulation/ApprovalLedgerRecorder.cs` (identity :87-95); `MacroSystem.RecordApprovalAttribution` :2344-2399 | §21; §30 | BUILT | all | — |
| D-33 | Party approval stock | none | `Data/Country.cs`:965 `PartyApprovalRating`, written only at seating (GC:2124, 2193, 2227) | R-CL1 (§119; §197) | PARTIAL (inert) | all | "Nothing moves it yet" (Country.cs:960) |
| D-34 | Discrete approval writes | laws, failed bills, budget tax hikes, cabinet options, meeting options, reshuffles, events | SM:1986; PS:49, 742, 526-527; CS:582-588; FPS:89-99; GC.PoliticsV35:400-403; `EventSystem.cs`:472 | §20; §31; §235; §292 | BUILT | player's country (events: all) | Every writer clamps to [0, 100] (§4 INV-A1) |
| D-35 | Cabinet pressure: approval below 40, or a bill lost today, triggers loyalty rolls | Docket minister alerts (GC.DocketV35:80-86) | `CS.UnderPressure` :470-480; `TryRollCabinetEvents` :482-522; SM:5160-5164 | §235; "Elias's ruling F5" (CS:472); §779 | BUILT | player's country (only it has ministers) | — |
| D-36 | Economic vote / perceived performance | HQ economy line | `Elections/EconomicVote.cs` (:35-37, 51, 78-85, 150-163, 199-206); `PerceivedPerformance.OverTerm` :189-207 | CAMPAIGN SPEC §19; POLSYS §5.5; `docs/reference/ECONOMIC_VOTE.md`; §638; §752; FL 22nd entry | BUILT (the player's election only) | SE, DE, PL | AI elections and the run-up are not shifted (FL :174). Only unemployment and inflation are read |
| D-37 | Loyalty | none | per party `LoyaltyModel` (`NationalElection.cs`:161, 201); per group `GroupLoyaltyModel` is Editor-only | CAMPAIGN SPEC §8; §444 | PARTIAL | — | §8 asks for a loyalty value per voter group |
| D-38 | Voter-group view | People page (GC.PeopleV35:124-129, 417-418) | `Elections/CohortVoterGroups.cs` | §112 (C-D1); §214 | BUILT | all; turnout sourced for SE only (CohortVoterGroups.cs:31-37) | — |
| D-39 | Polls between elections | none | — | none requires one; `docs/specs/USA_STAGE_PLAN.md`:52, :690 (R-US15) expects "the game's own poll" | NOT BUILT (planned for PS-6 only) | — | Polls exist only from PreCampaignStart to polling day |
| D-40 | Incumbency multiplier | none | `Elections/PerceivedPerformance.cs`:233-264 ("wired to nothing", :11) | — | CODE-ONLY (Editor harness only) | — | — |

### 1c. Events

| ID | Feature | Player-facing entry | Code | Spec authority | Status | Countries | Notes |
|---|---|---|---|---|---|---|---|
| D-41 | Random economic events: 50, chance 0.40 per country per turn, passive | Desk BREAKING card (GC.Desk:898-935); fading map markers (GC:6146-6158); calendar (GC:3563-3569) | `EventSystem.TryRollEvent` :444-454; `ApplyEvent` :460-473; `MacroSystem.ApplyEventShockDaily` :467-474; SM:5125-5136 | MR "Political Layer" :652-661 and "Expanded Event Pool" :1593-1638; CAMPAIGN SPEC §18; §293; §294; §404 (FT-9); §407 | BUILT | all six, no country filter | The player decides nothing. 16 positive events and 33 negative; mean approval −1.2 per event |
| D-42 | Events → issue salience and elections | none | `EconomicEvent` carries GDP, inflation and approval only (EventSystem.cs:21-23) | CAMPAIGN SPEC §18 :781-795; §184 row 18 | NOT BUILT | — | — |
| D-43 | Event kill switch | none | `EventSystem.Enabled` :439-442 | — | CODE-ONLY (harness switch) | — | — |

### 1d. Challenge scenarios

| ID | Feature | Player-facing entry | Code | Spec authority | Status | Countries | Notes |
|---|---|---|---|---|---|---|---|
| D-44 | Two challenge scenarios: Inherit the Fund (SE), Italy Debt Crisis (IT) | selector strip "Scenario: {Name}" (`UI/CountrySelectorScreen.cs`:139-155, 900-911; GC:1785-1786) or the IMGUI fallback (GC:2346-2376) → party pick → `StartScenarioAsParty` → `StartScenario` (GC:2133-2146, 2038-2068) | `Data/ScenarioLibrary.cs` `All` :41-45 (`InheritTheFund` :95, `ItalyDebtCrisis` :229); `Data/ScenarioDefinition.cs` | §21 (R-S3a-f); §30; §34; §36; §198; §492; §528; §718 | BUILT | SE, IT | Any playable party can start a scenario, PM or not (GC:2365-2373); see §5 SD-46 |
| D-45 | Scenario evaluator and verdict | the verdict takeover holds the clock (GC:739-742); CLOSE → `DismissScenarioVerdict` sets game over (GC:2094-2104) | `Simulation/ScenarioEvaluator.cs` `EvaluateAtBoundary`, once per AdvanceTurn (GC:6137; 2076-2089) | §21; §30 | BUILT | SE, IT | `ThresholdAtDate` is judged at EndTurn rather than ByTurn (dormant; §5 SD-48) |
| D-46 | Scenario persistence | Save / Load | `Persistence/SaveGame.cs`:170-182 (`ScenarioProgress`, `ScenarioVerdictPending`); load refuses an unknown id (GC:1045-1061) | §21 (R-S3e) | BUILT | — | — |
| D-47 | Premise shown at entry and on the verdict; "the three challenge scenarios" | — | `ScenarioDefinition.Premise` (:32) is read by no UI | `ScenarioDefinition.cs`:32 (code contract); `docs/audit/AUDIT_PROTOCOL.md`:35, :53 | SPEC-ONLY (premise display). The third scenario is not found in any source | — | See the appendix |

### 1e. Cabinet ministers

| ID | Feature | Player-facing entry | Code | Spec authority | Status | Countries | Notes |
|---|---|---|---|---|---|---|---|
| D-48 | Appoint ministers (Search a shortlist, then Appoint) | Politics › Cabinet: Search (GC.PoliticsV35:354-357) → `CabinetSystem.GenerateCandidates`; Appoint (:453-459) → direct write `c.CabinetMinisters[portfolio] = candidate` | CS (pool of 18 :56-167; `GenerateCandidates` :523-535); `Country.CabinetMinisters` (`Data/Country.cs`:869) | MR "## Cabinet (Political Systems Overhaul Part A)" :3454-3528; COMPLETED.md:48 (§2); POLSYS §5.2; §234; §235; §634; §745 | BUILT | player's country, any of the six (one pool for all) | Every portfolio starts vacant. Search is not role-gated. No SimulationManager method. AI countries never have ministers |
| D-49 | Reshuffle (dismissal) | Reshuffle (GC.PoliticsV35:397-405) | `ReshuffleApprovalCost` 2.0 (CS:52) × `PopularityFactor` | MR :3519-3521; §234 (D-P2-5.1b); §235 | PARTIAL (the popularity term is dead, §5 SD-09) | player's country | The new shortlist is the same three candidates |
| D-50 | Minister attributes (Efficiency, Popularity, Knowledge, Loyalty) and passive competence | card chips; option estimates are hidden below Knowledge 70 (GC:9095-9121) | CS:424-468, 543-546; `MacroSystem` :763, :1026, :1583, :2527-2533; SM:7102-7108 | §19 (ruling R3); §234; §235 | BUILT | player's country | Defense and Foreign competence are inert by ruling R3. A vacant seat reads the best factors (§5 SD-11) |
| D-51 | Cabinet decisions: 36 decisions × 2 options | Docket item → Decide → `DrawCabinetDecisionModal` (GC:9074-9132) → CHOOSE A/B → `SM.ResolveCabinetDecision` (GC:9101-9104; SM:1102-1118) | `CS.TryRollDecisions` :556-575 (0.12 per minister per turn :49); pools :172-415; `ApplyDecisionOption` :582-607; roll gate SM:5143-5157 | MR :3503-3518; §630; §634; §731; §755 | BUILT | player's country, only for portfolios the player holds | No expiry; holds the clock (GC:761-766). Choosing probably throws, see §5 SD-02 |
| D-52 | Department effectiveness | card chip (GC.PoliticsV35:391-392; GC.Effectiveness:23-31) | `Data/PortfolioEffectiveness.cs` `Record` :101-119; SM:6261 | §336; §337 (P5-C7) | PARTIAL | player's country | Only Health reads it (`Data/HealthFamily.cs`:220) |
| D-53 | Portfolios by Gamson's law, weighted by salience | formation sheet posts (GC.FormationSheet:238-281) | `Elections/GovernmentRecord.cs` `AllocatePortfolios` :149-201; `Elections/PortfolioSalience.cs`:20-118 | POLSYS §5.3 and formateur premise 2; `docs/reference/GAMSON_PORTFOLIOS.md` :14-27; §634; §706; §711; §717; §753 | BUILT. The small-party bonus and manifesto choice are NOT BUILT (GovernmentRecord.cs:55-58) | all; PL and US weights DERIVED as the mean of four (PortfolioSalience.cs:106-109) | — |
| D-54 | Seatless parties never in a cabinet or support role | none | `Elections/CoalitionFormation.cs`:352-363 (assert), 420, 527-528, 613; GovernmentRecord.cs:156, 163, 184-199 | §679; §683 | BUILT | all | — |
| D-55 | The AI's own cabinet ("the same machinery … the cabinet") | none | — | POLSYS §5.2 | NOT BUILT (INFERRED reading of the spec) | — | The player's appointees keep acting and rolling leaks under an AI government (SM:5160 is outside `holdsAny`) |

### 1f. Parliamentary gating

| ID | Feature | Player-facing entry | Code | Spec authority | Status | Countries | Notes |
|---|---|---|---|---|---|---|---|
| D-56 | Role gate: one rule for introducing bills | each lever's lock, drawn by `DrawLeverLock` (GC:10187-10196) | `SM.PlayerGoverns` :2632-2642; `SM.PlayerMayIntroduce` :2650-2659 | POLSYS §5.2; §628; §630; FL PS-3 | BUILT | player's country, all six | Refusal texts at SM:2656-2657. Appointments, the chair, fleet orders and overrides are gated in the UI only (§5 SD-38) |
| D-57 | Portfolio leg of the gate (junior partner) | same locks | `SM.PlayerMayIntroduce(id, portfolio)` :2670-2690; `PortfolioOfLaw` :2753-2763; `FinancePartner.PlayerHolds` | §634; §755 | BUILT | where a junior partner exists | — |
| D-58 | Budget bill (the player's own) | Budget "Introduce budget bill" (GC.BudgetV35:863-869), docket body (GC:9577-9596) → `SM.IntroduceBudgetBill(PlayerCountryId, BuildBudgetBillFromDrafts())` | SM:1506-1553; `PS.ApplyBillResult` :464-528 | MR "## Parliament PILOT" :3758-3867 and "## Master Sequence step 5a/5b/5c/5d" :3869-4178; COMPLETED.md:100 (§5); §630 | BUILT | all six; Poland splits into acts (D-70) | The budget window is enforced only in the UI (GC.BudgetV35:865) |
| D-59 | Tax-programme bill (implement or remove a tax) | Budget tax switch (GC.BudgetV35:495-510) → `SM.IntroduceTaxProgramBill(id, type, !levied)` | SM:1599-1650; PS:552-565 | Master Sequence 5c/5d; §634 (Finance) | BUILT | all | No tax-hike approval penalty (§5 SD-16) |
| D-60 | Welfare-programme bill | welfare row (GC:10103-10106) → `SM.IntroduceWelfareProgramBill` | SM:1659-1710 | §634 (Health) | BUILT | all | — |
| D-61 | Labour bill | Laws › Labour "Introduce labour bill" (GC.LawsV35:203-205) → `SM.IntroduceLaborBill(id, BuildLaborBillFromDrafts())` | SM:1719-1800 | §737 | BUILT | all | PM only |
| D-62 | Crime & justice bill | none ("kept in code, unoffered", GC.LawsV35:237-238) | SM:1803-1846 | MR §5d :4118-4125 (stale); §738 | CODE-ONLY in play | — | A direct call zeroes five dials (§5 SD-07) |
| D-63 | Sector bill | Laws › Sectors (GC.LawsV35:473-474); Energy (GC.EnergyV35:418-419) → `SM.IntroduceSectorBill` | SM:2166-2209 | §739 | BUILT | all | PM only |
| D-64 | SWF drawdown bill | SWF tab (GC:9538-9542) → `SM.IntroduceSwfDrawdownBill` | SM:2226-2318 | MR "## Sovereign Wealth Fund Drawdown Mechanic" :2827 | BUILT | where a fund exists | UI range 0-25 % of GDP (GC:140-141). The manager does not bound it; apply caps at the fund's assets |
| D-65 | Trade bill: base tariff and partner overrides | Laws › Trade "Introduce trade bill" (GC.LawsV35:815-816) → `SM.IntroduceTradeBill(id, BuildTradeBillFromDrafts())` | SM:2321-2388; `Simulation/TradeSystem.cs` | MR "## Per-Partner Tariff Overrides" :1212-1281; §92 (C-B3); §634; §741 | BUILT | all six; EU members have no base dial | Tariffs clamped to [0, 50] (SM:581-583, 5821, 5842) |
| D-66 | Law bill (enact or repeal) | see D-82 and D-83 | SM:1849-1997 | §20; §630 | BUILT | all | — |
| D-67 | Finance partner's fiscal-stance dial | Budget page dial (GC.BudgetV35:306-333) → `SM.SetFinanceStanceTarget` | SM:2695-2701; clamp ±2.5 (`Simulation/FinancePartner.cs`:40) | §716; §755 (ruling A2) | BUILT | where the player's party is a partner holding Finance | — |
| D-68 | Chamber vote: the count decides; the author votes for its own bill; 21 days | vote bars and seat maps | `PS.WouldBillPass` :342-353; StanceModel (author FOR :286-293, terms :296-420); `PS.RecordDivision` :1130-1145; `BillDurationDays` PS:40 | P3-A2 (rule text at PS:334-341); §247 (stance model); §654 (author votes for) | BUILT | all (one chamber) | Deterministic. A failed bill costs 1.5 approval (PS:49) |
| D-69 | Sweden's frame decision / the alternative budget | "Table an alternative budget" (GC.BudgetV35:877-897; docket GC:9604-9617) → `SM.TableShadowBudget` | SM:1172-1275 (`TableGovernmentBudget`, `TableShadowBudget`, `ResolveGovernmentBudget`); `WorldClock.BudgetProcedureOf` :356-368 | §632; §633; §654 | BUILT (SE). Other countries vote the government's bill alone | SE | A junior partner sees a live button that the manager refuses (§5 SD-40) |
| D-70 | Poland: presidential veto, 3/5 override, statute acts | Laws board "CARRIES · VETO n %" (§785) | `SM.PresidentialVetoGate` :1452-1476; `Elections/PresidentialVeto.cs` (`Decide` :165-184, `Draw` :91-99); `PolishStatuteActs` SM:1297-1321 | §736; §757; §761; §768; §773; §775; §778; §798; FL PS-5, SP-7 | BUILT | PL only (PresidentialVeto.cs:45) | The budget act and constitutional amendments are exempt |
| D-71 | Docket: the things waiting on the player | Docket tab `DrawDecisionsTab` (GC.DocketV35:24-126): central-bank candidates, a meeting, cabinet decisions, minister alerts, the budget process | GC.DocketV35 | §731 | BUILT | player's country | Pending bills are not listed there; they are on the pending cards (GC:9405-9427) |
| D-72 | Signing screen | auto-queued for every non-motion division of the player's country (GC:2280-2301); "Sign"/"File" → `SignPendingDivision` (GC:2315-2318) | `UI/SigningScreen.cs` (:232-238, 270-274, 319-380, 463-539, 637-654) | §633; §749; §761; CLAUDE.md Canvas-film rule (§578) | BUILT | player's country | A ceremony only: no choice and no effect. Holds the clock (GC:747) |
| D-73 | ChamberVerdicts: cached vote projections | every vote readout (GC.BudgetV35:495-516, 976-1076; GC.LawsV35:1283-1327; GC.LawsBoard:244-277; GC:8903-8911, 9405-9427, 10145-10163) | `UI/ChamberVerdicts.cs` (cache :62-105; `Veto` :116-123; `VerifyHits` / `Drifts` :42-45, 212-235) | §642; §761 | BUILT | all | — |
| D-74 | DeskRoleRules: the effects card's subject and the rail lamp | Desk effects card (GC.Desk:684-701, 1069-1107); rail (GC:4944, 5204; GC.Campaign:252) | `UI/DeskRoleRules.cs` `DeskEffectsNote` :35-115, `RailLit` :124-151 | §694 | BUILT | all | Display only; checked by `Assets/Editor/DeskRoleCheck.cs` |
| D-75 | The player casting its party's votes | none | StanceModel decides every party, the player's included | POLSYS §5.2 (the table's "what the player controls": "the party's votes") | SPEC-ONLY | — | — |
| D-76 | Junior partner: negotiate the budget in cabinet, threaten to leave, the coalition agreement | only "leave" exists (GC.ParliamentRows:786-797 → `SM.LeaveGovernment` :2877-2894) | — | POLSYS §5.2 (the junior-partner row) | NOT BUILT. The agreement object is SPEC-ONLY | — | — |
| D-77 | Support party: enforce, threaten, withdraw | Withdraw (GC.ParliamentRows:118-125 → `SM.WithdrawSupport` :2801-2811) | `SM.TrackAgreements` :2782-2794; `Elections/SupportAgreement.cs` | POLSYS §5.2; §635; §644; §654 | PARTIAL (withdraw built; enforce and threaten are not) | where a supporter exists | — |
| D-78 | Opposition motions (member bills) | none (no-confidence belongs to another slice) | — | POLSYS §5.2 (the opposition row); §630 ("not built") | NOT BUILT | — | — |
| D-79 | Second chambers and the US and French executives | none | WorldClock.cs:104-111 ("no Senate is modelled (US-15)"); GovernmentRecord.cs:43-46 | POLSYS §4, §6, §8; USA_STAGE_PLAN US-15, US-18, US-21, US-22; §803 | NOT BUILT (planned; PS-6 parked) | US, IT, FR; Germany's Bundesrat is named nowhere | — |

### 1g. Statutes and laws

| ID | Feature | Player-facing entry | Code | Spec authority | Status | Countries | Notes |
|---|---|---|---|---|---|---|---|
| D-80 | Law catalogue: 140 laws in 6 categories | statute book (GC:7686-7948) | LC `All` :144-2509; `Data/LawDefinition.cs`:9-23 | §20; §302; §304; §330; §332; §378; §415; §500; FL EN-7 | BUILT | SE, PL 140; DE, FR, IT 130 (no monetary regime); USA 130 (no electricity tax) | Categories: CrimeJustice 50, LaborMarket 50, LabourInstitutions 10, FiscalFramework 10, MonetaryRegime 10, ElectricityTax 10. No country filter beyond competence (§3 below) |
| D-81 | Statute book browser: filter, sort, search, select | Laws › Laws (GC.LawsV35:40-110, 907-1002; GC.LawsBoard:129-141, 202; GC:7686-8213, 8940) | — | §24; §411 (board 10b); §415; §742 | BUILT | all | — |
| D-82 | Introduce (enact) a law | pane "INTRODUCE AS A BILL ›" → `SM.IntroduceLawBill(PlayerCountryId, new LawBill { LawId, IsRepeal = enacted })` (GC:8757, 8768-8771) | `SM.IntroduceLawBill` :1863-1887; `AdvanceLawBillsDay` :1890-1934; `ApplyLawBillEffects` :1957-1997 | §20; §630; §634; §755 | BUILT | all | Costs 0.25-2.0 approval on passage. There is no approval gate ("Affordable now" is display only) |
| D-83 | Repeal a law | the same button, "REPEAL THIS LAW ›" (GC:8757) | `ApplyLawBillEffects` :1979-1982 | §292 (P4-C2) | BUILT | all | No refund. The recompute undoes the effects exactly |
| D-84 | Law composition: dials and structural parameters as a pure function of the enacted laws | dial tiles | SM:2005-2147 (Recompute*); `Data/StructuralParameters.cs`:90-124 | §20; §31; §302; §304 | BUILT | all | Not recomputed on load (§5 SD-19) |
| D-85 | Competence gating | lock text (GC:8750-8753) | `LC.IsWithinCompetence` :2514-2517; SM:1866-1871 | §330 ruling (a); §500 | BUILT | DE, FR, IT get no monetary laws; the USA gets no electricity-tax laws | — |
| D-86 | Crime & justice dials "set by law" | Laws › Crime & justice tiles (GC.LawsV35:249-296, 321-362) | SM:2052-2090 | §738 | BUILT | all | — |
| D-87 | Labour dials: a statutory base plus law offsets | Laws › Labour sliders (GC.LawsV35:148-197) | SM:1767-1800, 2110-2147 | §737; LawDefinition.cs:42-49 | BUILT | all | Minimum-wage laws are inert in SE and IT, which have no statutory minimum wage (§756) |
| D-88 | Law verbs (D24) | list rows (GC.LawsV35:975-976; GC:8443) | `UI/LawVerbs.cs` (`BothWaysReading` empty :21) | FL D24; §662; §663 | PARTIAL | all | The 13 both-ways laws show no verb |
| D-89 | Support-agreement law items | Parliament tab agreement block | `Elections/SupportAgreement.cs`:99-176; SM:1988, 2767-2790 | §635; §654 | BUILT | where a supporter exists | — |
| D-90 | Carbon rate statute (EN-4e) | Budget carbon row | `Data/CarbonRateStatute.cs` `AdvanceYear` :105-129 (`ComingYear` = 2023 + turn + 1, :95); SM:4935, 5283 | §464; §467; §471; MR :237-243 | BUILT (with discrepancies, §5 SD-12, SD-13) | SE indexed; DE BEHG schedule; FR nominal; IT, PL, US have no rate | — |
| D-91 | Pension age statute (PN-1 layer and mark) | Budget pension row (GC:10523-10720) | `Data/PensionAgeStatute.cs` (rules :96-110; `AgeInForce` :126-133) | §489; §491; §509; §515; FL PN-1 | BUILT | all six | — |
| D-92 | Pension age dial: a budget-bill override, 60-70 | drag the pension row → `BuildBudgetBillFromDrafts` (GC:9845-9855) → `IntroduceBudgetBill` | `Data/BudgetBill.cs`:70-74; PS:524; `Country.PensionAgeOverride` (Country.cs:169) | §590; §596; §597; FL PN-1 | BUILT (◐ per the row: the override freezes the age) | all; in PL a vetoable act of its own | — |
| D-93 | Law conflicts and prerequisites; laws for the dials-only set | none | LawDefinition.cs:70-78 ("deferred to v2") | §378 | NOT BUILT (planned; Elias's question) | — | Exactly cancelling pairs can both be enacted (§3) |

### 1h. The central bank

| ID | Feature | Player-facing entry | Code | Spec authority | Status | Countries | Notes |
|---|---|---|---|---|---|---|---|
| D-94 | Policy rate by rule with an appointed head | bank pages, read-only (GC.PoliticsV35 ~700-749; GC:4064-4173; `UI/GameController.RiksbankBoard.cs`:30-143) | `FRS.ApplyFedChairInterestRate` :149-157 (target = Clamp(Taylor + bias, 0, 15), moving 15 % of the gap per turn); `Simulation/CurrencySystem.cs`:48-55 | MR "## Federal Reserve" :663-740; "## Federal Reserve Rate Damping" :742-798; §100 (C-C7) | BUILT | US, SE, PL | The player's `InterestRateChange` is ignored there |
| D-95 | Appoint the head: 2-3 candidates every 4th turn | Docket candidate cards "Appoint" (GC.DocketV35:204-261, write :253) or the bank tab (GC:3972-3979, 4239-4258, write :4250) | `GC.UpdateFedChairSelectionState` :3833-3855; `FRS.GenerateCandidates` :95-135 | MR :702-712; §100; §630; §731; §746 | BUILT | US, SE, PL; PM only | A forced pick that holds the clock. No SimulationManager method. The term is the turn cadence (`ElectionSystem.IsElectionTurn`) |
| D-96 | Euro-member national rate push, ±0.75 | dial (GC:4031) → `BuildPlayerDecision` (GC:6277-6282) → AdvanceTurn, only if PlayerGoverns (GC:6105) | `EurozoneRateSystem.ApplyEurozoneRate` :118-127 (push clamp :105) | MR "## Eurozone Rate Voice" :2753-2812 | BUILT | DE, FR, IT | Free every turn; moves the rate by at most about 0.11 pp |
| D-97 | Taylor rule | the bank page's waterfall | `Simulation/TaylorRule.cs`:128-135 | MR "## Federal Reserve"; §230; §412 | BUILT | all | — |
| D-98 | Monetary-regime laws (10) | statute book | LC:2190-2214; StructuralParameters.cs:104-113 | §330 | BUILT | SE, PL, US | — |
| D-99 | ±2 rate dial for an own-currency country with no head | dial (GC:3985-4007) | `CurrencySystem.cs`:70-84 | — (superseded by §100) | CODE-ONLY (a dead branch) | none reachable | No code sets `CurrentFedChair` to null |

### 1i. Foreign policy

| ID | Feature | Player-facing entry | Code | Spec authority | Status | Countries | Notes |
|---|---|---|---|---|---|---|---|
| D-100 | Foreign-policy meetings (4 kinds) | Docket item → Decide → `DrawForeignPolicyMeetingModal` (GC:9168-9189) → `SM.ResolveForeignPolicyMeeting` (SM:1149-1160) | FPS (0.01 per day :36; pool :40-63; `ApplyMeetingOption` :89-99); `SM.TryRollForeignPolicyMeeting` :1134-1146, gated at SM:298 | MR "## Continuous Time Migration Phase 0" :3702-3722; §630; §634; §731; FL PF-11 (closed §648) | BUILT | player's country; PM, or a junior partner holding Foreign | No expiry; holds the clock |
| D-101 | Trade bill | see D-65 | — | — | BUILT | — | — |
| D-102 | "Set override": pin a partner at today's rate | tile "Set override" (GC.LawsV35:856-876, write :873) | `TradePartner.PlayerTariffOverride` (`Data/TradePartner.cs`:33) | MR :1273-1275 (R-D2) | BUILT | all | Free, immediate, irreversible; a UI-only write |
| D-103 | Partner retaliation (mirrors an override) | none | `TradeSystem.cs`:110-145, 154-157 | MR "## Per-Partner Tariff Overrides" | BUILT | all | — |
| D-104 | Retaliation against a base hike (T-5), its memory (T-6), volumes indexed to GDP (T-4) | none | gaps stated at TradeSystem.cs:116-121 | FL T-4, T-5, T-6 (§197) | NOT BUILT (trigger rows) | — | — |
| D-105 | Trade blocs: EU at 3 % external, 0.1 % internal | none | `Data/WorldFactory.cs`:1002-1003, 1023; TradeSystem.cs:64-108 | MR trade sections | BUILT (fixed) | the EU five | EU members can set bilateral tariffs inside the EU (CODE-ONLY, §3) |

---

## 2. Specified but missing

These are ledger rows of type "spec". Within a group, the order is roughly the order of what a player would notice.

**Campaign** (CAMPAIGN SPEC is a reference; FL rows are named where they exist)
- §3 pre-campaign:
  - "Raise money": refused, "no donor model exists" (PCR:59).
  - "Select campaign strategy", "Develop policies", "Train candidates": refused (PCR:60-62).
  - "Identify voter groups": no verb.
- §3 campaign:
  - "Participate in debates": no player input.
  - "Attack opponents" and "Defend against attacks": no player verb. `ToDecision` never sets a target party (PlayerCampaignRecord.cs:102-109).
  - "Respond to events": §18 events never reach the run.
  - "Change campaign strategy": queuing it crashes (§5 SD-01).
- §9:
  - 4 of 9 staff roles are missing (§68; §37 deferred, FL F-1).
  - Debate preparation is not charged; it is 8 h in code, against the spec's 6 h (CR:1099).
  - No regional tour.
  - No campaign income of any kind (Fundraise refused; no state support, LSET:117).
- §10: the player cannot site offices; local polling and staff capacity are defined but never read.
- §11: the player cannot choose a strategy (it is the cast personality's, LSET:329, 362).
- §12 targeting:
  - rally speaker and size;
  - TV region, demographic, frequency;
  - digital targeting;
  - post categories and virality ("virality is not modelled", MediaSystem.cs:169).
- §14: "Players can choose which outlets to target" is absent; C-N8 is open (FL).
- §15: no pre-debate or in-debate player choices.
- §16: Leadership and Media Skill are absent (`Elections/ElectionTypes.cs`:241-252).
- §17: Resign does not replace the candidate. It is refused by ruling (§503; trigger T-9).
- §18: events never shift salience. `eventSalience` is always 0 (CR:588), and the salience shift is computed but never applied (CampaignActions.cs:199).
- §20-§21:
  - no demographic or regional breakdowns;
  - the player cannot buy a poll during the campaign;
  - the W-E4 offer ladder is film-only.
- §25: the swing index is never live (LCS:200-201).
- §26-§27: GOTV, turnout and election noise do not reach the live count. `GetOutTheVote` has no executor.
- §30: the W-E7 results sheet is unreachable; the voter-group block is absent.
- §36: no intelligence, field reports or focus groups. The scandal probability is a constant.
- §37: staff progression (FL F-1).
- §38: capital affecting the next campaign; donor and grassroots networks.
- §39: momentum and turnout as layers of the final share.
- §2: other election types (FL F-2).
- FL K-1c: the cartogram's 2026 mandate column (Design).
- FL D-DE: the Länder map in the HQ (D-DE was built for the night; the HQ map is Sweden-only).

**Approval**
- R-CL1 (§119): party approval moves nothing and is read by nothing (Country.cs:960-965).
- CAMPAIGN SPEC §19:
  - Growth, public satisfaction (approval) and the other tracked items are not read by the vote (PerceivedPerformance.cs:184; EconomicVote.cs:30-31).
  - Perception through media or attacks is absent; the index reads `Published` only (PerceivedPerformance.cs:21-23).
- CAMPAIGN SPEC §8: per-group loyalty is not live.
- `docs/specs/ENERGY_SPEC.md`:61 and :262 claim an approval → parliament link ("This game already has the last four links"). There is none (PS:53-80; StanceModel has no approval term). **SPEC-ONLY.**

**Events**
- CAMPAIGN SPEC §18:
  - Events do not affect issue salience or elections except through the economy (EventSystem.cs:21-23).
  - No event moves crime; there is no war event.

**Scenarios**
- AUDIT_PROTOCOL's "three challenge scenarios" (:35): two exist (see the appendix).
- The premise is never shown (ScenarioDefinition.cs:32). The objectives are shown only on the verdict (GC:6836).
- Per-scenario term accumulation: FL T-1.

**Cabinet**
- Popularity-scaled dismissal (§235): its code is dead (§5 SD-09).
- POLSYS §5.2: "the cabinet" as part of the AI's machinery (INFERRED reading).
- The economy portfolio (`Data/CabinetPortfolio.cs`:4-6, 18-20; §634 says "there is no economy portfolio").
- Gamson's small-party bonus and allocation by manifesto emphasis (GovernmentRecord.cs:55-58).
- §336's "effectiveness is what the coupling table reads": only Health reads it.

**Parliamentary gating**
- POLSYS §5.2:
  - "the party's votes" as a player control: **SPEC-ONLY**.
  - Junior partner: "negotiate the budget in cabinet" and "threaten to leave": NOT BUILT. "A say in the budget through the coalition agreement": **SPEC-ONLY**; the phrase appears only in refusal text (SM:1197).
  - Support party: "enforce" and "threaten": NOT BUILT.
  - Opposition: "table motions" (member bills): NOT BUILT; §630 says so.
- POLSYS §6 and USA_STAGE_PLAN: the Senate, bicameral bills, the US veto and override (US-15/18/21/22): NOT BUILT, PS-6 parked (§803).
- POLSYS §8: the French president's powers.
- POLSYS §4 and §9 stage 7: the Italian Senato.
- FL PS-3 says "the simulation refuses by one rule". It does so only for bills and rolls. Appointments, the chair, fleet orders and overrides are UI-only gates (§5 SD-38).

**Laws and statutes**
- B1, carbon: "a passed bill's figure for the coming year wins" (§471; `CarbonRateStatute.cs`:18-19; MR :240) does not hold for the player's budget in Germany during the BEHG schedule, nor in Sweden, where the rate is indexed again (§5 SD-12).
- B2, POLSYS §3: "Statutes read the calendar year … so every statute is true for the chosen date".
  - The carbon schedule reads 2023 + turn + 1 (§5 SD-13).
  - The pension paths begin in 2026, so the Italian (2022), French (2024) and German (2024) starts read the 2026 age (PensionAgeStatute.cs:99-109, 126-127).
- D24: verbs for the 13 both-ways laws (LawVerbs.cs:21).
- PN-1 "still open": the override freezes the age, and no check drives the bill route end to end (FL PN-1).
- Law conflicts and prerequisites (LawDefinition.cs:70-78); laws for the dials-only set (§378).
- MR has no section on the law system (no "law", "LawCatalog" or "EnactedLaw" heading).

**Central bank**
- §100: the pressure mechanics are "recorded as future, not built".
- POLSYS §6: the US president's appointments (PS-6).
- GC.PoliticsV35:728 and §746 say the term ends "on the bank's own cycle". The code uses one generic 4-turn cycle (GC:3843).

**Foreign policy**
- FL T-4, T-5, T-6 (trigger rows); FL E-4 (Elias's felt verdict on the trade bill's costs).
- MR :3720-3722: "ongoing-process budgets" deferred.

---

## 3. Present but unspecified

These are ledger rows of type "unspecified".

**Campaign**
- The player's party **auto-buys internal polls** at its cast's cadence, 120,000 kr and 1 hour each (CR:680-696, before the script at :702). No record states this. It contradicts §207 ("an empty day is a day of nothing") and PlayerCampaignRecord.cs:53.
- The one-region rule for local acts (LCS:155-173).
- The 0.30 deny line (CR:1082).
- A sacrifice removes the newest hire (CampaignStaff.cs:187-195).
- The 12 hours a day cap (CampaignResources.cs:43, 91).
- The legality panel's "N OF 17 VERBS" (GC.Campaign:1046-1047).
- Kronor in Germany (LSET:189-190, stated as billed).
- The extra-election calendar: no run-up, at most 8 weeks (SM:4283-4287; "a premise", §636).
- Constants and data read by nothing:
  - `LocalPollSampleSize`, `StaffCapacity` (CampaignOffices.cs:40, 45)
  - `MarginalEffectiveness` (CampaignResources.cs:56)
  - `SlotsFor` (MediaSystem.cs:508)
  - `AttacksOn` (CampaignReactivity.cs:118)
  - `ToTurnoutBonus` (CampaignActions.cs:347)
  - the candidate's `CampaignSkill` and `ScandalResistance`
  - `MediaSystem.ResolveThroughOutlet` :281-315 and `MediaInterest.Bookings` :383-440, which have no runtime caller

**Approval and events**
- The law browser's "Affordable now: N of M" counts laws by `EnactmentApprovalCost <= approval` (GC:8812-8825). There is no such gate in the model.
- `EventSystem.Enabled` (a harness switch).
- The incumbency multiplier, wired to nothing (PerceivedPerformance.cs:11).

**Scenarios**
- A mid-run `StartScenario` keeps the running world (GC:2045-2047, 2158-2164); harness use.
- `ScenarioProgress.StartTurn` is written (ScenarioEvaluator.cs:34) but never read.

**Cabinet**
- `StagePendingCabinetDecision` (SM:1086): a harness hook with no validation (named "P2-4.3" in its doc).
- Search works, and draws the Cabinet stream, in any role (GC.PoliticsV35:354).
- A decision whose minister has gone still resolves, at the full approval factor (GC.DocketV35:67-69; CS:584).
- The player's appointees outlive a change of government and keep rolling leaks and resignations under an AI government (SM:5158-5164).
- Unused: `CabinetDecisionOption.StateLean` (`Data/CabinetDecision.cs`:50-54) and `Effectiveness.LargestDeparture` (PortfolioEffectiveness.cs:139-144).
- `Country.CabinetEvents` is never pruned (CS:519).

**Gating**
- The signing ceremony fires for every division of the player's country whatever the role (GC:2280-2300), so an opposition player "Signs" the AI government's budget.
- A caretaker PM keeps every lever (SM:2632-2642).
- The budget window has no expiry (SM:2419-2422). With `HoldOnBudgetWindow` off, the clock runs on (GC:1387). INFERRED.
- Poland's veto draw is keyed on (seed, country, date, title) (PresidentialVeto.cs:91-99), so re-tabling a vetoed statute on another day re-rolls it.
- `TableShadowBudget` has no PM refusal (SM:1189-1204).
- A tabled alternative's `DaysRemaining` is copied once and never read (SM:1200).
- `ParliamentSystem.RandomSource` is declared and unused (PS:51); the local `direction` is unused (SM:1909).

**Laws**
- No per-country gating beyond competence: the USA can "Join Frontex border patrols" or "Adopt the EU wage directive"; Europeans can "Deputise police for immigration" (287(g)) (LC ids `frontex_border_cooperation_agreement`, `adequate_minimum_wage_directive_act`, `ice_287g_agreements_law`).
- An unknown LawId is accepted and resolves "uncontested" (C2 below; SM:1863-1887; LC:2514-2517).
- Bills for a non-player country pass the gate (SM:2654) and never resolve in play (GC:783 ticks the player's country only).
- `GetPendingLawBills` returns the live inner dictionary (SM:1849-1854).
- `AdvanceLawBillsDay` is public with no date guard (SM:1890).
- Exactly cancelling pairs can both be enacted, net zero:
  - `full_employment_mandate_act` / `single_mandate_act`
  - `neutral_rate_reassessment_act` / `monetary_normalisation_act`
  - `household_electricity_price_shield_act` / `household_electricity_tax_restoration_act`
  - `industrial_electricity_tax_relief_act` / `business_electricity_tax_restoration_act`
  - `third_party_reporting_act` / `tax_amnesty_act`
- Two laws have an empty concern and pass in any chamber: `debt_office_long_issuance_act` and `electronic_invoicing_mandate_act` (LrEcon 0, LC:2133, 2160; PS:344).

**Central bank**
- The dead own-currency ±2 dial branch (GC:117, 3985-4007; CurrencySystem.cs:70-84).
- AI-run head countries (the US, SE or PL when not the player's) keep their seeded placeholder head forever (GC:3837-3852).
- The preview prices the undamped target; the projection takes the damped step (SM:5366-5370 vs `Simulation/RatePathProjection.cs`:40; MR :2797-2801 documents this as an approximation).
- The head's term follows `ElectionSystem.IsElectionTurn`, which no election uses any more (GC:3843).

**Foreign policy**
- EU members can set bilateral tariffs on fellow EU members (GC.LawsV35:788-797; TradeSystem.cs:55-73). Compare the treaty-competence rule for monetary laws.
- A trade bill with no changes can be introduced and always passes (GC.LawsV35:1276; PS:344).
- Meeting approval is not scaled by the Foreign minister's popularity (FPS:98); cabinet options are (CS:582-588).

---

## 4. Invariant map

These are what a harness can assert on every run.

**Status values**
- *enforced*: a clamp, refusal or throw holds it.
- *structural*: true by construction, with no check.
- *log-only*: a `Debug.LogError` reports it but play goes on.
- *NOT enforced*: it should hold, and nothing makes it.

| # | Invariant | Read at | Enforced at | Status |
|---|---|---|---|---|
| INV-C1 | Every party's campaign money is ≥ 0 and finite, every day | `CampaignRun.State.Pools[p].Money` (`Elections/CampaignResources.cs`:74); `PreCampaignRun.State.Money` (PCR:95) | ctor :78-88; `TrySpend` :99-113; `WithMoney` :116; CampaignOffices.cs:112, 133; CampaignStaff.cs:212; PCR:196, 238, 248 | enforced for negatives, **not for NaN** (a NaN spend passes, §5 SD-03) |
| INV-C2 | No campaign income: money never rises from one day to the next | same fields | structural (no writer adds money; Q4 of the engine read) | structural |
| INV-C3 | Conservation: StartingMoney − MoneyLeft = Σ MoneyByAction + PollMoney + OfficeMoney + StaffMoney | ledger lines CR:882, 691, 468, 651, 725, 665; MoneyLeft CR:1006 | — | structural |
| INV-C4 | Each cost is deducted exactly once | `TrySpend` CR:801 (acts), :682 (polls); `OfficeNetwork.Open`/`Day`; `PayDay` | one site per cost | structural. Door-to-door discards the unused part of the spend (CR:826 `out _`) |
| INV-C5 | Hours: 0 ≤ Pools[p].Hours ≤ 12; hours spent per party per day ≤ 12 | `ResourcePool.Hours` | `StartDay` :91; `TrySpend` :106 | enforced |
| INV-C6 | Final shares are each ≥ 0 and sum to 1 (±1e-9) | `CampaignRun.Result.FinalShares` (CR:1019-1021) | `PreferenceModel` normalises (:169); `ApplyRecordShift` keeps the sum (EconomicVote.cs:199-206) | `WithinReach` can drop mass when room ≤ 0, with no re-normalisation (CR:1192): NOT enforced in that branch |
| INV-C7 | The day counter steps by 1; Finished ⇔ Day ≥ TotalDays; no step on or after polling day | `CampaignRun.State.Day` (CR:990, 351) | SM:4310 | enforced |
| INV-C8 | No pending story at the finish; no story breaks on the last day; at most one per party per day | `PendingScandals` | CR:913-917 | structural |
| INV-C9 | Credibility stays in [0, 0.6] and never rises | CR:550 | cost capped at Scandals.cs:227 | structural |
| INV-C10 | Coverage stays in [0, ≈4.85]; the daily gain is in [0, 1) | MediaSystem.cs:184-188, 363 | formula | structural |
| INV-C11 | Interviews per party per day ≤ bookings; bookings per outlet ≤ slots | CR:785, 833-835; MediaSystem.cs:488 | refusals | enforced |
| INV-C12 | At most one office per region per party, only where it stands; volunteers ≤ 150; influence in [0, 1] | CampaignOffices.cs:112; CR:457 | refusals | enforced |
| INV-C13 | Each staff role at most once | CampaignStaff.cs:175 (throws on a duplicate); PCR:237, 282 | throw | enforced |
| INV-C14 | Debates only on days 20 and 41, two distinct parties, equal coverage, opposite momentum | CR:933-955 | — | structural |
| INV-C15 | After a load, every campaign stream's draw count equals the save's | SM.AssertReplayLandedOn :4638-4650 | — | **log-only** ("CAMPAIGN REPLAY: stream …"). A harness should assert no such line |
| INV-C16 | The queue holds only legal, executable inputs: kind among the eight in the campaign; spend ≥ 0 and finite; issue in 0-8 or none; region in [0, Regions) or national; scandal answer a valid enum | SM.QueueCampaignDecision :4538-4562; QueueScandalResponse :4516-4528 | **NOT enforced**; failures land later at CampaignActions.cs:142-143, CampaignResources.cs:101-104, CR:1226, Scandals.cs:172 | NOT enforced |
| INV-C17 | The HQ shows the engine's stored values: money = Pools[p].Money (or `pre.Money`); hours; queue rows = `CampaignRecord.QueuedFor(day)`; race = `LatestPoll` or `PublicPoll`; story panel ⇔ `SM.PendingPlayerScandal()` | LCS:41-146, 237-287; GC.Campaign:439, 453-474 | — | structural; display lags in §5 SD-30–34 |
| INV-C18 | While a story waits (or the opening, with the setting on), `SM.CurrentDate` does not change across Update frames | GC:761-766, 807-814 | UI hold | enforced in the UI only. `SM.AdvanceDay` itself never holds |
| INV-A1 | 0 ≤ ApprovalRating ≤ 100 after every writer | `EconomyState.ApprovalRating` (EconomyState.cs:44-45) | clamps at MacroSystem.cs:2325; EventSystem.cs:472; FPS:98; CS:514, 600; PS:468, 527, 556, 586, 632, 666, 742, 794, 864, 1157; SM:1317, 1986; GC.PoliticsV35:402 | enforced at writers. The ctor does not clamp (EconomyState.cs:390); the load path is unread |
| INV-A2 | Approval ledger identity each boundary: \|ΔA − (events + terms + clamp loss)\| ≤ 0.001 | ApprovalLedgerRecorder.cs:27, 87-95 | — | **log-only** ("ATTRIB:"; fatal only in Editor harnesses through CheckExit). Assert zero ATTRIB lines |
| INV-A3 | Clamp loss is nonzero only when the formula's result sits at 0 or 100 | ApprovalLedgerRecorder.cs:105-111 | — | log-only |
| INV-A4 | The approval formula runs exactly once per country per AdvanceTurn | SM:5121 (one call site besides the preview, :5404) | — | structural |
| INV-A5 | `PreviewTurn` never mutates real approval and draws no Event stream | SM:5183-5185; clone ledger :5668-5686 | — | structural |
| INV-A6 | Perceived and term index in [0, 100] (50 with no component); EconomicVote magnitudes in [0, 0.10]; the partner line floored at 0; support capped at the smallest partner | PerceivedPerformance.cs:81-92, 205; EconomicVote.cs:35-37, 78, 82-85 | clamps | enforced |
| INV-A7 | The record shift is judged once and stored; a replay never re-judges | SM:4229-4245, 4346-4350 | — | structural |
| INV-A8 | Polls: shares ≥ 0 summing to 1; none drawn before PreCampaignStart; at most one run-up poll a day; one tracker every 7 campaign days | Polling.cs:148-165; PCR:247; CR:574 | — | structural |
| INV-E1 | At most one event per country per AdvanceTurn; Event-stream draws per turn = 6 + the number fired (INFERRED); zero when `Enabled` is false | EventSystem.cs:444-454; SM:5125 | — | structural |
| INV-E2 | After `ApplyEvent`: the GDP shock is spread over 365 days and has run out by the next boundary; GDP is unchanged at the boundary itself | EventSystem.cs:468-470; MacroSystem.cs:470-473 | `Assets/Editor/EventShockPathDiagnostic.cs`:9-17 | enforced by a check |
| INV-E3 | Inflation stays in [0, 30] after an event | EventSystem.cs:471 | clamp | enforced |
| INV-E4 | The realised event rate is 0.40 within 3 SE; pool ≥ 48 events, unique names, \|GDP\| ≤ 3, every event cited | `Assets/Editor/EventRateDiagnostic.cs`:77-84; `Assets/Editor/EventPoolCheck.cs`:18-50 | bar checks (CheckSuite.cs:358, 504) | enforced on the bar |
| INV-S1 | A scenario verdict resolves once; EvaluateAtBoundary is a no-op afterwards | ScenarioEvaluator.cs:53 | guard | enforced |
| INV-S2 | Evaluated once per AdvanceTurn, never per day | GC:6137 | — | structural |
| INV-S3 | Terminal objectives are judged at the first boundary with currentTurn ≥ EndTurn; NeverBreach is lost at the breaching boundary; Sustained is sticky once met | ScenarioEvaluator.cs:80-86, 94-109, 131-170 | — | structural |
| INV-S4 | Save → load keeps the progress shape; an unknown id is refused *(history: the scenarios retired, §825 - an older save's scenario is read past and named in the log)* | GC:1045-1061; `Assets/Editor/ScenarioSliceDiagnostic.cs`:194-246 (not on any bar) | refusal | enforced |
| INV-K1 | One minister per portfolio; `CabinetMinisters[p].Portfolio == p` | `Country.CabinetMinisters` (Country.cs:869) | the dictionary key; the second half is NOT enforced (holds because of GC.PoliticsV35:456) | partly |
| INV-K2 | A cabinet decision resolves once and is removed | SM:1114-1117 | removal only. **No check** that it was pending or that the option belongs to it (SM:1102-1118) | NOT enforced at the API |
| INV-K3 | A decision or meeting is pending only for a portfolio the gate allowed at roll time | SM:5157; SM:298 | roll gate; **not re-checked at resolve** (by design, §630) | partly |
| INV-K4 | AI countries have no ministers and no pending decisions | SM:5157 | — | structural, NOT enforced |
| INV-K5 | Pending cabinet decisions are empty at each boundary in play | the comment at SM:5143-5146 | the UI hold only (GC:761-766) | NOT enforced headless (Testing/PlayerSmoke.cs:73-74 can pile them up) |
| INV-K6 | After `AllocatePortfolios`: all six posts held exactly once, only by cabinet parties; the PM's party holds at least one when another holds two or more | GovernmentRecord.cs:149-201 | algorithm. `FromProposal` (:213) trusts the sheet | partly |
| INV-K7 | No seatless party in a formed cabinet or support | CoalitionFormation.cs:352-363 | assert (throws) | enforced |
| INV-K8 | Bounds after a cabinet option: crime, poverty and youth unemployment in [0, 100]; debt clamped; the trade balance is not bounded | CS:593-606; SM:6736-6738 | clamps (except trade) | partly |
| INV-P1 | At most one pending bill per slot: per country for budget, labour, crime, sector, trade and SWF; per TaxType and WelfareProgramType; per LawId; one alternative | SM:1509, 1608, 1668, 1722, 1812, 1879, 2175, 2240, 2330, 1196 | refusals | enforced |
| INV-P2 | Every pending bill has 1 ≤ DaysRemaining ≤ 21 after each day tick | set at SM:1181, 1513, 1613, 1673, 1727, 1817, 1884, 2180, 2251, 2335 | countdown | structural |
| INV-P3 | A refused `Introduce*` changes no state (only a `LEVERS:` log line) | SM:1508-1517, 1601-1614 … | checks before mutation | structural |
| INV-P4 | No `Introduce*` succeeds for a player who is not the PM's party, except a junior partner's portfolio bills | SM:2650-2690 | gate | enforced |
| INV-P5 | An ordinary division's `Passed` equals `WouldBillPass(country, concern)` at resolution (veto votes and frame decisions excepted) | PS:342-353, 1130-1145 | — | structural |
| INV-P6 | Frame decision: VotesFor + VotesAgainst + Abstentions = the seated chamber; Passed is true; AlternativeAdopted ⇔ VotesAgainst > VotesFor | SM:1219-1260 | — | structural |
| INV-P7 | `Required` > 0 only in Poland, and Required = ceil(0.6 × voting); a vetoed passage is a Motion | PresidentialVeto.cs:120-127; SM:1460, 1471-1472 | — | structural |
| INV-P8 | In any division on a player's bill the player's party votes FOR | StanceModel.cs:286-293 | — | structural |
| INV-P9 | The division log holds ≤ 24 entries and Number strictly increases | `Data/DivisionRecord.cs`:95, 120-133 | ring buffer | enforced. Ceremonies can be evicted (§5 SD-41) |
| INV-P10 | A failed bill costs exactly 1.5 approval (clamped), writes one ledger event, and sets `BillLostOn` | PS:49, 742, 1143, 1150 | — | structural |
| INV-P11 | The AdvanceTurn decision for the player's country is `None()` unless PlayerGoverns | GC:6105 | — | structural |
| INV-P12 | The Parliament stream's draw count is always 0 | SimulationRandom.CaptureDrawCounts; PS:51 has no consumer | — | structural |
| INV-P13 | With `VerifyHits` on, `ChamberVerdicts.Drifts` == 0 | ChamberVerdicts.cs:42-45, 212-235 | — | check hook |
| INV-L1 | No law enacted twice: `Country.EnactedLaws` has distinct LawIds | Country.cs:83 | guard SM:1973-1977; the only writer is :1985 | enforced |
| INV-L2 | Every enacted id is in the catalogue | `LC.GetById` :2527 | — | NOT enforced (only a save edit breaks it; an orphan can never be removed, SM:1959-1963) |
| INV-L3 | Pending law bills: key == bill.LawId, the id is in the catalogue, IsRepeal matches whether it is enacted, the player's country only | SM:1849, 1879; LawBill.cs:25 | — | **NOT enforced**: an unknown id is accepted; IsRepeal is guaranteed by the UI only |
| INV-L4 | Competence: no enacted MonetaryRegime law where the country does not own its currency zone; no ElectricityTax law in the USA | LC:2514-2517 | refusal at SM:1868 | enforced |
| INV-L5 | Each C&J dial = Clamp(50 + Σ deltas, 0, 100); each labour dial = Clamp(base + Σ deltas); each structural parameter = Clamp(base + Σ, Min, Max) | SM:2005-2147; StructuralParameters.cs:90-124 | recompute on each law event | structural. **Broken by a direct CrimeJusticePolicyBill** (§5 SD-07); not recomputed on load |
| INV-L6 | Enact X then repeal X leaves every dial and parameter bit-identical, with approval = before − cost | `Assets/Editor/LawCompositionDiagnostic.cs`:87-95 (§292) | check | enforced on the bar (INFERRED that it runs on the bar) |
| INV-L7 | Pension: `PensionAgeOverride` is −1 or in [60, 70]; AgeInForce is in [60, 70]; statute figures in [62.75, 67.25]; CalendarYear == CurrentDate.Year after day 1 | PS:524; BudgetBill.cs:74; PensionAgeStatute.cs:98-133; SM:2485-2490 | clamp | enforced, **except NaN** (passes `Mathf.Clamp`) |
| INV-L8 | Carbon at the boundary (use the public `SM.BoundaryLedger` seam: "open" SM:4932, "policy" SM:4947). DE: Rate == `BehgFor(2023 + turn + 1)` while scheduled. SE: Rate × round(ratio, 4). Others unchanged. Every ceiling × ratio | CarbonRateStatute.cs:105-129 | — | structural (see §5 SD-12) |
| INV-L9 | Electricity tax (households and non-households) in [0, 60]; both 0 in the USA | StructuralParameters bounds | clamp | enforced |
| INV-L10 | Each resolved law bill appends one DivisionRecord titled "Enact: X" or "Repeal: X" (plus one veto division in Poland when vetoed) | SM:1912, 1471 | — | structural |
| INV-B1 | Every zone's rate stays in [0, 15] after every boundary | `CurrencySystem.cs`:84; FRS:152-156; `EurozoneRateSystem.cs`:123-126 | clamps | enforced, NaN not guarded |
| INV-B2 | Damping is exact: Δrate = 0.15 × (Clamp(rule + bias or push, 0, 15) − old), so ≤ 2.25 pp a turn | FRS:156; EurozoneRateSystem.cs:126 | — | structural |
| INV-B3 | The Taylor reading is ≥ 0 | TaylorRule.cs:134 | floor | enforced |
| INV-B4 | Exactly the US, SE and PL have a head; a head-run zone is never shared | `Country.CurrentFedChair` (Country.cs:848); CurrencySystem.cs:51-55 | — | NOT enforced (a head on a euro member would set the whole zone) |
| INV-B5 | A candidate list exists only if the player governs, the country has a head, and the next turn is a multiple of 4; 2-3 candidates with distinct philosophies; RateBias ∈ {±1.5, ±1.25, ±0.25, 0} | GC:3833-3855; FRS:52-112 | — | structural |
| INV-B6 | Euro push in ±0.75; AI members submit an empty decision | EurozoneRateSystem.cs:105; GC:6105 | clamp | enforced |
| INV-F1 | At most one pending meeting per country; no roll while one is pending | SM:937-938, 1136-1139 | guard | enforced |
| INV-F2 | Resolving clears the slot | SM:1159 | **no check that a meeting was pending or that the option came from it** | NOT enforced at the API |
| INV-F3 | Base tariff in [0, 50]; an override is −1 or in [0, 50] and never returns to −1 | SM:5821, 5842; TradePartner.cs:33; GC.LawsV35:873 | clamps | enforced (NaN passes `Mathf.Clamp`) |
| INV-F4 | AI countries never hold overrides | TradePartner.cs:20-32 | — | true in play; NOT enforced (`IntroduceTradeBill` accepts any country the gate passes) |
| INV-F5 | Retaliation = fraction × max(0, override − normal); own + retaliation < 100 | TradeSystem.cs:121-124, 143-144 | — | structural |
| INV-F6 | Blocs never change after creation; an EU member never charges its base rate | TradeSystem.cs:89-107 | — | structural |
| INV-G1 | While any hold stands, CurrentDate does not move in play (fed pick, opening, story, cabinet decision, meeting, budget window, declaration, formation) | GC:761-766, 807-814 | UI | enforced in the UI only |

---

## 5. Suspected discrepancies

Every entry is SUSPECTED: a lead for a run, not a finding. "API" means reachable only through a direct call or a tampered save. "UI" means reachable by a player.

### 5a. Exceptions, crashes and NaN (candidate S1s)

| # | Lead | Where | Path |
|---|---|---|---|
| SD-01 | In the Campaign phase, five non-§12 kinds (RecruitStaff, Fundraise, EstablishOffice, CommissionPolling, SetStrategy) are **legal and queueable**. `ToDecision` → `CampaignActions.Spec` then throws ArgumentException inside `StepDay`, which runs from `AdvanceDay` with no catch. So the same day throws every frame, and the campaign never finishes (INFERRED). The queue is saved, so the replay throws too, and `LCS.Build` throws while listing it. §492 records the same throw class once | CampaignClock.cs:155-163; SM:4554-4561; PlayerCampaignRecord.cs:102-109; CampaignActions.cs:142-143; LCS:49 | API. The HQ lists them as "OPEN TO YOU TODAY" with no chip (GC.Campaign:1005-1049) |
| SD-02 | **Choosing a cabinet decision on the Docket probably throws InvalidOperationException.** The Docket runs `foreach` over the live list from `GetPendingCabinetDecisions`. The body is invoked synchronously. CHOOSE → `ResolveCabinetDecision` → `RemoveAll` on that same list, so the next `MoveNext` fails the version check (INFERRED, .NET List semantics). The option is applied once and the GUI event aborts with a logged exception. The headless runner avoids it with `.ToList()` | GC.DocketV35:65-76, 192-198; GC:9101-9104; SM:1096-1099, 1114-1117; Testing/SimulationTestRunner.cs:204 | **UI** |
| SD-03 | A NaN campaign spend passes `TrySpend`: both comparisons are false and the ctor admits NaN. The chest becomes NaN, every later spend is "affordable", and NaN reaches the shares | CampaignResources.cs:78-88, 99-113 | API |
| SD-04 | A negative campaign spend throws ("a spend cannot be negative") inside `StepDay` → `AdvanceDay` | CampaignResources.cs:101-104; SM:477 | API |
| SD-05 | An issue id ≥ 9 throws IndexOutOfRange at `TrueMessage`, *after* the money is deducted. An out-of-range `ScandalResponse` throws in `Scandals.Table` the next day, having already released the UI hold | CR:1226, 801-802; Scandals.cs:172; SM:4516-4528 | API |
| SD-06 | `IntroduceLawBill` with a null LawId throws at `ContainsKey(null)` (the competence check passes on `law == null`). A null bill throws NullReferenceException. An **unknown** LawId is accepted and resolves "Enact: <id>" uncontested | SM:1863-1887; LC:2514-2517, 2527-2538 | API |
| SD-07 | A direct `IntroduceCrimeJusticeBill` with an object initialiser (for example `{PoliceFunding = 55f}`) zeroes the other five C&J dials, because unset fields default to 0f and the applier writes values ≥ 0. It overwrites the law composition until the next law event. The film harness does exactly this | Data/StandalonePolicyBills.cs:28-33; SM:5942-5950, 6088-6105; Testing/UiScreenshotDriver.cs:1896, 3610 | API (harness). The same default-zero trap applies to `LaborPolicyBill{MinimumWage=12f}` (UiScreenshotDriver.cs:1895, 3609) |
| SD-08 | NaN passes `Mathf.Clamp` for the pension age (sets a NaN override that `AgeInForce` treats as none), the SWF drawdown, the stance target and tariffs | PS:524; SM:2313-2317, 2699, 5821, 5842 | API (INFERRED) |

### 5b. Money, approval and logic on player paths (candidate S2/S3)

| # | Lead | Where | Path |
|---|---|---|---|
| SD-09 | **The reshuffle's popularity discount never applies.** The minister is removed before `PopularityFactor(c, portfolio)` is read, and that reads 1 for a vacant seat. Every dismissal costs the flat 2.0, against §235's "an unpopular one less" | GC.PoliticsV35:400-402; CS:461-463 | UI |
| SD-10 | Cabinet decisions and meetings are resolved without checking that the item is pending, that the option belongs to it, or the role. A repeat call re-applies the option | SM:1102-1118, 1149-1160 | API |
| SD-11 | A vacant portfolio reads the best Efficiency, Popularity and Knowledge (factor 1). Every candidate is ≤ 90 / ≤ 80, so any appointee lowers Health effectiveness, and the card reads "Underfunded" at full funding. §336 accepted this on film; the wording misleads | CS:459, 463, 468; PortfolioEffectiveness.cs:96; GC.PoliticsV35:391-392; GC.Effectiveness:26-28 | UI |
| SD-12 | **Carbon: "a passed bill's figure wins" does not hold for the player.** A passed budget sets `line.Rate` at passage. The player's turn decision carries no TaxRateOverrides. At the next boundary `AdvanceYear` overwrites Germany's rate with the BEHG schedule while a figure exists, and indexes Sweden's again. A German cut to exactly 0 survives the step (Rate ≤ 0 is skipped); a cut to 1 € is reset to 45 or 55 | PS:481-488; GC:6277-6301; CarbonRateStatute.cs:18-19, 105-129; SM:4935 | UI |
| SD-13 | The carbon clock is 2023 + turn + 1, not the calendar. In a German game (epoch 2024-11-06) the BEHG figure lands about a year late, against POLSYS §3 "every statute is true for the chosen date". The pension paths start in 2026, so the IT, FR and DE starts read the 2026 age. `PensionAgeStatute.SeedYear + CurrentTurn + 1` is also used as a calendar anchor for the AI energy ministry | CarbonRateStatute.cs:55, 95; PensionAgeStatute.cs:99-109, 126-127; SM:4883 | UI |
| SD-14 | Law `ui_duration_cut_act` reads **"Extend unemployment insurance"** but cuts it: natural rate −0.4, benefit rate −0.03 | LC:1951-1958 | UI (S3/S4) |
| SD-15 | Asymmetry: `benefit_generosity_act` ("Raise unemployment benefit") moves only the natural rate (+0.5), not the benefit rate, while the three cut laws lower both | LC:2040 vs 1913, 1928, 1956 | UI |
| SD-16 | Implementing a tax through the tax-programme bill pays no tax-hike approval penalty; a budget rate rise does (documented as intended in a comment) | PS:551-565 vs 526-527 | UI |
| SD-17 | **The player's party auto-buys internal polls** (120,000 kr + 1 h) at its cast's cadence before the queue runs. The lost hour can make a queued act fail, and the `break` then drops the rest of the day. Contradicts §207 and PlayerCampaignRecord.cs:53 | CR:680-702, 801 | UI |
| SD-18 | One unaffordable or over-hours act ends the **whole** remaining queue for the day. The HQ's warning implies the whole queue fails; in fact the earlier items run | CR:801; GC.Campaign:900-917 | UI |
| SD-19 | An enacted law whose id has left the catalogue can never be removed (ApplyLawBillEffects returns before RemoveAll). There is no recompute on load, so a re-authored delta leaves stale dials until the next law event | SM:1959-1963 | save/patch |
| SD-20 | Approval, the Desk's hero stat, reaches no vote, confidence motion or game over (documented in §637). Approval is the *government's* (EconomyState.cs:44), yet scenarios and R-CL1 treat it as the player's: an opposition player's actions (voting down the AI budget) move "their" approval | SM:1270-1273; COMPLETED.md:34322 | design question (ARCHITECT) |
| SD-21 | The field organiser does nothing: `OfficeScale` is never passed to `OfficeNetwork.Day`. Grassroots parties hire one for 1,800 a day. §68 claims the run passes it | CampaignStaff.cs:234; CR:651; LSET:440 | UI |
| SD-22 | The media advisor's interview bonus is overwritten: the audience is reset at CR:834 after the multiplier at :673 | CR:673, 834; CampaignStaff.cs:109-110 | UI |
| SD-23 | Door-to-door counts the spend twice (contacts = spend/5 and the effectiveness factor on the same spend), so it is convex below the volunteer cap (15,000 kr ≈ 1,896 reached; 30,000 ≈ 5,189). This goes against §35 | CR:826, 847; CampaignActions.cs:176-179 | API (the UI spends the default only) |
| SD-24 | §35 applies per action only. Pressure adds linearly, so many small buys beat one large buy: TV as 660 one-hour buys ≈ 1.58× the effect of 4.8 buys of 500,000 | CampaignActions.cs:314-318 | API |
| SD-25 | Campaign day 0 (and run-up day 0) is never playable by the player. `Begin` and `StepDay(0)` run in the same `AdvanceDay`, and the queue only keys the next day. AI parties act on day 0 | SM:4355-4368, 4402-4410, 4551, 4556 | UI |
| SD-26 | Germany's staging has no created-party branch, so a created German party gets the full war chest (`TryCreatedDayZero` reads Sweden's only) | LSET:123 | UI |
| SD-27 | An event's inflation shock probably lives about one day: Phillips is a daily level map that overwrites `state.Inflation`. A release the next morning could publish it. `EventShockPathDiagnostic` covers GDP only (INFERRED) | EventSystem.cs:471; MacroSystem.cs:510; SM:333, 459 | all |
| SD-28 | Event pool: mean approval −1.2 at 0.40 a year gives an equilibrium about 9.6 points below 50 (INFERRED from reversion 0.05, MacroSystem.cs:2096). No country filter: "Currency Under Attack" can hit the euro members and "Sharp Rate Rise Abroad" the USA | EventSystem.cs:256-266, 389-392 | all |
| SD-29 | A support party that tables an alternative is recorded as a break on the government. A junior partner is refused, but the refusal goes only to `Debug.Log` (see SD-40) | SM:1252-1254, 1197; GC.BudgetV35:896 | UI |

### 5c. UI ⇄ engine disagreements (candidate S3/S4)

| # | Lead | Where |
|---|---|---|
| SD-30 | The HQ's hours are stale: the next day's queue is compared with the hours left from the last stepped day, but the step resets hours to 12 first. "OVER THE DAY'S HOURS — THE SPEND WOULD BE REFUSED" can be false | GC.Campaign:453-474, 900-917; CR:614 |
| SD-31 | The money warning ignores the next morning's fixed costs, which are taken before the queue: offices, payroll, the auto-poll | CR:649-696 |
| SD-32 | The HQ's TODAY, day counter and days-to-polling run one day ahead of `SM.CurrentDate`. On polling day itself the HQ reads "1 D TO POLLING DAY"; "POLLING DAY" / "POLL CLOSED" and the ELECTION DAY chip are unreachable | LCS:33-34; SM:4363-4368; GC.Campaign:373-383 |
| SD-33 | Run-up TV BUY counts 500,000 against the chest in the queue totals but is never charged, and for the player it only moves the AI-pace reserve. CL-1 (§477) said "a chip that quietly did nothing would be the lie" | LCS:243; PCR:255-261 |
| SD-34 | Run-up: a second OFFICE press the same day targets the same region and is refused ("already planned there"). The hours bar reads "0.0 / 12" | SM:4549; PCR:225; GC.Campaign:471-473; LCS:285 |
| SD-35 | Debates on days 20 and 41 are announced even when an extra-election campaign is shorter than 6 weeks, so the debate date can fall after polling day | CR:161; LSET:365-367; SM:4283-4286; GC.Campaign:1106-1112 |
| SD-36 | Clicking the story hold chip opens the HQ, but leaves the map or DECLARED page on top, so the answer chips stay off-screen | GC:6011-6013, 2758, 2797 |
| SD-37 | The queue plate can overflow at about 10 or more queued items: it reserves room for 2 rows (INFERRED from layout, not filmed) | GC.Campaign:722, 313 |
| SD-38 | **Gates that exist only in the UI.** A direct call bypasses them: the budget window (`IntroduceBudgetBill` has no window check); fleet orders (`EnergyFleet.Place`); the chair; minister appoint and reshuffle; "Set override"; `TableGovernmentBudget`; `TryOpenBudgetProcess` (any date); `ResetArrivalBudgetWindow` and `CountBudgetVoteOnAgreements` are public. FL PS-3 says "the simulation refuses by one rule" | SM:1506-1518, 1172, 2417, 1207, 2767; Simulation/EnergyFleet.cs:117-134; GC.DocketV35:253; GC:4250; GC.PoliticsV35:397-405, 456; GC.LawsV35:873 |
| SD-39 | The Docket stamps the budget process "Holds time" unconditionally; it holds only when `HoldOnBudgetWindow` is on | GC.DocketV35:91; GC:1387 |
| SD-40 | A junior partner sees a live "Table an alternative budget" button that the manager refuses; the refusal is logged, not shown. The Desk already excludes a junior partner | GC.BudgetV35:877-897; GC:9604-9615; SM:1197; GC.Desk:1075-1076 |
| SD-41 | Ceremonies can be silently dropped: more than 24 divisions in one tick (mass law bills, Polish acts plus vetoes) evict entries before `QueueNewlyResolvedDivisions` reads them (INFERRED) | Data/DivisionRecord.cs:95, 130-132; GC:2282 |
| SD-42 | The signing impact column says "WITH AGAINST WITHOUT THIS ACT", but it shows the turn-end snapshot of the player's draft preview, attached to every division resolved afterwards, AI budgets included (INFERRED) | SigningScreen.cs:533-537; GC:6115, 2289-2295 |
| SD-43 | Poland gets "THIS COUNTRY'S BUDGET PROCEDURE IS NOT YET MODELLED", though its procedure is modelled (only weighing an alternative is unsourced) | SM:1195, 1264-1265; GC.BudgetV35:881; GC:9611; WorldClock.cs:350-364 |
| SD-44 | The law vote bar draws "MAJORITY total/2+1", where total includes unmeasured parties. The model passes on FOR > AGAINST with abstentions, so a bill can carry below the drawn tick | GC.LawsBoard:293-300; PS:342-353 |
| SD-45 | The law decision block shows the enactment cost for a repeal too; a repeal costs nothing. The SHOW counts collapse for filtered-out statuses. "IN FORCE SINCE" is formatted without InvariantCulture | GC.LawsBoard:237-239; GC:7784-7816; GC.LawsV35:919 |

### 5d. Scenarios

| # | Lead | Where |
|---|---|---|
| SD-46 | The scenario party pick offers **every playable party**, PM or not. For a non-PM party the AI governs and the player's decision is dropped. In Italy no election and no record-seating ever changes the government (USA-only seating), so an Italy Debt Crisis run as a non-PM party has no lever for 30 turns (INFERRED from the gate lines) | GC:2365-2373; GC:6105; SM:2632-2642; WorldClock.cs:112-116, 398 |
| SD-47 | The "approval below 30" and Sustained objectives are judged only at yearly boundaries, so mid-year dips are invisible. Sustained is sticky (hold 40 for ten turns once and it is met for good) | ScenarioEvaluator.cs:9-15, 94-109; ScenarioLibrary.cs:158, 301 |
| SD-48 | `ThresholdAtDate` is judged at EndTurn from LastValue, not at ByTurn (dormant: no shipped scenario uses it). `StartTurn` is never read, so a mid-run start is miscounted (harness only) | ScenarioEvaluator.cs:34, 153-157; ScenarioDefinition.cs:75 |
| SD-49 | *(The scenarios retired, §825.)* The scenarios' measured claims predate PS-1 starts, PS-3 role gating and the AI finance ministry. Their diagnostics build a world with no `PlayerCountryId` (so the AI ministry governs) and are on no bar | ScenarioLibrary.cs:71-89, 178-202, 255-265, 293-297; Assets/Editor/ItalyDebtCrisisSliceDiagnostic.cs:111-121 |
| SD-50 | Inherit the Fund's `still_creditor` (debt ≤ 0) can be met by draining the fund through an SWF drawdown bill, which feeds the treasury (INFERRED) | ScenarioLibrary.cs:133-165; SM:2313-2317 |

### 5e. Cabinet, central bank and foreign policy, beyond 5b

| # | Lead | Where |
|---|---|---|
| SD-51 | A pending decision keeps the old minister's scenario, but the Docket names the new minister and the approval scaling reads the new or vacant seat | GC.DocketV35:67-69; CS:584 |
| SD-52 | A meeting or decision rolled while the player governs survives a change of government, holds the clock and resolves unlocked. For meetings this can happen the same day (Germany's constructive vote later in the same day tick) | SM:298, 309 → 2977 → 3145-3162; GC:9074-9132, 9168-9189 |
| SD-53 | AI-run head countries never get a new head; the term cadence is `IsElectionTurn`, which no election uses. A term can be skipped if power is lost and regained within the turn (INFERRED) | GC:3837-3852 |
| SD-54 | The rate preview for head-run countries prices the undamped target, about 6.7× the real step (documented approximation) | SM:5366-5370; RatePathProjection.cs:40 |
| SD-55 | "Set override" says "CHANGING NOTHING" but pins the partner permanently: later base or bloc changes do not reach it, and a later base cut below the pin is mirrored as retaliation (INFERRED) | GC.LawsV35:783, 838, 873; TradeSystem.cs:143-144 |
| SD-56 | Dominated options. Meetings: "Decline politely" beats "Agree to the exercise"; "Stand firm" beats the conciliatory statement. Cabinet: "Publicly back the verdict" +1 vs 0; "Implement the savings" +90 vs 0; "Recover the funds" +100 vs +40 | FPS:53-62; CS:234-238, 268-272, 286-290, 404-408 |
| SD-57 | `ForeignPolicySystem` says a cadence multiplier "can never shift the ForeignPolicy stream's position"; it can, because it decides whether the second draw happens | FPS:72-75, 85 |

### 5f. Stale documents and comments (record and board; S4)

| # | Claim | Where | Contradicted by |
|---|---|---|---|
| SD-58 | "The live game's campaign calendar is the next election turn's boundary … (CL-5 ruled §579)" | CLAUDE.md:88 | SM:4279-4289; §619 ("THE ELECTION IS A DATE"; "CL-5's reversal is reversed by the calendar itself") |
| SD-59 | "⚠ NOTHING IN THE LIVE GAME DRAWS FROM IT" for CampaignAi, Debate and Scandal | Simulation/SimulationRandom.cs:76-77, 86, 94 | SM:4355-4356, 4626-4627. The lines at :59 (ElectionNoise) and :111 (ElectionNight) are still true for live play |
| SD-60 | "WIRED TO NOTHING (R-N2)" headers | CampaignAi.cs:8-10; CR:11-12; CampaignClock.cs:6-8; CampaignActions.cs:6-7; MediaSystem.cs:9-10; LoyaltyModel.cs:6-7; PreferenceModel.cs:6-7; Polling.cs:6-7 | the live call sites (SM:4355; NationalElection.cs:161, 201) |
| SD-61 | "the player cannot yet touch it" (the campaign is AI-played) | SM:2566-2569; GC.Campaign:22-23, 78-79; LCS:14-18 | the queue since §207 |
| SD-62 | "the portfolio-gated levers are PS-3's next part"; "a junior partner its portfolios' (part 4, not yet built - none here)" | SM:2627-2628, 2645-2648 | SM:2661-2690 (§634) |
| SD-63 | "alignment decides; no seats-based majority" | PS:24-35, 224-254; GC:9391-9404 | PS:334-353 (the count decides) |
| SD-64 | "HARNESS ONLY — R-N2 holds until W-G1" | GC.CampaignAction:13; GC.CampaignPolling:11; GC.CampaignDebate:11; GC.CampaignResults:12; GC.CampaignCoalition:11 | R-N2 retired (FL S-31; §197). The screens are still harness-only in fact |
| SD-65 | Scenario: "the remaining five scenarios"; "losing one still ends the run" | ScenarioDefinition.cs:21-23, 40-42; ScenarioEvaluator.cs:20-23 | ScenarioLibrary.cs:10-16 (two shipped); §619 (losing office never ends a run) |
| SD-66 | Event: "a one-turn shock"; "small chance"; "Phillips recomputes next turn" | EventSystem.cs:8, 56-62 | 365-day GDP spread; daily Phillips |
| SD-67 | MR "## Political Layer": the approval terms, "four spending categories", `ElectionCycle (12)`, a 12 % event rate and a pool of 8 | MR :606-625, 642, 652-653 | MacroSystem.cs:2263-2326; ElectionSystem.cs:29 (`ElectionCycle = 4`); EventSystem.cs:73 (0.40), 50 events |
| SD-68 | MR "## Expanded Event Pool": 24 entries, envelope [−2.5, +1.5] | MR :1595-1598 | 50 events; Global Recession −3.0 (EventSystem.cs:284-287) |
| SD-69 | MR "## Cabinet": 3 portfolios, `Clamp01(CE+bias)`, a rate matching the event chance, 18 decisions, a flat reshuffle, a "Cabinet tab" | MR :3462-3528 | CabinetPortfolio.cs:35-39 (six); SM:7108 (`Max(0, …)`); 36 decisions; GC.DocketV35 |
| SD-70 | MR Federal Reserve and Eurozone sections describe a USA-only Fed, `_previewRandom` and a disabled Advance Turn. The code says the same in FRS:8-14 ("Sweden, Poland … keep the player-controlled slider"; a `GetDefaultChair` that does not exist), TaylorRule.cs:29-30, CurrencySystem.cs:38-43, EurozoneRateSystem.cs:19-20, GC:332, 3905-3918, 3982-3998 | MR :664-712, 2775-2796 | §100 (three heads); GC:341-347 (`_previewRandom` gone) |
| SD-71 | MR "## Per-Partner Tariff Overrides": `BuildPlayerDecision` sends overrides; `DrawTradePartnerRow`. MR :3710-3719: one meeting per 121-day turn; a "Foreign Policy tab" | MR :1249-1264, 3710-3719 | GC:6277-6301; GC.LawsV35:827; FPS:36; PF-11 |
| SD-72 | MR §5d: trade overrides are excluded from the vote; the Crime & Justice tab has an introduce control. The PILOT pass/fail formula | MR :4080-4083, 4118-4125, 3811-3818 | PS:806-818; GC.LawsV35:237-238; PS:342-353 |
| SD-73 | FL row PS-3i-2b reads OPEN | POLISIM_FEATURE_LIST.md:158 | §654 "RULED AND BUILT" (cited by the same file at :172) |
| SD-74 | FL S-27 "TacticalVoting … has NO caller"; FL :177 "the campaign is Sweden's only" | POLISIM_FEATURE_LIST.md:310, 177 | NationalElection.cs:289 (§212); LSET:148 (§697) |
| SD-75 | Law docs: LC:7-10 (two categories); LC:2511-2513 (omits electricity); LawDefinition.cs:31-39 and SM:1936-1956 ("opposite sign" deltas); LawDefinition.cs:173 ("the MVP's four laws"); GC.LawsV35:236 cites a CLAUDE.md law section that does not exist; COMPLETED.md:1055 says "LawCatalog.All is read only from the UI layer" (SupportAgreement.cs:106 reads it) | as listed | SM:2005-2147 (recompute); LC (6 categories) |
| SD-76 | Pension: "nothing in the model reads this figure yet" (PensionAgeStatute.cs:41-43); the SeedYear doc's 2026-10-01 epoch (:88); "the participation response … is still not built" (GC:10521); `ApplyDemographicPensionPressure` (MR :3131-3136) | as listed | SpendingDrivers.cs:90-91; PensionPayment.cs:55; §596 |
| SD-77 | Cabinet comments: CS:33 names a non-existent `SM.TryRollCabinetDecisions`; CabinetMinister.cs:36 names a non-existent `GameController.DrawCabinetOptionEstimate`; SM:1080 says "Cabinet tab"; SM:925-926 says it "blocks Advance Turn … see OnGUI"; CS:51 gives the event approval range as −2 to −5; doubled `<summary>` blocks at CS:417-418, 577-578, SM:1080-1081, 4424, GC.Campaign:84; EventSystem.cs:438-439 | as listed | the code beside them |
| SD-78 | `docs/specs/ENERGY_SPEC.md`:61, 262 claim an approval → parliament link that "already" exists | ENERGY_SPEC.md | PS:53-80; StanceModel (no approval term) |
| SD-79 | Polling.cs header gives momentum half-life 10; the constant is 7. CohortVoterGroups.cs:10-12, 24-29 say one group at uniform loyalty; StatTracePanel.cs:224 and GC.Desk:546 say "the nine non-misery terms" | as listed | Polling.cs:106; GroupLoyaltyModel.cs:15-24; StatTracePanel.cs:204-249 |
| SD-80 | `SM:4890-4893` says the seat-share formula reads approval | SM:4890-4893 | PS:74-80 (seats move only at elections) |

---

## 6. Harness actions

Every action a fuzz, exploit-seeking or extreme player can take in this slice, with the entry point the UI uses. Unless stated, the method is public on `SimulationManager`. "UI range" is what the screen can pass; "engine check" is what the method enforces.

### 6a. Campaign (SE, DE only; needs `PlayerCountryId` set and the player's party in the cast, SM:4456-4462)

| Action | Entry point | Arguments: UI range / engine check | Cost / effect | Refusals |
|---|---|---|---|---|
| Queue a run-up verb | `QueueCampaignDecision(kind, regionIndex, issue, spend, out refusal, role)` SM:4538 | UI: (EstablishOffice, −1, null, 100,000), (CommissionPolling, −1, null, 120,000), (PrepareAdvertising, −1, null, 500,000) (GC.Campaign:797-815). Engine: kind legal in PreCampaign; priced; region < Regions.Length (no lower bound); spend stored but never read | office paid on day 0; poll 120,000 (one a day); TV buy free | "no campaign is running"; "the run-up is over"; not legal; the unpriced four; "no such region" (SM:4541-4552) |
| Queue a run-up hire | same, `RecruitStaff, -1, null, 1800, role 0..4` (GC.Campaign:817-831) | role checked only when applied (PCR:233) | 1,800 a day from the next day | already on the roster; cannot pay |
| Queue a campaign action | same, with kind ∈ the eight (GC.Campaign:853-872) | UI: region = map pick or one-region rule for RALLY, TOWN HALL, DOORS, else −1; spend = `Spec.MoneyCost`; issue null. Engine: legal in phase; region < Regions.Length; **no check** on region < 0, spend, issue or kind executability | see the table below | "the campaign is over"; party not in the campaign; not legal; "no such region" (SM:4554-4559) |
| Clear the next day's queue | `ClearCampaignQueue()` SM:4565 | — | — | — |
| Answer a story | `QueueScandalResponse(response, out refusal)` SM:4516 | UI: Deny, Apologize, Explain, AttackSource, Ignore; SacrificeStaffMember only if staff > 0; Resign disabled. Engine: no enum-range check | multipliers per Scandals.cs:160-174 | no campaign; finished; nothing pending; Resign; sacrifice with an empty roster |
| Acknowledge the opening | private `AcknowledgeCampaignOpening` (GC.Campaign:132-137) | reflection only | releases the hold | — |
| Pick a region (Sweden) | private `PickCampaignRegion(0..28)` (GC.Campaign:189-196) | reflection only; not saved | routes the next local act | — |
| Step | `AdvanceDay()` SM:312 | ignores every UI hold; an unanswered story resolves on the party's instinct | — | — |
| Replay | `RestoreCampaign(record, masterSeed, savedCounts)` SM:4579 | a tampered record (queue, answers, counts) is accepted | — | log-only mismatch |

**The eight actions** (`Elections/CampaignActions.cs`:112-145; legality `Elections/CampaignClock.cs`:145-175; all legal in the Campaign phase only):

| Action | Money (UI spend) | Hours | Reach / attention / persuasion / enthusiasm | Notes |
|---|---|---|---|---|
| Rally | 300,000 | 4 | 0.06 / 0.80 / 0.30 / 1.00, local | Region −1 (API) gets the whole electorate (CR:813-815; MediaSystem.cs:132) |
| Town hall | 25,000 | 3 | 0.01 / 0.95 / 1.00 / 0.55, local | same −1 issue |
| Door-to-door | 15,000 | 5 | 1.00 / 0.90 / 0.55 / 0.90, local | contacts = min(spend/5, volunteer hours/0.25); region −1 knocks in region 0 (CR:825) |
| TV ad | 500,000 | 1 | 0.55 / 0.35 / 0.22 / 0.15 | audience = electorate × 0.80 |
| Digital ad | 150,000 | 1 | 0.30 / 0.45 / 0.30 / 0.20 | × 0.55 (× 1.25 with the digital strategist) |
| Social post | 5,000 | 1 | 0.12 / 0.40 / 0.20 / 0.45 | electorate × published share × 0.30 |
| Interview | 0 | 2 | 0.20 / 0.60 / 0.45 / 0.25 | needs a booking; skipped otherwise (CR:785) |
| Policy announcement | 50,000 | 3 | 0.18 / 0.55 / 0.50 / 0.30 | the salience shift is never applied |

Effectiveness = 1 − e^(−spend/scale) (CampaignResources.cs:47-53), so the UI's default spend buys ≈ 63 % of an action's reach.

**Exploit candidates, campaign (all SUSPECTED)**
- National rally or town hall at region −1 (API).
- Splitting a spend into many small buys (SD-24).
- Door-to-door up to the volunteer cap (SD-23).
- Free interviews when booked: ≈ a TV buy's persuasion for 0 kr.
- SacrificeStaffMember of the inert field organiser: it is removed first (CampaignStaff.cs:191), costs nothing real, and beats Explain.
- Hire on the run-up's last day: pay starts the next day, and run-up staff have no effect.
- Free, unlimited run-up TV BUY.
- Save/load scumming: stories, evidence and debates replay identically under the seed.
- Aiming at the top-salience issue (API only; ×1.35 to ×1.5 against the UI's null "general message").
- Queue order: cheap, short acts first.
- A fresh 2.4 M chest at every election, extra ones included (not convertible, FL S-14).

### 6b. Approval, events and scenarios

| Action | Entry point | Arguments / range | Notes |
|---|---|---|---|
| End a turn | `AdvanceTurn(Dictionary<CountryId, PolicyDecision>)` SM:4813 | the player's decision only where PlayerGoverns (GC:6105); it carries only `InterestRateChange` (GC:6277-6301) | the approval formula runs here |
| Event switch | `EventSystem.Enabled` (static) | harness only; restore after use | — |
| Start a scenario | private `StartScenario(definition)` / `StartScenarioAsParty(definition, abbrev)` (GC:2038-2068, 2141-2146) | definition ∈ `ScenarioLibrary.All` (2); abbrev any playable party (GC:2365-2373) | the film driver uses reflection (UiScreenshotDriver.cs:3818-3828, 3911-3918) |
| Dismiss the verdict | private `DismissScenarioVerdict` (GC:2094) | — | sets game over |

**Exploit candidates, approval (all SUSPECTED, INFERRED magnitudes)**
- **Uncapped sustained approval terms** (ruled, "NO combined ceiling", MacroSystem.cs:2135-2137): the USA's paid leave 0→104 weeks gives +5.2 a turn (0.05/week, LaborCouplings.cs:169) and pins approval at 100. Drug policy at 100 gives +1.0 a turn. UBI or universal healthcare at 100 % generosity gives +3.0 a turn each (MacroSystem.cs:2196, 2202, 2227).
- **The meeting drip**: "Stand firm" +0.5 at no cost, ≈3.65 meetings a year, ≈+2.3 a year (FPS:24-27, 62).
- **Cabinet options**: +0.5 to +1.5 at 0.12 per minister per turn (CS:49).
- **Low-popularity shield**: seat a low-Popularity minister before a negative option, because Popularity scales the approval effect both ways (CS:582-588).

Since approval reaches no vote, farming it matters for the scenario objectives and the cabinet-pressure floor only.

### 6c. Cabinet

| Action | Entry point | Arguments / range | Cost | Refusals / gate |
|---|---|---|---|---|
| Search a shortlist | UI: `CabinetSystem.GenerateCandidates(portfolio)` (public static, CS:523) via GC.PoliticsV35:354-357 | every vacant portfolio with no shortlist | free; 2 Cabinet-stream draws per portfolio | **none** (any role) |
| Appoint | UI direct write `Country.CabinetMinisters[p] = candidate` (GC.PoliticsV35:453-459) | a candidate from the shortlist | free, immediate | UI gate `PlayerMayIntroduce(id, p)` (GC.PoliticsV35:375); the sim validates nothing |
| Reshuffle | UI (GC.PoliticsV35:397-405) | a seated minister | −2.0 approval, always (SD-09); redraws a shortlist | UI gate; no cooldown |
| Answer a decision | `ResolveCabinetDecision(id, portfolio, decision, option)` SM:1102 | the UI passes a pending pair and one of its two options | the option's shocks, budget, approval × popularity | **none** (SD-10) |
| Stage a decision | `StagePendingCabinetDecision(id, portfolio, decision)` SM:1086 | anything | holds the clock | none |
| Read pending | `GetPendingCabinetDecisions(id)` SM:1096 | returns the **live** list (mutating it mutates the game) | — | — |
| Finance stance (partner) | `SetFinanceStanceTarget(id, float)` SM:2695 | clamped ±2.5 | — | refused unless the player-partner holds Finance |

**Exploit candidates, cabinet (all SUSPECTED)**
- Leave portfolios vacant: best factors, no leaks or resignations, no holds.
- Seat a low-Efficiency minister before a spending cut.
- Low Popularity before a negative option.
- Dominated options (SD-56).
- Steer the Cabinet stream (and so later decision and loyalty rolls) by clicking Search or Reshuffle.

### 6d. Parliamentary gating, bills and laws

All the `Introduce*` methods are `(CountryId, …) → bool`. A bill sits 21 days (PS:40) and resolves on the player's day tick only (GC:783). There is no cooldown after a failure and no withdrawal.

| Action | Entry point | Arguments: UI range / engine check | Gate |
|---|---|---|---|
| Budget bill | `IntroduceBudgetBill(id, BudgetBill)` SM:1506 | tax lines clamped to [MinRate, MaxRate] at apply; welfare and brackets 0-100; `PensionAge` [60, 70] or < 0 (BudgetBill.cs:68-74). **No window check** | PM; one per country |
| Alternative budget | `TableShadowBudget(id, BudgetBill, out string)` SM:1189 | — | Sweden only; not a junior partner; one |
| Tax programme | `IntroduceTaxProgramBill(id, TaxType, bool isAdd)` SM:1599 | — | Finance; one per TaxType |
| Welfare programme | `IntroduceWelfareProgramBill(id, WelfareProgramType, bool)` SM:1659 | generosity 0-100 (SM:586-588) | Health; one per type |
| Labour | `IntroduceLaborBill(id, LaborPolicyBill)` SM:1719 | minimum wage 0-100 (only where implemented); leave 0-104 weeks; others 0-100; < 0 means "no change". ⚠ default-zero trap (SD-07) | PM; one |
| Crime & justice | `IntroduceCrimeJusticeBill(id, CrimeJusticePolicyBill)` SM:1809 | 0-100; ⚠ default-zero trap; no UI | Interior; one |
| Law | `IntroduceLawBill(id, LawBill)` SM:1863 | LawId ∈ the 140; IsRepeal = currently enacted; DaysRemaining overwritten to 21. **Not refused:** an unknown id, an inconsistent IsRepeal, a non-player country, game over, approval 0, re-introduction after a fail or veto. **Crashes:** a null bill or null LawId | portfolio by category; competence; one per LawId |
| Sector | `IntroduceSectorBill(id, SectorPolicyBill)` SM:2172 | 0-100 | PM; one |
| SWF drawdown | `IntroduceSwfDrawdownBill(id, SwfDrawdownBill)` SM:2237 | UI 0-25 % of GDP (GC:140-141); unbounded at the manager; apply caps at the fund's assets | PM; a fund must exist |
| Trade | `IntroduceTradeBill(id, TradePolicyBill)` SM:2327 | base [0, 50] (non-EU only); overrides [0, 50]; NaN passes the clamp; an unknown partner is ignored; a partner with no override gets one at apply | Foreign; one |
| AI budget path | `TableGovernmentBudget(Country)` SM:1172; `TryOpenBudgetProcess(id, DateTime)` SM:2417; `ResetArrivalBudgetWindow(id)` SM:1207; `CountBudgetVoteOnAgreements(Country)` SM:2767; `TrackAgreements(Country)` SM:2782 | ungated publics | — |
| Day ticks | `AdvanceCountryDayTick(id)` SM:296; `Advance*BillDay(id)`, e.g. `AdvanceLawBillsDay` SM:1890 | repeatable any number of times a day; any country | — |
| Withdraw support / leave | `WithdrawSupport(id, out string)` SM:2801; `LeaveGovernment(id, out string)` SM:2877 | — | support role / junior-partner role |
| Veto forcing (test hook) | `PresidentialVeto.PlantDraw(double u)` PresidentialVeto.cs:105-110 | 0 vetoes every at-risk statute; 1 signs every one | — |
| Sign a division | private `SignPendingDivision` (GC:2315-2318) | ceremony only | — |

**Exploit candidates, gating and laws (all SUSPECTED)**
- **Polish veto shopping:** no cooldown, and the draw is keyed on the date, so a vetoed statute can be re-tabled the next day for a fresh roll at 1.5 approval per loss (PresidentialVeto.cs:91-99).
- **Guaranteed passage:** `debt_office_long_issuance_act` and `electronic_invoicing_mandate_act` (more revenue) pass uncontested anywhere for 0.5 approval. So do empty budget bills (each also counts a budget vote on agreements, SM:1552) and empty trade bills.
- **Unlimited concurrent law bills:** up to 140, no cap; can overflow the 24-entry division log (SD-41).
- **Enact-repeal cycling:** leaves no residue in dials. A cancelling pair delivers a supporter's law item and neutralises it, because `Track` checks membership only (SupportAgreement.cs:162-167).
- **Enactment at any approval:** the cost clamps at 0.
- **Mutating a bill after introducing it:** `Introduce*` stores the caller's object (SM:1514, 1728, 1818, 1885, 2181, 2252, 2336). A LawId mutated after introduction re-resolves every day (SM:1927).
- **An empty `PlayerPartyAbbrev`** makes `PlayerGoverns` true (SM:2640).
- **Tax programmes carry no tax-hike penalty** (SD-16).
- **A German carbon cut to exactly 0** escapes the statute (SD-12).
- **The UI-only gate bypasses** in SD-38.

### 6e. Central bank

| Action | Entry point | Arguments / range | Notes |
|---|---|---|---|
| Appoint the head | `FederalReserveSystem.GenerateCandidates(CountryId)` (public static, FRS:95), then write `Country.CurrentFedChair` as the UI does (GC.DocketV35:253; GC:4250) | UI: one of 2-3 candidates during the hold, PM only. The sim validates nothing: any `FedChair` (any RateBias, NaN) or null (which drops the country into the dead raw-change branch) | free; the hold releases when `_fedChairCandidates` is nulled |
| Rate push (euro members) | `AdvanceTurn(decisions)` with `InterestRateChange` | UI ±0.75 (GC:4031); the sim clamps the push to ±0.75; the raw-change path clamps only the level | free every turn; ≤ 0.11 pp of real move |
| Monetary-regime laws | `IntroduceLawBill` | the 10 ids; own-currency countries only | Finance |

### 6f. Foreign policy

| Action | Entry point | Arguments / range | Notes |
|---|---|---|---|
| Answer a meeting | `ResolveForeignPolicyMeeting(id, ForeignPolicyMeetingOption)` SM:1149 | the UI passes one of the pending meeting's options | **no refusals** (SD-10): repeat or made-up options re-apply |
| Roll a meeting | `TryRollForeignPolicyMeeting(id)` SM:1134 | public; no role check inside (the gate is the caller's, SM:298) | 0.01 a day × `ForeignPolicyCadenceMultiplier` (SM:1055; any float, negatives act as 0) |
| Read the meeting | `GetPendingForeignPolicyMeeting(id)` SM:1121 | — | — |
| Trade bill | `IntroduceTradeBill` (6d) | — | — |
| Set override | UI direct write `link.PlayerTariffOverride = Clamp(today's rate, …)` (GC.LawsV35:868-874) | Foreign gate (GC.LawsV35:785) | free, immediate, irreversible |
| Estimate | `EstimateTradeBill(id, bill)` SM:5533 | read-only | — |

**Exploit candidates, foreign policy (all SUSPECTED)**
- The meeting approval drip.
- The USA raising its base tariff with no retaliation (the stated T-5 gap, TradeSystem.cs:120-121).
- Spamming no-change trade bills.
- EU members setting intra-EU tariffs.
- A trade bill for an AI country passes the gate and never ticks (API).

---

## 7. Randomness

Every stream is a `CountingRandom` seeded with `masterSeed + stream × 7919` (`Simulation/SimulationRandom.cs`:148-163).
- Unseeded, the master seed is `Environment.TickCount`, drawn on first use and recorded.
- The seed and each stream's draw count are saved (`Persistence/SaveGame.cs`:33-37; SaveGameService.cs:96-97) and restored by fast-forward (SimulationRandom.cs:209-225).
- The shadow baseline and the impact ledger's forks swap the real RNG state out and back in a `finally` (`Simulation/ShadowBaseline.cs`:101-146), so they consume no real draws.
- No `UnityEngine.Random` and no unseeded `System.Random` exist in this slice's runtime code. The film driver alone builds seeded `System.Random`s (`Testing/UiScreenshotDriver.cs`:5627, 5732).

| Stream (id) | Draw sites | Seeded? | Cadence and notes |
|---|---|---|---|
| Cabinet (0) | CS:561 `NextDouble` per seated minister per boundary (decision roll; only when `holdsAny`, SM:5154-5157), CS:571 `Next(pool)` on a hit; CS:497-498 loyalty roll per minister under pressure (not role-gated); CS:535 two `Next(i+1)` per shortlist (Search, Reshuffle, harnesses) | yes | **UI actions (Search, Reshuffle) consume a simulation stream**: two runs differing only in a Search click diverge in later decision and loyalty rolls. Shortlists are not saved (GC:509), so a re-Search after a load draws again |
| Event (1) | EventSystem.cs:447 `NextDouble` per country per turn; :452 `Next(50)` when fired | yes | One stream shared by the six countries in world order (WorldFactory.cs:1022): USA, Sweden, Germany, France, Italy, Poland |
| FederalReserve (2) | FRS:103 `Next(2,4)`, :109 `Next(n)` per candidate, :135 shuffle | yes | Only at the head's term, only where the player governs (GC:3850) |
| ForeignPolicy (3) | FPS:79 `NextDouble` per eligible day with no meeting pending; FPS:85 `Next(4)` when one fires | yes | Consumption depends on the player's role (gate SM:298) and the cadence multiplier |
| Parliament (4) | none (PS:51 declared, unused; the W-G1 jitter retired, PS:42-45) | — | The chamber is deterministic (StanceModel.cs:116-117) |
| PublicationRevision (6) | `Simulation/PublicationSystem.cs`:30 | yes | Indirect: the economic vote reads the published figures |
| ElectionNoise (7) | none in live play (Editor harnesses only) | — | The live count uses no noise |
| CampaignAi (8) | public tracker every 7 days, 600 draws (CR:576; Polling.cs:139-141); each internal poll 1,200 draws (1,800 with a pollster), **including the player's automatic polls** (CR:686); issue measurement 2 × sample per contested issue (CR:687-688; CampaignAi.cs:1017-1030); AI softmax, one draw when T > 0 (CampaignAi.cs:679; CR:796) | yes | Started at SM:4355 and 4626. The player's queue changes the draw counts |
| Debate (9) | 24 draws per debate (Debates.cs:212-213, 239-244; CR:944-947) | yes | — |
| Scandal (10) | one draw per party per day for the rate (CR:917); `Draw` 3 (Scandals.cs:254-259); `EvidenceAsSeen` 1 (:183); `Resolve` one per aftermath day for Deny or AttackSource (:214) | yes | **The player's answer changes every later story draw** |
| ElectionNight (11) | film only (Testing/ElectionNightFilm.cs:59) | — | — |
| PreCampaign (12) | 1,200 draws per run-up poll (PCR:250; SM:4402) | yes | Kept apart from the campaign's three, so the staging can be asserted identical (§477) |

- **Keyed draw, not a stream: Poland's presidential veto.** `PresidentialVeto.Draw(MasterSeed, country, date, title)` is an FNV-1a/Murmur hash (PresidentialVeto.cs:91-99), called at SM:1457 (and for statute acts at SM:1310). It moves no stream. The outcome depends on the seed, date and title only. It is plantable (`PlantDraw`, :105-110).
- **Deterministic in this slice:** the approval formula and ledger, EconomicVote, PerceivedPerformance, LoyaltyModel, PreferenceModel, StanceModel, WouldBillPass, the frame decision (a tie keeps the government's frames, SM:1226-1227, 1248), ScenarioEvaluator, CarbonRateStatute, PensionAgeStatute, the Taylor rule and damping, trade retaliation.
- **Order hazards (INFERRED):**
  - Same-day law resolutions follow `Dictionary` enumeration order (SM:1899).
  - Cabinet decisions (SM:5157) then loyalty (SM:5160) follow dictionary order.
  - Both are deterministic for a given insertion and removal history; a save/load that rebuilds the dictionaries in another order would reorder draws.
- **Stale stream documentation:** SD-59.

---

## Appendix — "the three challenge scenarios": what each source says

| Source | What it says |
|---|---|
| `docs/audit/AUDIT_PROTOCOL.md`:35 | "start: every start point and the three challenge scenarios"; :53 "Events and the challenge scenarios." |
| Code, `Data/ScenarioLibrary.cs`:41-45 | `All = { InheritTheFund(), ItalyDebtCrisis() }`: **two**. Its doc (:10-16): "v1 shipped ONE scenario … 'Italy Debt Crisis' is the second scenario to ship … Poland convergence and The Unequal Recovery were MEASURED and DROPPED" |
| `COMPLETED.md` §21 (:1096-1111) | "### Step 3 — Challenge Mode": Inherit the Fund SHIPPED; Italy Debt Crisis SHIPPED; The Disinflation DROPPED; Wage Boom Management DROPPED; Poland convergence and The Unequal Recovery LIVE (then) |
| `COMPLETED.md` §30 (:1913-1949) | "Step 3 — challenge-mode scoping, the slice shipped `cd52461` 2026-08-18"; "the slate of six"; "Two of SIX scenarios remain" |
| `COMPLETED.md` §36 (:2512-2516) | the remaining two measured and dropped: "`ScenarioLibrary` stays at two" |
| `COMPLETED.md` §34, §198, §492, §528, §718 | Italy Debt Crisis playable (§34); seen in Playtest 3 (§198); "Inherit the Fund" opens Sweden's party pick (§492); "the selector's scenario lines" (§528); "with the two scenarios" (§718) |
| `docs/archive/CLAUDE_LESSONS_2026-08.md`:5626, 5662 | "Step 3 (challenge-mode scoping) UNBLOCKS …"; no count of three |
| `POLISIM_FEATURE_LIST.md` | no count; the only scenario row is `T-1` "Per-scenario term accumulation" (:272) |
| `docs/specs/*.md`, `docs/reference/MODEL_REFERENCE.md`, `CLAUDE.md` | no challenge scenario. "scenario" in MR means the harness's `-scenario=` validation runs (e.g. MR :1511). CAMPAIGN SPEC §43 "Example Campaign Scenario" is unrelated |

**Conclusion.**
- Two challenge scenarios exist, both playable from the country selector: Inherit the Fund (Sweden) and Italy Debt Crisis (Italy).
- The protocol's "three" is supported by no other source. It is a question for Elias (which third, or "two").
- For the matrix, the scenario start × role cross is meaningful only for the PM's party (SD-46).
- The batch runner's `-scenario=` flag names validation stress runs, not these.
