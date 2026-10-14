# Gameplay audit, Phase 0: slice C, elections and office

Read-only inventory of `G:\UNITY\Projects\PoliSim` at HEAD `92114481` (s803, 2026-10-13); the only uncommitted file is `ProjectSettings/ProjectAuditorSettings.asset`.
Every code fact gives `file:line` (paths are relative to `Assets/Scripts/` unless they start with `Assets/Editor`, `docs/` or a root file). Every spec fact gives its document and section, its feature-list row, or its `COMPLETED.md` § number.

The tags used below:
- **INFERRED**: I reasoned it from the code and did not run it.
- **DERIVED-OFFLINE**: I reproduced the code's date arithmetic in a PowerShell replica. A Phase 1 run should confirm these.
- **SUSPECTED**: a lead for runs, not a finding.

Nothing was run in Unity.

---

## 0. The architecture in one page (needed to read the tables)

- **Who holds the election.** `SimulationManager.AdvanceDay` raises `PollingDayToday` for the player's country only (`Simulation/SimulationManager.cs:479`). `GameController.Update` then calls the private `CheckElection` on that day (`UI/GameController.cs:796-799`, `6349-6367`). That call runs the count (`RunNationalElection`, `6638-6701`), the verdict (`ResolveElectionVerdict`, `6379-6465`) and the night (`ShowElectionNight`, `6492-6636`).
  - The manager alone never holds an election. `BatchSimulationRunner` (`Assets/Editor/BatchSimulationRunner.cs:71-79`) drives `SimulationTestRunner` with no player country, so it never holds one either.
  - The only non-UI caller of `CheckElection` and `HoldElectionWithoutTheNight` is the film driver, by reflection (`Testing/UiScreenshotDriver.cs:3111, 4141, 4832`).
- **AI countries hold no elections in a run.** Their chambers and governments stay as seated at the start, except that the USA's record is seated by date, and only when the USA is the player's country (`SimulationManager.cs:3995-4017`).
  - Authority: §618 ruling 4 and `CLAUDE.md` "THE WORLD CLOCK". Every country's calendar is row PS-8, NOT BUILT.
  - Poland's presidential rounds also run only in a Polish game (`SimulationManager.cs:4253-4257`).
- **Who governs** is stored in `Country.Government` (`Data/Country.cs:949`) as a `GovernmentRecord`.
  - It is written at the start (`UI/GameController.cs:2188` and `2230`; `Data/WorldFactory.cs:1092`), after an election, by a round, by a constructive vote, and by the US record.
  - `PlayerGoverns` (`SimulationManager.cs:2632-2642`) is the one role gate.
- **Two formation regimes.**
  - **Round countries** (`RoundsApply`, `SimulationManager.cs:3234-3236`: Sweden for the Riksdag, Germany for the Bundestag) run a dated procedure after the election.
  - **Every other country that votes** (only Poland) installs the formation's government instantly on election night (`UI/GameController.cs:6429`).
- **Game over** has two routes:
  - `_pendingElectionVerdictEndsGame`, when the player's party wins no seat (`UI/GameController.cs:6412-6418` and `6451-6456`), landing through `ApplyElectionVerdict` (`6714-6726`).
  - A scenario's verdict being dismissed (`UI/GameController.cs:2093-2103`).
  - The flag is `_isGameOver` (`UI/GameController.cs:295`), saved as `UiState.IsGameOver` and `GameOverReason` (`Persistence/SaveGame.cs:157-158`).

---

## 1. Feature table

The **Countries** column says where each feature runs in a played game, as the player's country. It is not a statement about AI countries.

| ID | Feature | Player-facing entry (screen / control → method) | Code (file:symbol) | Spec authority | Status | Countries | Notes |
|---|---|---|---|---|---|---|---|
| C-01 | The world clock: each country opens on its own start | Country selector card → `GameController.OpenWorldAt` (`UI/GameController.cs:2156`) | `Elections/WorldClock.cs` `StartDate` 223-233, `StartLine` 236-246, `IsSnapStart` 98, `GoverningModeOnly` 101, `NoElectionYet` 108 | POLITICAL_SYSTEM_SPEC §3, §4, §10 d1-d2; §618; `CLAUDE.md` THE WORLD CLOCK | BUILT | all 6 | Starts: SE 2026-01-18, DE 2024-11-06, PL 2023-02-19, IT 2022-07-21, US 2024-03-12, FR 2024-07-18 (§618 "Built") |
| C-02 | Chambers of record by date, seated as elected | Desk / Parliament tab (read) | `WorldClock.Chambers` 249-301, `ChamberAt` 304-312, `SeatedVintage` 319-323, `SeatingDeviation` 326-337; `Data/PartySystem.cs` `SeatsAt` 755-786, `InitialSeats` 711-731, `SeatsSourced` 733+, `ChamberSizeAt` 788-797; `Data/WorldFactory.cs:1032` | §618 rulings 2 and 5; E-47; E-46 | BUILT (E-47 deviations) | all 6 | Italy 2018 and France 2022 are unsourced, so the latest sourced table is seated in their place; `Usa2026` is unsourced (US-2) |
| C-03 | Government of record at the start | Masthead role chip | `WorldClock.Governments` 371-426; `GovernmentRecord.AtStart` (`Elections/GovernmentRecord.cs:252-273`); `SeatedGovernment.TryAt` (`Elections/SeatedGovernment.cs:57-75`) | §618, §628, §629; spec §4 | BUILT | all 6 | Written once. Only the USA is re-seated by date afterwards (C-04). Italy's start is a provisional formation stand-in |
| C-04 | The record seated on its dates (House and president) | Parliament tab record row (`UI/GameController.ParliamentRows.cs:453`) | `SimulationManager.SeatTheRecordOnItsDate` 3995-4017, `TakeOffice` 4028-4034; `WorldClock.RecordSeatsChamber` 112, `RecordSeatsExecutive` 116 | R-US1 (a), ruling F8; US-2 §783 | BUILT | USA only | 119th House on 2025-01-03; Trump on 2025-01-20; both stand thereafter |
| C-05 | Polling day on the calendar's date | Desk calendar mark (`UI/GameController.cs:3540`); the day loop | `WorldClock.TryNextPollingDay` 548-580, `SecondSundayOfSeptember` 583-588, `FirstSundayOfTermWindow` 525-530, `PolandFirstSittingAfterDays` 535; `SimulationManager.TryPlayerPollingDay` 4269-4276, `PollingDayToday` 479 | CL-4 / PS-2 §619; §697; §762, §767 (D3), §773 (E2) | BUILT for SE, DE, PL; NOT BUILT for IT, US, FR | SE, DE, PL | Germany holds only 2025-02-23 and nothing after it (551-557) |
| C-06 | The election, held by the controller | Automatic on polling day | `UI/GameController.cs` `CheckElection` 6349-6367, `RunNationalElection` 6638-6701; `ParliamentSystem.SetSeatsFromElection` (`Simulation/ParliamentSystem.cs:96-105`) | §619; ELECTIONS_CAMPAIGN_SPEC §3 "Election Day" | BUILT | SE, DE, PL | Player's country only. The other three record `NotImplemented` with `NationalElection.NotHeldReason` (`Elections/NationalElection.cs:405-421`), unreachable without a polling day |
| C-07 | The vote prediction (no campaign) | — | `NationalElection.TryCompatibility` 128-169, `TryPredictShares` 197-209; `PreferenceModel.Preference` (`Elections/PreferenceModel.cs:66-92`); `EntrantLayer`; `EconomicVote.ApplyRecordShift` (`Elections/EconomicVote.cs:188-207`) | ELECTIONS_CAMPAIGN_SPEC §7-§8; §638 (PS-3k); §752 (A1) | BUILT | SE, DE, PL | Returns FRACTIONS (§674). Italy, USA and France have no electorate or history (`USA_STAGE_PLAN.md` §2.4) |
| C-08 | The campaign's shares counted | Campaign HQ (slice E) | `NationalElection.SharesFromCampaign` 272-296; `LiveCampaignSetup.TryFor` (`Elections/LiveCampaignSetup.cs:144-178`, gated to SE and DE at 148) | C-R4b (D-21); §697 | BUILT for SE, DE; NOT BUILT for PL | SE, DE | Spec §4's table bills Poland's "districts and cast" as owed |
| C-09 | Tactical voting on the count | — | `NationalElection.cs:276-291` → `TacticalVoting.Apply`; blocs `SwedenBlocs2022` 248-261 | ELECTIONS_CAMPAIGN_SPEC §23; D-10 (a) | PARTIAL | SE only | Runs only when a campaign tracker exists. The blocs are 2022's for every future election |
| C-10 | Sweden's count | — | `NationalElection.Run` 378-388 → `Allocate` 423-455 → `SeatAllocation.AllocateWithThreshold` (`Elections/SeatAllocation.cs:103-113`); divisor `ModifiedSainteLagueDivisor` 42 | vallagen 14 kap.; ELECTIONS_CAMPAIGN_SPEC §28 | BUILT, national totalfördelning only | SE | Modified Sainte-Laguë 1.2 at a 4% national threshold over 349. No 12% valkrets route; no per-valkrets seat placement |
| C-11 | Sweden's full two-tier procedure (12% rule, 310 fixed + 39 adjustment, återföring) | — | `Elections/SeatConversion.cs:117-214`; no runtime caller (only a comment at `NationalElection.cs:385`) | ELECTIONS_CAMPAIGN_SPEC §28; W-D2 | BUILT but wired to nothing (Editor harness only) | — | See D-11 |
| C-12 | Germany's count | — | `NationalElection.Run` 372-376; `GermanThreshold` 87; `ExemptFromThreshold` 100 | BWahlG §4(2), §6; §704 (SSW) | BUILT | DE | Sainte-Laguë/Schepers, 630 seats, 5%, SSW exempt. No Grundmandatsklausel (stated at 86) |
| C-13 | Poland's Sejm count | — | `Elections/PolishSejmAllocation.cs` `Allocate` 45-81, thresholds 25-27, `KindOf` 30-33, `DHondt` 84-99; `Elections/Generated/PolishSejmDistricts2023.cs` (41 magnitudes summing to 460, line 70) | Kodeks wyborczy art. 196-197, 201, 232; §730, §762 | BUILT | PL | Uniform swing from 2023 per okręg. Coalitions (KO, TD) at 8%; MN exempt |
| C-14 | Italy's Rosatellum, national stage | — | `Elections/Rosatellum.cs` (Editor-only callers) | R-EL9; spec §4 and §9 stage 7 | BUILT, Editor-only; NOT BUILT live | — | Sub-national stages (R-EL11 / F-4) and the Senato (E-46) not built; PS-7 |
| C-15 | USA: Electoral College, House and Senate counts | — | `Elections/ElectoralCollege.cs` (Editor-only); `Assets/Editor/UsHouseCountCheck.cs`, `UsSenateRaceCheck.cs` | spec §6; USA_STAGE_PLAN US-8, US-15, US-16; R-EL8 | NOT BUILT live (PS-6 parked by H5, §803) | — | The allocator reproduces 2024 in the Editor |
| C-16 | France's two-round legislative election | — | `NotHeldReason` (`NationalElection.cs:409-410`) | R-EL10; spec §8; F-3 | NOT BUILT | — | Governing mode only (C-49) |
| C-17 | Poland's presidential election, the game's own (two rounds, gate, field, run-off, oath) | No verdict screen. Parliament tab president row (`UI/GameController.ParliamentRows.cs:534`); desk calendar (`UI/GameController.cs:3543-3549`) | `Elections/TwoRoundElection.cs` (rule 54-63, `CountFirstVote` 80-113, `CountRunOff` 116-120); `Elections/PresidentialElection.cs` (`HoldRounds` 213-225, `FirstVoteFor` 173-186, `TryNextFirstVote` 190-206, `HoldRunOff` 313-333, `TermsServed` 346-357); `Elections/PresidencyOfRecord.cs:37-49`; `SimulationManager.HoldPresidentialRound` 4253-4266 | §720, §764, §765, §770, §772 (E1), §773 (E2); PRESIDENTIAL_VOTE.md | BUILT | PL (Polish game only) | Candidate shares are PERCENT (`PresidentialElection.cs:278`) |
| C-18 | Presidential start: the player as candidate | Start card LOCKED (`Elections/StartPoints.cs:76-78, 87-90`) | `StartPoints.PresidentialStartReason` 56 | SP-7, SP-8; D-PR | NOT BUILT | PL, FR locked | — |
| C-19 | Election day with turnout and noise | — | `Elections/ElectionDay.cs:52-104`; `RegionalAggregation`; `TurnoutModel` (campaign only) | ELECTIONS_CAMPAIGN_SPEC §27; W-D1 | BUILT but wired to nothing | — | The live count is deterministic from shares and carries no turnout |
| C-20 | Regional breakdown, derived by uniform swing | Night map | `NationalElection.DeriveRegional` 298-350; `SwedishRegions`; `GermanRegions`; `RegionalVoteModel.RegionalSharesByUniformSwing` | F1; §688, §689; ELECTIONS_CAMPAIGN_SPEC §24 | BUILT | SE (29 valkretsar), DE (16 Länder) | Poland's okręgi exist only inside the count |
| C-21 | Election night | A Canvas takeover; CONTINUE → `DismissElectionNight` (`UI/GameController.cs:6706`); the verdict lands under the cover (1879-1907) | `UI/GameController.cs:6492-6636`; `Elections/ElectionNightFromModel.cs` (`Available` 38-39, `At` 46-93); `Elections/ElectionNight.cs` (`At` 164-204; seats 198-200); `UI/ElectionNightScreen.cs` (`Build` 140, `BuildGovernment` 681, `BuildReference` 1112, `BuildCalls` 1212, `SpeakerLine` 1291-1297) | W-E6, F1, D-8.3; §607-§609, §656, §685, §707 (D-DE) | BUILT for SE, DE; NOT BUILT for PL (D-PL) | SE, DE | See D-01, D-04, D-08, D-19 |
| C-22 | History as the reference | Night foot (SE, DE); Parliament tab reference row (DE, PL: `UI/GameController.ParliamentRows.cs:369-450`) | `WorldClock.TryReference` 634-687 | spec §7; §619, §705, §767 | BUILT | SE 2026, DE 2025, PL 2023 | Only for a polling day of record. A later election shows no reference |
| C-23 | Polls before an election | Campaign HQ poll pages (`UI/GameController.Campaign.cs:601-632`; `UI/GameController.CampaignPolling.cs:82`) | `Elections/Polling.cs` (`PollingSystem.Conduct` 125-166); `CampaignRun` public tracker and internal polls; `PreCampaignRun` | ELECTIONS_CAMPAIGN_SPEC §20-§22 | BUILT for SE, DE (run-up and campaign window only) | SE, DE | No poll outside the window and none for PL, IT, US or FR. Momentum half-life doc vs code: D-18 |
| C-24 | Post-election analysis ("why you won or lost") | Night ledger (`ElectionNightScreen.BuildLedger` 1020) | `SimulationManager.RecordCampaignLedger` 4434-4454; `VoteAttribution` | ELECTIONS_CAMPAIGN_SPEC §31; P2-4.3 | BUILT | SE, DE (when a campaign ran) | — |
| C-25 | Game over on leaving the chamber | Desk GAME OVER stamp (`UI/GameController.Desk.cs:1127-1161`); hold banner (`UI/GameController.cs:1961`) | `UI/GameController.cs` 6412-6418 (round path), 6451-6456 (instant path), `ApplyElectionVerdict` 6714-6726 | `CLAUDE.md` THE WORLD CLOCK; spec §7; §619 | BUILT | SE, DE, PL | IT, US and FR have no election, so their only route is a scenario (`UI/GameController.cs:2093-2103`) |
| C-26 | Losing office never ends a run | Verdict sentence | `UI/GameController.cs:6458-6464` | spec §2 (OP-1, D-5), §10 d4; §619 | BUILT | SE, DE, PL | — |
| C-27 | Who governs, stored; the player's role | Masthead role chip; Parliament tab | `GovernmentRecord.RoleOf` (`Elections/GovernmentRecord.cs:237-244`); `PlayerRole` 8-19; `SimulationManager.PlayerGoverns` 2632-2642 | spec §5.2; PS-3a §628, §629, §642 | BUILT | all 6 | For a presidency, the PM party is the president's party |
| C-28 | The AI governs the player's own country | Masthead "THE GOVERNMENT IS THE AI'S" | `PlayerGoverns` gates (`SimulationManager.cs:4860, 4883, 6333`); `TableGovernmentBudget` 1172 | spec §5.2; §628, §632 | BUILT | all 6 | — |
| C-29 | Role-gated levers | Disabled controls with a reason | `SimulationManager.PlayerMayIntroduce` 2650-2659 and 2670-2690; `PortfolioOfLaw` 2753-2763 | §630, §634, §755 | BUILT | all 6 | — |
| C-30 | Portfolios by Gamson's law, weighted by salience | Formation sheet posts; Cabinet tab | `GovernmentRecord.AllocatePortfolios` 149-201, `GamsonPosts` 229-235; `PortfolioSalience` | spec §5.3; §634, §706, §711, §753 | BUILT | all 6 | `NearTieShare` 0.10 is [AUTHORED-DRAFT] (147) |
| C-31 | Junior partner holding Finance: the stance dial | Budget page dial → `SetFinanceStanceTarget` (`UI/GameController.BudgetV35.cs:333`) | `SimulationManager.cs:2695-2701`; `FinancePartner` | §716, §755 (A2) | BUILT | all 6 (where reachable) | — |
| C-32 | The coalition formation model | — | `Elections/CoalitionFormation.cs` (`Form` 290-356, `Evaluate` 519+, kinds 597-600, `Majority` 178-183); `Elections/GovernmentFormation.cs` (`Form` 76-112, `ViewOf` 190-231, `TryFormSeats` 411-429, `ChamberRules` 473-504); `DeclaredRedLines` (`IsSourced` 37); `DerivedRedLines` | ELECTIONS_CAMPAIGN_SPEC §29; K-1f/g/h §607, §639; §653; §683; §780 F1 | BUILT | all 6 | Declared lines are sourced for SE, DE, PL; the other three use derived lines only. The `Collapse` outcome is never produced (D-20) |
| C-33 | Instant formation on election night (non-round countries) | Verdict only. No night for PL | `UI/GameController.cs:6429` (`GovernmentRecord.FromView` 291-306) | R1 (spec §5.3: "every other country forms at once") | BUILT | PL | No offers and no sheet. The new government takes office on polling day; PF-19 is open |
| C-34 | The Speaker's round (Riksdag) | Parliament tab round block (`UI/GameController.ParliamentRows.cs:218-358`); hold chip (`UI/GameController.cs:5922-5925`) | `SimulationManager` `OpenSpeakerRound` 3352-3382, `AskNext` 3409-3448, `AdvanceSpeakerRound` 3477-3515, `Investiture` 3519-3533, `Install` 4036-4055, `BreakOff` 4058-4068, `OpenRoundAfterElection` 4071-4104; `Elections/SpeakerRound.cs` (constants 29-33, `VoteDayIfTabled` 100-104); `WorldClock.FormationDays` 449-478 | spec §5.3 premises 1-9, R1-R8; §640, §646, §653, §714 | BUILT | SE | First vote is not before polling day + 37 days (§714) |
| C-35 | The chancellor's election (Art. 63 GG) | The same round block in its Bundestag words | `SimulationManager` `PersonBallot` 3577-3634, `Nominated` 3914-3956, `Tally` 3828-3895, `PluralityBallot` 3760-3820, `ElectedGovernment` 3690-3715, `GroupAlone` 3720-3735; `SpeakerRound.Bundestag*` 64-67 | §705, §706, §712, §713, §715 | BUILT | DE | Convening is +30 days; the vote is not before polling day + 72 days. Art. 63(4) has the premise "he appoints" (3749-3756) |
| C-36 | The formation sheet (player as formateur) | OPEN THE SHEET (`UI/GameController.ParliamentRows.cs:341`) or auto-open (`UI/GameController.cs:9650-9658`); cabinet toggles (`UI/GameController.FormationSheet.cs:134-144`), posts (264-274), supporters (185-191), demands (212-216), TABLE IT (366-368), PASS (375-377), CLOSE (82) | `SimulationManager` `DraftProposal` 3308-3349, `PreviewFormation` 4125-4132, `SubmitFormation` 4107-4121, `PassFormation` 4138-4149, `ProjectPersonBallot` 3654-3667; `Elections/FormationProposal.cs` (`Formateur.Answer` 107+) | spec §5.3 premises 1-5; §647, §685 | BUILT | SE, DE | — |
| C-37 | An offer to the player's party | ACCEPT THE OFFER / DECLINE (`UI/GameController.ParliamentRows.cs:315, 319`) | `SimulationManager.AnswerOffer` 4153-4177 | premise 5 / R4; §646 | BUILT | SE, DE | — |
| C-38 | The Speaker's line on the night | Night foot; CONTINUE · FORM A GOVERNMENT | `ElectionNightScreen.SpeakerLine` 1291-1297, `ContinueToSheet` 1300; `UI/GameController.cs:6409-6427` | premise 9; §656 | BUILT | SE, DE | — |
| C-39 | Support agreements (owed, delivered, broken) | Parliament tab agreement block (`UI/GameController.ParliamentRows.cs:74-133`) | `Elections/SupportAgreement.cs` (constants 31-42, `PastTolerance` 48); `GovernmentRecord.FormAgreements` 117-121; `SimulationManager` `TrackAgreements` 2782-2794, `CountBudgetVoteOnAgreements` 2767-2779, `AiWithdrawals` 2989-3002 | spec §5.3; §635, §644, §654 | BUILT | wherever a supporter exists | Tracked daily in the player's country (`AdvanceConfidenceDay` 2971-2978) |
| C-40 | The player withdraws support | WITHDRAW SUPPORT (`UI/GameController.ParliamentRows.cs:122`) | `SimulationManager.WithdrawSupport` 2801-2811 | spec §5.2; §635 | BUILT | any country (not gated by country) | A consequence (an AI motion) follows only in SE and DE |
| C-41 | A junior partner leaves the government | LEAVE THE GOVERNMENT (`UI/GameController.ParliamentRows.cs:790`), drawn only where confidence rules are sourced (`ParliamentRows.cs:702`) | `SimulationManager.LeaveGovernment` 2877-2894; `GovernmentRecord.LeaveCabinet` 96-102 | spec §5.2; §636, §705 | BUILT in the UI for SE, DE; the method is ungated | SE, DE | The government's `Outcome` is not recomputed (D-14) |
| C-42 | A motion of no confidence (Riksdag) | MOVE NO CONFIDENCE (`UI/GameController.ParliamentRows.cs:779`) | `SimulationManager.MoveNoConfidence` 2828-2874; `ConfidenceProcedure.Vote` (`Elections/ConfidenceProcedure.cs:210-231`), `CanBeTakenUp` 234-241 | RF 13:4; §636 | BUILT | SE | Taken up at a tenth (35); carried at more than half (175) |
| C-43 | The constructive vote of no confidence (Art. 67) | ELECT A SUCCESSOR (`UI/GameController.ParliamentRows.cs:685`); projection `ProjectConstructiveVote` 3127-3132 | `SimulationManager` 2844-2860, `ConstructiveVoteOf` 3108-3124, `DraftSuccessor` 3071-3102, `InstallSuccessor` 3135-3149; `ConfidenceProcedure.ConstructiveVote` 86-202 | GG Art. 67; §698, §754 (A4) | BUILT | DE | Any seated party may move it (stated premise, ConfidenceProcedure.cs:32) |
| C-44 | AI motions | — | `SimulationManager.TryAiMotion` 3026-3059; `TryAiConstructiveVote` 3169-3193 (Mondays and the day after a formation, 3175-3178) | §641 ruling (2), §644, §655, §698 | BUILT (reachability limited, §655) | SE, DE | Player's country only |
| C-45 | The government's week: EXTRA ELECTION or ASK TO BE DISCHARGED | Chips (`UI/GameController.ParliamentRows.cs:737, 742`); the clock holds through `DeclarationHolds` (`UI/GameController.cs:1391-1397`) | `SimulationManager` `OrderExtraElection` 2897-2904, `CanOrderExtraElection` 2907-2921, `AskToBeDischarged` 2928-2939 | RF 6:7-6:8, 3:11; §636, §641 | BUILT | SE | — |
| C-46 | Discharge and caretaker government | CARETAKER line (`UI/GameController.ParliamentRows.cs:135`) | `SimulationManager.DischargeAndRound` 4180-4198; `GovernmentRecord.Discharge` 106-111 and `Caretaker` 67; Bundestag discharge at convening 3484-3489 | RF 6:9, 6:11; GG Art. 69(2)-(3); §640, §705 | BUILT | SE, DE | — |
| C-47 | Extra (early) elections | EXTRA ELECTION stamp and date (`UI/GameController.ParliamentRows.cs:707-716`) | `SimulationManager.ScheduleExtraElection` 2941-2952, `BreakOff` 4058-4068; `ConfidenceProcedure.ExtraElectionDay` 244-248; saved at `Persistence/SaveGame.cs:92-93` | RF 6:5, 6:7, 3:11; §636, §640 | BUILT for SE; NOT BUILT elsewhere | SE | Germany: dissolution is not modelled (premise at 3749-3756) |
| C-48 | Confidence rules for PL, IT, US, FR | The block is not drawn (`ParliamentRows.cs:702`) | `ConfidenceProcedure.RulesOf` 34 returns `Unsourced` | spec §5.4 (Poland's constructive vote, Italy's two chambers) | NOT BUILT | — | — |
| C-49 | France's governing mode, a what-if | Card "WHAT-IF · YOUR PARTY GOVERNS" | `GovernmentRecord.WhatIfGoverning` 280-288; `UI/GameController.cs:2233-2235` | spec §8; PS-3d §631 | BUILT | FR | A one-party cabinet on the 2024 Assembly; no elections |
| C-50 | The opposition's shadow budget | Budget page alternative (`UI/GameController.BudgetV35.cs:896`; `UI/GameController.cs:9614`) | `SimulationManager.TableShadowBudget` 1189-1206; `WorldClock.WeighsAlternative` 368 | spec §5.2; PS-3e/f §632-§633 | BUILT for SE only | SE | Elsewhere it is refused with "PROCEDURE IS NOT YET MODELLED" |
| C-51 | The player decides the party's vote on bills | none found | Votes come from the stance model; the bill's author is the player's party (`Simulation/ParliamentSystem.cs:900-904`) | spec §5.2 ("vote on every bill") | NOT BUILT, or by design (INFERRED; Elias to say) | — | The locked-lever text reads "YOUR PARTY VOTES ON THEM" (`SimulationManager.cs:2657`) |
| C-52 | Partner exit dynamics | none (`LeaveCabinet` is called only by the player path, `SimulationManager.cs:2890`) | — | spec §5.2 (junior partner "threaten to leave"; the PM party "dismisses the partner"; a partner "walks out") | NOT BUILT | — | Only AI supporters withdraw (C-39) |
| C-53 | Term limits | — | Poland's president: `MaxTerms` 2 (`Elections/TwoRoundElection.cs:56`), `TermsServed` (`PresidentialElection.cs:346-357`). USA R-US6: none | Konstytucja Art. 127(2); spec §6; ruling F8 R-US6 (b) | BUILT for PL presidents; NOT BUILT for the USA (US-28) | PL | A term limit is never the player's in any built country |
| C-54 | Every country's calendar (AI countries vote) | — | `TryNextPollingDay` is for the player's country only; `PollingDayToday` is the player's | PS-8; R-US5 (a); spec §3 and §10 d3 | NOT BUILT | — | — |
| C-55 | Start points and the derived brief | Start panel and cards (`UI/CountrySelectorScreen.cs:394-545`) | `Elections/StartPoints.cs` (`For` 60-96, `ModeLine` 121-128); `Elections/StartBrief.cs` | SP-1 §622, SP-2 §623 | BUILT | 6 playable, 2 locked | The Italy and USA cards: D-15 and the PS-7 row |
| C-56 | Declarations by date | Campaign HQ DECLARED chip (§657) | `DeclaredRedLines` timelines (`Elections/DeclaredRedLines.cs:315, 361, 413, 448-452`); `DeclarationReading`; `WorldClock.VintageOfElection` 595-607 | K-1f §607, §621, §653, §776, §780 | BUILT | SE, DE, PL | IT, US and FR run on derived lines |
| C-57 | The economic vote | — | `EconomicVote.RecordOverTerm` and `ApplyRecordShift`; `SimulationManager.cs:4227-4248, 4343-4351` | §638, §752 | BUILT | SE, DE, PL (and the Polish presidential first round) | — |

---

## 2. Per-country table

| Country | Electoral system (live) | Thresholds (code) | Chamber size(s) as the code holds them | Polling days in a 30-year run from the start (DERIVED-OFFLINE) | Presidential rounds | Election night | Formation | Confidence / no confidence | Early election | Caretaker | Term limits | Game over |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **Sweden** (start 2026-01-18) | BUILT: national modified Sainte-Laguë 1.2 (C-10); the full two-tier procedure is Editor-only (C-11) | 4% national (`SeatConversion.cs:44`, used at `NationalElection.cs:387`); the 12% regional threshold (45) is not live | Riksdag 349 (`Data/PartySystem.cs:327`; `SeatConversion.cs:42`); the 2022 table is seated at the start, and the game's own count from 2026-09-13 | 2026-09-13 (day 238), 2030-09-08, 2034-09-10, 2038-09-12, 2042-09-14, 2046-09-09, 2050-09-11, 2054-09-13 (8), plus extra elections | n/a | BUILT (29 valkretsar) | BUILT: the Speaker's round, sheet and offers; the first vote no earlier than polling day + 37 days (2026-10-20, day 275) | BUILT: motion (tenth / half), AI motions, the week | BUILT (RF 6:7; RF 6:5 after four rejections); a Sunday no later than 3 months out | BUILT | none in law; none in code | BUILT: zero seats at an election, earliest day 238 |
| **Germany** (start 2024-11-06) | BUILT: national Sainte-Laguë/Schepers; no Grundmandat | 5% (`NationalElection.cs:87`); SSW exempt (100) | Bundestag 630 (`PartySystem.cs:328`); the seated 2021 chamber holds 736 (`ChamberSizeAt` 788-797) until the 2025 count | 2025-02-23 only (day 109) — none after (`WorldClock.cs:551-557`) | n/a (assembly) | BUILT (16 Länder; the seats are the chamber's) | BUILT: Art. 63 — convening 2025-03-25 (day 139), vote no earlier than 2025-05-06 (day 181), the fourteen days, then plurality with "he appoints" | constructive vote BUILT; Art. 68 confidence question NOT BUILT | NOT BUILT (dissolution not modelled) | BUILT (Art. 69 from convening) | n/a | BUILT at the 2025 election only |
| **Poland** (start 2023-02-19) | BUILT: 41 okręgi, d'Hondt, uniform swing | 5% party / 8% coalition (`PolishSejmAllocation.cs:25-27`); MN exempt | Sejm 460 (`PartySystem.cs:331`) = sum of `Magnitudes` (`PolishSejmDistricts2023.cs:70`); Senate stated absent (spec §4) | Sejm: 2023-10-15 (day 238), 2027-10-17 (≈ day 1701), 2031-10-19, 2035-10-21, 2039-10-23, 2043-10-25, 2047-10-27, 2051-10-29 (8) | BUILT (Polish game only): first votes 2025-05-18 (day 819; run-off 2025-06-01), 2030-05-19, 2035-05-20, 2040-05-20, 2045-05-21, 2050-05-22; run-off at +14 days | NOT BUILT (D-PL); reference row on the Parliament tab | INSTANT on election night (no round, offers or sheet) | NOT BUILT (`Unsourced`) | NOT BUILT | NOT BUILT | presidents 2 terms (BUILT) | BUILT: zero seats at a Sejm election (caveat D-02) |
| **Italy** (start 2022-07-21) | NOT HELD (`NotImplemented`); Rosatellum Editor-only | (Rosatellum, Editor-only) | Camera 400 (`PartySystem.cs:330`); the 2022 table is seated from the start (2018 unsourced, E-47); Senato not modelled (E-46) | none (the card names 2022-09-25; never held) | n/a | none | a provisional stand-in at the start, never revisited | NOT BUILT | NOT BUILT | not modelled | n/a | no election route; scenario "Italy Debt Crisis" only |
| **USA** (start 2024-03-12) | NOT HELD (US-8, US-16 not built); Electoral College Editor-only | — | House 435 (`PartySystem.cs:332`; `Usa2020` holds 434); 118th at the start, 119th from 2025-01-03 (day 297), standing thereafter; no Senate | none | none: the record seats Trump on 2025-01-20 (day 314) and he stands past 2029-01-20 (`WorldClock.ExecutiveStanding` 191-198) | none | n/a (a presidency) | n/a (`Unsourced`) | n/a | n/a | R-US6 NOT BUILT | none |
| **France** (start 2024-07-18) | NOT MODELLED (R-EL10) | — | Assemblée 577 (`PartySystem.cs:329`), 2024 table | none | presidential start LOCKED | none | the what-if one-party cabinet (C-49) | NOT BUILT | NOT BUILT | n/a | n/a | none |

**Start point × role at day 0.** Each start has one playable card (`StartPoints.For` 60-96). The roles at day 0, read from the record tables:

| Country | Card | Roles at day 0 | Party creation |
|---|---|---|---|
| SE | RIKSDAG ELECTION | M = PM; KD, L = junior; SD = support; S, V, C, MP = opposition. Verified in §628's record ("M begins as the prime minister's party…"). A created party starts at 0 seats in opposition | Sweden only (`UI/PartyCreationFlow.cs:25`) |
| DE | BUNDESTAG (SNAP) | SPD = PM; Grüne, FDP = junior (§628 records "SPD+Grüne+FDP still standing on 6 November 2024"); CDU, CSU, AfD, Linke, SSW, BSW = opposition. BSW holds 0 seats in the 2021 table | — |
| PL | SEJM | PiS = PM (`WorldClock.cs:392`, cabinet DERIVED); everyone else opposition | — |
| IT | GENERAL (SNAP) | Whatever the formation's stand-in on the 2022 seats gives (provisional). Run to read | — |
| US | PRESIDENTIAL | DEM governs (Biden's presidency); REP is opposition while holding the House (`USA_STAGE_PLAN.md` §2.5 item 3) | — |
| FR | LEGISLATIVE (SNAP), governing mode | The picked party is PM, alone | — |

LOCKED: Poland's presidential card and France's presidential card.

---

## 3. Specified but missing (one ledger row each)

1. **Poland's election night and the presidential night folded into it.**
   - Authority: D-PL; Elias's ruling E3 (§774); PS-5 row ("its election night waits on D-PL").
   - In code: `ElectionNightFromModel.Available` is SE/DE only (38-39). A Polish verdict is never displayed (D-04).
2. **Poland's campaign** (districts and cast).
   - Authority: spec §4 table; spec §5.2 ("every role keeps … campaign HQ, polling").
   - In code: `LiveCampaignSetup.TryFor` is SE/DE only (148). No polls, run-up or HQ for Poland.
3. **Confidence and collapse for Poland, Italy, the USA and France** (spec §5.4).
   - `ConfidenceProcedure.RulesOf` is `Unsourced` (34) for all four, which covers Poland's constructive vote (Art. 158) and Italy's two-chamber confidence.
4. **A Polish formation procedure** (Art. 154-155) and a new election when it fails (spec §5.4).
   - The instant path keeps the old government when none forms (`UI/GameController.cs:6429-6435`), and nothing follows until the next ordinary election (D-03).
5. **Germany's confidence question (Art. 68), the dissolution, and any election after 2025.**
   - Authority: spec §5.4. §618 ruling 1: "the confidence vote and dissolution on their real dates" (on the record, not in code — a grep for 2024-12-16 or 2024-12-27 finds nothing). PS-8.
   - `TryNextPollingDay` gives no polling day after 2025-02-23 (551-557).
6. **Italy, PS-7.** Rosatellum sub-national stages (R-EL11, F-4), the Senato (E-46), confidence in both chambers, the snap election held. Also "US-1's kind of fix in Italy's own words", which the PS-7 row lists as owed.
7. **USA, PS-6 (parked, H5 §803).**
   - US-8: Electoral College live.
   - US-15: Senate. US-16: House races. US-27: midterms. US-28: the two-term limit and the next nominee (R-US6 (b)). US-33: your own nominee.
   - SP-6b: third party. US-22: shutdown.
8. **France.** The legislative model (R-EL10, F-3) and the presidential start SP-8. The Poland presidential start SP-7 ("the presidential START … not built").
9. **PS-8: every country's calendar** (AI countries hold elections).
10. **The junior partner's "threaten to leave" and "the PM's party dismisses the partner"; an AI partner walking out** (spec §5.2 role table). No code found (C-52).
11. **The player choosing the party's vote** ("vote on every bill", spec §5.2). Votes are the model's (C-51). INFERRED; it may be intended. A question for Elias.
12. **A shadow budget outside Sweden** (spec §5.2, opposition's "shadow budget"). `WeighsAlternative` is Sweden only (`WorldClock.cs:368`).
13. **The other election types** of ELECTIONS_CAMPAIGN_SPEC §2 (regional, local, leadership, referendum). Register F-2 trigger row.
14. **Election-day turnout and noise on the live path** (ELECTIONS_CAMPAIGN_SPEC §27). Also TURNOUT and the group breakdowns on the results screen (§30). `ElectionDay` is wired to nothing; the night has no turnout.
15. **FPTP and mixed systems live** (ELECTIONS_CAMPAIGN_SPEC §28). The Electoral College and Rosatellum are Editor-only.
16. **Leader compatibility and personal relationships in the formation** (ELECTIONS_CAMPAIGN_SPEC §29). The "Government collapse" outcome enum is never produced (`CoalitionFormation.cs:13`; only checked in `GovernmentFormation.cs:95, 154, 245`).
17. **Sweden's 12% valkrets route and per-valkrets seat placement in the live count.** `SeatConversion.Sweden` is unwired (C-11).
18. **Spec §7:** "a run ends … when the player ends it, or — in the USA — when the player declines to run again". The US clause is NOT BUILT. I found no "end the run" verb; I did not search the main menu, which is slice A.
19. **Open rows:**
    - PF-19 (the election-night install leaves the budget window open; this is the Polish path, C-33).
    - PS-3-F1 (the round's start against the Riksdag's convening).
    - K-1b (the Riksdag's real PM vote replacing the provisional record).
    - K-1j (the coalition screen shows no party rule).
    - K-1i (1), (4) (SD's reading).
    - PS-3's done-when ("each of the four roles played for a full term in Sweden").
    - D-PS's three lacking frames (§670).
20. **The German start's government** (spec §4: "Scholz's minority after the coalition broke"). The world opens on the SPD+Grüne+FDP row, and no by-date change is applied (D-06). §628 records this as accepted, so it is a spec-text versus record question.

---

## 4. Present but unspecified (CODE-ONLY or stated premises with no spec row; each needs a ruling or a row)

1. **The constructive vote's AI timing.** It is weighed only on Mondays and on the day after a formation (`SimulationManager.cs:3175-3178`). This is a performance premise stated in §698's review; the spec does not name it.
2. **"Any party may move" the constructive vote.** The Bundestag's own quorum is not on disk (`ConfidenceProcedure.cs:32`, a stated premise).
3. **The Art. 63(4) premise "the Bundespräsident appoints, never dissolves"** (`SimulationManager.cs:3749-3756`; §713). It is ruled, but it is the reason Germany has no early election.
4. **Sunday premises.** The extra election falls on the Sunday on or before three months out (`ConfidenceProcedure.cs:244-248`; stated in §636). The Polish Sejm's polling day is "the window's first Sunday" (ruling D3, §767). The presidential first vote is "the last Sunday" (ruling E2).
5. **Poland's first sitting is always polling day + 30 days** (`WorldClock.cs:535, 567`; ruling E2). The anniversary therefore drifts 2-3 days later each term: 2027-11-16, 2031-11-18, … (DERIVED-OFFLINE). Every date stays legal.
6. **Shared static state.**
   - `NationalElection.LastRegionalShares`, `Names`, `Weights`, `WorstAbsError` and `Keys` (69-84) are process-wide statics. The night reads them (`ElectionNightFromModel.cs:38-54`), and every `TryPredictShares` caller overwrites them, including `UI/PartyCreationFlow.cs:199-208` and `SimulationManager.cs:4259`.
   - No spec covers this. It touches `CLAUDE.md`'s "the game holds several worlds at once" rule (D-17).
7. **2022's blocs applied forever.** The tactical layer (C-09) and the Parliament bar's bloc order (`HemicycleRenderer.BlocsKnown`) use 2022's blocs for every later Swedish election (`NationalElection.cs:248-261`). D-10 (a) sourced them for 2022 only.
8. **Unused in play.** `ElectionNight.Schedule`'s seeded jitter is unused; the live night uses `ArrivalsBySize` (`ElectionNightFromModel.cs:99-116`). `TwoRoundElection`'s withdrawal postponement (Art. 127(5)) is never passed an `outOfRunOff` in the game (`PresidentialElection.cs:290`).
9. **A seatless player country reads as governing.** `PlayerGoverns` returns true for the player's country when no party is seated, "the instrument's hand" (`SimulationManager.cs:2638-2640`). This is the harness's case and matters for Phase 1.

---

## 5. Invariant map (where a harness can read each one)

### 5.1 Seat totals equal the chamber size

- **After every count.** `ElectionRecord.Seats` (`Elections/NationalElection.cs:33`) on `country.ElectionHistory[^1]` (`Data/Country.cs:911`), produced by `NationalElection.Run` (366-403). Its sum must equal `PartySystems.ChamberSeats(id)` (`Data/PartySystem.cs:323-335`): SE 349, DE 630, PL 460.
  - Poland per district: `PolishSejmAllocation.Allocate(shares, out perDistrict)` (45-81). Each `perDistrict[d]` must sum to `PolishSejmDistricts2023.Magnitudes[d]`. The magnitudes total 460.
  - Two ways the count can come up short: `SeatAllocation.HighestAverages` stops early when no eligible party has votes (`SeatAllocation.cs:72-75`), and Poland's per-district loop skips a district with no admitted list (`PolishSejmAllocation.cs:88`).
- **After every seating.** `Country.ParliamentSeats` (`Data/Country.cs:893`) has exactly four writers: `WorldFactory.cs:1032` (start), `ParliamentSystem.cs:78` (`UpdateSeats` when empty), `:88` (`SeatChamberOfRecord`, US record) and `:104` (`SetSeatsFromElection`, from `UI/GameController.cs:6692`).
  - Before the game's first count, compare against `PartySystems.ChamberSizeAt(id, WorldClock.SeatedVintage(id, date))` (788-797): Germany 2021 → 736, USA 2020 → 434, otherwise `ChamberSeats`.
  - After the count, compare against `ChamberSeats`.
  - `SeatingDeviation` (`WorldClock.cs:326-337`) names the E-47 stand-ins.
- **Display.**
  - The Parliament tab sums `ParliamentSeats` and appends "THE CHAMBER DECLARES n SEATS" when the sum is not `ChamberSeats` (`UI/GameController.PoliticsV35.cs:119-121, 152`; see D-09).
  - `HemicycleRenderer.LastChamberSeats` and `LastDeclaredSeats` (`UI/HemicycleRenderer.cs:56, 158-159, 493-494`).
  - The night's `NightState.SeatsOnCounted` at `FinalMinute`.
- **Majority line.** `CoalitionMath.Majority` = total/2 + 1 (`CoalitionFormation.cs:178-183`). `SimulationManager.MajorityOf` (3555-3560). `ConfidenceProcedure` `Needed` = Members/2 + 1 (200, 229).

### 5.2 Vote shares sum to 100

The units differ, and a harness must use the right one for each value:

| Value | Unit | Where it is read | Sum rule |
|---|---|---|---|
| `ElectionRecord.Shares` (`NationalElection.cs:35`) | FRACTIONS | — | Σ = 1. "A game's shares sum to one" (`NationalElection.cs:446-448`). `PreferenceModel.Preference` normalises (`PreferenceModel.cs:85-91`); `ApplyRecordShift` preserves the sum (`EconomicVote.cs:192-207`) |
| `NationalElection.LastRegionalShares[r][p]` | fractions | `LastRegionalWeights[r]` | Each region Σ = 1. The vote-weighted regional total sits within `LastRegionalWorstAbsError` (69-84) of the national shares |
| `NightState.CountedShare(p)` (`Elections/ElectionNight.cs:109`) | fractions | the night at its final minute | Rounding residue allowed: at most 1 vote per party per region (`ElectionNightFromModel.cs:20-23, 68`) |
| `Poll.Share(i)` | fractions | — | Renormalised, Σ = 1 (`Polling.cs:150-165`) |
| `PresidentialElection.Candidate.Share` (`PresidentialElection.cs:278`) | **PERCENT** | — | Σ = 100. The count runs in parts per million (287) |
| `Contest.RunOffShareA`, `RunOffVoting` | PERCENT | — | — |
| Poland's per-okręg local shares | — | — | **Not** renormalised (`PolishSejmAllocation.cs:73`). d'Hondt needs ratios only, so no sum invariant applies there |

### 5.3 Election dates are legal

| Country | Rule in code | Where |
|---|---|---|
| Sweden, ordinary | `ElectionRecord.Date` ∈ {`WorldClock.SecondSundayOfSeptember(y)` : (y − 2026) mod 4 = 0} | `WorldClock.cs:573-588`; RF 3:3, VL 1:3 |
| Sweden, extra | The date equals `SimulationManager.ExtraElectionDate` (2825), which is `ConfidenceProcedure.ExtraElectionDay(decided)` and is ≤ decided + 3 months, a Sunday. `_extraElectionOrderedOn` is saved (`Persistence/SaveGame.cs:92-93`). An order is refused within 3 months of the new Riksdag (`SimulationManager.cs:2913-2917`; see D-13) | `ConfidenceProcedure.cs:244-248` |
| Germany | 2025-02-23 only | `WorldClock.cs:551-557` |
| Poland, Sejm | 2023-10-15, then each term's `FirstSundayOfTermWindow(previous polling day + 30)`: a Sunday within the 30 days before the 4th anniversary of the first sitting | `WorldClock.cs:525-535, 558-571`; Konstytucja Art. 98(2), 109(2) |
| Poland, presidential | `Contest.FirstVote` is a Sunday in [termEnd − 100, termEnd − 75] (`FirstVoteFor` 173-186); `RunOffOn` = FirstVote + 14 (`TwoRoundElection.cs:108`); `TakesOffice` = the predecessor's term end (237) | Art. 127(4), 128(2) |
| Campaign window | `CampaignCalendar.FromWorldStart` never opens before the world's start (`CampaignClock.cs:104-110`) | — |

Existing checks to reuse: `Assets/Editor/PollingDayDiagnostic.cs`, `WorldClockDiagnostic.cs`, `PresidentialElectionLiveDiagnostic.cs`.

### 5.4 A government holds a majority or is a declared minority

- **The label.** `GovernmentRecord.Outcome` (`GovernmentRecord.cs:38`) holds one of:
  - a `CoalitionOutcomeKind` name: `MajorityCoalition`, `ConfidenceAndSupply` or `MinorityGovernment`, set by `FromView` 295 and `FromProposal` 210;
  - `"of record"` (`AtStart` 260), `"what-if"` (France, 283) or `"none"` (294).
- **The rule that sets the kind.** `CoalitionFormation.cs:597-600`: Majority when cabinet ≥ majority; C&S when cabinet + support ≥ majority with support declared; otherwise Minority.
  - A government only wins if supported ≥ majority, or under the negative rule (opposed < majority). The negative rule is Sweden only (`GovernmentFormation.cs:475`). So `MinorityGovernment` can only arise in Sweden. *(History since §819, GA-3-4: an exit now relabels the record by its seats, so a minority left by a supporter's withdrawal or a partner's exit reads `MinorityGovernment` in any chamber.)*
  - `SeatedGovernment.TryAsResult` applies the same rule to an installed record (117-121).
- **Recompute from seats; do not trust the label.** Use `Government.Cabinet`, `Government.Support` and `ParliamentSeats`. `Outcome` is not updated after `LeaveCabinet` or `WithdrawSupport` (`GovernmentRecord.cs:88-102`; D-14). *(History: both relabel it since §819, GA-3-4; the audit's GOVERNMENT invariant still recomputes from the seats, and counts a minority an exit left as the note EXIT-MINORITY.)*
- **Expected exceptions to whitelist:**
  - `Caretaker == true` (67);
  - France's `"what-if"`;
  - Poland after a failed formation (the old record stands);
  - Germany's Art. 63(4) plurality chancellor ("short of a majority … appointed", 3793-3817);
  - `Round` open (`SpeakerRound.Open`, `SpeakerRound.cs:109`).

### 5.5 The elected and seated record matches what the night and the Parliament tab present

| What | Where | Note |
|---|---|---|
| The record | `ElectionHistory[^1]` (Seats, Shares, Date, Method) | — |
| The chamber | `ParliamentSeats` | Set from the record at `UI/GameController.cs:6692` |
| The night's seats | `NightState.SeatsOnCounted` | Overwritten from the chamber for Germany only (6526-6531); for Sweden computed independently (D-01) |
| The night's government | `GovernmentFormation.ViewOf(country, DeclarationReading.OfElection(...))` (6592) | Compare with `country.Government` after the round installs (`Install` 4036-4055); see D-21 |
| The reference | `WorldClock.TryReference` (634-687) | — |
| The Parliament tab | `DrawParliamentV35` (`UI/GameController.PoliticsV35.cs:116-152`); round block (`UI/GameController.ParliamentRows.cs:218`) | — |
| The president | `country.PresidentialElections` (`Data/Country.cs:916`) against `PresidentialElection.PresidentAt` (151-164) and the president row (`UI/GameController.ParliamentRows.cs:534`) | — |
| Game over | `GameController._isGameOver` / `_gameOverReason` | Private; saved as `UiState.IsGameOver` and `GameOverReason` |

---

## 6. Outcome reachability (route; earliest day from the start, INFERRED from dates in code)

| Country | Win (the player's party becomes the PM party) | Loss (loses the PM role) | Hung chamber / coalition | No confidence or early election | Term limit | Game over |
|---|---|---|---|---|---|---|
| SE | M holds it at day 0. Otherwise: the 2026 round (tabled sheet or accepted offer), installed ≥ 2026-10-20 (day 275); or after a carried opposition motion → week → discharge → round (≥ day 8 plus consultation) | The 2026 round (≈ day 275); a carried AI motion (reachability limited, §655 "unreachable as ruled" before 2026); the player as PM answering a declaration | Every election's formation. NewElection → round → four rejections → extra election | The player's motion from day 1 if the party is in opposition with ≥ 35 seats: at the start only S (107) qualifies, and SD (73) only after WITHDRAW SUPPORT. Carried at ≥ 175 (§636 measured SD's motion carried at 222). Extra election: the PM player's answer, or `BreakOff` | none (n/a) | Zero seats on 2026-09-13 (day 238) or any later election |
| DE | SPD holds it at day 0. Otherwise: the Art. 63 round, installed ≥ 2025-05-06 (day 181); or the player's constructive vote (needs 369 of 736 before the count, 316 of 630 after) | The 2025 round (day 181); an AI constructive vote (Mondays) | The 2025 formation → phase 2 (14 days) → plurality ballot | Constructive vote only. An early election is unreachable | n/a | The 2025 election only (day 109); unreachable afterwards |
| PL | PiS holds it at day 0. Otherwise: a Sejm election's instant formation (day 238; ≈ day 1701 for 2027-10-17; then every 4 years) | The same days | Instant formation. If none forms, the old government stands until the next election (D-03) | Unreachable (`Unsourced`) | Presidential: the 2-term bar binds the game's own candidates (e.g., a 2025 winner who also wins 2030 cannot stand in 2035). Never the player's | Zero seats at a Sejm election (day 238+); caveat D-02 |
| US | REP gains on 2025-01-20 by record (day 314) | DEM loses on 2025-01-20 by record (day 314) | n/a | Unreachable | NOT BUILT | Unreachable |
| IT | Fixed by the start stand-in | Unreachable | Unreachable (no election) | Unreachable | n/a | Scenario only (`ScenarioLibrary` "Italy Debt Crisis"; slice I) |
| FR | Day 0 (the what-if) | Unreachable | Unreachable | Unreachable | n/a | Unreachable |

`ScenarioLibrary.All` holds two scenarios, "Inherit the Fund" (Sweden) and "Italy Debt Crisis" (Italy) (`Data/ScenarioLibrary.cs:41-45`). The audit protocol names three challenge scenarios.

---

## 7. Suspected discrepancies (each SUSPECTED, a lead for runs)

### Severity S2

- **D-01: Sweden's night may show seats that differ from the chamber's.**
  - The chamber comes from `NationalElection.Allocate`, run on 1e8-scaled national shares (`NationalElection.cs:423-455`).
  - The Swedish night re-allocates on rounded per-valkrets votes, with the threshold base set to the counted sum (`Elections/ElectionNight.cs:197-200`).
  - Only Germany's night is overwritten with the chamber (`UI/GameController.cs:6526-6531`).
  - The zero-floor residue (`LastRegionalWorstAbsError`) can move a quotient.
  - Run: compare `SeatsOnCounted` with `ParliamentSeats` on every Swedish night.
- **D-02: a seatless Polish player can escape game over, and the verdict misreports the role.**
  - On the instant path the "no government formed" return (`UI/GameController.cs:6431-6435`) comes before the out-of-parliament test (6451-6456).
  - So a player's party with zero seats in a Polish chamber that forms nothing is not game-over.
  - The sentence also says "You stay in office" to any role, including opposition.
- **D-03: a hung Polish Sejm has no procedure.** The old record stands (`UI/GameController.cs:6404-6406, 6429`) with no Art. 154-155 steps, no shortened term and no new election, until the next ordinary polling day four years later.
- **D-05: Poland's uniform swing can count a list where it does not stand.**
  - MN's rows are zero outside okręg 21 (`PolishSejmDistricts2023.cs`, MN column), but `local = max(0, v/size + (national − national2023))` (`PolishSejmAllocation.cs:73`).
  - MN is threshold-exempt, so if MN's national share rises above its 2023 share it enters d'Hondt in all 41 okręgi.
  - Germany has the opposite rule ("voted for only where it is on the ballot", §699).
  - Edge: a district with no admitted list allocates nothing, so the total falls below 460 (`PolishSejmAllocation.cs:88`).
- **D-07: Germany can end in a permanent caretaker with no way out.**
  - `PluralityBallot` can conclude with "the outgoing government serves on" when no candidate stands or no government can be drawn (`SimulationManager.cs:3769-3773, 3803-3807`).
  - A caretaker bars the constructive vote (2841, 3172), and Germany has no polling day after 2025.
  - Result: a permanent caretaker and no exit. Reachability is unknown.
- **D-14: the coalition label goes stale and nothing follows a partner's exit.**
  - `WithdrawSupport` and `LeaveCabinet` change the cabinet or support without recomputing `Outcome` (`GovernmentRecord.cs:88-102`).
  - A record can read "MajorityCoalition" below a majority.
  - *History: the label half is fixed in §819 (GA-3-4) - the record is relabelled by its seats at each exit. That nothing follows the exit stands (GA-4-1, GA-0-16).*
  - Outside the AI-motion test nothing follows a partner's exit; spec §5.2 says "the government may fall".

### Severity S3

- **D-04: Poland's election is invisible to the player.**
  - There is no night (`Available`), so `CheckElection` applies and clears the verdict at once (`UI/GameController.cs:6361-6366`).
  - The sentence is never shown (it is logged as "ROLE: …") and the clock does not hold.
  - The player sees the result only on the Parliament tab.
- **D-06: the German start does not match spec §4.**
  - The start date 2024-11-06 (`WorldClock.cs:227`) falls on the SPD+Grüne+FDP row (385). The minority row starts 2024-11-07 (386).
  - Nothing re-seats it (`SimulationManager.cs:3995-4017` is USA only), so the FDP sits in cabinet until the 2025 formation.
  - Spec §4 says "Scholz's minority after the coalition broke". §628 records the code's state as verified.
  - Relatedly, the 2024-12-16 confidence vote and the 2024-12-27 dissolution are not events in a German game.
- **D-08: the German night can promise a new election that the procedure never holds.** The night prints "NO GOVERNMENT CAN FORM · NEW ELECTION" (`UI/ElectionNightScreen.cs:689-697`), but Art. 63's path always ends in an appointment (premise, `SimulationManager.cs:3749-3756`).
- **D-09: Germany's Parliament tab reads 736 seats beside "THE CHAMBER DECLARES 630 SEATS" from day 0 to day 109.** The comparison uses `ChamberSeats` instead of `ChamberSizeAt` (`UI/GameController.PoliticsV35.cs:152`).
- **D-10: no Grundmandatsklausel.** A CSU below 5% nationally gets no seats where the law would seat it (`NationalElection.cs:86`; `GovernmentFormation.cs:489-490`, stated).
- **D-11: no 12% route in Sweden.** A party under 4% nationally but at ≥ 12% in a valkrets is denied seats (`NationalElection.cs:380-388`; `SeatConversion.cs:15-16` is unwired).
- **D-12: a Swedish player can move no confidence on the evening of polling day.**
  - After the count and before the round opens, an opposition player can move it on the new chamber's seats against the outgoing government. `MoveNoConfidence` has no RF 13:4 "between election and first sitting" bar for the Riksdag (`SimulationManager.cs:2828-2874`); the Bundestag branch checks `ElectionAwaitsRound` at 2848.
  - This is stated as not modelled in §636.
- **D-13: Sweden's three-month bar on an extra election can end about 15 days early.** `CanOrderExtraElection` counts the three months from the game's polling day instead of the first sitting (`SimulationManager.cs:2913-2917`). This is a stated premise.
- **D-15: the Italian card promises an election the game never holds.**
  - The card says "SNAP ELECTION" and names 2022-09-25 (`WorldClock.cs:243-245`; `StartPoints.cs:127`), and that election is never held.
  - The chamber shows the September 2022 result from 21 July 2022 (E-47).
  - The PS-7 row already names this as owed.
- **D-16: a REP player at the US start reads opposition while holding the House.** This is a known item (`USA_STAGE_PLAN.md` §2.5 item 3).
- **D-17: the night could draw stale regional shares.**
  - The process-wide `NationalElection.LastRegional*` can be overwritten between the count and the night by any other `TryPredictShares` call (§4 item 6).
  - The risk in play is low. It is a real risk for a Phase 1 harness that runs several worlds or predictions in one process.
- **D-21: the night's government may not be the one installed.**
  - The night's "WHO GOVERNS — FORMED ON THE NEW CHAMBER" is the formation's government (`UI/GameController.cs:6592`).
  - The round may install a different one (§660: the real round asks S first and installs S).
- **D-22: stale blocs.** The 2022 blocs are applied to every future Swedish election (§4 item 7).
- **D-25: the Polish presidency can stay vacant of a decision.**
  - A first-round tie, or a run-off with an unplaced finalist, leaves the contest undecided (`TwoRoundElection.cs:105`; `PresidentialElection.cs:317-321, 327`).
  - No rerun follows, so `PresidentAt` keeps the previous president past the term.

### Severity S4

- **D-18: the momentum half-life is documented as 10 days and coded as 7.0.** `Elections/Polling.cs:24-25` says 10; the constant at 106 is 7.0 (slice E).
- **D-19: latent hard-coded Swedish values on every non-German night.** `chamberSeats = 349` and a 4% threshold (`UI/GameController.cs:6513, 6519-6520`). They will be wrong for Poland once D-PL lands.
- **D-20: stale record rows** (record type):
  - `POLISIM_FEATURE_LIST.md:158-159` mark PS-3i-2b and PS-3i-2c OPEN, while line 157 and §653-§654 record them RULED and BUILT.
  - Line 163 (OP-1, "D-5 (a) stands - lose-only") contradicts spec §2 and §10 d4, and the code.
  - Line 175 (D-23, "polling day stays the turn boundary") is superseded by CL-4 (§619).
  - The pointer rows S-27 (TacticalVoting "has NO caller") and S-32 ("election night has no door") contradict the code (`NationalElection.cs:289`; `UI/GameController.cs:6360`).

---

## 8. Harness actions (what a player can do, and the method the UI calls)

All controller methods below are **private**. Today they are reached only by reflection (`UiScreenshotDriver`). The manager's methods are public.

| Action | UI entry | Method called |
|---|---|---|
| Pick a country and party | Selector → party panel | `GameController.SelectPlayerCountryAndParty` (`UI/GameController.cs:2111`) → `OpenWorldAt` (2156) → `SelectPlayerCountry` (2203). A created party (Sweden) goes through `UI/GameController.PartyCreation.cs:68` |
| Hold the election | Day loop on `PollingDayToday` | `GameController.CheckElection` (6349). The warm-up form without the night is `HoldElectionWithoutTheNight` (6339) |
| Dismiss the night | CONTINUE | `DismissElectionNight` (6706) → `ApplyElectionVerdict` (1907) |
| Formation as formateur | Sheet: invite partners, move posts, add supporters, accept demands | `DraftProposal` (3308) and `PreviewFormation` (4125); TABLE IT → `SubmitFormation` (4107); PASS → `PassFormation` (4138) |
| Answer an offer | ACCEPT / DECLINE | `AnswerOffer(id, true/false)` (4153) |
| Move no confidence (SE) | MOVE NO CONFIDENCE | `MoveNoConfidence` (2828). Projection: `ConfidenceProcedure.Vote` and `CanBeTakenUp` |
| Constructive vote (DE) | ELECT A SUCCESSOR | `MoveNoConfidence` (2828). Projection: `ProjectConstructiveVote` (3127) |
| Answer a declaration (SE, player as PM) | EXTRA ELECTION / ASK TO BE DISCHARGED | `OrderExtraElection` (2897), gated by `CanOrderExtraElection` (2907); `AskToBeDischarged` (2928) |
| Leave the government | LEAVE THE GOVERNMENT | `LeaveGovernment` (2877) |
| Withdraw support | WITHDRAW SUPPORT | `WithdrawSupport` (2801) |
| Table a shadow budget (SE) | Budget page | `TableShadowBudget` (1189) |
| Set the Finance stance (junior partner holding Finance) | Budget page dial | `SetFinanceStanceTarget` (2695) |
| Campaign-side choices | Campaign HQ | Slice E. `QueueCampaignDecision` (4538), `QueueScandalResponse` (4516) |

### Holds a harness must answer, or the clock stops for good

| Hold | Where | Released by |
|---|---|---|
| The night | `_electionNight != null` (`UI/GameController.cs:732, 807`) | CONTINUE |
| `FormationHolds` (1400-1404) | PlayerAsked or OfferToPlayer | table, pass, accept or decline |
| `DeclarationHolds` (1391-1397) | a declaration against the player's government | extra election or discharge |
| Campaign opening and budget window (1384, 1387) | Settings-dependent | — |

An **idle** style needs a default answer for each hold. A **fuzz** style can loop for ever by passing or declining, since a pass is not a rejection under RF 6:5 (`PassFormation` → `AskNext`; Order cycles `Turn % Count`, 3412).

### Persistence for save/load at random ticks

- `Country.Government` (Round, StandingRefusals, Agreements), `ElectionHistory`, `PresidentialElections`.
- `SimulationPendingState.ExtraElectionDate` and `ExtraElectionOrderedOn`.
- `UiState.PendingElectionVerdict`, `PendingElectionVerdictEndsGame`, `PendingElectionDate`, `IsGameOver` (`Persistence/SaveGame.cs:92-93, 157-165`).
- The save format is 38 (`Persistence/SaveGameService.cs:53`).

### Existing Editor checks to reuse as oracles (`Assets/Editor/`)

`PollingDayDiagnostic`, `WorldClockDiagnostic`, `SpeakerRoundDiagnostic`, `ConstructiveVoteDiagnostic`, `ConfidenceDiagnostic`, `OfficeTestDiagnostic`, `PlayerRoleDiagnostic`, `FormationSweepDiagnostic`, `GermanFormationDiagnostic`, `GermanNightDiagnostic`, `PolishSejmAllocationDiagnostic`, `PresidentialElectionLiveDiagnostic`, `AiMotionReachDiagnostic`, `SeatlessFormationDiagnostic`, `SupportAgreementDiagnostic`, `FormationTimeDiagnostic`, `StartPointsDiagnostic`, `StartBriefDiagnostic`.
