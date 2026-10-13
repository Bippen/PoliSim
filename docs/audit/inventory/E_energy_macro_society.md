# Gameplay audit, Phase 0 inventory - slice E: energy, macro, and the six society-stat families

Repo `G:\UNITY\Projects\PoliSim`, HEAD `92114481` (s803), read 2026-10-13. Read-only: nothing in the repo was edited, built or run.

**Method.** Energy, macro, monetary, trade and sectors were read directly. The six families, the statistics/publication layer and the demography/labour block were each read by a read-only sub-inventory under the same citation rules; their key claims were spot-checked against the code (the effectiveness capture `SimulationManager.cs:6252-6261`, the budget bill's pass-day application `:1548-1567`, minister efficiencies `CabinetSystem.cs:424-457`, `CohortDemographics.cs:109-201`, the projection clamp `PopulationProjections.cs:545-552`, `WorldClock.StartDate` `:223-231`, the Q2 reference period `ReleaseCalendar.cs:113-116`, `StatHistory.cs:56-77`, `DerivedStats.cs:143-152`). Their INFERRED marks are kept.

**Path keys.** `Sim/` = `Assets/Scripts/Simulation`, `Data/` = `Assets/Scripts/Data`, `UI/` = `Assets/Scripts/UI`, `Ed/` = `Assets/Editor`. Bare file names are under those folders (SimulationManager.cs is `Sim/`, GameController*.cs are `UI/`, the families are `Data/`). `§n` = `COMPLETED.md` section n. FL = `POLISIM_FEATURE_LIST.md`. SPEC = `docs/specs/ENERGY_SPEC.md`. SPINE = `docs/data/ENERGY_LAYER_SPINE.md`. CAT = `docs/data/SOCIETY_STATS.md`. MR = `docs/reference/MODEL_REFERENCE.md`.

---

## 0. Read this first

- **The six families** are the catalog's: health (P5-C2), education (C3), infrastructure (C4), environment (C5), immigration and poverty depth (C6) and department effectiveness (C7, "KEEP as a mechanic") - CAT Part I opening paragraph and the closing order line ("C2 health … → C7 effectiveness"). The doc's own title says "five family spines" (the data families). Other "six" counts exist: §531 counts the cohort substrate as the sixth plate; the boundary checkpoint calls five families plus the energy ledger "the six yearly readouts" (`SimulationManager.cs:4971-4976`). Crime is ruled N/A as a family (CAT §7) - its couplings are listed separately (EM-66).
- **Energy supply = demand + losses holds per block, by construction** for the five single-zone countries; losses are not modelled - they are folded into the static 2023 demand constants (EnergyLayerData.cs:271; SPINE §3). Sweden's losses fall into `ChainUnbalanceMw`. The annual figures do NOT net curtailment (recorded §544 (a)), and Sweden's `CurtailedMw` carries exports. Exact fields in 4.1.
- **The energy layer's load, retail consumption and the trade volumes never grow** (no load growth: an ABSENT row on the page; trade T-4 open). Over 30 years their shares of GDP drift with GDP.
- **Fleet orders are free** (CAPEX billed, FL E-44) and land without a vote. Where a source publishes no queue line, any size stands (FR/IT gas and nuclear, DE coal and gas). Retiring the fossil fleet drives blocks to the 4 000 EUR/MWh ceiling with no reliability veto for the player, and unserved load has no consequence beyond the price.
- **SUSPECTED HIGH: department effectiveness cannot see the player's own budget.** The player's spending lands the day a budget bill passes, so the next boundary's "requested" figure already includes it. Allocated = requested for the player's country every year, so the health waits move only with the minister. And an empty chair (efficiency 1) beats every candidate (0.55-0.90). SD-30, SD-31.
- **Statistics show only live values.** The published view was cut (§62, ruling E-5); the publication layer runs on but only the crime bulletin line shows a published figure. Suspected calendar defects: the Q2 GDP reference period starts 31 March, publication runs before the day's step, and the EU inflation lag runs opposite to its comment (SD-40 to SD-42).
- **Demography.** Per-country epochs before 2024 freeze the pyramid (Italy two years, Poland one). A held immigration lever compounds (about +16 % population at 30 years at the dial's top, INFERRED). Mid-year lever changes are retroactive. Two population figures coexist (SD-20 to SD-23).
- **Exploit leads for Phase 1** (section 7):
  - the costless regulation and ownership dials, worth up to +0.5 pp of potential growth a year;
  - the inflation-target law, which moves the approval and poverty reference with it;
  - the USA's base tariff, which draws no retaliation (T-5 open);
  - empty ministers' chairs;
  - free capacity orders.
- **NaN/Inf.** Every division read is guarded, but `Math.Max(0.0001f, NaN)` passes NaN through. A NaN price level in Germany or Poland therefore THROWS in `EnergyMarket.WaterValueOf` at the top of every turn. The runner's `CheckFinite` is a 52-field hand list that misses every energy and family field, although CLAUDE.md:31 says it covers every float.
- **Tick time.** Energy is yearly and bounded. In the played game a boundary advances up to 13 worlds: the real one, the shadow, and up to eleven except-worlds. That cost is unmeasured. The cohort day allocates about 13-17 small arrays per country per day (INFERRED: 100-130 MB of garbage over 30 years). Per frame, the UI re-runs a full market clearing (`MixSharesNow`).

---

## 1. Feature table

Status vocabulary as asked: BUILT, PARTIAL, NOT BUILT (planned in a spec/row), SPEC-ONLY (specified as built/required but absent), CODE-ONLY (in code, named by no spec/row/§ found). HELD marks built code switched off by ruling.

### 1a. Energy

| ID | feature | player-facing entry (screen/control -> method) | code (file:symbol) | spec authority | status | countries | notes |
|---|---|---|---|---|---|---|---|
| EM-01 | Energy catalog and physical readouts: 2023 fleet by 7 labels (record vs Ember), load folds, Sweden's 4 zones and 6 links, combustion CO2, ECB 2023 bridges | read-only (Energy page plates) | `Data/EnergyLayer.cs` (pure functions, :25-320); `Data/Generated/EnergyLayerData.cs` (by `Ed/EnergyCatalogGenerator.cs` from `EnergyData/`) | SPEC II S1-S5, S15; SPINE §1-§4, §11; §455, §457 | BUILT | all six | `EnergyLayer.Flows/ZoneSurplusMw/DerivedMix/Decomposition` have Editor callers only |
| EM-02 | Load blocks (base/mid/peak, P90 cut) | read-only | `EnergyLayerData.DispatchHours`, `DispatchDemandMw` (:271-272, "consumption + net export + losses + pumping") | SPEC II §3.1, S3 (P90 [AUTHORED-DRAFT]); SPINE §3 | BUILT (static) | all six | 2023 load forever (EM-25) |
| EM-03 | Winter-peak adequacy test / reserve margin | none | `EnergyLayerData.MaxMw` read only by `EnergyLayer.Blocks` (:157) and Editor dumps; only the HELD ministry's veto reads a peak residual (`AiEnergyMinistry.cs:257`) | SPEC I §2 ("installed != dependable ... the reserve margin"), II §3.1 ("the adequacy test compares it to dependable capacity") | SPEC-ONLY | - | scarcity reads the peak BLOCK mean, not the maximum hour |
| EM-04 | Merit-order clearing per block (single-zone countries): 200 tranches +/-25 %, must-run (nuclear, hydro, wind, solar, firm, fossil trough floors), scarcity onset 0.9, curtailment offer 0, ACER ceiling 4 000 | Energy page rule row (read-only) | `EnergyMarket.ClearBlock` (:240-286), `MustRun` (:288-294), `ClearCountryBlock` (:319-333), `ClearAtCore` (:353-397), constants (:51-65) | SPEC II S6; §459 | BUILT | DE FR IT PL US | 10 FITTED adders (`FitAdders` :596-629; gate 7 `Ed/EnergyLayerCheck.cs:178-201`). Convention :324-331: a block with a running fossil floor is priced at the floor's cheapest tranche, so curtailment never lowers the price (§544 failure mode 3, recorded) |
| EM-05 | Sweden's zones SE1-SE4 on the chain against dated NTCs; binding splits prices; rent | Energy zones row, links in the gutters | `EnergyMarket.ClearSweden` (:521-586); `EnergyLayer.LinkCapacityMw` (:228-235); NTCs `LinkNtcMw` (:144-145) | S1, S15; SPINE §4; §457, §459, §466 | BUILT | SE | `CurtailedMw = max(0, surplus)` (:574) records a zone's EXPORT as curtailment (§544 (a)); `ChainUnbalanceMw` (:573) never priced |
| EM-06 | Sweden's water value: DE and PL block prices each over its own price level, weighted 615/600 MW; per-zone beta; zone seed prices from the exchange | water plate (read-only) | `EnergyMarket.BeginTurn` (:455-468), `WaterValueOf` (:488-497; THROWS if a neighbour's index is not > 0, :491), own price (:542) | §466 (EN-3b), §552 (FT-10 P-B) | BUILT | SE (reads DE, PL) | process-static `_waterValue` (:147), re-set for the played world after the shadow's turn (`GameController.cs:6130`) and after a load (`:905`); structural fix open (§544 (c)); `EndTurn()` (:506) has no caller |
| EM-07 | Reservoir balance (SE) and the hydro shift (IT, DE) | read-only | `EnergyMarket.AdvanceReservoir` (:471-481, from `EnvironmentFamily.cs:146`); shift bisection (:369-396) | §466 | PARTIAL | SE; IT, DE | in play inflow == use (`ProbeInflowScale` = 1 unless a diagnostic sets it, :142), so `HydroReservoirBalanceGwh` stays 0 and the deficit term is inert; slope [AUTHORED-DRAFT] (:131); seed fill unsourced; LEVER_MAP §2 lists the field UNREACHED |
| EM-08 | ETS price in the fleet's variable cost | none | `EnergyLayerData.EtsPerT` (:460-461: 85.51, USA 0); `EnergyMarket.CostParts` (:218-226, carried by the price level); probe `ProbeEtsRisePerT` (:144) | S5 as re-read by EN-4d; §467 | BUILT (exogenous level); path NOT BUILT | EU five | no player lever reaches the fleet's carbon price |
| EM-09 | Power CO2 per head from the dispatch (+ seed residual, over the SEED population) | People Environment tile | `EnergyMarket.SeedResidual` (:683-691), `PowerCo2PerHead` (:699-705); writer `EnvironmentFamily.cs:145` (clamp [0.02, 40], :71) | S4; §460 | BUILT | all six | LEVER_MAP §2 lists it UNREACHED (Sweden probe; the map predates the fleet) |
| EM-10 | Retail stack per class (households, non-households): wholesale + FITTED margin + network + levy + env tax + VAT, book USD | Energy Overview "Prices" cards | `EnergyLedger.ComputeOn` (:360-437), `FitMargins` (:257-276), `Write` (:546-563) | S7; §463 | BUILT | all six (US components BILLED) | THREE UNITS: book USD (`BookCurrency` :70), market EUR (USD for the USA), tax national (`EnergyLayer.CurrencyCode` :100-109); the electricity-tax statute is EUR/MWh even in SE/PL |
| EM-11 | Two ledgers: system cost and incidence, closing to the unit | Energy plate 3 bridges | `EnergyLedger.Book` (:87-126), `Gap` (:110), `FossilCosts` (:523-544) | S7; §463 | BUILT | all six | the Book is not stored; 8 figures reach the state (`Write` :546-563) |
| EM-12 | Support scheme and levy rule (K = seed line / seed levy; Annex II support share) | Energy Policy "Retail intervention" caption; instruments plate LevyScale (`GameController.Energy.cs:290`) | `EnergyLedger.LevyScaleWith` (:331-339), `LevyScaleAtDial` (:347-358), shares (:164-173: FR 76.29 %, SE 36.0 %, IT 3.47 %, PL 97.95 %), probe `SupportShareOverride` (:187) | §545, §552, §553, §571/§572 | BUILT | levy FR SE IT PL DE; none USA (`HasPolicyLevy` :497-503); DE has no energy line | the sentinel's 4 recorded cells are this rule's bounded answer (`Ed/TrajectorySentinelCheck.cs:160-168`) |
| EM-13 | Congestion rent and its credit to next year's network tariff | read-only | `EnergyLedger.CreditFor` (:249-254, rent ABOVE the seed's carried by P), `ComputeOn` (:399, :435); rent `ClearSweden` (:575-581) | §463 (iv), §466 | BUILT | SE | rent is positive at the seed since EN-3b (zone prices differ; asserted `Ed/EnergyLedgerDiagnostic.cs:198-199`); credit ~0 at no policy. The class doc's "zero where no block binds, as at the seed" (EnergyLedger.cs:33-35) is stale |
| EM-14 | HICP pass-through of the household price's REAL change into inflation (a level term) | none | `EnergyPassThrough.Planned` (:32-36) -> `SimulationManager.cs:5073` -> daily Phillips (:459); preview `PlannedForPreview` (:42-51) | S8; §465 | BUILT | all six | |
| EM-15 | Industry bill share change -> BusinessConfidence | none | `MacroSystem.cs:2536`; `EnergyConfidenceSensitivity` 0.0015 (:2434) | §463 (v) | BUILT | all six | static load: the share falls every year at no policy, a tiny standing upward push |
| EM-16 | BUILD AND RETIRE: fleet orders by technology, EIA lead times, 1-year retirement notice, landing at boundaries | Energy > Overview > Power plants tile: "-" `GameController.EnergyV35.cs:543` -> `EnergyFleet.Place(c, k, -step, c.CalendarYear, CurrentTurn)`; "+" `:546` -> `Place(c, k, EnergyConnectionQueue.StepUpMw(...), ...)`; step = 1 % of the seed fleet, >= 100 MW (`EnergyFleet.StepMw` :103-108) | `EnergyFleet.Place` (:115-133), `CannotOrderWhy` (:72-91), `Advance` (:189-200; called `SimulationManager.cs:4819`), `CapacityMw(Country,int)` (:214); state `Country.FleetOrders` (`Country.cs:787`, saved) | §539 (P6-F2), §544 | BUILT; prices nothing (`CapexBill` :63; FL E-44) | wind/solar where the record runs them; coal/gas refused in SE (`EnergyMarket.ClearsFossilFleet` :154) and FR/IT coal; DE nuclear refused; hydro/other never | role gate in the UI only (EnergyV35 :180); `Place` checks no role. No UI calls `EnergyFleet.Withdraw` (:147): a queued build cannot be cancelled. Orders never pruned |
| EM-17 | Connection-queue capacity: operator queue (stock), statute (DE EEG §28/§28a, per calendar year), or BILLED (any size) | "+" disabled when full; slip sentences | `EnergyConnectionQueue` catalog (:141-196), `TakenMw` (:215-226), `RoomMw` (:229-233), `RefusalFor` (:241-259), `StepUpMw` (:274-278) | §551 (P6-F2e), §575 | BUILT | operator SE FR IT PL US; statute DE; no line: FR/IT gas+nuclear, DE coal+gas | past a statute's last named year its last volume stands by CONVENTION (:110-120) |
| EM-18 | Four instruments as dials: Retail intervention (Energy Subsidy), Market liberalisation (Energy Regulation), Investment planning (Energy Tax Credits), State ownership (Energy Nationalization/Deregulation); Research Grants descriptive | Energy > Policy: `GameController.EnergyV35.cs:360/368, 376, 396, 405` (0-100) -> drafts `_sector*Inputs[Energy]` -> "Introduce sectors bill" `:418-419` -> `SimulationManager.IntroduceSectorBill` (:2172) -> vote -> `ApplySectorPolicyChanges` (:5960) | `EnergyLedger.LiberalisationGap` (:441-448), `LiberalisationShiftPerKwh` (:456-465); `SectorCouplings.EnergySupportCostTarget` | S9; §498 (EN-7a), §542, §748 | PARTIAL | all six | Investment planning and State ownership do not reach the layer (dial captions EnergyV35 :392, :403); SPEC I §3.6's combinations are not representable |
| EM-19 | Electricity-tax law category (10 laws, statute per class, EUR/MWh) | Energy > Policy laws chip (`GameController.Energy.cs` `DrawEnergyLawsLink` :101-) -> Laws board -> `IntroduceLawBill` (`SimulationManager.cs:1863`; competence gate :1868) | `LawCatalog.cs:2357-2510`; `StructuralParameters` rows (bounds 0-60); `EnergyLedger.ElectricityTaxDeltaEurPerMwh` (:469-475), `EffectiveElectricityTaxEurPerMwh` (EU business floor, :479-489) | §499, §500, §501, §543 | BUILT | EU five; USA not offered | coverage capped at 1 (`EnergyLayer.ElectricityTaxCoverage` :89-95) |
| EM-20 | Carbon tax per tonne (national currency) and its statutory indexation | Budget tax tile (`GameController.BudgetV35.cs:456`, draft `_taxRateInputs[CarbonTax]`) -> budget bill `TaxLines[CarbonTax]` | `CarbonRateStatute.AdvanceYear` (:105-127; called `SimulationManager.cs:4935`); `TaxLine.CarbonTaxMax` 300 USD/t (`TaxLine.cs:70`), `RateCeiling` carried by P | §464, §467, §471 | BUILT | SE indexed; DE BEHG; FR nominal; IT PL US none at the seed (`WorldFactory.cs:221-241`) | the BEHG schedule runs on a 2023 clock (`SeedYear` :55, `ComingYear` :97), not the world clock (stated in the class doc) |
| EM-21 | AI energy ministry (statutory mandates, security-of-supply veto) | "Grid and target" mandate row (read-only, EnergyV35 :267-290) | `AiEnergyMinistry` (`Live = false` :40; call site `SimulationManager.cs:4883`) | SPEC S11 (stage 7); §544 | HELD | mandates DE IT FR SE; PL BILLED; USA none | its year is `PensionAgeStatute.SeedYear (2026) + CurrentTurn + 1`, not the world clock's |
| EM-22 | Energy page: own rail cell; v3.5 Overview (prices, plants, grid and target) and Policy; kept "why" plates (rule row, fleet, zones, water, ledgers) | rail cell Energy | `GameController.EnergyV35.cs` `DrawEnergyTab` (:33-55), `DrawEnergyOverviewV35` (:75-); `GameController.Energy.cs`, cache `EnsureEnergyCache` (:54-73) | §481, §486, §536, §569, §747, §748 | BUILT; PF-6 OPEN | all six | section 4.4 |
| EM-23 | Energy events state-triggered, duration-bearing (stage 8) | - | three typed random events remain (`EventSystem.cs:93, 135, 319, 396`); `ShockDurationDays` (:78) | SPEC S12; SPEC II §5 | NOT BUILT | - | |
| EM-24 | Italy's 7 zones, the USA's 3 interconnections | - | each clears as one zone | SPEC I §4 | NOT BUILT | IT, US | |
| EM-25 | Load growth / electrification trend and its transport-CO2 cross-term | ABSENT row (`GameController.Energy.cs:161, 581`) | - | SPEC II §2.1; SPINE §8 | NOT BUILT | - | |
| EM-26 | Unserved energy / controlled shedding | only the ceiling price caption on the rule row | `BlockResult.UnservedMw` (:270-276); no runtime reader outside EnergyMarket | SPEC I §4 ("scarcity -> controlled shedding is in") | PARTIAL | five single-zone | shed load costs no output, approval or vote |
| EM-27 | Retail electricity price -> approval | - | approval reads `EnergySpendingChange` (`MacroSystem.cs:2263-2290`), not a price | SPEC II §2.2 last row ("the retail price's approval term goes through the existing approval model, one term") | NOT BUILT | - | the price reaches votes only via inflation |
| EM-28 | Energy sector row derived from system cost; support schemes as appended `SpendingCategory` members (S10) | - | deviation stated in the `EnergyLedger` class doc (:20-31) | SPEC II §7 stage-4 notes (iii), (v) | NOT BUILT (deviation) | - | |
| EM-29 | EN-3c seed prices vs the exchange | - | - | FL EN-3c, T-8; §470, §472 | DEFERRED (trigger) | DE, PL measured | |
| EM-30 | Generation-cost pricing of orders (CAPEX, O&M) | "Build costs" BILLED tile (EnergyV35 :198-) | `EnergyFleet.CapexBill` (:63) | FL E-44 | NOT BUILT (BILLED) | - | orders are free |

### 1b. Macro, monetary, trade, sectors

| ID | feature | player-facing entry | code | spec authority | status | countries | notes |
|---|---|---|---|---|---|---|---|
| EM-31 | National accounts identity, daily affine slice: C and I uniform shares 0.60/0.20 of prior GDP, G = T-3 scaled plan deflated, NX = TradeBalance, half the gap to period-open potential | none (consequence) | `MacroSystem.ApplyNationalAccountsDaily` (:620-668; contraction [0, 0.99] :650; `MinGdp` 1 :47); `IdentityGovernmentConsumption` (:230-231); call `SimulationManager.cs:444` | MR "Economic Theory" (:50-101); §507 | BUILT | all six | S-25 (uniform C/I) and FT-16b recorded; the USA's GDP runs ~12 % under potential for the whole run (POTENTIAL_PREMISE; "a structural offset", §508, Elias's) |
| EM-32 | Potential output from its factors | none | `Data/PotentialOutput.cs` (`LabourInput` :52, `Potential` :66, `DerivedGrowthPercent` :74); `MacroSystem.ApplyPotentialGdpGrowthDaily` (:584-599), `ApplySectorGrowthEffect` (:2024-2060; total adjustment +/-1 :2029, trend [0, 8] :2050) | §322, §394, §522; `docs/generated/POTENTIAL_PREMISE.md` | BUILT | all six | |
| EM-33 | Okun daily, FT-7 supply shock, FT-8 natural-rate composition shift | none | `MacroSystem.ApplyOkunsLawDaily` (:373), `ApplySupplyShockToUnemployment` (:418-447), `ApplyNaturalRateFromLabourForce` (:456-462); U [0, 30] (:253) | MR "Economic Theory"; §391, §398 | BUILT | all six | `EffectiveNaturalUnemploymentRate` (`Country.cs:207`) unclamped |
| EM-34 | Phillips curve with level terms, anchored expectations, the daily price level | none | `ApplyPhillipsCurveInflation` (:504-519; inflation [0, 30], :510 - deflation unrepresentable), `ApplyInflationExpectations` (:547-557), `ApplyPriceLevelDaily` (:578-582, floor 0.0001); calls `SimulationManager.cs:459-460, 5111` | MR "Economic Theory"; §327, §385 | BUILT | all six | `NominalGdp` = GDP x PriceLevel (`EconomyState.cs:22`); `DebtToGdpRatio` (:369) |
| EM-35 | Central bank: Taylor rule + the head's bias; head appointed at election turns from 2-3 drawn candidates (bias -1.5..+1.5) | Politics > The bank (`GameController.PoliticsV35.cs:74`); candidates `GameController.cs:3833-3856` (holds the clock); "Appoint" writes `CurrentFedChair` directly (`GameController.cs:4250`; Docket `GameController.DocketV35.cs:253`) | `TaylorRule.GetSuggestedInterestRate` (:128); `FederalReserveSystem.ApplyFedChairInterestRate` (:149; 15 % a turn :41), `GenerateCandidates` (:95); `CurrencySystem.ApplyInterestRateChanges` (:45; [0, 15] :18) | MR "Federal Reserve" (:663), "Rate Damping" (:742); C-C7 (`WorldFactory.cs:966-1000`) | BUILT | SE PL USA | the preview runs the year at the TARGET (`SimulationManager.cs:5366-5370`), as `Ed/PreviewParityDiagnostic.cs:102-123` asserts |
| EM-36 | Euro rate: members' readings blended by HICP weights, 15 % a turn; the player member's push +/-0.75 | "National rate push" (`GameController.cs:4031`) -> `PolicyDecision.InterestRateChange` (`BuildPlayerDecision` `:6277-6299` - the only boundary decision field the player sends) | `EurozoneRateSystem.GetBlendedSuggestedRate` (:50), `HicpCountryWeight` (:84), `ApplyEurozoneRate` (:118) | MR "Eurozone Rate Voice" (:2753); §585 | BUILT | DE FR IT | FT-16b OPEN |
| EM-37 | Monetary-regime laws (10) on target, r*, the gap weights | Laws board (MonetaryRegime) -> `IntroduceLawBill` | `LawCatalog.cs:2194-2290` (`RequiresOwnCurrency`); `StructuralParameters` zone rows (target 0-6, r* -1..5, weights 0-2) | §330; LEVER_MAP §3 | BUILT | SE PL USA | refused for the euro three (`SimulationManager.cs:1868`) |
| EM-38 | Player policy-rate slider +/-2 (own currency, no head) | `GameController.cs:4006` | `CurrencySystem.ApplyInterestRateChanges` (:66-84) | S-18; `PolicyImpactLedger.cs:74-77` | CODE-PRESENT, UNREACHABLE | none | all three own-currency countries carry a head from the seed (`WorldFactory.cs:962, 992, 997`) |
| EM-39 | Floating currency strength | none | `CurrencySystem.ApplyCurrencyStrength` (:108-139, [50, 200]) | MR "Economic Theory" | BUILT | SE PL USA | FT-12 OPEN: unweighted partner average (:125) |
| EM-40 | Trade: sourced 2023 matrix, static volumes, bloc/base/override tariffs, mirror retaliation on overrides, import-price pass-through, the Trade bill on the openness axis | Laws > Trade: base dial (`GameController.LawsV35.cs:766`, USA only, 0-50 %), override tiles (:827-876), "Introduce trade bill" (:815-816) -> `IntroduceTradeBill` (`SimulationManager.cs:2327`, ForeignAffairs gate) -> `ApplyTradeBillEffects` (:2389) | `TradeSystem.ApplyTradeEffects` (:221-245; exports net of partner tariff :236, imports gross :237), `GetRetaliatoryTariffRate` (:130), `ComputeTariffRevenue` (:190); `TradeCosts` (:36, :48, :61); volumes written only at construction (`TradePartner.cs:43-44`) | MR "Per-Partner Tariff Overrides" (:1212); §533 | BUILT | all six; EU members have no base dial ("the base rate the model holds is never charged", LawsV35 :758) | T-4, T-5, T-6 OPEN; `EstimateTradeBill` (`SimulationManager.cs:5533-5551`) estimates on clones |
| EM-41 | "Set override": a live per-partner override at today's rate, no bill | override tile button (`GameController.LawsV35.cs:868-876`) writes `link.PlayerTariffOverride` | - | slip (LawsV35 :783); MR :1212 | CODE-ONLY (direct UI write) | all six | nothing ever writes -1 again; `ResetPartnerTariffDraft` (`GameController.cs:9492-9496`) clears the draft only, though `SimulationManager.cs:5824-5835` documents a reset |
| EM-42 | Economic sectors: 8 x 5 dials, reversion, growth (cap 0.5) and unemployment (cap 0.3) terms, SC-1 costs | Laws > Sectors (`GameController.LawsV35.cs:507-553`, bill :473-474) and Energy > Policy -> `IntroduceSectorBill` | `MacroSystem.ApplySectorEffectsInternal` (:1810-1840), `GetSectorGrowthAdjustment` (:2003), `GetSectorUnemploymentAdjustment` (:2074); `SectorCouplings.SupportCost/SupportCostTarget`; `Data/Sector.cs`, `SectorType.cs` | MR "Economic Sectors" (:1787), "Sector Integration" (:2360), (:2891), (:3027); §503/§504, §739 | BUILT | all six | shares floored at 0, no sum constraint; regulation and ownership dials cost nothing (`SectorCouplings` class doc) |
| EM-43 | Infrastructure condition stock + growth term | Budget infrastructure lines | `MacroSystem.ApplyInfrastructureCondition` (:1860), `...Daily` (:1894), `ApplyInfrastructureGrowthEffect` (:1959) | MR "Infrastructure System" (:2186), "Feedback" (:2271) | BUILT | all six | separate from the infrastructure FAMILY |
| EM-44 | Real wage, productivity, house prices, housing, gini, poverty readouts | none | `ApplyRealWageIndex` (:1238), `ApplyProductivity` (:1537), `ApplyHousePriceIndex` (:1396), `ApplyPovertyRate` (:743), `ApplyGini` (:1150) | MR | BUILT | all six | |
| EM-45 | Approval's inflation term against the zone's TARGET | none | `ApplyApprovalRating` (:2263; gap :2271); poverty (:747); expectations anchor (`SimulationManager.cs:5111`) | MR "Political Layer" (:601) | BUILT | all six | X-09 |

### 1c. Demography and labour

| ID | feature | player-facing entry | code | spec authority | status | countries | notes |
|---|---|---|---|---|---|---|---|
| EM-50 | Cohort substrate: 21 bands anchored to EUROPOP2023 (EU five) / Census 2023 middle series (USA), rebased on the seeded 2024 stock; committed once a year | People population card (`GameController.PeopleV35.cs:105-111`) | `CohortDemographics.Step` (:109-148), `CommitYear` (:196-201; called `SimulationManager.cs:4826`), `WalkToEpoch` (:208-220); `PopulationCohorts.AnchoredNext/Generate`; `Data/Generated/PopulationProjections.cs` (2024-2100, clamped outside :545-552) | §195, §199 (F2), §343/§353/§364 (excess deaths) | BUILT | all six (US: 1 July stock) | Poland's projection base 5 % above the seeded stock (INFERRED arithmetic) |
| EM-51 | Daily scalar readings: Population (Lerp of start and next totals), BirthRate, DeathRate (written by the health family), NetMigrationRate (the residual), PopulationGrowthRate, DependencyRatio (65+ / 15-64) | People; Laws slip | `CohortDemographics.Apply` (:153-186; the only live writer of `State.Population` :182); `ApplyDaily` (:192-193; `SimulationManager.cs:370`) | §195 | BUILT | all six | `NaturalBirthRate`, `NaturalNetMigrationRate` written daily, read by nothing |
| EM-52 | Family policy (fertility multiplier) and Immigration policy (additive migrants by the sourced age profile) | Laws > Labour dials (`GameController.LawsV35.cs:183, 188`, 0-100) -> labour bill (`IntroduceLaborBill` `SimulationManager.cs:1719`) -> `ApplyDemographicPolicyChanges` (:6118-6129) | `CohortDemographics.Step` (:124-129); `LaborCouplings` (+0.03 births/1000/pt, +0.1 migration/1000/pt) | MR "Demographics, Part B" (:3377) | BUILT | all six | SD-21 (the immigration lever compounds) |
| EM-53 | Participation (15+): structural anchor = pyramid x sourced rates by age + PN-1 points; dial, health, education and FT-5 terms; reversion 0.15 a year | none | `MacroSystem.ApplyLaborForceParticipationRate` (:877-912; `SimulationManager.cs:378`); `ParticipationRateTable.StructuralRate`; `LaborTaxParticipationTerm` (:849-859) | §199, §396, §522, §596, §599 | BUILT | all six | |
| EM-54 | Labour dials: minimum wage (Kaitz 0-100, disabled SE/IT), paid family leave (0-104 weeks), overtime (0-100), retraining (0-100) | Laws > Labour (`GameController.LawsV35.cs:148, 158, 170, 175`) -> labour bill | `Sim/LaborCouplings.cs`; `SimulationManager.ApplyLaborPolicyChanges` (:6061-6077); clamps (:591-593, :624-631) | MR "Labor Market Basics" (:1640), "Deeper Labor Market Policies" (:2069) | BUILT | all six | statutory minimum-wage steps (`MinimumWageRates`) are UI captions only (LawsV35 :1166-1177): the Kaitz index stays at its seed |
| EM-55 | Pension age dial (60-70) and the PN-1 participation response | Budget pensions row (`GameController.cs:10542`) -> budget bill `PensionAgeSet/PensionAge` (`BudgetBill.cs:70-74`) | `PensionAgeStatute.AgeInForce`; `PensionParticipationResponse` (FR 0.209, DE 0.135 measured; else rate x 0.425) | FL PN-1; §489, §520, §590, §596, §599 | PARTIAL | all six | PN-1's open list (FL PN-1 row) |
| EM-56 | Youth unemployment | none | `MacroSystem.ApplyYouthUnemployment` (:1020-1031, [0, 60]) | MR | BUILT | all six | `BaselineYouthUnemploymentRate` and the 15-24 rates by age are two unreconciled sources (SD-27) |

### 1d. The six society-stat families (+ crime couplings)

| ID | feature | player-facing entry | code | spec authority | status | countries | notes |
|---|---|---|---|---|---|---|---|
| EM-60 | Health: coverage, treatable mortality (trended anchor), cataract and knee waits, 5 supporting readouts; writes `DeathRate` (its only writer) | People Health tiles (`GameController.PeopleV35.cs:139-166`), slips (`PeopleSlips.cs:256-295`); Health minister slip (`GameController.PoliticsV35.cs:559-562`) | `HealthFamily.Seed` (:138-163), `AdvanceYear` (:248-267), targets (:229-244), `DeathRateFor` (:96-97), `QualityLog` (:112-117); call `SimulationManager.cs:4971` | CAT Health §1-§9; §333, §343, §353, §364 | BUILT | waits ABSENT (-1) DE FR US; supporting readouts ABSENT FR; US coverage ceiling 91.8 | feedbacks: life expectancy (`MacroSystem.cs:1077`; LE has no simulation consumer beyond display and publication), participation (:903), the pyramid's excess deaths (`CohortDemographics.cs:137-145`). Coverage -> approval not built by ruling (§343 §D) |
| EM-61 | Education: attainment (3 shares), early leavers, students per teacher; participation and productivity-trend feedbacks | People Education tiles (PeopleV35 :168-198; PISA and graduation print "billed"); Education minister slip (PoliticsV35 :564-566) | `EducationFamily.AdvanceYear` (:194-222), `ParticipationTerm` (:142-148), `ProductivityTrendTerm` (read `MacroSystem.cs:2028-2029`, one turn lagged); call `SimulationManager.cs:4972` | CAT Education §1-§8; §338, §348, §373 | BUILT (PISA, graduation BILLED) | US early leavers ABSENT | does not read Education effectiveness |
| EM-62 | Infrastructure: road quality (WEF 2019 score, saturating rebuild) and connectivity | People Infrastructure tiles (PeopleV35 :200-217) | `InfrastructureFamily.AdvanceYear` (:85-114); call `SimulationManager.cs:4973` | CAT Infrastructure §1-§7; §339, §354, §350/§359/§362, §366, §374 | BUILT (readouts) | all six | the level form is sheeted (billed source) |
| EM-63 | Environment: power CO2 (EM-09), transport CO2 (real carbon rate + infrastructure lines), GHG headline, electricity mix | People Environment tiles (PeopleV35 :219-245); Energy mix and carbon rows (`GameController.Energy.cs:205-214, 301-302`) | `EnvironmentFamily.AdvanceYear` (:139-149), `TransportTargetFor` (:129-136); tax base `TaxBases.cs:80-83`, revenue `:136-142` | CAT Environment §1-§8; §340, §342, §349, §460, §464, §467, §471 | BUILT | carbon tax SE DE FR; none IT PL US at the seed | "per person" for power is per SEED person (`EnergyMarket.cs:703`; §460 convention) |
| EM-64 | Immigration and poverty depth: irregular migration (EU flow / US stock), poverty gap (Eurostat / OECD), underemployment, homelessness | People Migration tiles (PeopleV35 :247-261) | `MigrationPovertyFamily.AdvanceYear` (`Data/ImmigrationPovertyFamily.cs:118-147`); call `SimulationManager.cs:4976` | CAT Immigration §1-§7; §341, §351, §375 | BUILT (readouts) | US carries the two definitions as flags | feedback STOPPED (§351, billed §375) |
| EM-65 | Department effectiveness: allocated / requested x minister efficiency per portfolio; the level ratio | minister card chip "Underfunded x r" (PoliticsV35 :391-392), slips (:409-414, :538-552), Budget tile caption (`GameController.BudgetV35.cs:717`), state line (`GameController.Effectiveness.cs:23-31`) | `PortfolioEffectiveness` (`Data/PortfolioEffectiveness.cs:15-29`), `Effectiveness.Record` (:103-118; called `SimulationManager.cs:6261`), `EfficiencyOf` (:89-97: empty chair = 1) | CAT §6; §336, §337 | BUILT; only Health reads it | all six | six portfolios mapped (:33-72) vs CAT §6's "eight departments". SD-30, SD-31 |
| EM-66 | Crime and justice couplings (not a family, CAT §7): crime, organised crime, corruption, prisons; six dials | Laws > Crime & Justice tiles (`GameController.LawsV35.cs:255-301`), dials set by law or the crime-and-justice bill | `Sim/CrimeJusticeCouplings.cs` (:79-), `MacroSystem.ApplyCrimeIndex` (:1578), `ApplyPrisonPopulationRate` (:1648), `ApplyOrganizedCrimeIndex` (:958), `ApplyCorruptionIndex` (:980) | MR (:1719, :2118, :2951); §738 | BUILT | all six; prison seeds sourced US DE FR only (`WorldFactory.cs:520-536`) | Laws tiles recompute dial costs from today's GDP (LawsV35 :264-276, :300-301) |
| EM-67 | The families' eleven history series | none (sparklines retired §732) | `Data/StatHistory.cs:144-165, 214-229` | §732 | CODE-ONLY (written daily, persisted, unread) | all six | |

### 1e. Statistics and publication

| ID | feature | player-facing entry | code | spec authority | status | countries | notes |
|---|---|---|---|---|---|---|---|
| EM-70 | Statistics: Domestic (public finances, sector bar, six series cards, society tiles, more readings, "your policies") and International (map chips, trade card, partner shares, pair page) | rail STATS (`GameController.cs:5133`) -> `DrawStatisticsTab` (:7305); tabs (`GameController.StatisticsV35.cs:37-38`); dagger dense view (`GameController.V35.cs:77-91`); pager OLDER/NEWER (`GameController.Statistics.cs:396-398, 473-492`); pair-page partner codes (:655-680); map chip/event (StatisticsV35 :286-295); slips: hover, pin up to 3, Esc (`GameController.Slip.cs:21-136`) | `UI/GameController.Statistics.cs`, `GameController.StatisticsV35.cs`, `StatsReadings.cs`, `StatsSlips.cs` | §701, §708 (D-ST 23a-23c), §728, §729, §243, §97, §103-§106 | BUILT | all six; currency strength only SE PL US; overburden ABSENT USA; statutory-rule warnings EU five only (`UI/FiscalRules.cs:32-40`) | no time-range or country selector |
| EM-71 | Policy-screen stat strips and the trace panel | Budget strip (`GameController.BudgetV35.cs:828-849`); Laws "what these dials move" tiles (`GameController.LawsV35.cs:1078-1159`); chip click -> `StatTracePanel.NotifyChipClicked` (`PolicyScreenStatsRenderer.cs:238-241`) | `PolicyScreenStats.ReadLiveValue` (:109-137); renderer (max 4 chips :120); `StatTracePanel` (traces Approval, ConsumerConfidence, DebtToGdp only, :76-77) | §9, §20, §21 | BUILT | all six | |
| EM-72 | Publication: monthly unemployment/youth (first Friday), inflation and real wage (US 12th; EU last weekday), quarterly GDP (US t+30/60/90; EU t+30/65), annual readouts at the fiscal-year start; GDP preliminary noise +/-1.5 % on the level | only the crime bulletin line on the Laws Crime slip (`GameController.LawsV35.cs:1116-1120` -> `UI/PublishedFigure.cs:31-41`) | `PublicationSystem.PublishDueFigures` (:151-207; noise :28, :195-197); `ReleaseCalendar`; `Data/PublishedData.cs`; called `SimulationManager.cs:333`, BEFORE the day's step | §6, §7, §11, §12, §188 Part 1, §604, §709 | BUILT; the published VIEW was cut (§62, E-5) | all six | election perceived performance reads published unemployment and inflation (`Elections/PerceivedPerformance.cs:189-207`) - FINAL copies, no noise; RNG stream `PublicationRevision` (`SimulationRandom.cs:47`) isolated |
| EM-73 | Multi-resolution stat history | every chart (Quarterly) | `Data/StatHistory.cs` (37 series x 4 resolutions, cap 250 `:103`, `RemoveAt(0)` :72-75); `History.Append` daily (`SimulationManager.cs:470`) | §4 | BUILT | all six | Daily/Weekly/Monthly read only by Editor checks |
| EM-74 | Derived statistics (GDP per person; tax, spending, deficit, primary deficit shares) | Statistics public finances, pair page | `Sim/DerivedStats.cs:36-152` | §9 (A4), §13, §586 | BUILT | all six | `RealGdpGrowthPercent`'s consumer `CreditRatingSystem.Evaluate` (:66) has no caller |
| EM-75 | Shadow baseline line and the impact ledger ("your policies") | Statistics cards' counterfactual line; "Your policies" rows (`GameController.Statistics.cs:548-593`) | `Sim/ShadowBaseline.cs`, `Sim/PolicyImpactLedger.cs` (families :60-90, `AdvanceTurn` :132-150) | §103-§106 | BUILT | all six | the fleet is not a ledger lever (§544 (b)); PF-18 open |

### 1f. Per-country availability in this slice (startable countries: all six)

| lever / feature | SE | DE | FR | IT | PL | US |
|---|---|---|---|---|---|---|
| zones | 4 + water value | 1 | 1 | 1 (7 named, not built) | 1 | 1 (3 named, not built) |
| fleet orders (orderable labels; INFERRED from `CannotOrderWhy` :72-91 except the named refusals, which are code and §544) | wind, solar (coal/gas refused: `ClearsFossilFleet` false) | coal, gas, wind, solar (nuclear refused: closed by law) | gas, nuclear, wind, solar (coal refused: fuel groups) | gas, wind, solar (coal refused; no nuclear in the record) | coal, gas, wind, solar (no nuclear in the record) | coal, gas, nuclear, wind, solar |
| queue kind | operator (Svk) | statute (EEG) | operator (wind, solar lines only) | operator (wind, solar only) | operator | operator (LBNL) |
| policy levy (EUR/kWh households, `EnergyLayerData.RetailPolicy` :517-525) / energy line | 0.0002 / yes (36 % support) | 0.0291 / NO line | 0.0062 / yes (76.29 %) | 0.0251 / yes (3.47 %) | 0.0161 / yes (97.95 %) | 0 (none) / yes |
| electricity-tax laws | yes | yes | yes | yes | yes (excise only) | not offered |
| carbon tax at seed | 1 330 SEK, indexed | 30 EUR, BEHG schedule | 44.6 EUR, nominal | none | none | none |
| ETS | 85.51 | 85.51 | 85.51 | 85.51 | 85.51 | 0 |
| rate | head (Riksbank) + laws | ECB push +/-0.75 | ECB push | ECB push | head (NBP) + laws | head (Fed) + laws |
| base tariff dial | no (EU) | no | no | no | no | yes (0-50) |
| minimum-wage dial | disabled | yes | yes | disabled | yes | yes |
| health waits | yes | ABSENT | ABSENT | yes | yes | ABSENT |
| start (world clock) | pre-campaign before 2026-09-13 | 2024-11-06 | 2024-07-18 | 2022-07-21 | pre-campaign (2023) | pre-campaign (2024) |

The fleet row was read off the order rules, not run.

---

## 2. Specified but missing

| item | where specified | what exists |
|---|---|---|
| Winter-peak adequacy test, reserve margin (EM-03) | SPEC I §2, II §3.1 | `MaxMw` carried, unread at runtime |
| Retail electricity price -> approval term (EM-27) | SPEC II §2.2 (hemicycle/approval row) | approval reads the energy spending line only |
| Stage 8: state-triggered, duration-bearing energy events (EM-23) | SPEC S12; II §5 | three typed random events |
| Load growth, the electrification cross-term (EM-25) | SPEC II §2.1; SPINE §8 | ABSENT row |
| Italy's zones, the USA's interconnections (EM-24) | SPEC I §4 | named future items |
| An ETS path | SPEC S5; ENERGY_LAYER_PREMISE §7 ("until a stage gives it a path") | 2023 mean carried by the price level |
| Investment planning and state ownership reaching the layer (EM-18) | SPEC I §3.6, S9 | descriptive sector effects only |
| The energy ministry live, with minister attributes (EM-21) | SPEC S11 | HELD; no Energy portfolio exists (§544 "owed, with the portfolio") |
| Generation costs (EM-30) | FL E-44 | BILLED |
| Appended support-scheme lines, a derived Energy sector row (EM-28) | SPEC S10, II §2.2 | deviations stated (§463) |
| Trade volumes indexed to GDP; retaliation against a base-rate hike; retaliation memory | register T-4, T-5, T-6 (FL pointer index) | static volumes; base hikes unanswered |
| Removing a per-partner override | `SimulationManager.cs:5824-5835` doc; MR :1212 ("until the player changes or resets it") | no writer of -1 |
| Effectiveness read by education and infrastructure | CAT §6 ("the ministry's outcomes (the families above) read effectiveness") | only Health reads it |
| "Eight departments" | CAT §6 | six portfolios |
| Underemployment's labour-dial terms; homelessness reading the poverty gap; the US stock following the substrate's inflow | CAT Immigration §6 | not in `MigrationPovertyFamily` |
| PISA, graduation rate | CAT Education §4, §7 | BILLED (§373) |
| Statistics graph features: a real date axis, release markers, preliminary/revised treatment, 1yr/5yr/all ranges; the published view on Statistics | §6 (B1), §11 (A3) | index axis; published view cut (§62/E-5) |
| EU inflation flash (~17th full figure), EU GDP t+45 flash and ~t+110 estimate; the flash describing its own month | §188 Part 1 | a single monthly release describing the previous month |
| Fiscal axis ticks every 10 % (the slip still promises them) | §701 | not drawn (`GameController.StatisticsV35.cs:119-125`; `StatsSlips.cs:92`) |
| PN-1's open list (age-specific effects reused for a lowering; Germany women-only; no bill-route check; the natural rate's age mix ignoring the response) | FL PN-1 | open |
| §604's three-quarter walk of the pyramid ("its own family") | §604; `CohortDemographics.cs:58-61` | not built; the nine-month lag is stated |
| Turnout by age (Sweden) | §201 | billed |

---

## 3. Present but unspecified (or contradicting its own specification)

- **"Set override" writes a live override with no bill** (EM-41). The slip states it (LawsV35 :783); no § was found that rules a UI-side state write.
- **Central-bank appointment writes `CurrentFedChair` from the UI** (`GameController.cs:4250`, `GameController.DocketV35.cs:253`). No `SimulationManager` entry point exists for it, so a harness must replicate the UI's write.
- **Fleet orders mutate `Country.FleetOrders` from the UI** (`GameController.EnergyV35.cs:543, 546`). They are not a `PolicyDecision` field, so the impact ledger and the lever map cannot see them (§544 (b); LEVER_MAP generated 2026-09-15, before §539).
- **The dead policy-rate slider** (EM-38).
- **Written and unread:**
  - the families' history series (EM-67);
  - `NaturalBirthRate`, `NaturalNetMigrationRate`;
  - Daily/Weekly/Monthly history buckets;
  - `BaselineDependencyRatio`, `BaselineNetMigrationRate` (vestigial anchors);
  - `EnergyMarket.EndTurn`;
  - `Effectiveness.LargestDeparture`;
  - `CreditRatingSystem.Evaluate`;
  - `PolicyScreenStats.GetLeverCount`;
  - `PublishedSeries.LatestForPeriod`.
- **`MinimumWageRates` statutory steps** are UI captions only (EM-54).
- **Stale generated documents:**
  - `docs/generated/ENERGY_LAYER_PREMISE.md` §7 (generated 2026-09-12) says "The fleet does not invest or retire" (false since §539) and that Sweden's indexation "is not carried" (false since §471).
  - `docs/generated/LEVER_MAP.md` predates the fleet and lists `PowerCo2PerCapita` unreached. Both need a re-run, not an edit.
- **Stale reference text:**
  - MR :4191 names `EnergyMarket.CarbonTaxPointPerTonne` (no such member).
  - MR :79 and :93 have Phillips and Taylor reading the seeded NAIRU; they read the effective rate since §398/§399.
  - MR :3081-3453 describes the retired scalar demography.
  - CAT :104 and :240 call feedbacks "not built" that are built.
  - `EnergyLedger.cs:33-35` says the congestion rent is zero at the seed.
  - CLAUDE.md:31 says "`CheckFinite` covers every float".

---

## 4. Invariant map

### 4.1 Energy supply = demand + losses

What a clearing holds: `EnergyMarket.Result r` (EnergyMarket.cs:106-128). `r.Zones[zone][block]` is a `BlockResult` (:89-95) with these fields: `DemandMw, MustRunMw, ResidualMw, CurtailedMw, UnservedMw, Price, Scarcity, DependableMw, FossilMw[3], MarginalCost[3]`.

None of this is stored on the state. Re-clear with `EnergyMarket.Clear(country)` (:337), which opens the country's fleet scope itself.

**Single-zone countries (DE FR IT PL US), per block b.** The zone index is `z = EnergyLayer.ZoneIndex(EnergyLayer.Code(id))`.

- `B.DemandMw == EnergyLayerData.DispatchDemandMw[z][b]` (set at :322).
- The floors: `floor_k = EnergyMarket.MustRunFossilMw(id, k)` (:195). This is public and scaled by the fleet, so read it inside `EnergyFleet.For(country)`.
- `B.MustRunMw` INCLUDES the floors (:322), and so does `B.FossilMw[k]` (:323). Subtract them once.
- **Identity (all three branches of `ClearBlock` :240-286):**
  `(B.MustRunMw - sum_k floor_k) + sum_k B.FossilMw[k] - B.CurtailedMw + B.UnservedMw == B.DemandMw`, to a relative 1e-9.
- **Losses** are not a modelled quantity. `DispatchDemandMw` is defined as "consumption + net export + losses + pumping" (EnergyLayerData.cs:271). ENTSO-E's load "carries network losses" (SPINE §3 gate). The USA's ~4 % of losses and own use sits inside its load. The losses are 2023's and constant.
- **Imports and exports:** no flow between the six is cleared. Each country is an island with its 2023 net exchange folded into demand, and neighbours outside the six are an exogenous exchange (SPEC II §3.2).

**Sweden, per block b, per zone SE1..SE4** (`ClearSweden` :521-586).

- `B.DemandMw = ZoneConsumptionBlockMw + ZoneExternalExportMw` (:549); `B.MustRunMw` is the zone's supply (:550); `surplus_z = MustRun - Demand` (:553).
- **Chain identity:** if no `r.Links[k].Binding[b]`, then `sum_z surplus_z == r.ChainUnbalanceMw[b]` (:573).
- A binding link clamps the flow to its capacity (:566-570), and the excess leaves the chain's accounting (spilled, or priced at 4 000 north of a deficit cut).
- **Losses are NOT modelled.** eSett settlement consumption excludes network losses (SPINE §3: 127.02 against 130.62 TWh, -2.8 %); they land in `ChainUnbalanceMw` ("tens of MW", :115). Assert it is small, not zero.
- **`B.CurtailedMw` is export** (:574; §544 (a)). Never sum it as curtailment.

**Annual figures.**

- `r.AnnualGwh` (:117, `Accumulate` :433-451) counts must-run LEVELS times hours. It does not net curtailment (recorded, §544 (a)).
- So "annual generation = demand + losses" fails by construction in any year with a curtailing block. Assert per block.
- `MixSharesNow` (:708-718) inherits this.

**Fleet stock-flow.**

- `EnergyFleet.CapacityMw(country, k) == max(0, EnergyLayer.RecordCapacityMw(id, k) + sum of landed orders on k)` (EnergyFleet.cs:205-214).
- Every order satisfies `OnlineTurn == OrderedTurn + (Mw > 0 ? LeadTimeYears[k] : 1)` (:125-130).
- After the boundary at `CurrentTurn + 1`, `Landed == (OnlineTurn <= turn)` (:189-200).
- ⚠ The id-keyed `EnergyLayer.CapacityMw(id, k)` (:113) sees landed orders ONLY inside `EnergyFleet.For(country)`. Outside any scope it reads the RECORD (EnergyFleet.cs:15-21).

**Queue.**

- Operator countries: `TakenMw(country, line, year) <= line.CapMwIn(year) + 1e-6`.
- Germany: the same per calendar year of `OrderedYear` (EnergyConnectionQueue.cs:215-247).
- Retirements count against neither.

**Reservoir.** `|HydroReservoirBalanceGwh| <= EnergyLayer.SwedenReservoirCapacityGwh()` (EnergyMarket.cs:480).

### 4.2 Stock-flow identities

**Energy ledgers.**

- `EnergyLedger.Book.Gap == 0` (EnergyLedger.cs:110; :107 vs :108). This holds by construction.
- `SupportCost == LevyRevenue + BudgetSupport` (:103).
- `InframarginalRent == WholesaleOutlay - FossilVariableCost` (:102). It can go negative where the floors price at their cheapest tranche - a signal, not a break.
- The Book is never stored. Recompute it with `EnergyLedger.Compute(country, EnergyMarket.Clear(country), PriceLevel, EnergyLedger.CreditFor(country))` (:321).
- **Stored vs recomputed:** `EnergyLedgerDiagnostic` §5 already asserts that the state's prices and bill equal the recomputed stack after ten years, relative 1e-5 (`Ed/EnergyLedgerDiagnostic.cs:217-221`).

**Power CO2.**

- `PowerCo2PerCapita == clamp((r.DerivedCo2Mt + env.PowerResidualMt) / env.PowerPopulationSeedM, 0.02, 40)` (EnergyMarket.cs:699-705; EnvironmentFamily.cs:71, :145).
- It is exact at the seed (`Ed/EnergyLayerCheck.cs:158-160`).

**Price level and output.**

- `NominalGdp == GDP * max(0.0001, PriceLevel)` (EconomyState.cs:22).
- Daily: `PriceLevel_{d+1} == max(0.0001, PriceLevel_d * (1 + Inflation_d/100)^(1/365))` (MacroSystem.cs:581).
- `PotentialGDP == PotentialGdpSeed * (LabourInput / PotentialLabourSeed) * PotentialProductivityIndex` (PotentialOutput.cs:66-71), daily and at the boundary rewrite (MacroSystem.cs:2056-2061).

**Population.**

- **Daily, exact:** `State.Population == Mathf.Lerp(Cohorts.Total, sum(CohortDemographics.Step(c, SubstrateYear(turn)).Next), dayOfTurn/365f)`. The same `Lerp` holds for `DependencyRatio` and `OldAgeDependencyRatio` (CohortDemographics.cs:182-185).
- **After `AdvanceTurn`:** `State.Population == Cohorts.Total`. This holds only if no demographic lever and no `DeathRate` changed between the day-365 step and the commit (SD-22).
- **Closure (by construction):** `delta sum == (BirthRate - DeathRate + NetMigrationRate)/1000 x mid` (:162-181). Migration is the residual.
- **Anchor:** with both levers at 50 and zero excess deaths, `Cohorts.Counts[k] ~= CohortDemographics.Target(id, SubstrateYear(turn))[k]` to 5e-4 relative. `Ed/CohortAgingStepDiagnostic.cs:74` uses that tolerance, but only on the seeded table. No check covers the live path.

**Trade.**

- The matrix is seeded symmetric, and the volumes are never written after construction (TradePartner.cs:43-44). So A's `ExportVolume` to B == B's `ImportVolume` from A for the whole run.
- ⚠ World net exports do NOT sum to zero. Exports are net of the partner's tariff and the currency factor, while imports stay gross (TradeSystem.cs:236-237) - SD-12.

**Effectiveness.**

- `PortfolioEffectiveness.Ratio == (Requested > 0 ? Allocated/Requested : 1) * Efficiency` (PortfolioEffectiveness.cs:24-28).
- Recorded once per boundary (`SimulationManager.cs:6261`) - SD-30.

Fiscal identities (debt change = deficit) belong to area B.

### 4.3 No NaN / Inf

**Guarded (read):**

- Energy: `EnergyLayer.NationalPerUsd` (:64), `ZoneShare` (:211), `LinkFlow.Utilisation` (:245); `ClearBlock` ratio (EnergyMarket.cs:278), `PowerCo2PerHead` (:703); `EnergyLedger.Write` (:549, :553), `LevyScaleWith` (:337), `CreditFor` (:253), `LoadWeightedPricePerMwh` (:519).
- Macro: `TaylorRule.GetOutputGapPercent` (:100-108); `PotentialOutput` (:66-77); the FT-5 log (MacroSystem.cs:855-856); the daily identity's `1 - contraction >= 0.01` (:650).
- Every `Mathf.Pow(1 + g/100, ...)` has g clamped above -100 (:581, 594, 1243, 1404, 1547).
- The cohort, participation and composition divisions (`CohortDemographics.cs:121-185`; `PopulationCohorts`; `ParticipationRateTable`; `UnemploymentRateByAgeTable`).
- Every family's denominators and logs (HealthFamily.cs:115, 199-237; EducationFamily.cs:167-220; InfrastructureFamily.cs:75-112; ImmigrationPovertyFamily.cs:99-132).
- `DerivedStats` (:143-152).

**Absence is the sentinel -1, never NaN.** This covers the family fields and the energy prices until the ledger seeds (EconomyState.cs:173-209). A NaN check will not see absence, and a bounds check must skip -1.

**NaN is not stopped by the guards.**

- C#'s `Math.Max(0.0001f, NaN)` returns NaN, and `Mathf.Max(a, NaN)` returns NaN when NaN is the second argument. So a NaN price level passes every `Max(0.0001f, PriceLevel)`.
- In Germany or Poland it then reaches `EnergyMarket.WaterValueOf`, which THROWS (:491) inside `BeginTurn` at the top of `AdvanceTurn` (`SimulationManager.cs:4820`). That is an S1 crash path.
- The families' guard diagnostics compare with `<`/`>`, so they are NaN-blind (`Ed/HealthFamilyDiagnostic.cs:100`, `Ed/EducationFamilyDiagnostic.cs:86`, `Ed/ImmigrationPovertyFamilyDiagnostic.cs:87`, `Ed/EnvironmentFamilyDiagnostic.cs:90`).

**Sign blow-ups.**

- `EnergyMarket.ClearBlock` scarcity `MaxClearingPrice - price` (:273) goes NEGATIVE when an unserved block's last tranche costs more than the nominal 4 000 (FT-11 OPEN). It is reachable by retiring the fossil fleet (X-02).
- `CohortDemographics.cs:125` divides by the neutral crude birth rate, guarded only by `> 0` (INFERRED: a tiny positive rate would give a huge multiplier; not reachable with sourced data).

**Unclamped values next to clamped ones.** `EffectiveNaturalUnemploymentRate` (Country.cs:207); `SupplyUnemploymentExcess`, which still moves while U sits on its clamp (MacroSystem.cs:445-446); `Population`, `NetMigrationRate`, `PopulationGrowthRate`, `DependencyRatio`.

**Enumerate fields by reflection, not the runner's list.** `SimulationTestRunner.CheckAnomalies`' 52-field hand list (Testing/SimulationTestRunner.cs:796-990) omits `PriceLevel`, every `Energy*` field, `PowerCo2PerCapita`, `TransportCo2PerCapita`, `HydroReservoirBalanceGwh`, the family fields, `Productivity`, `RealWageIndex`, `HousePriceIndex` and `Gini`. `TrajectoryBaselineDump.StateFields()` (Ed/TrajectoryBaselineDump.cs:83-88) enumerates every public `EconomyState` field. Add the Country-level fields: `PotentialGrowthRate`, `ProductivityTrendGrowthRate`, `Cohorts.Counts`, `FleetOrders` MW, the zone's `InterestRate` and Taylor parameters, and the `EnvironmentSeeds` arrays.

### 4.4 Stored value == presented value: candidate divergence points

**Energy.**

- **D-E1 (PF-6, OPEN).**
  - The Energy page caches one clearing and book per (turn, country) (`GameController.Energy.cs:54-73`), computed at the price level of the first frame drawn that turn.
  - The v3.5 Prices cards print the STORED headline (`s.EnergyHouseholdPrice`, `s.EnergyIndustryPrice`; EnergyV35 :109-112) over the CACHED stack bars (:100-104). The bars sum to the printed figure only if the cache was built on a boundary day.
  - Nothing invalidates the cache when an electricity-tax law passes mid-turn.
- **D-E2.** The Wholesale tile (:118) and the peer ticks (:150) read clearings with no stored counterpart. Each peer is cleared at its own price level and drawn on one 0-250 scale in its own market currency (EUR, or USD for the USA).
- **D-E3.** "Generation by technology" calls `EnergyMarket.MixSharesNow` (`GameController.Energy.cs:205`; also `PeopleSlips.cs:368`). That is a fresh full clearing inside the draw, and it counts curtailed energy as generated.
- **D-E4.** The levy-scale row reads the cached book (`GameController.Energy.cs:290`). The player's book holds the boundary figure (EnergyLedger.cs:384-386).

**Macro.**

- **D-M1.** The preview runs the year at the chair's TARGET rate, while the boundary moves only 15 % toward it (EM-35). This is asserted as designed, but the effect arrows describe a different year than the one that will run.

**Statistics.**

- **D-S1.** The fiscal shares divide the closed year's flows by TODAY's `NominalGdp` (`DerivedStats.cs:150`). They drift daily with no new fiscal data, and the deficit warning can flip without any.
- **D-S2.** The pair page's budget % does the same, guarded on real `GDP` but dividing by `NominalGdp` (`GameController.Statistics.cs:761-762`).
- **D-S3.** "GDP" is real on the strips (`PolicyScreenStats.cs:114`) but nominal on the cards (`GameController.Statistics.cs:163`).
- **D-S4.** Consumer confidence on the strip uses the live wage gap (`MacroSystem.cs:2475-2477`), while the trace's "Effective" row uses the period-open gap (`StatTracePanel.cs:362-379`). The identity consumes the period-open form. Both claim to be the same number (`PolicyScreenStats.cs:130-133`).
- **D-S5.** The tariff pass-through is labelled "applied" but reads the PLANNED term (`GameController.StatisticsV35.cs:359`; `SimulationManager.cs:5007`).
- **D-S6.** Growth is computed four ways: UI YoY (`StatsReadings.cs:104-110`), QoQ (`DerivedStats.cs:111-122`), the rating's YoY (`CreditRatingSystem.cs:213-218`), and the preview's.
- **D-S7.** Approval prints as "%" on the strips but as a score on Statistics. The inflation direction is "lower is better" on Statistics and neutral on the Policy Web.

**Families.**

- **D-F1.** `HealthFamily.PublicCoverageNow`, `SupportingNow` and `RetireeCoverage` are recomputed in the UI. The retiree label "F2 SUBSTRATE x COVERAGE" (`PeopleSlips.cs:273`) names a cohort computation that does not happen.
- **D-F2.** Ranks are taken against the peers' SEEDS (stated on the slip).
- **D-F3.** The education draft arrow skips the clamps (`GameController.PeopleV35.cs:175-184` vs `EducationFamily.cs:206-207`). The health draft arrow omits the effectiveness channel.
- **D-F4.** The Laws page recomputes enforcement costs (`GameController.LawsV35.cs:264-276, 300-301`).
- **D-F5.** Power CO2 is labelled "÷ POPULATION" (`PeopleSlips.cs:380`), but the divisor is the SEED population.

**Population.**

- **D-P1.** The People card shows `Cohorts.Total` (start of year). The Laws slip shows `State.Population` (daily). Tax bases read the state figure; potential and the spending drivers read the cohorts. The two differ by up to a year's growth (SD-23).

### 4.5 Bounds

- **Energy.**
  - Prices are unclamped by design: there is no floor, and the ceiling of 4 000 applies only to scarcity (EnergyMarket.cs:51).
  - `LevyScale >= 0` (EnergyLedger.cs:338). The network component is `>= 0` (:399), and so is the environmental tax (:408).
  - Margins can be negative: they are FITTED, and Poland's households' margin is the 2023 freeze.
  - The dispatched mix sums to 100 when total generation is positive (:708-718), but no check asserts it - only the seed mix is checked (`Ed/EnvironmentFamilyDiagnostic.cs:38-44`).
- **Macro.**

  | Quantity | Bounds | Where |
  |---|---|---|
  | U | [0, 30] | MacroSystem.cs:253 |
  | Inflation | [0, 30] | :483, :510 |
  | PriceLevel | >= 0.0001 | :581 |
  | GDP | >= 1 | :47 |
  | Rate | [0, 15] | CurrencySystem.cs:18 |
  | Currency strength | [50, 200] | :25-28 |
  | Confidences | [0.7, 1.3] | MacroSystem.cs:2444-2446 |
  | Productivity trend | [0, 8] | :2050 |
  | Sector shares | >= 0, no upper bound, no sum to 100 | :1831-1837 |
  | Tariffs | [0, 50] | SimulationManager.cs:581-583 |
  | Dials | [0, 100]; family leave [0, 104]; SWF contribution [-10, 10] | :596-631 |
  | Structural parameters | per `StructuralParameters.All` (natural rate [2, 12]; electricity tax [0, 60]; target [0, 6]; r* [-1, 5]; weights [0, 2]) | |

- **Families.**
  - Coverage: [0, ceiling].
  - Treatable mortality: [10, 400]. Waits: [1, 1000]. Road quality and connectivity: [1, 100]. Intensities: [0.02, 40].
  - Attainment: three shares summing to 100. The seed is asserted to +/-0.15 (`Ed/EducationFamilyDiagnostic.cs:36`). DE and FR seed at 100.1.
- **Demography.**
  - Bands are >= 0 (`PopulationCohorts`; `CohortDemographics.cs:144`). DeathRate >= 0.5 (HealthFamily.cs:97).
  - The runner flags DependencyRatio outside [0, 100] and population growth outside +/-50 per 1 000 (Testing/SimulationTestRunner.cs:891-935).
- **Statistics.** No range assertion on published values or readings. Display clamps hide out-of-range values: a negative gauge fill vanishes, and sector segments past 100 overflow with OTHER at 0.

---

## 5. Stability and tick time

### Cadence

`DaysPerTurn = 365` (SimulationManager.cs:216): a turn is a year.

**Per day, per country** (`AdvanceDay` :312-470):
- publication (:333);
- the credit review;
- sector and infrastructure reversion;
- the cohort day (:370);
- about 15 daily readouts;
- fiscal accrual;
- the event shock;
- the identity (:444);
- potential, Okun and Phillips (:459);
- the price level (:460);
- `History.Append` (:470).

No energy quantity is daily (SPEC S13).

**Per turn** (`AdvanceTurn` :4813):
- fleet landings (:4819);
- the water value (:4820), which clears DE and PL;
- the cohort commit (:4826);
- rates (:4828);
- currency (:4832);
- trade (:4848);
- then `ApplyDomesticPolicy` (:4930), with the families at :4971-4976.

Each country is cleared twice more inside `ApplyDomesticPolicy`: once in the environment step (`PowerCo2PerHead`) and once in the ledger (EnergyLedger.cs:313).

### Heavy work

**Energy, yearly.**
- A clearing is three `ClearBlock` calls. Each builds and sorts a list of up to 600 tranches (EnergyMarket.cs:252-262).
- In Italy and Germany, the hydro-shift bisection can add up to 14 more `ClearCountry` calls (:387-395) - up to 45 sorts per clearing. At no policy the spread holds, so the shift does not run (:372-375).
- `FitAdders` runs once per process per country and is then cached (:137-139, :590-629).

**Energy, per frame** (not per tick).
- `MixSharesNow` re-runs a full clearing inside the plate draw (`GameController.Energy.cs:205`; `PeopleSlips.cs:368`).
- The plant tiles scan `FleetOrders` several times per tile per IMGUI event (EnergyV35 :516-560).
- `FleetOrders` is never pruned, and every id-keyed fleet read is linear in it (EnergyFleet.cs:205-261). A fuzz player's thousands of clicks therefore slow both the page and every clearing, and grow the save.

**Worlds.** In the PLAYED game each boundary also advances:
- the shadow baseline (365 days plus a boundary; `ShadowBaseline.AdvanceTurn` :129-148);
- one except-world per touched dial family (`PolicyImpactLedger.AdvanceTurn` :132-150; up to eleven families).

That is up to thirteen world-years in one burst. This is INFERRED from code; no boundary cost is recorded in COMPLETED.md, so it is the first Phase-1 timing to take. The trajectory dump and the bars drive `SimulationManager` alone, with one world.

**Allocations (INFERRED by the demography sub-inventory).**
- The cohort day allocates about 13-17 small arrays and 2 objects per country per day (`PopulationCohorts.cs:44, 66-67, 86, 286-289, 336, 362`; `CohortDemographics.cs:140, 183-184`). That is roughly 100-130 MB of short-lived garbage over 30 years.
- `PublishDueFigures` allocates two `Enum.GetValues` arrays per country per day (PublicationSystem.cs:151, 156).
- `StatHistory` eviction is `List.RemoveAt(0)`, an O(250) shift, with no steady allocation.
- Published entries and closings grow linearly, about 65 a year per country (`PublishedData.cs:176-192, 246-302`).
- `StatHistory`'s Monthly bucket keeps about 20.5 years, so it truncates a 30-year run. The Quarterly bucket holds about 120 points at 30 years.

**Process statics.**
- Deterministic caches: `EnergyMarket` `AdderCache`, `GapCache`, `SeedSpreadCache`, `_waterValueSeed`.
- Turn state: `_waterValue` and `_swedenDeficitShare`.
- Probes, reset by `WorldFactory.CreateDefault` -> `ResetTurnState` (WorldFactory.cs:1085).
- Fleet scopes: `EnergyFleet.Ambient` and `_recordDepth` (EnergyFleet.cs:216-242).
- `StatTracePanel`'s static selection (`StatTracePanel.cs:69-72`).

A harness that runs several worlds in one process must call `EnergyMarket.BeginTurn(world)` for the world it reads next. `GameController.cs:6126-6130` does this after the shadow advances, and `:905` after a load.

### What a 30-year no-policy run is known to do

Source: `docs/generated/POTENTIAL_PREMISE.md`, generated by `PotentialPremiseDump` on the tree that ran it, with no player and the AI finance ministry live. Values at turn 30:

| Country | potential ÷ seed | GDP ÷ seed | debt (% of nominal GDP) | participation, turn 1 -> 30 |
|---|---|---|---|---|
| USA | 1.722 | 1.517 | 125.7 | 61.8 -> 60.0 |
| Sweden | 1.542 | 1.572 | 25.9 | 65.3 -> 62.4 |
| Germany | 1.220 | 1.171 | 47.3 | 60.8 -> 56.5 |
| France | 1.121 | 1.028 | 94.3 | 55.7 -> 51.9 |
| Italy | 0.879 | 0.816 | 120.6 | 48.8 -> 43.4 |
| Poland | 1.884 | 1.741 | 37.0 | 57.1 -> 48.1 |

- **Population, 2026 -> 2056 (INFERRED from the anchored tables):** Sweden 10.68 -> 12.31, Germany 83.72 -> 82.92, France 69.03 -> 70.58, Italy 58.86 -> 56.25, Poland 36.31 -> 32.38, USA 343.11 -> 365.62.
- **Dependency ratio at 2063 (§195):** USA 39.9, Sweden 44.7, Germany 49.0, France 49.8, Italy 59.4, Poland 63.0.
- **Energy at no policy.** Real energy prices are held to the 1 % clean band except four recorded cells (`Ed/TrajectorySentinelCheck.cs:160-168`): France household LEVEL <= 20 %, France industry <= 6 %, Poland household 3 %, Poland industry 3 %. The ratchet is `EnergyBreachCeiling` = 4 (:154). The USA and Germany read 0.000 (§541's measurement).
- **Static quantities.** Load, retail consumption and trade volumes never grow, so their shares of GDP drift for the whole run.

### Known runaways and their state

| Runaway | What it was | Measured / fixed | State |
|---|---|---|---|
| FT-10 | Energy price runaway: Poland's `EnergyIndustryPrice` past 10^6 at t361 | measured §541; fixed by P-A §545, P-B §552, P-A' §553/§572 | four cells remain |
| FT-6 | Inflation ratcheting until the book left float range at year ~765 | §385, decaying expectations | closed |
| FT-13 | "Century debt" of USA 841 % - a B6 crossing in the dump's own column | measured §582, fixed §584 | closed |
| RF-1 | Road quality drifting to its cap | §354 | closed |
| Health trend | Binds `MinTreatableMortality` within the century (`HealthTrendDiagnostic`; CAT Health §9) | binding years measured before RF-2's re-form; re-measure | open |
| §141 / §195 | Demography runaways | fixed by the anchored substrate | closed |
| FT-11 | Negative scarcity | unreachable at no policy, reachable by retiring the fossil fleet | open |

### Guards the bars already run

- `TrajectorySentinelCheck`: a 20-turn digest plus a century bounds pass on the energy prices (:171-246).
- `EnergyLayerCheck` gates 1-11.
- `EnergyLedgerDiagnostic`, including the stored-vs-recomputed check at ten years.
- `EnergyMinistryDiagnostic`.
- `PreviewParityDiagnostic`. It compares no demographic field.
- The family diagnostics: 20 years, mostly Sweden, cuts through the boundary decision rather than the bill path, NaN-blind.
- `PublicationCadenceCheck`: USA only, 12 years, release counts.
- `SimulationTestRunner.CheckAnomalies` (52 finite checks).

---

## 6. Suspected discrepancies (leads for runs, not findings)

Every item is SUSPECTED. `(recorded)` marks one the record already holds.

### Energy

| ID | lead | where | why it matters |
|---|---|---|---|
| SD-01 | Annual generation does not net curtailment; Sweden's `CurtailedMw` carries exports (recorded §544 (a)) | EnergyMarket.cs:433-451, :574 | mix and generation overstated under a renewable build-out; harness must assert per block |
| SD-02 | Curtailment never lowers a price where a fossil floor runs (floor-tranche convention; recorded §544 failure mode 3) | EnergyMarket.cs:324-331 | the merit-order effect of new renewables is absent; a ruling, not a bug |
| SD-03 | Orders are free; FR/IT gas+nuclear and DE coal+gas have no queue line, so "any size stands" | EnergyFleet.cs:63; EnergyConnectionQueue.cs:202-208, :312-317 | X-01 |
| SD-04 | The player can retire every fossil MW; no reliability veto (the held ministry has one); unserved load costs nothing but the 4 000 price; FT-11 negative scarcity past the nominal ceiling | EnergyFleet.cs:115-133; EnergyMarket.cs:270-276; AiEnergyMinistry.cs:257 | X-02; S2/S3 candidate |
| SD-05 | No UI cancels a queued build (`Withdraw` unused); `FleetOrders` never pruned | EnergyFleet.cs:131, :147 | UX; per-frame and per-clearing cost grows with clicks |
| SD-06 | The page decomposes a cached clearing at the day's price level under a stored headline; cache not invalidated by mid-turn laws (PF-6 OPEN) | GameController.Energy.cs:54-73; EnergyV35 :100-112 | stored != presented |
| SD-07 | `MixSharesNow` performs a full clearing per IMGUI event | GameController.Energy.cs:205; PeopleSlips.cs:368 | UI frame time |
| SD-08 | The reservoir is inert in play (inflow == use without a probe) | EnergyMarket.cs:471-481, :142 | the deficit term never fires |
| SD-09 | Clock mismatches. The held ministry's year = 2026 + turn + 1 (SimulationManager.cs:4883); `CarbonRateStatute`'s BEHG schedule runs on 2023 + turn + 1 (CarbonRateStatute.cs:55, :97). Both differ from the per-country world clock: Italy 2022, Poland 2023, Germany/France/USA 2024, Sweden 2026 | | latent while HELD; the German carbon tile shows a schedule year 3 behind the calendar on a Sweden start |
| SD-10 | Stale generated docs and comments (section 3) | ENERGY_LAYER_PREMISE.md §7, LEVER_MAP.md, EnergyLedger.cs:33-35, MR :4191 | S4 |
| SD-11 | `EnergyMarket.EndTurn` has no caller; the water value is process state (recorded §544 (c)) | EnergyMarket.cs:147, :506 | harness worlds must re-call `BeginTurn` |

### Macro

| ID | lead | where | why it matters |
|---|---|---|---|
| SD-12 | Tariff incidence is counted on both sides. A partner's tariff cuts our exports in full (:236), while our buyers pay our full take through the 100 % pass-through, "the whole take is paid by domestic buyers" (TradeCosts.cs:36 doc). World NX do not sum to zero | TradeSystem.cs:236-237 | model question (ARCHITECT) |
| SD-13 | A per-partner override can never be removed. "Set override" writes live state without a bill | GameController.LawsV35.cs:868-876; GameController.cs:9492-9496; SimulationManager.cs:5824-5835 | doc/code mismatch; bloc rate changes stop reaching that partner |
| SD-14 | The USA's base-tariff hike draws no retaliation (T-5 open), imports are static and the pass-through is looked through | TradeSystem.cs:130-148; SimulationManager.cs:5819-5822 | X-08 |
| SD-15 | Raising the inflation target moves approval's and poverty's inflation reference and the expectations anchor with it | MacroSystem.cs:747, :2271; SimulationManager.cs:5111 | X-09 |
| SD-16 | Regulation and nationalization/deregulation dials cost nothing yet push sector output, so the sector growth term reaches its +0.5 pp cap | MacroSystem.cs:1810-1840, :2003-2015; SectorCouplings class doc | X-07 |
| SD-17 | `CheckFinite` "covers every float" (CLAUDE.md:31) - it is a 52-field hand list | Testing/SimulationTestRunner.cs:796-990 | invariant 3 blind spot |
| SD-18 | Inflation cannot go below 0 | MacroSystem.cs:510 | extreme tight-money runs pin at 0; deflation unrepresentable |
| SD-19 | FT-12 (unweighted currency average) and FT-16b (HICP weights) OPEN | CurrencySystem.cs:125; EurozoneRateSystem.cs:84 | recorded |

### Demography

| ID | lead | where | why it matters |
|---|---|---|---|
| SD-20 | Per-country epochs before 2024 freeze the pyramid. `SubstrateYear` = epoch year + turn and the projection clamps to 2024, so Italy (2022) has 2 frozen years and Poland (2023) 1 - growth 0, migration = deaths - births (INFERRED). The 2024 stock stands in for 2022/23 | CohortDemographics.cs:71, :215-219; PopulationProjections.cs:545-552; WorldClock.cs:223-231 | wrong demography on two starts |
| SD-21 | A held immigration lever compounds: the inflow is proportional to the current population and the anchored ratio carries it forward (about +16 % / -14 % at 30 years at the dial's ends, INFERRED). The docs claim "never compounding" | CohortDemographics.cs:126-129; PopulationCohorts.cs:286-297; CohortDemographics.cs:29-30 | 30-year balance; exploit lever for revenue (X-11) |
| SD-22 | Mid-year lever changes are retroactive: `Step` recomputes the year with the levers as they stand. The day-tick order in play (AdvanceDay -> day tick -> AdvanceTurn) differs from the smoke/film drivers' | CohortDemographics.cs:109-133; GameController.cs:773-791; Testing/PlayerSmoke.cs:73-74 | breaks the post-commit identity; play vs harness divergence |
| SD-23 | Two population figures (start-of-year cohorts vs the daily Lerp) are read by different consumers | PotentialOutput.cs:39-40; TaxBases.cs:79, :83; GameController.PeopleV35.cs:105; GameController.LawsV35.cs:1125 | stored != presented |
| SD-24 | The FT-8 split reads the trend after the commit and participation before it (INFERRED). Fast-ageing countries may book a positive supply excess in early years | SimulationManager.cs:4826 vs :4959; MacroSystem.cs:418-447 | print `SupplyUnemploymentExcess` for turns 0-6 |
| SD-25 | The preview re-runs the current year with the new lever, then commits the old-lever step; `PreviewParityDiagnostic` compares no demographic field | SimulationManager.cs:5304-5311 | preview != turn |
| SD-26 | Lever migrants leak into the births reading (INFERRED) | CohortDemographics.cs:146-147; PopulationCohorts.cs:296-297 | small |
| SD-27 | Youth-unemployment baseline vs the 15-24 rates by age: two unreconciled sources | WorldFactory.cs:332-337; UnemploymentRateByAgeTable.cs:29-35 | small |
| SD-28 | Poland's projection base is 5 % above the seeded stock (INFERRED) | PopulationPyramids.cs:41-49; PopulationProjections.cs | ratios carried onto a revised stock |
| SD-29 | Statutory minimum-wage steps never move the model (Kaitz static by stated premise) | GameController.LawsV35.cs:1166-1177; MinimumWageRates.cs:12-15 | S4/S3 |

### Families

| ID | lead | where | why it matters |
|---|---|---|---|
| SD-30 | **HIGH. Effectiveness cannot see the player's own budget.** The budget bill applies line changes the day it passes (SimulationManager.cs:1548, :1562-1565); the boundary captures "requested" AFTER indexing the already-changed line (:6252-6255); the player's boundary decision carries no line changes (GameController.cs:6277-6299). So allocated == requested and the ratio == efficiency every year for the player's country. Health waits then move only with the minister (HealthFamily.cs:242); the "Underfunded" chip is efficiency alone. AI books do move (the ministry writes into the boundary decision) | | §336's mechanic does not work for the player; confirm by a run |
| SD-31 | An empty chair is the best minister. Vacant = efficiency 1 (PortfolioEffectiveness.cs:89-97; CabinetSystem.cs:457 "absence is not a penalty"); every candidate is 55-90 (CabinetSystem.cs:424-444); no chair is appointed at the seed (only `GameController.PoliticsV35.cs:456` appoints). Seating any health minister raises treatable mortality x(1/e)^0.3 and waits x(1/e)^0.5 | | design defect, cross-area with cabinet (area 8) |
| SD-32 | Social lines move health outcomes: HealthSocialAffairs aggregates family, sickness and social lines | PortfolioEffectiveness.cs:40-47 | cutting child benefit raises hospital waits |
| SD-33 | Player and AI books index differently. The player's driverless lines (infrastructure, housing) fall per head as population grows, so road quality declines at no policy in a growing country (INFERRED) | SimulationManager.cs:6333-6363 | systematic player-vs-AI divergence |
| SD-34 | The preview clone shares the family seeds "immutable after the seed", but `TrendIndex` and the education wage index are written yearly | SimulationManager.cs:5603-5608; HealthFamily.cs:254; EducationFamily.cs:220-221 | latent; safe only while the preview never calls `AdvanceYear` |
| SD-35 | The trend deviation form is not exactly zero at baseline (reversion lag: treatable mortality ~1.076 x anchor for Sweden): a small permanent drag on life expectancy and participation (INFERRED). CAT Health §9 says "every term is zero" | HealthFamily.cs:248-267 | small |
| SD-36 | Vintage "2025-25" (PeopleSlips.cs:265); stale family docs (HealthFamily.cs:14-18, EducationFamily.cs:13-14, SimulationManager.cs:4971-4972, InfrastructureFamily.cs:65, CAT :104, :240) | | S4 |

### Statistics

| ID | lead | where | why it matters |
|---|---|---|---|
| SD-40 | The Q2 GDP reference period starts 31 March: `end.AddMonths(-3).AddDays(1)` on 30 June. The closing key (1 April) misses, so Q2 releases fall back to the live value on each release date | ReleaseCalendar.cs:113-116, :164-168; PublicationSystem.cs:185-188 | Q2 Revised/Final differ from each other |
| SD-41 | Publication runs BEFORE the day's step, contradicting its own doc. Closings are the previous day's state | SimulationManager.cs:333 vs :351-470; PublicationSystem.cs:136-144 | one-day offset |
| SD-42 | EU inflation describes the previous month (30-day lag) while the comment says it describes its own month and reaches the player sooner | ReleaseCalendar.cs:44-46, :130-136 | doc/code mismatch; §188 |
| SD-43 | US annual statistics miss their closing key (1 October start vs 1 January keys) and use the live fallback every year | ReleaseCalendar.cs:126-127, :170-177 | benign value, wrong path |
| SD-44 | Chart overlays (counterfactual line, law ticks, dashed estimate) are positioned over the full series, not the 50-point page. The graph foot says "LAST 50 YEARS" for a 50-quarter window | GraphRenderer.cs:249, :297-313, :653-655, :913-920; GameController.Statistics.cs:205-221, :376-381 | visible misplacement after ~12 years |
| SD-45 | The fiscal shares' daily-drifting denominator (D-S1) and the deficit warning flipping with it | DerivedStats.cs:150; GameController.StatisticsV35.cs:51, :62 | stored != presented |
| SD-46 | Old saves never get the pre-start record (seeded only into an empty GDP series, INFERRED) | PublicationSystem.cs:51-56, :66 | |

---

## 7. Harness actions: every lever in this slice, with the method the UI calls and its legal range

**Rules for the harness.**

- **Call the UI's entry point and its gate.** `SimulationManager.PlayerMayIntroduce(id[, portfolio], out why)` (:2650, :2670) is checked in the UI for fleet orders, and inside `Introduce*Bill` for bills.
- **Bills resolve after `ParliamentSystem.BillDurationDays` = 21 days** (ParliamentSystem.cs:40). They are voted, may meet Poland's presidential veto, and apply on the day they pass.
- **Only the euro push travels in the boundary decision.** `BuildPlayerDecision` sends `InterestRateChange` (GameController.cs:6277-6299).
- **Three levers are direct UI writes with no `SimulationManager` entry:** fleet orders, the head's appointment, and "Set override". The harness must call the same code the UI calls: `EnergyFleet.Place`, the `CurrentFedChair` assignment, and the `link.PlayerTariffOverride` assignment.

| # | lever | UI site -> method | legal range / gate | exploit lead |
|---|---|---|---|---|
| X-01 | Build capacity | EnergyV35 :546 -> `EnergyFleet.Place(c, k, EnergyConnectionQueue.StepUpMw(c, k, EnergyFleet.StepMw(id), c.CalendarYear), c.CalendarYear, sim.CurrentTurn)`; repeatable per frame | k in {0 coal, 1 gas, 2 nuclear, 4 wind, 5 solar} per `CanOrder`; room per `RoomMw` (infinite where no line); lead 2-6 turns | **SUSPECTED exploit:** free capacity. Unlimited FR/IT gas+nuclear and DE coal+gas; IT wind 168 872 MW and US solar 772 570 MW of queue (EnergyConnectionQueue.cs:160-174). It lowers power CO2 (environment plate), and prices only toward the floor convention |
| X-02 | Retire capacity | EnergyV35 :543 -> `EnergyFleet.Place(c, k, -EnergyFleet.StepMw(id), year, turn)` | capped at the fleet less queued retirements (EnergyFleet.cs:119-124); lands next boundary | **SUSPECTED extreme-policy hazard:** retire all coal and gas (DE, PL, IT, US) or FR nuclear. Blocks go unserved at 4 000 EUR/MWh, the household price spikes, pass-through drives inflation to its 30 clamp, and FT-11 can trigger. No reliability veto |
| X-03 | Energy sector dials (Retail intervention / Subsidy, Market liberalisation / Regulation, Investment planning / Tax credits, State ownership / Deregulation, Research grants) | EnergyV35 :360-405 or LawsV35 :507-553 -> `IntroduceSectorBill(id, SectorPolicyBill{SubsidyLevels, RegulationLevels, TaxCreditLevels, ResearchGrantsLevels, DeregulationLevels})` | 0-100 each (SimulationManager.cs:601-603); gate: head of government | Subsidy 100 displaces the levy one for one down to 0 (EnergyLedger.cs:331-339). Liberalisation shifts margin from industry to households (ratio x (1 - 0.3 x gap)) |
| X-04 | Electricity-tax laws (10) | Laws board -> `IntroduceLawBill(id, new LawBill{LawId, IsRepeal})` | composed statute clamped [0, 60] EUR/MWh; business floor 0.5/1; USA refused (SimulationManager.cs:1868) | stacking the household cuts (-10, -20, -35) zeroes the households' tax; revenue change booked to the budget |
| X-05 | Carbon tax rate | Budget tile -> `IntroduceBudgetBill(id, BudgetBill{TaxLines[CarbonTax]})` | 0 .. `RateCeiling` (~300 USD/t in national currency, carried by P); implement/repeal via `IntroduceTaxProgramBill` (area B) | transport CO2 only; revenue = rate x transport tonnes |
| X-06 | Euro rate push | `GameController.cs:4031` -> `PolicyDecision.InterestRateChange` | +/-0.75 per turn; DE FR IT only | |
| X-07 | Sector dials, all 8 sectors | LawsV35 :507-553 -> `IntroduceSectorBill` | 0-100 | **SUSPECTED exploit:** Regulation 0 and Deregulation 100 in every sector cost nothing. Output gaps rise about +4 pp per sector (0.04 x 50 x 2), the sector growth term saturates at +0.5 pp of potential growth a year (MacroSystem.cs:1984-2015, :2029, :2050), and employment nets to zero (+2 - 2). About +16 % potential over 30 years. Subsidies, tax credits and grants below 50 refund money to the line (SectorCouplings.SupportCost) |
| X-08 | USA base tariff | LawsV35 :766 -> `IntroduceTradeBill(id, TradePolicyBill{NewBaseTariffRate})` | 0-50 % (SimulationManager.cs:581-583); ForeignAffairs gate; USA only | **SUSPECTED exploit:** no retaliation against a base hike (T-5), imports static, the pass-through a one-year level term expectations look through |
| X-09 | Monetary-regime laws | Laws board -> `IntroduceLawBill` | SE PL USA; target base 2 (PL 2.5) + 1 + 0.25 at most by the catalog's deltas; weights 0-2; r* -1..5 | **SUSPECTED exploit:** raise the target. Inflation drifts up with no approval or poverty gap penalty (the reference moves), eroding the nominal debt ratio. Offset: higher nominal rates on new debt |
| X-10 | Head appointment | `GameController.cs:4250` / `GameController.DocketV35.cs:253` (direct write) at election turns (`GameController.cs:3833-3856`) | candidates' bias -1.5..+1.5 (pool `FederalReserveSystem.cs:52-82`) | the most dovish head for growth; the selection blocks the clock |
| X-11 | Immigration policy / Family policy | LawsV35 :183, :188 -> `IntroduceLaborBill(id, LaborPolicyBill{FamilyPolicy, ImmigrationPolicy, ...})` | 0-100 | SD-21 compounding population, so a larger tax base over 30 years (INFERRED) |
| X-12 | Labour dials (minimum wage, family leave, overtime, retraining) | LawsV35 :148-179 -> labour bill | Kaitz 0-100 (disabled SE/IT); 0-104 weeks; 0-100; 0-100 | family leave +0.05 approval a week (MacroSystem.cs:2310; demography sub-inventory) |
| X-13 | Per-partner override | LawsV35 :873 (live write at today's rate) then `IntroduceTradeBill(..PartnerTariffOverrides)` | 0-50 % | retaliation mirrors the excess; the override can never be removed (SD-13) |
| X-14 | Minister chairs | `GameController.PoliticsV35.cs:456` (appoint), :398-400 (dismiss) | 6 portfolios | **SUSPECTED exploit:** leave Health (and every chair) empty, since efficiency 1 beats every candidate (SD-31) |
| X-15 | Budget lines (energy, health, education, infrastructure, housing) | Budget page -> `IntroduceBudgetBill(id, BudgetBill{SpendingPercentChanges / SpendingNominalTargets / SpendingPinChanges})` | per-line band (SimulationManager.cs:739: SeedAmount x Min/MaxSpendingLineAmountRatio) | money paths are area B. Here: the energy line moves the levy at its support share; health lines move coverage and mortality, but not waits (SD-30) |
| X-16 | Pension age | Budget pensions row (`GameController.cs:10542`) -> budget bill `PensionAgeSet/PensionAge` | 60-70 (BudgetBill.cs:74) | PN-1 participation step |

**Observables per run, for the invariants above.**

- Per block and per zone: `EnergyMarket.Clear(country)`.
- The book: `EnergyLedger.Compute(...)`.
- `EconomyState` by reflection (`TrajectoryBaselineDump.StateFields()`).
- Fleet and queue: `EnergyFleet.CapacityMw`, `EnergyConnectionQueue.TakenMw`.
- Cohorts and their step: `country.Cohorts.Counts`, `CohortDemographics.Step`.
- `PortfolioEffectiveness` records: `country.Effectiveness`.
- The shadow's and the except-worlds' countries: `ShadowBaseline.CountryFor`.
- The boundary's timing: a stopwatch around `GameController.AdvanceTurn`, including the shadow and the ledger.
