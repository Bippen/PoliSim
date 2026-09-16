# The play protocol (CL-3, `COMPLETED.md` §523)

One play of Sweden's 2026 election from the run-up's first day to election night, on a stated seed, with the twenty-line sheet beside it and the save as the record. This is the protocol S-C3 ruled (`POLISIM_BACKLOG_PLAN.md`, §474); the play itself is Elias's.

## 1. The seed, stated

- **The world:** the seed world of this build (`WorldFactory.CreateDefault`), Sweden as the player, master seed **777** - the film harness's own, so a played run and a filmed one open on one world. The save carries the seed (`MasterSeed`) and the streams' draw counts, so a load resumes the same run.
- **The prior the polls read:** Valmyndigheten's final 2022 count (`ElectionsData/sweden/returns_2022.md`) - the 2022 prior, until **K-1** refreshes it. K-1 is the seed refresh from Sweden's real 2026 result; on 16 September 2026 at 18:03 Valmyndigheten's feed (`resultat.val.se/data/resultat/val2026/RD_S.json`) reported the FINAL count 27 % in - 1 815 of 6 626 districts, 1 842 259 of 8 051 355 entitled - so the refresh lands when the count completes, and this protocol states the 2022 prior until it does. A play made before K-1 is a play on the 2022 prior, and the sheet says so on its first line.

## 2. The saves

Four saves in the game's saves directory (`%USERPROFILE%\AppData\LocalLow\DefaultCompany\PoliSim\saves`), every one cut on the current save format by the tools that cut them - never by hand:

| save | cut by | what it opens on |
|---|---|---|
| `playtest_4_precampaign_day1.json` | `PlayProtocolStaging.Run` | **the play's opening**: 18 January 2026, the first day of the 26-week run-up (`CampaignCalendar.Sweden2026.PreCampaignStart`), Sweden, a clean book - nothing drafted |
| `playtest_2_riksbank_rate_decision.json` | the film harness, `-shotsaves`, Sweden | felt verdict 2 ("still not independent"): a rate decision drafted on the Riksbank tab |
| `playtest_1_trade_bill_costs.json` | the film harness, `-shotsaves`, the USA | felt verdict 1: the Trade bill open with its costs on screen |
| `playtest_3_dense_midgame.json` | the film harness, `-shotsaves`, the USA | the dense mid-game: the budget-process pause, pending cabinet decisions, a foreign-policy meeting, one bill of every type |

`PlayProtocolCheck` (the cheap bar) cuts the first to a temporary path and loads it back every run, so a save this protocol names cannot silently stop loading.

## 3. The play

1. Load `playtest_4_precampaign_day1`. The Desk opens on 18 January 2026; the rail's CAMPAIGN cell reads the run-up.
2. Play the run-up (26 weeks) and the campaign (8 weeks) to polling day, 13 September 2026, and through election night. The eight verbs of the run-up, the campaign's actions, the stories that break and their answers, the debate - every decision goes through the HQ and is queued on the record.
3. Save at the end of the night (the slot save is enough; a named copy beside it keeps it).
4. Fill **`PLAY_SHEET.md`** - one line per entry in its last column, the entry's "one thing to look for" as the question. Twenty lines, one each; a line may be "did not arise".
5. The record of the play is the save: `PlayRecordDump.Run -save=<the saved file>` prints every queued decision (day, kind, target, outlay), every story answer and the streams' counts as a table, and that table goes into the record verbatim.

## 4. What the sheet is for

`PLAY_SHEET.md` is GENERATED (`PlaySheetGenerator`) from `COMPLETED.md` §346 and the code as it stands - the twenty constants are read by reflection at generation, and `PlaySheetCheck` fails the cheap bar the day one moves until the sheet is regenerated. Only the last column is written by hand, and a regeneration keeps it. A constant marked `(was N at §346)` has moved since the list was made; the sheet says which.

## 5. After the play

The felt verdicts (§P) and the twenty lines are Elias's; what they change is sheeted before it is built. CL-4 (polling day on the calendar's date inside the election turn, DS-7) is ruled to follow this play.
