# PoliSim — the gameplay audit: Phase 0, the inventory

A dated record (2026-10-13; the code read at `92114481`, whose `Assets/` is `1dd5b43f`'s): what the game holds, read from the code and checked against `POLISIM_FEATURE_LIST.md`, `COMPLETED.md` and the specs in both directions (`docs/audit/AUDIT_PROTOCOL.md`, PHASE 0). Like `COMPLETED.md`, it says what was read on its day; a later change to the code makes it older, not wrong.

**Where the detail is.** Six read-only slices, each with its feature table, its specified-but-missing and present-but-unspecified lists, its invariant map, its suspected discrepancies (leads for runs, never findings by themselves) and the actions a harness can take - `docs/audit/inventory/`:

| slice | file | covers |
|---|---|---|
| A | `A_front_door.md` | the main menu, the selector, start points, roles, created parties, scenarios, time and speed, save and load, run end, determinism hazards, **the harness's entry points** |
| B | `B_money.md` | budget acts and bills, tax, pensions, benefits, the fund, debt and fiscal rules, vetoes, the impact ledger and what the player is shown |
| C | `C_elections_office.md` | every country's electoral system, polling days, election night, formation, confidence, early elections, caretakers, term limits, game over |
| D | `D_campaign_cabinet_events.md` | the campaign, approval, events, the challenge scenarios, cabinet ministers, parliamentary gating, laws and statutes, the central bank, foreign policy |
| E | `E_energy_macro_society.md` | energy (fleet, blocks, zones, ETS, both ledgers), macro, demography, the six society-stat families, statistics and publication |
| F | `F_ui_states.md` | every UI state, which film reaches it, the Design board it was built from and whether that board was stamped LOOKED AT |

The specified-but-missing and present-but-unspecified items are the ledger's `GA-0` rows (`docs/audit/AUDIT_LEDGER.md`); the suspected discrepancies are the leads Phase 2's areas run down.

## The variant matrix

Each country holds one playable start; the two presidential starts are LOCKED (`StartPoints.For`). "Not built" is not a bug. The role a party holds on day one is the government of record's (`GovernmentRecord.RoleOf` on `WorldClock`'s record for the start's date); every new game logs it as a `ROLE:` line, which the harness reads back.

| country | start (opens) | head of government | coalition partner | support party | opposition | created party | elections inside a run |
|---|---|---|---|---|---|---|---|
| Sweden | Riksdag election 13 Sep 2026 (2026-01-18, the run-up) | **startable**: M | **startable**: KD, L | **startable**: SD | **startable**: S, V, C, MP | **startable** (opposition, no seats) | 13 Sep 2026, then every fourth September; extra elections |
| Germany | Bundestag snap election 23 Feb 2025 (2024-11-06) | **startable**: SPD | **startable**: Grüne, FDP | none on record | **startable**: CDU, CSU, AfD, Linke, BSW, SSW | not built | 23 Feb 2025 only |
| Poland | Sejm election 15 Oct 2023 (2023-02-19) | **startable**: PiS | none on record (reachable after 2023) | none on record | **startable**: every other pickable party | not built | every Sejm term; the AI's presidential rounds from 2025 |
| Poland | presidential election 2025 | LOCKED (not built) | | | | | |
| Italy | general snap election 25 Sep 2022 (2022-07-21) | **startable, provisional**: the formation's stand-in on the 2022 chamber | the stand-in's other parties | — | **startable** | not built | none (the card names one) |
| USA | presidential election 5 Nov 2024 (2024-03-12) | **startable as the governing party**, not as president: DEM; the record seats REP on 20 Jan 2025 | — | — | **startable**: REP | not built | none; House and president seated by record |
| France | presidential election 2022 | LOCKED (not built) | | | | | |
| France | legislative snap election 2024 (2024-07-18, governing mode) | **startable for every party** (a what-if, one-party cabinet) | not built | not built | not built | not built | none |
| scenarios | "Inherit the Fund" (Sweden, 12 turns), "Italy Debt Crisis" (Italy, 30 turns) - **RETIRED** (Elias, 2026-10-13): out of the matrix | any playable party (the pick offers every one) | | | | none | as the country's |

**The brief names three challenge scenarios; the game holds two** (`ScenarioLibrary.All`), and no spec or feature row names any - only `COMPLETED.md` (§21, §30, §36, §492). Row GA-0-14. **Elias's answer (2026-10-13): "Scenarios aren't part of the game's plan. Drop them from the matrix and file a row to retire them"** - the audit runs no scenario from area 2 on, and GA-0-14 (with GA-0-40) is the row that retires them in Phase 4; GA-0-26 and GA-0-44 are closed by it.

**Length cells.** To the first election: Sweden (day 238), Germany (day 109), Poland (day 238). Two cycles: Sweden and Poland only. Thirty years: anywhere, with elections in Sweden and Poland only.

**Outcome cells** (slice C §6; earliest day from the start where reachable):

| | win (the player's party leads) | loss (loses the PM role) | hung / coalition | no confidence or early election | term limit | game over |
|---|---|---|---|---|---|---|
| Sweden | M at day 0; else the 2026 round (from day 275) or after a carried motion | the 2026 round; an AI motion (limited reach, §655) | every formation; four rejections then an extra election | the player's motion (≥ 35 seats: S at the start, SD after withdrawing support); an extra election | none in law | no seats at an election (day 238 on) |
| Germany | SPD at day 0; else Art. 63 (from day 181) or a constructive vote | the 2025 round; an AI constructive vote | the 2025 formation | the constructive vote only; no early election | — | the 2025 election only |
| Poland | PiS at day 0; else a Sejm election's formation | the same days | the formation that night; none formed keeps the old government | not built | not the player's | no seats at a Sejm election |
| Italy | the stand-in at day 0 | unreachable | unreachable | unreachable | — | a scenario only |
| USA | REP by record (day 314) | DEM by record (day 314) | — | unreachable | not built | unreachable |
| France | day 0 (the what-if) | unreachable | unreachable | unreachable | — | unreachable |

## What drives the harness (slice A §6, as built)

- The day loop is `GameController.Update`'s; the manager alone never holds an election (`CheckElection` is the controller's), so a harness that does not run the controller never meets polling day.
- The Canvas screens (the menu, the selector, signing, election night) advance only inside `OnGUI`; under `-batchmode` the dry film's `DryGuiPass` delivers it.
- Every controller handler is private; the manager's verbs are public. The harness clicks Canvas buttons through their own `onClick` and runs an IMGUI control's body through reflection (`AuditHarness`'s summary says what "the same entry point" means).
- Real play is seeded from the clock; `-seed=` is read only by `SimulationTestRunner`, which must be skipped. The harness seeds `SimulationRandom` itself before the party row is clicked.
- Real seconds pace the days; the harness fixes `Time.captureDeltaTime`, so a run's day-to-frame mapping is the run's own. The seam's envelopes stay on `Time.unscaledTime` - real seconds per takeover.
- Every save path writes the player's real saves folder; the harness saves to its own folder through `SaveToPath` and loads through the menu's route (`_pendingLoadPath`).

## The run-time estimate (measured 2026-10-13, plans `cal1` and `cal2`)

- **A day of play costs about 27 ms** at 3x with one day a frame (Sweden, 300 days through its election, as M and as S: 26.9 and 27.4 ms a day); with five frames a day 91 ms, with twenty 320 ms - the frame, not the day, is the cost, so a long run takes one day a frame.
- **Each takeover adds about a second of wall clock**: the Canvas seam's envelopes are timed in real seconds (a signing, an election night, the selector).
- **Fixed costs**: a Unity launch about 25 s per plan; a run's start through the menu and the selector about 4 s.
- **So**: a run to the first election about 10 s; two cycles in Sweden about a minute; thirty years about 5 minutes plus its takeovers.
- **The matrix**: the full cross of country x role x outcome is some sixty reachable cells (Sweden most of them; Italy, the USA and France have few outcomes to reach), most of them minutes long - about two hours; the pairwise runs over start, style, length and interruptions some fifty more, mostly short, with a handful of thirty-year runs - about an hour and a half; area 10's films (a dry sweep of six countries at four geometries, then the Canvas surfaces filmed real at 1280 and 2560) under an hour. **Four to six hours of Unity time in all**, in plans of a few dozen runs each, through the one queue.

## Counts, per slice (as each slice reported them)

| slice | feature rows | specified but missing | present but unspecified | suspected discrepancies | exploit candidates |
|---|---|---|---|---|---|
| A | 38 | 19 | 8 | 31 (+16 determinism hazards) | — |
| B | 97 | 22 | 23 | 49 | 19 |
| C | 57 | 20 | 9 | 25 | — |
| D | 105 | (grouped) | (grouped) | 80 | (in §6) |
| E | 66 | 20 | (grouped) | 43 | 16 levers |
| F | 116 states | — | — | (in §5) | — |
