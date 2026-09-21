# Review evidence — §544 (2026-09-21): the energy ministry, built and held, and build-and-retire re-founded

**What was reviewed.** The staged change that became `e44f41a` (P6-F2d) together with `c544620` (§539, build and retire), which had touched a money path three days earlier and had never been reviewed. Money paths in the change: `EnergyMarket.cs`, `EnergyLedger.cs`, and with them `EnergyFleet.cs` and `AiEnergyMinistry.cs`, which the money pattern names since §546.

**The form.** §524's rule asks for an adversarial review on a money path. The multi-agent form needs Elias's word, so this was one independent read-only reader on the change and a second independent read-only reader on the rework. Neither ran anything; both read the code paths end to end. The first found ten things, the first of them that the landed orders sat in one static table shared by every world the game holds. The second verified the rework and found nine more. What was fixed, and what was recorded and not fixed, is told in `COMPLETED.md` §544.

**Recovered, not retyped.** Both reports below are the reviewers' own final messages, taken from the session's subagent transcripts on 2026-09-21 when the review ledger was built (§546). Line numbers in the first report are the staged tree's BEFORE the rework; in the second, after it.

---

## The first reader - on the change and on c544620

*The reviewer's report, verbatim.*

Two defects can produce wrong figures in a played game today: the static fleet table is overwritten by the no-policy shadow world every turn, and the staged `FossilFleetScale` blows up on France's and Italy's coal. Nothing was run; every finding comes from reading the code paths end to end, and the working tree equals the index, so line numbers are the staged files'. Paths are relative to `G:\UNITY\Projects\PoliSim\`.

## Findings, most severe first

**1. The static delta table is shared with the ShadowBaseline worlds the game advances every turn. CONFIRMED.**
- **Where:** `Assets/Scripts/Simulation/EnergyFleet.cs:154-161, 199-210`, `Assets/Scripts/UI/GameController.cs:5614, 5620, 5628`, `Assets/Scripts/Simulation/ShadowBaseline.cs:139`.
- **Defect:** `_delta` is keyed by CountryId only. `GameController.AdvanceTurn` runs the impact-ledger forks, then the real turn, then `_shadowBaseline.AdvanceTurn()`. The shadow's own `EnergyFleet.Advance` always calls `RebuildCountry`, and its countries have no orders, so every live row is overwritten with zeros after each real turn.
- **Scenario:** the player's +wind lands at boundary N. The ledger at N is right, because `Advance` precedes it. For the whole following year every other reader sees the record fleet:
  - The Energy page's capacity bar and its cached clearing and book (`GameController.Energy.cs:63-64, 154, 427`) contradict the stored `EnergyHouseholdPrice`.
  - `EnergyPassThrough.PlannedForPreview` (`EnergyPassThrough.cs:46`) clears on the record fleet against the stored real price, so the preview shows a spurious rebound in inflation and electricity-tax revenue every turn.
  - `EnergyFleet.Place`'s retirement cap (`EnergyFleet.cs:99`) forgets landed retirements, so the same capacity can be retired twice; at the next rebuild `CapacityMw` goes negative.
- **The shadow is contaminated too:** its `BeginTurn` and its `EnvironmentFamily.AdvanceYear` read the player's rows before its own `Advance` zeroes them. For `PowerCo2PerCapita`, "with your policies" is computed on the record fleet and "without" on the player's fleet.
- **Other vectors:**
  - In play mode `AddComponent<SimulationManager>()` runs `Awake` → `WorldFactory.CreateDefault()` (`SimulationManager.cs:2664-2668`) → `EnergyFleet.Reset()` (`WorldFactory.cs:90`), so every first-touch fork (`PolicyImpactLedger.cs:142`) wipes the table just before the real turn.
  - Each fork's `Advance` also writes the fork's own subset of orders into the live rows.
  - `SaveGameService.RestoreInto` rebuilds nothing; only `GameController.cs:890` does.
- **Why no check caught it:** edit-mode diagnostics never call `Awake` and none hold two worlds with orders. If `Live` flips, the shadow's AI fleets and the real ones overwrite each other each turn.

**2. `FossilFleetScale` divides by label records that the code itself says do not hold the plants. Staged, money path. CONFIRMED.**
- **Where:** `Assets/Scripts/Simulation/EnergyMarket.cs:171, 186, 192`; the doc-comment at `:174-177` states the shortfall (France 19 MW and Italy 36 MW under "coal" against 0.5 and 2.1 GW of output).
- **Defect:** `CanOrder` passes for both (record > 0), and the +/− chips draw (`GameController.Energy.cs:155-161`).
- **Italy, one "+ coal" click:** step 1300 MW, `Scale` = 1336.1 / 36.1 = 37.0, so `MustRunFossilMw` = 1023.2 × 37.0 = 37,870 MW. That exceeds Italy's peak demand of 37,735 MW. Every block curtails and prices at the floor's cheapest tranche; coal `AnnualGwh` reaches about 332 TWh, `DerivedCo2Mt` explodes, and the wholesale price reaches bills, VAT receipts and the inflation pass-through.
- **France, one click:** step 1500 MW, `Scale` = 79.1, a 2.2 GW coal floor, and `PeakLevelMw` = 39.8 GW of "dependable" coal from a 1.5 GW order.
- **One "−" click:** capped at 19 or 36 MW, it takes `Scale` to 0 and removes the whole 0.5 or 2.1 GW of coal output.
- **Before the staged edit** the same order was a no-op (record dependable stays under the peak level); that is a c544620 defect but a harmless one.

**3. The preview never lands due orders. c544620. CONFIRMED.**
- **Where:** `Assets/Scripts/Simulation/SimulationManager.cs:3167` against `:2839-2840`; the clone at `:3543`.
- **Defect:** the boundary lands orders and then clears. `PreviewTurnOnClone` contains no `EnergyFleet` call and clears on the static table as it stands. The clone's `FleetOrders` is dead data.
- **Scenario:** in any turn whose boundary lands an order, `PreviewEnergyPassThroughPp` and `PreviewElectricityTaxRevenue` are computed on the old fleet while the turn plans on the new one. `PreviewParityDiagnostic` places no orders, so it cannot see this.

**4. Two readers inside the boundary run before `Advance`. c544620. CONFIRMED.**
- **Where:** `EnergyMarket.BeginTurn` at `SimulationManager.cs:2700`; `EnvironmentFamily.AdvanceYear` at `:2838` (it calls `PowerCo2PerHead`, which clears — `EnvironmentFamily.cs:140`); `Advance` is `:2839`.
- **Scenario:** Germany's coal leaves at boundary N. The ledger's prices at N are coal-free, but `State.PowerCo2PerCapita` written at N still carries the full coal fleet, and Sweden's water value at N is built from Germany's and Poland's pre-landing clearings. Both lag the ledger by a year; combined with finding 1, power CO₂ never sees player orders in the played game. "The fleet the year clears on is the one they made" is true of the ledger only.

**5. `Accumulate` does not scale nuclear, wind or solar, while `MustRun` does. c544620. CONFIRMED.**
- **Where:** `Assets/Scripts/Simulation/EnergyMarket.cs:434-437` against `:283`.
- **Scenario:** Germany's wind goes from 69 to 145 GW. Must-run wind is ×2.09 and displaces fossil, but `AnnualGwh[Wind]` stays at 2023's figure. `MixSharesNow` (`:681`), which feeds the Energy page's "Generation by technology" row and the Environment page's mix, shows wind's GWh unchanged and the total shrinking, so the "% FOSSIL" and wind shares are wrong. `Response()` reads the same array.

**6. `OnlineYear <= CalendarYear` is tested at 365-day boundaries that drift across leap years. CONFIRMED.**
- **Where:** `EnergyFleet.cs:159`; `SimulationManager.cs:216, 220, 2063-2067`.
- **Defect:** the epoch is 2026-01-01, so boundaries fall on 2027-01-01, 2028-01-01, 2028-12-31, 2029-12-31 and so on, labelled 2027, 2028, 2028, 2029.
- **Scenario:** an order placed mid-2026 with a 2-year lead lands at the second boundary and serves all of 2028. The same order placed in the turn after the 2028-12-31 boundary lands at the third boundary, 2031-12-31, while the page says 2031. A "one year's notice" retirement takes two boundaries. `Decide` runs twice with year = 2028. The ministry's "IN 2030" figures arrive on 2030-12-31.

**7. The `PeakBlockBears` veto is vacuous for Sweden and lags by one order elsewhere. CONFIRMED in logic; low impact on today's data.**
- **Where:** `Assets/Scripts/Simulation/AiEnergyMinistry.cs:237-247`.
- **Sweden:** `ClearSweden` builds its `BlockResult` without `DependableMw` (`EnergyMarket.cs:526`), so `dependable` is always 0. The verdict reduces to the sign of the zones' net peak residual: 22,076.0 − 22,108.4 = −32.4 MW, the unbalance the code calls "printed, never priced". It can never fire; a 33 MW data change would make it fire forever with a false sentence.
- **Elsewhere:** `excess` nets the queue, but the clearing is read before this boundary's landings, so each step is tested against a fleet still holding last year's pending retirement. The veto is also all-or-nothing, never partial.
- **Germany:** the ratio stays under 0.9 on the record, so this does not bite there.

**8. The diagnostic's zero-assertions accept any deferral as an excuse. PLAUSIBLE masking.**
- **Where:** `Assets/Editor/EnergyMinistryDiagnostic.cs:121, 127`.
- **Defect:** `deferred[c.Id].Count == 0` counts any label in any year, including `Place`'s "CANNOT BE ORDERED" lines. One unrelated early deferral lets coal stand above zero in 2039 without a failure.

**9. Readers "at the seed" still outside `RecordOnly`. Low; all are check or print readers, none is cached.**
- `EnergyMarket.cs:639` — `Response`'s second leg clears on the live fleet against an in-scope first leg.
- `EnergyMarket.cs:622-626` — `FittedParameters` reads `Caps(id)` outside the scope.
- `EnergyLedger.cs:127 → 267` — `Seed`'s `Compute` → `FossilCosts` reads live floors and caps.

**10. Germany's nuclear is orderable (record 4,205 MW) but `NuclearAvailability` is 0. c544620. CONFIRMED, low.**
- **Where:** `EnergyMarket.cs:140, 283`.
- **Scenario:** an order grows the capacity bar and produces nothing, and the row does not say so.

## Tried and could not break

- **A, inertness:** `record + 0.0` is exact, `x / x` is exactly 1, `× 1.0` is exact, and the addition order in `MustRun` is unchanged. Oil returns 1.0 and a zero record returns 1.0. Sweden's zones use the national scale. `Utilisation` on the record is identical at zero delta. `RecordOnly` cannot change a result at zero delta, and no calibration's evaluation order moved.
- **B, the scope:** nesting (`ClearAtSeed` → `WaterValueAtSeed` → `Adders`, depth 3), early `return` inside `using`, and exceptions all unwind. `FitAdders` writes the cache inside the scope. Every cached or saved seed (`AdderCache`, `SeedSpreadCache`, `_waterValueSeed`, `RetailMargin`, `RetailWholesaleSeed`, `PowerResidualMt`, `EnergyCongestionRentSeed`) comes from an in-scope clearing.
- **C, indices:** `Scale(id, 2/4/5)` are the nuclear, wind and solar labels against `level[3/5/6]`. Coal and gas categories map to labels 0 and 1. Oil ↔ "other" ↔ 1.0 is consistent. `zone[2]` is the peak block, and the diagnostic's and the page's labels are right.
- **D, retirement arithmetic:** no double landing (the `Landed` flag persists and `Rebuild` sums landed only). `Withdraw` refuses a landed order. `Scale` is clamped and has no NaN path. The ministry diagnostic resets the table in `finally`, and no other check lands orders.
- **E, `Decide`:** `PathAt` handles a first point before 2023, a year exactly on a point, duplicate years and years after the last point. `Build` and the share branch cannot produce a negative order. The GWh → MW formula is dimensionally right. `Clear` inside the veto mutates only record-scoped caches. `EndTurn` has no runtime caller, so the between-turn water value is the last `BeginTurn`'s, but it touches prices only, not residuals.
- **F, save/load and the preview:** `Order`, `Reason` and `FleetOrders` round-trip under the save settings (public fields, `TypeNameHandling.None`, reference preservation). The preview never calls `Advance` or `RebuildCountry`, so the clone cannot write the live row; the real gap is finding 3.
- **G, UI:** `MandateOf` is non-null for all six countries, the row height matches the drawn lines, the label indices are right and the formatting is invariant-culture.

---

## The second reader - on the rework

*The reviewer's report, verbatim.*

Verdict: the rework holds up. No original finding is NOT FIXED: 1, 3, 4, 6, 8 and 9/10 are FIXED, and 2, 5 and 7 are PARTLY. Everything below is from reading the staged code and hand arithmetic on `EnergyLayerData`; I did not run Unity or any check. Paths are under `G:\UNITY\Projects\PoliSim\`.

On (a), I found no runtime id-keyed fleet read for a played country outside a `For` scope:
- **Market callers:** `BeginTurn` opens `Clear(de)` and `Clear(pl)`, `PowerCo2PerHead` and `MixSharesNow` go through `Clear(country)`, and the veto clears inside its own scope.
- **Ledger and preview:** `EnergyLedger.AdvanceYear` runs `Clear` then `Compute`, `FossilCosts` sits inside `ComputeOn`, and `PlannedForPreview` clears the clone.
- **Page and lambdas:** the page's reads at `GameController.Energy.cs:154/201/210/427` sit inside `DrawEnergyTab`'s `using`; the `drawExtraRow` lambdas run synchronously (`GameController.Health.cs:337`).
- **Sweden and the ministry:** `ClearSweden` clears no other country except `WaterValueAtSeed`, which is inside `RecordOnly`. The ministry's `FleetWithQueueMw` and `RenewableSharePercent` take the Country.

On (b), every `For` is opened with `using`, so an `ExitGUIException` unwinds it and nothing stays open across a frame. There are no threads, and nesting is always last-in-first-out.

## Findings

1. **`Assets/Scripts/UI/GameController.Energy.cs:58-64` — CONFIRMED.**
   - The page's cached clearing, book and peer ticks are keyed on turn and country id only. `RestoreFromSave` (`Assets/Scripts/UI/GameController.cs:885-898`) resets the preview cache but never this one.
   - Scenario: Germany at turn 5 with a landed coal retirement, page viewed, then a turn-5 Germany save without that retirement is loaded. The price stack, rule row, zones and peer ticks show the pre-load world, while the capacity bar and `MixSharesNow` (not cached, `:427/:431`) show the loaded one.
   - The cache predates the rework. It is the (c) case: world A's clearing served to world B.

2. **`Assets/Scripts/Simulation/EnergyMarket.cs:138,145,448-457` — CONFIRMED by reading; predates the rework.**
   - `_waterValue` and `_swedenDeficitShare` are process statics written by whichever world last ran `BeginTurn`. The game never calls `EndTurn`, and `_shadowBaseline.AdvanceTurn()` runs after the real turn (`GameController.cs:5619` then `:5627`).
   - Scenario: for the whole following turn, Sweden's page clearing, Sweden's peer tick and the Sweden preview (`Assets/Scripts/Simulation/EnergyPassThrough.cs:46`) use the shadow world's Germany/Poland prices. After a load they use the pre-load world's.
   - With the ministry HELD the fleet effect is limited to a German or Polish player: Sweden's peer tick omits their landed orders. It grows when `Live` flips.

3. **`Assets/Scripts/Simulation/EnergyFleet.cs:167-189` with `EnergyMarket.cs:174,189,195` — PLAUSIBLE; needs a save from the HEAD build.**
   - Nothing on load, in `Advance`, or in `LandedMw` ignores orders on labels that are now refused.
   - HEAD's `CanOrder` was only `record > 0`, so France/Italy coal chips were live. The new `FossilFleetScale` then leverages such an order.
   - Italy with one +1 300 MW step: scale (36.1+1300)/36.1 = 37, so the coal must-run floor goes from 1 023 MW to 37.9 GW against a base demand of 16.2 GW.
   - France with one +1 500 MW step: scale 79, floor 27.9 MW to 2.2 GW, dependable 0.5 GW to 39.8 GW.
   - A pending legacy order lands by the year rule with no `CanOrder` check.

4. **`EnergyFleet.cs:80-88,93-98` — CONFIRMED.**
   - Sweden's coal and gas pass the new refusal (dependable 58.7 and 14.5 MW against 2023 peak levels of 0 and 0.3 MW). One step is 500 MW against a record of 69 and 17 MW, which is 725 % and 2 941 %.
   - Those are the only orderable labels where a step exceeds 50 % of the record; every other orderable label is at or below 14 %. France/Italy coal (7 800 % and 3 600 %) and German nuclear (62 %) are refused.
   - The order is inert. `ClearSweden` discards its fossil costs and caps (`EnergyMarket.cs:563-565`), so price and CO₂ do not move. Only the capacity bar, the mandate line and the veto's "dependable" total change.
   - That is the same ground on which German nuclear is refused ("A BUILT MW WOULD NOT RUN"). It also means the ministry's FossilFree retirements for Sweden change nothing in any clearing or CO₂ figure.

5. **`Assets/Scripts/Simulation/AiEnergyMinistry.cs:250-265` — CONFIRMED arithmetic; latent.**
   - The veto sums `DependableMw`, which includes the fossil floors, and compares it to `ResidualMw`, which is already net of those floors (`EnergyMarket.cs:316`). Adequacy in `ClearBlock` is the residual against the flexible capacity (`:238,264`).
   - Germany at the seed: the veto compares 73.8 GW with 19.7 GW; the real headroom is 62.7 GW against 19.7 GW. The veto is lenient by the floor sum, 11.1 GW.
   - It also ignores that retiring coal shrinks its scaled floor, which raises the residual by 0.163 MW per MW retired.
   - Pending retirements of other labels are not deducted. Sweden's coal is queued in the same `Decide` just before its gas is checked.
   - It binds in no current mandate. Germany's coal exit passes under either form.

6. **`EnergyMarket.cs:439-442` — CONFIRMED.**
   - `Accumulate` now scales nuclear, wind and solar but does not net out `CurtailedMw`.
   - One German wind step (+2 600 MW, about +477 MW in the base block) moves the base residual from +279 MW to −198 MW, and the full output is still counted.
   - On the ministry's 2030 path the mid block curtails about 12 GW over 7 008 hours, roughly 80 TWh/yr booked as generated. `MixSharesNow` then overstates wind and solar, and total generation exceeds demand.

7. **`GameController.cs:4307-4322` — CONFIRMED; low severity.**
   - The preview cache is fingerprinted on turn, interest rate and budget draft only, so placing an order does not invalidate it.
   - A retirement waits one boundary and lands at the coming one. The cached preview's inflation and pass-through stay stale until a slider moves or the turn ends.
   - Builds are unaffected, since they wait at least two boundaries.

8. **`Assets/Scripts/Simulation/PolicyImpactLedger.cs:141-147` and `ShadowBaseline.cs:96-104` — CONFIRMED by reading; a design gap, not introduced by the rework.**
   - `Place` writes only to the real Country. A fork made before an order never receives it; a fork made after does, through the save round-trip.
   - The fleet's effect on `EnergyHouseholdPrice`, inflation and `PowerCo2` is therefore booked to every family forked earlier, and negatively to "interaction".

9. **Trivia.**
   - `EnergyMinistryDiagnostic.cs:177` accepts a GAS deferral as the excuse for standing coal, and also accepts "CANNOT BE ORDERED" lines as excuses.
   - The second leg of `Response()` (`EnergyMarket.cs:644`) and `FittedParameters()` (`:631`) are not inside `RecordOnly`. Only checks call them today, and none of those run inside a scope.
   - The doc at `Assets/Scripts/Data/Country.cs:757-759` still says "rebuilt … on load".
   - Printed years run one early for orders placed in the first N days of a turn, where N is the number of leap days elapsed. The first case is 2028-12-31.

## Original findings

1. **FIXED.** No fleet state leaks across worlds. Two cross-world statics remain beside it (findings 1 and 2).
2. **PARTLY.**
   - Both signs are refused at the single choke point, `Place` (`EnergyFleet.cs:107`). The chips are disabled with `!can` on both, and the film driver and the ministry go through `Place`.
   - The label-to-category pairs are correct.
   - Legacy saves are unguarded (finding 3), and Sweden's coal and gas are leveraged but inert (finding 4).
3. **FIXED.** The clone deep-copies the queue (`SimulationManager.cs:3546`). `Advance` flips only the clone's orders. All four preview entry points go through `ClonePreviewCountry`, and nothing clears earlier in the preview. Caveat: finding 7.
4. **FIXED.** Turn arithmetic:
   - A player order placed in turn t lands at the w-th boundary.
   - The ministry's `Decide` runs before `CurrentTurn++` (`:2752` against `:2774`) with turn t+1 and also lands at the w-th boundary.
   - The diagnostic's clock matches (start year 2026 equals `PensionAgeStatute.SeedYear`).
   - The wait is always at least 1, so `OnlineTurn` is at least 1 and a turn-0 order can never fall into the year rule.
   - `CurrentTurn` is restored on load (`:2540`), and nothing clears before line `:2702`.
5. **PARTLY.** The scaling is consistent, including Sweden's zones at the national scale, and is exactly 1.0 on the record. Curtailment is not netted (finding 6).
6. **FIXED** as claimed. Landing is by turn and printed years are nominal and plausible, apart from the drift-day trivia above.
7. **PARTLY.**
   - The signs, `perMw`, the `MaxValue` path and partial retirement are all correct, and a retirement cannot exceed the fleet.
   - The floor mismatch and the cross-label gap remain (finding 5).
   - For Sweden the veto still never binds. The peak residual sums to −32 MW and is clamped to 0, so the spare is always at least the fleet. That is now a fact of the data rather than the structure.
   - The forever-deferral for a refused label is unreachable today, because every mandated label is orderable. Veto deferrals do repeat every year, by design.
8. **FIXED.** The strings match: "`…: COAL -N MW DEFERRED`" and "`…: COAL DEFERRED - CANNOT…`" both contain "`: COAL `". The looseness is in trivia above.
9/10. **FIXED** at runtime. German nuclear is refused and the diagnostic asserts it.

## `FleetIsTheCountrys`

- **Would it fail on the old table?** Yes. The shared row makes `mustRunA == mustRunB`, and B's `Advance` zeroes A's row, so `afterA != mustRunA`.
- **Static state left dirty?** None.
   - The ambient stack and the record depth are both back at zero when it returns.
   - `Adders`, `SeedSpread` and `WaterValueAtSeed` are first computed inside `For(deA)` with +10 GW of wind landed, and `RecordOnly` correctly forces them to the record (`EnergyMarket.cs:573, 422, 483`).
   - `ResetTurnState` runs in the `finally`.
- **What it does not cover:** the page cache (finding 1) and `_waterValue` (finding 2).
