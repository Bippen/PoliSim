# The play protocol (CL-3, `COMPLETED.md` §523)

> **RULED 2026-09-22 (§579, CL-5 closed): THE GAME IS RIGHT AND THE PROTOCOL WAS WRONG.** The run-up opening on the game's own calendar and polling day at the election turn's boundary is D-23 (a) as ruled (7 May and 31 December 2029 when ruled; 4 February and 30 September 2030 since K-1 moved the game's start to 1 October 2026, §604); the 18 January and 13 September 2026 this protocol used to name were the REAL Swedish election's dates, not the played game's. The protocol now reads the game's own calendar, and `PlayProtocolStaging` cuts the save on it - the dates below are **read from the save, never typed**. `CampaignCalendar.Sweden2026` remains the real election's calendar and is no longer what the play is cut on. E-39 is UNBLOCKED; DS-7 (b) - polling day on the calendar's date - fires after the first play, as sequenced.

One play of Sweden's first election in the played game - its own polling day on the calendar's date since CL-4 (§619), the date `PlayProtocolStaging` prints every run - from the run-up's first day to election night, on a stated seed, with the twenty-line sheet beside it and the save as the record. **Two sittings, in this order (ruled 2026-09-29, `COMPLETED.md` §665): the first as S, the second as M.** This is the protocol S-C3 ruled (`COMPLETED.md §580`, §474); the play itself is Elias's.

## 1. The seed, stated

- **The world:** the seed world of this build (`WorldFactory.CreateDefault`), Sweden as the player, master seed **777** - the film harness's own, so a played run and a filmed one open on one world. The save carries the seed (`MasterSeed`) and the streams' draw counts, so a load resumes the same run.
- **The seed the play opens on: K-1, landed 2026-09-24** (`COMPLETED.md` §601-§606), from Valmyndigheten's final result fixed on 19 September 2026 (`ElectionsData/sweden/2026/returns_2026.md`):
  - The seated chamber is S 99, M 70, SD 62, V 30, C 25, KD 22, MP 22, L 19. The polls' prior is the 2026 shares, with loyalty from 2022 to 2026.
  - The coalition declarations are the 2026 election's, and C's line on V is a new one-way shape.
  - The game started on 1 October 2026 under K-1; **reversed** (`docs/specs/POLITICAL_SYSTEM_SPEC.md`, PS-1 §618): Sweden starts in the run-up to 13 September 2026, on the save's own day (§2 below).
  - The day-one government is the government of record at the start - Kristersson's M+KD+L with SD's support (`GovernmentRecord.AtStart`, §628). The 2026 chamber's formation under every ruled declaration and the ruled hold-out (K-1f, §607; K-1h and K-1g, §639) is what the game's own 13 September election reads (`WorldClock.VintageOfElection`); `Formation2026Diagnostic` prints it, and it was **none** under K-1f alone. **The first sitting is S** - New game → Sweden → 2026 → S, in opposition to the government of record - **and the second is M**, leading it (ruled §665, reversing §633's order). **The formateur half is reached as S**: the Speaker's round after the election asks the parties in the order the formation names them, and on the real result that order puts S first (§660). **M is asked only if the campaign's seats make the formation name M** - the second sitting reaches the formation sheet only on a count that puts M first. The two readings §607 left open - the hold-out and the strength of "refuses" - are ruled (K-1h, §639).
  - The electorate the vote model moves is still the 2022 fit, the model §601 tested out of sample. The party leaders are 2026's (K-1e, §608); no screen of the play names them - the live campaign seats its debaters by party.
  - A play made before K-1 was a play on the 2022 prior; one made on this save is a play on the 2026 seed, and the sheet says which on its first line.

## 2. The saves

Five saves in the game's saves directory (`%USERPROFILE%\AppData\LocalLow\DWELOP Games\Incumbent\saves` since §693, the studio name - the old `DefaultCompany\PoliSim\saves` is copied across once, on the first launch, and left in place), each cut by the tool that cuts it - never by hand. ⚠ Only the two sittings' saves are on the current format (re-staged by §665; the staging prints it). The three felt-verdict saves were cut on format 19 and are refused by this build until the film harness re-cuts them (`-shotsaves`); K-1 did not re-cut them.

| save | cut by | what it opens on |
|---|---|---|
| `playtest_4_precampaign_day1.json` | `PlayProtocolStaging.Run` | **the FIRST sitting (ruled §665): S in opposition** to the government of record (Kristersson's M+KD+L with SD's support), the game New game → Sweden → 2026 → S opens; seed 777, turn 0, the run-up's first day of the game's own election (the staging computes it as the live day path does and PRINTS the three dates every run); a clean book - nothing drafted |
| `playtest_4b_precampaign_day1_m.json` | `PlayProtocolStaging.Run` | **the SECOND sitting (§665): M leading the government of record** from day one - the arrival budget window opens on the first day tick and holds the clock until a budget bill is introduced (C-C2), beside the campaign hold; the same seed, day and clean book |
| `playtest_2_riksbank_rate_decision.json` | the film harness, `-shotsaves`, Sweden | felt verdict 2 ("still not independent"): a rate decision drafted on the Riksbank tab |
| `playtest_1_trade_bill_costs.json` | the film harness, `-shotsaves`, the USA | felt verdict 1: the Trade bill open with its costs on screen |
| `playtest_3_dense_midgame.json` | the film harness, `-shotsaves`, the USA | the dense mid-game: the budget-process pause, pending cabinet decisions, a foreign-policy meeting, one bill of every type |

`PlayProtocolCheck` (the cheap bar) cuts both sittings' saves to a temporary path every run (§665) and loads it back TWICE - into a bare manager, and, since 2026-09-21 (§557), through the controller's own load path, the one the Load button takes - and holds that the player has the sitting's party in the sitting's role - S in opposition, M governing (§558, §665). Until then it loaded the save into a manager only, and the game itself could not open it.

## 3. The play

1. **The first sitting, S:** New game → Sweden → 2026 → S - or load `playtest_4_precampaign_day1`, the same game staged. The Desk opens on the run-up's first day as the staging prints it. The formateur half is reached as S: after the count the Speaker asks the party the formation names first - S on the real result (§660) - and the formation sheet opens on the round (§647, §656). **The second sitting, M:** load `playtest_4b_precampaign_day1_m`. M governs from day one; it is asked to form a government only if the campaign's seats make the formation name M first.
2. Play the run-up (26 weeks) and the campaign (8 weeks) to polling day (the date the staging prints), and through election night. The eight verbs of the run-up, the campaign's actions, the stories that break and their answers, the debate - every decision goes through the HQ and is queued on the record.
3. Save at the end of the night (the slot save is enough; a named copy beside it keeps it).
4. Fill **`docs/play/PLAY_SHEET.md`** - one line per entry in its last column, the entry's "one thing to look for" as the question. Twenty lines, one each; a line may be "did not arise".
5. The record of the play is the save: `PlayRecordDump.Run -save=<the saved file>` prints every queued decision (day, kind, target, outlay), every story answer and the streams' counts as a table, and that table goes into the record verbatim.

## 4. What the sheet is for

`docs/play/PLAY_SHEET.md` is GENERATED (`PlaySheetGenerator`) from `COMPLETED.md` §346 and the code as it stands - the twenty constants are read by reflection at generation, and `PlaySheetCheck` fails the cheap bar the day one moves until the sheet is regenerated. Only the last column is written by hand, and a regeneration keeps it. A constant marked `(was N at §346)` has moved since the list was made; the sheet says which.

## 5. After the play

The felt verdicts (§P) and the twenty lines are Elias's; what they change is sheeted before it is built. CL-4 (polling day on the calendar's date inside the election turn, DS-7) is ruled to follow this play.
