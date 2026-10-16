# B — The money paths: Phase 0 inventory (gameplay audit)

2026-10-13 · repo `G:\UNITY\Projects\PoliSim` at `1dd5b43f` · read-only (no file in the repo was touched; no Unity, no `dotnet`, no state-changing git).
Brief: `docs/audit/AUDIT_PROTOCOL.md` (§803), area 2 "Money paths: budget acts, tax, pensions, benefits, the fund, debt and fiscal rules, vetoes".

## How this was read

- The fiscal core (the daily book, debt, interest, the fiscal reaction, the fund, the rating, currency, the AI finance ministry, the fiscal rules, the preview's fiscal step) was read by me.
- Three read-only sub-agents of this inventory read the rest, each told to cite only what it read: (B1) budget acts, bills, parliament, vetoes, signing, role gates, the Budget page; (B2) tax, pensions, welfare, spending lines; (B3) the impact and attribution machinery and every money figure the player reads.
- I re-read the load-bearing claims myself: SM:6914-7191, 6141-6209, 6248-6629, 6690-6890, 5998-6052, 4813-5165, 5187-5420 (fiscal parts), 1540-1590, 1172-1231, 2226-2318, 2397-2440, 2620-2735; ES whole; DLR, DA, CRS, AFM, SWF, SWFS, CurrencySystem, EurozoneRateSystem, FiscalRules, DerivedStats whole; PS:115-166, 186-222, 342-353, 464-528, 866-956, 1100-1188; TS:574-602; TB:135-152; MS:146-168, 570-582, 2130-2326; WF:875-900, 955-1000, 1196-1204; GCB:140-210; GC:110-145, 3960-4040, 4235-4256, 6255-6330, 9505-9545, 10187-10196, 10296-10470; SB:53-70; STP:380-495; ScenarioLibrary.cs:56-270; WelfareProgram.cs:17-33; DocketV35.cs:240-258.
- Anything not read is marked INFERRED. Everything in sections 5 and 6 is SUSPECTED: a lead for a run, not a finding (R-T2: no finding rests on reading alone).

**Abbreviations** (all under `Assets/Scripts/` unless a path says otherwise): SM `Simulation/SimulationManager.cs` · MS `Simulation/MacroSystem.cs` · PS `Simulation/ParliamentSystem.cs` · DLR `Simulation/DebtLedgerRecorder.cs` · CRS `Simulation/CreditRatingSystem.cs` · AFM `Simulation/AiFinanceMinistry.cs` · FP `Simulation/FinancePartner.cs` · SWFS `Simulation/SovereignWealthFundSystem.cs` · PIL `Simulation/PolicyImpactLedger.cs` · SB `Simulation/ShadowBaseline.cs` · ES `Data/EconomyState.cs` · DA `Data/DebtAttribution.cs` · BB `Data/BudgetBill.cs` · SPB `Data/StandalonePolicyBills.cs` · SWF `Data/SovereignWealthFund.cs` · TS `Data/TaxSchedule.cs` · TB `Data/TaxBases.cs` · WF `Data/WorldFactory.cs` · PV `Elections/PresidentialVeto.cs` · WC `Elections/WorldClock.cs` · GC `UI/GameController.cs` · GCB `UI/GameController.BudgetV35.cs` · CV `UI/ChamberVerdicts.cs` · STP `UI/StatTracePanel.cs` · FL `POLISIM_FEATURE_LIST.md` · MR `docs/reference/MODEL_REFERENCE.md` (moved verbatim from CLAUDE.md, §579 — historical text, stale where noted) · PSS `docs/specs/POLITICAL_SYSTEM_SPEC.md` · USP `docs/specs/USA_STAGE_PLAN.md` · TAX `docs/specs/TAX_SPEC.md` · BP `docs/generated/BUDGET_PREMISE.md`. A bare § is a `COMPLETED.md` section.

**One fact shapes the whole area.** The player's money levers reach the model only through bills. `GC.BuildPlayerDecision` (GC:6277-6297) puts nothing but `InterestRateChange` into the boundary `PolicyDecision`; tax rates, bands, spending figures, welfare generosity, the fund and the pension age ride `BudgetBill` and land in `PS.ApplyBillResult` (PS:464-528) on the day the bill passes. Only the AI finance ministry, the Finance partner and harnesses write the decision's money fields (AFM:213, 231, 247; FP:233, 256, 276). Several downstream terms still read only the decision (see D-05).

## Top leads for Phase 1 (detail in sections 5 and 6)

1. **D-01, S1 candidate — a softlock on the PF-19 path.** With the clock hold on (the default), a budget window left open for a player who no longer governs holds the clock forever; turning the hold off makes `TryOpenBudgetProcess` return early for good.
2. **D-02 / X-01, S2 — the phantom fund drawdown.** A negative standing contribution keeps paying the budget after the fund is empty: the fund is floored at 0 while the budget books the full negative contribution. One uncontested budget bill can create an empty fund at −10 % of GDP a year in any country.
3. **D-05 / X-03, S2 — the player's spending changes never reach the approval spending term or the category effects.** A passed bill's spending result is discarded; the boundary's decision carries none.
4. **D-04 / X-02, S2 — the income-tax lever near its floor.** `TaxSchedule.YieldRatio` snaps to 1.0 (full seed revenue) where the yield at the lever is ≤ 0.
5. **D-03, S2 — dissolving a fund destroys its assets** (no credit to debt or budget, no ledger entry).
6. **X-04 — cheap approval through welfare.** Generosity is an approval stock with no debt damping: childcare at 100 % is worth about +30 equilibrium points for about 1 % of GDP.
7. **D-06, S3 — the Budget page's Balance tile shows the running accumulator in the first year**, labelled "the seed's standing balance", against §220.
8. **D-07 to D-10, S3 — the "without your policies" line and the impact ledger misattribute in play** (AI-governed shadow in a fresh game, reset on load, scenarios outside it, ledger populated only by the eurozone rate push).

---

## 1. Feature table

Status: BUILT · PARTIAL · NOT BUILT (planned in a spec or row) · SPEC-ONLY (specified as built or required, absent) · CODE-ONLY (in code, named by no spec, row or § found). Countries: SWE, DEU, POL, ITA, USA, FRA; "six" = all of them.

**Count: 97 rows — 77 BUILT, 10 PARTIAL, 7 NOT BUILT, 1 SPEC-ONLY, 2 CODE-ONLY.**

### 1A. The book and the debt (fiscal core)

| ID | feature | player-facing entry (screen/control -> method) | code (file:symbol) | spec authority | status | countries | notes |
|---|---|---|---|---|---|---|---|
| B-01 | The daily fiscal accrual: revenue, spending, balance, debt — every country, every day | none (automatic); read through B-02 | SM:425 (call in `AdvanceDay`); SM:6914-6991 `AccrueDailyFiscalFlows`; SM:7095-7191 `ApplyRevenueAndSpending` (revenue SM:7133, spending SM:7134, balance SM:7135, debt SM:7188) | §22 (R1-R5, F1); §327 (P5-B6: the book in current prices, erosion retired); CLAUDE.md "What the model's numbers ARE"; MR "Fiscal Accounting" (stale) | BUILT | six and every NPC country | Debt clamped to [−1000 %, +300 %] of nominal GDP (SM:506, 691, 7164-7165); no floor at 0 (SM:7139-7160) |
| B-02 | The fiscal period: the plan resolved at the year boundary and spent daily; the closed year as `FiscalTurnReport` | Budget strip and book card (GCB:148-212, 765-800); Statistics via `DerivedStats` | SM:786-917 `FiscalPeriod`; SM:4984-5080 (close and re-plan); SM:4997-5015 (report); SM:1058-1061 `GetLastFiscalReport`; SM:5017-5018 `History.BudgetBalanceAnnual`; SM:6846-6889 (seed period) | §220 (P2-0.4); the report's doc SM:39-56 | BUILT | six | A turn is 365 days (SM:216), the daily slice 1/365 (SM:753); periods and reports ride the save (SaveGame.cs:80, 105) |
| B-03 | `EconomyState.Budget`, the running accumulator (`RunningBalanceSum`) | Budget tile, first year only (GCB:166) | ES:47-57; writers SM:7137 and SM:6735 only | §371 (item 5); §220 | BUILT (a recorded trap) | six | Never reset; "NOT a display figure" by its own doc (ES:48-50); see D-06 |
| B-04 | Interest on debt: a blended effective rate rolling toward the issuance rate at 1/maturity a year; issuance = zone rate (or override) + risk premium × sensitivity; a net creditor pays and earns nothing | none (automatic); trace panel rate rows (STP:421-425) | SM:495-503 (premium 0.02 per point over 60 %, cap 5 points); SM:6703-6707; SM:6764-6770; SM:6785-6797; SM:6799-6829 | §22 (R4); MR "Reserve-Currency Debt Interest Treatment"; the 2026-08-02 net-creditor ruling (SM:6802-6815) | BUILT | six; USA base override 3.3 and sensitivity 0.05 (WF:955-956) | Maturities WF:431-436 (DEU, SWE, ITA estimated) |
| B-05 | Fiscal reaction function: revenue × [0.5, 1.5] by the debt ratio's gap to the comfortable ratio, frozen for the period | none (automatic) | SM:528-535, 573-578; frozen SM:857, 5080; applied SM:7099-7101, 7133 | §18 (the debt-to-zero bimodality); MR "Fiscal Reaction Function" | BUILT | six; comfortable ratios WF:299-304 | Multiplies taxes, the fund draw, tariffs and the electricity tax alike (SM:7114-7133) |
| B-06 | The price level and the nominal book | none (automatic) | ES:19-22, 369; MS:578-582 (daily compounding); SM:460 | §327; CLAUDE.md | BUILT | six | Inflation clamped to [0, 30] (MS:483, 510, 515), so the level never falls; the erosion term is retired (SM:6983, 7184-7187) |
| B-07 | One-time settlements to the debt stock (cabinet options, foreign-policy options, the fund drawdown) | Docket option -> `SM.ResolveCabinetDecision` (SM:1102-1118), `SM.ResolveForeignPolicyMeeting` (SM:1149-1160); drawdown B-76 | SM:6732-6739 `ApplyOneTimeBudgetImpact`; CabinetSystem.cs:599; ForeignPolicySystem.cs:96; scaled by `AuthoredImpactScale.ToCountryBillions` | §22 (R5/F1); §217 (per-country scaling); FT-14 (§587) | BUILT | six | Each observed as a debt-ledger event (SM:1112, 1158, 2283); FT-17 open (D-42) |
| B-08 | The debt ledger: the year's debt change as primary balance, fiscal reaction, interest at issuance, maturity lag, erosion, events and clamp, self-audited | trace panel on the debt chip -> `StatTracePanel.BuildDebtRows` (STP:397-477) | DLR:50-196; DA:51-128; SM:6959-6963, 6979-6990 (daily); SM:5028-5029 (close) | §21 (Step 2, third section); §220 | BUILT | six | Audit logs `ATTRIB:` (DLR:167-185); erosion always 0 since §327 (SM:6983); rows under 0.05 hidden (STP:487-495) |
| B-09 | Sovereign credit rating: derived, reviewed annually on the fiscal-year date, outlook refreshed as quarters close, read off published period closings | rating tile on the desk strip and Statistics (GameController.Statistics.cs:141-147, 767) | CRS:66-111, 126-134, 189-229, 252-274; called daily SM:341; Data/SovereignRating.cs `SovereignRatingState`; PublishedData.cs:247-313 | §9; §13 (Step C4); §586 (FT-15) | BUILT | six (USA discounted through `RiskPremiumSensitivity`) | Nothing in the simulation reads it (SovereignRating.cs doc); gross debt only; its deficit is derived from the debt stock (CRS:227) |
| B-10 | Fiscal rules as display state: the EU's 3 % and 60 % warning ink, Sweden's 35 % anchor notice, each country's national rule as a note | Budget strip, Statistics, slips (GCB:171, 189-192, 788, 1001; GC:8990; GameController.StatisticsV35.cs:62; StatsSlips.cs:32, 57, 101) | UI/FiscalRules.cs:32-81 | §725 (A6); §758 (B6) | BUILT (display only) | EU five; USA neutral by design (FiscalRules.cs:20-21) | No consequence for the player; the EU rule binds only AI governments (B-11) |
| B-11 | The AI finance ministry: every country the player does not govern answers its balance and debt ratio with the player's own levers (EU framework for five; US caps and sequester) | none for NPCs; the player's own AI-governed country gets it as a bill (B-15) | AFM:83-250; SM:4857-4888 (`Apply`, `Observe`, `Withdraw` at the boundary) | §387, §388, §390; FL line 184 | BUILT | six (USA rule distinct) | US trigger: ratio above its seed and rising two years (AFM:181-186) — not the dollar debt limit (B-12) |
| B-12 | US federal debt limit as a dated statute (B7: $41.1 trillion since P.L. 119-21; extraordinary measures; then Congress or a forced cut to revenue) | none | none (only text, FiscalRules.cs:20, 78) | FL PS-6 + SP-6 row; §759; USP US-14, US-24, R-US21 | NOT BUILT | USA | PS-6 parked (H5; AUDIT_PROTOCOL.md:5) |

### 1B. Budget acts, bills, vetoes, role gates

| ID | feature | player-facing entry (screen/control -> method) | code (file:symbol) | spec authority | status | countries | notes |
|---|---|---|---|---|---|---|---|
| B-13 | The player's annual omnibus budget bill: tax rates, income-tax bands, spending figures, welfare generosity, fund terms, pension age | Budget page "Introduce budget bill" (GCB:863-869) or Docket "Budget process" (DocketV35.cs:89-95 -> GC:9591-9594) -> `SM.IntroduceBudgetBill(id, BuildBudgetBillFromDrafts())` | BB `BudgetBill`; SM:1506-1518; SM:1528-1553 `AdvanceBudgetBillDay`; PS:464-528 `ApplyBillResult`; SM:1564-1590 `ApplyBudgetBillSpendingAndSwf`; GC:9793-9858 (private builder) | §5 (5c); §734, §735; PSS §5.2 | BUILT | six, where the player's party leads | Resolves after 21 days (PS:40); one pending per country (SM:1509); the model accepts any day (D-21) |
| B-14 | The budget window: the fiscal-year date, the incoming government's arrival window, the clock hold | Docket item; settings chip "THE BUDGET WINDOW" (GC:1531-1532); hold `BudgetWindowHolds` (GC:1387) | SM:2417-2469 `TryOpenBudgetProcess`; SM:2397-2401 `IsFiscalYearStart`; Data/FiscalYearData.cs:14-21 | §5 (5a); §95 (C-C2); §632 | BUILT | six (USA 1 October, the others 1 January) | Hold on by default (GameSettings.cs:197); PF-19 open (D-01) |
| B-15 | The AI government's budget as a bill in the player's own AI-governed country | Budget status line (GC:9711-9718); Desk effects card (GameController.Desk.cs:1093-1108) | SM:1172-1186 `TableGovernmentBudget`; AFM:114-127 `AsBudgetBill`; SM:1213-1275 `ResolveGovernmentBudget`; SM:4863-4867 | §632 (PS-3e); PSS §5.2 | BUILT | six | Tabled on arrival and on each fiscal-year date (SM:2428-2436); ignores `AiFinanceMinistryEnabled` (D-28) |
| B-16 | Alternative (shadow) budget and the Riksdag frame decision | "Table an alternative budget" (GCB:894-897; GC:9612-9615) -> `SM.TableShadowBudget` | SM:1189-1204; SM:1219-1260 | §632, §633; PSS §5.2 | PARTIAL | SWE only (WC:358-368 `WeighsAlternative`) | Refused elsewhere (SM:1195) and to a junior partner (SM:1197); AI parties table none |
| B-17 | A failed budget: the old budget stands | division title; signing plate | PS:466-470 (−1.5 approval, nothing else); SM:1264-1266 | §632 | BUILT | six | No provisional budget, continuing resolution or shutdown anywhere (B-21) |
| B-18 | Poland: the budget act plus each statute part (rates, pension age, benefit levels, fund rules) as its own vetoable act | if-passed slip (GCB:1043-1073); pending cards (GC:9300-9313) | BB:133-241; PS:910-915 `BudgetActOf`; SM:1297-1321 `PolishStatuteActs`; SM:2734-2750 `PartnerTaxAct` | §768 (D4), §773 (E2), §779 (F6), §798 (US-20) | BUILT | POL (WC:364) | The fund act is uncontested and never vetoed (BB:126-130, stated) |
| B-19 | Poland's presidential veto and the Sejm's 3/5 override | signing plate; slips via `ChamberVerdicts.Veto` | PV:45 (Poland only), :54, :63-78, :91-99, :120-127, :165-184; SM:1452-1476 `PresidentialVetoGate`; Elections/Generated/PolishVetoRates.cs | §757, §761, §778 (F3), §779 (F5) | BUILT | POL | The budget act is never gated (Art. 224); amendments never vetoed (SM:1913) |
| B-20 | US president's veto and two-thirds override | none | none (PV:45 is Poland only) | PSS §6; USP US-21; R-US17 (§802 stopped; H1-H3 §803; rule (c) first run §834: INSTALL, pending E4) | NOT BUILT | USA | Parked by H5 |
| B-21 | US budget through Congress: cloture, reconciliation, continuing resolution, shutdown, the president's pen | none | none (WC:358 marks the USA `Unsourced`) | PSS §6; USP §2.4, US-18, US-22, US-23; F8 (§781: "the game must be able to produce a shutdown") | NOT BUILT | USA | Today the House alone votes the government's bill |
| B-22 | A second chamber for money bills (US Senate, Italy's Senato) | none | none | PSS §4, §10 decision 8; USP US-15 | NOT BUILT | USA, ITA | |
| B-23 | The role gate on every bill, with its portfolio leg | lock captions `DrawLeverLock` (GC:10187-10196); LawsV35.cs:1261 | SM:2632-2642 `PlayerGoverns`; SM:2650-2659 and 2670-2690 `PlayerMayIntroduce`; SM:2753-2763 `PortfolioOfLaw` | §630, §634, §755; PSS §5.2 | BUILT | six | Enforced in every `Introduce*` (SM:1508, 1601, 1661, 1721, 1811, 1865, 2174, 2239, 2329). Budget, labour, sector, drawdown: PM only; tax programme Finance; welfare Health; crime Interior; trade Foreign |
| B-24 | The Finance partner's fiscal stance (the player as a junior partner holding Finance; an AI partner otherwise) | Budget › Revenue "Your fiscal stance" ±2.5 (GCB:306-335) -> `SM.SetFinanceStanceTarget` (SM:2695-2701) | FP:37, 40, 184-282; SM:4872-4888 (boundary); SM:1178-1180 (government bill) | §716, §755 (A2), §768, §773 | BUILT | six; Poland's rate step goes as a tax act (SM:2734-2750) | At most 0.25 point of GDP a year; a tightening raises only income tax and VAT |
| B-25 | A junior partner's say in the budget beyond the Finance stance ("negotiate the budget in cabinet") | none | none found | PSS §5.2; §633 | PARTIAL | six | INFERRED unbuilt beyond B-24 |
| B-26 | The party's vote on a bill as a player choice | none (the stance model casts it; an author votes for its own bill, PS:900-904) | `StanceModel.Stances`; PS:342-353 | PSS §5.2; §630 | PARTIAL (INFERRED; a design question) | six | |
| B-27 | Tax programme bill: levy or remove a tax | Budget › Taxes tile switch (GCB:495-510) -> `SM.IntroduceTaxProgramBill(id, type, isAdd)` | SM:1599-1650; PS:538-565 | §5 (5d); MR "Tax Portfolio" (stale: "immediate") | BUILT | six; veto-gated in Poland (SM:1639) | No approval cost either way (PS:551-565) |
| B-28 | Welfare programme bill: implement or remove | welfare row button (GC:10102-10106) -> `SM.IntroduceWelfareProgramBill` | SM:1659-1710; PS:568-595 | §5 (5d) | BUILT | six | Health portfolio gate (SM:1661) |
| B-29 | Labour bill: minimum wage, paid leave, overtime, retraining, family and immigration policy | Laws › Labour (LawsV35.cs:148-205) -> `SM.IntroduceLaborBill` | SPB:14-23; SM:1719-1800, 2110-2147; PS:608-637 | §5; §737; §756 | BUILT | six; minimum wage inert in SWE and ITA (PS:615, 721) | Paid leave has an approval term and no fiscal cost (MS:2310) |
| B-30 | Crime & Justice bill | none in play (callers only UiScreenshotDriver.cs:1896, 3610) | SPB:26-35; SM:1809-1846 | §5; §738 ("SET BY LAW - NO KNOB") | CODE-ONLY (unreachable in play) | — | Its dial costs land on spending lines (SM:6504-6520) |
| B-31 | Economic-sectors bill, including the energy instruments | LawsV35.cs:474; EnergyV35.cs:419 | SM:2172-2223; dial costs SM:6544-6563 | §5, §739, §748 | BUILT | six | Support cost lands on a spending line (SC-1) |
| B-32 | Trade bill (base tariff and partner overrides, 0-50) and the tariff revenue it sets | LawsV35.cs:766, 816, 841 -> `SM.IntroduceTradeBill` | SM:2327-2388; PS:820-857; `EstimateTradeBill` SM:5533-5552; tariff flow planned at the boundary (SM:4845-4849, 5069-5070), accrued daily (SM:6932) | §5, §92, §741; pass 5 (SM:7068-7082) | BUILT | six | Foreign portfolio gate (SM:2329); tariffs sit inside the fiscal reaction multiplier, outside collection efficiency |
| B-33 | Law bills, including the fiscal-framework, monetary and electricity-tax categories | Laws › Laws (GC:8770) -> `SM.IntroduceLawBill` | SM:1863-1934, 1957-1997; LawCatalog.cs:2055-2190 | §304, §742, §500 | BUILT | competence-gated (SM:1868) | Structural parameters recomputed from base, so laws do not stack (SM:2005) |
| B-34 | Budget preview and estimate: the if-passed panel, the strip's "with the draft" balance, the Desk effects card | GCB:976-1135, 148-216; GameController.Desk.cs:676-756, 1093-1108 | SM:5187-5420 `PreviewTurn`; SM:5197-5202 `PreviewTurnWithBudgetDraft`; SM:5482-5499 `EstimateBudgetBill`; GC:4681-4731 `RecomputePolicyPreview` | §94 (C-C1), §251, §252, §694; §773 (Poland's preview applies the whole draft — accepted) | BUILT | six | Divergences D-11, D-13, D-14, D-38 |
| B-35 | The preview clone, audited by field | none | SM:5587-5756 `ClonePreviewCountry` | §506; CLAUDE.md | BUILT | six | `Editor/PreviewParityDiagnostic.cs` not read |
| B-36 | Chamber and veto projection (cached verdicts) | every verdict surface | CV:89-105, 116-123, 127 | §525 (PF-1), §761 | BUILT | six; veto POL | |
| B-37 | The signing screen: the division, a budget contest, the veto plate, impact rows | Canvas takeover (GC:1817; queue GC:2280-2301) | UI/SigningScreen.cs:78, 463-539 | §749, §633, §761 | BUILT | six | Its impact rows are the draft's (D-25) |
| B-38 | Pending-legislation cards | Politics › Parliament (PoliticsV35.cs:180 -> GC:9284-9389) | `DrawPendingLegislation`, `DrawPendingBillCard` | §761, §773 | BUILT | six | |
| B-39 | Bill costs on approval: a failed bill −1.5; a tax rise 1.5 a point at passage; a law's enactment cost | approval ledger events (SM:1550, 2282) | PS:49, 466-470, 526-527; MS:2144; SM:1986 | MR "Parliament PILOT" (superseded formula); §590 ("counts tax rises only") | BUILT | six | Spending, welfare, fund and pension-age changes cost nothing at passage |
| B-40 | A lost bill puts the cabinet under pressure (F5) | none | PS:1143, 1150; SM:1473 | §779 | BUILT | six | |
| B-41 | Budget votes counted against support agreements | none | SM:2767-2779 (called SM:1542, 1552) | §654 | BUILT | six | |
| B-42 | Pending bills and tabled alternatives ride the save | save and load | SM:2500-2533 (capture); SM:4685-4726 (restore); SaveGame.cs:88-105 | §632; save format 29 (SaveGameService.cs:53) | BUILT | six | |
| B-43 | Each country's budget procedure as data | none | WC:356-368 | §632, §798 | PARTIAL (by design) | DEU, ITA, USA, FRA "unsourced" (stated) | |

### 1C. Tax

| ID | feature | player-facing entry (screen/control -> method) | code (file:symbol) | spec authority | status | countries | notes |
|---|---|---|---|---|---|---|---|
| B-44 | The tax portfolio: twelve tax types as lines, seeded per country, placeholder rates on unlevied lines | Budget › Taxes tiles | TaxType.cs; TaxLine.cs; WF:1112-1115 (placeholders: excise 8, property 1, wealth 1.5, stamp 1); WF:1163-1204 `SeedTaxLines` | MR "Tax Portfolio" (stale: Sweden at 52) | BUILT | six (tariffs never a line, TaxType.cs:3-7) | |
| B-45 | The tax rate lever (an absolute rate per line) | tax tile slider MinRate..MaxRate (GCB:520-522) -> bill.TaxLines (GC:9803) -> `PS.ApplyBillResult` clamps and sets (PS:473-489) | `TaxTypeRateRanges` (TaxLine.cs:57-97); AI path `SM.ApplyTaxRateChanges` (SM:5857-5883) | MR "Tax Rate Ranges"; §734 | BUILT | six; Poland's rates in their own tax act | Maxima: income 70, corporate 50, VAT 30, payroll 75, capital gains 50, sales 30, excise 30, property 10, estate 60, wealth 5, stamp 30, carbon its ceiling; minimum 0 |
| B-46 | The revenue engine: rate × a sourced base that follows its driver and the price level | chip "RAISES X A YEAR AT THE STANDING RATE" (GCB:483-484, 532) | TB:97-114 `Base`, TB:117, 135-152; `TaxBaseTable`; SM:6182-6197 `GetTotalTaxRevenue` | §282 (D-16 a); §319 (P5-B3); §327; §517 (F4-5); BP §2 | BUILT | five sourced; the USA only its income row | No Laffer term; FT-5 the one behavioural brake (B-53) |
| B-47 | Collection efficiency (the coverage bridge, may exceed 1) plus the Finance minister's competence bias | none | SM:7102-7108, 7133; seeds WF:283-292 | §282; MR "Tax Collection Efficiency" (stale: 0-1) | BUILT | six | USA 0.7531 … POL 1.3086 |
| B-48 | Income tax as its statute's shape (bands, formula, layers), scaled by one yield ratio | Budget › "Income tax · the statute" card (GCB:279-296) | TS:137-160; TS:580-585 `YieldRatio`; TS:595-601 `EffectiveRate`; lever seeded at the average rate on taxed income (WF:1135-1153) | TAX (D-3, S2 b); §488 (F4-2), §514 (F4-3), §517 (F4-5) | PARTIAL | SWE, DEU, FRA, POL, USA by statute; ITA billed and flat at 43 (TS:151-152; WF:230) | See D-04 |
| B-49 | Income-tax band (sub-row) rates | sub-row dials 0-50 (GC:9994-9997) -> bill.BracketRates (GC:9805-9811) -> PS:167-178, 493-501 (clamped 0-100) | `TaxLine.BracketRates`; TS:201-236 | §490 (F4-4, board 15b); TAX S4 | BUILT | SWE, DEU, FRA, POL, USA | The hike counts in effective-rate points (PS:497-500) |
| B-50 | The earned income credit (jobbskatteavdrag) inside Sweden's yield | readout on the statute card | EarnedIncomeCredit.cs:45 (`Live = true`), :48 (scale 100); TS:424, 444-471 | §534 (P6-E1/E2), §538 | PARTIAL | SWE only | Its own lever is owed (§534) |
| B-51 | Carbon tax per tonne, moved by each country's statute | carbon tile (step = ceiling/100, TaxLine.cs:156) | TB:137-142; `CarbonRateStatute.AdvanceYear` (CarbonRateStatute.cs:105-129), called SM:4935 | §464, §467, §471 | BUILT | SWE indexed, DEU scheduled, FRA nominal; ITA, POL, USA not levied | The statute calendar counts from 2023 + turn + 1 (stated) |
| B-52 | Household tax burden into consumption (C-N4 / FT-3) | none | MS:146-168 `HouseholdTaxBurdenShare`; MS:188-193; `Country.BaselineTaxRates` (WF:1202-1203) | §377; TAX S7 | BUILT | six | Defects suspected (D-32); excludes corporate tax and tariffs (MS:152) |
| B-53 | Labour-tax participation term (FT-5) | none | MS:849-857 (clamped ±5) | §372, §396 | BUILT | six | |
| B-54 | Electricity-tax change booked as a planned flow (named only; energy slice) | Energy pages | SM:4975, 5075, 6934, 7133 | §500 (EN-7b) | BUILT | five (USA not offered) | Inside the fiscal reaction multiplier, outside collection efficiency |
| B-55 | Italy's three-layer income tax | none | none (TS:151-152 flat) | TAX §3 (E-6) | NOT BUILT (billed) | ITA | |
| B-56 | US payroll-tax cap, France's CSG/CRDS, Poland's ZUS contributions | none | none | TAX §3 | NOT BUILT (billed) | USA, FRA, POL | Poland's solidarity levy IS built (TS:155) though TAX §3 still bills it |

### 1D. Spending lines, welfare, pensions, labour

| ID | feature | player-facing entry (screen/control -> method) | code (file:symbol) | spec authority | status | countries | notes |
|---|---|---|---|---|---|---|---|
| B-57 | Spending lines: each country's own budget structure (US federal lines, Sweden's expenditure areas, statute budgets for DEU/FRA/ITA/POL) | Budget › Spending tiles (GCB:610-731) | SpendingLine.cs; SpendingCategory.cs; WF:900-946, 1307-1919; totals SM:6618-6629 | MR "Detailed Spending Portfolio"; §735 | BUILT | six | |
| B-58 | Line indexation: driver × prices; for the AI, × real growth (driverless lines) or × the real wage (caseload lines) | "DRIVEN BY … NEXT YEAR" (GCB:720-722) | SM:6331-6372 `IndexSpendingLines`; SpendingDrivers.cs:40-100 | §314 (P5-B2), §327, §370, §384; BP §1 | BUILT | six | |
| B-59 | The player sets a line's figure (a nominal target) | slider standing × (1 ± 15 % mandatory / 30 % discretionary) (GCB:676, 697-707) -> bill.SpendingNominalTargets (GC:9814-9822) -> SM:1564-1567 -> SM:6583-6616 | `ComposeLine` / `ClampToSeedRange`, [0.2×, 3×] of the seed amount (SM:6407-6413, 733-740) | §314, §316 (P5-B5), §735; MR "Percentage-Based Spending Sliders" | BUILT | six | The yearly range is UI-only (D-20); the cash moves only at the next boundary (SM:5051, 6924) |
| B-60 | Pin a line against its indexation | no UI control | `SpendingPinChanges`; SM:6366, 6602-6605 | §314 (the P5-B2 row: "The player may pin a line") | SPEC-ONLY (for the player) | six | Only harnesses set it (Editor/LeverLivenessCheck.cs:416-417) |
| B-61 | Dial costs on spending lines (justice, border, sector support, energy subsidy), outside the seed band (SC-1) | the labour, crime, sector and energy dials | SM:6389-6446, 6504-6563 | §497, §503, §504 | BUILT | six (DEU has no energy line) | Held at zero (SM:6411, 6430-6438) |
| B-62 | Government consumption in the national accounts, scaled by k (T-3 form A) | none | Country.cs:145-151, 247-252; MS:230-231; SM:444 | §507; FL T-3 row | BUILT | six | The book is unscaled; USA k = 2.2426 (per FL) |
| B-63 | Unemployment benefits (automatic stabiliser) | none | SM:6696-6700 | MR "Fiscal Accounting" | BUILT | six | |
| B-64 | Welfare programme generosity (0-100) and its cost against the seeded portfolio | welfare slider (GC:10115-10121) -> bill.WelfarePrograms (GC:9824-9831) -> PS:503-512 | WelfareProgram.cs:17-33 (cost at 100 %: UBI 18, negative income tax 8, means-tested 6, universal healthcare 10, housing 1.5, childcare 1 % of GDP); SM:6141-6170 | MR "Welfare Policy" (partly stale); §188 §8 | BUILT | six (seeded from OECD SOCX, WF:657-685) | The cost can be negative (below the seed) |
| B-65 | Welfare effects: an approval stock every year, poverty, confidence | none | MS:2194-2228, 2304; MS:1420-1423, 2564 | MR "Welfare Policy" | BUILT | six | No debt damping (X-04) |
| B-66 | The pension age as its statute moves it | Budget › Pensions card (GC:10523) | PensionAgeStatute.cs:96-133 | §489 (PN-1), §491, §509 | BUILT | six | |
| B-67 | The pension line follows the headcount at the statutory age | the line's age label | SpendingDrivers.cs:28, 87-92; PensionPayment.cs:27-41 | §520 | BUILT | six | |
| B-68 | The pension age dial on the budget bill (60-70, month grain) | GC:10536-10549 -> bill.PensionAge (GC:9845-9855) -> `Country.PensionAgeOverride` (PS:524) | BB:68-74, 151-157; weighed in the vote PS:149-163 | §590; FL PN-1 | BUILT (stated gaps, FL line 145) | six; Poland its own statute act | No approval term (§590) |
| B-69 | The participation response to the pension age | none | PensionParticipationResponse.cs:73-96; MS:867-874 | §596, §599 | BUILT | six (FRA, DEU measured; four a median) | |
| B-70 | The pension payment readout (a replacement rate) | GC:10481-10496 | PensionPayment.cs:52-74 | §518, §519 | BUILT | six | |
| B-71 | Minimum wage (percent of the median wage) | Laws › Labour, 0-100 | SM:2110-2147, 5923-5931; MinimumWageRates.cs | §724, §756 | BUILT | DEU, FRA, POL, USA; off in SWE, ITA | No budget line of its own |
| B-72 | Department effectiveness (allocated ÷ requested × the minister's efficiency) | minister card; slips | PortfolioEffectiveness.cs; SM:6261 | §336, §337 | BUILT | six | Readout |
| B-73 | Spending's approval term and category effects | none | MS:2263-2326 (approval); MS:2519-2538 (category); MS:1907 (infrastructure); fed by SM:6665-6688 | MR "Detailed Spending Portfolio Phase 2" | PARTIAL | six | Never fed by the player's bills (D-05) |

### 1E. The fund

| ID | feature | player-facing entry (screen/control -> method) | code (file:symbol) | spec authority | status | countries | notes |
|---|---|---|---|---|---|---|---|
| B-74 | The fund created, dissolved and set (contribution −10..+10 % of GDP a year; four asset weights) on the annual budget bill | Budget › Fund: Create/Dissolve (GC:10331-10335), contribution row (GC:10387-10396), weights (GC:10421-10462) | SM:1569-1590; SM:5998-6035 `ApplySwfPolicyChanges`; ranges SM:614-621 | MR "Sovereign Wealth Fund", "… Expansion to All Six Countries", "… Drawdown Mechanic"; §5 (5c) | BUILT | six (seeded for SWE 195 and FRA 27, WF:879-897) | Excluded from the vote by stated design (PS:119-123); see D-02, D-03 |
| B-75 | The fund's daily flows: the contribution out of the budget, the market return in, the structural 3 % draw into revenue, the floor at 0, the cap at 300 % of GDP | Fund rows "Assets", "Contribution", "Returns" (GC:10344-10348) | SM:6929, 6938-6951; return drawn once a period (SM:5055-5057; SWFS:118-128), average estimate SWFS:131-141 | MR "Sovereign Wealth Fund Return-Model Rebalance"; SM:645-676 (Norway's rule, 3 %) | BUILT | where a fund exists | The draw is not recorded on the report (D-12); see D-02 |
| B-76 | The emergency drawdown bill (one-off, 0-25 % of GDP, capped by the assets) | Budget › Fund dial (GC:9521) -> Introduce (GC:9538-9541) -> `SM.IntroduceSwfDrawdownBill` | SPB:90-102; SM:2237-2318; PS:1153-1162 | §23 A2 (ruled and built 2026-08-02, `b1c077f`) | BUILT | where a fund exists | PM only (SM:2239); vetoable in Poland (SM:2275); booked through `ApplyOneTimeBudgetImpact` (SM:2317) |
| B-77 | The fund's domestic allocation | none (dial retired) | SWF:31-55 (field kept; nothing reads it) | §111 (C-N3, C-N6); §315 (P5-B4) | PARTIAL (consumer billed) | — | Carried unchanged on the bill |

### 1F. The monetary path to the interest bill

| ID | feature | player-facing entry (screen/control -> method) | code (file:symbol) | spec authority | status | countries | notes |
|---|---|---|---|---|---|---|---|
| B-78 | Policy rates: a central-bank head (USA, SWE, POL) follows the Taylor rule plus the head's bias; the euro trio share a blend weighted by household spending plus the player's bounded push | Central bank page; eurozone "National rate push" ±0.75 (GC:4031) -> `_interestRateChangeInput` -> `BuildPlayerDecision` (GC:6277-6281) | CurrencySystem.cs:45-86; EurozoneRateSystem.cs:50-127; FederalReserveSystem.cs:149-157 | §111 (S-18 corrected); §330; §585 (FT-16); MR "Federal Reserve", "Eurozone Rate Voice" | BUILT | six | Rate bounds [0, 15] (CurrencySystem.cs:15-18) |
| B-79 | Appointing the central-bank head (rate bias −1.5..+1.5) | "Appoint" (GC:4248-4253; DocketV35.cs:250-255) | the UI writes `Country.CurrentFedChair` directly; pool FederalReserveSystem.cs:52-80 | MR "Federal Reserve"; §242 (board 5f) | BUILT | USA, SWE, POL | No simulation-side method; UI-only gate (D-40) |
| B-80 | The "policy rate change" dial for an own-currency country with no head | GC:3985-4006 (±2, GC:117) | — | §111 (S-18: unreachable, to retire) | CODE-ONLY (unreachable) | none (every own-currency country has a head) | |
| B-81 | Currency strength (the rate gap to partners; trade only) | none | CurrencySystem.cs:108-140 | FL FT-12 (open: unweighted partners) | BUILT | USA, SWE, POL | No direct fiscal flow |

### 1G. Impact, attribution, presentation

| ID | feature | player-facing entry (screen/control -> method) | code (file:symbol) | spec authority | status | countries | notes |
|---|---|---|---|---|---|---|---|
| B-82 | The shadow baseline, "without your policies" | Statistics › Domestic, dashed line on six charts | SB:53-70, 93-118, 129-148; built GC:673, 911, 2178; advanced GC:6126; GraphRenderer.cs:249, 293-322 | §103-§105 (C-C9 / P-G1); §618 R5; §708 | PARTIAL | six | D-07, D-08, D-09, D-19 |
| B-83 | The impact ledger, "your policies did X", with leave-one-out forks | Statistics › Domestic › Your policies -> `PolicyImpactLedger.LinesFor` | PIL:49-290; GameController.Statistics.cs:508-592; GC:678, 913, 2180, 6112 | §106 (C-C10 / P-G2); §728 | PARTIAL | populates only for DEU, FRA, ITA; "—" for SWE, USA, POL (D-10) | Forks run no day ticks (D-45) |
| B-84 | PF-18: each fork steps to the boundary its own manager owes | none | not built | FL PF-18 | NOT BUILT | six | |
| B-85 | The approval ledger with its money terms (tax-hike penalty, spending effect, welfare effect, paid leave) | Desk approval rows; trace panel; scenario epilogue | ApprovalAttribution.cs:39-89; ApprovalLedgerRecorder.cs:36-119; MS:2344-2400 | §21 (Step 2) | BUILT | six | "Spending changes" ≈ 0 for the player (D-05) |
| B-86 | Published statistics and the release calendar | no money surface (no money statistic is published, PublishedData.cs:8-11) | PublishedData.cs; PublicationSystem.cs:140-208; ReleaseCalendar.cs:23-70 | §6, §12 | BUILT | six | Published GDP has no player-facing reader (§105 RIDE-1) |
| B-87 | The public-finances card: tax burden, spending, deficit, primary deficit as % of GDP | Statistics › Domestic | GameController.StatisticsV35.cs:46-136; DerivedStats.cs:51-94 | §728; §725 | BUILT | six | The closed year over today's GDP (D-18) |
| B-88 | Headline readings (GDP, debt, debt ratio, closed-year balance, rating) on the desk strip and More readings | Desk; Statistics | GameController.Statistics.cs:104-163 | §220; §728 | BUILT | six | |
| B-89 | The Budget strip (balance, debt, revenue, spending) | Budget | GCB:148-212 | §734; §725; §252 | BUILT | six | First-year fallback (D-06) |
| B-90 | The closed year's book card | Budget | GCB:765-800 | §735; the report's doc (SM:39-56) | BUILT | six | Does not reconcile (D-12) |
| B-91 | Option cost lines (cabinet and foreign policy) | Docket -> `DrawCabinetOptionCostLine` (GC:9141-9150) | AuthoredImpactScale.cs:28-37 | FT-14 (§587) | BUILT | six | Display and booking share one function |
| B-92 | Policy-screen stat chips | every policy screen | PolicyScreenStats.cs:109-136; PolicyScreenStatsRenderer.cs:243 | Step A (PolicyScreenStats doc) | BUILT | six | "GDP" there is real (D-17) |
| B-93 | The scenario epilogue | scenario verdict sheet -> `BuildScenarioEpilogue` (GC:6875-6935) | GC:6875-6935 | §21 (Step 3) | BUILT | ITA, SWE scenarios | The GDP arc is real, labelled "B" (D-17) |
| B-94 | The economic vote reads the government's record | elections; Campaign HQ | EconomicVote.cs:150-163; PerceivedPerformance.cs:189-230 | §638 (PS-3k); §709; §752 | BUILT | six | Published unemployment and inflation only — no money figure (X-07) |
| B-95 | The Policy Web's causal chords from the ledgers' terms (erosion and interest included) | Policy Web | PolicyWebRenderer.cs:85, 305, 504-520 | COMPLETED.md line 2508 (R-K1); §21 | BUILT (per the record; not read in full) | six | The "erosion" chord now acts through nominal GDP, not a stock term |

### 1H. Scenarios with fiscal premises

| ID | feature | player-facing entry (screen/control -> method) | code (file:symbol) | spec authority | status | countries | notes |
|---|---|---|---|---|---|---|---|
| B-96 | "Inherit the Fund": Sweden a net creditor (debt −50 % of GDP) with a fund of 60 % of GDP | scenario start -> `StartScenario` (GC:2048-2059) | ScenarioLibrary.cs:91-167 | §22 (R3's creditor branch); the scenario's doc | BUILT | SWE | The premise still names the retired erosion term (D-41); the objective reads nominal debt (ScenarioLibrary.cs:143) |
| B-97 | "Italy Debt Crisis": debt at 165 % of GDP | scenario start | ScenarioLibrary.cs:225-300 | §22 (the Italy debt measurement report) | BUILT | ITA | Absent from the shadow baseline (D-09) |

---

## 2. Specified but missing

| # | item | authority | where it shows |
|---|---|---|---|
| M-01 | US federal debt limit as a dated statute | FL PS-6 + SP-6; §759 (B7); USP US-14, US-24, R-US21 | B-12 |
| M-02 | US veto and two-thirds override | PSS §6; USP US-21; R-US17 (§802; H1-H3 §803) | B-20 |
| M-03 | US budget procedure: cloture, reconciliation, continuing resolution, shutdown | PSS §6; USP §2.4, US-18, US-22, US-23; F8 (§781) | B-21 |
| M-04 | Money votes in a second chamber (US Senate, Italy's Senato) | PSS §4, §10 decision 8; USP US-15 | B-22 |
| M-05 | US roles for the party out of the White House (US-19): today a REP player at the US start reads IN OPPOSITION with no money action | USP §2.5 item 3, US-19 | — |
| M-06 | An opposition shadow budget outside Sweden | PSS §5.2; §632 ("other procedures data when they land") | B-16 |
| M-07 | A junior partner negotiating the budget in cabinet | PSS §5.2 | B-25 |
| M-08 | A pin control for the player | §314 (the P5-B2 row) | B-60 |
| M-09 | The earned income credit's own lever; earned-income relief for the other five (P6-E2, billed) | §534 | B-50 |
| M-10 | Italy's income-tax layers (E-6) | TAX §3 | B-55 |
| M-11 | US payroll cap, France's CSG/CRDS, Poland's ZUS | TAX §3 | B-56 |
| M-12 | PN-1's open items: a bill's age freezes the statute's later steps; lowering reuses a measured effect; Germany's effect is women-only; the added older workers are not in the natural rate's age mix; the AI reads the labour step as potential growth; no check drives the bill route end to end; the payment readout's one-year jump; "the approval cost counts tax rises only" | FL line 145 (PN-1); §590 | B-68 |
| M-13 | PF-18's fix: each fork steps to the boundary its own manager owes | FL PF-18 | B-84 |
| M-14 | PF-19's fix: the election-night install does not close a window the player no longer governs (`CloseBudgetWindowIfNotGoverning`, SM:3157-3162, is not called there) | FL PF-19 (§783's review) | D-01 |
| M-15 | FT-17: measure how often the debt clamp splits `Budget` from the debt, then rule | FL FT-17 (§587) | D-42 |
| M-16 | A dash before the first close, never a number no year has computed | §220 (P2-0.4) | D-06 |
| M-17 | "The shadow equals the recorded no-policy baseline for an untouched game" | §103 / §105 assertion 3 | D-07 |
| M-18 | The impact ledger "attributed to enacted changes" | §106 (P-G2) | D-10 |
| M-19 | A surface showing tariffs or the electricity tax says "of which" | the report's doc, SM:22-23, 50-53 | D-12 |
| M-20 | S-18's remainder: retire the unreachable rate branch, correct `InterestRateChange`'s doc | §111 | B-80; D-41 |
| M-21 | The fund's domestic-allocation consumer (a sourced domestic vs international return spread) | §111 (C-N6) | B-77 |
| M-22 | Sweden's corporate and capital-income rates are "NOT VERIFIED … NOT ASSERTED" yet seeded (corporate 20.6, capital gains 30) | TAX §2; WF:239-241 | B-44 |

---

## 3. Present but unspecified

| # | behaviour | code location |
|---|---|---|
| U-01 | A negative standing contribution against an empty fund still books its full amount in the budget (the phantom drawdown). The drawdown mechanic's own record claims the opposite | SM:6929, 6940-6943, 7134; MR "Sovereign Wealth Fund Drawdown Mechanic" |
| U-02 | Dissolving a fund discards its assets: no credit to debt or budget, no ledger entry | SM:1569-1573 |
| U-03 | A fund can be created at zero assets in any country and set to −10 % in the same budget bill (MR still describes creation as an immediate action, USA-first) | SM:1575-1589; MR "Sovereign Wealth Fund" |
| U-04 | A budget bill is accepted on any day; the window is a UI check only | SM:1506-1518; GCB:865; GC:9585 |
| U-05 | Nominal spending targets are bounded only by the seed band in the model; the ±15/30 % yearly range exists only as the UI track | SM:6598-6601; GCB:676-683 |
| U-06 | The gate accepts bills for other countries; they never resolve in play (only the player's country is ticked) | SM:2654; GC:783 |
| U-07 | The government's own budget bill ignores `AiFinanceMinistryEnabled` | SM:1175 against SM:4860, 5293 |
| U-08 | A party with no seats can table an alternative budget | SM:1189-1204 |
| U-09 | The UI duplicates the model's private range constants ("must match") | GC:110-153 against SM:581-631, 694-702 |
| U-10 | The UI's bill builder is private; the film driver reaches it by reflection | GC:9793-9858; UiScreenshotDriver.cs:2035 |
| U-11 | `YieldRatio` returns 1.0 (full seed revenue) when the yield at the lever is ≤ 0; `EffectiveRate` returns the bare lever | TS:584, 599 |
| U-12 | Band rates are unbounded in effective terms (the lever's shift is added to each band; sub-rows allowed 0-100) | TS:253, 306-307; PS:175 |
| U-13 | Removing a household tax drops it from both sides of the burden comparison | MS:151 |
| U-14 | Lazy reads write model state (INFERRED benign) | TB:108-112; TS:544, 556-561 |
| U-15 | `BudgetDraftSignature` omits `SpendingPinChanges` | GC:9744-9782 |
| U-16 | `GetBillDirection` weighs the unclamped requested rate | PS:203 |
| U-17 | The first-year label "THE SEED'S STANDING BALANCE" over the accumulator | GCB:166, 183 |
| U-18 | The Desk "Net budget" arrow's range is 2 % of real GDP | GameController.Desk.cs:69, 716 |
| U-19 | The next-year lines' share projection (`LinesShareOfGdpNextPercent`) | GameController.Statistics.cs:93-100; GCB:200 |
| U-20 | After a load the counterfactual is a fork of the loaded game | GC:908-913 |
| U-21 | The central-bank head is appointed by a UI write to `Country.CurrentFedChair`, with no simulation method | GC:4250; DocketV35.cs:253 |
| U-22 | The eurozone rate push is gated only in the UI (`DrawLeverLock`); the model applies any decision's push | GC:4031, 10187-10196; EurozoneRateSystem.cs:88-110 |
| U-23 | `GetIdentityGovernmentConsumption` returns NaN before the first period exists (diagnostic reader only) | SM:5504-5505; Editor/PreviewParityDiagnostic.cs:232 |

---

## 4. Invariant map

### 4.1 Invariant (1): debt change = deficit

**Units and signs.**
- The book is nominal "book US$ billions" in current prices; the macro block is real; `NominalGdp = GDP × max(0.0001, PriceLevel)` (ES:22). Nominal with nominal, real with real (CLAUDE.md).
- `BudgetBalance` + = surplus (SM:50, 7135). `DerivedStats.DeficitPercentOfGdp` + = deficit (the one flip, DerivedStats.cs:76-79).
- `DebtAttribution` terms + = raises the debt (DA:76-77).
- `ContributionRatePercent` + = paying into the fund (SWF:23-29); `SwfDrawdownBill.WithdrawalPercentOfGdp` + = taking out (PS:872-884).

**The daily step**, for every country, called from `AdvanceDay` (SM:425 -> SM:6914-6991 -> SM:7095-7191). Each flow is the year's figure × 1/365 (SM:753):

| term | expression | where |
|---|---|---|
| G (discretionary plan) | `period.PlannedGovernmentSpending` | SM:6924 |
| M (mandatory plan) | `period.PlannedMandatorySpending` | SM:6925 |
| UB | `BenefitRatePerUnemployed × Unemployment/100 × NominalGdp` | SM:6699, 6926 |
| I (interest) | `Debt × effectiveRate/100` if Debt > 0, else 0; the rate advanced first | SM:6922, 6816-6828, 6927 |
| W (welfare) | cost(live programmes) − cost(seeded programmes), on nominal GDP; may be negative | SM:6148, 6928 |
| C (fund contribution) | `NominalGdp × ContributionRatePercent/100`, any sign | SM:6051, 6929 |
| T (tariffs) | `period.PlannedTariffRevenue` | SM:6932 |
| E (electricity tax change) | `period.PlannedElectricityTaxRevenue` | SM:6934 |
| D (fund draw) | `TotalAssets × 0.03` after the contribution, the return, the floor and the cap; clamped to the assets | SM:6938-6951 |
| R (theoretical tax) | Σ `TaxBases.Revenue(line)` over implemented non-tariff lines | SM:6182-6197, 7098 |

- `actualRevenue = (R × max(0, CE + FinanceBias) + D + T + E) × FRM_period` (SM:7108, 7133); `FRM_period` frozen at the period's open (SM:857, 5080, 6965).
- `totalSpending = G + M + UB + I + W + C` (SM:7134).
- `balance = actualRevenue − totalSpending` (SM:7135).
- `Budget += balance` (SM:7137).
- `Debt = clamp(Debt − balance, −10 × NominalGdp, +3 × NominalGdp)` (SM:7164-7165, 7188). **The erosion term is retired** (SM:6983, 7184-7187; §327): the debt is not multiplied by anything. The ratio erodes only through nominal GDP (the ratio identity, DA:42-45: Δratio = ΔDebt/GdpAtClose + DebtAtPeriodOpen × (1/GdpAtClose − 1/GdpAtPeriodOpen)).
- Debt writers: SM:7188 (daily), SM:6738 (one-time settlements), ScenarioLibrary.cs:118, 250 (scenario starts, recorded as an event at GC:2059), and the save restore. `Budget` writers: SM:7137 and SM:6735 only.

**The year**, closed where `AdvanceTurn` reaches the country (SM:4984-5029); `R` = `GetLastFiscalReport(id)` (SM:1058); `L` = `Country.FiscalLedgerLastPeriod` (DLR:187):
- (1a) `R.BudgetBalance = Σ balance`, `R.Revenue = Σ actualRevenue`, `R.TotalSpending = Σ totalSpending` (SM:6967-6977, 4997-5015). Assert `R.BudgetBalance ≈ R.Revenue − R.TotalSpending` (float accumulation).
- (1b) Assert `R.TotalSpending ≈ (R.BaselineGovernmentSpending + R.DiscretionarySpending) + R.MandatorySpending + R.UnemploymentBenefitCost + R.InterestOnDebt + R.WelfareCost + R.SwfContribution`. ⚠ The first two are the PLAN, not an accrual (SM:5000-5001); they equal Σ G only over a full 365-day period with no reload mid-period (SM:4986-4990). Tolerance to be set from a run (INFERRED).
- (1c) **The debt identity:** `L.DebtAtClose − L.DebtAtPeriodOpen == L.TermSum + L.EventSum + L.ClampLoss` within `max(0.01, 2e-4 × max(|open|, |close|))`. Already self-audited in code; a failure logs `ATTRIB: … DEBT ledger FAILS its audit` (DLR:163-173). By the recorder's algebra (DLR:83-97), `L.TermSum = −Σ balance` (erosion 0), so `−L.TermSum ≈ R.BudgetBalance`.
- (1d) **The harness form:** `ΔDebt_year == −R.BudgetBalance + Σ L.Events[i].AppliedDelta + L.ClampLoss`. `L.ClampBoundDays > 0` is the only legitimate non-trivial `ClampLoss`; the twin-drift alarm is DLR:180-185.
- (1e) **The two books, daily:** `Δ(GovernmentDebt + Budget) == 0` on every day except (i) a day the debt clamp binds, (ii) a one-time settlement truncated by the clamp (FT-17), (iii) a scenario start, (iv) a save restore. Fields: `EconomyState.GovernmentDebt`, `EconomyState.Budget` (ES:55, 106).
- (1f) **The annual series:** `History.BudgetBalanceAnnual[last] == R.BudgetBalance` (SM:5017). §220 records an Editor instrument for the run-long form (`DomesticMoneyBasisDiagnostic` section 4: debt delta = −Σ annual balances + erosion + settlements + clamp) — named per §220, not read here.

**The fund: no identity can be asserted from the record today.**
- `TotalAssets` moves daily: `+= C`; then `= max(0, assets + PlannedSwfReturn/365)`; then clamped to [0, 3 × NominalGdp]; then `−= D` (SM:6938-6951). It also moves by `−= withdrawn` on a drawdown bill (SM:2316), is created at 0 (SM:1577) or set to null on dissolution (SM:1571), and is seeded by a scenario (ScenarioLibrary.cs:122-130).
- The report carries `SwfContribution` (Σ C) and `SwfReturns` (Σ market return, which goes to the FUND), but **not the draw D** (which goes to the BUDGET). Clamp losses at either bound are not recorded.
- A harness must log `TotalAssets` daily, recompute the expected movement, and flag: (i) any day with `TotalAssets == 0 && ContributionRatePercent < 0` (the budget booked money the fund did not have, D-02); (ii) any day at the cap (the contribution was charged and the asset vanished); (iii) any dissolution with `TotalAssets > 0` (D-03).
- A further non-conservation is by ruling (2026-08-02, SM:7114-7128): the draw, tariffs and the electricity tax ride inside the fiscal reaction multiplier, so a draw of X reaches the budget as X × FRM (0.5 X to 1.5 X). "Debt minus fund" therefore is not a conserved net position even without clamps.

**The preview** (display only, never written back; SM:5167-5186):
- `PolicyPreview.NetBudgetImpact` = the clone's `Budget` delta over one turn-form year (SM:5257, 5418); `RevenueEstimate`, `SpendingEstimate` (SM:5419-5420).
- Assert `NetBudgetImpact ≈ RevenueEstimate − SpendingEstimate`.
- ⚠ The preview's fund step does not add the year's return before the draw (SM:5345-5355 against SM:6942-6943): see D-38.

### 4.2 Invariant (2): every stored value equals its presented value

Class: (i) the stored field itself; (ii) a recorded or lagged copy; (iii) recomputed separately in the UI or by a preview (a candidate divergence point).

| figure | stored | drawn at | class | units / conversion | divergence risk |
|---|---|---|---|---|---|
| Government debt | `EconomyState.GovernmentDebt` (ES:106) | GameController.Statistics.cs:133; GCB:188; GC:10345 | (i) | book US$ bn nominal; `UiFormat.Money` 3 significant figures (UiFormat.cs:84-124) | low |
| Debt-to-GDP | `ES.DebtToGdpRatio` property (ES:369) | GameController.Statistics.cs:134; GCB:190; GC:9019; CountrySelectorScreen.cs:1024; chart head = last quarterly sample (StatHistory.cs:198) | (i); chart (ii) | % | a NaN or non-positive GDP prints 0.0 % (hides NaN) |
| Closed-year balance | `R.BudgetBalance` (SM:5014); `History.BudgetBalanceAnnual` (SM:5017) | GameController.Statistics.cs:153; GC:9025; GCB:169, 789 | (i) | + = surplus | first year falls back to `ES.Budget` (D-06) |
| Balance "with the draft" | preview `RevenueEstimate − SpendingEstimate` | GCB:159 | (iii) | a full turn-form year | differs from the if-passed panel under a rate push (D-13) |
| Desk "Net budget" | preview `NetBudgetImpact` (SM:5418) | GameController.Desk.cs:727 | (iii) | value nominal, arrow range real | D-14 |
| If-passed deltas | `EstimateBudgetBill` (SM:5482-5499) | GCB:995-1002 | (iii) | `NetDelta` from `NetBudgetImpact`, rows from revenue and spending | D-13; the bill's spending lands a boundary later (E-10 under D-13) |
| Deficit, primary deficit, tax burden, spending as % of GDP | report flows ÷ today's `NominalGdp` (DerivedStats.cs:51-94, 143-152) | GameController.StatisticsV35.cs:48-52; GameController.Statistics.cs:758-763; rule inks GCB:167, 786-788 | (iii) | a closed-year flow over a moving denominator | D-18 |
| Revenue, total | `R.Revenue` | GCB:196, 777 | (i) | includes tariffs, the fund draw and the electricity tax, × FRM (SM:7133) | low |
| Revenue, per tax line | `TaxBases.Revenue(country, line)` (TB:117) | GCB:483-484, 532 | (iii) | gross statutory yield; carbon converts national per tonne to book US$ (TB:140-141) | D-15 |
| Spending, total | `R.TotalSpending` (SM:7134) | GCB:197 | (i) | includes `SwfContribution` | the book card does not reconcile (D-12) |
| Spending per line, share, Δ | `SpendingLine.Amount` | GCB:671, 687, 691; GC:10778-10782; GameController.Statistics.cs:85-100 | (i); share and Δ (iii) | nominal ÷ `NominalGdp` | tiles move at passage, cash at the next boundary (D-48) |
| Spending track | the draft `standing × (1 ± r)` | GCB:676, 691 | (iii) | unclamped | lands clamped to the seed band (D-20) |
| Welfare "x % GDP" | `CostShareOfGdp` at 100 % | GC:10129-10131; last-year row GCB:783 | (iii) | the book holds the deviation from the seed, possibly negative | D-37 |
| Tax rates | `TaxLine.Rate` | `TaxRateText` (GC:6205-6209) on GCB:485 | (i) | F2 %; carbon F0 national per tonne | rounding; dial snap steps depend on width (LedgerRow.cs:66-70, 341-344) |
| Income-tax band dials | `TaxLine.BracketRates` | GC:9994-9997 (0-50 track) | (iii) | effective rates can exceed 50 or fall below 0 | D-35 |
| Sweden's average income-tax rate | yield incl. the credit (TS:444-471) | GCB:541-542 -> TS:604-610 | (iii) | printed without the credit | low |
| Interest bill | `R.InterestOnDebt` | GCB:782 | (i) | | low |
| Issuance and effective rates | `DebtAttribution.IssuanceRate*`, `EffectiveRate*` | STP:421, 425 | (ii) recorded | % | trace panel only |
| Debt-change terms | `Country.FiscalLedgerLastPeriod` | STP:397-477 | (ii) recorded | rows under 0.05 hidden (STP:487-495) | visible rows need not sum to the header |
| Fund assets, net position | `SovereignWealthFund.TotalAssets` | GC:10344; net = debt − assets at GC:10346 | (i) / (iii) | | low |
| Fund contribution and returns "this year" | preview `SwfContributionEstimate`, `SwfReturnsEstimate` | GC:4700-4703, 10347-10348 | (iii) | "Returns" = average market return | D-16; the report's own fund fields are never drawn |
| Drawdown "Would release" | booked `min(NominalGdp × pct, assets)` at resolution (SM:2313-2317) | GC:9528-9530 | (iii) | same formula, today's GDP | low |
| Credit rating, outlook | `Country.Rating` (`Rating`, `Outlook`, `HasBeenReviewed`) | GameController.Statistics.cs:141-147, 767 | (i) | standing between reviews | low |
| GDP | `ES.GDP` (real) and `ES.NominalGdp` | nominal: GameController.Statistics.cs:159, GC:9015; real, also named "GDP": PolicyScreenStats.cs:114, GameController.Statistics.cs:532, GC:6880 | mixed | both printed as US$ "GDP" | D-17 |
| Impact lines | `real − none`, `real − except(f)` by reflection (PIL:156-187) | GameController.Statistics.cs:530-592 | (iii) counterfactual | | D-07 to D-10, D-21 under D-45 |
| Shadow gap | the shadow's `History` | GraphRenderer.cs:249, 293-322 | (iii) | | D-19 |
| Option cost | `AuthoredImpactScale.ToCountryBillions` | GC:9145 | (i) same function | nominal | low |
| Pension payment | the line over the headcount at the age in force | GC:10481-10496 | (iii) | headcount moves at once; the line at the next index (stated, §590) | low |
| Signing-plate impact rows | `_effectsAtTurnEnd`, the draft's preview snapshot | GC:6115, 2287-2295; SigningScreen.cs:533-536 | (iii) | | D-25 |

What a harness can read without drawing: every (i) field above; the preview objects (`PreviewTurn`, `PreviewTurnWithBudgetDraft`, `EstimateBudgetBill`); `DerivedStats.*`. The Budget strip and book card compute inline in private IMGUI code and `BuildHeadlineReadings` is private: a harness must replicate those expressions, reflect, or film them. The shadow and the impact ledger live only in `GameController` (`_shadowBaseline`, `_impactLedger`; reached by reflection in UiScreenshotDriver.cs:5833-5841).

### 4.3 Invariant (3): no NaN or Inf

**Guarded divisions** (test `> 0` or clamp with `Max`):
- core: ES:22, 369; SM:454, 5067, 5513, 6794; DLR:60, 83; CRS:162, 215, 220, 225; EurozoneRateSystem.cs:75; CurrencySystem.cs:129; AFM:105, 144, 193, 203, 223, 241; DerivedStats.cs:39, 121, 151; MS:231, 581; SWF:65.
- tax and spending: TB:108-113; TS:298, 470, 584, 630; TaxLine.cs:159; SpendingDrivers.cs:97; SM:6339, 6348, 6354; PensionPayment.cs:58, 71-73; FP:226, 268; MS:164, 855.
- bills and pages: PS:142, 146, 159, 331, 382, 856, 952; PV:71, 77; GCB:154, 534-536, 687, 725-727, 1001, 1092-1094; GameController.Statistics.cs:90, 99, 761-762; STP:375, 455-458; GraphRenderer.cs:297-310.

**Weak or by design:**
- TB:141 divides the carbon line by `EnergyLayer.NationalPerUsd` (INFERRED non-zero for the six).
- SM:5504-5505 returns NaN by design before the first period (U-23).
- `ParticipationRateTable.StructuralRate` returns NaN by design; its reader guards it (MS:898-899).
- The fund's normal draw avoids `Log(0)` (SWFS:87-93); a return is floored at −100 % (SWFS:100).

**The NaN hazard is hiding, not producing:**
- `Mathf.Max(a, b)` returns `a > b ? a : b`, so `Max(0.0001f, NaN)` and `Max(0f, NaN)` return NaN (INFERRED from Unity's implementation; not read). The guards above stop zero divisors, not NaN.
- `GDP > 0f` turns a NaN GDP into a 0 % ratio or an absent figure on screen, and `UiFormat.Money(NaN)` prints a NaN string (INFERRED, UiFormat.cs:86-123).
- A harness must assert finiteness on raw fields, never on derived properties or strings: `ES.GDP`, `ES.PriceLevel`, `ES.GovernmentDebt`, `ES.Budget`, `Country.EffectiveDebtInterestRate`, `SovereignWealthFund.TotalAssets`, every `FiscalTurnReport` field, every `SpendingLine.Amount`, every `TaxLine.Rate`.

**Overflow:**
- Inflation is clamped at 30 % a year (MS:483), so the price level is at most 1.3^t: about 2.6e3 at 30 years, about 2.5e11 at 100 years. Float range (3.4e38) is a century-scale risk only.
- The record puts the book's exit from float range near year 765 (COMPLETED.md line 24511, citing §379); the band tax would then produce Inf − Inf = NaN (TS:326). INFERRED; relevant to century runs only.
- `ES.Budget` is a never-reset float sum: precision, not overflow, degrades over long runs.

---

## 5. Suspected discrepancies (all SUSPECTED — leads for runs, not findings)

Severity is my reading against the protocol's scale (S1 crash/softlock/NaN; S2 a wrong number on a money path; S3 a discrepancy a player would notice; S4 text and polish). "✓" = I re-read the cited lines myself.

| ID | sev. | what | evidence |
|---|---|---|---|
| D-01 | S1 cand. | **Softlock on the PF-19 path.** With the hold on (default), a window open for a player who no longer governs holds the clock; the player cannot introduce, and the Docket draws only the lock. Turning the hold off frees the clock but `TryOpenBudgetProcess` returns early forever, so the new government never tables a budget | GameSettings.cs:197; GC:1387, 9587-9589, 9609; SM:2419-2422 ✓; FL PF-19 |
| D-02 | S2 | **The phantom fund drawdown.** The daily step adds the contribution to the fund, then floors the fund at 0, while the budget books the full contribution. A negative rate on an empty or small fund pays the budget money the fund never held. The doc claims "the fund can't be drawn down past empty" (true for the stock only); the drawdown bill guards this exact case (SM:2291-2295, 2313-2314) and the standing rate does not. MR's own drawdown validation ran both funds at 0 for ~480 turns at −3 % a turn while the debt "settled" | SM:6929, 6938-6951, 7134 ✓; SM:6038-6046 ✓; MR "Sovereign Wealth Fund Drawdown Mechanic" ("Validated" bullet) ✓; the preview mirrors it (SM:5345-5348) ✓ |
| D-03 | S2 | **Dissolution destroys the fund's assets** — `SovereignWealthFund = null`, nothing to debt or budget, no ledger event; fund terms ride the bill uncontested | SM:1569-1573 ✓; PS:119-123 ✓ |
| D-04 | S2 | **The income-tax lever at its floor.** `YieldRatio` returns 1.0 (full seed revenue) when the yield at the lever is ≤ 0, and `EffectiveRate` returns the bare lever (low burden, maximum participation gain). Whether the yield reaches ≤ 0 at or near lever 0 is INFERRED (the lever is seeded at the average rate on taxed income, so the yield is ≈ 0 at 0) | TS:580-585, 595-601 ✓; TB:143-150 ✓; WF:1135-1153 |
| D-05 | S2 | **The player's spending never reaches the approval spending term, the category effects or the infrastructure investment.** A passed bill's `ApplySpendingLineChanges` result is discarded; at the boundary the player's decision carries no spending, so the effective decision maps zeros. AI governments and the Finance partner, which write the decision, do get these effects. The UI tells the player the opposite ("A HIGHER APPROVAL COST FOR ITS SIZE"); the lever-liveness audit drives the decision path the player cannot use | SM:1564-1567 ✓, 6248-6273 ✓, 6583-6616 ✓, 6665-6688 ✓; GC:6277-6297 ✓; MS:2281-2302 ✓; SM:4963, 4967 ✓; GCB:615; Editor/LeverProbes.cs:161, 172 |
| D-06 | S3 | **The first year's Balance tile shows the running accumulator** (seeded 0, plus every day's balance and settlement) under "THE SEED'S STANDING BALANCE". §220 ruled a dash before the first close; §252 says "the seed's own figure", which the accumulator is not after day 1 | GCB:166, 183 ✓; ES:47-55 ✓; WF:130-180 (`budget: 0f`) ✓; §220 ✓; §252 ✓ |
| D-07 | S3 | **In a fresh game the shadow is an AI-governed counterfactual.** `ShadowBaseline(int)` never sets `PlayerCountryId`, so `PlayerGoverns` is false everywhere and the AI ministry writes the player's country's book and indexes its lines the AI's way; the real game, where the player governs, runs neither. An untouched game diverges from its own dashed line (against §103/§105) | SB:53-70 ✓; SM:2632-2642 ✓, 4860-4867 ✓, 6333 ✓ |
| D-08 | S3 | A load re-forks the shadow from the loaded game: the gap and every impact line restart at zero, so save→load→continue shows different attribution from an uninterrupted run (audit area 1) | GC:908-913; SaveGameService.cs:95, 243 |
| D-09 | S3 | Challenge scenarios' starting shocks are not in the shadow (the shadow is built before the deltas land on the real world) | GC:2048 -> 2205 -> 2178, then 2058; ScenarioLibrary.cs:118, 250 ✓ |
| D-10 | S3 | The impact ledger can attribute only the eurozone rate push in play: SWE, USA and POL always show "—"; DEU, FRA, ITA show MONETARY plus an INTERACTION line holding everything else | PIL:137-143, 234-248; GC:6277-6297 ✓; GameController.Statistics.cs:518-529 |
| D-11 | S3 | The preview's approval omits the tax-hike penalty: `PreviewTurnWithBudgetDraft` applies `ApplyBillResult` (which subtracts it) before the clone records its starting approval | SM:5197-5201 ✓, 5256 ✓; PS:526-527 ✓ |
| D-12 | S3 | The closed year's book card does not add up: tariffs and the electricity tax drawn as separate rows though inside Revenue, without "of which"; `SwfContribution` (part of `TotalSpending`) omitted; the fund draw (inside revenue) recorded nowhere | GCB:784-785; SM:22-23, 50-53 ✓, 7133-7134 ✓, 6975 ✓ |
| D-13 | S3 | Two estimates of one draft's balance: `EstimateBudgetBill` runs an empty decision, the strip's "with the draft" runs `BuildPlayerDecision()` (they differ under a eurozone push). Both preview a full year with the whole bill applied today, while the bill's spending reaches the book only at the next boundary | SM:5485, 5495-5497; GC:4686-4693; SM:5051, 6924 ✓ |
| D-14 | S4 | The Desk "Net budget" arrow's range is 2 % of real GDP while its value is nominal (against "nominal with nominal") | GameController.Desk.cs:716, 727 |
| D-15 | S3 | The tax chip "RAISES X" is gross statutory yield; the book applies collection efficiency (0.75-1.31), the minister's bias and the fiscal reaction multiplier (0.5-1.5) | GCB:483-484; SM:7098-7133 ✓ |
| D-16 | S4 | The fund panel's "Returns" is the average market return (to the fund), not the draw the budget receives; inside `ApplyRevenueAndSpending` the parameter named `swfReturns` is the draw | SM:5345-5346 ✓, 7109-7112 ✓; GC:10348 ✓ |
| D-17 | S3 | "GDP" is printed both real and nominal under one name (they part by the price level) | PolicyScreenStats.cs:114; GameController.Statistics.cs:159, 532; GC:6880, 9015 |
| D-18 | S4 | A money figure computed two ways: the closed year's deficit % of GDP divides by today's nominal GDP (it drifts through the year); the rating's deficit is derived from the debt stock on published closings (it includes settlements and clamps) | DerivedStats.cs:143-152 ✓; CRS:189-229 ✓ |
| D-19 | S4 | The dashed shadow line misaligns past one chart page (it passes the full history; the live line is a paged window) | GraphRenderer.cs:34, 214-218, 249, 287-289 |
| D-20 | S3 | The ±15/±30 % yearly range for a spending figure is UI-only; the model clamps a nominal target only to [0.2×, 3×] of the seed, so a direct call can jump a line from 0.2× to 3× in one bill (§316 and MR call the range the model's law) | SM:6590-6601 ✓; GCB:676 |
| D-21 | S3 | `IntroduceBudgetBill` accepts any day; only the UI requires an open window — a harness through the public method can pass a budget every 21 days | SM:1506-1518 ✓; GCB:865 |
| D-22 | S3 | A year's budget window is lost if any bill is pending on the exact fiscal-year date (the date check is one day) | SM:2397-2401 ✓, 2423 ✓, 2431 |
| D-23 | S2/S3 | A budget bill straddling a boundary reverts the boundary's moves: the UI bill names every levied line at its introduction-time rate (undoing the carbon statute's indexation and the Finance partner's rate step), and nominal targets undo a year of line indexation | GC:9797-9803; PS:473-489 ✓; GC:6117, 6315; SM:4872 ✓ |
| D-24 | S3 | Sweden's opposition sees an ordinary FOR/AGAINST count on the if-passed verdict and the pending budget card, but the chamber takes a frame decision in which the government's and its support parties' frames carry | GCB:979-981; GC:9294-9299; SM:1219-1248 |
| D-25 | S3 | The signing plate's and election night's "with against without this act" rows are the draft's preview, snapshotted at the last boundary and attached to every later division — the "standing budget" on election night can be a labour, sector or law division | GC:2287-2295, 6115, 6536-6544; SigningScreen.cs:533-536 |
| D-26 | S4 | The preview fingerprint omits band drafts: a band drag refreshes the panel's rows but not the strip's "with the draft" balance or the arrows | GC:4734-4748 against 9777-9781 |
| D-27 | S4 | For a junior partner in Sweden the Budget page draws an enabled "Table an alternative budget" that the model refuses (logged only) | GCB:876-897; GC:9604-9615; SM:1197 ✓ |
| D-28 | S4 | The government's own bill ignores `AiFinanceMinistryEnabled` (a harness measuring one rule alone still gets the ministry) | SM:1175 ✓ against SM:4860 ✓, 5293 ✓ |
| D-29 | S3 | An adopted opposition alternative's tax rise is charged to the sitting government's approval | SM:1256 -> PS:526-527 |
| D-30 | S4 | A player who tabled an alternative in opposition and becomes PM before the vote votes against it (the government-party check runs first) | SM:1242-1243 |
| D-31 | S4 | Every passed player budget is logged "Budget bill passed (tax hike)" | SM:1550 ✓ |
| D-32 | S3 | The household tax burden misses three moves: levying a new tax at its placeholder rate shows zero burden (`BaselineTaxRates` records every line, so FT-3's "counts from 0" fallback never fires); removing a household tax gives no consumption boost; a band-only rise is invisible (the baseline side reads today's bands) *(the third fixed, §826, GA-2-11: the seed side reads the statute without the line's own sub-rows)* | WF:1202-1203 ✓; MS:151-159 ✓; TB:147-149; TS:563, 569 |
| D-33 | S4 | The tax-hike penalty mixes units: lever points for a rate, effective-rate points for bands | PS:482 ✓, 497-500 ✓ |
| D-34 | S4 | A band rate can never be returned to the statute's figure: a draft back at it is dropped, keeping the standing override | GC:10002; PS:175 |
| D-35 | S3 | Effective band rates are unbounded (the lever's shift is added to each band; sub-rows up to 100): marginal rates below 0 or above 100 % (Poland's top band ≈ 158 %, INFERRED arithmetic) | TS:253, 306-307; PS:175 |
| D-36 | S4 | Raising the pension age cuts the mandatory pension line with no approval term and outside the ±15 % range (it acts through the driver at the next index). Stated in §590; a cost-free lever in play | SM:6364-6370 ✓; §590 |
| D-37 | S4 | The welfare row's "x % GDP" is the cost at 100 % generosity; the book holds the deviation from the seeded programmes; the last-year "Welfare" row prints that deviation, possibly negative, unlabelled | GC:10129-10131; GCB:783 |
| D-38 | S4 | The preview's fund step adds the contribution and clamps but never adds the year's return before the draw; the real day adds it first | SM:5345-5355 ✓ against SM:6942-6943 ✓ |
| D-39 | S4 | The eurozone push is gated only in the UI; the model's `GetMemberPush` applies any decision's `InterestRateChange`, so a stale draft (a player who loses office mid-year) or a direct decision still pushes | GC:4031 ✓, 6277-6281 ✓, 6302-6312 ✓, 10187-10196 ✓; EurozoneRateSystem.cs:88-110 ✓ |
| D-40 | S4 | The central-bank head is appointed by the UI writing the model directly (no simulation method); a harness must write `Country.CurrentFedChair` itself | GC:4248-4253 ✓; DocketV35.cs:250-255 ✓ |
| D-41 | S4 | **Stale text on money (records, docs, comments):** CLAUDE.md:50 says the debt "carries the erosion term (−π·b, symmetric per R3)" — retired (SM:6983, 7184-7187 ✓; §327 ✓); DA:93-96, STP:384-395, ScenarioLibrary.cs:61-69, 100, 116-117, 217, 258 describe the erosion term as live; SM:4913-4915 says tariff revenue "already added to Budget by TradeSystem" — no such writer (`Budget` writers SM:6735, 7137 only ✓); SM:6173-6175 and MR "Fiscal Accounting" give the old revenue formula (`GDP × rate × BaseShareOfGdp`), with an unused local `gdp` (SM:6185 ✓); MR "Fiscal Accounting" says the debt is clamped to [0, 300 %] (no floor since 2026-08-02, SM:7139-7160 ✓); PIL:74-76 calls the monetary family dead everywhere (live for the euro trio, §111 ✓); PublishedData.cs:243-244 "never read by the simulation" (read by CRS:197-214 ✓); MR "Sovereign Wealth Fund" "an immediate action … not a PolicyDecision field … USA-first" (now a budget-bill term for any country); PS:248-253, GC:9396-9403, BB:23-31, SM:947-951, 988, ProgramBill.cs:10-15 (the bills fork's list); SpendingDrivers.cs:9-10, PensionAgeStatute.cs:41-43, GC:10521, EarnedIncomeCredit.cs:24-30, SpendingCategory.cs:45-51, PolicyDecision.cs:42, SM:6230 (the tax fork's list); MR "Tax Portfolio", "Tax Collection Efficiency", "Welfare Policy"; TAX §1, §3 | as listed |
| D-42 | S3 | FT-17 (recorded, open): at the debt clamp `Budget` keeps the whole settlement while the debt takes the clamped part; the drawdown is one caller (at the net-creditor guard the fund loses the full withdrawal while the debt moves less) | SM:6735-6738 ✓, 2316-2317 ✓; FL FT-17 ✓ |
| D-43 | S4 | FT-12 (recorded, open): a floating currency follows the unweighted average of its partners' rates | CurrencySystem.cs:115-134 ✓; FL FT-12 ✓ |
| D-44 | S4 | Except-worlds receive the real game's `PolicyDecision` objects for non-player countries by reference; the AI ministry and the partner write into them and withdraw after the turn — correct only while every withdrawal is exact | PIL:201-204; SM:4864-4888 ✓ |
| D-45 | S3 | The impact ledger's forks never run the day ticks (no bill resolves, no window opens), so `real − except(f)` absorbs every bill passed after the fork | SB:138-139; SM:296-310 ✓; GC:781 |
| D-46 | S4 | `PolicyImpactLedger.Read` returns 0 for a missing country or field, so a full live value would print as "your policies" | PIL:181-187 |
| D-47 | S4 | The rating and the risk premium read GROSS debt: paying into the fund reads as a deficit to the rating (it is spending), and a drawdown improves rating and premium with no change in net position | CRS:69, 197-228 ✓; SM:6705 ✓, 7134 ✓, 2316-2317 ✓ |
| D-48 | S4 | One bill lands in pieces: rates, welfare and the fund act from passage day; spending lines from the next boundary; the pension line from the next index — the spending tiles move at passage while the book pays the old plan for the rest of the year | SM:1564-1567 ✓, 5051 ✓, 6924-6929 ✓ |
| D-49 | S4 | Only eight categories map into the spending effects even on the decision path; MunicipalGrants, EuMembershipFee, InternationalAid, CentralGovernment and others carry none, and corporate tax has no macroeconomic channel at all | SM:6665-6688 ✓; MS:152 ✓ |

**Per-country branches:** every per-country asymmetry found is stated in code or spec (alternative budgets SWE-only; statute acts and the veto POL-only; four budget procedures "unsourced"; the credit SWE-only; minimum wage off in SWE and ITA; USA neutral to the EU rules; ITA flat income tax billed). The one unstated asymmetry: the vote's line-to-axis map places only the USA's spending categories, so every European line falls to the default spend-versus-tax axis (PS:987-1035; SpendingCategory.cs:52-79; PS:987-988 calls it a default "until it is placed").

---

## 6. Harness actions

### 6.1 The day loop the UI runs (drive this order; `BatchSimulationRunner` -> `SimulationTestRunner` drives the manager, INFERRED, and never the controller's shadow, ledger, clock hold or windows)

1. Each day: `SM.AdvanceDay()` (SM:312-485: publication, the rating review, the daily macro, `AccrueDailyFiscalFlows`, the history point), then `SM.AdvanceCountryDayTick(PlayerCountryId)` (SM:296-310: the foreign-policy roll, the nine bill countdowns, `TryOpenBudgetProcess`, the confidence day), then the controller's election check (GC:773-799).
2. On a boundary day (`AdvanceDay` returns true): the controller's `AdvanceTurn` (GC:6098-6133) — the impact ledger's `AdvanceTurn`, then `SM.AdvanceTurn(decisions)` with `BuildPlayerDecision()` for the player (SM:4813-4908), then the shadow's `AdvanceTurn`, then `EnergyMarket.BeginTurn`.
3. The clock holds while `BudgetWindowHolds` (GC:1387; `GameSettings.HoldOnBudgetWindow`, GC:1531-1532); the controller blocks Advance Turn while a cabinet decision is pending (SM:921-927).
4. Bills resolve after 21 days (PS:40); one pending per kind per country (one per tax type for programme bills).

### 6.2 Money actions: the methods the UI calls, their arguments, legal ranges and gates

| action | method | arguments and legal ranges | gate | lands |
|---|---|---|---|---|
| Annual budget bill | `SM.IntroduceBudgetBill(id, BudgetBill)` (SM:1506) | `TaxLines[t]` ∈ [0, MaxRate] for levied lines (maxima in B-45; carbon ceiling 3 180 SEK / 280 EUR / 1 260 PLN / 300 USD per tonne at the start, indexed); `BracketRates[IncomeTax]` one value per sub-row, −1 = keep, model 0-100, UI 0-50; `SpendingNominalTargets[c]` UI standing × (1 ± 0.15 mandatory / 0.30 discretionary), model [0.2×, 3×] seed; `SpendingPercentChanges[c]` ±15 / ±30 in the model, no UI writer; `SpendingPinChanges[c]` no UI writer; `WelfarePrograms[w]` 0-100, levied programmes only; `SwfShouldExist`; `SwfContributionRatePercent` −10..10; four weights 0-100; `PensionAgeSet` + `PensionAge` 60-70 in months, negative = the statute's age | PM (SM:1508); none pending; UI also: the window open (GCB:865) | passage day +21; spending at the next boundary |
| Alternative budget | `SM.TableShadowBudget(id, bill, out reason)` (SM:1189) | as above | SWE only; a government bill pending; not a junior partner; one per bill | the government bill's day |
| Levy or remove a tax | `SM.IntroduceTaxProgramBill(id, TaxType, isAdd)` (SM:1599) | twelve types (not tariffs) | Finance portfolio; refused to a partner holding Finance | +21 days |
| Implement or remove a welfare programme | `SM.IntroduceWelfareProgramBill(id, type, isAdd)` (SM:1659) | six types | Health / Social Affairs portfolio | +21 days |
| Fund emergency drawdown | `SM.IntroduceSwfDrawdownBill(id, new SwfDrawdownBill{ WithdrawalPercentOfGdp })` (SM:2237) | UI 0-25 (GC:140-141); the model clamps the payout to [0, assets] | PM; a fund exists; none pending | +21 days, one-off |
| Labour bill | `SM.IntroduceLaborBill(id, bill)` (SM:1719) | minimum wage 0-100 (no-op in SWE, ITA); paid leave 0-104 weeks; overtime, retraining, family and immigration policy 0-100 | PM | +21 days |
| Sector bill | `SM.IntroduceSectorBill(id, bill)` (SM:2172) | five dials 0-100 per sector | PM | +21 days; support cost on a line at the next boundary |
| Trade bill | `SM.IntroduceTradeBill(id, bill)` (SM:2327) | base tariff 0-50; partner overrides 0-50 | Foreign portfolio | +21 days; tariff flow from the next boundary |
| Law bill | `SM.IntroduceLawBill(id, {LawId, IsRepeal})` (SM:1863) | the catalogue (fiscal framework, monetary, electricity tax, crime …) | by category; competence | +21 days |
| Finance stance (player as partner holding Finance) | `SM.SetFinanceStanceTarget(id, target)` (SM:2695) | −2.5..+2.5 points of GDP; the step is at most 0.25 a year | the player's party holds Finance as a junior partner | the year's boundary or the government's bill |
| Eurozone rate push | the decision's `InterestRateChange` via `BuildPlayerDecision` (GC:6277-6281) | ±0.75, 0.25 steps (GC:4031) | UI only (`DrawLeverLock`) | the next boundary |
| Central-bank head | write `Country.CurrentFedChair` (GC:4250; DocketV35.cs:253) | the pool's biases +1.5, +1.25, +0.25, 0, −0.25, −1.25, −1.5 (FederalReserveSystem.cs:52-80) | UI only | the next boundary |
| Cabinet / foreign-policy option | `SM.ResolveCabinetDecision(id, portfolio, decision, option)` (SM:1102); `SM.ResolveForeignPolicyMeeting(id, option)` (SM:1149) | the authored options, scaled per country | the portfolio's holder (SM:5157) | immediately (stock-side) |
| Save and load | the controller's save and load | — | — | rebuilds the shadow and the ledger (D-08) |
| Start a challenge scenario | `StartScenario` (GC:2048) | Inherit the Fund; Italy Debt Crisis | — | turn 0 |

**Read-only, for assertions:** `PreviewTurn`, `PreviewTurnWithBudgetDraft`, `EstimateBudgetBill`, `EstimateTradeBill`, `GetLastFiscalReport`, `GetPendingBudgetBill`, `GetPendingBudgetAlternative`, `GetPendingSwfDrawdownBill`, `country.Divisions`, `country.FiscalLedgerLastPeriod`, `country.ApprovalLedgerLastPeriod`, `country.Rating`, `country.History.BudgetBalanceAnnual`, `DerivedStats.*`; grep the run log for `ATTRIB:` and `LEVERS:` (refusals, SM:1508 and its siblings).

**Play styles:**
- **fuzz:** draw from 6.2 within the legal ranges, every 21 days, through the gates as the UI applies them (window open for the budget bill).
- **extreme:** every range at an end — every rate at its maximum or 0, every line at the track's edge, welfare 0 or 100, the fund at −10 or +10, the pension age 60 or 70, tariffs 50, drawdowns of 25 % every 21 days, a dovish or hawkish head.
- **exploit-seeker:** the candidates below, cheapest first.

### 6.3 Exploit candidates (all SUSPECTED)

| ID | candidate | how | why it may work | evidence |
|---|---|---|---|---|
| X-01 | **Money from an empty fund** | One budget bill: create a fund (any country) and set the contribution to −10 | The fund is floored at 0 while the budget books the full −10 % of nominal GDP a year as negative spending, forever; fund terms are outside the vote, so the bill passes uncontested; in Poland the fund act is never vetoed | D-02, D-03; SM:1575-1589 ✓, 6929-6943 ✓, 7134 ✓; PS:119-123 ✓, 344 ✓; BB:126-130 |
| X-02 | **Income-tax lever at its floor** | Set the lever to 0 or just above (DEU, FRA, POL, USA, SWE) | Possibly full seed revenue with zero burden and the maximum participation gain, or a revenue cliff | D-04; probe 0, 0.5, 1 and read `GetTotalTaxRevenue`, `TaxSchedule.YieldNow` |
| X-03 | **Spending changes with no approval effect** | Cut lines to the edge of the track each year (mandatory −15 %, discretionary −30 %), toward 0.2× seed; raise the pension age to 70 | No spending-cut penalty and no category effects for the player's bills; the pension age carries no approval term; only macro channels respond | D-05, D-36 |
| X-04 | **Cheap approval through welfare** | Raise childcare or housing assistance to 100 %, then universal healthcare and UBI | Welfare is an approval stock every year with no debt damping: childcare +1.5 a year ≈ +30 equilibrium points (reversion 0.05, MS:2096 ✓) for 1 % of GDP; housing likewise for 1.5 %; UBI +3 ≈ +60 for 18 % (relative to the seed portfolio) | MS:2196-2206 ✓, 2223-2228 ✓, 2323-2324 ✓; WelfareProgram.cs:17-22 ✓ |
| X-05 | **Free new taxes** | Levy unlevied taxes at their placeholder rates through programme bills | No hike penalty (programme bills set only `IsImplemented`) and no household-burden change (D-32) | PS:552-565; WF:1112-1115, 1202-1203 ✓ |
| X-06 | **Unbounded take** | Lever 70 plus bands at 100 | Effective marginal rates above 100 %; the only brakes are the one-time penalty, the ±5 participation clamp and the reaction multiplier | D-35 |
| X-07 | **Borrowing with no political cost** | Cut taxes, raise welfare, run deficits | No approval or economic-vote term reads debt, deficit or the rating; the one debt term damps a spending bonus the player never receives (D-05) | MS:2263-2326 ✓, 2295-2296 ✓; PerceivedPerformance.cs:189-205; SovereignRating.cs doc ✓ |
| X-08 | **Revenue from the stabiliser** | Let the debt ratio rise above the comfortable level | The fiscal reaction multiplier raises effective revenue up to ×1.5 with no hike penalty (the penalty reads only the player's rate rises) | SM:573-578 ✓, 7133 ✓; MS:2279 ✓ |
| X-09 | **Deficits absorbed at the ceiling** | Run the debt to 300 % of nominal GDP (extreme style) | The clamp swallows further deficits (`ClampLoss`), and the premium is capped at 5 points | SM:503 ✓, 7188 ✓; D-42 |
| X-10 | **Tax rises right after an election** | Time rate rises for the start of a term | The one-time penalty decays at 0.05 a year before the next vote | MS:2144 ✓, 2323 ✓ |
| X-11 | **Dodging Poland's veto** | Put money through spending lines (the budget act is veto-proof); re-table a vetoed statute on a new day | The draw is keyed by date and title (Nawrocki 45 of 174); −1.5 per standing veto | PV:91-99; B-18, B-19 |
| X-12 | **API-only reach** | Budget bills every 21 days; spending jumps 0.2× → 3×; bills for other countries; an alternative from a seatless party | Gates exist only in the UI | D-20, D-21; U-06, U-08 |
| X-13 | **Extra budget windows** | Change governments | Each change resets the arrival window | SM:1207, 3146 |
| X-14 | **Gross debt cut by the fund** | Repeated drawdowns (each up to 25 % of GDP, capped by the assets) | Lowers gross debt one for one, improving the rating and the premium with no change in net position | D-47; SM:2313-2317 ✓, 6705 ✓ |
| X-15 | **Destroying assets** | Dissolve a fund (extreme style) | The assets vanish | D-03 |
| X-16 | **A dovish central bank** | Appoint the −1.5 head (USA, SWE, POL) | Lower rates cut the interest bill; more inflation raises nominal GDP and erodes the ratio | B-79; SM:6764-6770 ✓ |
| X-17 | **Paid leave for approval** | Raise paid leave toward 104 weeks | +0.05 a week a year with no fiscal cost | MS:2310 ✓ |
| X-18 | **US stimulus by k** | Raise federal discretionary lines | k = 2.2426: a dollar of the book adds about 2.24 to demand (by the T-3 ruling — a lead, not a defect) | B-62 |
| X-19 | **Negative welfare cost** | Cut programmes below their seeded generosity | Books a negative cost (by design) and an ongoing approval loss | SM:6143-6148 ✓ |
