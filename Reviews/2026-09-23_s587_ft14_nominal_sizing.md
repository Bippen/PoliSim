# Review: FT-14 - an authored one-off cost sized on nominal GDP (2026-09-23, s587)

**What was reviewed.** The FT-14 change: `Assets/Scripts/Simulation/AuthoredImpactScale.cs` (`ToCountryBillions` on nominal GDP, where it read real GDP) and `Assets/Editor/DomesticMoneyBasisDiagnostic.cs`'s share. The family `traj_ft14` is byte-identical to the baseline `ft16`, so no digest moved; the review is owed because the change books money (the tier's clause), and after this review `AuthoredImpactScale.cs` is named into the ledger's money pattern, so its state carries a row.

**The form.** One independent read-only reader, told the ruling and every caller. The report is below VERBATIM, then what was done with each finding.

---

# Adversarial review - FT-14: AuthoredImpactScale.cs sized on nominal GDP

**VERDICT: SIGN WITH FIXES.** The one-line change in `Assets/Scripts/Simulation/AuthoredImpactScale.cs:36` is correct and consistent with the ruling ("booked nominal, sized nominal"). I could not break the booking path: every caller of the sized figure lands it in a nominal book, the scale's denominator really is a seed figure where real equals nominal, no saved field holds a sized figure, and the byte-identity claim for `traj_ft14` holds. Two fixes are owed before commit. First, **nothing in the bar can tell the old rule from the new one** (F1). Second, **AuthoredImpactScale.cs is outside the money pattern** (F2). A pre-existing sign inversion on the Docket's cost line (F3) is not caused by FT-14, but it prints the figure FT-14 re-sizes, so it goes on the record as its own row.

Scope read: the staged diff (`git diff --cached -- Assets`: AuthoredImpactScale.cs, DomesticMoneyBasisDiagnostic.cs), every caller of `ToCountryBillions` / `ApplyOneTimeBudgetImpact`, the resolve and roll sites, the preview path, persistence, SimulationTestRunner, TrajectoryBaselineDump, UiScreenshotDriver, ScenarioLibrary and Tools/bar_tier.ps1. Read-only; nothing was run.

---

## Findings, ranked

### F1 (FIX BEFORE COMMIT) - no check can tell the two rules apart; the family cannot either
- `Assets/Editor/DomesticMoneyBasisDiagnostic.cs:155-156`: `share = |ToCountryBillions(a, c)| / c.State.NominalGdp`. With the new formula this is `|a| / AuthoredScaleGdp` for every country at every date. Before the change it was `|a| * GDP/29000 / GDP`, which is also `|a| / 29000`. **The assertion is a tautology under both rules.** It passes whether the seam divides by GDP, by NominalGdp, or by anything else, as long as the diagnostic uses the same denominator as the formula. The USA identity check (`:176-183`) runs on a fresh world where PriceLevel = 1, so it is also blind to the change.
- `traj_ft14 == traj_ft16` holds only because the change is never reached (see Q5). So after this commit, no check exercises the new behaviour at PriceLevel ≠ 1.
- **Fix:** add one assertion that can fail. Take a country after the two closed years, where PriceLevel is above 1, or set `PriceLevel = 2` on a fresh country. Assert `ToCountryBillions(a, c) == a * c.State.GDP * c.State.PriceLevel / AuthoredScaleGdp`, within float tolerance. Also assert that the applied share of *real* GDP equals `|a|/29000 * PriceLevel`: that is FT-14's signature. It is red on the parent commit and green on this one, and that is the evidence the review ledger needs.

### F2 (FIX BEFORE COMMIT) - AuthoredImpactScale.cs belongs in `$money`: YES
- `Tools/bar_tier.ps1:37` matches file **names** against `Fiscal|Budget|Tax|Spending|Debt|Ledger|...|SimulationManager|MacroSystem`. `AuthoredImpactScale` matches nothing, so this staged change reports as a plain sim change and **no review is REQUIRED by the tool**. This review exists only because the caller asked for it.
- Reasons to name it in:
  1. It sets the size of a figure written to `State.Budget` and `State.GovernmentDebt` (through `SimulationManager.cs:4574-4581`). It books by another name, the same class SimulationManager (s549) and MacroSystem (s575) were named in for.
  2. **Neither guard would catch a defect here.** The name guard misses the file. The baseline sentinel misses it too, because the no-policy family never resolves a cabinet or foreign option (Q5). A defect in this file would pass both.
  3. The file is small and pure, so a review costs little.
- Secondary: `CabinetSystem.cs:596` and `ForeignPolicySystem.cs:96` are the other two booking call sites, and neither matches the pattern. Naming them would pull a lot of non-money content (decision pools, text) into the ledger. My recommendation: name `AuthoredImpactScale` now, and leave the two systems to a ruling.
- Adding the name makes ReviewLedgerCheck require a `reviewed` row for the file's current state. This review, committed beside it, is that row's evidence.

### F3 (PRE-EXISTING, file as its own row; not caused by FT-14) - the Docket's cost line has its sign inverted
- `Assets/Scripts/UI/GameController.cs:9061-9068`: `costBillions > 0f ? "Cost of this option" (Bad ink) : "Saving from this option" (Good ink)`.
- The booked convention runs the other way. `ApplyOneTimeBudgetImpact` does `Budget += amount; GovernmentDebt -= amount` (`SimulationManager.cs:4577-4580`), so a **positive** BudgetImpact is money **in**. Authored examples:
  - "Launch the pilot" +180 (tax enforcement revenue, `CabinetSystem.cs:180`)
  - "Bank it against the debt" +200 (`:198`)
  - "Approve emergency hiring" -60 (a cost, `:193`)
  - "Fund the maintenance" -80 (`:211`)
  - "Send substantial aid" -60 (`ForeignPolicySystem.cs:50`)
- So the Docket prints every windfall as "Cost of this option" in red and every real cost as "Saving from this option" in green. The harness's staged film decision makes the same mistake: `UiScreenshotDriver.cs:1826` authors "Fund the visible-policing pilot" at `budgetImpact: +4f`, which would bank 4 billion if it were resolved. The inversion dates from P6-6 (bbe7a88), or from the harness figure it was filmed against.
- **Fix:** flip the test (`costBillions < 0f` is a cost), and print the cost as a magnitude. Correct the harness's figure to `-4f`. This needs a real film of the Docket.

### F4 (LOW, record it) - outputs outside the family DO move; "byte-identical" is a claim about the family only
- `SimulationTestRunner.cs:73, 193-201`: the matrix's `cabinetstress` scenario seats every USA minister and auto-resolves every decision with the worst-case option, over 100 and 500 turns. Its one-off shocks now keep a constant share of nominal GDP instead of fading as 1/PriceLevel, so its debt path changes. Option selection is unchanged, because it uses authored magnitudes. This is expected and correct, but re-run `-scenario=cabinetstress` if the bar owes a matrix.
- Film frames: `UiScreenshotDriver.cs:1343-1380` (dense staging after a warm-up) and `:2766+` (82c pin) put a rolled decision on the Docket after days have passed, so PriceLevel > 1 there. Any frame that shows a cost line will print a slightly larger figure. The P2-4.3 seed-staged frame (`:1815-1832`) is unchanged, because PriceLevel is 1 at the seed. Name the moved frames in the record rather than let FilmDiffCheck discover them.

### F5 (LOW) - stale "share of GDP" wording
- `Data/CabinetDecision.cs:30`, `Data/ForeignPolicyMeeting.cs:21` and `DomesticMoneyBasisDiagnostic.cs:145` still say "the same share of the deciding country's GDP". Say **nominal** GDP. These are comment-only lines and inherit the review under §584.

### F6 (LOW, pre-existing, not FT-14's) - the accumulator and the stock part at the clamp
- `SimulationManager.cs:4577-4580`: `Budget += amount` is unclamped but the debt write is clamped. At the ceiling or the net-creditor guard, the "same entry" claim in the doc is false. Nominal sizing makes one-offs slightly larger late in a run, so the clamp is marginally more reachable. No action for FT-14.

---

## The six questions

**(1) Is every caller nominal-consistent?** Yes.
- `CabinetSystem.cs:596` and `ForeignPolicySystem.cs:96` pass the sized figure to `ApplyOneTimeBudgetImpact`, which writes the nominal accumulator and the nominal stock, and clamps on `NominalGdp`.
- `SimulationManager.cs:1913-1917` (SWF drawdown): `requested = NominalGdp * pct/100`, clamped to `fund.TotalAssets`. The fund is nominal: it is fed from the nominal book, capped on `NominalGdp` (`:4786`), and seeded at PriceLevel 1 (`ScenarioLibrary.cs:124`). So `withdrawn` is nominal.
- `GameController.cs:9064` displays the same figure, in billions, with no percent-of-GDP beside it.
- The preview path (`PreviewTurn` / `ClonePreviewCountry`, `SimulationManager.cs:3049-3085`) never calls `ApplyOneTimeBudgetImpact`, the seam, or the drawdown effects.
- No AI chooses options (see Q5).
- The scenario library writes debt directly (`ScenarioLibrary.cs:118, 250`) as a ratio times `GDP`, at world creation where PriceLevel is 1, so real and nominal agree.
- `EventSystem.ApplyEvent` touches no budget or debt. The only other writers of debt and budget are the daily fiscal write (`:4979, :5030`) and those above.

**(2) Is AuthoredScaleGdp a seed-time figure where real equals nominal?** Yes. `WorldFactory.cs:86`: `UsaSeedGdp = 29000f` is the `gdp:` argument of the USA's `EconomyState` (`:130`). `PriceLevel` initialises to 1 (`EconomyState.cs:19`), and its only writer is `MacroSystem.cs:580`. At the seed, `NominalGdp = GDP * Math.Max(0.0001f, 1f)` is exactly GDP in float, so the USA identity stays byte-exact.

**(3) Is the figure compared with a REAL quantity afterwards?** No runtime reader does that.
- The debt ledger reads `NominalGdp` (`DebtLedgerRecorder.cs:59-60, 149, 192`).
- `DebtToGdpRatio` is nominal over nominal (`EconomyState.cs:369`).
- The Budget accumulator is displayed only as money (`GameController.Statistics.cs:75`).
- ApprovalEffect is independent of BudgetImpact.
- The diagnostic now divides by `NominalGdp` too (consistent, but see F1).

**(4) Save/load: does a saved field store the sized figure?** No. Pending cabinet decisions are saved as the whole `CabinetDecision` with its **authored** `BudgetImpact` (`SaveGame.cs:59-63, 93`; `SimulationManager.cs:2111-2119, 2637-2647`). The figure is sized at resolve time, so a pre-FT-14 save resolves under FT-14 with no mixing. `PriceLevel` is a public field of the JSON-serialised World. The debt ledger's history keeps observed past moves, which are facts rather than sizes. A pending foreign meeting is not saved at all; that is pre-existing and not a money issue.

**(5) Is "SAFE - byte-identical because no option fires in the no-policy century" true?** Yes. Decisions roll only for seated ministers (`CabinetSystem.cs:553-571`). The only runtime code that seats a minister is the player's UI (`GameController.cs:9453`); no AI, election or government-formation path does. With no ministers there are no RNG draws on that stream. Resolution comes only from `GameController.cs:9025/9100`, the harness, and editor checks. Meetings roll only through `AdvanceCountryDayTick` (`SimulationManager.cs:254`), which `TrajectoryBaselineDump` does not call (it uses AdvanceDay/AdvanceTurn with None). SWF drawdown bills are player-introduced. The claim is true, but it is also why the family proves nothing about FT-14 (F1).

**(6) Should AuthoredImpactScale.cs be in `$money`?** **Yes**, for the reasons under F2.


---

## What was done with each finding (the author)

- **F1 (no check tells the old rule from the new): FIXED.** `DomesticMoneyBasisDiagnostic` now raises a country's price level 2.5-fold and asserts the applied figure is authored × GDP × the price level ÷ the scale - proved RED with the old expression restored in place (`s587_red`) and GREEN on this change (the simulation bar).
- **F2 (name `AuthoredImpactScale.cs` into the money pattern): DONE** - `Tools/bar_tier.ps1`'s `$money` carries it; the file has no commit since the guard, so its current state takes one `reviewed` row, evidence this file. `CabinetSystem.cs` and `ForeignPolicySystem.cs` book through it and match nothing either - left to a ruling, as the reader did, and named in the record.
- **F3 (the Docket's cost line reads a positive impact - money in - as a cost, in red; pre-existing): FILED** as its own row, PF-14; it owes a real film of the Docket.
- **F4: NAMED** in the record - a stress run through the cabinet and any Docket frame after warm-up now carry nominal-sized figures.
- **F5: FIXED** - `CabinetDecision.cs` and `ForeignPolicyMeeting.cs` say nominal GDP.
- **F6 (at the debt clamp the budget accumulator and the debt stock part; pre-existing, not measured here): FILED** as FT-17 for measurement.
