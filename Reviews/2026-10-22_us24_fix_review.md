# Fix review: PS-6 US-24, the federal debt limit live (B7), family `us24dl` - the fix round

**Reviewed:** branch `wave/us24` (worktree `G:/UNITY/Projects/PoliSim-wt/us24`), `3e3e9e73..b64ffc5b`. It answers `Reviews/2026-10-22_us24_review.md`, which this review does not repeat. Read-only: nothing was run, built or committed. Every claim comes from reading the code. Out of scope, as the brief says: H1's registration of the diagnostic in a bar, and M1's family digests (both the integrator's).

**Files reviewed:**
`CLAUDE.md`, `Reviews/2026-10-22_us24_review.md`, `Assets/Scripts/Data/WorldFactory.cs`, `Assets/Scripts/Simulation/SimulationManager.cs` (the diff, plus `FiscalTurnReport`, `FiscalPeriod` and `ResetAccrual`, `GetOrSeedFiscalPeriod`, the report's build at the boundary, the daily loop in `AdvanceDay`, `AccrueDailyFiscalFlows`, `ApplyPeriodFiscalStepForValidation`, `AccrueDayForValidation`, `ApplyRevenueAndSpending`, the preview call, `ClonePreviewCountry`'s debt-limit and lapse lines, `AnswerDocket`, `SamePassage`, the pending state's save and restore of `FiscalPeriods`), `Assets/Scripts/Simulation/SimulationManager.DebtLimit.cs`, `Assets/Scripts/Simulation/UsDebtLimit.cs`, `Assets/Scripts/Simulation/AiFinanceMinistry.cs` (unchanged in the round; the US retirement re-read), `Assets/Scripts/Simulation/UsLapse.cs` (`OutlayToday`, `Lapsed`), `Assets/Scripts/UI/FiscalRules.cs` (`NationalNote`, `DebtLimitLines`), `Assets/Scripts/UI/GameController.BudgetV35.cs` (`DrawBudgetLastYearBook`, the spending tile), `Assets/Scripts/UI/GameController.DocketV35.cs`, `Assets/Scripts/UI/GameController.cs` (the pending cards, the stats sheet's `StatsSlips.Build` call), `Assets/Scripts/UI/StatsSlips.cs`, `Assets/Scripts/UI/UiFormat.cs` (`Money`, `BookUnit`), `Assets/Scripts/Persistence/SaveGameService.cs` (format 52, the serializer), `Assets/Scripts/Persistence/SaveGame.cs` (`LastFiscalReports`), `Assets/Scripts/Data/Country.cs` (the lapse and limit fields), `Assets/Scripts/Testing/UiScreenshotDriver.cs` (its reflection sites), `Assets/Editor/UsDebtLimitLiveDiagnostic.cs`, `Assets/Editor/UsDebtLimitCheck.cs` (the engine block's walk, `WriteBridge`'s inputs), `Assets/Editor/MetaTextCheck.cs`, `Assets/Editor/ConstantProvenanceCheck.cs`, `Assets/Editor/StatsSheetCheck.cs` and `Assets/Editor/StatsSlipReachabilityCheck.cs` (their `Build` calls), `Assets/Editor/AuditInvariants.cs` (the FUND evening's reads), `Assets/Editor/PensionParticipationProbe.cs` (its reflection over the report), `Tools/textcheck/TextCheck.csproj`, `docs/generated/US_DEBT_LIMIT.md` (the seed line, the headroom), `docs/ASKS_FOR_ELIAS.md`.

## Findings

### M1 - "ASKED" is written in the source, but no ask exists in the tree: the first review's M4 ask and L1's asks are still owed
- **Where:**
  - `UsDebtLimit.cs:90-92`: `RaiseUsd` is "[AUTHORED-DRAFT] - DECLARED ... ASKED".
  - `UsDebtLimit.cs:200-201`: `CutToday` reads "The book only ... (DECLARED - no sourced output effect of a binding limit is read; ASKED)".
  - `docs/ASKS_FOR_ELIAS.md:19-25` holds only the original R-US21 (i)-(vi) item. The round touches no document at all (its stat is 12 code files).
- **What is unasked:** none of the readings the first review asked to be put to Elias is on record:
  - the book-only cut (M4's first half);
  - the raise's size, $5.0 tn;
  - the government tables the raise automatically, with no withholding;
  - reconciliation as the vehicle;
  - a raise, never a suspension;
  - the new M3 reading (back pay held for every armed day);
  - whether a no-policy world signs a raise rather than waiting out the docket (M2's second half).
- **Why it matters:** CLAUDE.md's TRACKING class: "ASKED" in a comment is a claim about the state of the work, and here it is false. It is also §643's rule: a ruling a later session depends on is written to the repo.
- **Failure:** a later session reads "ASKED" and waits for an answer that was never requested. Meanwhile a model reading that moves about a third of federal outlays only on the book ships as if it were pending a ruling.
- **Fix:** add the asks to `docs/ASKS_FOR_ELIAS.md` in the same commit (one item, the readings listed above, each with its recommendation). Otherwise change "ASKED" to "to be asked" until they are written.

### L1 - The slip contradicts itself on every binding day
- **Where:** `FiscalRules.cs:127` and `:136`.
- **What it prints:** once the limit is reached, `DebtLimitLines` prints "DEBT SUBJECT TO THE LIMIT HELD AT IT BY EXTRAORDINARY MEASURES · $X BORROWED THROUGH THEM SINCE ...", keyed only on `DebtLimitReachedOn != MinValue` (`:127`). When the measures have run out (`Binding`), the next line prints "THE EXTRAORDINARY MEASURES RAN OUT ON ..." (`:136`).
- **Scenario:** planted state (e) is exactly that state. The slip reads "held at it by extraordinary measures" directly above "the extraordinary measures ran out". Also, during the binding period the excess keeps growing by the interest the cut never covers (`CutToday` never cuts interest, `UsDebtLimit.cs:206`), so "borrowed through them" misnames it. (e)'s check only asks for "CUT SO FAR", "IS BEFORE CONGRESS" and the limit's head, so nothing catches it.
- **Fix:** branch `:127` on `Binding`. For example, "DEBT SUBJECT TO THE LIMIT · $X ABOVE IT, BORROWED THROUGH THE MEASURES AND FOR INTEREST SINCE ...", or name the measures' part (to `BindsOn`) and the interest part apart. Add a `!Contains("HELD AT IT BY EXTRAORDINARY MEASURES")` clause to (e).

### L2 - The engine block's walk still reinstates with s401(b)'s headroom, so the measurement no longer models the game's rule
- **Where:** `UsDebtLimitCheck.cs:548-554`. `LimitA`, reading (ii)(a), is `dslYesterday + (double)rec.HeadroomUsd`, with the comment "plus the headroom s401(b) carried".
- **The mismatch:** L2 removed the headroom from the live rule (`UsDebtLimit.cs:156`). The generated document's (ii)(a) walk is now a twin of a rule that no longer exists.
- **Why it is low:** the dates cannot differ, because $25.0 m is against a deficit of about $5 bn a day.
- **Failure:** a later reader re-derives R-US21 (ii) from the document and reinstates the headroom.
- **Fix:** drop `+ rec.HeadroomUsd` and its comment from `LimitA`, and regenerate. Keep `reinstatement.headroom_usd` as the record's reproduction, which `:288-298` still checks legitimately.

### L3 - The simulation's division title now depends on the UI's money formatter
- **Where:** `UsDebtLimit.cs:220`: `BillTitle()` calls `PoliSim.UI.UiFormat.Money`. It is the only file under `Assets/Scripts/Simulation` that references `PoliSim.UI`. There is no asmdef, so it compiles.
- **Why it matters:** the title is FORK-2's identity key (`SimulationManager.cs:1883`), and it is stored in saved `DivisionRecord`s and docket entries.
- **Failure:** a later tier or decimals change in `UiFormat` (for example "US$5T" for "US$5.00T") changes the title. A saved held raise, or a docket entry restored from a save, then no longer matches a raise re-presented by the new build.
- **Fix:** a fixed literal in the simulation, for example `const string RaiseWords = "US$5.00T"` held to `UiFormat.Money` by the diagnostic, or the figure formatted locally with `CultureInfo.InvariantCulture`.

### L4 - Two of the new teeth are weaker than their messages
- **The title tooth** (`UsDebtLimitLiveDiagnostic.cs:288`): `!Regex.IsMatch(BillTitle(), @"\d{2,},\d{3}")` catches only the old full-digit form. A world figure through `UiFormat` (" to US$41.10T") passes. The (d) clause `senate.Title == UsDebtLimit.BillTitle()` (`:208`) is a tautology.
- **The back-pay tooth:** (e)'s M3 clause (`:231`) proves only "stays owed" on the armed day, then zeroes the debt (`:233`). Payment after a raise is shown only on a bare country (`:286-288`), never through the day path, where `Raise` disarming and the next `AccrueDailyFiscalFlows` must together pay it.
- **Fix:**
  - Assert the title's exact string, or that it is equal across the Sweden-start USA and the US game, whose limits differ.
  - In (e), leave the back pay standing through the docket-signed or voted raise and check that `LapseDeferredOutlays == 0` and the day's debt moves by it on the first unarmed day.

## Checked and clean

- **`WorldFactory` (H1), inert:**
  - Old form: `governmentDebt: 29000f * 1.24f`, a literal constant expression folded by Roslyn to one `ldc.r4`.
  - New form: `public const float UsaSeedDebt = UsaSeedGdp * 1.24f` (`WorldFactory.cs:91`), where `UsaSeedGdp` is `public const float 29000f` (`:86`). The operands are identical constants, so the fold is identical, and the IL at `:136` is the same `ldc.r4`.
  - Neither form was ever evaluated at run time or through a `static readonly`.
  - `SeedDebtUsd` moved from `const` to `static readonly`. Its value is `(double)35960f * 1e9 = 3.596e13`, exact in double and equal to the old literal. It is declared before `BridgeRatio` (`UsDebtLimit.cs:46`, `:49`), so the initialisation order holds and `BridgeRatio` is bit-identical.
  - No const context uses `SeedDebtUsd`.
- **`ApplyRevenueAndSpending`:**
  - `debtLimitCut`'s store and `totalSpending`'s expression are unchanged.
  - The new `_debtLimitCutToday = debtLimitCut` (`SimulationManager.cs:8887`) reads an already-stored local into a new field. It writes no existing local, out or field.
  - It sits between the `out totalSpending` store and its re-read for `budgetBalance`. Because `stind.r4` rounds the value before any reload, the re-read value is the stored float either way.
  - The accrual `+= _debtLimitCutToday` (`:8721`) stores only to the new field.
  - The preview (`:7001`) and validation (`:8772`) calls also write the field. Nothing reads it but the accrual directly after its own call, so it cannot leak across countries or worlds.
  - Byte-identity of turns 1-2 is still proved by the dump, never by reading (CLAUDE.md, the IL-shape case). The integrator should read the per-country diff for every country, not only the USA's.
- **`UsLapse` (M3):**
  - The no-back-pay path returns at `:104` before the new line (`:107`). The path with back pay owed and nothing armed is byte-for-byte the old one.
  - Outside the USA, `LapseDeferredOutlays` is always 0.
  - `today` is the same `CurrentDate` that `CutToday` compares (`:8640`, `:8885`).
  - The back pay is paid when the cut disarms: `Raise` clears `DebtLimitCutFlowDay` (`UsDebtLimit.cs:187`), both for a vote in the tick and for a docket answer between ticks, and the tick computes `flows` as `MinValue` once `DebtLimitReachedOn` is cleared (`SimulationManager.DebtLimit.cs:46`).
  - Across saves and forks: `LapseDeferredOutlays` (format 42) and `DebtLimitCutFlowDay` (format 52) are saved, and the preview clone carries both (`SimulationManager.cs:7335`, `:7345`). The preview never calls `OutlayToday`.
- **M4 and the report:**
  - `FiscalTurnReport.DebtLimitCut` is a separate line. `TotalSpending` stays net (`:6594`), so nothing double counts. `DerivedStats`, the spending tile and the fiscal vote read the net total as before.
  - The Budget book's rows now add up: the components less the cut row, with `MoneyDelta(-cut)` printed only where the cut is non-zero (`GameController.BudgetV35.cs:868`). That is the same pattern as the electricity-tax row, with its own slip id.
  - `ResetAccrual` zeroes the new field (`:1037`).
  - `FiscalPeriods` and `LastFiscalReports` are saved whole through Newtonsoft with public fields, so the new fields ride format 52's step. A pre-field v52 save defaults to 0.
  - No dump, digest or hand-list enumerates `FiscalTurnReport` or `FiscalPeriod`. Only `PensionParticipationProbe`'s diagnostic diff reflects over the report, and it prints movers only. `PreviewParityDiagnostic` is untouched, because no `Country` field was added.
- **M2:**
  - The title is world-independent.
  - Two raises cannot collide: there is one slot per country (`_pendingDebtLimitBillByCountry`), and a fallen raise is re-tabled the next day and voted a bill's course later, so `SamePassage`'s date always parts them.
  - The `"Reconciliation ("` prefix that `IsStatuteActTitle` excludes is unchanged.
  - With the headroom gone, a 2024 start reaches on 2 January in every world.
- **L5:**
  - The pending card is gated by `usActs` (`GameController.cs:9097`). The Docket item reads a slot that only the player's USA fills (`AdvanceDebtLimitDay` requires `UsBudgetProcedure.Applies`).
  - Both use `UiFormat`, `DeskDay` and invariant upper-casing, carry no developer text, and have `action: null`, so they are reads only, as US-23's majority item is.
  - `ConcernOf` is shared by the vote and the card.
- **L6:**
  - The one game caller passes `CurrentDate` (`GameController.cs:7130`).
  - `StatsSheetCheck` and `StatsSlipReachabilityCheck` pass no day on Sweden. For a non-US country (or before `IsRead`) the live note falls back to the fixed one (`FiscalRules.cs:91`), so their books and anchor keys are unchanged.
- **L2 and L4 as built:**
  - The headroom constant and every live read of it are gone.
  - `MetaTextCheck`'s roots now include `UsDebtLimit.cs`. Its non-log literals ("Reconciliation (the debt limit): ...", `LawWords`, the `Raise` reason) hit no banned pattern.
- **Diagnostic:**
  - The new (a) tooth reads the document's own printed seed (`US_DEBT_LIMIT.md:109`, and the regex matches it). It fails on a re-seed without `WriteBridge`, and on a NaN when the line is not found.
  - (c)'s walk now uses the document's ratio, independent of the code's seed.
  - The (e) M3 and M4 clauses fail if back pay is consumed or the line is not booked. The reflection's NaN fails closed.
  - Everything stays inside the existing `EpochScope` (`:117`), with the RNG captured and restored.
- **Static guards:**
  - `ConstantProvenanceCheck` scans `Assets/Scripts/Simulation` only. `SeedDebtUsd` is marked DERIVED and `RaiseUsd` is [AUTHORED-DRAFT]. `WorldFactory` lies outside the scan.
  - `DeadStateCheck`: each new field is written and read.
  - `UnwiredSubsystemCheck`: `ConcernOf` has two callers.
  - `NumberLocaleCheck`: every new UI figure goes through `UiFormat`.
  - `CommentClaimCheck`: there are no line-number or count claims in the new comments.
  - `Tools/textcheck` compiles no simulation file, so the UI reference does not break it.
- **`AiFinanceMinistry`** is unchanged by the round. The US path still writes nothing (`:146`).

## Verdict

**READY WITH FIXES.** H1 (the seed is now read, not copied), M2, M3 and M4's arithmetic are sound, and the play path's shape is inert by reading. The proof is still the `us24dl` dump, which is the integrator's.

Before merge:
- **M1:** write the asks that the comments already call "ASKED". It is documentation-only, and it discharges the first review's M4 ask and L1.

Ride the same pass or file:
- **L1:** the slip's binding-day contradiction, which the player sees.
- **L2:** the engine walk's headroom.
- **L3:** the title's UI dependency.
- **L4:** the teeth.
- The first review's L7 (the plan's stale row) is still open; the round did not claim it.
