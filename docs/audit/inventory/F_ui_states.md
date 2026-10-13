# Phase 0 inventory, slice F: every UI state, its film coverage, and the Design board it was built from

Written 2026-10-13 for the gameplay audit (`docs/audit/AUDIT_PROTOCOL.md`, area 10: *"Every UI state on dry film against its LOOKED AT board: clipping, number format, colour neutrality, stale or wrong-country text"*). This is a read-only inventory. Nothing in the repo was changed and no Unity run was made. Code facts cite `file:line`. Record facts cite the `COMPLETED.md` §, the feature-list line, or the document. **INFERRED** marks a claim I reasoned to but did not see a measurement or a line for.

Paths are relative to `G:\UNITY\Projects\PoliSim`. Film-driver code is `Assets/Scripts/Testing/UiScreenshotDriver.cs` (written **UiSD** below). `GameController.cs` is written **GC**, and partials are written `GC.<Part>.cs`.

---

## 0. How the film works: what "dry" and "real" can claim

- **Real film**: `UiScreenshotCapture.Run`, a windowed Editor that refuses `-batchmode` (`Assets/Editor/UiScreenshotCapture.cs:243-265`).
- **Dry film**: `UiScreenshotCapture.RunDry`, which runs under `-batchmode` and refuses without it (`UiScreenshotCapture.cs:145-153`). It is one Unity process with one session per `Country@WxH` (`:160-187`). The default country when none is named is USA (`:156`). Only the four standard geometries are accepted: 1280x720, 1600x950, 1920x1080, 2560x1440 (`Assets/Scripts/UI/GameSettings.cs:62-64`, read at `UiScreenshotCapture.cs:84`).
- **Both kinds run the same driver and the same choreography.** In a dry film, `Capture` pumps one IMGUI pass and measures it instead of photographing it (UiSD:2533-2582).
  - In a dry film, the Canvas text assert returns early (UiSD:2138-2160). The run prints "canvas text NOT CLAIMED" (UiSD:2273-2275).
  - The identity token, the frame-size traps and `ScreenEdgeCheck` need pixels, so they belong to the real film only (UiSD:2585-2650; `UiScreenshotCapture.cs:486-490`).
  - **So a Canvas state (main menu, selector, start panel, party picker, signing, election night) is REACHED by a dry film but MEASURED only by a real film.** This is CLAUDE.md's canvas-film rule, §578.
- **Modes.** The default is the sweep. Each of the following flags replaces or extends it (`UiScreenshotCapture.cs:441-485`):
  - `-shotstates`: state pins after the sweep (UiSD:1463).
  - `-shotinterrupts`: no warm-up; plays the country's own run-up to polling day (UiSD:564).
  - `-shotformation`: with interrupts, continues into the formation (UiSD:4867).
  - `-shotelectionnight`: a staged Swedish board (UiSD:558).
  - `-shotcampaign`: staged Swedish campaign screens, Sweden only (UiSD:570, :5107-5117).
  - `-shotledger` (UiSD:577) and `-shotladder` (UiSD:551).
  - `-shotsaves` (UiSD:542).
  - `-shotload=`, `-shotcreatestart`, `-shotparty=`, `-shotstop=`.
- **Scope ledger.** `Tools/film_scope.ps1` writes one row per dry film to `Tools/film_scope.tsv` (columns at `film_scope.tsv:17`). `DryFilmScopeCheck` fails the cheap bar on a session the row does not name (§574).
  - The ledger's rows, counted by country (`Tools/film_scope.tsv`, read 2026-10-13):

    | country | dry-film rows | geometries | last row |
    |---|---|---|---|
    | Sweden | 102 | 1280: 102, 2560: 3, 1920: 1 | `drys758`, 2026-10-02 |
    | Germany | 53 | 1280: 53, 2560: 2 | `drys756` |
    | Poland | 13 | all 1280 | `dry798`, 2026-10-06 |
    | USA | 10 | 1280: 10, 2560: 2 | `drys783` |
    | France | 2 | all 1280 | `drys725` |
    | Italy | **0** | none | none |

  - No session at 1600x950 has ever been declared.
- **Sessions per country.** The standing set is 6 countries x 4 geometries. "Twelve sessions" in the record means 6 countries x {1280, 2560} (§573). The measured frames per dry session on the v3.5 tree:
  - Sweden 155
  - Germany 147
  - Poland 145-147
  - France 143
  - USA 142-143
  - Italy: no dry figure on the v3.5 tree. Its last recorded film is the real `film749c`, 141 captured (§749).
  - Sources: §725, §726, §761-§785.

Countries are written SE, DE, FR, IT, PL, US. Roles are written PM (governing), JP (junior partner), SUP (support from outside), OPP (opposition) and CT (caretaker).

**Who the film's player is.** The sweep seats the largest party through the one-argument commit, unless `-shotparty=` is given (UiSD:460-465).
- Sweden: the player is S, in opposition at the start (§628, §694).
- USA: REP, in opposition at 93a (§694).
- Poland: PiS by default, or KO with `-shotparty=KO` (§785).
- Other countries: INFERRED, not measured.

---

## 1. UI state table

Legend for the film column:
- **D+R**: the shot runs in both kinds, and IMGUI is measured in both.
- **R-only claim**: a Canvas or pixel state. A dry film reaches it but measures nothing.
- **flag**: the mode the shot needs.
- **none**: no shot reaches the state.

Legend for the board column: the board ids the record names for the state, and whether the record shows a **LOOKED AT** stamp.
- The record has exactly eight "LOOKED AT" mentions: §685, §686, §701, §707 (x3), §708 and §797.
- The stamps it shows are for: **20b, 21a-21e (+#dps-note), 23a-23c (+note), 24a-24b (+#dde-note), #dstr-note**.
- **28a/28b are a PROPOSAL, not stamped** (§797).
- No other board carries a recorded stamp. That includes 27a, the v3.5 composition (§721 says "its RULED block is not in either file").

### 1a. The front door

| ID | screen/state | reached by (path -> code) | countries/roles | film shot(s) | board (LOOKED AT? §) | notes |
|---|---|---|---|---|---|---|
| F-01 | Main menu, a save exists: CONTINUE in brass | launch -> `MainMenuScreen.Build(hasContinue)` `Assets/Scripts/UI/MainMenuScreen.cs:67, :152-158`; the takeover seam at GC:1274-1293 | all | `00_main_menu` (UiSD:305-312), R-only claim | 17a (no stamp; built "the D11 way", §626) | Which of F-01 or F-02 a film shows depends on the saves directory on the machine (INFERRED). The CONTINUE caption naming the save was refused by 17a and not built (`MainMenuScreen.cs` doc; §626). |
| F-02 | Main menu, no save: NEW GAME in brass, column one item shorter | same | all | `00_main_menu`, if no save is on disk | 17a (no stamp, §626) | see F-01 |
| F-03 | Settings from the menu: SOUND, DISPLAY, GAME; foot BACK TO THE MENU | menu SETTINGS -> `ChooseMainMenu` GC:1293 -> no-country branch GC:2592 -> `DrawSettingsScreen` GC:1444 | all | `00a_settings` (UiSD:313-318), D+R | 17b (no stamp, §626); built §615 | The film pins preferences, so a fresh player's defaults are shown (UiSD:220). PF-17's 2560 edge was fixed in §680. |
| F-04 | Settings from the desk: game behind, foot BACK TO THE DESK | masthead SETTINGS (17c) -> `OpenSettings` GC:1374 -> GC:2652 | all | **none**: the driver never opens settings from the desk (UiSD has no `OpenSettings` call) | 17b / 17c (no stamp, §626) | Non-default settings states are not filmed either: autosave on, a set window mode, holds off. |
| F-05 | Saves screen from the menu (LOAD GAME): no Save row, "Back to the menu" | menu LOAD GAME -> GC:2599 -> `DrawSavesMenuScreen` GC:1122 (`inGame` false, :1140, :1223) | all | **none** | none recorded (legacy IMGUI) | |
| F-06 | Saves screen in game, with rows (Load / Delete) | masthead SAVES -> `OpenSavesMenu` GC:1098 -> GC:2645 | all | `92_saves_menu` (UiSD:2095-2124: two `zz_driver_capture_*` saves written, filmed, deleted), D+R | none recorded | The row text prints `Turn {header.Turn}, {header.Date:yyyy-MM-dd}` (GC:1172). See §5 (S-11). |
| F-07 | Saves screen empty: "No saved games yet…" | GC:1163-1165 | all | **none** | none | |
| F-08 | Saves confirm/status: "Replace unsaved game?", "Really delete?", Deleted / Delete FAILED | GC:1179-1206 | all | **none** | none | |
| F-09 | Country selector (Canvas): folder grid and scenario strip | NEW GAME -> `CountrySelectorScreen.Build` `CountrySelectorScreen.cs:52` | all | `01_country_selector` (UiSD:334-337, and `AssertSelectorClearance` UiSD:2183-2207), R-only claim | 1f (v2, no stamp; board table at COMPLETED.md:14945); the scenario strip §718 | The strip shows **two** scenarios (`Assets/Scripts/Data/ScenarioLibrary.cs:41-45`: Inherit the Fund (SE), Italy Debt Crisis (IT)). The audit protocol speaks of "the three challenge scenarios". |
| F-10 | IMGUI fallback selector (the Canvas build failed) | GC:2614-2620 -> `DrawCountrySelector` GC:2334 | all | **none** (a degradation path) | 1f lineage | Not filmable by design: no film stages a missing sprite. |
| F-11 | Start panel: playable card with mode, seat bar and derived brief; SELECT / BACK | folder -> `ShowStartPanel` `CountrySelectorScreen.cs:388`; data in `Assets/Scripts/Elections/StartPoints.cs:60-98` | all six (each has one playable start) | `01f2_start_points` (UiSD:351-369), R-only claim | 18a (no stamp, §626); §622, §623, §627 | **TL-1** (UiSD:352): a missing frame now fails the film (§645). The US card draws the House (§627). IT's card names a polling day the game does not hold (PS-7 row, `POLISIM_FEATURE_LIST.md:91`). |
| F-12 | Start panel: LOCKED card ("THE PRESIDENTIAL START IS NOT YET BUILT") | `StartPoints.cs:76-89` | PL (2025 presidential), FR (2022 presidential) | `01f2_start_points` on PL/FR films. Real `mm3fr_1280` (§626) and PL films `film770pl2`/`real776` (§770, §776) | 18b (no stamp, §626) | |
| F-13 | Party panel / picker: parties largest first, cabinet marked, PROVISIONAL line; CREATE A PARTY (SE) | start card -> `ShowPartyPanel` `CountrySelectorScreen.cs:242`; `ProvisionalLine` `:219-221`; commit `SelectPlayerCountryAndParty` GC:2111 | all; CREATE only where `PartyCreationFlow.Offered` = SE (`PartyCreationFlow.cs:25`) | `01g_party_picker` (UiSD:374-392), R-only claim | none numbered in the record (CL-2, per the UiSD:371-373 comment; §566, §528 touched it) | PROVISIONAL wording names THE RIKSDAG (`CountrySelectorScreen.cs:220-221`). Its shown-when is the K-1 provisional government, which is Sweden's (INFERRED). |
| F-14 | Scenario party pick (the scenario country's chamber) | scenario strip -> `OpenScenarioPartyPick` GC:2133 | SE (scenario 0); IT (scenario 1) unfilmed | `01h_scenario_party_picker` (UiSD:394-400), R-only claim | none recorded | Only `ScenarioLibrary.All[0]` is filmed (UiSD:393). |
| F-15 | Selector yielding: the scrim mid-cover | the commit -> seam CoverOut | all | `01a_selector_yielding` (UiSD:482), R-only claim | 1e (the hand-off envelope; no stamp) | The guards' results under this name are the Desk's (UiSD:472-481). |
| F-16 | Creation flow, steps 1-5 (profile, placement, declarations, leader, review), an origin slip pinned | party panel CREATE A PARTY -> `RequestPartyCreation` `GC.PartyCreation.cs:32` -> `DrawPartyCreationScreen` `:75` (GC:2607) | SE only | `01k1`-`01k5` (UiSD:410-438), D+R | **no board**: D-CP was sent (§677) and no answer is recorded; SP-4b is not built (`POLISIM_FEATURE_LIST.md:77-78`) | The film writes the draft's fields directly (UiSD:418-428), so the ink-fence refusal state (§676) is **not** staged (INFERRED from the code: no shot names it). |
| F-17 | Created party seated on the Desk | flow START -> `CommitPartyCreation` `GC.PartyCreation.cs:60` | SE | `01k6_created_seated` with `-shotcreatestart` (UiSD:439-450) | none | |

### 1b. The Desk (Screen 0), the rail, the banner and the slips

| ID | screen/state | reached by | countries/roles | film shot(s) | board | notes |
|---|---|---|---|---|---|---|
| F-18 | Desk RUNNING at turn 0: lamp green, masthead, cards, strip | game lands on the Desk -> `DrawDeskStage` `GC.Desk.cs:191` (GC:2800) | all | `01b_running_strip`, `01c_desk` (UiSD:508-515), D+R | 1m / 1m-r2 / 17c masthead / 10d tokens (no stamps); the v3.5 page (27a, no stamp) §726 | The map chips are asserted on `01c_desk` (UiSD:515; §750). |
| F-19 | Desk HELD: banner over the masthead, lamp amber, faces disabled | any hold (GC:2713) | all | `01d_desk_held` (UiSD:604-607), D+R | 1m (no stamp) | **The name promises HELD, but `AssertDeskState` checks only `OnDesk`** (UiSD:4195-4203). Whether a hold is up at 01d is not asserted. See §5 (S-7). |
| F-20 | Desk event card (BREAKING) | `_lastEventsByCountry` -> `DrawDeskEventCard` `GC.Desk.cs:898` | all | `01e_desk_event`, staged with `EventPool[0]` (UiSD:4216-4244), D+R | 1m C1-C5 (§41); §726 | |
| F-21 | Desk GAME OVER stamp and reason | `_isGameOver` -> `DrawDeskGameOver` `GC.Desk.cs:1127` | all | `01f_desk_gameover`, staged with a harness reason (UiSD:4247-4270) | 1m conditionals (no stamp) | The staged reason "Out of office at year N…" (UiSD:4264) is a sentence the game can no longer produce. See §5 (S-1). |
| F-22 | Desk as loaded from a save (resumes PAUSED) | saves -> Load | all | `01l_desk_as_loaded` with `-shotload=` (UiSD:487-496) | as F-18 | |
| F-23 | Effects card, the PM's own draft ("Effects of your draft"; empty: NO DRAFT PENDING…) | `DeskEffectsNote` (`Assets/Scripts/UI/DeskRoleRules.cs`) -> `DrawDeskEffectsCard` `GC.Desk.cs:676` | PM | **none on film**. "The governing states … are the check's to prove, not the film's: neither film's player governs at a Desk frame" (§694); held by `DeskRoleCheck` | §694 ruling; 1m-r2 lineage | A PL film played by KO governs after the warm-up (§780, §785), but no Desk frame from it is recorded (INFERRED). |
| F-24 | Effects card for OPP/SUP/JP: the government's budget before the chamber | `TableGovernmentBudget` -> the card | OPP/SUP/JP; SE and US filmed | `93a_desk_government_budget` (UiSD:2006-2035; last in the sweep), D+R | §694 | |
| F-25 | Effects card: the player's alternative tabled | `TableShadowBudget` | OPP/SUP (where the procedure weighs an alternative) | `93b_desk_alternative_tabled` (UiSD:2036-2044). Skipped where refused: the US refuses (§694) | §694 | |
| F-26 | Effects card, empty for a non-PM: NO BUDGET BEFORE THE CHAMBER… | as F-24 | OPP | `01c_desk` (§694: "01c the opposition's empty state at turn 0") | §694 | |
| F-27 | Role chip: GOVERNING / CARETAKER / JUNIOR PARTNER / SUPPORTER FROM OUTSIDE / IN OPPOSITION / none | `RoleChip` `GC.Desk.cs:276-288`, `DrawRoleChip` `:294-325` | by role | IN OPPOSITION on SE frames. **GOVERNING, JUNIOR, SUPPORTER and CARETAKER lack a frame** (§670's list). PL-KO governing is INFERRED. | **21e, LOOKED AT 29 SEP** (§685) | |
| F-28 | Desk calendar card and "This month" card | `DrawDeskCalendarCard` `GC.Desk.cs:790`, `DrawDeskMonthCard` `:818` | all | `01c` and `01d` incidentally | 1k (no stamp); §726 | **The calendar month-flip pin films the Docket, not the calendar.** `80a_calendar_month_end` and `80b_calendar_month_flip` set the Decisions tab with `_onDesk` false (UiSD:3387-3395; `_onDesk` set false at :610, never set back before the pins). See §5 (S-8). |
| F-29 | Desk dense view (†) | page title † (`DeskProvenance`) | all | **none**: the driver switches † on only for Energy (UiSD:769), People (:870) and the Policy web (:1594) | D16 §2 | |
| F-30 | Rail: exactly one lit cell; the campaign cell during a run-up; HELD chip; held-cell marks | `DrawFoldedRail` GC:5116; `RailLit` in `DeskRoleRules.cs` | all | every IMGUI frame; a `RAIL: n cell(s) lit` error would fold the film (§694) | 1n / 1n-r2 / 1n-r3 / v3.5 rail (§723); no stamps | |
| F-31 | Interrupt banner: the HELD line of chips naming the rail cells | `DrawFoldedInterruptBanner` GC:5859; `CollectHolds` GC:5909-5930 | all | `90`, `91`, `03b`-`03d`, `83a`/`83b`, `84`/`84b`, `e7a`, `cl2_campaign_hq_scandal` | 1m C27; PF-11 closed (§648) | The declaration hold (no confidence declared) has no frame (§670). |
| F-32 | Slip, level 1 (opened by the pointer after 250 ms) | `DrawSlips` `GC.Slip.cs:72` | all | **none by design**: "while the harness is armed no pointer slip opens" (§685) | 19b (no stamp) §666, §708, §733 | |
| F-33 | Slip, level 2, and pinned chains ①-③ | pin -> `GC.Slip.cs` | all | `04i_people_slips_pinned`, `04b2`, `04c2`, `02a/02b_*_slips_pinned`, `05a_budget_tax_taxact_slip` (PL), `07a_*_president_elect` (PL), `01k1` | 19b / 23c (23c LOOKED AT, §701) | |

### 1c. Statistics, Docket, People

| ID | screen/state | reached by | countries/roles | film shot(s) | board | notes |
|---|---|---|---|---|---|---|
| F-34 | Statistics › Domestic (v3.5) at rest: public finances, sectors, live series, Society, More readings, Your policies (NIL) | rail STATS -> `DrawStatisticsTab` GC:7305 -> `DrawDomesticStatisticsContent` `GC.Statistics.cs:363` | all | `02_statistics`, `02a_statistics_domestic` (+`_rows`, `_deep`, `_slips_pinned`), D+R | 27a / V35 (no stamp) §728, over **23a + 23c (LOOKED AT, §701)** and 2a/1a/1l (no stamps) | "More readings" and "Your policies" were kept outside the composition and asked of Design (§728). |
| F-35 | Statistics › International (v3.5): world trade card, trade and rating, partners and peers; the pair page kept | title tab -> `DrawInternationalStatisticsV35` `GC.StatisticsV35.cs:270` | all | `02b_statistics_international` (+`_rows`, `_deep`, `_slips_pinned`; chips asserted UiSD:907-910), D+R | 27a (no stamp) §729; the pair page is **23b (LOOKED AT, §701)**, "its faces are D-ST's" (§729) | |
| F-36 | Statistics fiscal breach: warning ink and ✗, the slip names the rule | `FiscalRules` (§725) | the euro-area five when a ratio passes the rule; FR at its seed (§725) | `02_statistics` in `film725` (FR, real, §725); no FR dry film since | §725 ruling (A6) | |
| F-37 | Impact ledger populated ("Your policies" with entries) | play with drafts over turns | all | `c10_ledger_top`, `c10_ledger_scrolled` with `-shotledger` (UiSD:5831-5903) | none recorded | PF-18 is open. |
| F-38 | Trace panel open: approval / confidence / debt | stat chip -> `StatTracePanel.RequestSelection` | all | `06h_policylaws_trace_approval`, `06i_…confidence`, `06j_…debt` (UiSD:1536-1555, sweep); `93_trace_approval`, `93b_trace_confidence`, `93c_trace_debt` (`-shotstates`, UiSD:3765-3810) | none numbered | |
| F-39 | Statistics dense view (†): window dates, pair provenance, glyph key | † -> `DrawStatsDenseLines` GC:7372-7379 | all | **none** | 23a (LOOKED AT) | |
| F-40 | Docket list: an item with its body open (cabinet decision options) | rail DOCKET -> `DrawDecisionsTab` `GC.DocketV35.cs:24` | all | `03_decisions` (cabinet decision staged by the harness, UiSD:2466-2490), `03a_decisions_options` (UiSD:800-812), D+R | 1d (v2) / 8f option card / 27a (no stamps) §731 | PF-14 was closed in §591. |
| F-41 | Docket: budget process item (HOLDS TIME) with its body | `GetPendingBudgetProcess` -> `GC.DocketV35.cs:88-97` | all | `03b`, `03c` (staged flag), `03d` on Budget (UiSD:2732-2786); `83a`/`83b` reached by search (`-shotstates`, UiSD:3505-3527) | 1d / 27a | |
| F-42 | Docket: central bank appointment, candidates as cards | `_fedChairCandidates` -> `GC.DocketV35.cs:44-52` | SE, US, PL (chairs seeded at `Assets/Scripts/Data/WorldFactory.cs:962, :992, :997`) | **no shot names it**. 90/91 film Statistics and Budget under that hold (UiSD:3315-3334) | 27a | INFERRED: a Docket frame can carry it if the hold coincides. |
| F-43 | Docket: foreign-policy meeting | rolled -> `GC.DocketV35.cs:53-62` | all | `84_decisions_pending`, `84b_meeting_decisions` (`-shotstates`, bounded search, UiSD:3532-3580) | 1d / 27a | |
| F-44 | Docket: a new-portfolio cabinet decision | `CabinetSystem.TryRollDecisions` | all | `82d_cabinet_new_decision` (UiSD:3459-3489) | 27a | |
| F-45 | Docket empty: "Nothing is waiting on you" | `GC.DocketV35.cs:99-105` | all | **none**: every Docket frame stages or waits for an item (INFERRED from the shots above) | 27a | |
| F-46 | Docket: minister alerts (cabinet events of the past year) | `GC.DocketV35.cs:79-87` | all | incidental only, never staged | 27a | |
| F-47 | People (v3.5): population card, sections as tiles, the draft's ▲▼ on tiles | rail PEOPLE -> `DrawDemographicsTab` `GC.PeopleV35.cs:32` | all | `04_demographics`, `04a`-`04f` (UiSD:836-863) | 27a (no stamp) §732; 20a (no stamp) §662; **20b LOOKED AT (§686) but retired**: `PlateRetrofit20b = false` (`GC.Health.cs:95`; §732) | The sweep drafts spending lines before the tabs (UiSD:594-595), so ▲▼ draft states are likely on these frames (INFERRED). |
| F-48 | People slips pinned (levels 1-2) | pin | all | `04i_people_slips_pinned`, `04b2_…wait_pinned`, `04c2_…billed_pinned` | 19b / 27a | |
| F-49 | People dense view (†) | † | all | `04g`, `04g2`-`04g4`, `04h_people_dense` (UiSD:864-889) | 27a; rule 3 | |

### 1d. Budget, Laws, Politics, Energy

| ID | screen/state | reached by | countries/roles | film shot(s) | board | notes |
|---|---|---|---|---|---|---|
| F-50 | Budget at turn 0 (seed named) and Budget at rest (no draft) | rail BUDGET -> `DrawBudgetProcessTab` `GC.BudgetV35.cs:39` (GC:2817) | all | `05t_budget_turn0` (UiSD:520-526), `05r_budget_rest` (UiSD:586-592) | 1b (v2) / 27a §734 | |
| F-51 | Revenue tab: tax dial tiles, the if-passed panel, the strip, the CTA | title tab | all | `05_budget`, `05a_budget_tax` (+`_dragged`, `_rows_carbon`, `_rows`, `_deep`) (UiSD:1121-1166) | 27a (no stamp) §734; income-tax statute card is 15b (kept) | P4-1 geometry asserted while dragging (UiSD:1135). |
| F-52 | Revenue: Poland's tax act on the if-passed slip | `WorldClock.StatutePartsAreActs` (`Assets/Scripts/Elections/WorldClock.cs:364`) | PL | `05a_budget_tax_taxact_slip` (UiSD:1136-1151) | §768, §773 | |
| F-53 | Spending tab: dial tiles in two groups, range caption at the 14 px floor, closed year | title tab | all | `05b_budget_spending` (+`_dragged`, `_rows_dragged`, `_rows`, `_deep`) (UiSD:1173-1290) | 27a §735 | PF-2 was closed in §592. |
| F-54 | Pension row and dial: the law's path; 2026 and 2034 states; drafted; reloaded; set by a bill | Spending -> pension card | all six (statute per country) | `05b_budget_spending_pension`, `_pension_2026`, `_pension_2034`, `_pension_dial_drafted`, `_pension_dial_reloaded`, `_pension_dial_set_by_bill` (UiSD:1200-1281) | 15c / 15c-r2 / 15c-r3 (no stamps) §509, §515, §590 | The card's slip says "KEPT AS BUILT - THE COMPOSITION'S BUDGET DRAWS NEITHER; ASKED" (`GC.BudgetV35.cs:649`). See §5 (S-3). |
| F-55 | Kept tabs: Welfare, Infrastructure, Fund | title tabs -> `DrawBudgetKeptCategory` | all | `05c_budget_welfare`, `05d_budget_infrastructure`, `05e_budget_swf` (+`_rows`, `_deep` each) | none in v3.5. "Kept as their own tabs, drawn as built, and asked" (`GC.BudgetV35.cs:15-17`; §734) | |
| F-56 | Budget CTA by role: "Introduce budget bill [n changes]"; lock line; "Your alternative is tabled…"; "procedure not yet modelled"; "Table an alternative budget" | `DrawBudgetAction` `GC.BudgetV35.cs:856-910` | PM / OPP / SUP / JP; procedure by country (`WorldClock.WeighsAlternative`) | Only the role the film seats is filmed: SE OPP; PL PM with KO. No frame is recorded per variant (INFERRED). | §632-§634, §734; "KEPT - THE COMPOSITION DRAWS NO OPPOSITION ACTION; ASKED" (`GC.BudgetV35.cs:901`) | |
| F-57 | Budget page under the budget-process hold, and under the fed-chair hold | as F-41 | all | `03d_budget_process_open`, `83b_budget_pause_budget`, `91_interrupt_held_budget` | 1b | |
| F-58 | Pages with their controls disabled at game over (Budget, Energy, Cabinet, Bank, Laws except the web, Docket) | `_isGameOver` gates at GC:2817-2830 and the v3.5 pages | all | **none** | none | |
| F-59 | Laws › Labour (v3.5): readings as tiles, dials in model units; minimum wage OFF in SE and IT | rail LAWS -> `DrawPolicyLawsTab` `GC.LawsV35.cs:40` | all | `06_policylaws`, `06a_policylaws_labormarket` (+`_caption_drag`, `_caption_held`, `_caption_faded`, `_rows`, `_deep`) (UiSD:1058-1112) | 27a §737; 6a slider family (no stamp) | |
| F-60 | Laws › Crime & justice: set by law, no knob | title tab | all | `06b_policylaws_crimejustice` (+`_rows`, `_deep`) | 27a §738 | |
| F-61 | Laws › Sectors: eight sector tiles, five dials; caption states | title tab | all | `06c_policylaws_sectors` (+captions, `_rows`, `_deep`) | 27a §739 | |
| F-62 | Laws › Policy web: rest, a policy node, a stat node, provenance, restored | title tab -> `DrawPolicyWebCard` `GC.PolicyWebV35.cs:60` | all | `06d_policylaws_policyweb`, `06j_…_rest`, `06k_…_node_policy`(+`_rows`), `06l_…_node_provenance`, `06l_…_node_stat`(+`_rows`), `06m_…_restored` (UiSD:1557-1624) | 27a §740; **2b retired** (§740) | Node descriptions reach the slip (`GC.PolicyWebV35.cs:390`). See §5 (S-5). |
| F-63 | Laws › Trade: in a bloc; outside a bloc (US base tariff dial); overrides; draft moved and reset | title tab | all; US outside a bloc (§741) | `06e_policylaws_trade` (+`_rows`, `_deep`), `06m_policylaws_trade_draft_moved`, `06n_…_draft_reset` (UiSD:1937-1990) | 27a §741 | |
| F-64 | Laws › Laws: in force and before Parliament (D24 verbs); the statute book with its detail pane; expected effects; ENACTED·REPEALABLE; the four category cards; the monetary row; arrival on the electricity-tax filter | title tab -> `DrawLawsInForceCard` `GC.LawsV35.cs:907` + `DrawLawsTab` GC:7686 | all | `06f_policylaws_laws` (+`_rows`, `_deep`), `06g_laws_expected_effects`, `06g_laws_repealable`, `06i_laws_monetary_row`, `06h_laws_{institutions,fiscal,monetary,electricity}_card`, `08_energy_laws_link_arrived` (UiSD:1367-1452, :750-757) | 27a §742; 1i/1j law browser (no stamp); 10b; 19a pairs (§662) | The statute book's slip carries builder text (`GC.LawsV35.cs:106`). See §5 (S-3). |
| F-65 | Laws: the veto's at-risk face "CARRIES · VETO n %" | PL with a party the President's backing party opposes | PL (`-shotparty=KO`) | `real785` read on `06f` (§785), D+R with the flag | §778, §785 | |
| F-66 | Pending bills of every type (cards with live PASS/FAIL) | bills introduced | all | `85a_bills_parliament`, `85b`-`85e`, `85g_bill_laws`, `85f_bill_tax_rows` (`-shotstates`, UiSD:3583-3712) | 1c / 1d; `DrawPendingBillCard` GC:9405 | PF-10's second finding is open. |
| F-67 | Politics › Parliament (v3.5): the chamber as one bar with its majority tick, party tiles, divisions | rail POLITICS -> `DrawPoliticsTab` `GC.PoliticsV35.cs:33` -> `DrawParliamentV35` | all | `07_politics`, `07a_politics_parliament` (+`_rows`, `_deep`); the seat-count assert on every parliament frame (UiSD:2559-2571); `87`, `87b` divisions (`-shotstates`) | 27a §743; 1c | |
| F-68 | Parliament: support agreement OWED | `DrawSupportAgreements` `GC.ParliamentRows.cs:74` | SE (where a supporter exists) | `07a_politics_parliament_deep` (§670) | **21b, LOOKED AT** (§685) | |
| F-69 | Parliament: agreement DELIVERED / BROKEN / WITHDRAWN | same | SE | **none** (§670: "the films' government never delivers or breaks an item") | 21b (LOOKED AT) | |
| F-70 | Parliament: caretaker line (CARETAKER · SINCE) | `DrawCaretakerLine` `GC.ParliamentRows.cs:135` | SE, DE after an election | `e8a_formation_round_opened`, `e8c_formation_ballot_eve` (`-shotformation`). After the warm-up's held election on `07a` (INFERRED) | 21b (LOOKED AT) | |
| F-71 | Parliament: the Speaker's round (asked first, proposal due, offer to the player, player asked: OPEN THE SHEET) | `DrawSpeakerRound` `GC.ParliamentRows.cs:218` | SE (Riksdag), DE (Bundestag) (`SimulationManager.RoundsApply`, `Assets/Scripts/Simulation/SimulationManager.cs:3234-3236`) | `07a_politics_parliament_speaker_asks` (staged), `e8a`/`e8c`. The OFFER TO PLAYER state is accepted in the formation tail, not filmed (UiSD:4995-5000) | 21b (LOOKED AT) | |
| F-72 | Parliament: confidence. NO CONFIDENCE DECLARED with the week's answers; the opposition's motion count with MOVE NO CONFIDENCE; LEAVE THE GOVERNMENT | `DrawConfidence` `GC.ParliamentRows.cs:699` | SE (the motion as OPP); answers when PM | The motion as OPP: SE `07a` (dryps3i, §636; INFERRED on today's frames). **NO CONFIDENCE DECLARED: none** (§670) | 21b (LOOKED AT) | |
| F-73 | Parliament: the constructive vote (Art. 67) | `DrawConstructiveVote` `GC.ParliamentRows.cs:657` | DE | DE `07a` (drys698, drys754) | §698, §754 (no board) | |
| F-74 | Parliament: the reference row (history beside the count) | `DrawReferenceRow` `GC.ParliamentRows.cs:369` | SE, DE, PL (§767) | `07a` frames on those films | 21d/21b lineage | |
| F-75 | Parliament: the record row (US-2) | `DrawRecordRow` `GC.ParliamentRows.cs:453` | US | `real783` to `07b` (§783) | §783 (no board) | |
| F-76 | Parliament: the President row and president-elect slip | `DrawPresidentRow` `GC.ParliamentRows.cs:534` | PL | `07a_politics_parliament_president_elect` (UiSD:1303-1353: rounds held for the film and their days PLANTED) | §770 (no board) | |
| F-77 | Politics › Compass (v3.5 chips, six countries kept) | title tab | all | `07b_politics_compass` (+`_rows`, `_deep`) | 27a §744; the kept six-country compass's slip carries builder text (`GC.PoliticsV35.cs:312`) | PF-3 was closed in §595. |
| F-78 | Politics › Cabinet (v3.5): portfolio cards, vacancy, shortlist, search | title tab | all (Cabinet disabled at game over) | `07c_politics_cabinet` (+`_rows`, `_deep`), `82a_cabinet_search`, `82b_cabinet_appointed`, `82c_cabinet_new_portfolios` | 27a §745 | |
| F-79 | Politics › Bank (v3.5, a chair seated): rate and path, rule bar, two readings, governor; rate page kept; selection modal | title tab -> `DrawBankV35`; `DrawRiksbankPage` GC:4064; `DrawFedChairSelectionModal` GC:4325 | SE, US, PL (chairs at `WorldFactory.cs:962, :992, :997`) | `07d_politics_federalreserve` (+`_rows`, `_deep`) | 27a §746; 5f / 8c / 10c (no stamps) | The selection modal itself has no named frame (INFERRED). |
| F-80 | Politics › Bank (euro area, no chair): the legacy tab under the v3.5 title | `GC.PoliticsV35.cs:95-101` -> `DrawFederalReserveTab` GC:3950 | DE, FR, IT | `07d_politics_federalreserve` on those films (drys746 for DE, §746) | "keeps its tab as built" (§746); no v3.5 board | No FR or IT dry film since the ledger began (§0). |
| F-81 | Energy › Overview: prices and stacks, plants and orders, queue, mandate, why-plates | rail ENERGY -> `DrawEnergyTab` `GC.EnergyV35.cs:34` | all six (`EnergyLayer.Has`; the "no energy layer" text at `GC.EnergyV35.cs:81` is unreachable for the six) | `08_energy`, `_plate`, `_mid`, `_lower`, `_decisions`, `_decisions_ordered`, `_decisions_queue_full` (UiSD:633-694) | 27a §747; 12a-12f, 15a, 16a (no stamps); D-EN asked §719, no answer recorded | PF-6 is open. |
| F-82 | Energy › Policy: instrument dials and drag captions; laws link; provenance plates | title tab | all | `08_energy_instrument_dials`, `_dragged`, `_laws_link`, `_instruments_provenance`, `_electricity_tax_provenance` (UiSD:695-795) | 27a §748 | |

### 1e. Formation, the campaign, and screens that only a film can reach

| ID | screen/state | reached by | countries/roles | film shot(s) | board | notes |
|---|---|---|---|---|---|---|
| F-83 | Formation sheet: asked by the Speaker; supporter asked with demand rows; a partner refusing; TABLE IT; Germany's chancellor variant | `AutoOpenFormationSheet` GC:9650 -> `DrawFormationSheetStage` `GC.FormationSheet.cs:31` (GC:2754) | SE, DE when the player's party is asked | `07a_politics_parliament_formation_sheet`, `_rows`, `_revised` (staged, UiSD:944-1052); `e8b_formation_sheet` (live, `-shotformation`) | **21a, LOOKED AT** (§685); §647, §705 | |
| F-84 | Formation tail: round opened, ballot eve, government installed | day path after the night | SE, DE | `e8a`, `e8c`, `e8d` (`-shotformation`, UiSD:4947-5022); real `de707f_1280`/`_2560` (§707) | 21b | |
| F-85 | Campaign HQ in the run-up: opened, a verb queued, a day stepped | rail CAMPAIGN cell (`DrawRailCampaignCell` `GC.Campaign.cs:248`) -> `OpenLiveCampaign` `:140` -> `DrawCampaignHqStage` `:282` | SE, DE (`LiveCampaignSetup.TryFor`, `Assets/Scripts/Elections/LiveCampaignSetup.cs:148`) | `cl1_runup_hq_opened`, `_queued`, `_stepped` (`-shotinterrupts`, UiSD:4626-4675) | none of its own (built "in the v3 idiom", `GC.Campaign.cs:1-30`); the masthead was re-cut by 21c (LOOKED AT, §685) | The HQ prints "kr" whatever the country (`GC.Campaign.cs:446-447`). See §5 (S-2). |
| F-86 | Campaign opening interrupt (HQ opens itself; the clock is held) and acknowledged | sim begins the campaign -> Update | SE, DE | `e7a_campaign_opening_hq`, `e7b_campaign_opening_acknowledged` (UiSD:4680-4728) | as F-85 | |
| F-87 | HQ in the campaign: a rally queued; a story waiting for an answer (clock held); answered; stepped | live HQ chips | SE, DE | `cl2_campaign_hq_rally_picked`, `_scandal`, `_scandal_answered`, `_scandal_stepped` (story staged, UiSD:4730-4779) | as F-85 | |
| F-88 | Live campaign map as the region picker: picked | HQ masthead -> `OpenLiveCampaignMap` `GC.Campaign.cs:170` | **SE only** (`CampaignMapDrawn` `GC.Campaign.cs:163`: "Germany's Länder wait on Design") | `cl2_campaign_map_live`, `cl2_campaign_map_picked` | 4a cartogram (no stamp) §438; K-1c is open | |
| F-89 | Declarations page: DECLARED standing and lifted since the last election | HQ DECLARED chip -> `DrawCampaignDeclaredStage` `GC.CampaignDeclared.cs:75`; available when `GC.CampaignDeclared.cs:34-35` | SE (DE INFERRED). **PL has a timeline but no page** (§776: "LiveCampaignSetup stages none for Poland") | `ps3d_runup_declared`, `ps3d_campaign_declared` (UiSD:5934-5958) | **21c, LOOKED AT** (§685); §657 | |
| F-90 | Staged HQ snapshots: pre-campaign, campaign, over-committed | harness `SetCampaignScreen` | SE (the film refuses other countries, UiSD:5107-5117) | `e1_campaign_hq_*` (`-shotcampaign`) | none | Fixture states. The live HQ is F-85 to F-87. |
| F-91 | **HARNESS ONLY**: action ladder (W-E3) | `SetCampaignActionScreen`, called only by UiSD:5545 (`GC.CampaignAction.cs:13-14`: "HARNESS ONLY") | none in play | `e3_campaign_action_{tight,wide,unmeasured}` | none | Filmed, not reachable in play. |
| F-92 | **HARNESS ONLY**: polling (W-E4) | `SetCampaignPollingScreen`, UiSD:5450 only | none | `e4_campaign_polling_{rich,poor,nomomentum}` | none | Filmed, not reachable in play. |
| F-93 | **HARNESS ONLY**: debate (W-E5) | UiSD:5250 only | none | `e5_campaign_debate_{prep,midway,verdict}` | none (D-8.6 asked) | Filmed, not reachable in play. |
| F-94 | **HARNESS ONLY**: results and attribution (W-E7) | UiSD:5318 only | none | `e7_results_{largest,lost_ground}` | none | Filmed, not reachable in play. |
| F-95 | **HARNESS ONLY**: coalition sheet (W-E8), including the "A HUNG CHAMBER" fixture with parties A/B/C | UiSD:5372 only; fixture at UiSD:5389-5406 | none | `e8_coalition_{confidence_and_supply,new_election,majority}` | none | K-1j (open) is about this sheet's ledger. |
| F-96 | **HARNESS ONLY**: the campaign map with bought regional or full detail | `SetCampaignMapScreen`, UiSD:5633 | none (the live map is "honestly unknown, regional detail not being on sale", `GC.Campaign.cs:164-168`) | `e2_campaign_map_{unbought,regional,full}` | 4a | |
| F-97 | **HARNESS ONLY**: instrument ladder | `SetInstrumentLadder` GC:5529, UiSD:5912 only | none | `ladder_*` (20 kinds, UiSD:5809-5813) | none | |

### 1f. Canvas takeovers, scenarios, game over, playtest saves

| ID | screen/state | reached by | countries/roles | film shot(s) | board | notes |
|---|---|---|---|---|---|---|
| F-98 | Signing entrance (mid-envelope) and settled: CARRIED + Sign | a division recorded -> `TriggerSigningForNewestDivision` GC:2304 -> `SigningScreen.Build` `SigningScreen.cs:78` | all | `89d_signing_entrance`, `89e_signing_settled` (UiSD:1650-1675), `89_signing_document` (UiSD:4085-4102), R-only claim | 1g (v2) / 26d / 27a (no stamps) §749 | |
| F-99 | Signing LOST + File | a lost division | all | `89c_signing_rejected` (UiSD:4114-4128), R-only claim | §749 | |
| F-100 | Signing VETO STANDS (the Sejm's 3/5 vote on the veto) | `PresidentialVetoGate` | PL | `89g_signing_veto` (UiSD:1678-1692; draw planted to veto, UiSD:1726), R-only claim | §761 | |
| F-101 | Signing budget contest: FRAMES ADOPTED | a frame decision (§633) | SE (the staged sides name SE parties; skipped elsewhere, UiSD:1697) | `89f_signing_budget_contest`, R-only claim | §633, §749 | |
| F-102 | Signing ALTERNATIVE ADOPTED | `SigningScreen.cs:271` | SE OPP | **none**: the staged contest adopts the government's frames whenever forG >= forA (UiSD:1748) | §749 | |
| F-103 | Signing VETO OVERRIDDEN | `SigningScreen.cs:272` | PL | **none** (the film plants a veto that stands) | §761 | |
| F-104 | Signing yielding (the seal) and restored | Sign | all | `89a_signing_yielding`, `89b_signing_restored` (UiSD:4104-4112) | 1g | |
| F-105 | Signings the game opens by itself during play (the government's budget in the run-up; before the night) | live division queue | SE, DE | `cl1_signing_government_budget[_n]`, `e7_signing_before_the_night` (`SealLiveSigning` UiSD:5025-5050) | §749 | |
| F-106 | Election night, live, Sweden: complete count, cartogram, who governs, reference ("Sweden 2026, as it happened"), foot "CONTINUE" in paper or "CONTINUE · FORM A GOVERNMENT" in brass | polling day -> `CheckElection` GC:6349 -> `ShowElectionNight` (GC:6492-6634) -> `ElectionNightScreen.Build` `ElectionNightScreen.cs:140` | SE (`ElectionNightFromModel.Available`, `Assets/Scripts/Elections/ElectionNightFromModel.cs:38-39`) | `88n_election_night_takeover` (`-shotstates`, UiSD:4139-4150), `e7c_election_night_takeover` (`-shotinterrupts`, UiSD:4823-4846), R-only claim | 1h / 4a / 5c (no stamps); foot and reference **21d LOOKED AT** (§685); PF-16 fixed §609 | |
| F-107 | Election night, live, Germany: Länder map, the 5 % line, the Bundestag chamber, "THE BUNDESPRÄSIDENT PROPOSES FIRST" | same, German branch `ElectionNightScreen.cs:334-348` | DE | `e7c` on DE (`de707f_1280`/`_2560`, §707); `e7e_election_night_partial_staged` (UiSD:4888-4944), R-only claim | **24a / 24b LOOKED AT** (§707) | |
| F-108 | Election night, staged Swedish fixture: early, partial, called, final, model, player-first | harness only, `ElectionNightScreen.Build` with a fixture (UiSD:4480-4605) | none in play; fixture is SE 2022 vs 2018 | `e6_election_night_{model,early,partial,called,final,player_first}` (`-shotelectionnight`), R-only claim | 1h, 21d | Fixture artefacts are known: "NO PLAYER'S PARTY ON THIS BALLOT" (§685). |
| F-109 | After the night: the Desk, or game over after the night | `DismissElectionNight` GC:6706 -> `ApplyElectionVerdict` GC:6714-6727 | SE, DE | `88c_after_election_night` / `88b_game_over` (UiSD:4151-4156); `e7d_after_election_night[_gameover]` (UiSD:4851-4856) | 1m | The game-over branch fires only if the film's party wins no seat. The largest party never does (INFERRED). |
| F-110 | **An election held with no night** (PL), and **no election held at all** (IT, FR, US) | `ShowElectionNight` returns early when Available is false (GC:6494-6499); `ApplyElectionVerdict` then lands at once (GC:6361-6365) | PL: the verdict sentence is never shown unless it ends the game. IT/FR/US: "a country whose calendar is not modelled holds none" (CLAUDE.md) | **no screen exists** | 28a/28b PROPOSAL, not LOOKED AT; **not built** (§797) | |
| F-111 | Scenario entry | scenario strip -> `StartScenario` GC:2038 | SE (Inherit the Fund), IT (Italy Debt Crisis) | `94_scenario_entry`, `95_italydebt_entry` (`-shotstates`, UiSD:3812-3923) | none recorded | |
| F-112 | Scenario in progress (trace open) | play | SE, IT | `94b_scenario_in_progress`, `95b_italydebt_in_progress`, `95d_italydebt_trace_debt` (filmed at entry since PF-12, §598) | none | |
| F-113 | Scenario verdict screen (exclusive IMGUI; objectives with margins) | `CheckScenarioObjectives` -> `DrawScenarioVerdictScreen` GC:6790 (GC:2636) | SE, IT | `94c_scenario_verdict`, `95c_italydebt_verdict` | none (R-S3c/R-S3d rulings) | Whether Won or Failed is decided by the evaluator. Only the outcome the run reaches is filmed. |
| F-114 | GAME OVER after a scenario verdict is dismissed ("Scenario complete/failed - …") | `DismissScenarioVerdict` GC:2094-2103 | SE, IT | **none**: the film clears the verdict without dismissing it (UiSD:4064) | 1m | |
| F-115 | GAME OVER, out of parliament (won no seat): the night's foot sentence, then the Desk stamp | `ResolveElectionVerdict` GC:6414-6416, :6453-6455 | SE, DE (PL lands it straight on the Desk) | **none real**. Only the staged F-21 | 1m | These are the only two real game-over reasons: `_isGameOver = true` only at GC:2099 and GC:6718, plus a save load at GC:1033. |
| F-116 | Playtest saves (proof frames) | `-shotsaves` | US, SE | `p1_trade_bill_costs`, `p2_riksbank_rate_decision`, `p3_dense_midgame` (UiSD:1775-1915) | none | Staging proofs, not states of their own. |

---

## 2. Coverage gaps

### 2a. States that no film reaches

All of these are by reading the driver. Each cites the row in §1.
- **Front door**
  - F-04: settings from the desk.
  - F-05, F-07, F-08: the saves screen from the menu, empty, and its confirm and status states.
  - F-10: the IMGUI selector fallback.
  - F-14 for Italy's scenario: only `ScenarioLibrary.All[0]` is filmed.
  - The creation flow's ink refusal (F-16, INFERRED).
- **Desk**
  - F-23: the governing player's effects card. §694 says outright that no film's player governs at a Desk frame.
  - F-27: the GOVERNING, CARETAKER, JUNIOR PARTNER and SUPPORTER chips, as §670 recorded.
  - F-29: the Desk's dense view.
  - The calendar's month flip (F-28). The pin for it now films the Docket.
- **Statistics**: F-39, the dense view.
- **Docket**
  - F-42: the central bank's candidate cards. No named frame.
  - F-45: the empty Docket.
  - F-46: minister alerts. These are incidental only.
- **Budget**
  - F-56: every CTA variant except the film's own role.
  - F-58: every page at game over.
- **Parliament**
  - F-69: agreement DELIVERED, BROKEN or WITHDRAWN.
  - F-72: NO CONFIDENCE DECLARED with its two answers, and the declaration's HELD chip.
  - F-71: the offer to the player, which the formation tail answers without a frame.
- **Signing**
  - F-102: ALTERNATIVE ADOPTED.
  - F-103: VETO OVERRIDDEN.
- **Game over**
  - F-114: the game over after a scenario.
  - F-115: the game over out of parliament.
  - The only game-over frame is staged with a reason the game cannot produce (S-1).
- **Elections**
  - F-110: Poland's election has no screen and no shot. Italy, France and the USA hold no election.
  - The Swedish night's "In office" and "Out of office" sentences sit in the non-round branch, GC:6431-6463. Both countries that show a night run rounds (§646), so those sentences reach no night (INFERRED).
- **Slips**
  - F-32: a pointer-opened slip, suppressed by design while the harness is armed (§685).
  - The dense view outside People, the Policy web and Energy.
- **Geometry gaps**
  - No dry session at **1600x950** in the scope ledger, ever.
  - 1920 appears once (`drys750`, Sweden).
  - **Italy has no dry-film row at all** since the ledger began on 2026-09-22.
  - France has two rows (`drypap2`, `drys725`).
  - Poland has only 1280.
  - `Tools/film_scope.tsv`, read 2026-10-13.
- **Canvas states on real film**
  - The canvas rule asks for real films at 1280 and 2560 at a track's close (CLAUDE.md, §578).
  - The record shows 2560 real films for SE (`s718s_2560`, `film749d`) and DE (`de707f_2560`).
  - No 2560 real film is recorded in §749-§802 for the PL veto plate, the PL start panel, or the FR/IT/US selector frames (INFERRED from the sections read).

### 2b. States without a LOOKED AT board

**Most of the game.** By the record, LOOKED AT covers only 20b, 21a-21e, 23a-23c and 24a-24b. Everything else was built from boards that carry no recorded stamp.
- The **v3.5 composition**: 27a and `AssetPackArchive/V35_desk.zip`, built §722-§750. It governs the rail, Desk, Statistics, Docket, People, Budget, Laws, Politics, Energy, signing plate and map chips (F-18, F-34/35, F-40-49, F-50-82, F-98).
- **17a-17c and 18a-18b**, built "the D11 way" (§626): main menu, settings, masthead, start sheet (F-01-F-03, F-11, F-12).
- **Every earlier board**: 1a-1n, 2a, 4a, 5a-5f, 6a/6b, 7a/7b, 8a-8f, 9a-9d, 10a-10d, 12a-12f, 13a, 14a, 15a-15c-r3, 16a-16e, 19a/19b, 20a, 26d.
  - Of these, 20a was "read" but carries no recorded stamp. Its §662 build predates the §685 gate.

**States with no board at all** in the record:
- The saves screen (F-05-F-08).
- The party picker (F-13). The record names no board for the panel itself.
- The scenario strip, party pick, entry and verdict screens (F-14, F-111-F-113).
- The creation flow (F-16/F-17): D-CP was sent (§677) with no answer, and SP-4b is not built.
- Campaign HQ, its live screens and the staged snapshots (F-85-F-87, F-90). Built "in the v3 idiom"; only its masthead was re-cut by 21c.
- The harness-only Track E screens (F-91-F-96) and the ladder (F-97).
- The euro-area bank tab (F-80): "keeps its tab as built" (§746).
- The Budget's kept tabs (F-55) and every "kept as built, asked" block of the v3.5 pass (§726, §728, §729, §734, §737-§748; the 29 asks of `BOARDS_BUILT.md`, §751). Examples:
  - the Desk strip, effects card, compass and event card;
  - More readings, Your policies, the pair page;
  - the statute book, the six-country compass, the rate page;
  - the why-plates and "what else reaches it".
- Poland's President row and veto plate (F-76, F-100), US-2's record row (F-75) and Germany's constructive vote (F-73). These are rulings with no board.

**Consequence for area 10.** "Every UI state on dry film against its LOOKED AT board" can be run as worded only for the formation sheet, the Parliament blocks, the declarations page, the role chip, the night's foot and reference, Statistics' slips and pair page, and the German night.
- The night's foot and reference and the German night are Canvas surfaces. They need a real film, not a dry one.
- For everything else the audit needs a ruling on what stands in for the stamped board: the v3.5 composition, the older boards, or none.
- **This is a question for Elias. A session should not decide it.**

### 2c. LOOKED AT boards with no built state, or one built and then superseded

- **20b** (LOOKED AT 29 SEP, built §686): **no live state.**
  - v3.5 retired People's five family plates (§732).
  - The 20b rules are compiled off: `PlateRetrofit20b = false` at `Assets/Scripts/UI/GC.Health.cs:95`, read at `:268`, `:339`, `:384`, `:452`.
- **23a** (LOOKED AT): partly superseded by v3.5 Domestic (§728). Its Δ-unit rules, slips and held-seed rule persist through `StatsSheetCheck` and `StatsSlips`. Its card and plate drawing do not.
- **23b** (LOOKED AT): its map and trade parts were replaced by v3.5 International (§729). Its **pair page is kept as built**.
- **21b** (LOOKED AT): its blocks are kept *under* the v3.5 Parliament tiles (§743). States F-69 and F-72 have no frame.
- **28a/28b**: correctly not built, because they are not stamped (§797).
- Built and present: 21a, 21c, 21d, 21e, 23c, 24a and 24b.

---

## 3. Area-10 check map: which check measures what

| area-10 check | instruments that measure it today (where they run) | not measured by anything: the gaps |
|---|---|---|
| **Clipping / overflow (IMGUI)** | See the list below this table. | - Raw `GUI.Label` / `GUILayout.Label` sites: "~178 raw label calls" (`UiOverflowGuard.cs:29-32`) and "271 raw … sites the overflow guard cannot see" (`DryGuiPass.cs:31`). The label table reports them and judges nothing.<br>- **Overlap between two drawn elements**: §746, "an overlap no guard reports" (the rule's bar on its own name).<br>- Text over text on a slip versus the page under it, beyond 19b's anchor rule.<br>- Any state no film reaches (§2a). |
| **Clipping (Canvas)** | `CanvasTextGuard` (`Assets/Scripts/Testing/CanvasTextGuard.cs:7-27`): `preferredHeight` > rect, and width when wrap is off. **Real film only** (UiSD:2138-2160). | - Text clipped by an ancestor mask, and containment on Canvas (`CanvasTextGuard.cs:17-22`).<br>- e6 frames now assert (UiSD:4469, :4543), but `Tools/bar_tier.ps1:46` still says they carry no assert. |
| **Frame / edges** | `ScreenEdgeCheck`: four margin lines of each PNG, real only. Run before exit (`UiScreenshotCapture.cs:486-490`) plus its self-test in the cheap bar (`CheckSuite.cs:192`). | Interior clipping, and any dry film. |
| **Number format** | See the list below this table. | - **Date format consistency.** 21b's rule is "dates always `31 JAN 2029`" (§685), yet ISO dates reach player text: GC:1172 and GC:6623.<br>- **Currency or unit words per country** (S-2).<br>- Decimal precision outside Statistics.<br>- "Turn" against "Year" vocabulary (S-11). |
| **Colour neutrality** | See the list below this table. | - **No check that a contested figure draws in the neutral or direction ink at its draw sites.** §725 lists the sites by hand; the levers on their own screens "are each screen's own pass".<br>- UI v3.3 §5, "Colour never carries meaning alone" (`docs/specs/UI_V33_SPEC.md:44-45`): no instrument.<br>- The deferred "WITH THE DRAFT marker still prints in the good ink - the Budget pass" (§725): no check records whether it closed. |
| **Stale or wrong-country text** | See the list below this table. | **NONE generally.** Nothing checks that a string drawn in country X names X's institutions, currency, units or record year. That is the class §718 fixed by hand ("THE NATIONAL ASSEMBLY" for all six). See S-2, S-4, S-5, S-6, S-9, S-10. Builder-facing slips (S-3) also slip past `MetaTextCheck`'s tokens. |

The instruments behind each row:

- **Clipping / overflow (IMGUI)**, dry and real:
  - `UiOverflowGuard`: the `MeasuredLabel` choke point plus fixed-rect sites; deduped; total printed (`Assets/Scripts/UI/UiOverflowGuard.cs:7-37`).
  - The v3.5 **14 px floor** through `V35.FloorGuarded`, armed on each v3.5 page (e.g. `GC.Desk.cs:506`, GC:7334, `GC.StatisticsV35.cs:272`; §726).
  - `UiContainmentGuard`: composite widgets and named sites (`UiContainmentGuard.cs:6-38`).
  - Ledger geometry kept stable while dragging (UiSD:2394-2420).
  - Map-chip overlap on `01c` and `02b` (UiSD:4277-4295).
  - The hemicycle dot count (UiSD:2559-2571).
  - The selector's clearance (UiSD:2183-2207).
  - The `DryGuiPass` label table, which reports and does not judge (`DryGuiPass.cs:30-37`).
  - The empty-caption-band guard and `RangeCaptionCheck` (cheap bar).
  - The slip guard (§778).
- **Number format**:
  - `NumberLocaleCheck` (cheap bar): the one culture is installed, no other culture is built, and a four-culture battery passes (`Assets/Editor/NumberLocaleCheck.cs:13-30`; §718).
  - `StatsSheetCheck` (cheap bar): the Δ in the reading's own unit, decimals and the true minus. Statistics only (`StatsSheetCheck.cs:12-21`).
  - `DialLabelCheck`: dial ends against range.
  - `MoneyFormatDiagnostic`: `UiFormat.Money`. **It is not in the bar**; `CheckSuite.cs` has no entry for it.
- **Colour neutrality**:
  - `FiscalRulesCheck`: the breach *rule* and its constants, not the draw sites (`Assets/Editor/FiscalRulesCheck.cs:11-50`).
  - `InkContrastCheck`: contrast floors.
  - `PartyInkDrawSiteCheck`: party ink only in allow-listed files, never beside an area accent (`PartyInkDrawSiteCheck.cs:9-38`).
  - `CountryInkFenceCheck` and `D16AcceptanceCheck`: fences, † parity, the at-rest token.
  - `StatsSheetCheck`: gauges in Neutral is part of D-ST (§701).
- **Stale or wrong-country text**:
  - `MetaTextCheck`: developer-facing tokens in UI and catalog literals (`Assets/Editor/MetaTextCheck.cs:11-47`, patterns `:65-101`).
  - `StartPointsDiagnostic` and `StartBriefDiagnostic`: card and brief strings traced to records; no register numbers.
  - `WorldClockDiagnostic`.
  - `DeskRoleCheck`: no note names a lever the role lacks.
  - `StatsSheetCheck`: held seeds against the seed world.
  - People and Stats `SlipReachabilityCheck`.
  - `GermanNightDiagnostic`.
  - Not text checks, but close: `CaptureIdentity`, the wrong *surface* (real only, `CaptureIdentity.cs:7-33`), and the film header's country and width (§661), the wrong *film*.

---

## 4. Known open UI rows (do not re-file these as new)

Each line gives the row's status as the source shows it.

- **PF-6**, the Energy page reads a cached clearing at the day's price level. **OPEN** (`POLISIM_FEATURE_LIST.md:130`).
- **PF-10**, party names in a ten-pixel cell. The cell was **FIXED** in §594. **Still open:** "the page drew in one dry session of twelve", not measured (`:136`).
- **PF-18**, the impact ledger's except-worlds run a period ahead. **OPEN**, not measured (`:140`). It affects F-37.
- **PF-19**, the election-night install leaves a budget window open that the player no longer governs. **OPEN** (`:141`). It affects F-41/F-56 after F-106.
- **TL-1**, the dry film skips the Canvas selector's frames. Listed **OPEN** at `:161`, but **built in §645** ("THE DRY FILM FAILS, NEVER SKIPS…"; `Expect` at UiSD:305, :352, :373). The row's tracking is stale.
- **K-1j**, the coalition screen shows no party rule. **OPEN**, Code's (`:162`). It concerns the harness-only W-E8 sheet (F-95) and the DECLARED ledger.
- **K-1c**, the cartogram's mandate column on the 2026 roll. **OPEN**, Design's (`:190`).
- **D24**, UI v3.3's foundations. **Partly open** (`:142`). Still with Design: its reading of the 13 both-ways laws, the 16 px interest-rate crop, and the plates' rows TSV.
- **PS-7**: Italy's start still names a polling day the game does not hold, in the folder line, start card, brief and ledger (`:91`; §782's review). This is a known stale-text row.
- **E-47 / E-46**: billed chambers whose view says so. Data rows, not UI defects (`ERRANDS.md`).
- **§751 BOARDS_BUILT**: 29 v3.5 asks are open with Design. These are the "kept as built, asked" blocks.
- **§721**, items 3-6:
  - Crime draws 3 of 6 levers, Sectors 4 of 5, and Trade has no per-partner tariff. Asked.
  - The desk strip clips at 1280 and is held.
  - The working-week direction.
  - One field carries two names.
- **D-PL** (28a/28b): **not built**. The board is a PROPOSAL, not stamped (§797). Poland has no election night (F-110).
- **D-CP**: sent (§677), no answer recorded. **SP-4b** is not built (`:77-78`).
- **D-EN**: sent (§719), no answer recorded. Energy was then rebuilt from v3.5 (§747/§748).

Closed, for reference:
- PF-1 (§525), PF-2 (§592), PF-3 (§595), PF-4 (§554), PF-5 (§564/§568), PF-7 (§565), PF-8 (§559), PF-9 (§593)
- PF-11 (§648), PF-12 (§598), PF-13 (§578), PF-14 (§591), PF-15 (§600), PF-16 (§609), PF-17 (§680)
- The record lines: COMPLETED.md:32939-32945, :33160, :33172, :33184, :33200, :33245, :33277.
- §685's two Design sightings were fixed in §694: the opposition caption promising the rate dial, and two lit rail cells.
- §707's "seen on film" list was fixed in §718: National Assembly, THREE PORTFOLIOS, "1 seats", MARS.

---

## 5. Suspected discrepancies: leads for the films, not findings

Every line is **SUSPECTED**. Each was found by reading and none has been seen on a frame. These are leads for the audit's films.

- **S-1. The game-over film stages a reason the game cannot produce.**
  - The staged reason is "Out of office at year {turn}: the chamber formed a government without {key}." (UiSD:4264).
  - Since §619 "LOSING OFFICE NEVER ENDS A RUN" (GC:6446-6461).
  - The real reasons are out of parliament (GC:6414-6416, :6453-6455) or a scenario's verdict (GC:2099-2102).
  - The staged line also prints the party **key**, not its short name.
- **S-2. Campaign HQ prints "kr" for every country.**
  - The war chest: "kr · … % OF THE CHEST SPENT" / "kr · WAR CHEST" (`GC.Campaign.cs:446-447`).
  - Staff and office lines: `:537`, `:564`, `:575`, `:746`, `:903`.
  - A German game has a live HQ (`LiveCampaignSetup.cs:148`). Its purse is "national units" by ruling S-14 (`POLISIM_FEATURE_LIST.md:246`).
  - Lead: `cl1_runup_hq_opened` on DE.
- **S-3. Builder-facing text in player-visible slips.**
  - The words "THE COMPOSITION…", "KEPT AS BUILT…", "ASKED" and "(ELIAS'S RULING: …)" appear at these sites:
    - `GC.BudgetV35.cs:51`, `:287`, `:649`, `:837`, `:901`, `:1114`
    - `GC.EnergyV35.cs:300`, `:427`
    - `GC.LawsV35.cs:106`
    - `GC.PoliticsV35.cs:78`, `:312`
  - `MetaTextCheck`'s patterns do not cover these words (`MetaTextCheck.cs:65-101`).
- **S-4. A Swedish statute in every country's slip.** Parental leave's census reads "…(480 BENEFIT DAYS IS SWEDEN'S STATUTE; THE MODEL HOLDS WEEKS)" whatever the country (`GC.LawsV35.cs:165`).
- **S-5. A stale minimum-wage description.**
  - The Policy web's lever slip prints `PolicyWebRenderer.GetPolicyDescription` (`GC.PolicyWebV35.cs:390`).
  - For Minimum Wage that reads "(USA only - Sweden/Italy have no statutory minimum)" (`PolicyWebRenderer.cs:190`).
  - Since §756, DE, FR, PL and US carry statutory minimum wages.
- **S-6. "valkrets" and data keys on the German election night.**
  - `BuildCalls` is shared by the German branch (`ElectionNightScreen.cs:341`).
  - On the complete night, `SummariseGroup` prints "each at the valkrets count it was called" (`:1268`).
  - `CallText` and `SummariseGroup` print `partyNames[...]`, which are the data **keys** (`:1255`, `:1276-1278`). The German Greens' key is "Grune" (`Assets/Scripts/Data/PartySystem.cs:393`). CLAUDE.md requires the short name where an abbreviation is drawn.
  - Lead: `e7c_election_night_takeover` on DE.
- **S-7. `01d_desk_held` does not assert the hold its name claims.** `AssertDeskState` checks only `OnDesk` (UiSD:4195-4203).
- **S-8. The calendar month-flip pin films no calendar.**
  - `80a` and `80b` set the Decisions tab with `_onDesk` still false (UiSD:3387-3395, :610).
  - The calendar is a Desk card (`GC.Desk.cs:790`).
- **S-9. A fixed year in the declarations page's fallback.** A fact with no bracket keys reads "THE 2022 RECORD" (`GC.CampaignDeclared.cs:48`). That would be wrong for the 2026 timeline, and for Poland's 2023 timeline if Poland ever gets a page.
- **S-10. "VS 2021" is a literal** on the German tiles and legend (`LaenderTileView.cs:119`; `ElectionNightScreen.cs:509`). The fallback label is "GERMANY 2021" (`:454`). These go stale at a German game's second election (INFERRED reach).
- **S-11. Saves rows use the old vocabulary and an ISO date.**
  - The rows read "Turn {n}, yyyy-MM-dd" (GC:1172).
  - "Turn → Year" was cleared in playtest 3 (§198 §V), and the masthead says YEAR (§685, 21e).
  - Election night's estimate citation also prints an ISO date (GC:6623).
- **S-12. Three stale descriptions in `Tools/bar_tier.ps1:46`.**
  - It says e6 frames "carry no canvas assert". They do (UiSD:4469).
  - It lists 88n as "the sweep". 88n is a `-shotstates` pin (UiSD:4148).
  - It names no `01f2_start_points`, `89c` or `89g` among the Canvas shots, though 01f2 is a CountrySelectorScreen surface (18a/18b).
- **S-13. A stale code comment**, `CampaignMapDrawn`: "Germany's Länder wait on Design (D-DE)" (`GC.Campaign.cs:162`). D-DE was built for the night in §707. The German **campaign** map is still not built. This is TRACKING, worth a ruling on whether the German HQ should have a map.
- **S-14. The HELD frames are skipped for the euro three.** 90/91 are skipped for DE, FR and IT because no chair is seated (UiSD:3315-3326). The euro-area "rate push" hold has no frame.

---

## 6. Run cost, as the record measures it

**Measured**:
- **A Unity launch costs 23 s before any work** (§573).
- **Dry film**:
  - 12 sessions (6 countries x 1280 and 2560): **505-528 s**, `dryf2e` and `dryui1` (COMPLETED.md lines 30873, 30925).
  - 2 sessions at one width: **145 s**. 12 sessions: **522 s** (§573, §574).
  - **31-43 s a session** on the scoped items of §575.
  - One session, `drymm1` (Sweden 1280, 132 frames): **36 s** (§614).
  - Those sessions measured 111-132 frames. Today's sessions measure 142-155 (§0).
- **Real film**:
  - "Films at one width - 15 launches ~15 min, 55-80 s each", usually cut with `-shotstop` (§573).
  - **No wall time is recorded in the record for a full real sweep.** The full sweep is 155 frames for Sweden at 1280, or 186 with `-shotstates` (§725, §726, §731, §733).
  - An older environment note (2026-09-05) records a ~99-shot real film run against a 45-minute budget, overrun by a hang.
  - An item's whole close ran as **15 min 6 s** in one detached script: cheap bar, dry film, film and simulation bar (§575).
- **Bars**: cheap bar 25-55 s (`RunNamedBatch` 25 s for three). Simulation bar about 10-13 min (§573, §575).
- **Constraints**:
  - One Unity job at a time; the 2560 interrupts film died of out-of-memory after another job (§648).
  - Windowed films stall when Unity loses focus unless `runInBackground` is forced (`UiScreenshotCapture.cs:420-428`).
  - Teardown hangs are watched and recorded (§702; `Tools/unity_hangs.tsv`).

**Estimate for area 10 (INFERRED from the figures above):**
- **Plain sweep, dry, all six countries x four geometries (24 sessions):** about 24 x 45-60 s plus one 23 s launch, about **18-25 min**. This is extrapolated from 522 s for 12 sessions of about 111 frames, scaled to about 150 frames.
- **Plus `-shotstates` dry for six countries:** no recorded timing. Its day searches run up to four game years each (UiSD:3338). **Unmeasured; expect markedly longer per session.**
- **Plus `-shotinterrupts` (+`-shotformation`) for Sweden and Germany:** each plays the run-up day by day to polling day. Sweden's is 238 days (UiSD:532). No timing is recorded.
- **Plus the staged single-country modes** (`-shotcampaign`, `-shotelectionnight`, `-shotledger`): short, Sweden only.
- **Canvas states need REAL films** at 1280 and 2560 per country that reaches them. Roughly 6 countries x 2 widths for the selector and start panel, plus SE, DE and PL for signing and the nights. With no recorded full-sweep wall time, budget **tens of minutes per real film (INFERRED)**, run one at a time.
- **A first measuring step for the audit**: time one dry session per country at 1280 with and without `-shotstates`, and one real sweep, before committing to the matrix.
