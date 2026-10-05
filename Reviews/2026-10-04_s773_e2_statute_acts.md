# Review - §773, Elias's ruling E2: Poland's statute acts and the Finance partner's rates (2026-10-04)

The money-path rule: `BudgetBill.cs`, `SimulationManager.cs`. A workflow review (`polisim-staged-review`) of the UNSTAGED diff (E1 staged beside it, out of scope): three lenses - the money paths and the approval ledger, the statute parts' semantics, the pages, the night and the tests - each finding put to a refute-first skeptic who read the code. Read only; no Unity run. Below: every CONFIRMED finding as written, then the refuted titles.

## The first pass - confirmed (verbatim)

### 1. The Fund act can never fall, so Without(Fund)'s write-back cannot be reached in play, and a Polish fund dissolution goes through as an uncontested act that cannot be vetoed

- **Lens:** money - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Simulation/ParliamentSystem.cs:911

**The scenario.** GetBudgetBillConcern reads only rates, spending (with the pension term) and welfare. SWF is left out on purpose, a stated simplification (ParliamentSystem.cs:109-114). So the concern of PartOf(Fund) is always empty. WouldBillPass returns true for an empty concern (line 334), and RecordDivision records no sides (1119-1129). PresidentialVeto.Decide therefore counts clubMembers = 0, and Vetoes() is false (PresidentialVeto.cs:44, 78-84). Example: a Polish budget that dissolves the fund always gets 'Fund act: the sovereign wealth fund dissolved' signed, and the slip always reads 'IT WOULD PASS AND BE SIGNED'. The DECLARED reading says the fund's rules are 'voted and put to the President like the tax act', but in effect the separation only adds a division and a ceremony. Without(Fund) (BudgetBill.cs:212-224) is correct, but no live path reaches it.

**The fix proposed.** Put this in front of Elias with the declared reading. Either keep the fund's rules in the budget act (only rates, pension age and benefits become statutes), or give the Fund act a concern (for example the contribution as a spending move) so it can be contested and vetoed. If it stays as built, say in the doc comment that the act is uncontested by construction.

**The skeptic's evidence.** I could not refute the core chain. I traced it line by line in the working tree.

1. The Fund act's concern is always empty. `BudgetBill.PartOf(Fund)` (BudgetBill.cs:186-192) copies only GovernmentBill, TabledBy and the seven Swf* fields. TaxLines, BracketRates, SpendingPercentChanges, SpendingNominalTargets and WelfarePrograms stay as new empty dictionaries, and PensionAgeSet stays false. `GetBudgetBillConcern` (ParliamentSystem.cs:911-942) adds moves only from BracketRates, TaxLines, SpendingPercentChangesOf (which includes the pension term at :143) and WelfarePrograms. No Swf field is read anywhere, which is the stated simplification at :109-114. `BillConcern.IsEmpty => _moves.Count == 0` (StanceModel.cs:63), so it is true. `Authored` (:888-892) only sets Author.

2. The act always passes and is never vetoed. `WouldBillPass` returns true at `if (concern == null || concern.IsEmpty) { return true; }` (:334). `RecordDivision` sets `contested = false` and records no sides (:1119-1129). `PresidentialVetoGate` (SimulationManager.cs:1416-1420) calls `PresidentialVeto.Decide`, which loops over an empty `sides`, so `clubMembers = 0`. `Vetoes(...) => MayVeto(act) && clubMembers > 0 && ...` (PresidentialVeto.cs:44) is false, so the gate returns true. Without a president in office, Decide returns null and the gate still returns true. `fell` never gets Fund, so `applied = applied.Without(part, country)` (SimulationManager.cs:1302) is never reached for Fund. The slip agrees. `ChamberVerdicts.WouldPass` returns true for an empty concern (:91) and `Veto` returns null (:116), so GameController.BudgetV35 always prints "...A SEPARATE FUND ACT - IT WOULD PASS AND BE SIGNED", with no veto line.

3. It is reachable in play. Poland starts with no fund (WorldFactory seeds only Sweden and France), but the player's draft sets `bill.SwfShouldExist = GetSwfExistsDraft(fund != null)` (GameController.cs:9827). So creating, dissolving or re-ruling the fund produces a Fund act that cannot fall.

One part of the finding is overstated. "No live path reaches Without(Fund)" is wrong: `BudgetActOf` calls `bill.SpendingOnly(country)` (ParliamentSystem.cs:900), which runs Without(Fund) on every Polish budget that changes any statute part. Test (k) asserts its result with `!budgetAct.ChangesFund(pl)` (PresidentialVetoDiagnostic.cs:416-418). Only the call on the fall path (:1302) is unreachable for Fund.

Why I grade it a note and not a defect. Before §773 the fund rode in the budget act, which the President cannot veto and whose vote SWF terms never moved either. So the fund's outcome is unchanged: it applies exactly when the budget act passes. There is no money-path or ledger error, because no "Fund act failed" event can ever be written. What the change adds is one division that is always uncontested and one slip line that always reads the same. That falls short of the DECLARED reading's "voted and put to the President like the tax act" (BudgetBill.cs:121-124), and nothing at that site says so.

A separate defect turned up while tracing, and it should be its own finding. `PartnerTaxAct` builds `var act = new BudgetBill { GovernmentBill = true, TabledBy = written.Holder };` (SimulationManager.cs:2694), so SwfShouldExist is false and no fund fields are set. It then runs the act through `PolishStatuteActs` (:2696), which checks all four parts. If the player's Poland holds a fund, `ChangesFund` is true, because `SwfShouldExist != (fund != null)`. The partner's tax act then records a phantom PASSED division, "Fund act: the sovereign wealth fund dissolved" (:1358), and logs "stands - it applies". The returned bill is thrown away, so the fund stays. Because the Fund act cannot fall, there is no phantom approval cost, but the division log and the ceremony are wrong. Test (l) misses this because the planted Poland has no fund; test (k)'s 3-division shape check shows that.

**The skeptic's corrected fix.** For the finding: change no behaviour without a ruling. (a) In the next report, put the declared reading in front of Elias with its effect stated. Because the vote model excludes SWF terms (ParliamentSystem.cs:109-114, a stated simplification), the Fund act is uncontested by construction and can never fall or be vetoed. His options are to keep the fund's rules in the budget act (only rates, pension age and benefits become statutes) or to give the fund a concern, for example the contribution as a spending-scale move. (b) Until he rules, say "uncontested by construction: SWF terms load no axis (ParliamentSystem.GetBudgetBillConcern), so this act always passes and is signed" at the reading's site (the BudgetBill.cs §773 block comment) and in the PolishStatuteActs summary. Leave Without(Fund) in place: it is live through SpendingOnly/BudgetActOf, and the fall path becomes live if the fund ever gets a concern. (c) Correct the finding's wording: only the call at SimulationManager.cs:1302 is unreachable for Fund, not Without(Fund) itself.

For the related defect (file it separately): make PartnerTaxAct vote the rates alone. Either copy the standing fund into `act` as AiFinanceMinistry.AsBudgetBill does (`act.SwfShouldExist = fund != null;` plus the six rule fields), or give PolishStatuteActs a parts filter and pass only StatutePart.Rates. Then add a case (l') to PresidentialVetoDiagnostic with the planted Poland holding a fund, and assert that the partner's tax act records only the tax act and its veto division, with no Fund act.

### 2. After a fall, the LEVERS log line still reports Apply's full stance step even though only StancePoints minus RatePoints is credited

- **Lens:** money - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Simulation/SimulationManager.cs:2703

**The scenario.** Apply writes 'the stance -0.25 pp of GDP (...)' into Moves (FinancePartner.cs:201). PartnerTaxAct then cuts StancePoints to -0.15 and appends 'the rates' part fell ...'. The LEVERS line at 4776 prints both, so a reader sees -0.25 next to a credit of -0.15. Only the log is affected; no tool or check parses that line (searched).

**The fix proposed.** Rewrite or append the credited figure when the rates' part falls, e.g. 'the lines' part stands: -0.15 pp'.

**The skeptic's evidence.** THE PATH EXISTS.
1. FinancePartner.cs:199-202 (Apply): written.StancePoints = Mathf.Sign(step) * moved / gdp * 100f, where moved covers both the lines and the rates. Moves[0] is then formatted from that value as "the stance {0:+0.00;-0.00} pp of GDP (...)". The string is fixed at Apply time.
2. SimulationManager.cs:2699-2703 (PartnerTaxAct, on a fall): `written.StancePoints -= written.RatePoints;` then `written.Moves.Add("the rates' part fell - its tax act failed in the Sejm or its veto stood; the lines' part stands");`. Moves[0] is never rewritten, and the new entry gives no figure.
3. SimulationManager.cs:4775-4776: Record credits the reduced StancePoints, which is correct. The LEVERS Debug.Log then prints string.Join("; ", partnerWrote.Moves), guarded only by Moves.Count > 0. So the line still says "moves the stance -0.25 pp of GDP ..." after only the lines' part landed.

ONE CORRECTION TO THE FINDING. The credited -0.15 is never printed. The reader sees -0.25 followed by "the rates' part fell; the lines' part stands" with no landed figure. The line is incomplete rather than showing two conflicting numbers.

A STRONGER SUB-CASE. If every spending line is pinned (the P5-B2 pin, SimulationManager.cs:6497), Tighten at FinancePartner.cs:245-262 finds scope = 0. Lines stays empty and the whole step goes to the rates. After a fall, StancePoints is about 0, !Any holds, and Record returns early (correctly, nothing is credited). The LEVERS line still prints "moves the stance -0.25 pp of GDP (...); the rates' part fell ...; the lines' part stands", even though nothing moved and there is no lines' part.

HOW REACHABLE. All of these must hold: the player plays Poland; the player's party leads; a partner holds Finance and asks for a tightening; the free lines are too small for the clamps to absorb 0.25 pp of GDP (the clamps are 15 % mandatory and 30 % discretionary, SimulationManager.cs:692/700), which in practice means nearly every line is pinned; and the tax act fails in the Sejm or its veto stands. That is narrow but possible.

IMPACT IS THE LOG ONLY.
- Moves is read only at SimulationManager.cs:4776 and at FinancePartnerDiagnostic.cs:72/125. The diagnostic calls Apply directly and never reaches PartnerTaxAct.
- A grep of the whole repo (Library, Temp, Logs, .git and obj excluded; Tools/ included) finds nothing that parses "LEVERS:", "pp of GDP (" or "Finance minister's party".
- The only logMessageReceived handlers (CheckSuite.cs:54, UiScreenshotDriver.cs:207) count only Error, Exception and Assert. D18Inventory collects check output and is not a player surface.
- Game state is correct: the decision's rates are removed and the credit is the reduced StancePoints.
- Diagnostic case (l) builds Written by hand with an empty Moves list, so it never checks this text.

**The skeptic's corrected fix.** In PartnerTaxAct, after the subtraction (where the Moves.Add already sits), write a line that gives the landed figure and covers the case where no lines moved. Format it at runtime, as Moves[0] is:

written.StancePoints -= written.RatePoints;
written.RatePoints = 0f;
written.RateAmount = 0f;
written.Moves.Add(written.Lines.Count > 0
    ? string.Format(System.Globalization.CultureInfo.InvariantCulture,
        "the rates' part fell - its tax act failed in the Sejm or its veto stood; the lines' part stands: {0:+0.00;-0.00} pp of GDP", written.StancePoints)
    : "the rates' part fell - its tax act failed in the Sejm or its veto stood; nothing of the step lands");

Alternatively, replace Moves[0] with a re-formatted stance line. Diagnostic case (l) could seed Moves[0] the way Apply does and assert the new entry, so the text gets tested.

### 3. Test coverage: (l) calls PartnerTaxAct through reflection only; neither the AdvanceTurn call site nor the Benefits and Fund parts is exercised

- **Lens:** money - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PresidentialVetoDiagnostic.cs:433

**The scenario.** Case (l) invokes the private method on a hand-built Written, so the condition at 4771 and the Record/SteppedOn interplay (the no-lines branch at 2704) are never run. No case covers a vetoed Benefits act, or the shape with a fund present, which would have caught the first finding.

**The fix proposed.** Add a case where the partner wrote rates only (written.Lines empty) and assert that FinancePartnerSteppedOn == boundary with no credit. Add a fund to the Polish world for (l). Add a Benefits-act veto case to match (k).

**The skeptic's evidence.** I could not refute it. Every factual claim holds against the working tree; the title slightly overstates one point.

1. **Reflection only.** In PresidentialVetoDiagnostic.cs, lines 435-442 call the private `PartnerTaxAct` through `GetMethod(...).Invoke` on a hand-built `Written`. Line 440 runs `written.Lines.Add(...)`. `Written.Any` is `Lines.Count > 0 || Taxes.Count > 0` (FinancePartner.cs:111), so it stays true after Taxes is cleared. That means SimulationManager.cs:2704 (`if (!written.Any) { country.FinancePartnerSteppedOn = boundary; }`) is never taken. `FinancePartner.Record` (FinancePartner.cs:54-59, which returns early on an empty Written) is never called, so the credit of the reduced StancePoints is not checked either.

2. **The call site at 4771 never runs its body in any diagnostic.**
   - FinancePartnerDiagnostic drives Germany only (lines 99, 143, 170), where the veto does not apply.
   - The only other Polish player run that calls AdvanceTurn is PollingDayDiagnostic §5. It seats PiS as the player's party, stops at 20 Oct 2023 (the boundary is EpochDate + 365 days, SimulationManager.cs:2677) and asserts nothing about the partner.
   - PresidentialReferenceWorld only calls AdvanceDay.

3. **No Benefits or Fund act is ever voted.** A grep of Assets/Editor finds no "Benefits act" or "Fund act". `ChangesFund` appears only in (k)'s negative assert (line 417), on a bill with no fund and no welfare entries.
   - Overstatement in the title: the predicates (`Changes` returning false) and `Without`/`SpendingOnly` do run in (k) through `BudgetActOf` and in the `PolishStatuteActs` loop. They run only on that trivial shape.

4. **No fund is present in the Polish test world.** WorldFactory creates funds only for Sweden (line 879) and France (line 889). The bills in (j) and (k) are `new BudgetBill()` with `SwfShouldExist` false, so no fund is created before (l).

5. **"Would have caught the first finding" holds.**
   - `PartnerTaxAct` builds `new BudgetBill { GovernmentBill = true, TabledBy = written.Holder }`, so `SwfShouldExist` defaults to false (BudgetBill.cs:60).
   - With a fund present, `ChangesFund` returns true (`SwfShouldExist != (fund != null)`). `PolishStatuteActs` then records a division titled "Fund act: the sovereign wealth fund dissolved" (the default branch of `StatuteActTitle`). If it falls, it also charges `BillFailedApprovalCost` and logs a "Fund act failed" ledger event, even though `PartnerTaxAct` discards `applied`.
   - This is the trap `AiFinanceMinistry.AsBudgetBill` guards against ("a bill that says no fund would dissolve one", lines 110-125). `PartnerTaxAct` does not guard against it.
   - With a fund planted, (l)'s `partnerAdded.Count == 2` (line 444) would fail.

**Reachability of 2704:** a rates-only `Written` needs no free spending line in `Tighten` (`scope <= 0`, FinancePartner.cs:246-262), meaning every line is pinned or claimed. That is real but narrow.

**Severity:** the approval ledger at the call site looks balanced: the act's cost is recorded as an event before `EnsureAccruing` and the formula at 5015-5016. CLAUDE.md's money-path rule (line 62) requires a review row, not branch coverage. So this is a coverage observation, not a defect in its own right; the fund defect it hides should be graded on its own finding. Note is the right grade.

**The skeptic's corrected fix.** Keep the reviewer's three additions, with two corrections.

1. **Plant the fund only for (l).** Set `pl.SovereignWealthFund` after (k) and before the reflection call, then assert `partnerAdded.Count == 2` and that no title starts with "Fund act: ".
   - Do not plant it earlier for the whole block. The hand-built `new BudgetBill()` bills in (j) and (k) would then vote phantom Fund acts too and fail for an unrelated reason.
   - The UI's bills avoid this because they fill the fund fields through `GetSwfExistsDraft` (GameController.cs:9827).

2. **Rates-only case.** The test must call `FinancePartner.Record(pl, written, boundary)` itself after the reflection invoke, because `PartnerTaxAct` never calls it. Use a `Written` whose Lines list is empty and whose tax act is vetoed. Assert:
   - `pl.FinancePartnerSteppedOn == boundary`;
   - `FinanceStanceHolder` and `FinanceStanceApplied` are unchanged (no credit).

3. **Benefits-act veto case**, matching (k): a generosity step on an implemented program that the planted Sejm passes and PiS opposes, stepped through `AdvanceBudgetBillDay`. Expect three divisions (budget act, "Benefits act: ...", veto) and the program's `GenerosityLevel` unchanged.

4. **The gating at 4771** is covered only by reading the code unless one `AdvanceTurn` is driven in the planted Polish world with a non-head party holding Finance and every spending line pinned. The fix should state which of the two it chooses.

### 4. The Finance partner's tax act records a fake 'Fund act: the sovereign wealth fund dissolved' when the player's Poland has a fund

- **Lens:** parts - **reviewer:** defect - **skeptic:** defect
- **Where:** Assets/Scripts/Simulation/SimulationManager.cs:2694

**The scenario.** PartnerTaxAct builds `new BudgetBill { GovernmentBill = true, TabledBy = written.Holder }` (2694). That bill leaves SwfShouldExist at its default false and copies only the partner's rates. It then passes the bill to PolishStatuteActs (2696), which checks all four parts (1290-1292). BudgetBill.ChangesFund (BudgetBill.cs:168) does `SwfShouldExist != (fund != null)`, so when the fund exists it reads the partner's act as a dissolution. Concrete case: the player leads Poland, an AI partner holds Finance, and the player earlier created a fund through a budget. The Fund tab is offered for every country (GameController.BudgetV35.cs:34), and in Poland the Fund act always passes. At a turn boundary the partner's tightening reaches the household rates. PolishStatuteActs then votes 'Tax act: ...' as intended, and also votes 'Fund act: the sovereign wealth fund dissolved' (title at 1358). Its concern is empty, so it passes with no sides and cannot be vetoed. It is logged as 'stands - it applies' and appended as a passed division that is not a motion. GameController.QueueNewlyResolvedDivisions (GameController.cs:2280-2297) queues a signing ceremony for it, and GameController.Statistics.cs:214 marks it as an enactment. PartnerTaxAct throws away the returned bill, so the fund is never dissolved: the record and the ceremony say the fund was dissolved while it still exists. This is the hazard PartOf's own doc warns about (BudgetBill.cs:177-178: 'a bill built new carries no fund ... applied it would dissolve one'), reached through Changes() instead of an apply. Diagnostic case (l) plants a Poland with no fund, so its `partnerAdded.Count == 2` check cannot see this.

**The fix proposed.** Have the partner's act state the fund as it stands before it is voted: `act = act.Without(BudgetBill.StatutePart.Fund, country);` right after building it. Or give PolishStatuteActs a list of parts to vote and pass `{ StatutePart.Rates }` from PartnerTaxAct. Add a variant of case (l) with `pl.SovereignWealthFund = new SovereignWealthFund()` planted, still expecting exactly 2 divisions.

**The skeptic's evidence.** Traced in the unstaged working tree (E2). Every cited line matches.

THE PATH
- SimulationManager.cs:2694 builds `new BudgetBill { GovernmentBill = true, TabledBy = written.Holder }`. `SwfShouldExist` keeps its default of false (BudgetBill.cs:60). Line 2695 fills only `TaxLines`.
- Line 2696 passes that bill to `PolishStatuteActs`, which loops over all four parts (1290: `foreach (... part in BudgetBill.StatuteParts)`, then `if (!bill.Changes(part, country)) continue;`).
  - PensionAge is no change (`PensionAgeSet` is false).
  - Benefits is no change (empty dictionary).
  - Fund: `ChangesFund` (BudgetBill.cs:168) does `if (SwfShouldExist != (fund != null)) return true;`, so it reads as a change whenever Poland holds a fund.
- The Fund part is then voted:
  - `PartOf(Fund)` copies `SwfShouldExist = false`.
  - `GetBudgetBillConcern` (ParliamentSystem.cs:911-940) loads no axis for any `Swf*` field, and `GetBillDirection` ignores them, so the concern is empty (`IsEmpty => _moves.Count == 0`).
  - With an empty concern, `WouldBillPass` returns true (`if (concern == null || concern.IsEmpty) return true;`) and `RecordDivision` writes no sides.
  - `PresidentialVeto.Decide` then counts `clubMembers` = 0, so `Vetoes` (`clubMembers > 0 && ...`) is false. `PresidentialVetoGate` returns true without touching the passage.
- `StatuteActTitle`'s default branch (1356-1358) gives the title "Fund act: the sovereign wealth fund dissolved", because the fund is not null and `!act.SwfShouldExist`. The division is appended as Passed and not a Motion, and the log says "stands - it applies".
- `PartnerTaxAct` discards `PolishStatuteActs`' return value, so the fund is never dissolved. The record says one thing; the state says another.

WHAT THE PLAYER SEES
- `GameController.QueueNewlyResolvedDivisions` (2280-2297) runs on every day tick, after the boundary at 786-791. It enqueues every non-motion division for a signing ceremony and fires `AudioCue.BillPasses`.
- `GameController.Statistics.cs:214` (`if (!division.Passed || division.Motion) continue;`) adds an enactment marker for it.
- Election night's scan excludes it, because `IsStatuteActTitle` matches "Fund act: ".
- No approval cost is charged, because the act cannot fall.

REACHABILITY (narrow, but real)
- Poland starts with no fund: WorldFactory seeds Sweden and France only, and the scenario seeds only Sweden. The player can create one, though:
  - The Fund tab is static for every country (BudgetV35.cs:33-34).
  - `DrawSwfPolicyContent` offers Create with no country gate.
  - `BuildBudgetBillFromDrafts` sets `SwfShouldExist` from that draft.
  - As PM the player keeps the budget lever even with an AI partner at Finance (`PlayerMayIntroduce`, §755).
  - In Poland the fund-only act passes (empty concern), so the fund is created.
- The partner's tax act fires only when `Written.Taxes.Count > 0`. That needs `Tighten`'s spending-line clamps (15 %/30 %) to leave a shortfall against a step of at most 0.25 pp of GDP. The player's decision carries only `InterestRateChange` (`BuildPlayerDecision`), so in practice nearly every line must be pinned.
- So the bug is deterministic every time `PartnerTaxAct` runs in a fund-holding Poland, but that combination is uncommon.

TEST GAP
- Diagnostic case (l) runs on the factory's Poland, which has no fund. No earlier case creates one: the bills in (j) and (k) carry `SwfShouldExist` false against no fund. So `partnerAdded.Count == 2` cannot catch this.

PRECEDENT
- `AiFinanceMinistry.AsBudgetBill` (AiFinanceMinistry.cs:112-124) already guards this exact hazard: it copies `SwfShouldExist = fund != null` and all six rules, with the doc line "a bill that says no fund would dissolve one".
- `PartOf`'s own doc warns about it too (BudgetBill.cs:177-178).

SEVERITY
- No state or money-path damage. The output is a false, player-visible enacted statute: a signing takeover, an audio cue, a graph marker and a log entry, deterministic within the new feature's own domain.
- The repo graded §768's title-text faults as review defects, so I kept this at "defect", with the narrow trigger noted.

**The skeptic's corrected fix.** Choose one of these two fixes in SimulationManager.PartnerTaxAct:

(a) Smallest change: state the fund as it stands before the act is voted. Build the bill as `var act = new BudgetBill { GovernmentBill = true, TabledBy = written.Holder }.Without(BudgetBill.StatutePart.Fund, country);` and then fill `act.TaxLines`. `Without(Fund)` writes `SwfShouldExist = fund != null` and copies the fund's six rules, so `ChangesFund` returns false. This mirrors `AiFinanceMinistry.AsBudgetBill`. Pension age and benefits are already unchanged on a fresh bill.

(b) Preferred, because it says the ruling's scope directly ("a rate moves only through the tax act") and holds up if more parts are added later. Give `PolishStatuteActs` an optional `IReadOnlyCollection<BudgetBill.StatutePart> parts` parameter, defaulting to `StatuteParts`, and have `PartnerTaxAct` pass `new[] { BudgetBill.StatutePart.Rates }`.

In either case, add a variant of diagnostic case (l):
- Plant `pl.SovereignWealthFund = new SovereignWealthFund { EquitiesWeight = 100f }` and restore it to null in a finally block.
- Assert exactly 2 divisions, with no title starting "Fund act: ".
- Assert `pl.SovereignWealthFund` still stands afterwards.

### 5. The Fund act can never fail or be vetoed: its concern is always empty, so separating it changes no outcome

- **Lens:** parts - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/Data/BudgetBill.cs:186

**The scenario.** PartOf(Fund) (186-192) carries only the fund's fields. GetBudgetBillConcern (ParliamentSystem.cs:911-942) has no fund term: GetBillDirection's doc (ParliamentSystem.cs:109-114) keeps the fund out of the vote, a simplification from the earlier budget-bill design. So the concern is empty, and then: WouldBillPass returns true (ParliamentSystem.cs:334); RecordDivision records no sides (ParliamentSystem.cs:1119-1129); PresidentialVeto.Decide sees clubMembers = 0, so Vetoes is false (PresidentialVeto.cs:44, 78-84); ChamberVerdicts.Veto returns null (ChamberVerdicts.cs:116). Example: with PiS holding 194 seats, a Polish player drafts only 'Create fund'. The budget act is uncontested, the Fund act passes with no sides, and the President cannot veto it, whatever the chamber thinks. As a result `fell` never contains Fund, and the 'Fund act failed' ledger event, the slip's 'THE FUND WOULD STAY AS IT IS' line (BudgetV35.cs:962) and Without(Fund) inside PolishStatuteActs can never run. The slip always reads 'IT WOULD PASS AND BE SIGNED'. The declared reading (BudgetBill.cs:121-124; SimulationManager.cs:1275-1283) says the fund's rules are 'voted and put to the President like the tax act' without saying the vote is empty by construction. Separately, the book counts a positive contribution as a budget expense (SimulationManager.cs:5930-5940), which this reading moves out of the 'spending only' budget act. That is declared, but worth putting to Elias.

**The fix proposed.** Either state it in the declared reading: the fund act is uncontested by construction (the fund carries no concern, by the earlier design's simplification), so it never falls and is never vetoed. Or ask for a ruling that gives the fund part a concern (for example, the contribution read as a spending-line move). If the contribution is meant to count as spending, keep it in the budget act and move only creation, dissolution and allocation.

**The skeptic's evidence.** I could not refute it. Every step of the scenario holds in the working tree.

1. The Fund act's concern is always empty.
- PartOf(Fund) (BudgetBill.cs:186-192) builds a new BudgetBill carrying only GovernmentBill, TabledBy and the seven Swf fields.
- By field default, its TaxLines, BracketRates, SpendingPercentChanges, SpendingNominalTargets and WelfarePrograms are empty and PensionAgeSet is false (BudgetBill.cs:45-70).
- GetBudgetBillConcern (ParliamentSystem.cs:911-942) reads only those fields: through SpendingPercentChangesOf, which includes the pension-age term at 143-153. It has no Swf term, and neither does GetBillDirection. Its doc at 109-114 says the fund terms are "simply excluded from the vote ... a stated simplification".
- BillConcern.Add drops zero moves, and `IsEmpty => _moves.Count == 0` (StanceModel.cs:52-63).

2. So in PolishStatuteActs (SimulationManager.cs:1285-1309), the Fund act always stands.
- WouldBillPass returns true on an empty concern (ParliamentSystem.cs:334).
- RecordDivision writes no sides (ParliamentSystem.cs:1119-1129).
- PresidentialVetoGate (SimulationManager.cs:1416-1419) calls Decide. Decide loops over no sides, so clubMembers = 0 and `Vetoes(...)` is false (PresidentialVeto.cs:44, 84), and the gate returns true.
- Fund never enters `fell`. For the Fund part, `applied = applied.Without(part, country)` (1302) and the "Fund act failed" ledger event (1306) cannot run. Without(Fund) is still reached through SpendingOnly, but only for the vote's concern.

3. The slip agrees with this.
- ChamberVerdicts.Veto returns null on an empty concern (ChamberVerdicts.cs:116), and WouldPass returns true.
- So the line always reads "IT WOULD PASS AND BE SIGNED" (BudgetV35.cs:1058), and "THE FUND WOULD STAY AS IT IS" (962) never shows.

4. Why the severity stays "note".
- The fund's legislative outcome is the same as before section 773: the fund rules apply whenever the budget passes, and the fund never swayed the vote.
- The comment that each act is "voted ... and put to the President like every ordinary statute" is literally true of the calls. It just leaves unsaid that the result is fixed, and the premise behind that is already stated at ParliamentSystem.cs:109-114.
- SimulationManager.cs:5931-5935 does call a positive contribution "A NEW BUDGET EXPENSE", so the declared reading is a fair question for Elias.

5. One qualification to "changes no outcome".
- The Fund act is a non-motion division that always passes and is voted last (the StatuteParts order).
- It gets its own signing ceremony (QueueNewlyResolvedDivisions, GameController.cs:2280-2284).
- CabinetSystem.UnderPressure reads only the newest entry (CabinetSystem.cs:472-476). So on a day a budget resolves at the turn boundary, the Fund act hides a same-day failed or vetoed act from it. This is an edge case.

6. A separate defect, outside this finding, found while tracing.
- PartnerTaxAct builds `var act = new BudgetBill { GovernmentBill = true, TabledBy = written.Holder };` (SimulationManager.cs:2694), so SwfShouldExist defaults to false.
- PolishStatuteActs (2696) checks every part, and ChangesFund returns true when `SwfShouldExist != (fund != null)` (BudgetBill.cs:168).
- Poland starts with no fund (only Sweden and France seed one), but the player can create one through a budget. Once it exists, every partner tax act also records a passed "Fund act: the sovereign wealth fund dissolved" division (SimulationManager.cs:1358), with a signing ceremony and the passing sound.
- The fund itself is not dissolved, because PartnerTaxAct discards the returned bill.
- Test (l) asserts `partnerAdded.Count == 2` only because its Poland has no fund.

**The skeptic's corrected fix.** For this finding:
- Declare it in the section 773 comment (BudgetBill.cs:120-124) and in PolishStatuteActs' summary. The Fund act is uncontested by construction: fund terms carry no concern (GetBillDirection's stated simplification), so the act always passes and is never vetoed, and its separation only adds its own division and signing ceremony.
- Put two questions to Elias: should the fund's rules get a concern (for example, the contribution read as a spending move), or should a positive contribution, which the book counts as a budget expense, stay in the budget act while only creation, dissolution and allocation travel as a statute?

For the separate PartnerTaxAct defect:
- Vote only the rates. Either give PolishStatuteActs a parts argument, or have PartnerTaxAct run the Rates step alone.
- Alternatively, build the partner's act from the standing fund (SwfShouldExist = fund != null, plus its six rules), so ChangesFund is false.
- Add a case to test (l) with a seeded fund, asserting that no "Fund act" division is recorded.

### 6. Pending card for the Fund act says 'Unopposed - no change requested'; the fund title names nothing for a weights-only change

- **Lens:** parts - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/UI/GameController.cs:9412

**The scenario.** DrawPendingLegislation adds a card for every part where `budgetBill.Changes(part, ...)` is true (9301-9306). The Fund act's concern is always empty (see the note above), so DrawPendingBillCard prints 'Unopposed - no change requested' (9412) under 'Fund act with the budget - voted if the budget passes'. That card exists only because the bill creates, dissolves or changes the rules of the fund, so the text is wrong. Also, StatuteActTitle's fund case (SimulationManager.cs:1359-1361) gives from/to only for the contribution. A weights-only change is titled 'Fund act: the sovereign wealth fund's rules' with nothing named, although the method's doc (1327) promises 'what it moves, from and to'.

**The fix proposed.** When an ordinary-statute card has an empty concern but its part changes, print 'Unopposed - uncontested' or similar, not 'no change requested'. Make the fund title list each moved weight from/to, as the contribution does.

**The skeptic's evidence.** I tried to refute both parts and could not. The working tree's line numbers match the finding.

1. The card can be reached. In Poland, GameController.cs:9301-9306 adds one card per part only when `budgetBill.Changes(part, _playerCountry)` is true. The label is "Fund act with the budget - voted if the budget passes, in N day(s)" and the concern is `GetBudgetBillConcern(_playerCountry, budgetBill.PartOf(part))`. On the Budget page's SWF tab the player can create or dissolve the fund and move the contribution and the four weights (GameController.cs:10314-10324 and 10412-10450). BuildBudgetBillFromDrafts carries those values (9825-9832), so `BudgetBill.ChangesFund` is true exactly when a fund change was drafted. DrawPendingLegislation runs on Politics > Parliament (PoliticsV35.cs:180).

2. The fund act's concern is always empty. `PartOf(Fund)` is a new BudgetBill that carries only the Swf* fields; its dictionaries default to empty and `PensionAgeSet` is false (BudgetBill.cs:48-80). GetBudgetBillConcern (ParliamentSystem.cs:911-942) adds moves only from BracketRates, TaxLines, SpendingPercentChangesOf and WelfarePrograms, and never reads an Swf* field. GetBillDirection's doc says "SWF terms are simply excluded from the vote". So `IsEmpty => _moves.Count == 0` (StanceModel.cs:63) is true every time.

3. The text is wrong. GameController.cs:9407 sets `bool contested = concern != null && !concern.IsEmpty;` and line 9412 then prints `contested ? (...) : "Unopposed - no change requested"`. The Fund act card therefore always says "no change requested", although it exists only because a fund change was requested. No branch handles an empty-concern statute act.

The verdict itself is right: `WouldBillPass` returns true for an empty concern (ParliamentSystem.cs:334) and `ChamberVerdicts.Veto` returns null (ChamberVerdicts.cs:116). The sim agrees: PolishStatuteActs passes the act and the veto gate does not stop it. So "Unopposed" and the green ink are correct; only the words "no change requested" are false. The Budget page's slip handles the same case correctly ("IT WOULD PASS AND BE SIGNED"). Before §773 the budget card could already show this text for a fund-only bill. The new card shows it every time it appears.

4. The title gap is real. SimulationManager.cs:1327 promises "what it moves, from and to", but lines 1355-1361 append from/to only when `SwfContributionRatePercent` moves. Example: an existing fund with Equities moved from 40 to 50 and nothing else changed is titled "Fund act: the sovereign wealth fund's rules". Division Records shows that title as written (GameController.cs:9242, `$"No. {record.Number} · {record.Title}"`). The title is vague but true, so this part is note-level; the card text keeps the finding at minor.

Severity: minor. It is player-visible wording only. No outcome, number or vote is wrong.

**The skeptic's corrected fix.** For the card (GameController.cs:9284-9416), tell DrawPendingBillCard when a card is a statute act whose part changes. One way is an optional parameter, `bool requestsChange = false`, which the per-part loop sets to true. When the concern is empty and that flag is set, print "Unopposed - the chamber does not weigh the fund's rules" instead of "Unopposed - no change requested". That matches GetBillDirection's stated simplification, and the verdict and ink stay as they are.

For the title (SimulationManager.cs:1355-1361), list each fund field that moves, from and to, the way the contribution already does: domestic allocation and the equities, bonds, infrastructure and real-estate weights, each compared against the standing fund with the same 1e-4 test ChangesFund uses. If that is not wanted, narrow the 1327 doc so it says the fund title names only the contribution.

### 7. The partner's tax act also votes a spurious 'Fund act: the sovereign wealth fund dissolved' wherever the player's Poland has a fund

- **Lens:** ui - **reviewer:** defect - **skeptic:** minor
- **Where:** Assets/Scripts/Simulation/SimulationManager.cs:2694

**The scenario.** Setup: the player governs Poland. An earlier budget created a sovereign wealth fund through the Fund tab (WorldFactory.cs:879/889 seed funds only for Sweden and France). A coalition partner holds Finance, and its tightening reaches IncomeTax/VAT at a boundary because the lines are pinned or clamped. What happens: PartnerTaxAct builds `new BudgetBill { GovernmentBill = true, TabledBy = written.Holder }`, so SwfShouldExist is false. It hands that bill to PolishStatuteActs (2696), which loops every StatutePart. BudgetBill.ChangesFund (BudgetBill.cs:168) returns true, because false != (fund != null). PartOf(Fund)'s concern is empty (GetBudgetBillConcern has no SWF term), so WouldBillPass returns true. RecordDivision then writes 'Fund act: the sovereign wealth fund dissolved' with no sides. PresidentialVetoGate finds no backing-party NO and signs it, and the log says 'stands - it applies'. PartnerTaxAct throws the returned bill away, so the fund survives. Result: the division record holds a passed act dissolving a fund that still exists, and QueueNewlyResolvedDivisions queues it for a signing ceremony, since it is not a motion. This happens whether the rates act stands or falls. Case (l) cannot see it because its Poland has no fund. BudgetBill.PartOf's own doc warns that a bill built new carries no fund.

**The fix proposed.** Vote only the rates for the partner. Factor the per-part vote out of PolishStatuteActs (e.g. VoteStatuteAct(country, bill, part) returning whether it stands) and call it for StatutePart.Rates alone in PartnerTaxAct. Alternatively, seed `act` from the standing fund: SwfShouldExist = fund != null plus its six rules. Add an (l) variant with a planted fund that asserts exactly two divisions.

**The skeptic's evidence.** The trace is correct line by line. I'm grading it down from defect to minor because nothing changes in the game state and nothing in normal play can trigger it today.

HOW IT HAPPENS:
- SimulationManager.cs:2694 builds `var act = new BudgetBill { GovernmentBill = true, TabledBy = written.Holder };`. `SwfShouldExist` defaults to false (BudgetBill.cs:60).
- Line 2696 hands it to PolishStatuteActs, which loops over every StatutePart. Line 1292, `if (!bill.Changes(part, country)) continue;`, reaches ChangesFund. At BudgetBill.cs:168, `if (SwfShouldExist != (fund != null)) return true;` is true whenever Poland has a fund.
- PartOf(Fund) has an empty concern, because GetBudgetBillConcern and GetBillDirection carry no fund term. So WouldBillPass passes it (ParliamentSystem.cs:334, `if (concern == null || concern.IsEmpty) { return true; }`).
- RecordDivision records an uncontested vote with no sides. With no sides, PresidentialVeto.Decide counts clubMembers = 0, so there is no veto and the act stands.
- The title comes from line 1358: "Fund act: the sovereign wealth fund dissolved". The log says "stands - it applies".
- PartnerTaxAct throws away what PolishStatuteActs returns, so the fund survives.
- QueueNewlyResolvedDivisions (GameController.cs:2280-2297) queues every division that is not a motion. So a signing ceremony and the BillPasses cue fire for a dissolution that never happened. This does not depend on whether the rates act stands or falls.
- The codebase already knows this trap. AiFinanceMinistry.AsBudgetBill (lines 111-125) copies the existence flag and all six fund rules, with the comment "a bill that says no fund would dissolve one". PartOf's own doc carries the same warning.
- A fund in Poland can be reached in play. The Fund tab exists for every country (BudgetV35.cs:32/815). Create sets SwfShouldExist (GameController.cs:9827), and under §773 that Fund act always passes and cannot be vetoed.
- Check (l) asserts `partnerAdded.Count == 2` (PresidentialVetoDiagnostic.cs:444) on a Poland with no fund. WorldFactory only seeds funds at lines 879 and 889 (Sweden, France), so (l) cannot see this.

WHY MINOR, NOT DEFECT:
1. It only affects the record. The Fund act can never fall, so there is no approval cost and nothing is applied.
2. In play the trigger can't happen today. PartnerTaxAct runs only when FinancePartner.Tighten (FinancePartner.cs:243-280) leaves a shortfall after its uniform cut to the spending lines.
   - In the player's Poland with the player as prime minister, the decision comes from BuildPlayerDecision (GameController.cs:6277), which sets only the interest rate, and the ministry does not run. So every unpinned line is free to cut.
   - Poland's lines are all discretionary (WorldFactory.cs:1784), with a 30% range (SimulationManager.cs:692), and they sum to GovernmentSpendingRate × GDP. A step of at most 0.25 points of GDP is about a 1% cut and never reaches the clamp.
   - Pinned lines are the only other route. SpendingPinChanges is written only by Editor tools (LeverLivenessCheck.cs:417, SpendingIndexationDiagnostic.cs:65), and no UI path pins a line.
   - So today it is reachable only through harness pins or (l)'s reflection call. It becomes live in play if a pin control ships.

**The skeptic's corrected fix.** Restrict the partner's act to the rates. Give PolishStatuteActs an optional parts filter, for example `IReadOnlyList<BudgetBill.StatutePart> only = null` that defaults to BudgetBill.StatuteParts, and call `PolishStatuteActs(country, act, out fell, new[] { BudgetBill.StatutePart.Rates })` from PartnerTaxAct. The minimal alternative is to seed `act` from the standing fund the way AiFinanceMinistry.AsBudgetBill does: `SwfShouldExist = fund != null` plus the six rules. The filter is the better choice because it also protects any future part whose 'no change' is not a fresh bill's default. Then add an (l) variant that plants a SovereignWealthFund on `pl` before invoking PartnerTaxAct and asserts exactly two divisions, no 'Fund act: ' title, and the fund still present.

### 8. Case (k)'s 'budget act carries the spending alone' check is vacuous for rates, benefits and fund; Benefits/Fund acts are untested

- **Lens:** ui - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/PresidentialVetoDiagnostic.cs:417

**The scenario.** (k)'s bill (414-415) has no TaxLines, no WelfarePrograms and SwfShouldExist=false, and Poland in the default world has no fund. So TaxLines.Count==0, WelfarePrograms.Count==0 and !ChangesFund(pl) already hold for the input bill before BudgetActOf runs. Only !PensionAgeSet actually tests SpendingOnly. Example: break Without(Benefits) so it keeps WelfarePrograms. A Polish budget raising a welfare level is then weighed in the budget act's concern and voted again as a Benefits act, and (k) stays green. Nothing in the diagnostic drives a Benefits or Fund act at all: not ChangesBenefits/ChangesFund, not StatuteActTitle's two branches, not Without(Benefits/Fund). IsStatuteActTitle is asserted only on a 'Pension act: ' title. Dropping 'Benefits act: ' or 'Fund act: ' from the night's scan would pass every check.

**The fix proposed.** Give (k)'s bill a rate move and a welfare move, plus a planted fund with a moved rule. Assert each is gone from BudgetActOf's result and is voted as its own act under its own title. Assert IsStatuteActTitle is true for all four ActWords prefixes, and false for 'Annual budget bill' and for a 'Vetoed by the President (...)' title.

**The skeptic's evidence.** I could not refute it. Every factual claim in the finding holds.

1) The check at line 417 can fail on only one of its four exclusion clauses. Case (k) builds its bill at lines 414-415:
`var bill = new BudgetBill { PensionAgeSet = true, PensionAge = newAge }; bill.SpendingPercentChanges[spend.Category] = 5f;`
- It has no TaxLines, no BracketRates and no WelfarePrograms, and SwfShouldExist is left at its default of false.
- Poland has no fund. WorldFactory.cs:850-852 says Poland's fund "honestly stays null", and only Sweden and France are seeded (lines 879 and 889). Case (j)'s bills also carry SwfShouldExist=false, so ApplyBudgetBillSpendingAndSwf (SimulationManager.cs:1527-1530) leaves it null.
- ChangesFund starts with `if (SwfShouldExist != (fund != null)) return true; if (fund == null) return false;`, so it is already false on the input bill.

So `TaxLines.Count == 0 && WelfarePrograms.Count == 0 && !ChangesFund(pl)` are all true before BudgetActOf runs. If Without(Rates), Without(Benefits) or Without(Fund) became a no-op, (k) would stay green. Only `!PensionAgeSet` tests an exclusion; the spending count only tests that spending is kept. BudgetActOf (ParliamentSystem.cs:900) also stops at the PensionAge part, so the Benefits and Fund parts are only ever evaluated as false. The check's message ("no rate, no pension age, no benefit level, the fund as it stands") therefore claims more than it tests, which the repo's own MODEL_REFERENCE.md:1152 calls an "overestimated check".

2) No test drives a Benefits act or a Fund act. git grep over Assets/Editor and Assets/Scripts/Testing finds no StatutePart.Benefits, StatutePart.Fund, ChangesBenefits, "Benefits act" or "Fund act". IsStatuteActTitle is asserted once, at line 426, and only on a "Pension act: " title. Case (l) never calls it. Nothing tests the night's standing-budget scan (GameController.cs:6543). The only test bill that moves welfare and the fund, SimulationTestRunner.BuildParliamentStressBill, is USA-only, and the veto rule is `Applies(CountryId c) => c == CountryId.Poland`.

3) The gap hides a real runtime defect, which should be filed separately as a defect. PartnerTaxAct (SimulationManager.cs:2694) builds `new BudgetBill { GovernmentBill = true, TabledBy = written.Holder }` with SwfShouldExist=false. It passes that bill to PolishStatuteActs, which loops over all four StatuteParts with `if (!bill.Changes(part, country)) continue;`.
- A Polish fund can exist: the player's budget can create one through a Fund act (the draft toggle at GameController.cs:9827 is not limited by country).
- When Poland holds a fund, ChangesFund returns true for the partner's bill. Beside the partner's tax act, the game then votes a spurious "Fund act: the sovereign wealth fund dissolved" and puts it to the President.
- If that act falls, the game charges BillFailedApprovalCost and records a "Fund act failed" ledger event. If it passes, the record shows a dissolution that PartnerTaxAct never applies.
- PartOf's own comment warns that a bill built new "would dissolve" a fund.
- (l)'s `partnerAdded.Count == 2` would catch this if a fund were planted before it, as the finding's suggested fix does.

Grade: minor. This is a test-coverage gap and an over-claiming check message, not a broken assertion. The PartnerTaxAct problem in point 3 is the defect-level item.

**The skeptic's corrected fix.** 1) Keep (k)'s run through the game's own path and its three-division shape as they are.
- Next to it, add a separate check that calls BudgetActOf directly on a bill that changes all four parts: a moved rate, a moved level on an implemented Polish welfare program, the pension age, and SwfShouldExist=true on Poland's null fund (or a planted fund with one moved rule, restored afterwards).
- For each part, assert that `bill.Changes(part, pl)` is true on the input and false on BudgetActOf's result. Also assert that the spending dictionaries are kept.

2) Drive at least one Benefits act and one Fund act through the game's path.
- Assert the titles start with "Benefits act: " and "Fund act: ", and that IsStatuteActTitle returns true for them.
- Assert that when the act falls, the levels or fund are left unchanged.

3) Assert IsStatuteActTitle is true for `ActWords(part) + ": "` for all four parts. Assert it is false for "Annual budget bill" and for a "Vetoed by the President (Karol Nawrocki): Tax act: ..." title. The night's scan already excludes the veto titles through `Required > 0`.

4) Run (l) a second time with a fund planted on Poland, and require exactly the tax act and its veto: no Fund act.

5) Fix PartnerTaxAct so it cannot create a Fund act. Either vote only the Rates part (use a Rates-only overload of PolishStatuteActs, or have the act name the fund as it stands, as AiFinanceMinistry.AsBudgetBill does: `SwfShouldExist = fund != null` plus the fund's six rules).

### 9. Case (l) tests PartnerTaxAct in isolation; the boundary wiring and the 'step spent anyway' branch are unexercised, and the fixture is not a partner-shaped move

- **Lens:** ui - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Editor/PresidentialVetoDiagnostic.cs:433

**The scenario.** (l) calls the private method by reflection with a hand-built Written. The only call site, SimulationManager.cs:4771-4774, is never run. Delete that call, or break its PlayerCountryId/Applies condition, and a player-governed Poland's partner again raises IncomeTax/VAT at the boundary with no Sejm vote, while (l) stays green. The fixture reuses (j)'s moved/newRate. (j)'s search tries a -2 cut first, over any implemented tax line, but FinancePartner.Tighten only ever raises IncomeTax/VAT. written.Lines is non-empty, so the 'year's step is spent anyway' branch (SimulationManager.cs:2704, FinancePartnerSteppedOn when !written.Any) never runs, though the change description claims it.

**The fix proposed.** Drive one AdvanceTurn on a Polish world with a planted partner holding Finance and the lines pinned, so Tighten writes the household rates. Assert the Tax act division and the rates withheld from the turn. In the reflection case, use a household rate and a rise. Add a variant with Lines empty that asserts FinancePartnerSteppedOn == boundary.

**The skeptic's evidence.** I tried to refute each claim and none of them falls. This is a gap in test coverage, not a defect in behaviour.

1. Case (l) tests the method in isolation. PresidentialVetoDiagnostic.cs:435-442 calls `GetMethod("PartnerTaxAct", NonPublic)` and `Invoke(sim, { pl, decision, written, sim.CurrentDate })` with a hand-built Written. The file never calls AdvanceTurn or AdvanceDay.

2. Nothing else reaches the boundary wiring. A grep for PartnerTaxAct finds only three hits: the definition (SimulationManager.cs:2692), the one call site (4771-4774, inside the boundary block 4768-4775) and the reflection string in the test.
   - FinancePartnerDiagnostic drives the boundary on Germany only (lines 147 and 180).
   - In the default epoch KO holds Finance under KO's own prime minister (PortfolioSalienceDiagnostic:111). FinancePartner.Holder therefore returns null (FinancePartner.cs:124), so no Polish world in any other check gets a partner.
   - No log in PoliSim-captures/logs, including n773a.log, contains a "LEVERS: Poland - the Finance minister's party" line.
   - So if line 4773 were deleted, or the PlayerCountryId/Applies guard on 4771 were broken, (l) would stay green.

3. The fixture is not a move the partner could make.
   - The search in (j) (lines 232-245) runs over every implemented tax line and tries `-2f` first. The measured run found a cut: n773a.log:768 reads "IncomeTax 12.36 -> 10.36", and :774 reads "Tax act: income tax 12.36 % to 10.36 %".
   - FinancePartner.Tighten only raises rates. It filters on `IsHouseholdRate`, sets `to = Mathf.Min(line.MaxRate, line.Rate + points)` and skips the line when `to <= line.Rate`.
   - The fixture's `Holder = "KO"` is the head's own party, which Holder() can never return.
   - PartnerTaxAct does not care about tax type or direction, so this weakens realism rather than the check of the mechanism.

4. The "step spent anyway" branch never runs.
   - Test line 440 adds a line, so `Written.Any` (FinancePartner.cs:111) is true and `if (!written.Any) { country.FinancePartnerSteppedOn = boundary; }` (SimulationManager.cs:2704) is skipped.
   - FinancePartner.Record is not called either, so whether the year's step is spent is untested in both cases.
   - In real play this branch is the main path when the act falls. The step is 0.25 pp of GDP (StepPointsPerYear), while the line clamps are 15 % and 30 % (SimulationManager.cs:692/700). Tighten therefore puts a step on the rates only when nearly every line is pinned or already written. With every line pinned, written.Lines is empty, and 2704 is the line that spends the year's step.
   - Lines 2697-2704 are correct as written: Record returns early on `!Any`, so the date set at 2704 is the only one. That is why this grades as minor.

5. A related defect, outside this finding, that (l)'s fixture cannot see because its Poland has no fund.
   - PartnerTaxAct builds its act as `new BudgetBill { GovernmentBill = true, TabledBy = written.Holder }` (line 2694), so SwfShouldExist is false.
   - PolishStatuteActs votes every StatutePart. `ChangesFund` returns true whenever `SwfShouldExist != (fund != null)`.
   - A player can create a fund in Poland through a budget's Fund act (SimulationManager.cs:1533-1536). From then on, every partner tax act also records an extra "Fund act: the sovereign wealth fund dissolved" division. If that act falls, it also charges BillFailedApprovalCost and logs a "Fund act failed" ledger event.
   - The fund itself survives, because PartnerTaxAct discards the bill PolishStatuteActs returns.

**The skeptic's corrected fix.** 1. Add a boundary case to PresidentialVetoDiagnostic.
   - Set up the world: start a fresh EpochScope world, set `sim.PlayerCountryId = Poland` and `pl.PlayerPartyAbbrev = "KO"`. KO is then prime minister, so PlayerGoverns is true and PartnerStepsAtBoundary holds with AiFinanceMinistryEnabled left on.
   - Plant the partner: put FinanceTreasury on "Konf" in pl.Government.Portfolios, with the government not provisional. Konf's lrecon is 8.96 against KO's 6.17, so it asks for -0.70 and steps -0.25, a tightening.
   - Pin the lines: set `FinancePartnerSteppedOn = DateTime.MinValue` and pin every spending line. Tighten then puts the whole step on income-tax and VAT rises, and written.Lines is empty.
   - Drive one boundary with AdvanceTurn or the AdvanceDay loop.
   - Make the rise fall: find seats under which it fails, by searching as (j) does or by planting seats. (j)'s chamber only shows PiS opposing a cut; a rise is not measured.
   - Assert on the fall: a "Tax act: income tax ..." division (plus the vote on the veto if one stands); income tax and VAT unchanged after the turn; `FinancePartnerSteppedOn` equal to the boundary date; FinanceStanceApplied unchanged. This covers lines 4771-4775 and 2704.
   - Add the opposite case, where the act stands: the rates move and the full step is credited.

2. In the reflection case, build the fixture from FinancePartner.Apply on that planted world, not from (j)'s moved/newRate. At minimum use IncomeTax or VAT raised above its current rate, with a Holder that Holder() can return.

3. Plant a sovereign wealth fund on Poland in one case and assert that the partner's act records only the Tax act and its veto. This fails today. The code fix is to vote only the Rates part in PartnerTaxAct: either give PolishStatuteActs a filter for which parts to vote, or build the act with `SwfShouldExist = fund != null` and copy the fund's six current rules.

### 10. A Fund act's pending card reads 'Unopposed - no change requested'

- **Lens:** ui - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/UI/GameController.cs:9412

**The scenario.** A Polish draft creates, dissolves or re-weights the sovereign wealth fund. DrawPendingLegislation adds 'Fund act with the budget - voted if the budget passes, in N day(s).' (9304-9305). But PartOf(Fund)'s concern is always empty, because GetBudgetBillConcern has no SWF term ('SWF terms are simply excluded from the vote', ParliamentSystem.cs:110-113). DrawPendingBillCard therefore prints 'Unopposed - no change requested' under a card that exists only because the fund changes, and draws every seat UNDECIDED.

**The fix proposed.** Give a statute-act card its own uncontested wording, e.g. a flag on the pending tuple so it reads 'Uncontested - the chamber weighs no fund terms; signed if the budget passes' instead of 'no change requested'.

**The skeptic's evidence.** I could not refute it. The path below is reachable and happens every time.

1. The trigger is reachable in Poland. The Budget page's Fund tab is one of five fixed tabs (GameController.BudgetV35.cs:32-34, 815), with no country gate. Its Create/Dissolve button sets `_swfExistsDraft = !draftExists` (GameController.cs:10324), and its sliders feed the SWF weights. BuildBudgetBillFromDrafts copies all of these into the bill (`bill.SwfShouldExist = GetSwfExistsDraft(fund != null)` and the weights, GameController.cs:9827-9833). IntroduceBudgetBill stores that bill unchanged (SimulationManager.cs:1471-1472).

2. The card is drawn. DrawPendingLegislation is live: it is called from the Politics page at GameController.PoliticsV35.cs:180. Inside it, `if (!budgetBill.Changes(part, _playerCountry)) continue;` lets the Fund card through. `ChangesFund` returns true on `SwfShouldExist != (fund != null)` or on any `Moved(...)` weight (BudgetBill.cs, §773 hunk). The card's label is `"Fund act with the budget - voted if the budget passes, in N day(s)."` (GameController.cs:9304).

3. Its concern is always empty. `PartOf(Fund)` sets only GovernmentBill, TabledBy and the Swf* fields. Every dictionary keeps its empty default and PensionAgeSet stays false. GetBudgetBillConcern (ParliamentSystem.cs:911-937) reads only BracketRates, TaxLines, SpendingPercentChangesOf and WelfarePrograms. It has no SWF term, by design: "SWF terms are simply excluded from the vote" (ParliamentSystem.cs:108-113). `Authored` sets `Author` only and adds no move. So `BillConcern.IsEmpty => _moves.Count == 0` is true (StanceModel.cs:63).

4. The wording follows from that. `bool contested = concern != null && !concern.IsEmpty;` (GameController.cs:9407) is false, so line 9412 prints `"Unopposed - no change requested"`. That text sits under a card that is drawn only because a fund change was requested.

5. "Unopposed" is correct; only "no change requested" is false. WouldBillPass returns true for an empty concern (ParliamentSystem.cs:334). `ChamberVerdicts.Veto` returns null for one (ChamberVerdicts.cs:116). In the simulation, `PresidentialVeto.Vetoes` needs `clubNo * 2 > clubMembers`, and no party is ever AGAINST here. So the act always passes and is signed.

6. The Budget page already gets this right. Its slip names the same act as "ITS FUND RULES ARE A SEPARATE FUND ACT - IT WOULD PASS AND BE SIGNED" (GameController.BudgetV35.cs, §773 hunk). The two pages therefore disagree, and the disagreement comes from this diff, since the Fund act card is new in §773.

One correction to the finding: "draws every seat UNDECIDED" is overstated for the player's own bill. `Authored(country, concern, false)` sets `Author = PlayerPartyAbbrev` (ParliamentSystem.cs:890). StanceModel.cs:286-293 then draws the author's party FOR ("its author votes for it"), so the caption reads "FOR n · UNDECIDED m · AGAINST 0". The other seats are UNDECIDED (`loaded.Count == 0` gives side 0). This does not affect the wording defect.

Why minor: it is a false line of text on a new card. It has no effect on the simulation, the money path or the verdict.

**The skeptic's corrected fix.** Give DrawPendingBillCard an optional uncontested-text parameter, or add a fifth element to the pending tuple. It defaults to the existing "Unopposed - no change requested", so every other card keeps its wording. At GameController.cs:9304, when a statute act's concern is empty (always the case for StatutePart.Fund), pass wording that matches the Budget slip, for example "Uncontested - the chamber weighs no fund terms; it passes and is signed if the budget passes". Those words are accurate: an empty concern passes (ParliamentSystem.cs:334), and no party is AGAINST, so `PresidentialVeto.Vetoes` cannot fire. Optionally, also refresh the comment at GameController.cs:9416-9417 ("an unopposed bill maps every seat UNDECIDED"). Since PS-3i-2b (§654), the author's own party is drawn FOR on the player's bill.

### 11. C4's accepted premises are not relabelled, so the two Polish polling-day bases now disagree

- **Lens:** ui - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/Elections/PresidentialElection.cs:180

**The scenario.** E2 accepts C4: 'election day on the last Sunday of the window: accepted', and 'the swearing-in date, the 100,000-signature check ... accepted'. This change rewrites WorldClock's Poland basis (WorldClock.cs:375) to say 'the principle for both of Poland's votes, ruled with it: the most recent practice'. But FirstVoteFor still returns 'PREMISE, DECLARED - the last Sunday of the window ...' (180), and the class doc still says DECLARED at 15, 17, 21-22 and 167. Neither E1's staged hunks nor E2 touch these lines. The Parliament row's slip turns the basis into 'A PREMISE: THE LAST SUNDAY OF THE WINDOW BEFORE THE TERM ENDS, AS IN 2025' (GameController.ParliamentRows.cs:549-551). PresidentialElectionLiveDiagnostic.cs:51 pins StartsWith("PREMISE, DECLARED"). A reader sees the same principle as 'ruled' in one basis and 'DECLARED' in the other.

**The fix proposed.** Relabel these the way D3's first sitting was relabelled: 'ruled, E2' in the basis string and the class doc, including the swearing-in and the signature gate. Update DayWords' slip text. Update the live diagnostic's pin to assert the new label and no 'DECLARED', as PollingDayDiagnostic now does.

**The skeptic's evidence.** I tried to refute this and couldn't. Every line the finding cites matches the working tree.

1. **The E2 diff adds the "ruled" half.** WorldClock.cs:375 (unstaged) now says: "...(Art. 109 ust. 2's last day - ruled, E2); the principle for both of Poland's votes, ruled with it: the most recent practice". This sentence covers the President's vote as well as the Sejm's.

2. **PresidentialElection.cs still says DECLARED for the same accepted premises.** `git status` shows the file as "M " (staged only), so E2 never edits it. The labels still in place:
   - :15 "otherwise the window's last Sunday - PREMISE, DECLARED: 2025's precedent"
   - :17 "PREMISE, DECLARED: the oath on that day"
   - :21-22 "The gate is the signatures: READING, DECLARED"
   - :167 "Sunday (PREMISE, DECLARED - see the class)"
   - :180 `basis = "PREMISE, DECLARED - the last Sunday of the window Art. 128 ust. 2 sets ..."`
   
   One small correction to the finding: E1's staged hunk `@@ -15,21 +15,24 @@` did rewrite the "Who stands" bullet. It kept "READING, DECLARED" when it did, so the conclusion doesn't change.

3. **In this code, DECLARED means "no ruling fixed it".** The old WorldClock doc read "§767, DECLARED - the premise D3 needs and no ruling fixed". E2 accepted all of C4's premises: "election day on the last Sunday of the window: accepted", and "the swearing-in date, the 100,000-signature check ... accepted". These are the three premises §770 recorded as "Owed to Elias" (COMPLETED.md §770). After E2, the DECLARED tags make a claim that is false.

4. **The same E2 diff relabels the other accepted premises, which shows C4 was left out by mistake:**
   - WorldClock `PolandFirstSittingAfterDays` becomes "RULED §773 (Elias's ruling E2 ...)".
   - SimulationManager `PolishStatuteActs` gets "RULED (E2): a budget the Sejm rejects takes its acts with it".
   - PollingDayDiagnostic:80 now asserts `Contains("ruled, E2") && !Contains("DECLARED")`.
   
   The session's draft record (scratchpad rec773.md) says "`PresidentialElection.FirstVoteFor` says ruled" and "The oath, the gate and the player's own leader. Ruled in `PresidentialElection`'s account." Neither is true of the tree, so the § given to Elias as installing E2 would overstate what it did.

5. **Nothing catches it.** PresidentialElectionLiveDiagnostic.cs:51 pins `laterBasis.StartsWith("PREMISE, DECLARED")`, which is the stale label. MetaTextCheck and DocumentClaimCheck don't separate DECLARED from RULED, so the bar passes.

6. **Only labels are wrong.** Runtime behaviour is unchanged. DayWords (GameController.ParliamentRows.cs:549-551) keys on `StartsWith("PREMISE")` and prints "A PREMISE: THE LAST SUNDAY OF THE WINDOW BEFORE THE TERM ENDS, AS IN 2025", which is still true of an accepted premise.

**Severity: minor.** Stale tracking claims, and an E2 install that is incomplete against its own record. No effect on simulation or money paths.

**The skeptic's corrected fix.** Finish E2's C4 relabels in this pass, as unstaged E2 work on top of E1's staged file, the same way WorldClock was done.

1. In PresidentialElection.cs, use the form the change already uses:
   - Class doc line 15: "otherwise the window's last Sunday - RULED §773 (Elias's ruling E2: one principle for both votes, the most recent practice - 2025's last Sunday for the President; 2015 and 2020 were ordered for its second)".
   - Line 17: "the oath on that day - RULED §773 (E2: the swearing-in date accepted)".
   - Lines 21-22: "The gate is the signatures: READING, RULED §773 (E2: the 100,000-signature check accepted)".
   - FirstVoteFor doc at line 167: "(RULED §773, E2 - see the class)".
   - Basis string at line 180: "PREMISE, RULED (E2) - the last Sunday of the window Art. 128 ust. 2 sets, 100 to 75 days before the term ends, the most recent practice (2025; 2015 and 2020 were ordered for its second)".

2. Keep the "PREMISE" prefix on the basis string. DayWords tests `StartsWith("PREMISE")`, and a relabel to "RULED ..." would flip the slip to "THE RECORD'S DAY". The more robust option is to change DayWords to key on the record's own prefix ("the record's day"). Either way, older saves' `Contest.DayBasis` values ("PREMISE, DECLARED - ...") still map correctly. DayWords' text can optionally read "THE LAST SUNDAY OF THE WINDOW BEFORE THE TERM ENDS - THE MOST RECENT PRACTICE, AS IN 2025".

3. Move PresidentialElectionLiveDiagnostic.cs:51's pin in the same pass, to `laterBasis.StartsWith("PREMISE", Ordinal) && laterBasis.Contains("E2") && !laterBasis.Contains("DECLARED")`, mirroring PollingDayDiagnostic:80. Then rerun PresidentialElectionLive and PollingDay in the named batch.

This also makes rec773.md's two lines about PresidentialElection true.

### 12. The tax act's title prefix has two sources of truth, and TaxActTitlePrefix's doc names a reader that no longer uses it

- **Lens:** ui - **reviewer:** minor - **skeptic:** minor
- **Where:** Assets/Scripts/Simulation/SimulationManager.cs:1316

**The scenario.** TaxActTitle builds the title from TaxActTitlePrefix (1396). IsStatuteActTitle matches ActWords(Rates) + ": " (1323). The night's scan now calls IsStatuteActTitle (GameController.cs:6543), but the const's doc (1315) still says 'the night's standing-budget scan tells it from the budget act by it'. Failure: rename ActWords(Rates), e.g. to 'Tax law' to change the slip and ledger wording. The tax act's title stays 'Tax act: ...', so IsStatuteActTitle stops matching it. Election night then cites a signed tax act as the standing budget again (the §768 review's defect 4). (j) and (l) assert the literal 'Tax act: ' prefix and (k) checks only the pension title, so all three stay green.

**The fix proposed.** Use one source: build TaxActTitle from ActWords(StatutePart.Rates) + ": " (or have IsStatuteActTitle compare TaxActTitlePrefix for Rates), then drop or re-document the const. Assert IsStatuteActTitle on (j)'s tax-act title.

**The skeptic's evidence.** I could not refute it. Every factual claim holds in the working tree. Nothing fails at runtime today, because "Tax act" + ": " == "Tax act: ".

1. **Two sources of truth.** `SimulationManager.cs:1396` `TaxActTitle` returns `TaxActTitlePrefix + (...)`, and the const is `"Tax act: "` (1316). `IsStatuteActTitle` (1323) matches `title.StartsWith(ActWords(part) + ": ")`, where `ActWords(Rates)` is `"Tax act"` (1313). Nothing ties the two strings together.

2. **The const's doc is stale, and this diff made it so.** Line 1315 still says "the night's standing-budget scan tells it from the budget act by it". In the index version the scan did read it: `GameController.cs` (index) 6542 was `!divisions[d].Title.StartsWith(SimulationManager.TaxActTitlePrefix, ...)`. In the working tree 6543 is `!SimulationManager.IsStatuteActTitle(divisions[d].Title)`. `git grep TaxActTitlePrefix` now finds one reader, `SimulationManager.cs:1396`, plus the dated review record `Reviews/2026-10-03_s768_tax_act.md:64`.

3. **`ActWords`'s doc is also untrue for the tax act.** Line 1311 says "a statute act's name as its title and ledger event read it". For Rates, `StatuteActTitle` (1333-1334) goes to `TaxActTitle`, which reads the const, not `ActWords`. So the doc invites the edit that breaks things: rename `ActWords(Rates)`, and the slip (BudgetV35 1055), the pending card (GameController 9304) and the ledger event (1306) all change, but the title does not.

4. **The title is the scan's only guard against a signed tax act.**
   - `PolishStatuteActs` 1297 records the act with no axis, so it gets the default `BillAxis.Fiscal` (`ParliamentSystem.cs:1117`).
   - `PresidentialVetoGate` 1420 returns true on no veto and leaves the passage unmarked: not a Motion, `Required == 0`.
   - `QueueNewlyResolvedDivisions` (`GameController.cs:2284-2295`) adds the turn's effects to every non-motion division, so `Effects.Count > 0`.
   - The scan (6542) therefore passes a signed `"Tax act: ..."` division unless `IsStatuteActTitle` matches it.
   - With `ActWords(Rates)` renamed, the scan picks up the signed tax act, or the partner's (l) act, as the standing budget. That is exactly §768's defect 4: "The plate then cites 'Tax act: …' ... as the standing budget".

5. **No test would catch it.**
   - (j) at `PresidentialVetoDiagnostic.cs:378-379` asserts the literal `"Tax act: "` and `"Vetoed by the President (Karol Nawrocki): Tax act: "`.
   - (l) at 444 asserts the literal `"Tax act: "`.
   - (k) at 424/426 calls `IsStatuteActTitle` only on the pension act's title.
   - Nothing tests the night scan itself.
   - Renaming `ActWords(Rates)` leaves all three green. Renaming the const instead would turn (j) and (l) red, so only the finding's direction goes uncaught.

**Grade: minor.** There is no present behavioural defect, so it is not a defect. It is not just a note either: this diff turned the const's doc into a wrong claim about which code reads it, which breaks the repo's rule that a code change must leave no document wrong. It also leaves the guard for a previously fixed review defect resting on two strings that happen to be equal, with no test pinning it.

**The skeptic's corrected fix.** 1. Keep one source. At `SimulationManager.cs:1396`, build the title as `ActWords(BudgetBill.StatutePart.Rates) + ": " + (...)`. Then delete `TaxActTitlePrefix` and its doc (1315-1316): 1396 is its only reader in code (the Reviews/ mention is a dated record).
   - The titles stay byte-identical (`"Tax act: "`), so saves, divisions and films do not change.
   - `ActWords`'s doc at 1311 ("as its title and ledger event read it") then becomes true for every part.
2. If the named prefix is wanted, it cannot be a `const` derived from `ActWords`. Make it `public static string TaxActTitlePrefix => ActWords(BudgetBill.StatutePart.Rates) + ": ";` and change its doc to name its real reader (`TaxActTitle`), not the night scan.
3. Pin the guard on the tax act itself:
   - In `PresidentialVetoDiagnostic` (j), add `&& PoliSim.Simulation.SimulationManager.IsStatuteActTitle(added[1].Title)` to the shape check at 377-379.
   - Optionally add the same check on (l)'s `partnerAdded[0].Title` at 444.
   - A rename on either side then turns a named check red.

### 13. Doc comments left stale by §773

- **Lens:** ui - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/Simulation/SimulationManager.cs:1212

**The scenario.** Four call sites of BudgetActOf still say the budget act is 'without its rates': SimulationManager.cs:1212 and 1502, GameController.BudgetV35.cs:974, and GameController.cs:9297 ('without the rates a tax act takes'). It is now without every statute part. BudgetBill.cs:78-79 still says 'POLAND'S BUDGET IS TWO ACTS ... These three split a bill so', which §773's block below it supersedes. IsStatuteActTitle's doc (SimulationManager.cs:1318-1319) says 'the tax, pension, benefits or fund act a budget carried', and the night's comment (GameController.cs:6540-6541) says 'the budget act it rode with'. Both describe acts that ride a budget, but PartnerTaxAct's boundary tax act also matches and rides with no budget.

**The fix proposed.** Update the four call-site comments to 'Poland's budget act: the spending alone (§773)'. Mark the §768 block as superseded by §773. Mention the partner's boundary tax act in the IsStatuteActTitle and night-scan comments.

**The skeptic's evidence.** I checked every cited line in the working tree. All of them exist, and each one now describes less than the code does. None of them changes behaviour.

1. Four call-site comments. They are at SimulationManager.cs:1212 and :1502 and GameController.BudgetV35.cs:974 ("// §768: Poland's budget act without its rates"), and at GameController.cs:9297 ("§768: without the rates a tax act takes"). The diff left all four untouched; 9297 sits right next to the lines it rewrote. ParliamentSystem.BudgetActOf (ParliamentSystem.cs:897-902) now returns `bill.SpendingOnly(country)`. That bill drops all four statute parts, not just the rates. So each of these comments is incomplete, and read literally it implies the pension age, benefits and fund stay in the budget act's concern.

2. BudgetBill.cs:78-79 still says "§768 (Elias's ruling D4): POLAND'S BUDGET IS TWO ACTS ... These three split a bill so". Read on its own, "TWO ACTS" is now wrong: a bill can split into the budget act plus up to four statute acts. The author rewrote the same headline on PolishStatuteActs (SimulationManager.cs:1274, now "THE BUDGET ACT AND ITS STATUTES") but not this one. The §773 block at :121-124 contradicts it without marking it superseded. "These three split a bill so" is still true for ChangesTaxRates, TaxActPart and WithoutRateChanges.

3. The IsStatuteActTitle doc (SimulationManager.cs:1318-1319, new in this diff) says "the tax, pension, benefits or fund act a budget carried". The same diff adds PartnerTaxAct (:2692). It builds `new BudgetBill { GovernmentBill = true, TabledBy = written.Holder }` and calls PolishStatuteActs. That goes through StatuteActTitle and TaxActTitle (:1396, `return TaxActTitlePrefix + ...`), so a "Tax act: ..." division is recorded at the turn boundary with no budget behind it. The function's prefix match does catch it. Only the doc is narrower than the code.

4. The night comment (GameController.cs:6540-6541) was edited in this diff and still says "newer than the budget act it rode with on the same day". The partner's boundary act rides with no budget act. The filter at :6543 (`!SimulationManager.IsStatuteActTitle(...)`) still skips it correctly, so only the stated reason is incomplete.

I could not refute any of these. They are comment drift with no runtime effect, so this stays a note.

Missed by the finding: the TaxActTitlePrefix doc (SimulationManager.cs:1315) says "the night's standing-budget scan tells it from the budget act by it". The scan no longer reads this constant. It calls IsStatuteActTitle, which matches `ActWords(StatutePart.Rates) + ": "`. That is a second literal that equals "Tax act: " only because both are spelled the same way. If either is renamed, the night silently starts treating a passed tax act as the standing budget.

Aside, a separate real defect outside this finding: PartnerTaxAct's `new BudgetBill` leaves SwfShouldExist at its default, false (BudgetBill.cs:60). If Poland holds a fund, ChangesFund (BudgetBill.cs:168: `if (SwfShouldExist != (fund != null)) return true;`) returns true. PolishStatuteActs then records a spurious "Fund act: the sovereign wealth fund dissolved" division (StatuteActTitle :1358) and puts it to the veto gate. If it falls, it charges BillFailedApprovalCost and logs a "Fund act failed" ledger event, even though PartnerTaxAct discards the bill it returns. If it passes, the record says the fund was dissolved when it was not. AiFinanceMinistry.AsBudgetBill (AiFinanceMinistry.cs:120-125) avoids this by copying the standing fund into its bill; PartnerTaxAct does not. A Polish fund is reachable through the player's own budget Fund act. Test (l) asserts `partnerAdded.Count == 2` only on a world where Poland has no fund.

**The skeptic's corrected fix.** Comments:
- Change the four BudgetActOf call-site comments (SimulationManager.cs:1212 and :1502, GameController.BudgetV35.cs:974, GameController.cs:9297) to "§768/§773: Poland's budget act - the spending alone".
- Re-head BudgetBill.cs:78 as "§768 (D4), generalised by §773 below:", or reword it to the "THE BUDGET ACT AND ITS STATUTES" headline already used on PolishStatuteActs.
- Make the IsStatuteActTitle doc read "a statute act's - one a budget carried, or the Finance partner's boundary tax act (PartnerTaxAct)".
- Make the night comment say the act rode with a budget act "or, the partner's boundary tax act, with none".
- Reword the TaxActTitlePrefix doc (SimulationManager.cs:1315) so it no longer claims the night scan reads the constant.
- Better still, have TaxActTitle build its prefix from ActWords(BudgetBill.StatutePart.Rates) + ": ", so the title and IsStatuteActTitle cannot drift apart.

Separate defect (the aside): in PartnerTaxAct, either name the standing fund on the act, as AiFinanceMinistry.AsBudgetBill does (SwfShouldExist = fund != null, plus the six Swf fields), or vote only the Rates part, for example by giving PolishStatuteActs a parts filter. Add a case (l) variant on a Poland that holds a fund.

### 14. Claim convention: a transcribed count of BudgetBill's parts

- **Lens:** ui - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/Data/BudgetBill.cs:123

**The scenario.** The §773 block says the benefit levels and the fund's rules are 'the bill's two other parts that are not spending lines'. That is a DERIVED count of BudgetBill's fields. Adding a field such as another non-spending lever makes the comment wrong, and no check would notice.

**The fix proposed.** Reference rather than count, e.g. 'the bill's other non-spending parts (StatuteParts)', or delete the count.

**The skeptic's evidence.** I could not refute it. The line is in the unstaged E2 diff, at G:/UNITY/Projects/PoliSim/Assets/Scripts/Data/BudgetBill.cs:121-124 (the word "two" is on line 123):
  "// ... and - READING, DECLARED, the general rule's - the benefit levels and the sovereign fund's rules, the bill's two // other parts that are not spending lines."

1. The comment is true today, so this is about the convention, not about wrong behaviour. BudgetBill's fields group as follows:
   - Rates: TaxLines and BracketRates.
   - Spending: SpendingPercentChanges, SpendingNominalTargets and SpendingPinChanges.
   - Benefits: WelfarePrograms.
   - Fund: the Swf* fields.
   - Pension age: PensionAgeSet and PensionAge.
   - Bookkeeping: GovernmentBill, TabledBy, the FinanceStance* fields and DaysRemaining.
   Only two non-spending parts are left besides rates and the pension age, so the sentence is a copied DERIVED fact, a "there are N of X" claim about BudgetBill's makeup.

2. The rule covers source comments. The CLAUDE.md head says THE CLAIM CONVENTION "governs every document AND every source comment". It counts "a count ... a *there are N of X*" as DERIVED, allows only GENERATED, REFERENCED or DELETED, and says "Nobody transcribes, anywhere, in any file." Elias's P2 kickoff in COMPLETED.md also says "no transcribed counts".

3. The repo has already been caught by exactly this. Assets/Editor/CheckSuite.cs:144-150: "It used to, and it was WRONG: it read 'TWENTY-ONE' while the array held twenty-five, and no check in the bar could see it ... only the NUMBER was stale ... a count is referenced or generated, never transcribed — in a comment exactly as in a document."

4. "No check would notice" is correct. CommentClaimCheck.cs:43 only matches a backticked `Type.Member` (regex `([A-Z][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)`), so a count written as a word is invisible to it. DocumentClaimCheck reads documents, not .cs comments.

5. The scenario is realistic. BudgetBill has grown one lever at a time (P5-B2 nominal targets, F4-4 bracket rates, PN-1 §590 pension age, §755/§768 stance fields). If someone adds another non-spending lever, "the bill's two other parts" becomes false, and the comment will still read as proof that StatuteParts is complete. That is the exact moment the claim matters, because SpendingOnly would let the new lever into the budget act.

Severity is a note: the comment has no runtime effect and is accurate today. There is a similar copied count in the same file, "These three split a bill so." at line 79, but it is pre-existing §768 text and outside E2's diff.

**The skeptic's corrected fix.** Removing only the word "two" is not enough. "The bill's other parts that are not spending lines" would still claim that these two are all of the bill's non-spending parts, and that is the half that can go stale. Keep the reason, drop the count and the claim that the list is complete, and point to the array, for example:
  "... and - READING, DECLARED, the general rule's - the benefit levels and the sovereign fund's rules, which are not spending lines either: the parts that leave the budget act are whatever `BudgetBill.StatuteParts` lists. The budget act keeps the spending lines alone."
Because `BudgetBill.StatuteParts` is backticked, CommentClaimCheck will check that it resolves.

### 15. PresidentialVetoDiagnostic's class summary does not name cases (j), (k) or (l)

- **Lens:** ui - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Editor/PresidentialVetoDiagnostic.cs:22

**The scenario.** The summary's 'Checked:' paragraphs (22-36) list cases (a)-(i). The §768 budget case (j) and the new §773 cases (k) and (l) are not named, so a reader of the header does not learn that the diagnostic covers the budget acts and the partner's tax act.

**The fix proposed.** Add one line each for (j), (k) and (l) to the summary.

**The skeptic's evidence.** I could not refute the finding. It is accurate, but it only affects documentation.

Assets/Editor/PresidentialVetoDiagnostic.cs:22-36. The summary's "Checked:" list runs from (a) to (i) and ends at line 36: "and (i) drives one statute through the game's own bill path - introduced, resolved, its effect withheld where the veto stands.</para>". I searched lines 14-37 for "pension", "partner", "tax act", "two acts", "§768", "§773" and "spending" and found none of them. The only budget wording there is about the exemption: line 19 says "the budget act ... cannot be vetoed" and line 26 says "(e) the budget act exempt". Neither describes the two-act split.

Where the three cases are:
- Line 335: "// (j) §768 (Elias's ruling D4): POLAND'S BUDGET IS TWO ACTS". This case is already in HEAD. It arrived in 454a23b (s768), and that commit did not update the summary. So the gap for (j) predates E2. The s768 review (Reviews/2026-10-03_s768_tax_act.md) did not raise it, so this is not a point already decided.
- Line 392: "// (k) §773 (Elias's ruling E2)". New in the unstaged E2 diff.
- Line 431: "// (l) §773 (E2): ... The Finance partner's boundary step". New in the unstaged E2 diff.

The E2 diff to this file is one hunk (@@ -388,6 +388,65 @@) and changes no line of the summary. E2 therefore adds two cases without a summary line, on top of the older gap for (j).

The file's own practice is to keep this summary up to date. Commit 6975574 (s761) added "(h) drives the gate itself ... and (i) drives one statute ..." to the summary in the same commit as those cases. The s761 review (Reviews/2026-10-02_s761_veto_live.md:37-38) flagged stale header text in this same file, and that was fixed.

Why it is only a note: nothing reads the summary. CLAUDE.md:37 says "the code can change freely and no document becomes wrong — only incomplete". The summary is incomplete, not false, and nothing in the cheap bar checks it. The registry comment at CheckSuite.cs:218 names only (a)-(e) and was already incomplete before this change. It is outside this diff.

**The skeptic's corrected fix.** Extend the summary's last <para> after "(i) drives one statute through the game's own bill path - introduced, resolved, its effect withheld where the veto stands" with a sentence per case. Keep these sentences free of transcribed figures (no planted seat counts, no rates), as the claim convention requires:

"; (j) (§768, ruling D4) a player's budget moving a rate and a spending line, stepped through the game's own path: the budget act adopted, the rate in its own tax act, vetoed where PiS opposes it, the old rate standing while the spending moves, and a budget that changes no rate one division; (k) (§773, ruling E2) a pension-age step in its own pension act, vetoed, while the budget act carries the spending alone; (l) (§773, ruling E2) the Finance partner's boundary rate rise put to the Sejm as a tax act its party tables, voted then vetoed, the rate leaving the turn's decision and the step keeping only the spending lines' part."

Optionally, extend the CheckSuite.cs:218 registry comment in the same way. That gap is older and outside this diff.

### 16. In Poland the Budget page's Finance-stance tile and the turn preview do not mention the tax act or the veto for the partner's rates

- **Lens:** ui - **reviewer:** note - **skeptic:** note
- **Where:** Assets/Scripts/UI/GameController.BudgetV35.cs:326

**The scenario.** The tile still tells the player the partner acts 'THROUGH THE LINES - IN A TIGHTENING THE INCOME TAX AND VAT ONLY FOR WHAT THE LINES' LIMITS LEAVE'. It says nothing about that part now being a tax act the Sejm votes and the President may veto. The turn preview applies the partner's rates whole (SimulationManager.cs:5185-5188) with no veto warning beside it. D4's accepted 'the slip's veto warning stays beside it' has no counterpart for the partner's step.

**The fix proposed.** Where PresidentialVeto.Applies, add a clause to the tile's census, e.g. 'IN POLAND THE RATES' PART IS A TAX ACT - THE SEJM'S VOTE, THEN THE PRESIDENT'S VETO'. Optionally add a projected veto line using ChamberVerdicts.Veto on the step's rates.

**The skeptic's evidence.** I could not refute it. The gap is real, but narrower than the finding says, and it does not breach the ruling.

What holds:
- GameController.BudgetV35.cs:325-326 (unchanged by the diff): the census still reads "...AT MOST 0.25 PP OF GDP A YEAR, THROUGH THE LINES - IN A TIGHTENING THE INCOME TAX AND VAT ONLY FOR WHAT THE LINES' LIMITS LEAVE". It says nothing about a tax act or a veto. The census is a slip line (LawsV35.cs:356/1237 `slip.Add(face.Census)`), so a clause fits without touching the caption band.
- The diff adds SimulationManager.cs:4770-4774. When `partnerWrote.Taxes.Count > 0 && PlayerCountryId == country.Id && PresidentialVeto.Applies(country.Id)`, it calls `PartnerTaxAct` (2692-2705). That method records a division and runs `PresidentialVetoGate`. If the act falls, it removes the rates from `decision.TaxRateOverrides` and subtracts `RatePoints`.
- The turn preview (SimulationManager.cs:5185-5188) calls `FinancePartner.Apply(previewedReal, decision, ...)` and then `ApplyTaxRateChanges(previewCountry, decision)`. `PartnerTaxAct` is never consulted, so the rates count whole in both `PreviewTurn` results (GameController.cs:4690-4693). Those results feed the fiscal header's projected balance and the Statistics projection. The comment in `BuildPlayerDecision` (GameController.cs:6294-6295) states the preview's own principle: "no longer shows an effect that won't actually happen until a bill passes".
- The tile is the only UI reader of `FinancePartner`. The veto surfaces are the if-passed slip for the player's draft (BudgetV35:1041) and the pending cards (GameController.cs:9298). Neither covers the boundary step, which is voted at the boundary and is never a pending bill.
- The scenario can occur in a live game. `AiFinanceMinistryEnabled` defaults to true (SimulationManager.cs:707), and `GovernmentRecord.AllocatePortfolios` can give Finance to a partner (GovernmentRecord.cs:55).

What narrows it:
- The player-partner ("mine") tile and the AI-head case use the government's-bill path. There the partner's rates ride the government's pending bill (`TableGovernmentBudget`, 1175-1177), and the pending cards already list "Tax act with the budget" with its veto lean (GameController.cs:9298-9306). The gap is only §773's new boundary path: the player is PM and an AI partner holds Finance.
- The rates enter the step only when the lines' clamps leave a remainder of a step of at most 0.25 pp of GDP. The clamps are 15 % and 30 % (SimulationManager.cs:692/700; `FinancePartner.Tighten`). `BuildPlayerDecision` carries no lines, and `Free()` excludes only pinned lines. So in practice this needs the player to have pinned most lines, which is rare.
- It is not a ruling breach. E2's "the budget preview applies the whole draft: accepted. The slip's veto warning stays beside it" answers §768's owed item about the player's own draft. E2 asks for no surface for the partner's step.
- The census is still accurate, only incomplete. The fallen act is reported afterwards in the divisions record and the approval ledger ("Tax act failed").

Graded a note: copy completeness plus a small preview/turn mismatch in a rare case.

**The skeptic's corrected fix.** In `DrawBudgetFinanceStance`, where `PoliSim.Elections.PresidentialVeto.Applies(PlayerCountryId)`, add a clause to the census for both the AI and the "mine" wording. Do not say "IN POLAND"; follow the if-passed slip's style, for example: " · WHERE THE PRESIDENT HOLDS A VETO, THE RATES' PART IS A TAX ACT OF ITS OWN - THE SEJM VOTES IT, THEN THE PRESIDENT; WHERE IT FALLS THE OLD RATES STAND AND ONLY THE LINES' PART COUNTS".

The turn preview can stay as it is, by extension of D4's accepted premise. In that case, state it in §773's record as an owed or declared item: the preview counts the partner's boundary rates whole, and the tile's clause is the warning beside it.

A projected veto line is optional. It would compute the step on a scratch decision: `FinancePartner.Apply(c, PolicyDecision.None(), ...)`, then a rates-only `BudgetBill` concern, then `ChamberVerdicts.Veto`. It is more work than the census clause.

## The first pass - refuted by the skeptics

- [money] PartnerTaxAct passes a freshly built BudgetBill (fund unnamed) through the whole statute loop: in a Poland that has a fund, every boundary tax act also records a passed, signed 'Fund act: the sovereign wealth fund dissolved' that never happened - *THE MECHANISM IS AS DESCRIBED, BUT IT CANNOT RUN IN PLAY. It is a latent trap, not a failure anyone can reach today.

What checks out, line by line:
- SimulationManager.cs:2694 builds the act as `new BudgetBill { GovernmentBill = true, TabledBy = written.Holder }`, so SwfShouldExist keeps its default of false (BudgetBill.cs:60).
- BudgetBill.cs:168 (`if (SwfShouldExist != (fund != null)) { return true; }`) therefore makes the Fund part count as a change whenever a fund stands.
- PartOf(Fund) carries no tax, spending, welfare or pension move. GetBudgetBillConcern adds nothing, so IsEmpty is true (StanceModel.cs:63) and WouldBillPass returns true (ParliamentSystem.cs:334).
- RecordDivision writes no sides. PresidentialVeto.Vetoes needs clubMembers > 0, so it never vetoes.
- The title is "Fund act: the sovereign wealth fund dissolved" (SimulationManager.cs:1358). The bill that comes back is thrown away (2696). QueueNewlyResolvedDivisions (GameController.cs:2280-2298) queues any non-motion record for a signing.
- The result would be a false record. Money and approval are untouched, because the Fund act cannot fall.

Why it cannot happen:
1. PartnerTaxAct has one caller, SimulationManager.cs:4771-4774. That call runs only when `partnerWrote.Taxes.Count > 0`.
2. Taxes are written only by FinancePartner.Tighten (243-280), and only when a single uniform cut over the free lines falls short. A free line is one that is not pinned and not already in the decision's spending changes (FinancePartner.cs:214-215).
3. At the boundary, the partner steps in the player's country only where the player governs (PartnerStepsAtBoundary, 2682-2683). There the ministry writes nothing (`aiGovernment` is false, 4756). The decision comes from GameController.BuildPlayerDecision (6277-6297), which carries InterestRateChange and no spending lines.
4. Nothing in play pins a line. The only code that sets SpendingPinChanges is in two Editor diagnostics (LeverLivenessCheck.cs:417, SpendingIndexationDiagnostic.cs:65). BuildBudgetBillFromDrafts never sets it, and `git log -S SpendingPinChanges` finds no UI or Testing code that ever did.
5. So every line is free. Poland's lines total 18% of GDP (WorldFactory.cs:184, 1750). A step is at most 0.25 pp of GDP (FinancePartner.cs:37, 191), which makes a uniform cut of about 1.4% per line. The line limits are 30% and 15% (SimulationManager.cs:692, 700). Nothing is left over, Taxes stays empty, and PartnerTaxAct never runs in play.
6. No tool gives Poland a fund. The only funds created are Sweden's and France's seeds, the Sweden scenario, the test runner's USA and Germany, and SwfDrawdownBooksDiagnostic's Sweden. Case (l) calls the method by reflection on a Poland with no fund.

The trap goes live as soon as something lets the partner's step reach the tax rates in a player-led Poland that has a fund, for example a pin control or a decision that carries spending lines. A side point for the orchestrator: the same gate means E2's partner tax-act clause cannot be reached from play today. Only case (l) exercises it.*
- [money] The turn preview still writes the partner's household rates with no forecast of the tax act, so in the player's Poland it shows rates (and no failed-act cost) that the boundary may withhold - *The code fact is right, but the failure can't happen in live play.

THE CODE FACT. In PreviewTurnOnClone, SimulationManager.cs 5185-5190 calls FinancePartner.Apply(previewedReal, decision, ...) and then ApplyTaxRateChanges(previewCountry, decision). PartnerTaxAct runs only at the boundary (4771-4774). The preview never calls it.

THE PRECONDITION NO LIVE STATE REACHES. The boundary branch, and so the gap, needs `partnerWrote.Taxes.Count > 0` in the player's Poland.
- FinancePartner.cs 261-280: Tighten writes a household rate only for the `shortfall` left after one uniform cut on every Free line, each clamped to RangeOf: 15% mandatory, 30% discretionary (SimulationManager.cs 692/700).
- Free (FinancePartner.cs 214-215) = `!line.Pinned && !decision.SpendingLineChanges.ContainsKey(..) && !claimed`.
- The partner steps at the boundary in the player's country only when the player governs: PartnerStepsAtBoundary 2682-2683, with FinancePartnerRuns requiring AiFinanceMinistryEnabled (2672-2673), and PlayerGoverns meaning the PrimeMinister role (2590-2599).
- In that case the decision is BuildPlayerDecision (GameController.cs 6277-6301), which carries only InterestRateChange. The ministry writes nothing (aiGovernment is false), and the preview's claimedByMinistry is null. So every unpinned line is Free.
- No live path pins a line. SpendingPinChanges is written only by Editor harnesses (LeverLivenessCheck.cs 416-417, SpendingIndexationDiagnostic.cs 65). `git log -S SpendingPinChanges -- Assets/Scripts/UI` is empty: the UI never wrote it.
- PreviewTurnWithBudgetDraft (5093-5097) applies the draft to the clone, and the partner reads the real country's lines, so a draft can't claim lines either.

THE NUMBERS FOR POLAND.
- Every line is `isMandatory: false`, so the clamp is 30% (WorldFactory.cs 1784, 1788).
- The lines sum to GDP x GovernmentSpendingRate, which is 18f (line 184).
- The step is at most StepPointsPerYear = 0.25 pp of GDP (FinancePartner.cs 37, 191).
- So the uniform cut is about 0.25/18 ≈ 1.4% per line, or about 7% even if every line sat at the 0.2x seed floor. Both are far under 30%.
- So `shortfall` is 0 and `Taxes` stays empty. Neither PartnerTaxAct nor the preview gap fires. The scenario's "the preview shows IncomeTax/VAT rising" can't occur.

THE INSTRUMENT CLAIM. The finding says a parity instrument would see a gap. That is hypothetical.
- PreviewParityDiagnostic never seats a player. SetWorld doesn't set the nullable PlayerCountryId (2430, 2535). The §773 branch is gated on `PlayerCountryId.Value == country.Id`, so it can't fire there.
- LeverLivenessCheck pins lines but neither seats a player nor previews.

PART OF THE SCENARIO IS ALSO WRONG. The preview never calls FinancePartner.Record, so it does not show "the stance moved by the whole step".

SIDE FACT FOR THE REPORT. The same unreachability means §773's PartnerTaxAct runs today only through test (l)'s reflection call with a hand-built Written (`written.Taxes.Add(moved.Type)`), never through FinancePartner.Apply. The rule is installed and correct where it is reached, but no live state reaches it.*
- [money] The ruling's 'whoever proposes it' is built only for the player's Poland: in an AI-governed Poland the partner's and the ministry's rates still move at the boundary without a vote - *The behaviour the finding describes does exist. Its premises, that the ruling has no such limit and that the scope is left implicit, do not hold.

1. The behaviour, confirmed in SimulationManager.cs:
   - 4771: `PartnerTaxAct` runs only where `PlayerCountryId.Value == country.Id`.
   - Ministry, 4756-4763: for a non-player Poland `aiGovernment` is true and `ministryByBill` is false, so `AiFinanceMinistry.Apply` writes into the boundary decision. `IsEuMember` (AiFinanceMinistry.cs:70, `id != USA`) sends Poland to `ApplyEuRule`, whose `RaiseHouseholdRates` (:246-248) writes IncomeTax/VAT overrides.
   - Partner: `FinancePartnerRuns`/`PartnerStepsAtBoundary` (2672-2683) is true for any non-player country while the ministry switch is on. `FinancePartner.Tighten` (FinancePartner.cs:271-279) writes household rates when `Holder` (:117-127) is a partner outside the PM's party and group.
   - None of these rates is voted on.

2. The ruling answers an item that was itself limited to the player-governed Poland. E2's line "D4, the Finance partner moving rates without a vote: overruled" answers §768's owed item. COMPLETED.md:36748 words it: "the Finance partner's boundary step on a player-governed Poland moves rates with no Sejm vote (§755's path)". Reviews/2026-10-03_s768_tax_act.md:67 has the same wording. "Whoever proposes it" covers every proposer within that scope, and the build covers each one:
   - player head: `AdvanceBudgetBillDay` -> `PolishStatuteActs` (1505)
   - AI head's ministry, and any partner, AI or player, under an AI head: `TableGovernmentBudget` (1172-1177) -> `ResolveGovernmentBudget` -> `PolishStatuteActs` (1266)
   - AI partner under a player head: `PartnerTaxAct` (4771-4773)
   - `PartnerStepsAtBoundary` is false for the player's country under an AI head, so no boundary step escapes.

3. AI countries writing their book without a bill is ruled standing design, not an omission:
   - 2385 (PS-3e, §632, ruled): "PLAYER PATH ONLY: a country that is not the player's never comes here (the ministry writes its book direct)".
   - 2606 (§630): "the AI writes its book without a bill".
   - A non-player Poland has no budget act either; its spending also moves at the boundary unvoted. A tax act for its rates alone would invert E2's own structure ("only spending stays in the budget act"). So the finding's second fix, extending the act to an AI Poland, is not coherent without an AI legislature that E2 does not ask for.

4. The scope has already been recorded and stated:
   - §768's first review, question 3: "A non-player Poland gets its rates from the ministry with no chamber vote; that is the game's scope for AI countries."
   - The call-site comment at 4769-4770 says "In the player's Poland".
   - The §773 record is not yet written (COMPLETED.md ends at §771).

Residual nit only: `PartnerTaxAct`'s doc heading (2686) reads "THE FINANCE PARTNER'S RATES IN POLAND ARE A TAX ACT", which is broader than its one caller's gate.*

## What the author did about the first pass

- **4 and 7 (the defect) - fixed.** `PolishStatuteActs` takes the parts to vote (`only`, defaulting to every part the bill changes), and `PartnerTaxAct` passes the rates alone. A bill built new names no fund, so the fund's act read it as a dissolution nobody asked for.
  - Case (l) now plants a fund on Poland for the partner's act, and asserts exactly the tax act and its veto: no fund act, and the fund kept.
- **1 and 5 (the fund act uncontested) - declared, and put to Elias.** The `BudgetBill` §773 comment and the `PolishStatuteActs` summary say it plainly: the fund's act is uncontested by construction, because the chamber's concern weighs none of the fund's terms. It always passes and is signed; its separation adds a division and a signing, never an outcome.
  - **The question for him:** keep the fund's rules in the budget act, or give them a concern (the contribution as a spending move).
- **2 - fixed.** After a fall, the partner's move line states what lands: the lines' part, in points of GDP, or that nothing of the step lands.
- **3 and 9 - taken.**
  - **(l) rates-only:** a step with no lines whose tax act falls. Nothing lands, nothing is counted, the year's step is spent - asserted after `FinancePartner.Record`.
  - **(m) the boundary itself:** the turn's own `AdvanceTurn`, on a planted head (NL) with Finance planted on KO, which sits to NL's right and so asks a tightening, and every spending line pinned so the step falls wholly to the household rates. The boundary put the rates to the Sejm as a tax act, and the turn applied them only where it stood. On this chamber it stood; the fall path is (l)'s.
- **6 and 10 - fixed.**
  - **The fund act's title** lists every rule it moves, from and to.
  - **A statute act's pending card** whose concern is empty reads *Uncontested - the chamber weighs none of its terms; it passes, and is signed, if the budget passes*, not *no change requested*. It uses a label-keyed wording, so the other cards are untouched.
- **8 - taken.**
  - **(k2):** one bill changing every statute part; its budget act changes none of them and keeps the spending.
  - **(k3):** a benefits act through the game's own path. A cut to means-tested welfare passes the Sejm, PiS opposes it, it is vetoed, and the level stays.
  - **(k4):** a fund act through the same path - uncontested, passed and signed, the fund created.
  - **A title check:** the night's scan reads every act's title as a statute act's, and the budget act's as none.
- **11 - already done.** E2's C4 relabels landed in E1's staged file (the reviewer read the unstaged diff alone).
- **12 - fixed.** The tax act's title builds its prefix from `ActWords`; `TaxActTitlePrefix` is gone, so the title and `IsStatuteActTitle` cannot drift.
- **13 and 14 - fixed.**
  - The four `BudgetActOf` call-site comments now read "the spending alone".
  - The `BudgetBill` §768 header points to §773.
  - `IsStatuteActTitle`'s doc names the partner's act.
  - The night comment is updated.
  - The count of parts is dropped for a pointer to `BudgetBill.StatuteParts`.
- **15 - fixed.** The diagnostic's summary names (j) to (m).
- **16 - taken.** In Poland, the Finance-stance tile says the rates' part is a tax act of its own, voted and then put to the President. The turn preview counting the partner's boundary rates whole is stated in §773's record, as an extension of D4's accepted premise.
- **Measured:** `n773b`, 4 of 4 clean, every new case passing.

## The second pass - confirmed (verbatim)

The same workflow on the unstaged diff after the first pass's fixes, the author's response read first. No defect; every finding a minor or a note.

### 1. The fund act's declaration 'changes no outcome' is not true. It is voted last and always passes, so it hides an earlier act that fell from the boundary's cabinet-pressure roll

- **Lens:** fixes - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Simulation/SimulationManager.cs:1283

**The scenario.** PolishStatuteActs votes the parts in StatuteParts order, so Fund is last (BudgetBill.cs:133). The fund act's concern is always empty, so it always passes. CabinetSystem.UnderPressure (CabinetSystem.cs:472-477) reads only the newest division: it applies pressure when that entry is !Passed, !Motion and dated today. The game calls it at every boundary, TryRollCabinetEvents(country, CurrentDate) in ApplyDomesticPolicy (SimulationManager.cs:5066). On a boundary day the day's bill countdowns run before AdvanceTurn (GameController.cs:773-791). Example: the player's Polish budget creates the fund and cuts income tax, which PiS opposes and the President vetoes, as in (j). The player introduces it so that it resolves on a turn-boundary day, approval is >= 40 and ministers are seated. The log then reads 'Annual budget bill | [motion] Tax act ... | Vetoed ... the veto stands | Fund act: the sovereign wealth fund created'. UnderPressure reads the passed fund act and returns false, so no loyalty roll runs: no resignation, no leak (each leak costs ReshuffleApprovalCost), and no cabinet-stream draws. Under §768, or with a bill that leaves the fund alone, the vote on the veto is newest and the government is under pressure. So the separation does change an outcome. The first pass's skeptic named exactly this qualification (finding 5, point 5). The new text at SimulationManager.cs:1282-1283 ('changes no outcome') and BudgetBill.cs:126-127 ('never an outcome') says otherwise, and that is the premise put to Elias.

**The fix proposed.** Qualify the declaration in both places: the fund act changes no outcome for the fund's own rules, but because it is voted last it becomes the day's newest division, which CabinetSystem.UnderPressure reads. Put that to Elias with the question. Two code fixes are possible, each a behaviour change that needs his ruling: vote Fund first, or have UnderPressure weigh every non-motion division dated today. Note that UnderPressure already breaks DivisionLog's write-only rule (DivisionRecord.cs:71-77).

**The skeptic's evidence.** I could not refute it. The path runs in the working tree.

1. **The fund act is voted last and always passes.**
   - BudgetBill.cs:133 sets `StatuteParts = { Rates, PensionAge, Benefits, Fund }`.
   - SimulationManager.cs:1294 runs `foreach (part in only ?? BudgetBill.StatuteParts)`. A fallen part only goes into `fell`, and the loop goes on, so a player's bill (`only` null) that changes the fund always votes the fund act after every other act.
   - The fund act's concern is empty. ParliamentSystem.cs:334 then returns true (`if (concern == null || concern.IsEmpty) { return true; }`), and RecordDivision (1117-1130) appends a passed record dated `CurrentDate`.

2. **What the fund act hides.**
   - A vetoed act leaves its passage marked `Motion = true`, followed by `country.Divisions.Append(title, CurrentDate, ..., veto.Overridden, ...)` (SimulationManager.cs:1437): a failed record that is not a motion.
   - Under §768 that veto record was the newest entry. With a fund change in the same bill, the passed fund act now comes after it.

3. **The reader.**
   - CabinetSystem.cs:472-477 checks `entries[entries.Count - 1]` alone: `!Passed && !Motion && Date == today`, plus approval below 40.
   - It is the only reader of `Divisions.Entries` under Assets/Scripts/Simulation. The other hits are writers that set `.Motion` or `.Contest` on the record just appended.
   - It breaks the write-only rule at DivisionRecord.cs:71-72. That has been so since P2-5.2 (617fcf0).

4. **The same day.**
   - In GameController.cs:772-791 the day loop runs `AdvanceDay` (the only mover of `CurrentDate` in play, line 314), then `AdvanceCountryDayTick` (where `AdvanceBudgetBillDay` runs first), then `QueueNewlyResolvedDivisions`, then `AdvanceTurn`.
   - SimulationManager.cs:5066 calls `TryRollCabinetEvents(country, CurrentDate)` with no gate, so a budget that resolves on a boundary day is the newest entry the roll reads.
   - When `UnderPressure` is false the roll returns before drawing (CabinetSystem.cs:489). So there is no resignation and no leak (2 points each), and the Cabinet stream makes no draws.

5. **It is reachable in play.**
   - `BudgetWindowHolds()` is `GetPendingBudgetProcess && GameSettings.HoldOnBudgetWindow` (GameController.cs:1387). It defaults on but is a settings toggle (1531-1532).
   - The open process is removed only when a bill is introduced or the government changes (1480, 1180, 3124, 4581). With the hold off, the player can introduce 21 days (`BillDurationDays`) before a boundary.
   - The fund can be drafted for any country (GameController.cs:10317-10327; the bill builder at 9830).
   - Ministers can be seated in play (PoliticsV35.cs:456). Every candidate's loyalty is below 100.
   - Approval is seeded at 50 (WorldFactory.cs:1028), and a fall costs 1.5.

6. **The text is false in this case.**
   - SimulationManager.cs:1282-1283 says the separation "changes no outcome".
   - BudgetBill.cs:126-127 says "never an outcome".
   - The first pass's skeptic named this qualification (finding 5, point 5), and the response did not carry it.

7. **Two refinements.**
   - The finding's "with a bill that leaves the fund alone, the vote on the veto is newest" is not quite right. A pension or benefits act that passes after a vetoed tax act hides the veto the same way. So hiding comes from §773's several acts on one day, not from the fund alone. The fund act causes it every time it is present, because it always passes and is voted last.
   - The effect is small: one probability-scaled roll (0.12 × disloyalty per minister), and only on a boundary day the player times on purpose. No money-path figure is wrong. Note is the right grade.

**The skeptic's corrected fix.** Change the text only, and put the question to Elias. Do not change behaviour until he rules.

1. **Qualify the declaration in both places** (the BudgetBill.cs §773 block and the `PolishStatuteActs` summary). Suggested wording: "The fund act changes nothing the fund's rules come to. But it is a division of its own, voted last and always passed. On a turn-boundary day it is the newest division `CabinetSystem.UnderPressure` reads, so it hides an earlier act's fall from the boundary's loyalty roll. Any later act that passes does the same." Name the reader by reference, not by line number, to keep to the claim convention.

2. **Add the point to the question for Elias**, beside "keep the fund's rules in the budget act, or give them a concern".

3. **Offer the code options as rulings**, each a behaviour change:
   - (a) Vote the fund act first. This removes only the fund's share of the hiding. A passing pension or benefits act would still hide a vetoed tax act.
   - (b) Make the pressure test count any failed division of the day that is not a motion. Better still, have the writer set a per-country "a bill was lost on date X" field where an act or bill falls, and have `UnderPressure` read that field instead of the log. That closes the general case and also ends `UnderPressure`'s breach of DivisionLog's write-only rule.

4. **Test.** If (a) or (b) is ruled, add a diagnostic case: a fund change plus a vetoed tax act resolving on a boundary day must leave `UnderPressure` true.

### 2. On a fall, the statute act's log line says 'the rest of the budget applies' for the partner's boundary act, which has no budget

- **Lens:** fixes - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Simulation/SimulationManager.cs:1302

**The scenario.** PartnerTaxAct reuses PolishStatuteActs, whose fall wording is fixed: 'falls - what it would set stays as it is, and the rest of the budget applies'. In n773b.log (lines 697 and 738, from (l)'s two invocations) the partner's act logs 'BUDGET: Poland - Tax act: income tax 12.36 % to 10.36 %: falls - what it would set stays as it is, and the rest of the budget applies'. The boundary act would log the same, but no budget is in play there: what remains is the lines' part of the partner's step, which the LEVERS line now states correctly. Only the log is affected; nothing parses 'BUDGET:' lines (grep of Tools and Assets/Editor).

**The fix proposed.** Make the fall wording neutral ('falls - what it would set stays as it is'), or pass a context string from PartnerTaxAct ('... and the lines' part of the partner's step applies').

**The skeptic's evidence.** PATH EXISTS AND IS MEASURED.
1. SimulationManager.cs:1302 (PolishStatuteActs) uses fixed wording with no context: `Debug.Log($"BUDGET: {country.Id} - {title}: {(passed ? "stands - it applies" : "falls - what it would set stays as it is, and the rest of the budget applies")}");`
2. SimulationManager.cs:2704 (PartnerTaxAct) runs through the same line: `PolishStatuteActs(country, act, out List<BudgetBill.StatutePart> fell, new[] { BudgetBill.StatutePart.Rates });`. Here `act` is a bill built new from the boundary decision's TaxRateOverrides. No budget carries it.
3. n773b.log lines 697 and 738 read exactly as the finding quotes. Their stacks are PolishStatuteActs:1302 <- PartnerTaxAct:2704 <- PresidentialVetoDiagnostic.cs:451 and :472, which are case (l)'s two calls. The log was written at 21:17:24, after SimulationManager.cs was last saved at 21:12:01, and the line numbers match the working tree.

NO BUDGET IS IN PLAY AT THE BOUNDARY.
- AdvanceTurn (around SimulationManager.cs:4776-4790) calls PartnerTaxAct, then Record, then the LEVERS log, then ApplyDomesticPolicy(country, decision).
- In the player's Poland, only the partner writes lines and rates into that decision:
  - The UI never writes SpendingLineChanges or TaxRateOverrides. The only writers in Assets/Scripts are AiFinanceMinistry.cs:213/231/247, FinancePartner.cs:233/256/276 and the screenshot harness.
  - For the player's country the ministry goes by bill (`ministryByBill`).
  - The player's own budget is a BudgetBill (GameController.cs:9588, IntroduceBudgetBill).
- So "the rest of the budget" names nothing. What remains is the lines' part of the partner's step.
- Line 738 is the rates-only case. In play, the LEVERS line printed right after it ends "nothing of the step lands" (SimulationManager.cs:2713), while the BUDGET line says "the rest of the budget applies".

THE IMPACT IS THE LOG ONLY, SO THE GRADE IS NOTE.
- A repo grep for "rest of the budget", "stays as it is" and "stands - it applies" finds only line 1302. No diagnostic asserts the text, so rewording it breaks nothing.
- Tools/ has no parser for BUDGET lines. The only hit is the tab name in text_baseline.pl:76.
- Of the logMessageReceived handlers, CheckSuite.cs:54 and UiScreenshotDriver.cs:207 count errors only, and D18Inventory.cs:361 collects check output (an Editor tool, not a player surface).
- The UI never shows this wording. The pending cards use their own label-keyed text (GameController.cs:9305-9308).
- Game state is right: fell=[Rates], the rates leave the decision, and StancePoints is reduced.
- The "stands - it applies" branch is neutral and correct for both callers (n773b.log:831, case (m)).

**The skeptic's corrected fix.** Either fix works, since no check reads the text.
(a) Neutral wording at SimulationManager.cs:1302: `passed ? "stands - it applies" : "falls - what it would set stays as it is"`. The budget's outcome already has its own BUDGET line (ResolveGovernmentBudget, line 1271), and the partner's landed part is on its LEVERS line.
(b) To keep the budget wording, give PolishStatuteActs an optional `string restWords = "and the rest of the budget applies"`. PartnerTaxAct then passes boundary wording, for example "and the rest of the partner's step is as its LEVERS line states".

### 3. (l)'s reflection fixture is still not shaped like a partner's move (an income-tax cut, with the head's own party as Holder), so the fall path runs only on a move the partner cannot make

- **Lens:** fixes - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PresidentialVetoDiagnostic.cs:443

**The scenario.** Both (l) invocations reuse (j)'s moved/newRate, logged as 'income tax 12.36 % to 10.36 %' (n773b.log:940), a CUT. They also set Holder = "KO", which is the head's own party in the default epoch, and FinancePartner.Holder never returns the head's party (FinancePartner.cs:124). FinancePartner.Tighten only ever raises IncomeTax and VAT (FinancePartner.cs:271-279). The (l) comment at 437-438 says 'where it raises the household rates'. The response marks finding 9 'taken', but its fixture fix was not applied: 'use a household-rate rise with a Holder that Holder() can return'. (m) drives a real partner-shaped rise through the boundary, but on its chamber the act stood (n773b.log:946). So the fall branch at the boundary (the rates left out of the turn, the lines' part credited, the step spent) is exercised only on this cut fixture. The mechanics do not depend on direction, so this is a realism gap in the tests, not a wrong result.

**The fix proposed.** In (l), build the Written from FinancePartner.Apply on a world planted the way (m) plants it (a different head, Finance on a partner to its right, lines pinned), or take a rise found by the same search (j) uses. Or plant (m)'s seats so the partner's rise falls, then assert that income tax is unchanged after the turn, that FinanceStanceApplied is 0 and that FinancePartnerSteppedOn equals the boundary date.

**The skeptic's evidence.** I could not refute the facts. They all check out. The fixture's shape still cannot hide a defect, so this stays a note. One clarification: the boundary's own fall path is exercised by no case at all, not "only on this cut fixture".

**What is confirmed:**

1. **(l) reuses (j)'s cut.**
   - PresidentialVetoDiagnostic.cs:360 tries `-2f` first. The log shows the result at n773b.log:934: "IncomeTax 12.36 -> 10.36".
   - Line 443 (`decision.TaxRateOverrides[moved.Type] = newRate;`) and line 466 reuse that cut.
   - n773b.log:940 records it as "Tax act: income tax 12.36 % to 10.36 %".
   - The comment at 437-438 says "where it raises the household rates".

2. **`Holder = "KO"` (lines 444 and 467) is the head's own party.**
   - (l) runs in (j)'s default-epoch world (lines 347-354).
   - That world's government of record is "Donald Tusk (KO)", with no end date (WorldClock.cs:250).
   - FinancePartner.cs:124 returns null when Finance is held by the head's party, so `Holder()` can never return "KO" here.

3. **The partner's act can only raise a rate.**
   - Expand writes no tax (FinancePartner.cs:222-238).
   - Tighten touches only IncomeTax and VAT, sets `to = Mathf.Min(line.MaxRate, line.Rate + points)`, and skips a line when `to <= line.Rate` (lines 271-279).
   - PartnerTaxAct copies only `written.Taxes` into its act (SimulationManager.cs:2702).

4. **(m)'s act stood** (n773b.log:831 and :946), so the fall arms of `ratesRight` and `countRight` (lines 595-596) never ran.

5. **The first pass's fixture fix was not applied.** It asked for a rise with a Holder that `Holder()` can return. The response marks finding 9 "taken" but says "the fall path is (l)'s".

**Why it is only a note: no code on the path reads the move's direction or the holder.**

- **TabledBy is never read.**
  - `TabledBy = written.Holder` (line 2701) is copied by TaxActPart (BudgetBill.cs:106-110).
  - GetBudgetBillConcern ends with `Authored(country, concern, bill.GovernmentBill)` (ParliamentSystem.cs:941), which sets `Author = null` for a government bill (line 890).
  - The only other place TabledBy is read is the alternative-budget path (SimulationManager.cs:1195-1256).
- **The vote's mechanics ignore direction.**
  - ChangesTaxRates compares with `Math.Abs` (BudgetBill.cs:84-89).
  - TaxActTitle (line 1378) and PresidentialVetoGate (lines 1423-1441) do not depend on direction.
- **The fall branch reads only the Written's fields** (lines 2706-2714).
  - The fixture's StancePoints/RatePoints are -0.25/-0.10 and -0.25/-0.25. These have the same signs Apply gives a tightening (FinancePartner.cs:199-200).
- **Holder is read only by Record, through Credit.**
  - The first (l) case never calls Record.
  - In (l)-R, Record returns early on `!written.Any` (FinancePartner.cs:56).
- So a fixture shaped like a real partner's move would run the same code in PartnerTaxAct.

**The real gap is elsewhere: no case takes a fall through AdvanceTurn.**

- Suppose a regression moved the PartnerTaxAct call (lines 4781-4784) after ApplyDomesticPolicy (line 4790). A fallen act's rates would then land.
- (l) would stay green, because it calls the method by reflection and runs no turn.
- (m) would stay green, because its act stood and `ratesRight` only reads `incomeAfter > incomeBefore`.
- The lines' part is also never credited by Record after a fall. The first (l) case asserts `StancePoints == -0.15` but does not call Record.
- (m)'s fall arm checks only `FinancePartnerSteppedOn != DateTime.MinValue`, not the boundary date.

**The skeptic's corrected fix.** Reshaping (l) only makes it more realistic. The coverage gap is closed by a fall through the boundary itself.

1. **Add (m2).** Run (m)'s planted world again (NL heads, KO holds Finance, every line pinned), with seats under which the partner's IncomeTax+VAT rise falls, either in the Sejm or by a veto that stands.
   - (j)'s search may not supply such seats. It stopped at the -2 cut, so whether that chamber vetoes a rise is not measured. Search the seats over the tightening's own rise instead.
2. **Assert after the turn:**
   - a "Tax act: " division that did not stand;
   - income tax and VAT unchanged;
   - `FinanceStanceApplied == 0`, with FinanceStanceHolder unchanged;
   - `FinancePartnerSteppedOn` equal to the boundary date;
   - the decision's TaxRateOverrides empty.
3. **Tighten (m)'s own fall arm** at line 596 the same way: compare against the boundary date, not `!= DateTime.MinValue`.
4. **For (l), pick one:**
   - build the Written with `FinancePartner.Apply` on (m)'s planted world, which gives a household-rate rise and a Holder that `Holder()` returns; or
   - reword lines 437-438 to say the fixture borrows (j)'s cut and head-party holder on purpose, because neither the direction nor TabledBy is read on PartnerTaxAct's path.

### 4. (k3) never asserts the veto, so a benefits act that can no longer be vetoed still passes

- **Lens:** tests - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PresidentialVetoDiagnostic.cs:513

**The scenario.** The search sets `levelAsked = asked` for every candidate (513) and breaks only on one that passes and is vetoed. When none is vetoed it falls through with the last candidate (+5).

The Check (529-530) then accepts any outcome - Sejm rejection, veto, or the act standing - as long as the level matches it. Nothing asserts that the search found a vetoed step, the three-division shape, `Motion`, or `Required > 0`.

Concrete case: let `BudgetBill.PartOf(StatutePart.Benefits)` lose its WelfarePrograms copy.
- The benefits act's concern is empty, so `WouldBillPass` returns true.
- `RecordDivision` writes no sides (ParliamentSystem.cs:1119-1129), so `PresidentialVeto.Decide` finds no PiS members and never vetoes.
- The search ends on 2.1 -> 7.1, the act stands and the level moves.
- (k3) prints "it stood; the level moved" and passes, while every Polish benefits act has become unvetoable.

No other case would catch this: (k), (k2) and the title check never call PartOf(Benefits).

The author's response says (k3) shows "it is vetoed, and the level stays". That holds only in today's run (n773b.log:943); the test does not assert it. The first pass's corrected fix asked for exactly this: three divisions, GenerosityLevel unchanged.

**The fix proposed.** Follow (k)'s pattern:
- Keep a `found` flag, set only on a candidate that passes and is vetoed, and Check it with its own label.
- Assert added.Count == 3: "Annual budget bill" passed; "Benefits act: " Passed && Motion; "Vetoed by the President (Karol Nawrocki): Benefits act: " !Passed && Required > 0.
- Assert GenerosityLevel == levelBefore.
- Drop the outcome-generic branch, or keep it only behind a failing "no vetoed step found" check.

**The skeptic's evidence.** Every step of the finding's failing path holds in the code. No other check closes the gap.

**1. The search never proves it found a vetoed step.** In PresidentialVetoDiagnostic.cs:
- Line 513 runs `levelAsked = asked;` for every candidate that changes the level.
- Line 514 breaks only when `WouldBillPass(...) && projected != null && projected.Vetoed && !projected.Overridden`.
- No Check covers "found". (k) has one: `Check(newAge >= 0f, ...)`.
- The else at 534 fires only when no candidate changes the level, and its label reads "no implemented welfare program".

**2. The verdict accepts any outcome.**
- 528: `stood = benefitsAct != null && benefitsAct.Passed && !vetoStood`.
- 529: `levelRight = stood ? |level - levelAsked| < 1e-4 : |level - levelBefore| < 1e-4`.
- 530 checks only `introduced && benefitsAct != null && IsStatuteActTitle(benefitsAct.Title) && levelRight`.
- Nothing asserts the division count, Motion, Required > 0, or the veto title.

**3. The regression path (`PartOf(Benefits)` loses its WelfarePrograms copy)**, traced:
- **Search:** the probe's concern has no moves, so `IsEmpty` holds (StanceModel.cs:63). StancesOver gives every non-author party side 0 when `loaded.Count == 0` (312-314). PiS's clubNo is 0, so `Vetoes` is false (PresidentialVeto.cs:44). No candidate breaks, so the search ends on 2.1 + 5 = 7.1, as the finding says.
- **The vote:** PolishStatuteActs builds `act = bill.PartOf(part)` (SimulationManager.cs:1296). Its concern is empty, so `WouldBillPass` returns true (ParliamentSystem.cs:334). RecordDivision's `contested` is false and it writes no sides (1119-1129).
- **The veto gate:** Decide counts clubMembers 0, so Vetoed is false and the gate returns true (1426-1427).
- **Apply:** `applied` is still the full bill (1292). ApplyBillResult sets `program.GenerosityLevel = Mathf.Clamp(kvp.Value, 0f, 100f)` from it (ParliamentSystem.cs:499).
- **Result:** the title is "Benefits act: the standing levels" (1352), which still passes StartsWith and IsStatuteActTitle. `stood` and `levelRight` are true, so (k3) prints "it stood; the level moved" and passes.

**4. Nothing else catches it.** In Assets/, `PartOf(` is reached only by SimulationManager:1296, the two UI pending cards, and the probes at diag 408 (PensionAge) and 509 (Benefits). (k2) uses BudgetActOf, which goes through SpendingOnly and Without and never calls PartOf. The title check uses ActWords. (l) and (m) vote Rates only. No other Editor or Testing code drives a Polish welfare move: BudgetDraftEstimateDiagnostic:148 keeps the standing level, and SimulationTestRunner:427 is the USA stress bill.

**5. The finding quotes the review file correctly.**
- The first pass's finding 3, corrected fix item 3 (line 116): "Expect three divisions (budget act, "Benefits act: ...", veto) and the program's GenerosityLevel unchanged."
- The author's response (line 733) claims "it is vetoed, and the level stays".
- That is true only in today's run (n773b.log:943: "it fell; the level stays (2.1 -> 2.1) ... [motion] Benefits act: means tested welfare 2.1 to 0 | Vetoed by the President (Karol Nawrocki) ..."). The code does not assert it.

**Severity: minor.** Today's game behaviour is correct, and the generic gate is still tested by (j) and (k). But the fix the author marked as taken does not pin the benefits act's veto, so a benefits-specific regression that makes the act unvetoable stays green.

**The skeptic's corrected fix.** In (k3), follow (k)'s pattern at 397-420.

1. **Mark the search's success.** Declare `bool found = false;`. Set `levelAsked = asked; found = true;` only inside the break branch (514). Then `Check(found, F("§773: on the planted chamber a benefits step the Sejm passes and PiS opposes - {0:0.#} -> {1}", levelBefore, found ? levelAsked.ToString("0.#") : "none found"))`. Keep the else at 534 for `program == null` only.

2. **Run the game path only under `if (found)`**, and replace the outcome-generic verdict:
```csharp
bool shape = added.Count == 3
  && added[0].Title == "Annual budget bill" && added[0].Passed
  && added[1].Title.StartsWith("Benefits act: ", StringComparison.Ordinal) && added[1].Passed && added[1].Motion
  && added[2].Title.StartsWith("Vetoed by the President (Karol Nawrocki): Benefits act: ", StringComparison.Ordinal) && !added[2].Passed && added[2].Required > 0;
Check(introduced && shape && IsStatuteActTitle(added[1].Title)
  && Math.Abs(program.GenerosityLevel - levelBefore) < 1e-4f && spendLine.Amount > spendBefore, ...)
```
   Capture `spendLine` and `spendBefore` before the bill is introduced, as (k) does.

3. **Drop the `stood ? ... : ...` branch.**

Optionally, also assert that `added[1].Title` names the asked level (e.g. contains `" to " + levelAsked.ToString("0.#", inv)`). That makes a `PartOf(Benefits)` that drops its levels, and so reads "Benefits act: the standing levels", fail on the title as well as on the veto.

Today's run (n773b.log:943) already has exactly this shape: 2.1 to 0, three divisions, the veto stands. So the stronger check passes now and fails on the regression.

### 5. (m) only ever reaches its 'stood' branch; the boundary's order on a fall (PartnerTaxAct before Record) is tested nowhere

- **Lens:** tests - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PresidentialVetoDiagnostic.cs:596

**The scenario.** On the planted 2024 chamber the partner's rise stands (n773b.log:831 "stands - it applies", :946 "a tax act that stood"). So (m)'s fall-branch expectations are never evaluated: ratesRight's equality and countRight's `FinanceStanceApplied == 0f && FinancePartnerSteppedOn != MinValue`.

(l) does run the fall, but it calls PartnerTaxAct and then `FinancePartner.Record` itself (472-473), in the test's own order, not AdvanceTurn's.

Concrete case: move `FinancePartner.Record(country, partnerWrote, ComingBoundaryDate)` (SimulationManager.cs:4785) above the PartnerTaxAct block (4781-4784).
- On a fallen tax act, Record runs while Taxes is still set, so the boundary credits the whole step (StancePoints -0.25) although nothing landed.
- (l) stays green because it uses its own order.
- (m) stays green because where the act stands, Record before or after gives the same result.
- (m)'s fall branch would catch it (Applied -0.25 != 0), but no case reaches it.

Also never executed: the boundary's approval audit (`ApprovalLedgerRecorder.CloseAtBoundary`; the armed log fold turns its LogError into a failure) with a fallen act's BillFailedApprovalCost inside the window.

The response says "the fall path is (l)'s". (l) covers PartnerTaxAct's arithmetic, not the boundary's wiring.

**The fix proposed.** Run the boundary a second time where the rise falls, as the first pass's corrected fix asked.
- Plant the Sejm's seats before the day loop so the tax act is rejected. `UpdateSeats` runs only after ApplyDomesticPolicy (SimulationManager.cs:4800), so the plant holds at the vote.
- Assert the fall branch: income tax and VAT unchanged, FinanceStanceApplied 0, FinancePartnerSteppedOn == the boundary date.
- At minimum, say in (m)'s comment and label that its chamber reaches only the stood branch.

**The skeptic's evidence.** Every fact in the finding holds. The shipped code is correct, so this is a test gap, not a behaviour bug.

1. (m) only ever takes its "stood" branch.
   - n773b.log:831 reads "BUDGET: Poland - Tax act: income tax 12.37 % to 12.74 %, VAT 23.0 % to 23.37 %: stands - it applies". The stack runs PartnerTaxAct then AdvanceTurn (SimulationManager.cs:4783).
   - n773b.log:946 reads "...became a tax act that stood; ... the stance counted -0.25 pp".
   - The result is fixed, not chance. WouldBillPass (ParliamentSystem.cs:332-343) reads only seats and stances. Seats change only through SetSeatsFromElection (86-95); UpdateSeats is a no-op once seated (73-79). No Polish Sejm election falls between 2024-01-15 and 2025-01-14.
   - So the fall operands at PresidentialVetoDiagnostic.cs:595-596 never run: `Math.Abs(incomeAfter - incomeBefore) < 1e-6f` and `pl.FinanceStanceApplied == 0f && pl.FinancePartnerSteppedOn != DateTime.MinValue`.

2. The proposed swap passes every check.
   - Move `FinancePartner.Record(country, partnerWrote, ComingBoundaryDate)` (SimulationManager.cs:4785) above the PartnerTaxAct block (4781-4784).
   - Record (FinancePartner.cs:54-59) then runs while Taxes is still set. Any is true, so it credits AppliedBefore + StancePoints (the full -0.25).
   - On a fall, PartnerTaxAct then zeroes StancePoints (2699+), but the credit already stands.
   - (l) stays green: it calls PartnerTaxAct and then Record itself, in its own order (PresidentialVetoDiagnostic.cs:472-473). Its first sub-case (451) never calls Record at all.
   - (m) stays green: when the act stands, PartnerTaxAct returns early and changes neither `written` nor `decision`, so the order gives the same result.
   - (m)'s fall branch would catch the swap (-0.25 != 0f), but nothing reaches it.

3. A second swap also goes unseen. Move PartnerTaxAct below ApplyDomesticPolicy (4790) but above Withdraw (4794).
   - When the act stands, the rates still apply and the act is still recorded from decision.TaxRateOverrides, so (m) passes.
   - Only a fall would show that the rates had already landed.

4. No other check reaches the boundary through PartnerTaxAct.
   - FinancePartnerDiagnostic seats Germany only (99, 143, 170). PresidentialVeto.Applies is Poland-only (PresidentialVeto.cs:37).
   - SpendingIndexationDiagnostic sets AiFinanceMinistryEnabled = false (45), so FinancePartnerRuns is false (SimulationManager.cs:2679-2680).
   - LeverLivenessCheck seats no player.
   - A grep for PartnerTaxAct and FinancePartner.Record finds only SimulationManager.cs:2699/4783/4785, PresidentialVetoDiagnostic.cs:441/473, and the Germany diagnostic.

5. The fix the first pass asked for is only partly built.
   - Finding 9's corrected fix asked for exactly this case: "Make the rise fall ... Assert on the fall ... FinancePartnerSteppedOn equal to the boundary date; FinanceStanceApplied unchanged ... Add the opposite case, where the act stands."
   - The author built only the opposite case, and wrote "On this chamber it stood; the fall path is (l)'s."
   - (m)'s comment (561-563) says "the turn must apply them only where it stood", which reads as if both branches are checked.

6. One sub-claim needs a qualifier. The approval audit with a fallen act's cost is indeed never run, but by reading it would balance.
   - PolishStatuteActs records the cost as an event (SimulationManager.cs:1306-1309).
   - That happens before `approvalBeforeFormula` is taken (5025).
   - ClampLoss is measured from that value (MacroSystem.cs:2398), so CloseAtBoundary (ApprovalLedgerRecorder.cs:77-95) sums events + terms + clamp equal to the observed change.
   - So this is coverage only, not a hidden failure.

Why minor rather than a defect, or a lower grade:
- The current order (PartnerTaxAct, then Record, then ApplyDomesticPolicy, then Withdraw) is correct.
- The first pass's skeptics found that no live game state reaches PartnerTaxAct today: no UI pins a line, so the partner never writes Taxes.
- What is missing is a guard against a two-line reorder that would credit a fallen act's step in full, and that exact case was asked for and not built.

**The skeptic's corrected fix.** Make (m) cover the fall as well as the stand. Parameterise it, or run the boundary twice on the same planted world.

1. Make the rise fall.
   - Plant pl.ParliamentSeats before the day loop so the Sejm rejects an income-tax and VAT rise.
   - The plant holds until the vote: seats change only at an election (ParliamentSystem.SetSeatsFromElection; UpdateSeats is a no-op once seated), and no Sejm election falls in the epoch's first year.
   - A veto route needs PiS AGAINST under Duda in 2024. A rise may not get that, so a rejection in the Sejm is the reliable route.
   - Do not try an expansion instead: with every line pinned, Expand writes nothing.

2. Assert the fall at the boundary:
   - a "Tax act: " division that did not pass, or a standing "Vetoed by the President ... Tax act: " division;
   - income tax and VAT exactly unchanged;
   - decisions[Poland].TaxRateOverrides empty;
   - pl.FinanceStanceApplied == 0f;
   - pl.FinancePartnerSteppedOn equal to the boundary date (EpochDate + DaysPerTurn), not just != MinValue;
   - a "Tax act failed" event in pl.ApprovalLedgerLastPeriod.Events, and no ATTRIB error at the close (the armed log fold catches one).

3. Effect: the Applied == 0f assert catches Record being moved above PartnerTaxAct, and the unchanged-rates assert catches PartnerTaxAct being moved below ApplyDomesticPolicy.

4. If the fall case is not built now, at minimum:
   - Reword (m)'s comment and its Check label to say that on its chamber the act stands, so only the stood branch is exercised.
   - Say there that the boundary's order on a fall (PartnerTaxAct before Record and before ApplyDomesticPolicy) is guarded by no check.
   - Delete the dead fall branch or mark it as such.

### 6. (l)'s fixture is unlabelled and contradicts its comment: a rate cut by the head's own party, not a partner's rise

- **Lens:** tests - **reviewer:** minor - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PresidentialVetoDiagnostic.cs:444

**The scenario.** The comment (437-438) says the partner's step goes to the Sejm "where it raises the household rates". The fixture contradicts it in three ways, none marked PLANTED (unlike (m)):
- It reuses (j)'s searched move, an income-tax CUT 12.36 -> 10.36 (n773b.log:934, 940). Tighten only ever raises IncomeTax or VAT (FinancePartner.cs:271-279).
- It sets Holder = "KO". In this world (default epoch, Tusk's KO cabinet with KO holding Finance) that is the head's own party, and `FinancePartner.Holder` returns null for it (FinancePartner.cs:124).
- Its StancePoints and RatePoints are a tightening's, whose rate part would be a rise.

The first pass (finding 9, corrected fix 2) asked for an IncomeTax or VAT rise with a holder Holder() can return. The response does not say this was declined.

Result: the only test of the partner's act falling uses a move the partner can never make. The log line "the partner's boundary rates as a tax act - voted, vetoed ... Tax act: income tax 12.36 % to 10.36 %" reads as if the partner's act is vetoed on this chamber. (m) shows the partner-shaped rise stands.

**The fix proposed.** Either label the fixture, e.g. "PLANTED: a cut and a holder no partner could be - the move the planted chamber vetoes; PartnerTaxAct reads neither the direction nor the tax type". Or search IncomeTax/VAT rises for one that falls on a planted chamber, with a holder Holder() returns.

**The skeptic's evidence.** The finding's facts hold, but its consequence is weaker than claimed: the check of the mechanism is sound, and only the label and the record are wrong.

WHAT HOLDS
- The fixture is a cut. PresidentialVetoDiagnostic.cs:443 sets `decision.TaxRateOverrides[moved.Type] = newRate;` and :444 sets `new FinancePartner.Written { Holder = "KO", StancePoints = -0.25f, RatePoints = -0.10f, RateAmount = 1f }`. The rates-only case at :466-467 does the same with RatePoints -0.25f. `moved`/`newRate` come from (j)'s search (:356-370), which tries `-2f` first and breaks on the first vetoed move. The log shows a cut: n773b.log:934 "IncomeTax 12.36 -> 10.36" and :940 "recorded: [motion] Tax act: income tax 12.36 % to 10.36 % | Vetoed by the President ...".
- No partner ever writes a cut. In FinancePartner.cs, Expand (:222-238) writes spending lines only. Tighten (:271-279) touches only IsHouseholdRate lines, with `float to = Mathf.Min(line.MaxRate, line.Rate + points); if (to <= line.Rate) { continue; }`, so it can only raise.
- "KO" is not a partner in this world. FinancePartner.cs:124 reads `return held.Key == government.PmParty || SameGroup(...) ? null : held.Key;`. (j)'s world is the default epoch: the head is "Donald Tusk (KO)" (WorldClock.cs:250, via GovernmentRecord.cs:263 HeadParty), and Finance sits with KO (PortfolioSalienceDiagnostic.cs:111). Holder() therefore never returns "KO".
- Nothing marks the fixture as planted:
  - the comment at :437-438 says "The Finance partner's boundary step, where it raises the household rates, goes to the Sejm as a tax act its party tables";
  - the message at :460 says "the partner's boundary rates as a tax act - voted, vetoed";
  - the class summary at :40-41 describes (l) the same way;
  - (m), by contrast, is labelled PLANTED at :561 and :598.
- The first pass's fix was neither done nor declined. Its corrected fix 2 (review file :386) reads "At minimum use IncomeTax or VAT raised above its current rate, with a Holder that Holder() can return". The response (:725-727) says "3 and 9 - taken ... On this chamber it stood; the fall path is (l)'s". It says nothing about fix 2.

WHY THIS IS ONLY A NOTE
- PartnerTaxAct (SimulationManager.cs:2699-2715) never reads the fields the fixture gets wrong. It reads written.Taxes, decision.TaxRateOverrides, Lines, StancePoints and RatePoints. It uses Holder only as `TabledBy = written.Holder`.
- TabledBy is read only by Sweden's alternative-budget contest (SimulationManager.cs:1195-1256). The statute-act vote path never reads it: not GetBudgetBillConcern, WouldBillPass, RecordDivision or PresidentialVetoGate.
- In the rates-only case, Record returns at `if (written == null || !written.Any) { return; }` (FinancePartner.cs:56), so "KO" never reaches the country's state.
- Every assertion in (l) would therefore hold the same for a partner-shaped rise. The fall path is proved; it is just mislabelled. (m) covers the partner-shaped move through Apply and the real boundary (n773b.log:831, :850, :946).
- One overstatement in the finding: "(m) shows the partner-shaped rise stands" is true of a different chamber. (m) runs on the record's 2024 seats with Duda as president, not (l)'s planted chamber. No rise was searched on (l)'s chamber, so whether one would fall there is unknown, not shown.

**The skeptic's corrected fix.** Label both of (l)'s fixtures as planted, in the comment and in both check messages. Use a pointer, not figures, per the claim convention.

Suggested comment: "PLANTED: the Written is hand-built - (j)'s searched move, in whatever direction the search found it, tabled by the head's own party. No partner writes either: Tighten only raises the household rates, and Holder() never names the head's party. PartnerTaxAct reads neither the direction, the tax nor the holder (TabledBy is not read by the vote), so the fall proved here is the partner's."

Add a matching suffix to the messages at :460 and :476: "(PLANTED: (j)'s searched move, a holder no partner could be)".

Add one line to the review's response saying that the first pass's corrected fix 2 was declined, and why: (m) carries the partner-shaped move, and it stood.

Optional, stronger: on (j)'s planted chamber, search IncomeTax and VAT rises for one the Sejm passes and PiS opposes. Plant Finance on a cabinet party other than the head's (then restore it before (k2)-(k4)), and build (l) from FinancePartner.Apply. If no rise falls there, say so in the comment: stopping is a result.

### 7. (k4)'s label says 'uncontested' but nothing asserts it; the fund-rules title from the finding-6 fix has no case

- **Lens:** tests - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PresidentialVetoDiagnostic.cs:550

**The scenario.** The Check tests passed, signed (no veto title) and created, but not uncontested. Uncontested is visible in the record: `RecordDivision` writes no sides exactly when the concern is empty (ParliamentSystem.cs:1119-1129).

If the fund's terms get a concern (the question put to Elias), a fund creation the Sejm passes without PiS opposing it would still pass (k4), and the label would still print "uncontested".

(k4) also covers only the "created" title. The rules branch of StatuteActTitle (every rule moved, from and to - the finding-6 fix) is run by no case. By reading it is right: same 1e-4 tolerance as ChangesFund, values read from PartOf(Fund).

**The fix proposed.** Add `fundAct.Sides.Count == 0` to the Check. Optionally add a rules-only change on a planted fund (restored afterwards) and assert the title names the moved rule.

**The skeptic's evidence.** I could not refute either claim. Both are accurate. Nothing fails today: the premise holds and the title branch looks correct when read.

**1. Nothing in (k4) asserts "uncontested".** The check at PresidentialVetoDiagnostic.cs:550-552 is:
`introduced && fundAct != null && fundAct.Passed && fundAct.Title == "Fund act: the sovereign wealth fund created" && IsStatuteActTitle(fundAct.Title) && !added.Any(d => d.Title.StartsWith("Vetoed by the President"...)) && created`
- No term reads the concern or `fundAct.Sides`. A grep of the diagnostic for `IsEmpty` or `Sides` finds nothing.
- The label "uncontested, passed and signed" prints "uncontested" whatever happened.
- Every other new case's label states only what its check asserts. (k4) is the one exception.

**Why the label is true today, and how it could go stale.**
- `GetBudgetBillConcern` (ParliamentSystem.cs:911-942) reads no `Swf*` field, and `PartOf(Fund)` carries only `Swf*` fields. So the fund act's concern is empty.
- An empty concern makes `WouldBillPass` return true (332-334), and `RecordDivision` writes no sides (`contested = concern != null && !concern.IsEmpty`, 1119-1129).
- With no sides, `Decide` (PresidentialVeto.cs:74-88) counts no PiS member against (`clubNo = 0`), so there is no veto.
- The two things (k4) does check (passed, no veto record) would hold just as well for a contested fund act that KO (the author, 262 planted seats) carries and PiS does not oppose.
- So if the fund's terms get a concern (the question put to Elias), (k4) stays green and still prints "uncontested". The declared "UNCONTESTED BY CONSTRUCTION" comments (`BudgetBill` §773, the `PolishStatuteActs` summary, the class summary) would go stale and no check would fail.
- The author's response also describes (k4) as asserting "uncontested", which overstates the check.

**2. No case runs the fund-rules title branch** (SimulationManager.cs:1357-1368). Every case misses it:
- (k4) has no fund, so it takes the "created" branch.
- (l) plants a fund (diagnostic:449), but `PartnerTaxAct` votes only the rates (`new[] { StatutePart.Rates }`, 2704).
- (k2) calls only `BudgetActOf` and `Changes`; no title is built.
- (j), (k) and (k3) build bills new (`SwfShouldExist` false) on a fundless Poland, so `ChangesFund` is false.
- Poland never starts with a fund: `WorldFactory` seeds funds only for Sweden (879) and France (889), and ScenarioLibrary:122 is Sweden.
- No other Editor diagnostic introduces a Polish budget with a fund. The branch is reached only in play, when a Poland that already created a fund changes its rules through `BuildBudgetBillFromDrafts`.

**Reading the branch, it is correct.** It uses the same 1e-4 tolerance as `ChangesFund`. "From" is the fund as it stands before the apply; "to" is the six fields `PartOf` copies. One small issue: it prints the figure as asked, unclamped, while `ApplySwfPolicyChanges` (5909-5937) clamps each rule. That only matters for an out-of-range request.

**The skeptic's corrected fix.** 1. **Make (k4) assert "uncontested".** In (k4)'s check, right after `fundAct != null`, add `fundAct.Sides.Count == 0`. An equivalent alternative is to assert `ParliamentSystem.GetBudgetBillConcern(pl, bill.PartOf(BudgetBill.StatutePart.Fund)).IsEmpty` before introducing the bill. Either way, the declared premise trips (k4) the moment the fund's terms get a concern, instead of the label silently printing "uncontested".

2. **Optionally, add a case for the rules branch:**
   - Plant a fund with known rules on `pl`.
   - Introduce a bill with `SwfShouldExist = true` that names all six rules at the planted values except one (for example, contribution +1), plus a spending line.
   - Step it through `AdvanceBudgetBillDay`.
   - Assert that the fund act's title equals `ActWords(Fund) + ": the sovereign wealth fund's rules - its contribution " + from + " % to " + to + " %"`, built from the planted values with invariant "0.0#" formatting. It should name that rule alone.
   - Assert that the act passed with no veto and that the new rule landed.
   - Restore `pl.SovereignWealthFund` afterwards.

### 8. The fund act's title prints the four raw asset-class weights as percentages

- **Lens:** ui-text - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Simulation/SimulationManager.cs:1361

**The scenario.** The weights are raw and "NOT required to sum to 100": they are normalised by their live sum (SovereignWealthFund.cs:13-15, GetNormalizedWeight). Scenario: Poland holds a fund at the default 40/30/15/15, created by an earlier budget's Fund act. The player drafts Equities 40 -> 60.
- The Fund tab shows that row as a raw weight with no unit ("RAW WEIGHTS, NORMALISED TO 100"; format F0, empty suffix; GameController.cs:10399, 10430-10431). Its trailing column reads "50% of fund" (the draft's sum is 120).
- The Fund act passes, and its title reads "Fund act: the sovereign wealth fund's rules - its equities weight 40.0 % to 60.0 %". The title appears on the Canvas signing plate at 26 pt (SigningScreen.cs:284), in Division Records (GameController.cs:9242) and on the Docket (GameController.cs:3559).
- In fact equities go from 40 % to 50 % of the fund. Bonds go from 30 % to 25 %, infrastructure and real estate from 15 % to 12.5 %; the title names none of these.
- Precision also differs: a weight snapped to tenths prints "57.8 %" where the tab shows "58".

**The fix proposed.** Either print the weights as the tab does (raw, F0, no unit: "its equities weight 40 to 60"), or print shares. For shares, apply GetNormalizedWeight to the standing fund and to a throwaway fund carrying the act's four weights, and when any weight moves, list every class whose share moved ("equities 40 % to 50 % of the fund, bonds 30 % to 25 %, ..."). Optionally qualify the contribution as "% of GDP", the unit of SovereignWealthFund.ContributionRatePercent.

**The skeptic's evidence.** The finding holds. I traced the path in the working tree. This line is new in the unstaged E2 diff: it is the fix for first-pass findings 6/10, which asked the title to list every rule the act moves.

1. **The title adds "%" to raw weights.** SimulationManager.cs:1361:
   `void Rule(string name, float from, float to) { if (System.Math.Abs(to - from) > 1e-4f) { rules.Add(name + " " + from.ToString("0.0#", inv) + " % to " + to.ToString("0.0#", inv) + " %"); } }`
   Lines 1364-1367 pass the four asset-class weights through it.

2. **The weights are raw, not percentages.**
   - SovereignWealthFund.cs:13-15: the weights are "NOT required to sum to 100 - GetNormalizedWeight divides each by their live sum".
   - GetNormalizedWeight returns `EquitiesWeight / Math.Max(1f, sum)`.
   - ApplySwfPolicyChanges (SimulationManager.cs:5919-5937) clamps each weight to 0..100 on its own.

3. **The Fund tab shows them without a unit.**
   - GameController.cs:10399: "ASSET CLASS MIX · RAW WEIGHTS, NORMALISED TO 100 · THE FIGURE AT THE RIGHT IS EACH CLASS'S SHARE OF THE FUND".
   - 10427-10431: `SwfRow("Equities", ..., "F0", string.Empty, Share(...))`, where Share = `GetNormalizedWeight*100 "F0" + "% of fund"`.
   - The comment at 10401-10413 explains why: dragging one raw weight changes what the other three amount to.

4. **The scenario is reachable through the normal game path.**
   - The Fund tab has no country gate (BudgetV35.cs:32-34, 819).
   - The player's bill carries all four drafted weights (GameController.cs:9830-9836).
   - BudgetBill.ChangesFund fires on any weight that moves, so PolishStatuteActs votes a Fund act. GetBudgetBillConcern never reads the fund's fields, so the act passes uncontested.
   - RecordDivision stores the title. QueueNewlyResolvedDivisions (GameController.cs:2281-2296) queues every division that is not a motion for a signing plate.
   - The title is printed at SigningScreen.cs:284 (26 pt), in Division Records at GameController.cs:9242, and on the Docket at 3559.

5. **Worked case.** The standing fund is 40/30/15/15 (sum 100) and the player drafts equities 60 (sum 120).
   - The plate reads "Fund act: the sovereign wealth fund's rules - its equities weight 40.0 % to 60.0 %".
   - For the same draft the tab's trailing column reads "50% of fund".
   - The real shares are equities 40→50 %, bonds 30→25 %, infrastructure and real estate 15→12.5 %. The title names none of the shares.
   - Any single move away from a sum of 100 makes the printed "%" figure differ from the class's share.

6. **Precision.** The slider snaps to 0.1, 0.5 or 1 depending on track width (LedgerRow.StepFor). The title formats with "0.0#" and the tab with "F0", so a snapped 57.5 or 57.8 reads differently on the two surfaces. This is a cosmetic side point.

**What does not refute it:**
- PolicyWebRenderer.cs:1286 also prints raw weights with "%". That is older code outside this diff, and the Fund tab, where the player sets the weights, deliberately drops the unit.
- The contribution's "%" is a real percent (of GDP a year) and matches the tab's suffix.
- The domestic allocation is a real percent, and the player's bill carries it unchanged (P5-B4).
- No diagnostic pins the weight wording: (k4) asserts only "the sovereign wealth fund created", and (k2) toggles the fund's existence. A fix breaks no assertion.

**Severity: minor.** This is wrong wording on a player-facing surface, and only on a narrow path (Poland, a standing fund, a weight move). It does not change the simulation.

**The skeptic's corrected fix.** In StatuteActTitle's default case (SimulationManager.cs:1360-1368), leave Rule() for the contribution and the domestic allocation; both are real percents. Optionally say the contribution as "% of GDP a year".

Replace the four weight Rule() calls. Print the raw weights the way the Fund tab does, with no unit. When any weight moves, also print every class whose share of the fund moved, using the same GetNormalizedWeight the tab uses:

```csharp
// the asset-class weights are RAW - normalised by their live sum (SovereignWealthFund.GetNormalizedWeight) - so they are said as the Fund tab says them,
// without a unit, and every class whose SHARE of the fund moves is said in shares
void Weight(string name, float from, float to) { if (System.Math.Abs(to - from) > 1e-4f) { rules.Add(name + " " + from.ToString("0.#", inv) + " to " + to.ToString("0.#", inv)); } }
Weight("its equities weight", fund.EquitiesWeight, act.SwfEquitiesWeight);
Weight("its bonds weight", fund.BondsWeight, act.SwfBondsWeight);
Weight("its infrastructure weight", fund.InfrastructureWeight, act.SwfInfrastructureWeight);
Weight("its real-estate weight", fund.RealEstateWeight, act.SwfRealEstateWeight);
var mix = new SovereignWealthFund { EquitiesWeight = act.SwfEquitiesWeight, BondsWeight = act.SwfBondsWeight, InfrastructureWeight = act.SwfInfrastructureWeight, RealEstateWeight = act.SwfRealEstateWeight };
var shares = new List<string>();
foreach (SovereignWealthAssetClass c in (SovereignWealthAssetClass[])System.Enum.GetValues(typeof(SovereignWealthAssetClass)))
{
    float from = fund.GetNormalizedWeight(c) * 100f, to = mix.GetNormalizedWeight(c) * 100f;
    if (System.Math.Abs(to - from) >= 0.05f) { shares.Add(Words(c.ToString()) + " " + from.ToString("0.#", inv) + " % to " + to.ToString("0.#", inv) + " %"); }
}
if (shares.Count > 0) { rules.Add("its shares of the fund " + string.Join(", ", shares)); }
```

The worked case then reads: "Fund act: the sovereign wealth fund's rules - its equities weight 40 to 60, its shares of the fund equities 40 % to 50 %, bonds 30 % to 25 %, infrastructure 15 % to 12.5 %, real estate 15 % to 12.5 %".

If that is too long for the 26 pt plate, the minimum fix is to drop the " %" from the weight lines and use "0.#", so the title matches the tab's unitless raw weights.

Optionally pin the wording with a (k4b) case in PresidentialVetoDiagnostic that plants a 40/30/15/15 fund, moves equities, and asserts the title carries no "% to" on a weight.

### 9. "never an outcome" / "changes no outcome" overstates the fund act's effect: voted last, it can hide a same-day failed act from CabinetSystem.UnderPressure

- **Lens:** ui-text - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Data/BudgetBill.cs:127

**The scenario.** The same claim appears at SimulationManager.cs:1283. Scenario: a Polish budget moves a rate PiS opposes and also the fund's contribution, and it resolves on a turn-boundary day. That can happen when a window opens on a government's arrival or a fiscal-year date and BillDurationDays later falls on the boundary; GameController.Update runs AdvanceCountryDayTick before AdvanceTurn on that day (GameController.cs:783-791).
- PolishStatuteActs appends, in order: Annual budget bill; the Tax act (marked a motion by the gate); "Vetoed by the President ... the veto stands" (!Passed, !Motion, dated today); and last the Fund act (Passed), because StatuteParts orders Fund last.
- AdvanceTurn's TryRollCabinetEvents (SimulationManager.cs:5066) calls UnderPressure (CabinetSystem.cs:476), which reads only the newest entry. That entry is the passed Fund act, so UnderPressure returns false (approval at or above 40) and no resignation or leak is rolled.
- Before §773 the fund rode in the budget act, the veto vote was the newest entry, and the government was under pressure.
- The first pass's skeptic recorded exactly this qualification (finding 5, point 5). The reading that now goes to Elias says the opposite.

**The fix proposed.** Reword to what is true, e.g. "the fund's own outcome is unchanged (it applies exactly when the budget act passes); the act adds a division and a signing, and, voted last, it is the day's newest division". Alternatively, vote the Fund act first in StatuteParts, so it is never newer than a fallen act. Either way, say so where the question goes to Elias.

**The skeptic's evidence.** I tried to refute this and could not. Every link holds in the working tree.

1. The Fund act is always voted last, and it always passes.
- BudgetBill.cs:133 orders StatuteParts as { Rates, PensionAge, Benefits, Fund }, and PolishStatuteActs loops in that order (SimulationManager.cs:1293).
- The Fund act's concern is empty. GetBudgetBillConcern (ParliamentSystem.cs:911-942) has no Swf term, so WouldBillPass returns true (:334).
- RecordDivision then appends Passed=true with no sides (:1119-1130).
- PresidentialVeto.Decide sees clubMembers=0, so Vetoes is false (PresidentialVeto.cs:44), and no veto record is written.

2. A failed act comes before it.
- A tax act the Sejm rejects is recorded as Passed=false, Motion=false.
- If the Sejm passes it and the President vetoes it, PresidentialVetoGate (SimulationManager.cs:1428-1437) sets passage.Motion=true. It then appends "Vetoed by the President ... the veto stands" with Passed=veto.Overridden=false. Motion keeps its default of false (DivisionRecord.cs:40, 110-120), and Date=CurrentDate.

3. Only the newest entry is read.
- CabinetSystem.cs:472-477 reads entries[Count-1] alone: `!Passed && !Motion && Date == today`.
- The caller is TryRollCabinetEvents(country, CurrentDate) at SimulationManager.cs:5066. It runs at the end of ApplyDomesticPolicy for every country, with no early return before it.
- Inside AdvanceTurn nothing else appends a division, except PartnerTaxAct. The first pass's skeptics showed that call cannot be reached in play.

4. The same day is reachable.
- GameController.cs:783-791 runs AdvanceCountryDayTick, then QueueNewlyResolvedDivisions, then AdvanceTurn, on one CurrentDate. AdvanceTurn never moves the date.
- AdvanceBudgetBillDay runs first in the day tick (SimulationManager.cs:299).
- The bill resolves BillDurationDays=21 days after it is introduced (IntroduceBudgetBill, SimulationManager.cs:1478). A turn is 365 days (:216, :482).
- Two ways to land the resolution on a boundary day:
  - HoldOnBudgetWindow is a player toggle (GameController.cs:1387, 1531). With it off, the window stays open until a bill is introduced, so the player picks the day.
  - An arrival window, reset when the government changes (:1204), can open 21 days before a boundary.
- BuildBudgetBillFromDrafts carries the drafted fund fields, so a player bill with a rate change and a fund change votes both acts. The AI's bill names the standing fund (AiFinanceMinistry.cs:119-125), so on that path there is no fund act.

5. Outcome.
- On a boundary day where a rate act fell and the bill also changes the fund, with approval at or above 40 (the 1.5 fall cost rarely crosses it), UnderPressure now returns false. No resignation or leak is rolled, and no cabinet RNG is drawn.
- Before §773 the fund rode in the budget act, whose division comes before the tax act. The veto or failure record was then the newest entry, so UnderPressure was true.

6. Where the claim stands.
- BudgetBill.cs:127 "never an outcome" and SimulationManager.cs:1283 "changes no outcome" are both false in this narrow case. The author's answer to findings 1 and 5 in the review (line 722) repeats the claim to Elias.
- The first pass's skeptic recorded exactly this qualification (review line 215, point 5).
- The premise behind the claim is pre-existing and stale. ParliamentSystem.cs:424 and DivisionRecord.cs:71-76 say nothing in Simulation reads the log back, but UnderPressure has read it since P2-5.2 (617fcf0).

Severity: note. The effect needs a boundary-day resolution plus a same-day failed or vetoed act. It changes only the cabinet-event roll. The defect is the wording of a declared reading that goes to Elias.

**The skeptic's corrected fix.** Correct the claim wherever it appears: BudgetBill.cs:122-127, the PolishStatuteActs summary (SimulationManager.cs:1282-1283), and the question to Elias in the §773 record and the review.

Suggested wording: "the fund's own result never differs from the budget act's: it applies exactly when the budget act passes. Its separation adds a division and a signing. Because it is voted last (BudgetBill.StatuteParts), it is also the day's newest division, and CabinetSystem.UnderPressure reads only that one. So on a turn-boundary day it hides a same-day failed or vetoed act from the pressure test."

If parity with the pre-§773 behaviour is wanted rather than a disclosure, put Fund first in StatuteParts. It then never sits above a contested act.
- This does not cover the pension and benefits acts. They were also separated by §773 and can pass after a fallen tax act and hide it, so say that too.
- Reordering changes the order of the votes and the pending cards, so re-measure n773b.

Optionally, outside this diff: the "write-only" docs at ParliamentSystem.cs:424 and DivisionRecord.cs:71-76 are stale since P2-5.2's UnderPressure. They are the premise the "never an outcome" wording leaned on, so name the reader there.

### 10. In Poland the 'Annual budget bill' card reads 'Unopposed - no change requested' when all of the bill's changes went to statute acts

- **Lens:** ui-text - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/UI/GameController.cs:9297

**The scenario.** Scenario: the player's Polish draft changes only the pension age, or only benefit levels.
- BudgetActOf calls SpendingOnly (ParliamentSystem.cs:900), which leaves no move, so the budget card's concern is empty.
- Its label has no entry in uncontestedWords, so line 9415 prints "Annual budget bill - resolves in 21 day(s). / Unopposed - no change requested". That sits right above e.g. "Pension act with the budget - voted if the budget passes ... Currently leans VETOED".
- The Budget page's slip words the same state as "THE BUDGET ACT CHANGES NOTHING · UNCONTESTED" (BudgetV35.cs:1051).
- Before §773 a pension-only or benefits-only bill was contested on this card, because the pension term and welfare moves were in its concern. Only rates-only bills showed this, since §768.

**The fix proposed.** When any statute part changes, add the budget card's label to uncontestedWords with wording that matches the slip, e.g. "Unopposed - the budget act changes nothing; its statutes are voted apart".

**The skeptic's evidence.** I could not refute it. The path is reachable and draws exactly what the finding says. It is wording only.

**The trace (unstaged diff).**
- ParliamentSystem.cs:897-902: `BudgetActOf` now returns `bill.SpendingOnly(country)` as soon as any statute part changes. HEAD and the index (896-897) stripped only the rates (`ChangesTaxRates ? WithoutRateChanges() : bill`).
- BudgetBill.cs `SpendingOnly` / `Without`: the pension part sets `PensionAgeSet = false`, the benefits part empties `WelfarePrograms`, and the rates part empties `TaxLines`/`BracketRates`.
- The concern's moves come from only four places (ParliamentSystem.cs:911-942):
  - `BracketRates` and `TaxLines`
  - `SpendingPercentChangesOf` (121-154): `SpendingPercentChanges`, `SpendingNominalTargets`, and the pension age at 143-153
  - `WelfarePrograms`
- `BuildBudgetBillFromDrafts` (GameController.cs:9809-9817) writes a spending target only for a line the player moved.
- So a pension-only or benefits-only Polish draft leaves the budget act with no move. `IsEmpty => _moves.Count == 0` (StanceModel.cs:63), and `contested` is false (GameController.cs:9410).

**The card.**
- `uncontestedWords` gets entries only for the statute-act labels (9302-9309). The budget label at 9297 never gets one.
- So 9415 prints `uncontested ?? "Unopposed - no change requested"`: "Annual budget bill - resolves in 21 day(s)." (BillDurationDays = 21, ParliamentSystem.cs:40), followed by "Unopposed - no change requested".
- Directly under it is "Pension act with the budget - voted if the budget passes ...", with a live lean. The pension act's concern carries the SocialSecurity move from 143-153.

**It is reachable.**
- The card is live on Politics › Parliament (PoliticsV35.cs:180).
- Introduce is enabled for any draft (BudgetV35.cs:864).
- Poland has a pension statute (PensionAgeStatute.cs:106) and four implemented programmes (WorldFactory.cs:681-685).

**It contradicts the Budget page.** The slip at BudgetV35.cs:1051 words the same state "THE BUDGET ACT CHANGES NOTHING · UNCONTESTED" when `acts.Count > 0`. §768's second pass fixed exactly this contradiction on the slip ("NOTHING CHANGES · UNCONTESTED" above the tax-act line). It was never fixed on the card. No doc or ruling accepts the card's wording.

**A correction to the finding's history.** Fund-only bills already showed this text on the budget card before §773. The concern never weighs the fund's terms (ParliamentSystem.cs:109-113), as item 6's skeptic noted. Before §773, then, the text appeared for rates-only bills (since §768) and fund-only bills (always). §773 adds pension-only and benefits-only bills, and any mix of statute parts with no spending change.

**Why it stays a note.**
- "Unopposed" and the green ink are right: an empty concern passes (ParliamentSystem.cs:334), and the sim records the act as "Annual budget bill" at SimulationManager.cs:1511.
- "No change requested" is literally true of the budget act. It reads false only against the card's label "Annual budget bill" and the acts listed "with the budget" beneath it.
- No vote, number or money path is affected, and the card is kept as built (not in the composition).
- It is weaker than items 6 and 10, whose fund-act text was outright false.

**The skeptic's corrected fix.** In DrawPendingLegislation (GameController.cs:9294-9310), keep the budget card's label in a local, e.g. `string budgetLabel = $"Annual budget bill - resolves in {budgetBill.DaysRemaining} day(s).";`, and pass that same local to `pending.Add`.

Inside the per-part loop, after the `Changes(part, ...)` check passes, also set:
`uncontestedWords[budgetLabel] = "Unopposed - the budget act changes nothing; the acts with it are voted apart";`
Any wording that matches the slip's "THE BUDGET ACT CHANGES NOTHING · UNCONTESTED" will do.

DrawPendingBillCard uses this text only when the budget act's concern is empty, so:
- a budget that also moves spending keeps its PASS/FAIL lean;
- a Polish bill with no statute change, and every other country, keep "no change requested".

This also covers the fund-only and rates-only cases that predate §773. Keep the strings free of CamelCase identifiers so MetaTextCheck passes.

### 11. Claim convention: a transcribed count and transcribed figures kept in comments this diff rewrote

- **Lens:** ui-text - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Data/BudgetBill.cs:80

**The scenario.** (a) BudgetBill.cs:80, rewritten in this diff, says "These three split a bill's rates so" (a count of ChangesTaxRates, TaxActPart and WithoutRateChanges). The fix for finding 14 replaced the parts count two blocks below but kept this count in the line it rewrote. If a fourth rates member is added, the comment is wrong, and CommentClaimCheck, which reads only backticked Type.Member names, cannot see it.
(b) WorldClock.cs:391, re-wrapped into the rewritten doc, says "the record's 2019 Sejm sat first on the 30th day, 2023's on the 29th". These figures are derived from the term dates, and no check asserts them: PollingDayDiagnostic:65 pins only the game's own 30th day.
(c) PresidentialVetoDiagnostic.cs:561, new, says "the government of record on an epoch in 2024 (KO+TD+NL)". This is transcribed from WorldClock.cs:250 (a cabinet DERIVED there from ministers' clubs). The test never reads it, because it re-plants the head and Finance, so a re-derived cabinet would leave the comment wrong.

**The fix proposed.** (a) "These split a bill's rates so". (b) Point to the record (the first sittings in api_sejm_term.json, read by PollingDayDiagnostic) or drop the figures. (c) Delete the parenthesis, or reference `WorldClock.TryGovernmentAt`.

**The skeptic's evidence.** The finding holds only on part (a). Part (b) is refuted and part (c) is marginal.

Which convention applies. CLAUDE.md:39 and docs/archive/CLAIM_CONVENTION_AND_DISCIPLINE.md:25 define DERIVED as "a fact about the code or the environment: a type or member name, a line number, a count, a measured figure, ...". The test (line 19) is "The code can change freely and no document becomes wrong". No check sees prose counts:
- CommentClaimCheck.cs:43 matches only a backticked `Type.Member`.
- PhantomGuardCheck reads guard names only.
- A grep of Assets/Editor finds no count-word scan.

(a) REAL, note. BudgetBill.cs:80, which this diff rewrote, reads: "// statute, Art. 217). These three split a bill's rates so; §773's parts split the rest."
- HEAD had "These three split a bill so." (§768, 454a23b).
- The three are the members at 84/106/114: ChangesTaxRates, TaxActPart and WithoutRateChanges. The count is true today but is a count of code members.
- The first pass's skeptic on finding 14 named this exact line as "a similar copied count", left out only because it was pre-existing. The author then rewrote the line (the "generalised by §773" header) and kept the "three".
- Across the whole unstaged diff, it is the only count word added in a comment. That was checked by grepping the added lines for two/three/four/both.
- CheckSuite.cs:144-150 is the project's own precedent: "TWENTY-ONE" while the array held twenty-five, "a count is referenced or generated, never transcribed - in a comment exactly as in a document".
- The author's finding-14 fix itself is right: BudgetBill.cs:121-125 now points to `BudgetBill.StatuteParts`.

(b) REFUTED. WorldClock.cs:391 reads "the record's 2019 Sejm sat first on the 30th day, 2023's on the 29th".
- The text is byte-identical on the - and + sides of the diff. It is §767 text (6880d67) that this diff only re-wrapped.
- It is a fact about the historical record, not about the code. No code change can make it false, so it passes the convention's test.
- The long form lists "a measured figure", not any real-world figure. Real-world numbers fall under discipline rule 2 (source, vintage, basis).
- The figures are correct. WorldClock.cs:149-150 have Poland2019 D(2019,10,13) to D(2019,11,12), which is day 30, and Poland2023 D(2023,10,15) to D(2023,11,13), which is day 29. api_sejm_term.json agrees ("from":"2019-11-12" num 9, "2023-11-13" num 10).
- The neighbouring §767 doc at 380-381 states record dates the same way, unpinned.

(c) MARGINAL. PresidentialVetoDiagnostic.cs:561 says "(KO+TD+NL)".
- It is accurate today. GovernmentRecord.AtStart (254-265) installs the record's cabinet from WorldClock.cs:250 `new[] { "KO", "TD", "NL" }`, and the 2024-01-15 epoch is non-provisional.
- Case (m) never reads it. It plants `g.PmParty = "NL"` and moves FinanceTreasury to KO. FinancePartner.Holder (FinancePartner.cs:116-127) reads only PmParty, Provisional and the Finance holder.
- It is also a historical fact (Tusk's cabinet in January 2024). It goes stale only if the record's row is re-keyed or re-derived (GAP G8). That is a low risk, and it is a party list rather than a type/member name, count or measured figure.

Re-grade: a note. The live, actionable part is the one word in (a).

**The skeptic's corrected fix.** (a) BudgetBill.cs:80: drop the count, giving "...(taxes are set by statute, Art. 217). These split a bill's rates so; §773's parts split the rest." Do not write "These four" or any other number. If a pointer is wanted, name the members as backticked `BudgetBill.ChangesTaxRates`, `BudgetBill.TaxActPart` and `BudgetBill.WithoutRateChanges` so that CommentClaimCheck resolves them.
(b) No change is owed. The text is a fact of the historical record, unchanged §767 text that was only re-wrapped, and it is correct (30 and 29 days, per WorldClock.cs:149-150 and api_sejm_term.json). Optionally, as a rule-2 sourcing nicety rather than a claim-convention fix, cite [API-TERM] beside the figures as the dates' source.
(c) Optional: delete "(KO+TD+NL)", or write "the government of record on an epoch in 2024 (`WorldClock.Governments`)". Nothing in case (m) depends on the composition.

### 12. PolishStatuteActs' log line says 'the rest of the budget applies' for the partner's boundary act, which has no budget

- **Lens:** ui-text - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Simulation/SimulationManager.cs:1302

**The scenario.** When the partner's boundary tax act falls (PartnerTaxAct now routes through PolishStatuteActs), the log reads "BUDGET: Poland - Tax act: income tax X % to Y %: falls - what it would set stays as it is, and the rest of the budget applies". This happens at a turn boundary with no budget before the chamber; what applies is the turn's decision, the lines' part. It affects the log only: nothing in Tools/ or Assets/Editor parses "BUDGET:" lines.

**The fix proposed.** Use the summary's own words ("and the rest applies", line 1280), or let the caller supply the context.

**The skeptic's evidence.** CONFIRMED AS A TEXT FACT. G:/UNITY/Projects/PoliSim/Assets/Scripts/Simulation/SimulationManager.cs:1302 is one format string shared by every caller of PolishStatuteActs:
  Debug.Log($"BUDGET: {country.Id} - {title}: {(passed ? "stands - it applies" : "falls - what it would set stays as it is, and the rest of the budget applies")}");
- Two callers are budget-carried, and the wording is right there: 1266 (ResolveGovernmentBudget) and 1512 (AdvanceBudgetBillDay).
- The third, PartnerTaxAct at 2699-2715, builds `new BudgetBill { GovernmentBill = true, TabledBy = written.Holder }` with TaxLines only. It calls PolishStatuteActs(country, act, out fell, new[] { Rates }) at 2704 and throws away the bill that comes back. What follows a fall is not "the rest of the budget". The rates are removed from the turn's decision, and either the lines' part lands or nothing of the step does (2706-2713, and the doc at 2696-2697).
- The summary's own words at 1280, "the rest applies", have no such problem. In E1's staged version the line read "the old rates stand and the budget runs on them" and served budget paths only. So E2 introduced the mismatch, in its first version; the review fixes did not cause it.

THE PATH THAT PRINTS IT. PresidentialVetoDiagnostic.cs case (l), lines 441-476, invokes PartnerTaxAct by reflection twice. Both checks require the veto to stand: partnerAdded[1] is "Vetoed by the President (Karol Nawrocki): Tax act: ", and the rate leaves the decision. Since n773b was 4 of 4 clean, that run printed "...and the rest of the budget applies" twice where no budget exists.

WHERE THE SCENARIO OVERSTATES IT. "At a turn boundary" has not happened in any run.
- In live play PartnerTaxAct cannot be reached. The call site (4781) needs partnerWrote.Taxes.Count > 0. FinancePartner.cs (Free at 214-215, the shortfall at 261-277) writes Taxes only for what is left after cutting the free lines. Nothing outside Assets/Editor writes SpendingPinChanges (grep: only BudgetBill.cs:54 and PolicyDecision.cs:62 declare it, and SimulationManager 1531 and 6505 copy and apply it), and the GameController diff adds no pin or spending line.
- Only (m) goes through AdvanceTurn, and there the act stood. Its check accepts either outcome, and the author measured that it stood.

NO CONSUMER.
- Nothing in Tools/ or Assets/Editor matches "BUDGET:" text. The StanceModelDiagnostic and GameController hits are unrelated strings.
- CheckSuite.OnLog and UiScreenshotDriver.OnRunLog count only Error, Exception and Assert. This line is an info Debug.Log.
- The claim convention (CLAUDE.md:33) governs documents and source comments, not runtime log text.
- In play, the LEVERS line at 4786 would follow at once with the exact account PartnerTaxAct appended ("the rates' part fell ...; the lines' part stands: ..." or "nothing of the step lands"). In (l), the check messages state the outcome.

The result is a misleading phrase in a debug log, seen only in the diagnostic's output.

**The skeptic's corrected fix.** At SimulationManager.cs:1302, change the false branch to the summary's words at 1280: "falls - what it would set stays as it is, and the rest applies". That reads right for all three callers: the budget act's remainder, and the partner's decision less its rates. Nothing parses the line, so nothing else changes. Passing in a caller-supplied context string would be more than this needs.

### 13. 'RULED (E2)' covers more acts than the ruling's words

- **Lens:** ui-text - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Simulation/SimulationManager.cs:1281

**The scenario.** E2's item, verbatim in the draft record, reads "D4, a rejected budget takes its tax act down with it: accepted." The summary says "RULED (E2): a budget the Sejm rejects takes its acts with it", which includes the pension, benefits and fund acts that E2's general rule created. That extension is a reading. The same block labels the benefit and fund parts themselves "READING, DECLARED", but not this. The repo's rule that every DECLARED premise says so is therefore not met.

**The fix proposed.** "RULED (E2) for the tax act; the other statute acts likewise - the general rule's reading, DECLARED".

**The skeptic's evidence.** I could not refute it. The label's scope is wider than the ruling's words. The behaviour is right; only the attribution overclaims.

1. What E2 says, from the session's draft record (scratchpad rec773.md:6, "verbatim"): *"D4, a rejected budget takes its tax act down with it: accepted."* This accepts §768's premise, which was scoped to the tax act. COMPLETED.md:36741 reads: "Premise, DECLARED - the ruling is silent: ... a budget the Sejm rejects takes its tax act with it (in the Sejm they are separate bills)". No item in E2 says when the pension, benefits or fund acts are voted. The pension clause gives two properties only: "it travels in its own act and can be vetoed, like tax rates".

2. What the code claims. SimulationManager.cs:1281: "RULED (E2): a budget the Sejm rejects takes its acts with it - they are voted only once it is adopted." Here "its acts" means all four acts listed in the same summary (1277-1278).
   - The behaviour does match. Both Polish paths vote the acts only after the budget passes: 1266 `appliedG = passed ? PolishStatuteActs(country, government, out fellG) : government` and 1512 `applied = passed ? PolishStatuteActs(country, bill, out _) : bill`. The frame-decision path is Sweden's only (WorldClock.cs:225).
   - The label does not. It puts E2's authority on a wider claim than E2 makes.

3. The block is inconsistent with itself. Three lines up (1278) it marks the benefits and fund acts "(the general rule's reading, DECLARED)". It then marks the timing of those same acts RULED. The repo keeps this line between a ruling's words and the builder's reading: CLAUDE.md:54 says "that carry-forward is the implementation's default, not the ruling's words". The first pass also treated a wrong RULED/DECLARED label as a real finding (#11).

4. The record repeats the overclaim. rec773.md:16 says "The rejected budget takes its acts with it. `PolishStatuteActs` says ruled." Its "Owed to Elias" list names the general rule's reading but not this extension.

5. The gap is real but narrow. For the pension act, E2's "like tax rates" makes the extension a close analogy, though not the ruling's words. In reality Poland's pension-age changes (2012, 2017) were stand-alone statutes, so voting the pension act independently of the budget is a coherent alternative Elias was never asked about. For the benefits and fund acts, the existence is already declared, so only the label is at stake.

Severity stays at note: comment and record wording only, with no runtime effect.

The finding's own fix is slightly off. It files the pension act under "the general rule's reading", but the pension act comes from E2's explicit pension clause, not the general rule.

**The skeptic's corrected fix.** Scope the RULED tag to the words Elias used and declare the extension. For example, SimulationManager.cs:1281:
"RULED (E2) for the tax act ('a rejected budget takes its tax act down with it: accepted'); the other acts ride the same way - READING, DECLARED: the pension act by E2's 'like tax rates', the benefits and fund acts with the general rule's reading above. Each is voted only once the budget is adopted."

In §773's record (rec773.md:16), say that the ruling named the tax act and the other acts follow as the reading. Add that reading to the 'Owed to Elias' list, beside 'the general rule's reading'.

Optional, same class: ParliamentSystem.cs:895-896, the BudgetActOf doc, glosses the E2 quote with 'the benefit levels and the fund's rules are each their own act'. It carries no reading/DECLARED tag there, and could take the same '(the general rule's reading, DECLARED)' as SimulationManager.cs:1278.

### 14. The author's response says the preview's whole counting of the partner's boundary rates is stated in §773's record; the draft record does not state it

- **Lens:** ui-text - **reviewer:** note - **skeptic:** note
- **Where:** C:/Users/elias/AppData/Local/Temp/claude/C--Users-elias/c8a2f04f-adf9-462b-8470-14c260fdbdb5/scratchpad/rec773.md:17

**The scenario.** The review's author item 16 (Reviews/2026-10-04_s773_e2_statute_acts.md) says: "The turn preview counting the partner's boundary rates whole is stated in §773's record, as an extension of D4's accepted premise."
- COMPLETED.md ends at §771. The only draft, rec773.md (written 20:41, before the response at 21:17), says just "The preview. It applies the whole draft, as before". It says nothing about the partner's boundary rates.
- PartnerTaxAct's only call is in AdvanceTurn (4783); PreviewTurnOnClone never consults it.
- The draft's "The pages" list also lacks the stance tile's clause and the Uncontested card.
- If the record is appended from this draft, the review's claim is false.

**The fix proposed.** Before the record lands, add: the turn preview applies the partner's boundary rates whole (it never runs the tax act), an extension of D4's accepted premise, DECLARED, with the stance tile's clause as the warning beside it. Also list the tile clause and the Uncontested card under the pages.

**The skeptic's evidence.** I could not refute it. Every fact in the finding checks out against the files.

1. The claim. Item 16 of the author's response (Reviews/2026-10-04_s773_e2_statute_acts.md:745, the same text as scratchpad e2_author.md:29) says: "The turn preview counting the partner's boundary rates whole is stated in §773's record, as an extension of D4's accepted premise." The verb is present tense, "is stated".

2. No record exists yet. The last heading in COMPLETED.md is §771 (line 36788). Each record lands with its own commit (5edca04 added §771's 33 lines). The first pass's refuted section says the same thing: "The §773 record is not yet written (COMPLETED.md ends at §771)".

3. The only draft does not say it. A search of the temp tree finds one §773 draft, scratchpad/rec773.md. Its mtime is 20:41; e2_author.md is 21:17:57 and the review file is 21:18.
   - Line 17 reads "**The preview.** It applies the whole draft, as before, the slip's veto line beside it." That is D4's premise about the player's own draft. The partner's boundary rates are not mentioned anywhere in the draft.
   - "The pages" (lines 30-33) lists the slip, the pending cards "one per act" and the night's scan. It has no stance-tile clause and no Uncontested card.
   - "Checked" (lines 35-38) also predates (k2), (k3), (k4), (m), the rates-only (l) and the title check.
   - The placeholders FORMATION (line 40) and REVIEWE2 (line 42) are still unfilled.
   - Compare §772. Its draft (rec772.md, 21:08) was refreshed one minute after its response (21:07) and took the review's premise as "PREMISE, DECLARED (the review's finding)". rec773.md was never refreshed after its response.

4. The premise is real code behaviour. In SimulationManager.cs:5194-5200, PreviewTurnOnClone calls `previewPartner = FinancePartner.Apply(previewedReal, decision, ...)` and then `ApplyTaxRateChanges(previewCountry, decision)`. PartnerTaxAct's only runtime caller is AdvanceTurn at 4783; the Editor also calls it by reflection at PresidentialVetoDiagnostic.cs:441. The preview never runs it.

5. Nothing else declares the premise. No code comment in the unstaged diff does. The preview's own comment at SimulationManager.cs:5190 is unchanged and still says the preview takes "the Finance partner's boundary step, as the boundary takes it". In the player's Poland that is now incomplete, because the boundary may withdraw the rates through PartnerTaxAct and the preview does not. First-pass item 13 did not list this comment. Outside the review file, the premise is written nowhere.

6. Why this is a note and not a defect.
   - It is about the record only; nothing changes at runtime.
   - The record has not landed, and the unfilled REVIEWE2 placeholder is where the statement would go.
   - The first pass's refuted [money] finding (review lines 666-689) shows that no live path makes the partner's step write rates in Poland: no line is ever pinned in play, and the uniform cut of about 1.4% stays under the 30% clamp. So the premise only matters in planted states such as (m).

One small imprecision in the finding: PartnerTaxAct also has the Editor's reflection call (point 4), so 4783 is its only runtime caller rather than its only caller. This does not affect the point.

**The skeptic's corrected fix.** Before §773's record lands, fill rec773.md as item 16 promised.
- **Owed to Elias.** §768 put D4's preview premise in this list. Add: "PREMISE, DECLARED (the review's finding 16): the turn preview applies the Finance partner's boundary rates whole. It never runs their tax act. This extends D4's accepted premise. The Finance-stance tile's clause is the warning beside it. No live state reaches it today, because the partner's step never reaches the rates unless a line is pinned."
- **The pages.** Add the stance-tile clause (shown where the President holds a veto) and the Uncontested card wording. Both landed after the draft was written.
- **Checked.** When REVIEWE2 is filled, also add (k2), (k3), (k4), (m), the rates-only (l) and the title check.
- **Optional.** Extend the preview's §716 comment at SimulationManager.cs:5190 to say that in the player's Poland the preview counts the partner's rates whole where the boundary would put them to a tax act (DECLARED, §773). Then the premise also sits at the code it describes, and "as the boundary takes it" stops implying parity.
- **Until then.** If the record will not be updated, change item 16's response from "is stated" to "will be stated".

## The second pass - refuted by the skeptics

- [fixes] The partner's boundary tax act feeds CabinetSystem.UnderPressure at the same boundary: a fall always triggers the loyalty roll, and a pass hides a failure from earlier that day. Neither PartnerTaxAct's doc nor the response says so - *The code facts in the finding are accurate. It still has no failing path, and the behaviour it describes is the standing rule working, not a gap that §773 opened.

1. NOTHING IN PLAY REACHES IT.
- PartnerTaxAct has one live caller, SimulationManager.cs:4781-4784. It is gated on `partnerWrote.Taxes.Count > 0`. The only other call is the diagnostic's reflection call at PresidentialVetoDiagnostic.cs:441.
- Taxes are written only past the clamps: FinancePartner.cs:261-262 (`if (shortfall <= 0f) { return taken; }`), then 277.
- Free (214-215) leaves out only pinned lines, lines the decision already carries, and claimed lines.
- In play the decision is BuildPlayerDecision (GameController.cs:6277-6300), which carries InterestRateChange alone.
- Assets/Scripts has no writer of SpendingPinChanges or `.Pinned =` apart from the applier (6505-6507), and that is fed only by harness decisions.
- Every Polish line is `isMandatory: false`, so the clamp is 30%. The discretionary floor of 0.2×SeedAmount grows with GDP (doc at 714-733). A step of at most 0.25 pp of GDP therefore never leaves a shortfall.

2. NO HARNESS REACHES IT WITH ANY EFFECT.
- (l) invokes PartnerTaxAct by reflection and then FinancePartner.Record. No AdvanceTurn runs, so TryRollCabinetEvents never runs.
- (m) does run AdvanceTurn, but its act stood.
- (m) also seats no ministers. Country.CabinetMinisters starts empty (Country.cs:869) and is filled only by GameController.PoliticsV35.cs:456 and the Testing harnesses.
- TryRollCabinetEvents draws only per seated minister (CabinetSystem.cs:490-493). So even with pressure true it does nothing and draws nothing.

3. THE FALL CASE IS THE DESIGNED RULE.
- CabinetSystem.cs:470: "UNDER PRESSURE when approval sits below this floor or a division was lost this turn".
- The act is a government bill: `new BudgetBill { GovernmentBill = true, TabledBy = written.Holder }`, tabled by a cabinet party.
- Its fall is a failed, non-motion division (DivisionRecord.Motion defaults to false). When the veto stands, PresidentialVetoGate flags the passage as a motion (1428) and appends a failed override record (1437).
- Every vetoed or failed ordinary statute already leaves the same records: since §761 for tax programs (1603-1604), and the same for labour (1715) and laws (1877). A boundary-day loss is pressure for all of them.
- §773 adds no new premise, so no declaration is owed. Writing UnderPressure's behaviour into PartnerTaxAct's doc would transcribe another method's facts, which the claim convention forbids.

4. THE PASS CASE IS UNDERPRESSURE'S OWN LIMIT, NOT NEW WITH §773.
- UnderPressure (474-477) reads only the newest entry dated today.
- Today the tax-program loop (1595-1610) resolves several bills on the same day, so a later pass already hides an earlier failure.
- An AI motion appends a record with Motion = true (3016-3017). That masks an earlier failure the same way, because UnderPressure requires the newest entry to be a non-motion.
- It is true that nothing in AdvanceTurn recorded a division before §773. Every RecordDivision and Append site is on a daily-tick path. But in play that is still true, because PartnerTaxAct never runs.*
- [tests] (l)'s fundKept check can never fail - *The finding's facts are right. `fundKept` is true under the current code, and it would also have been true before the `only` fix. Its conclusion, that the clause "adds nothing", is wrong. It is the only clause in the diagnostic that guards the fund itself, which is a different property from the one the record clauses guard.

What the finding gets right:
- SimulationManager.cs:2702-2704: `var act = new BudgetBill { GovernmentBill = true, TabledBy = written.Holder };` leaves `SwfShouldExist` at its default of false. The result of `PolishStatuteActs(country, act, out ..., new[] { BudgetBill.StatutePart.Rates })` is thrown away.
- Nothing on that path writes the fund. The path is `Changes`, `PartOf`, `GetBudgetBillConcern`, `WouldBillPass`, `StatuteActTitle`, `RecordDivision`/`DivisionLog.Append` (DivisionRecord.cs:100-125), `PresidentialVetoGate` (1423-1440), `Without`, and the approval ledger.
- So the defect from findings 4 and 7 only touched the record. It is caught by `partnerAdded.Count == 2` and the `!d.Title.Contains("Fund act: ")` clause. Before the fix, a planted fund would have added a third division, "Fund act: the sovereign wealth fund dissolved".

Why it still adds something:
- The clause is placed correctly, so it is not a tautology. At PresidentialVetoDiagnostic.cs:449-454 the fund is planted, `PartnerTaxAct` is invoked, `bool fundKept = pl.SovereignWealthFund != null;` is read, and only then is the fund restored. It is a post-condition on a method that has the country in hand.
- The only runtime code that dissolves a fund is `ApplyBudgetBillSpendingAndSwf` (SimulationManager.cs:1534-1537: `if (!bill.SwfShouldExist) { country.SovereignWealthFund = null; return; }`).
  - It is reached only through `ParliamentSystem.ApplyBillResult` (ParliamentSystem.cs:452-511), which writes no division.
  - `BudgetBill.PartOf`'s doc names this exact hazard: "a bill built new carries no fund ... and applied it would dissolve one".
- Suppose a later change sends the partner's act through `ApplyBillResult`, using the same shape the budget callers already use (SimulationManager.cs:1268 and 1514: `ParliamentSystem.ApplyBillResult(country, applied, passed, ApplyBudgetBillSpendingAndSwf)`).
  - The partner's new bill would then dissolve the planted fund.
  - No division is added, so `Count == 2` and the title clause stay green.
  - Only `fundKept` would fail. Neither (m) nor any later case holds a fund on the partner's path to notice it.
- Both first-pass skeptics asked for this assertion. Finding 4: "Assert `pl.SovereignWealthFund` still stands afterwards". Finding 7: "and the fund still present". The author's response claims only "the fund kept", which is true, and does not claim the clause caught the fixed defect.
- The check message ("a held fund draws no fund act") does not overclaim.

Dropping the clause would remove the only guard against a destructive and plausible regression. There is no failing path here.*
- [ui-text] PartnerTaxAct's heading claims every Polish Finance partner's rates are a tax act; its one caller covers only the player's Poland - *The scenario fails at its second step, "FinancePartner.Apply raises income tax and VAT" in an AI-governed Poland. FinancePartner.cs:189 `if (holder == null || ...) { return written; }`: Holder (117-127) is null in every reachable non-player Poland, because a non-player Poland can never seat a Finance partner.
(1) AI countries hold no election: SimulationManager.cs:2578-2580 "The AI countries hold no election in a run until their models exist (§618's ruling 4): their chambers hold as of record". PollingDayToday and TryPlayerPollingDay read the player's country only (477, 4178).
(2) Nothing changes an AI country's government during a run. The day tick that holds AdvanceConfidenceDay (SimulationManager.cs:309: motions, Speaker's rounds, constructive votes) is called only as `_simulationManager.AdvanceCountryDayTick(PlayerCountryId)` (GameController.cs:783). RoundsApply (3198) and AiWithdrawals (2956) also gate on the player's country. Portfolios are written only by GovernmentRecord's own AllocatePortfolios, FromProposal and LeaveCabinet, which are reached from player-country paths (2855, 3105, 3951).
(3) Choosing a country rebuilds the world at its start (GameController.cs:2173-2188), so a game-formed Polish government never carries into a run where Poland is AI.
(4) Every start other than Poland's own seats one of two governments of record (WorldClock.cs:249-250, starts at 104-113). Morawiecki's cabinet is PiS alone. Tusk's is KO, TD and NL with KO as PM. AtStart installs it (GovernmentRecord.cs:258-264) and AllocatePortfolios (149-183) gives Finance, Poland's heaviest post (the four-country mean, PortfolioSalience.cs:109), to the most outstanding entitlement. With seats KO 157, TD 65, NL 26 (PartySystem.cs:449-451), KO comes to about 0.633 x 13.39 - 2.385 = 6.09 against TD 3.51 and NL 1.40. Finance goes to KO, which is PmParty, so `held.Key == government.PmParty ? null` (FinancePartner.cs:124). Case (m) in the diff has to PLANT the partner: "its head re-planted to NL and Finance to KO".
(5) So every Polish Finance partner a game can seat is in the player's Poland, and both of its paths vote the rates as a tax act. Under a player head, the boundary call at 4781 reaches PartnerTaxAct (PresidentialVeto.Applies is `country == CountryId.Poland`, PresidentialVeto.cs:37). Under an AI head, PartnerStepsAtBoundary is false and the step rides TableGovernmentBudget (1172-1177), then ResolveGovernmentBudget, then PolishStatuteActs.
The heading at 2693, "THE FINANCE PARTNER'S RATES IN POLAND ARE A TAX ACT", is therefore true in every reachable state, and the call-site comment at 4779 already states "In the player's Poland". The first pass's residual nit took the AI-Poland mechanism as live without checking that a holder could exist there.*

## What the author did about the second pass

Nothing in the second pass changed a figure the game computes. What the author changed is wording, the fund act's title and the tests. The new tests were mutation-proved: each reordering or regression the reviewers named was planted, seen to fail, and restored byte-exact (`cmp` against a copy taken first).

- **1 and 9 - fixed in the text, behaviour put to Elias.** The declaration is qualified in both places, `BudgetBill`'s §773 note and the `PolishStatuteActs` summary:
  - What the fund's rules come to is the budget act's outcome alone.
  - But every act is a division of its own, and `CabinetSystem.UnderPressure` reads the day's newest division alone. On a turn-boundary day the last act voted decides it, so an act that passes after a fallen one hides the fall. The fund act, always passed and voted last, does so whenever it is voted.
  - DECLARED, and asked of Elias with the two behaviour changes the skeptic offered: (a) vote the fund act first, which removes only the fund's share; (b) make the pressure test read the day's lost bills, not the newest division, which also ends the breach below. Behaviour is unchanged until he rules.
  - The two "write-only" docs the premise leaned on (`ParliamentSystem.RecordDivision`'s and `DivisionLog`'s) now name `UnderPressure` as the reader that has broken the rule since P2-5.2. Comment-only; `DivisionRecord.cs` was outside the diff.
- **2 and 12 - fixed.** The fall's log line reads "falls - what it would set stays as it is, and the rest applies", the summary's own words, right for all three callers.
- **3 and 6 - labelled.** (l)'s Written is marked PLANTED in its comment and in both check messages: (j)'s searched move, in whatever direction the search found it, tabled by a holder no partner could be. The first pass's corrected fix 2 (build it from `FinancePartner.Apply` on a partner's world) is declined, recorded here: `PartnerTaxAct` reads neither the direction, the tax nor the holder, and (m)/(m2) now carry the partner-shaped move through the boundary itself on both branches.
- **4 - fixed as the corrected fix asks.** (k3) checks that its search found a step the Sejm passes and PiS opposes. Its run asserts:
  - three divisions;
  - the passage a motion whose title names the asked level;
  - the veto standing (`Required > 0`);
  - the level unchanged and the spending moved.

  Mutation-proved (n773m1): with `PartOf(Benefits)` emptied of its levels, (k3) fails, "none found".
- **5 - fixed.** (m) runs twice. (m2) is the same planted boundary with the Sejm re-planted to Konfederacja's seats alone (PLANTED; seats change only at an election, and Poland's confidence rules are `Unsourced`, so no AI motion can fire during the year). The act falls, and (m2) asserts:
  - income tax and VAT unchanged;
  - nothing of the step counted;
  - the step spent on the boundary date. (m)'s stood branch now checks that date too, not `!= MinValue`.
  - "Tax act failed" on the approval ledger. The run's armed log fold saw no error at the boundary's close, so the audit balances with the fall's cost inside the window.
  - the decision's overrides empty.

  `stood == !fall` is part of the check, so neither plant can silently test the other branch. Mutation-proved:
  - `Record` above `PartnerTaxAct`: (m2) fails, -0.25 counted on a fallen act (n773m1).
  - `PartnerTaxAct` below the turn's apply: (m) and (m2) both fail; the rates land and no act is voted (n773m2).
- **7 - fixed.** (k4) asserts `Sides.Count == 0`, so the declared premise trips there the day the fund's terms get a concern. (k4b) is added: a planted fund at the class's defaults has its contribution and one raw weight moved. The expected title is built from the planted figures, not transcribed, and asserted equal. The act passes uncontested and the rules land.
- **8 - fixed as the corrected fix asks.** The fund act's title gives:
  - the asset-class weights raw and without a unit ("0.#"), as the Fund tab does;
  - beside them, the shares of the fund they come to (`GetNormalizedWeight`, the tab's right-hand figure);
  - the contribution as "% of GDP a year" and the domestic allocation as "% of the fund".

  Rules are joined by "; " because the shares' list has commas. Pinned by (k4b).
- **10 - fixed.** When a statute part changes, the budget card's label is keyed to "Unopposed - the budget act changes nothing; the acts with it are voted apart", the slip's own reading. `DrawPendingBillCard` uses it only when the budget act's concern is empty, so a budget that also moves spending keeps its lean.
- **11 - (a) fixed:** "These split a bill's rates so" (no count). **(b)** refuted by its skeptic, so nothing changed. **(c) fixed:** "(KO+TD+NL)" is dropped from (m)'s comment.
- **13 - fixed.** The `PolishStatuteActs` summary reads RULED (E2) for the tax act, in the ruling's words. That the other acts ride the same way is a READING, DECLARED: the pension act by E2's "like tax rates", the benefit and fund acts by the general rule's reading. `BudgetActOf`'s doc tags the general rule's reading DECLARED. §773's record says so and puts it on the "owed to Elias" list.
- **14 - fixed.** §773's record states the preview premise (PREMISE, DECLARED: the turn preview counts the partner's boundary rates whole and never votes their tax act; the stance tile's clause is the warning beside it). The premise also sits at the code it describes, in the preview's §716 comment. The record's pages list the stance-tile clause, the Uncontested card and the budget card's wording. Its checks list (k2) to (k4b), (l)'s rates-only case, (m)/(m2), the title check and the mutation runs.

Measured after the fixes: n773c, 10 of 10 clean - the veto diagnostic, the Finance partner, the government's budget, the polling day, the start points and the five text checks.

## The third pass - confirmed (verbatim)

The same workflow on the second pass's fixes only, the author's response read first.

### 1. The fund title's shares clause can print a share that did not visibly move ('infrastructure 5.6 % to 5.6 %')

- **Lens:** text - **reviewer:** minor - **skeptic:** minor
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Simulation/SimulationManager.cs:1385

**The scenario.** A share is listed when its unrounded move is at least 0.05 (line 1385), but both figures are printed with "0.#", which rounds to a tenth. A move between 0.05 and 0.1 can therefore print two identical figures. Shares only start as whole numbers when the standing weights sum to 100, which is why (k4b), planted at the defaults, never reaches this.

Path in play: the player's Poland creates its fund with a drafted 40/30/5/15 mix (the creating bill carries the drafted weights). The next budget drafts equities 40 -> 39. The plate, Division Records and the Docket then read: "Fund act: the sovereign wealth fund's rules - its equities weight 40 to 39; its shares of the fund: equities 44.4 % to 43.8 %, bonds 33.3 % to 33.7 %, infrastructure 5.6 % to 5.6 %, real estate 16.7 % to 16.9 %".

A second case: a standing 40.1/30/15/15 with equities drafted 40.2 gives a shares clause whose only entry is "equities 40.1 % to 40.1 %".

Both were confirmed with a csc-compiled probe using the same float arithmetic and invariant "0.#" formatting: the infrastructure move is 0.0624 and the equities move 0.0598, both listed.

A brute force found no case from the class defaults, and 517 integer-weight cases one bill away from them. §768's review fixed exactly this for the tax act (lines 1406-1407: "so a small step never reads '12.4 % to 12.4 %'").

A smaller point: the comment at 1366-1367 says the shares are said "as the tab's right-hand figure says them". The tab rounds to whole percent ("F0", GameController.cs:10431), so for (k4b)'s case the tab shows 13 % where the title shows 12.5 %.

**The fix proposed.** Format both figures first and list a share only when the two strings differ. This replaces the 0.05 constant:

string a = from.ToString("0.#", inv), b = to.ToString("0.#", inv);
if (a != b) { shares.Add(Words(assetClass.ToString()) + " " + a + " % to " + b + " %"); }

(k4b)'s expected title is unchanged, since every share moves at least 2.5 there. Optionally pin it with a second planted mix: 40/30/5/15 with equities set to 39, asserting that no share entry reads "X % to X %".

In the comment, say "the share the tab's right-hand figure shows" rather than "as the tab ... says them".

**The skeptic's evidence.** THE FAULTY LINE (unstaged diff, in scope). SimulationManager.cs:1384-1385:
`float from = fund.GetNormalizedWeight(assetClass) * 100f, to = mix.GetNormalizedWeight(assetClass) * 100f;`
`if (System.Math.Abs(to - from) >= 0.05f) { shares.Add(... from.ToString("0.#", inv) + " % to " + to.ToString("0.#", inv) + " %"); }`
- A share is listed when its unrounded move is at least 0.05, but both figures print to a tenth. A move between 0.05 and 0.1 can therefore print as "X % to X %".
- The 0.05 constant came verbatim from the second pass's corrected fix (review line 1246). The edge came in with that fix.

THE PATH IN PLAY (each step checked):
- The Fund tab has no country gate: BudgetV35.cs:32-34 and 819 call DrawSwfPolicyContent.
- While a fund is drafted into existence, the weight sliders are live and their values are written to the inputs: GameController.cs:10320, 10433-10458 (`interactive` = draftExists, range 0..100).
- Integer values are always resting values for a slider: LedgerRow.StepFor gives 0.1, 0.5 or 1.
- The player's bill carries the drafted weights, the creating bill included: GameController.cs:9836-9839.
- Creation builds a new fund, then applies the bill's weights (clamped 0..100): SimulationManager.cs:1561-1575 and 5942-5960. The standing mix therefore need not sum to 100.
- On the next budget, ChangesFund fires on any weight move (BudgetBill.cs:171-179). PolishStatuteActs (1300-1305) builds the title from the standing fund before anything applies, and RecordDivision stores it.

REPRODUCED. I compiled a C# probe with Add-Type that uses the same float math and invariant "0.#" formatting:
- Case 1: 40/30/5/15 to 39/30/5/15 gives "equities 44.4 % to 43.8 % [-0.6242], bonds 33.3 % to 33.7 %, infrastructure 5.6 % to 5.6 % [move 0.0624], real estate 16.7 % to 16.9 %".
- Case 2: 40.1/30/15/15 to 40.2 gives "equities 40.1 % to 40.1 % [0.0598]" as the only share listed. This case needs the 0.1 slider step. Case 1 needs only whole numbers, so it can happen at any resolution.

WHY NOTHING CATCHES IT:
- (k4b) (PresidentialVetoDiagnostic.cs:571-586) plants the class defaults, which sum to 100, and moves equities by +20. Every share moves at least 2.5 (probe: 40 to 50, 30 to 25, 15 to 12.5, 15 to 12.5), and ShareWords lists all four without a condition.
- With a standing sum of 100 the "from" shares are whole numbers, so the case cannot arise.

PRECEDENT. §768's review (2026-10-03_s768_tax_act.md:15) counted "income tax 12.4 % to 12.4 %" as part of its Defect 2. The fix is commented in the code at SimulationManager.cs:1406-1407: "so a small step never reads '12.4 % to 12.4 %'". The fund title now breaks that rule.

SEVERITY: minor. It is wording only, on a narrow path (Poland, a player-made fund whose weights do not sum to 100, a small weight step), and the simulation is unaffected. It shows on the signing plate, Division Records and the Docket, and it repeats a class of fault the project has already fixed once.

SIDE POINT (note): the comment at 1366-1367 says the shares are said "as the tab's right-hand figure says them". The tab prints whole percents ("F0" + "% of fund", GameController.cs:10431) and the title prints tenths. The quantity is the same; the wording differs.

**The skeptic's corrected fix.** The finding's fix is right and complete. It is better than raising the precision: shares are derived values with any number of decimals, so no fixed format can prevent equal strings, but comparing the formatted strings rules them out by construction. In SimulationManager.cs:1384-1385:

float from = fund.GetNormalizedWeight(assetClass) * 100f, to = mix.GetNormalizedWeight(assetClass) * 100f;
string a = from.ToString("0.#", inv), b = to.ToString("0.#", inv);
if (a != b) { shares.Add(Words(assetClass.ToString()) + " " + a + " % to " + b + " %"); }

- The existing `if (shares.Count > 0)` guard already covers a weight step whose share moves are all invisible at a tenth. The weight clause still names the step.
- (k4b)'s expected title is unchanged, since all four share strings differ there.
- To pin the fix without copying the rule into the test: add a planted 40/30/5/15 fund with equities drafted to 39, and assert that the title holds no entry of the form "<x> % to <x> %" with the same figure on both sides. The title should keep the equities, bonds and real-estate shares and drop infrastructure.
- Comment at 1366-1367: say "the shares of the fund they come to (the quantity the tab's right-hand figure shows, here to a tenth)" in place of "as the tab's right-hand figure says them".

### 2. The UnderPressure premise's gloss 'the last act voted decides it' does not match the day tick's order

- **Lens:** text - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Scripts/Simulation/SimulationManager.cs:1286

**The scenario.** The same words appear at BudgetBill.cs:128-129. The first clause is exact: UnderPressure reads only the log's newest entry (CabinetSystem.cs:475-476), and only through the boundary's roll (CabinetSystem.cs:489, then SimulationManager.cs:5087, then AdvanceTurn:4811).

The gloss is not exact. In a passage about the budget's acts, "the last act voted" reads as the budget's last act. But the budget is resolved FIRST in the day tick: AdvanceBudgetBillDay runs at line 299, before the tax-program, welfare, labour, crime, sector, trade, drawdown and law days and AdvanceConfidenceDay (300-309). The partner's boundary tax act is recorded later still, inside AdvanceTurn (4804).

Example: a Polish budget's tax act is vetoed, and it is the budget's last act, on a boundary day. A TaxProgramBill introduced the same day then resolves in the same tick (AdvanceTaxProgramBillsDay records at 1624) and passes. UnderPressure reads the program bill and the veto is hidden, even though the budget's last act is the fallen one.

This matters for the question put to Elias. Option (a), voting the fund act first, fixes only the budget's internal order; any later division that day still decides.

**The fix proposed.** In both places, change "on a turn-boundary day the last act voted decides it" to "on a turn-boundary day the last division recorded before the boundary's roll decides it - the budget's acts are voted first in the day tick, so a bill resolved later that day, or the partner's boundary act, decides it instead". The rest of the sentence (the fund act, always passed and voted last among the acts, hides an earlier fall) is right as it stands.

**The skeptic's evidence.** The finding holds in part. One of its two paths is reachable in play, and it misses a third copy of the same gloss.

What holds, read in the fixed tree:
- The day tick: SimulationManager.cs:299 `AdvanceBudgetBillDay` runs first. Lines 300-308 are the other eight bill days and 309 is `AdvanceConfidenceDay`.
- GameController.cs:773-791 runs AdvanceDay, then the day tick, then `AdvanceTurn`, all on one CurrentDate.
- The roll: SimulationManager.cs:4811 `ApplyDomesticPolicy`, then 5087 `TryRollCabinetEvents(country, CurrentDate)`, then CabinetSystem.cs:489. That is the only caller of `UnderPressure`, which reads `entries[entries.Count - 1]` alone with `Date == today` (474-476).

The live path, the finding's own example:
- `IntroduceTaxProgramBill` (1585-1599) refuses only a second bill of the same TaxType. Nothing stops one being pending beside a budget.
- Both bills get `DaysRemaining = ParliamentSystem.BillDurationDays` (1499, 1599), both count down in the same tick, and both resolve on the same day.
- The budget's acts are recorded first (via 1305). The program bill's `RecordDivision` (1624) comes after.
- On a boundary day the roll reads the program bill's record. If it passed, a vetoed tax act that was the budget's last act is not what decides the test.
- Reaching it needs the budget-window hold off and a timed bill (DaysPerTurn 365 at :216; the window opens on the fiscal-year date in `TryOpenBudgetProcess`), plus a second bill introduced the same day. That is an edge case, but it can happen in play.

Why the wording is wrong in its context:
- BudgetBill.cs:128-130 ties "voted last" to `BudgetBill.StatuteParts` in the same sentence as "the last act voted decides it".
- SimulationManager.cs:1286-1288 does the same. Read naturally, the last act is the budget's last act, not the day's newest division.
- DivisionRecord.cs:80 has the same gloss, and the finding does not cite it: "a Polish budget records several acts on one day, so the last voted decides what it reads".

Corrections to the finding:
1. **The partner's boundary act cannot run in live play.**
   - `PartnerTaxAct` (4802-4805) needs `partnerWrote.Taxes.Count > 0`.
   - `FinancePartner.Tighten` (FinancePartner.cs:243-280) writes Taxes only for a shortfall left after a uniform cut on the free lines. `Free` (214-215) excludes only pinned lines and lines already in the decision.
   - The only writer of `SpendingLine.Pinned` is SimulationManager.cs:6530, from `decision.SpendingPinChanges`. Outside Assets/Editor that field is only declared, named in the impact ledger's list, or copied from a bill field (1552) that no UI sets.
   - Two earlier skeptics found the same (review lines 647-665 and 1436).
2. **The other clauses stay true.** "An act that passes after a fallen one hides the fall" and "the fund act ... whenever it is voted" still hold. A later division changes which entry is read, but the earlier fall stays unread.
3. **The question to Elias is not misled.** It already says (a) removes only the fund's share and (b) reads the day's lost bills instead of the newest division, which covers this case (review line 1592). Hiding by a later bill has existed since P2-5.2, not since §773.

Comment-only; no figure moves.

**The skeptic's corrected fix.** Fix the gloss in three places, not two:
- SimulationManager.cs:1286-1287 (the PolishStatuteActs summary)
- BudgetBill.cs:128-129 (the §773 note)
- DivisionRecord.cs:80 ("so the last voted decides what it reads")

Suggested wording for the first two: "...reads the day's newest division alone, whoever recorded it: on a turn-boundary day that is the budget's last act only when nothing is recorded after it before the boundary's cabinet roll (the order is `AdvanceCountryDayTick`'s, then `AdvanceTurn`'s). So an act that passes after a fallen one hides the fall from that day's pressure test - the fund act, always passed and voted last among the acts, whenever it is voted."

Suggested wording for DivisionRecord.cs: "...records several acts on one day, so a later one that passes can stand between a fall and the reader (DECLARED...)".

Keep the rest of each sentence as it is. Do not adopt the finding's "the budget's acts are voted first in the day tick" as a bare statement. That is a derived claim about the tick's order, and the claim convention says to reference `AdvanceCountryDayTick` instead. Drop "or the partner's boundary act" from the new wording, or say it applies under the harness only: `PartnerTaxAct` cannot run in live play today, because nothing in play pins a line or writes the decision's spending lines. No change is needed to the question put to Elias, since option (b) already covers later divisions.

### 3. (l)'s PLANTED comment says PartnerTaxAct 'reads neither the direction, the tax nor the holder'; its path reads all three

- **Lens:** text - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PresidentialVetoDiagnostic.cs:442

**The scenario.** PartnerTaxAct reads all three:
- It copies written.Holder into TabledBy (SimulationManager.cs:2722).
- It builds the act from written.Taxes and the decision's rates (2723).
- Its PolishStatuteActs call turns the tax and its direction into the concern: ParliamentSystem.cs:922, -standing.PointsOf(rate - standing.Rate).

That concern decides the vote and the veto. It is exactly why (j)'s CUT falls on this chamber.

What is true is narrower. The vote never reads the holder: a government bill's concern carries no author (ParliamentSystem.cs:889-892). And PartnerTaxAct's handling of a fallen act reads none of the three. The second review's suggested wording kept "(TabledBy is not read by the vote)"; the rewrite dropped it.

Risk: a maintainer who takes the comment at its word swaps the fixture for a partner-shaped rise to make it realistic. Whether any rise falls on (j)'s chamber was never searched (second pass, finding 6's skeptic), so (l)'s "voted, vetoed" check could fail, or need a re-search, for a reason the comment says cannot exist.

**The fix proposed.** Rewrite as "what PartnerTaxAct does with a fallen act reads neither the direction, the tax nor the holder (the holder rides as TabledBy, which the vote never reads); the direction is what makes this chamber veto it, so the fall's handling proved here is the partner's".

**The skeptic's evidence.** THE CLAIM: PresidentialVetoDiagnostic.cs:442-443: "...and PartnerTaxAct reads neither the direction, the tax nor the holder, so the fall proved here is the partner's". The second-pass skeptic's wording (review, finding 6) carried "(TabledBy is not read by the vote)". The rewrite dropped it. The same sentence is the review record's reason for declining the first pass's fix 2.

WHAT PartnerTaxAct DOES (SimulationManager.cs):
- 2722: `var act = new BudgetBill { GovernmentBill = true, TabledBy = written.Holder };` reads the holder.
- 2723: `foreach (TaxType type in written.Taxes) { if (decision.TaxRateOverrides.TryGetValue(type, out float rate)) { act.TaxLines[type] = rate; } }` reads the taxes and the new rates.
- 2725: `PolishStatuteActs(country, act, out ... fell, new[] { BudgetBill.StatutePart.Rates });`.
- That call runs 1302 GetBudgetBillConcern, 1303 WouldBillPass, 1305 RecordDivision, then 1306 PresidentialVetoGate, which calls Decide(..., passage.Sides) at 1447.

THE DIRECTION IS READ BY THAT VOTE:
- ParliamentSystem.cs:922: `concern.Add(StanceAxis.Redistribution, -standing.PointsOf(kvp.Value - standing.Rate));`. PointsOf is the plain delta for percentage taxes (TaxLine.cs:159), so the load carries the move's sign.
- StanceModel.cs:264 `concern.Loaded()` gives (Axis, End, Weight), and from it each party's side. WouldBillPass (ParliamentSystem.cs:336-341) counts the sides, and the veto reads them.
- (j)'s search (PresidentialVetoDiagnostic.cs:362-370) tries -2 and then +2, and breaks on the first move this chamber passes and the President's veto of which stands. The fixture's direction was chosen for this chamber.
- n773c.log:1023/1029: (l) still runs that cut (IncomeTax 12.36 -> 10.36), vetoed with the veto standing.
- n773c.log:1038: (m2)'s partner-shaped rise falls in the Sejm itself, one division and no veto. How an act falls depends on the direction and the seats.
- (l)'s check at 460-461 requires a second, "Vetoed by the President" division.
- No rise was ever searched on (l)'s chamber (second pass, finding 6's skeptic). Swapping in a partner-shaped rise could therefore break the check, though the comment says the direction does not matter.

WHAT IS TRUE:
- The holder never reaches the vote. ParliamentSystem.cs:891 has `concern.Author = governmentBill ? null : ...`, and TabledBy is read only on Sweden's alternative-budget path (SimulationManager.cs:1195-1256).
- The fall branch (2726-2735) does not branch on the direction, the tax type or the holder. The comment's conclusion about the fall's handling holds; its premise is false for the method as a whole.
- Comment-only, in an Editor diagnostic. No runtime effect, no figure wrong.

**The skeptic's corrected fix.** Comment-only, no figures (claim convention). Rewrite PresidentialVetoDiagnostic.cs:441-443 so the premise covers only the fall's handling, says the vote reads the direction, and brings back the dropped TabledBy clause. Suggested:

"PLANTED (the second review's findings 3 and 6): the Written is hand-built - (j)'s searched move, tabled by the head's own party. No partner writes either (Tighten only raises the household rates; Holder() never names the head's party). The vote reads the move's direction (the concern is signed by it), so the move is (j)'s: this chamber passes it and the President's veto of it stands. No partner's rise was tried on this chamber. The holder rides only as TabledBy, which the vote never reads. What PartnerTaxAct does once its act falls is the same whatever the direction, the tax or the holder, so the fall's handling proved here is the partner's; (m) and (m2) carry a partner-shaped move through the boundary."

Give the same qualification to the review record's decline reason in 'What the author did about the second pass' (items 3 and 6). The decline itself stands on (m) and (m2).

### 4. Claim convention: (k3)'s new comment transcribes a count ('the act's three divisions')

- **Lens:** text - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PresidentialVetoDiagnostic.cs:503

**The scenario.** The comment copies the count the check asserts (added.Count == 3, line 537). CLAUDE.md counts "a count" as DERIVED, and the rule there is that nobody transcribes, in any file. The second pass's finding 11(a) removed a count of the same kind ("These three split...").

The wording is also inexact. The three divisions are the budget act's, the benefits act's passage and the vote on the veto, not all "the act's". If the asserted shape changes (say the passage stops being recorded as a motion), the comment goes stale silently; no check reads prose counts.

**The fix proposed.** Rewrite as "the search's success, the divisions the bill records (the passage a motion, the veto standing) and the level kept are asserted", with no number.

**The skeptic's evidence.** The finding holds, and it is a note.

**Where the comment is.** G:/UNITY/Projects/PoliSim/Assets/Editor/PresidentialVetoDiagnostic.cs:502-504 is new, added by the fix for the second pass's finding 4 (it names "The second review's finding 4"):
"// age; the act is vetoed, the level stays and the spending moves. The second review's finding 4: the search's success, the act's three
// divisions and the veto are asserted, so a benefits act that could no longer be vetoed fails here."
The check it describes (about line 537) is `bool shape = added.Count == 3 && added[0].Title == "Annual budget bill" ... added[1].Title.StartsWith("Benefits act: ") ... && added[1].Motion && added[2].Title.StartsWith("Vetoed by the President (Karol Nawrocki): Benefits act: ") && !added[2].Passed && added[2].Required > 0`. So the comment copies the count the code asserts.

**Why it breaks the rule.**
- CLAUDE.md's claim convention lists "a count" as DERIVED. It says: "Nobody transcribes, anywhere, in any file." Only COMPLETED.md is exempt.
- CheckSuite.cs:149-150 says a count is "referenced or generated, never transcribed - in a comment exactly as in a document". That rule came from a count that went stale in a comment right next to the array it counted ("TWENTY-ONE" while the array held twenty-five). Being next to the code it describes does not exempt a count.
- The second pass's finding 11(a), graded a note and fixed, set the precedent: BudgetBill.cs:80 now reads "These split a bill's rates so", with no count.
- I grepped every added comment line in the unstaged diff for count words. "three" at 503 is the only new numeric count of code behaviour. The other hits are "one" or "both" used as articles or to state a rule.

**No check would catch it.**
- CommentClaimCheck.cs:43 matches only a backticked `Type.Member` (regex "`([A-Z]...)\.(...)`").
- DocumentClaimCheck reads the same pattern.
- MetaTextCheck's patterns are §, refs and TODO.
- No count-word scan exists in Assets/Editor. So if the asserted count changes and the check is edited, the comment goes stale without anything failing.

**The attribution is also inexact, as claimed.**
- SimulationManager.cs:1532 records "Annual budget bill", the budget act's own division.
- :1305 records the benefits act's passage.
- :1449-1456 (`PresidentialVetoGate`) marks that passage `Motion = true` and appends "the veto and the vote on it", one division of its own.
- So only two of the three are "the act's" (the benefits act's). Listing "and the veto" beside "three divisions" also counts the veto's division twice.
- n773c.log:1033 shows the shape today: "Annual budget bill | [motion] Benefits act: means tested welfare 2.1 to 0 | Vetoed by the President (Karol Nawrocki): Benefits act: ... the veto stands". The figure is true today but transcribed.

**One quibble with the finding.** Its example, "the passage stops being recorded as a motion", would not change the count. `Motion` is a flag set on a passage that is already recorded (:1449). The count would change only if the veto's division or the budget act's division changed. This does not refute the finding.

**Severity.** No behaviour is affected; only a comment is wrong under the claim convention. That is the same grade as 11(a): a note.

**The skeptic's corrected fix.** Replace the comment's last sentence at PresidentialVetoDiagnostic.cs:503-504 with this text, which has no number and attributes each division correctly: "The second review's finding 4: the search's success, the divisions the bill records (the budget act's, the benefits act's passage marked a motion and naming the asked level, the vote on its veto failing) and the level kept are asserted, so a benefits act that could no longer be vetoed fails here." The finding's own wording is also acceptable: "the search's success, the divisions the bill records (the passage a motion, the veto standing) and the level kept are asserted". Do not write any number word, and do not call all of them "the act's".

### 5. (k4b)'s expected title always lists all four shares and depends on SovereignWealthFund's defaults, so a legitimate default change turns it red, and the >= 0.05 skip rule is never tested

- **Lens:** tests - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PresidentialVetoDiagnostic.cs:583

**The scenario.** The expected string (583-586) always appends ShareWords for Equities, Bonds, Infrastructure and RealEstate. StatuteActTitle adds a class only when its share moves by at least 0.05 pp (SimulationManager.cs:1385). With the class defaults (SovereignWealthFund.cs:57-60, sum 100) and equities +20, the shares move by 10, 5, 2.5 and 2.5 pp, so today the two agree. Each unmoved class shifts by weight/6 pp. Suppose a later pass sets one default weight below about 0.3, e.g. RealEstateWeight = 0f. The game's title then correctly omits real estate, but the expected still ends ', real estate 0 % to 0 %', so (k4b) fails on a correct title. Likewise, a default EquitiesWeight above 80, or a ContributionRatePercent above 9, is clamped when the bill applies (MaxSwfDialLevel 100 at :5944, MaxSwfContributionRate 10 at :5934), so `landed` fails. The test can only give a false red, never a vacuous pass. But because every share here moves by at least 2.5 pp, the skip rule is pinned in neither direction: a threshold changed to, say, >= 1f stays green.

**The fix proposed.** Plant the fund's rules explicitly (labelled PLANTED) and include a class at weight 0, whose share cannot move, so the skip rule is pinned too. Or build the expected share list with the title's own >= 0.05 test. Keep the moved figures inside the clamps by construction.

**The skeptic's evidence.** THE FINDING'S FACTS HOLD. The test is right and green today. One claim is overstated.

1. The expected always lists all four shares. PresidentialVetoDiagnostic.cs:572 plants `var planted = new SovereignWealthFund();`, which uses the class defaults. Line 577 sets `SwfEquitiesWeight = equitiesFrom + 20f`. Lines 585-586 append ShareWords for Equities, Bonds, Infrastructure and RealEstate with no condition.

2. The game lists a class only if its share moves. SimulationManager.cs:1385: `if (System.Math.Abs(to - from) >= 0.05f) { shares.Add(...) }`.

3. The defaults are 40/30/15/15 (SovereignWealthFund.cs:57-60), so the shares move 10, 5, 2.5 and 2.5 pp. A class the act leaves alone moves w/6 pp at sum 100. n773c.log:1035 prints the title with all four shares.

4. A scratchpad model reproduced GetNormalizedWeight, the shares loop at 1380-1388 and the expected at 581-586 (scratchpad/k4b/k4b.cs, run with dotnet). Results:
   - As built: GREEN.
   - Mutant `>= 1f`: GREEN (survives).
   - Mutant with the skip removed: GREEN (survives).
   - Default RealEstateWeight 0: RED. The expected reads "real estate 0 % to 0 %"; the game omits it. The same happens with 0.2.

5. The clamps are as stated. ApplyBudgetBillSpendingAndSwf (1550-1574) calls ApplySwfPolicyChanges, which clamps at 5934 (MaxSwfContributionRate 10, line 614) and 5944 (MaxSwfDialLevel 100, line 619). `landed` (592-593) compares against the unclamped bill figures, so a large default fails `landed`.

6. A grep of Assets/Editor finds no other caller of the shares branch.

ONE CORRECTION. "Pinned in neither direction" is overstated. The model's mutant `>= 3f` is RED: infrastructure and real estate drop out. The rule is unpinned only against removing the skip or any threshold up to 2.5 pp.

GRADING THE TWO HALVES.
- Defaults half: weak. The trigger points (a weight under about 0.3, equities above 80, a contribution above 9) are far from 40/30/15/15 and 1. A red would explain itself, because the check message prints the title.
- Coverage half: real. A common Fund-tab move, 40/30/15/15 to 50/20/15/15 (equal and opposite), depends on the skip. With the skip removed the title adds "infrastructure 15 % to 15 %, real estate 15 % to 15 %" and (k4b) stays green.

TWO RELATED PROBLEMS THE FINDING DID NOT CLAIM.
(a) The 0.05 rule itself still prints a class whose two figures read the same. The model, as built:
   - Standing 40/30/21/7, equities 40 to 41: "real estate 7.1 % to 7.1 %".
   - Standing 40/30/15/14.7, equities 40 to 40.6: "infrastructure 15 % to 15 %, real estate 14.7 % to 14.7 %".
   A move of 0.05 to 0.1 pp can stay inside one "0.#" rounding step. So the finding's second fix, copying the >= 0.05 test into the expected, would bake this in.
(b) (k4b) never moves the domestic allocation: line 576 sets `SwfDomesticAllocationPercent = planted.DomesticAllocationPercent`. Its " of the fund" wording is therefore unpinned, although the author's response lists it ahead of "Pinned by (k4b)".

**The skeptic's corrected fix.** 1. Skip a class by what the title would print, not by a numeric threshold. At SimulationManager.cs:1385:
   `string f = from.ToString("0.#", inv), t = to.ToString("0.#", inv); if (f != t) { shares.Add(Words(assetClass.ToString()) + " " + f + " % to " + t + " %"); }`
   This removes titles such as "real estate 7.1 % to 7.1 %". It is a minor text fix.

2. In (k4b), plant the fund's figures explicitly and label them PLANTED, as (k4) does at line 551. Keep every moved figure inside the clamps by construction. Use an equal and opposite move: contribution 1 to 2, equities 40 to 50, bonds 30 to 20. Also move the domestic allocation, so its " of the fund" wording is pinned.
   Build the expected from the planted figures. Its shares clause should name equities and bonds only. The absence of infrastructure and real estate pins the skip against removal, and it is the Fund tab's commonest drag.

3. Optionally add a second plant at 40/30/21/7 with equities +1, and expect real estate to be absent. That pins the skip by its purpose (no "X % to X %").

4. Do not build the expected with a copy of the title's own >= 0.05 test. The copy would match whatever the title does and pin nothing.

### 6. The (m)/(m2) check message says 'became a tax act that fell' when no tax act was voted

- **Lens:** tests - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PresidentialVetoDiagnostic.cs:656

**The scenario.** `stood ? "stood" : "fell"` prints 'fell' whenever stood is false, and stood is also false when taxAct == null. n773m2.log:993-994 show this: with PartnerTaxAct moved below the turn's apply, no act was voted, yet both FAIL lines read '... the partner's tightening became a tax act that fell; income tax 12.37 -> 12.74, VAT 23 -> 23.37 ...; recorded: '. The empty 'recorded:' is the only sign that nothing was voted. Someone triaging a red bar is told an act fell, when in fact none was voted and the rates landed.

**The fix proposed.** Print `taxAct == null ? "was never put to the Sejm" : stood ? "stood" : "fell"`.

**The skeptic's evidence.** I tried to refute this and could not. The finding is right, and the log shows it happening.

The code, in G:/UNITY/Projects/PoliSim/Assets/Editor/PresidentialVetoDiagnostic.cs:
- 645: `DivisionRecord taxAct = added.FirstOrDefault(d => d.Title.StartsWith("Tax act: ", StringComparison.Ordinal));`
- 647: `bool stood = taxAct != null && taxAct.Passed && !vetoStood;` A missing act therefore counts as not stood.
- 655: the message reads "... the partner's tightening became a tax act that {1}; ...".
- 656: `stood ? "stood" : "fell"`. A null act prints "fell".

How no act gets voted: in PolishStatuteActs, line 1300 skips a part the bill does not change (`if (!bill.Changes(part, country)) { continue; }`). If PartnerTaxAct runs after ApplyDomesticPolicy (SimulationManager.cs:4811), the rates are already in force. No division is recorded, and PartnerTaxAct returns early at line 2726.

The log shows the result. n773m2.log:993-994, both FAIL lines: "the partner's tightening became a tax act that fell; income tax 12.37 -> 12.74, VAT 23 -> 23.37; the stance counted -0.25 pp; ... recorded: ". The "recorded:" list is empty.

What limits the damage:
- The check's verdict is correct. Line 654 requires `taxAct != null`, so the run fails as it should.
- `Check` (line 123) prints the message on ok lines too. An ok line guarantees `taxAct != null && stood == !fall`, so ok lines are always accurate (n773c.log:1037-1038).
- The wrong word appears only on a FAIL line where no tax act was recorded. That line also carries the evidence against it: the empty "recorded:", and in n773m2 the moved rates.
- The author was not misled; the review response reads "the rates land and no act is voted".

A worse case the finding does not name: if a regression makes the partner write no rates on the fall branch, the rates do not move. The line would then read like a real fall ("fell; income tax 12.37 -> 12.37 ...; the fall NOT on the approval ledger; recorded: "). Only the empty "recorded:" and the ledger clause would show that nothing was voted.

Grade: note. Only the FAIL text is affected, not a verdict or a game figure. That matches how the second pass graded its log-wording findings 2 and 12.

**The skeptic's corrected fix.** The fix as proposed would read "became a tax act that was never put to the Sejm", which contradicts itself. Move the verb into the argument instead.

1. In the format string at PresidentialVetoDiagnostic.cs:655, change "the partner's tightening became a tax act that {1}" to "the partner's tightening {1}".
2. Pass this as {1}: `taxAct == null ? "was put to no vote - no tax act recorded" : stood ? "became a tax act that stood" : "became a tax act that fell"`.
3. Optional, same line: when taxAct == null, print "; nothing on the approval ledger" in place of "; the fall NOT on the approval ledger", because that clause assumes a fall happened.
4. Optional, same line: print the observed `holder` instead of relying on the PLANTED text "KO holds Finance". `holder == "KO"` is part of the check, but a FAIL on it is not visible in the line today.

Text only: no check's verdict changes, and nothing in the game changes.

### 7. (l)'s PLANTED comment, 'PartnerTaxAct reads neither the direction, the tax nor the holder', drops the qualifier that made it true

- **Lens:** tests - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PresidentialVetoDiagnostic.cs:442

**The scenario.** PartnerTaxAct reads written.Holder into TabledBy (SimulationManager.cs:2722), and written.Taxes with their rates into the act (:2723). The direction and the tax are what the act's concern weighs, so they decide whether it falls; that is why (l) must borrow (j)'s searched, vetoed move. The true claim is narrower: the bookkeeping after a fall reads none of them, and the vote never reads TabledBy (its only reader is the alternative-budget contest, :1195-1256). The second review's suggested text carried '(TabledBy is not read by the vote)'. Both the comment and the response (review :1595) drop it. Separately, 'No partner writes either' follows 'in whatever direction the search found it', and it holds only while (j)'s search returns a cut (it tries -2 first). A maintainer who trusts the comment could swap in a partner-shaped rise on (j)'s planted chamber, where no rise was ever searched. (l)'s two-division veto shape would then rest on an unmeasured outcome.

**The fix proposed.** Reword: '... and PartnerTaxAct's handling of a fallen act reads neither the direction, the tax nor the holder (the vote never reads TabledBy), so the fall proved here is the partner's; the move itself must be one the planted chamber vetoes, which (j)'s search supplies.'

**The skeptic's evidence.** The core of the finding holds. Its title and its claimed harm go too far.

WHAT HOLDS
- The second review's suggested comment (Reviews/2026-10-04_s773_e2_statute_acts.md:1120) reads: "PartnerTaxAct reads neither the direction, the tax nor the holder (TabledBy is not read by the vote), so the fall proved here is the partner's."
- The new (l) comment (PresidentialVetoDiagnostic.cs:439-443) takes that text almost word for word but drops "(TabledBy is not read by the vote)". The response at review :1595 drops it too. CommentClaimCheck and PhantomGuardCheck would not have flagged it (no backticked Type.Member, no Check/Harness/Diagnostic name), so no repo check forced the cut.
- Without the qualifier, the claim is contradicted by PartnerTaxAct's own first lines:
  - SimulationManager.cs:2722 `var act = new BudgetBill { GovernmentBill = true, TabledBy = written.Holder };`
  - :2723 `foreach (TaxType type in written.Taxes) { if (decision.TaxRateOverrides.TryGetValue(type, out float rate)) { act.TaxLines[type] = rate; } }`
- The vote PartnerTaxAct calls (:2725) weighs the tax and its signed move. ParliamentSystem.cs:922 `concern.Add(StanceAxis.Redistribution, -standing.PointsOf(kvp.Value - standing.Rate));`. So the direction and the tax decide whether the act falls, and that is why (l) needs (j)'s searched, vetoed move (diag :362, `-2f` tried first).
- The holder really is inert:
  - Authored reads GovernmentBill, not TabledBy (ParliamentSystem.cs:891).
  - A grep of Assets finds TabledBy decided on only in the alternative-budget contest (SimulationManager.cs:1195-1256). Elsewhere it is only copied (BudgetBill.cs:108, 190-195).
  - The qualifier was what told the reader this.
- The comment's conclusion is still right in the sense meant. The handling after a fall (:2726-2734) branches on none of the three. Only the stated premise is wrong as written.

WHAT IS REFUTED OR OVERSTATED
1. "The qualifier that made it true": the qualifier only covered the holder. The overstatement about direction and tax was in the second review's own suggested text as well.
2. The harm, "(l)'s two-division veto shape would then rest on an unmeasured outcome", does not happen.
   - (l)'s Check (diag :460-461) asserts `partnerAdded.Count == 2` and the "Vetoed by the President (Karol Nawrocki): Tax act: " title. A swapped-in move that the planted chamber does not veto turns (l) red, not silently green.
   - The partner-shaped fall is already carried through the real boundary by (m2) (diag :614-656, `stood == !fall`), and the comment points there.
3. "No partner writes either" holds today: (j) finds a cut (n773c.log:1023, "IncomeTax 12.36 -> 10.36"). Its stated reason ("Tighten only raises the household rates") covers only a cut. Tighten raises every free household line by the same points (FinancePartner.cs:270-279). The PLANTED label stays true whatever the search finds, because of the holder (FinancePartner.cs:124).

The change would be comment-only, with no effect on behaviour or tests.

**The skeptic's corrected fix.** Comment-only. Reword PresidentialVetoDiagnostic.cs:441-443 so that no part depends on the direction the search finds, and the holder's qualifier is restored. For example: "... (j)'s searched move, in whatever direction the search found it, tabled by the head's own party, which Holder() never names - so no partner writes this Written (Tighten only raises the household rates besides). PartnerTaxAct passes the holder on as TabledBy, which the vote never reads, and what it does once the act falls branches on none of the direction, the tax or the holder. Whether the act falls is the vote's, and (j)'s search supplied a move the planted chamber vetoes, so the fall handled here is the partner's. (m)/(m2) carry a partner-shaped move through the boundary on both branches." Optionally narrow the review response's reason at :1595 the same way. The decline of the first pass's fix 2 already stands on its second ground, (m)/(m2). No figures are transcribed, and no test or behaviour changes.

### 8. (k3)'s rewritten comment transcribes a count: 'the act's three divisions'

- **Lens:** tests - **reviewer:** note - **skeptic:** note
- **Where:** G:/UNITY/Projects/PoliSim/Assets/Editor/PresidentialVetoDiagnostic.cs:503

**The scenario.** CLAUDE.md's claim convention treats a count of the code's behaviour as DERIVED, never transcribed into a comment. The second review's finding 11(a) removed 'These three split...' for exactly this reason. The (k3) comment written in this pass says 'the act's three divisions ... are asserted'. Suppose a vetoed act later records a different number of divisions (e.g. the veto folded into the passage record). `added.Count == 3` would be edited, and the comment could be left saying three. CommentClaimCheck reads only backticked Type.Member names, so nothing catches it.

**The fix proposed.** Name the divisions instead of counting them: 'the act's divisions (the budget act, the act's passage, the vote on the veto)'.

**The skeptic's evidence.** I could not refute it. The comment is new in this pass, it transcribes a count of the code's behaviour, and no check sees it.

1. **The line is in scope.** PresidentialVetoDiagnostic.cs:502-504 is an added line in the unstaged diff and cites the second review's finding 4, so the second-pass fix wrote it: "The second review's finding 4: the search's success, the act's three divisions and the veto are asserted, so a benefits act that could no longer be vetoed fails here." The second pass's skeptic on finding 11 had grepped the added comment lines for two/three/four/both and found "These three" as the only count. A fresh grep of the unstaged diff's added comment lines for two to six finds this line, plus "TD is one key for two parties" in DeclaredRedLines.cs, which is a real-world fact. So the fix removed one count and added another.

2. **It transcribes the code.** Line 537 asserts `bool shape = added.Count == 3 && ...`. CLAUDE.md's claim convention "governs every document AND every source comment" and lists "a count" as DERIVED. It allows only GENERATED, REFERENCED or DELETED, and says "Nobody transcribes, anywhere, in any file." The repo treats this as a real nit and fixes it:
   - CheckSuite.cs:146-150, the TWENTY-ONE precedent: "never transcribed - in a comment exactly as in a document".
   - The s642 review, item 13: "a transcribed count ("the three methods"). Fixed."
   - In this same pass, 11(a) took "three" out of BudgetBill.cs:80 ("These split a bill's rates so") and 11(c) took "(KO+TD+NL)" out of (m)'s test comment.

3. **Nothing catches it.** CommentClaimCheck.cs:43 matches only a backticked `Type.Member`. DocumentClaimCheck reads `.cs` files only to index declarations (lines 114-120) and checks root `.md` files.

4. **The count is also misattributed today.** n773c.log:1033 records "Annual budget bill | [motion] Benefits act: means tested welfare 2.1 to 0 | Vetoed by the President (Karol Nawrocki): Benefits act: ...".
   - "Annual budget bill" is the budget act's own division. AdvanceBudgetBillDay writes it at SimulationManager.cs:1532, before PolishStatuteActs runs (1533).
   - The benefits act has two divisions: its passage (RecordDivision at 1305, made a motion at 1449) and the vote on the veto (Divisions.Append at 1458).
   - So "the act's three divisions", where "the act" is the benefits act as in the sentence before it, counts the budget act's division as the act's.

5. **Grade: note.** It is comment-only. The test is correct and was mutation-proved (n773m1.log:989 "none found"). The proposed fix, "the act's divisions (the budget act, ...)", removes the count but still gives the budget act's division to "the act".

**The skeptic's corrected fix.** Drop the count and attribute the divisions correctly. Line 504 runs past the file's comment width, so re-wrap it. For example, PresidentialVetoDiagnostic.cs:503-504:
"// age; the act is vetoed, the level stays and the spending moves. The second review's finding 4: the search's success, the divisions the bill records (the budget act's, the act's passage as a motion, the vote on the veto) and the veto standing are asserted, so a benefits act that could no longer be vetoed fails here."
A shorter alternative: "...the search's success, the divisions recorded and the veto are asserted...".
Naming the records describes the ruled shape, the way (k)'s own comment does ("the budget act carries the spending alone, the age its own pension act, vetoed"). Do not write another number. The count itself lives in the `added.Count` assertion.

## The third pass - refuted by the skeptics

- [tests] (k4b) moves only the contribution and the equities weight, so the domestic allocation's new '% of the fund' and the other three weight labels are unpinned, though the response says finding 8's fix is 'Pinned by (k4b)' - *The coverage facts are right. The conclusion has no failing path.

WHAT THE FINDING GETS RIGHT
- PresidentialVetoDiagnostic.cs:576-577 moves two things only: SwfContributionRatePercent (+1) and SwfEquitiesWeight (+20). Domestic allocation, bonds, infrastructure and real estate are copied from `planted`.
- n773c.log:1035 shows the title (k4b) asserts: "...its contribution 1.0 % to 2.0 % of GDP a year; its equities weight 40 to 60; its shares of the fund: equities 40 % to 50 %, bonds 30 % to 25 %, infrastructure 15 % to 12.5 %, real estate 15 % to 12.5 %".
- A grep of Assets finds no other check that builds a fund-rules title.
- So the domestic allocation's clause (SimulationManager.cs:1372) and the bonds, infrastructure and real-estate labels (1375-1377; 1374 is equities, which is pinned) are rendered by no check.

WHY IT IS NOT A PROBLEM

1. The domestic allocation's clause cannot render in play. Since P5-B4 it has no slider (GameController.cs:10395-10399), and every bill builder carries the standing figure:
   - Player: `SovereignWealthFund standingDefaults = fund ?? new SovereignWealthFund();` then `bill.SwfDomesticAllocationPercent = standingDefaults.DomesticAllocationPercent;   // P5-B4: no slider` (GameController.cs:9832-9835). TableShadowBudget tables that same draft.
   - AI: `bill.SwfDomesticAllocationPercent = fund.DomesticAllocationPercent;` (AiFinanceMinistry.cs:123).
   - Partner: PartnerTaxAct's act (SimulationManager.cs:2722) votes Rates only.
   - `Without(Fund)` writes the fund's own figure back (BudgetBill.cs:224).
   - The only writer of SwfDomesticAllocationOverride in Scripts is the bill-to-decision copy at SimulationManager.cs:1569.

   So `Rule("its domestic allocation", ...)` never fires, and a wrong suffix there has no failing path. The proposed fix of moving it in (k4b) would plant a bill no game path can table. That pins dead text on a field C-N6 calls "honest scenery" (SovereignWealthFund.cs:32-55).

2. The response does not overclaim in substance.
   - Item 7 (Reviews/...:1613) states (k4b)'s scope exactly: "its contribution and one raw weight moved".
   - "Pinned by (k4b)" (1619) holds for what finding 8 asked. The shared `Weight` helper's form (1370: "0.#", no unit) is pinned through the equities row, and the shares list (1380-1387) is pinned for all four classes, including their Words() names.
   - The response nowhere claims each label is pinned. The three labels read correctly today. A future typo in one is the ordinary limit of a single-case test, not a defect in the fix.

3. One of the finding's own claims is wrong: "Wrong-field errors among the weights are still caught, because the planted weights differ from one another."
   - Infrastructure and real estate are both 15 at the class defaults (SovereignWealthFund.cs: `InfrastructureWeight = 15f; RealEstateWeight = 15f;`), so a swap between those two fields would not trip.
   - The current field mapping at 1374-1377 and in the mix at 1380 is correct, so there is no failing path there either.

Nothing the fix touched is broken. (k4b) passes in n773c with the title above.*

## What the author did about the third pass

The third pass found no defect: one minor and seven notes, two of them seen by both lenses. All are fixed. The changes are text, the fund title's rule and the tests; nothing on the money path computes differently.

- **1 - fixed, and widened to the title's every figure.** A figure is now listed only where its printed "from" and "to" differ. That is §768's rule for the tax act's title, and it now covers all three of the fund title's helpers:
  - the contribution and the domestic allocation, printed to "0.0#";
  - the raw weights and the shares, printed to "0.#".

  So a small step never reads "X % to X %". The comment now says the shares are "the quantity the tab's right-hand figure shows, here to a tenth".

  **(k4c)** pins the rule by its purpose:
  - **The plant:** a PLANTED mix whose weights do not sum to a hundred, with equities moved one point.
  - **What it asserts:** real estate's share, which moves less than the tenth it is printed to, is not named, and no share reads "X % to X %".
  - **Why it bites:** under the old 0.05 threshold, that share would have printed the same figure twice.
- **2 - fixed in all three places, in the skeptic's wording:** the `PolishStatuteActs` summary, `BudgetBill`'s §773 note and `DivisionLog`'s doc.
  - `UnderPressure` reads the day's newest division, whoever recorded it. That is the budget's last act only where nothing is recorded after it before the boundary's cabinet roll.
  - The order is referenced, not stated: `AdvanceCountryDayTick`'s, then `AdvanceTurn`'s.
  - The partner's boundary act is not named, since it cannot run in live play.
  - The question to Elias is unchanged: option (b) already covers a later division of the day.
- **3 and 7 - fixed.** (l)'s PLANTED comment now says what is true:
  - The vote reads the move's direction, because the concern is signed by it. So the move is (j)'s: this chamber passes it and the President's veto of it stands. No partner's rise was tried on this chamber.
  - The holder rides only as `TabledBy`, which the vote never reads.
  - What `PartnerTaxAct` does once its act falls is the same whatever the direction, the tax or the holder.

  The second pass's decline reason is qualified the same way: "reads neither the direction, the tax nor the holder" was too broad as written. The decline itself stands on (m) and (m2).
- **4 and 8 - fixed.** (k3)'s comment names the act's divisions (the budget act, the benefits act, the veto) without counting them.
- **5 - fixed.** (k4b) is re-planted with every figure set in the test (PLANTED), each moved inside its clamp by construction:
  - the contribution and the domestic allocation each moved, so the allocation's "of the fund" is pinned too;
  - equities up and bonds down by the same step, so the infrastructure and real-estate shares do not move and are not named.

  The expected title is built from the planted figures. The skeptic warned against copying the title's own threshold into the test, and the test does not copy it; (k4c) pins the skip.
- **6 - fixed.** The (m)/(m2) message:
  - carries its verb in the argument: "was put to no vote - no tax act recorded", "became a tax act that stood" or "... fell";
  - says "nothing on the approval ledger" where no act was voted;
  - prints the partner it read (`holder`), so a FAIL on the plant is visible.
- **The refuted finding** (the allocation's wording unpinned) needs nothing. (k4b) now moves the allocation anyway, as finding 5's skeptic suggested; that is harmless, since the act's apply writes it.

**Found while fixing, and fixed.** (k4c)'s first run (n773e) failed on the test, not the title. The act's apply moves the planted fund object itself, so words read after the run saw the new weight. The step's words are now taken before the run.

Measured after the fixes: n773f, 12 of 12 clean:
- the veto diagnostic, (k4b) and (k4c) included;
- the Finance partner and the government's budget;
- the formation sweep at its new pin (§776);
- the Polish declarations, the declaration dates, the polling day and the start points;
- the five text checks.
