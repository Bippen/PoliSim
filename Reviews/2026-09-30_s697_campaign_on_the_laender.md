# Review — §697 PS-4, Germany's campaign on the Länder (2026-09-30)

Reviewed: the whole staged diff (17 files; `RangeCaptions.cs` was staged while the review ran and changes caption text only), plus `SimulationManager.cs` 220-270, 2520-2740, 2776-2777, 2874-2890, 2975-3030, 3080-3500; `WorldClock.cs` (staged and working tree); `CampaignClock.cs`; `CampaignRun.cs` 100-160, 400-918, 1020-1061, 1212; `CampaignAi.cs` 107-125, 520-760, 895-980; `LiveCampaignSetup.cs`; `PreCampaignRun.cs`; `GermanRegions.cs`; both German Land catalogs; `LiveCampaignSnapshot.cs` 20-300; `GameController.Campaign.cs` 150-250, 780-880; `GameController.cs` 6770-7110; `NationalElection.cs` 113-411; `ElectionNightFromModel.cs` 38-50; `PartySystem.cs` 386-397, 510-632; `SaveGameService.cs` 225-245; `PollingDayDiagnostic.cs` 40-70; `GermanRegionsDiagnostic.cs`; `bar_tier.ps1`'s money pattern. One independent reading, read-only. **Verdict NOT READY.**

## Defects

1. **The change that makes the item reachable in a game is unstaged.** `WorldClock.cs` (Germany's polling day, 2025-02-23) and `Tools/film_scope.tsv` (row `drys697`) are modified but not staged. In the staged tree `TryNextPollingDay` is Sweden-only, so `CurrentCampaignCalendar` returns null for Germany (`SimulationManager.cs:3140`) and `LiveCampaignSetup.Germany` is reached only from `GermanCampaignDiagnostic`. Committing the index as it stands, a German game opens no run-up, no campaign and no rail cell.
2. **Staging that `WorldClock.cs` as it stands fails the cheap bar.** `PollingDayDiagnostic.cs:53-56` asserts that Germany, Poland, Italy, the USA and France have no polling day from 2026-01-18 and no basis (`PollingDayBasis(other) == null`). The working-tree `PollingDayBasis` now returns a string for Germany. The polling-day half still passes, since 2026-01-18 is after the snap day.
3. **With that `WorldClock` change, a German game never drops its finished campaign.** `AdvanceCampaign` returns at `SimulationManager.cs:3161` whenever `CurrentCampaignCalendar()` is null, and the step that drops `PlayerCampaign`/`PlayerPreCampaign` once polling day has passed (`:3163-3169`) sits behind that return. After 2025-02-23 Germany has no next polling day, so `PlayerCampaign` stays non-null for the rest of the run. The rail keeps its CAMPAIGN cell (`GameController.Campaign.cs:242-243`), the HQ opens on the finished run, and every load re-steps 49 run-up and 56 campaign days (`:3446-3491`). Sweden always has a next polling day, so it never hits this. **Fix:** move the past-polling-day drop above the `!next.HasValue` return.

**Latent** (none fails today):
- **L1.** `NationalElection.cs:260`: `SharesFromCampaign` calls `DeriveRegional` without a date. The Germany branch at `:295` needs one, so a German election counted from a campaign derives no Länder. The prediction path does pass `on:`. This can't bite yet, because `ElectionNightFromModel.Available` is Sweden-only (`:38-39`).
- **L2.** Only local acts are gated by candidacy. The CSU's national acts reach the whole national audience of sixteen Länder (`CampaignRun.cs:799-801`). This is a modelling limit to state.
- **L3.** `CampaignRun.cs:811`: door-to-door with `RegionIndex −1` knocks in region 0 with no `StandsIn` check. Not reachable: the AI always names a region, and the HQ goes through the filtered `StrongestRegion`.
- **L4.** `TryCreatedDayZero` reads `CreatedParties.Of(Sweden)` only (`LiveCampaignSetup.cs:123`), and `Germany()` never calls it. Not reachable while party creation is offered only for Sweden (`PartyCreationFlow.cs:24`).
- **L5.** Docs and comments only:
  - `GameController.Campaign.cs:162-168`: the new `<summary>` was inserted after `OpenLiveCampaignMap`'s, so both now attach to `CampaignMapDrawn`.
  - Stale "Sweden only" text at `LiveCampaignSetup.cs:32-34`, `:140` and `PartyCreationFlow.cs:22`.
  - The new `issue_salience.md` section contradicts itself on the economic situation ("not added" to Economy, then "folds into" it). Its table is headed "Oct–Nov 2024", but Germany's fieldwork ran 10–31 October.

## Confirmed sound, stated

- **Sweden is byte-identical on every changed path.** In `BuildView`, `offices` is never null in `StepDay`. With `Stands` null, `StandsIn` is always true and each region is built with the same constructor arguments as before. The other paths:
  - `LargestRegions` builds the same full list and runs the same sort.
  - `MostPressuredRegions`, the defence reaction, `Begin`, the scripted skip, `StrongestRegion`, `NextOfficeRegion` and the run-up refusal each add a check that reads true for Sweden.
  - `OfficesFor`'s `Math.Min` is a no-op: 29 regions against at most 6 offices.
  - `PersonalityOf` resolves a Splinter's parent to the same `CampaignCasts.Of`.
  - `WithRecordShift` and `WithEntrants` both carry `Stands`, and the Editor's copies are Swedish harnesses with `Stands` null.
- **`FromWorldStart` returns the standard calendar for every Swedish epoch in use.** Its date arithmetic:

  | Case | Date | Why |
  |---|---|---|
  | Sweden's start | 2026-01-18 | the run-up's first day, so the early return |
  | Default epoch 2026-10-01 | run-up 2030-01-13 | before polling day 2030-09-08 |
  | Germany's run-up | 2024-11-10 | the standard run-up would open 2024-06-30, before the start; floor(53/7) = 7 weeks |

  `RestoreCampaign` reads `EpochDate` after `SetEpoch(save.EpochDate)` (`SaveGameService.cs:239` before `:244`).
- **Lengths and indexes line up.**
  - Cast, prior, loyalty, compatibility, the polling houses and the `Stands` rows are all `Keys(Germany).Length`, in the same key order.
  - The two Land catalogs list the Länder in one order.
  - `Eligible2021` and `Eligible2025` match the CSV and sum to 61,172,771 and 60,510,631.
  - The EB102 PDF's SHA-256 matches `SHA256SUMS.txt`.
- **The candidacy facts follow from the 2021 votes**, and the casts are as stated: CSU Professional, BSW Populist, SSW Professional with its one Land.
- **The diagnostic should pass as written.** The planted proof matches "Hamburg / general", and `EpochScope` restores the epoch.
- **The UI:** the map is Sweden-only, and the reopen path goes through the guarded `OpenLiveCampaignMap`.

**Money path:** yes by the project's definition (`bar_tier.ps1:37`), so a ledger row is owed. In substance, no:
- The two edited lines choose only how long the campaign's run-up is.
- Sweden's calendars are identical.
- War chests are the campaign's own constant, and nothing in the fiscal book reads the campaign.

## Applied after the review (the author's note - not the reviewer's words)

- **Defect 1** - `WorldClock.cs` and `Tools/film_scope.tsv` staged with the item.
- **Defect 2** - `PollingDayDiagnostic` reads Germany's polling day as it now stands: the snap's day from the start and on the day itself, none from
  2025-02-24, the basis citing [BWL-WT25]; Poland, Italy, the USA and France keep the old assertion.
- **Defect 3** - the fix the review names: the past-polling-day drop moved above the `!next.HasValue` return in `SimulationManager.AdvanceCampaign`
  (equivalent for Sweden: the day after a Swedish polling day falls outside the next window, and a stale run inside a new window was already
  replaced by the re-begin below). `GermanCampaignDiagnostic` (i) now steps a German game through polling day and holds that the finished run is
  dropped on 2025-02-24 with its result kept.
- **L5** - the two summaries in `GameController.Campaign.cs` re-ordered; the stale "Sweden only" text in `LiveCampaignSetup.cs` and
  `PartyCreationFlow.cs` corrected (creation stays Sweden's, and the comment says why); the salience note's two sentences made one and its
  fieldwork dated 10–31 October 2024.
- **L1-L4** stated in the record as limits; none reachable today.
