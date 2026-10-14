# Review - §826, the gameplay audit's Phase 4: GA-2-11, the household burden's seed side reads the statute as seeded (2026-10-14)

One adversarial reviewer, a single read-only agent, read the change as a patch prepared outside the tree while GA-2-11's control ran (`Assets/Scripts/Data/TaxSchedule.cs`: `TaxSchedule.YieldNowAtStatute`, `TaxSchedule.YieldRatioAtStatute`; `Assets/Scripts/Data/TaxBases.cs`: `TaxBases.RevenueAtRate`'s `atStatute`; `Assets/Scripts/Simulation/MacroSystem.cs`: the household burden's seed side; `Assets/Scripts/Data/Country.cs`: `Country.ScheduleMemoStatute`), against the files in the tree, every reader of the changed members, the clone and save paths, and the checks. It was told the constraints: the seed side bit-identical wherever the line moves no sub-row (the no-policy sentinel), no constant moved, the comments' claims real. Its report follows verbatim; then what was done with each finding.

## The report

**Verdict: APPLY.** The change is correct, and the seed side is bit-identical wherever the line moves no sub-row. It breaks no determinism, no clone and no check. There are 2 medium findings to record (one missing bar; one pre-existing restore defect) and 4 low ones (wording and residues).

Scope: the supplied diff equals the four patched copies exactly (45 changed lines). The tree's four originals equal HEAD 6492c537. The copies keep CRLF and have no BOM, like the originals. Patched line numbers below are marked (p); all other line numbers are the tree's.

### Findings

**1. MEDIUM: no permanent bar for the new behaviour or for constraint 1's all -1 case.**
- `HouseholdBurdenAnchorDiagnostic.cs:28-52` checks five things: the seed, the lever +5 and -5, a new tax, and an unemployment shock. None of them sets `BracketRates`.
- The X-02S runs (`Tools/audit/p4_ga2_11.plan`) are one-off audit runs. So "a sub-row change moves the gap" and "a -1 array leaves it to the bit" rest on reading the code plus one audit pass.
- *Remedy:* add two sections to that diagnostic.
  - (5) Sweden with the flat layer and the state band at 0 reads a large negative gap. Poland's 32 % band at 37 reads a positive gap equal to `RateSeed/100 x Base x (YieldRatio(moved) - 1) / NominalGdp`.
  - (6) An empty array and an all -1 array each read the untouched gap with `==`, not with a tolerance.

**2. MEDIUM (pre-existing F4-4 defect, record as its own row): a moved sub-row cannot be restored, so the new gap can never return to zero.**
- A dial that lands within 0.005 of the statute's figure is dropped from the draft (`GameController.cs:9658`), and the bill then carries -1 at that index (`:9465`).
- `MergedBracketRates` keeps the standing figure wherever the request is -1 (`ParliamentSystem.cs:175`). So after a bill zeroing Sweden's flat layer, the Budget page cannot put back 32.38: the dial snaps back to the standing 0. The nearest figure it can write (the statute's ±0.005 or more) stays in the array as a number.
- So, to the question asked: a restore writes neither -1 nor the figure. It is a no-op.
- `OverridesKey` never returns to "" once any sub-row has been set. An all -1 array also keys as "-1;-1;...;", not "". With two separate memos this is harmless for the values, and no memo thrashes.
- Before this change the defect was invisible to consumption. Now the leftover sub-row reaches consumption.
- *Remedy:* a ledger row, not in this commit. Give the bill a way to say "back to the statute", for example a requested figure equal to the statute's overwriting the standing one with -1.

**3. LOW: Germany's path switch leaves a small residue in the gap.**
- `Tax` keeps § 32a verbatim only while no entry is ≥ 0 (`TaxSchedule.cs:277-279`, `HasActiveOverride` `:286-291`).
- The new seed side always reads § 32a (null overrides). The live side switches to the ramps (`BandTax`) as soon as any entry carries a figure, even the statute's own value.
- So the gap includes `BandTax(statute) - FormulaTax(statute)`: § 32a's floor to a whole euro, plus about 0.06 EUR at the 69 878 EUR zone join. That is of order 1e-5 of the yield, or about 1e-6 of GDP. A German bill restating a sub-row reads a tiny nonzero gap.
- Sweden has a smaller version of the same thing: a stored 32.38f reads 32.380001 against the statute's 32.38.
- *Remedy:* no code change. Name the residue in `YieldNowAtStatute`'s doc: "for Germany the live side reads the ramps once a sub-row carries a figure; the seed side reads § 32a; the difference, about a euro per income, is in the gap".

**4. LOW: previewing a sub-row draft at a boundary-fresh state mixes two years' pyramids.**
- The memo's key is (boundary level, boundary wage, overrides). It does not cover the cohort pyramid (`TaxSchedule.cs:523-530`).
- `PreviewTurnWithBudgetDraft` applies the draft first (`SimulationManager.cs:5219`). `ApplyBillResult`'s hike then reads `EffectiveRate` before and after the merge (`ParliamentSystem.cs:502-504`), which fills the clone's live memo on this year's pyramid.
- The clone commits next year's pyramid only at `:5330`. At day 0, price and real wage still equal their last index; both move only in the daily path (`MacroSystem.cs:581`, `:1242`). So `IndexSpendingLines` (`:6363`, `:6372`) leaves the key unchanged.
- Result: the revenue (`:5384`) and the burden's now side (`:5408`) read the cached pre-commit yield, while the new statute memo fills after the commit. The preview's gap then carries one year of demographic drift in the AER, which is small next to the sub-row effect.
- The real turn is clean: `CommitYear` (`:4826`) runs before any read, and the key moves at `:6275`.
- This is pre-existing: the old seed side mixed the same way whenever the lever sat outside the seed's bucket.
- *Remedy:* record it. The root fix is to key the memo on the pyramid, for example on the `Counts` array reference that `CohortDemographics.cs:200` replaces. That would also change the boundary-window reads and move the sentinel, so it is a separate item.

**5. LOW: doc wording.**
- (a) `TaxSchedule.cs:559` (p) says "a Swedish bill zeroing the flat layer took the revenue to nothing". The ledger row says the flat layer *and the state band*; the flat layer alone leaves the state band's yield.
- (b) "(both sides read the live line's rates" (`:558` (p), `MacroSystem.cs:165` (p)) reads as present tense. Say "both sides used to read".
- (c) "where the line carries no rates of its own" (`:560` (p)): say "no array, an empty one, or every entry -1". The -1 array is the form a bill actually leaves on the line.
- (d) The `Memo` summary (`TaxSchedule.cs:521-522`) still says "The per-country memo"; there are now two per country. Its claim that "the AI ministry's search reads it at many lever positions" is stale and pre-existing: `AiFinanceMinistry.cs:240` and `FinancePartner.cs:266-279` read only `TaxBases.Base`.
- (e) `HouseholdBurdenGap`'s summary (`MacroSystem.cs:182-184` (p)), "moves with a RATE, and with a tax the country did not levy at the seed": add "and with the income tax's own sub-row rates (GA-2-11)".
- (f) Optional: the third clause of `docs/audit/inventory/B_money.md:385` D-32 ("a band-only rise is invisible") becomes history once this lands. The ledger row is the record.

**6. LOW (hygiene): YieldNow's guard now runs after its arguments.**
- `YieldNow` now evaluates `line.BracketRates`, `OverridesKey` and the memo's creation as arguments, before `Yield`'s `Responds` guard (`TaxSchedule.cs:553-554` (p)).
- So a non-responding country gets a memo object, and a null line now throws where the old method returned 0.
- This is unreachable today: every caller checks `Responds` and passes a real line (`TaxSchedule.cs:587`, `:606`; `PolicyWebRenderer.cs:1340-1344`; `TaxBases.cs:145-151`).
- *Remedy:* none needed. Optionally, check `Responds` in the wrappers.

### Verified OK

- **Constraint 1, the values.** `AverageEffectiveRate(..., null)` gives the same double as with an empty array, an all -1 array, or NaN entries:
  - `RowRate` takes the statute's figure for a negative or missing entry (`TaxSchedule.cs:218-222`).
  - `HasActiveOverride` is false for such arrays (`:286-291`), so `Tax` takes the same branch over the same doubles.
  - The seed-side shift is exactly 0f: `SeedIncomeLeverLevel` sets `Rate`, `RateSeed` and `BaselineTaxRates` to one float (`WorldFactory.cs:1149-1154`), and `CaptureStructuralBases` sets `RateSeed = Rate` again (`Country.cs:270`).
  - The float chain in `RevenueAtRate` is unchanged (`TaxBases.cs:153` (p)).
- **Constraint 1, the memo.**
  - Within one key period, nothing else the integral reads moves: the pyramid (only `CommitYear` writes `Counts`, `CohortDemographics.cs:200`), `RateSeed`, the scales, and the credit (`EarnedIncomeCredit.Live` is a const, `EarnedIncomeCredit.cs:45`).
  - The real boundary commits the pyramid (`SimulationManager.cs:4826`) before the index moves the key (`:6275`, then `:6363`/`:6372`).
  - The burden is read only daily (`MacroSystem.cs:620`, `:645`/`:660`; `SimulationManager.cs:444`), and in the preview after the clone's commit.
  - The statute memo ever holds one rate per period, the baseline's. So the 0.01-bucket aliasing (the key is `round(rate x 100)`) cannot reach it, and the 512 cap never binds. The one-entry saving in the shared memo has no reader that reaches 512.
  - The only way bits differ in a run without sub-rows is where the old seed side aliased onto another rate in the baseline's bucket. There the new figure is the correct one. AI and partner moves are raises of at least 0.01 points (`AiFinanceMinistry.cs:59`, `:247`; `FinancePartner.cs:42`, `:276`), so the no-policy dumps cannot produce that case.
  - The bars that confirm this: `TrajectorySentinelCheck`, and the plan's byte-identical area-2 and area-7 twins.
- **Side-effect order.** `RevenueAtRate` still calls `RateSeedOf`, then `Base`, then the yield. The lazy `IncomeTaxSeedAer` capture is the same code, now inside `Yield`. Both burden callers evaluate the now side first, left to right (`MacroSystem.cs:183`, `:190-191`).
- **(b) Other readers are unchanged.**
  - `YieldNow`: `EffectiveRate`, `YieldRatio`, `PolicyWebRenderer.cs:1344`.
  - `YieldRatio`: `RevenueAtRate`'s default path; `TaxScheduleDiagnostic` `:80`, `:104`, `:182`, `:186`, `:273`.
  - `RevenueAtRate`: `TaxBases.cs:117`, `CollectionEfficiencyBasisDiagnostic.cs:90`, `TaxScheduleDiagnostic.cs:200`, all positional with `atStatute` defaulting to false.
  - Nothing under Assets/ or Tools/ uses `RevenueAtRate` as a method group, delegate, `nameof` or reflection target.
  - `ScheduleMemo` is referenced only at `Country.cs:137` and in TaxSchedule.
- **(c) Cloning and serialization.**
  - `ClonePreviewCountry` builds a new Country from a hand-list (`SimulationManager.cs:5608-5776`), so both memos are null on every preview and estimate clone.
  - The shadow fork and the impact ledger's except-worlds are a JSON round trip (`ShadowBaseline.cs:96-104`). Json.NET 3.2.1 honours `[NonSerialized]`: a real autosave carries `IncomeTaxSeedAer` and `BracketRates` but no `ScheduleMemo`.
  - The clone audit skips `IsNotSerialized` fields (`PreviewParityDiagnostic.cs:289`, `:298`), so the new field needs no exemption. The other reflection walkers read float fields only.
  - No memo is shared anywhere.
- **(d) No other term has the same blindness.**
  - The Gini term (`MacroSystem.cs:1166`) and the labour term (`:853`) compare the live `EffectiveRate` against stored scalars (`BaselineIncomeTaxRate`, `LaborTaxRateSeed`), so sub-rows already reach them.
  - The bill hike (`ParliamentSystem.cs:501-505`) and `BracketRateMove` (`:181-189`) difference the line before and after its own merge.
  - `MacroSystem.cs:159` is the only seed side built through the revenue accessor.
- **(e) No Editor check depends on the old behaviour.** This covers `HouseholdBurdenAnchorDiagnostic`, `TaxScheduleDiagnostic`, `TaxTransmissionDiagnostic` (its own rate x base), `PreviewParityDiagnostic`, `FinancePartnerDiagnostic` (its -1 bill is only introduced) and `DeadStateCheck` (`Yield` and `RatioOf` both have callers). The uncommitted AuditDriver BURDEN readout reads the live country between days and is inert by the memo argument above.
- **(a) Semantics.** The seed's position is the `BaselineTaxRates` lever with no sub-rows (no bill has run at the seed), read at today's `IncomeScale` and `ThresholdScale` on both sides. So bracket creep cancels, the lever and the sub-rows enter the gap together, and the credit's municipal rate follows the statute. This is the right comparison point while `TaxSchedule.Of` is static. An income-tax statute that changed by law, as EN-4e's carbon statute does, would need "as in force" rather than "as seeded".
- **Constraints 2 and 3.** No constant was added or changed. The only literal added is "". The three backticked claims (`TaxSchedule.YieldNowAtStatute`, `Country.ScheduleMemoStatute`, `TaxSchedule.YieldRatioAtStatute`) name members the patch declares, and CommentClaimCheck resolves them by reflection.

## What was done

- **1 (MEDIUM): done, under R-N5.** The burden's seed side following the live state has now cost twice: FT-3's new tax cancelled itself in the gap (§377), then the income tax's own sub-rows did. So `HouseholdBurdenAnchorDiagnostic`, already in the simulation bar, gains two sections:
  - (5) Sweden's flat layer and state band at 0 must open a gap below -0.05, and Poland's top band five points up a gap above 1e-4.
  - (6) A line with no rates of its own, in each of its three forms, must read the gap to the bit (`==`) in every country whose income tax responds.

  On HEAD's burden code, with only `Country.ScheduleMemoStatute` added so the check compiles, section (5) fails on both cases (`n826pre`: Sweden 0, Poland 0). Section (6) and the four older sections pass. On the fix, every section passes (`n826`: Sweden -0.10389, Poland +0.00033). The older sections read the same figures before and after.
- **2 (MEDIUM): filed as GA-2-12** (S3, a clear bug, for Elias with the next question). A dial dragged back to the statute's figure drops its draft, and the merge keeps the standing figure, so a sub-row a bill has set cannot be put back. Not in this commit.
- **3 (LOW): written down.** `YieldNowAtStatute`'s doc names Germany's residue: once a sub-row carries a figure, the live side reads the ramps while the seed side reads § 32a's formula.
- **4 (LOW): filed as GA-2-13** (S4). Reading the key turned up a second facet: `OverridesKey` prints the rates to hundredths, so two sub-row figures within 0.005 of each other share an entry. The row asks for the real turn's window to be measured first, because keying on the pyramid may move the sentinel.
- **5 (LOW): fixed.**
  - (a) The doc names the flat layer and the state band.
  - (b) Both comments say "used to read".
  - (c) The three no-rates forms are named.
  - (d) The `Memo` summary now says there are two per country, and its stale claim is corrected. `AiFinanceMinistry.cs:240` and `FinancePartner.cs:278-279` read `TaxBases.Base` alone, as read for this pass.
  - (e) `HouseholdBurdenGap`'s summary names the sub-rows.
  - (f) The inventory's D-32 marks its third move fixed. Its second move (a removed household tax gives no relief, because both sides skip a line not levied today) is named under architect question 34, whose ruling on which lines the seed levied it shares.
- **6 (LOW): fixed.** `YieldNow` and `YieldNowAtStatute` check `Responds` before building any argument, as `YieldNow` did before.
