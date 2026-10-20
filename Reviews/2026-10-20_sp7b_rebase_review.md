# SP-7b rebase review: 71c14a10 on main 564296b8

**Reviewer:** independent adversarial pass, read-only (no code edited, nothing committed, Unity not run).
**Scope:** what the rebase changed beyond the pre-rebase branch (45381a46, its review fixes at 4ff8175d), meaning the integration with main's US-16 (§883) and US-21c/US-22c (§884).
**Method:** `git diff 564296b8 71c14a10 -- '*.cs'`. I compared that per file against `git diff d8bbe5b8 4ff8175d` (d8bbe5b8 is the merge base). The SP-7b patch changed in 10 files; the other 17 carried over byte-identical. I then read the code at 71c14a10 in the `PoliSim-wt/sp7b` worktree, which is clean at that commit.

## Verdict

**No High or Medium findings.** The integration is sound on all five points I was asked to check:

- **Item 1 (a Polish answer acts at once): correct.** A held Polish budget applies exactly once. The guard cannot skip a Polish bill in the normal tick. The answers-only run neither re-runs a countdown nor re-votes anything. No RNG is drawn on that path.
- **Item 2 (the 7/21-day terms): no off-by-one.** US entries are unchanged.
- **Item 3 (the veto-gate overloads): every reflection lookup still resolves unambiguously.**
- **Item 4 (save format 50): complete.** The format step and the clone hand-list are both in place, and the `PassageYear` merge is right.
- **Item 5 (static guards): no claim, constant or government-field write fails.**

There are five Low findings and three carried notes. Most of the Lows are comments or a branch that the "acts at once" change made stale or dead.

## Files reviewed

- `Assets/Scripts/Simulation/SimulationManager.cs` (the veto gates, `UsVetoGate` both forms, `ActOnDocketAnswers`, `SkipsForAnswers`, `HasDocketAnswer`, `DocketLastDay`/`DocketLawDay`, `AnswerDocket`, `DocketEntries`, `AdvanceBudgetBillDay`, `ResolveGovernmentBudget`, `PolishStatuteActs`, the nine bill sites' guards, `PassageYear`, `ClonePreviewCountry`, the role-gate edits)
- `Assets/Scripts/Simulation/SimulationManager.PlayerPresident.cs` (whole file)
- `Assets/Scripts/Elections/PresidentialDocket.cs`, `Assets/Scripts/Elections/PresidentialVeto.cs`, `Assets/Scripts/Elections/PresidentialElection.cs`
- `Assets/Scripts/Data/DocketEntry.cs`, `Assets/Scripts/Data/Country.cs`
- `Assets/Scripts/Persistence/SaveGameService.cs`
- `Assets/Scripts/Simulation/CabinetSystem.cs`, `Assets/Scripts/Simulation/FinancePartner.cs`, `Assets/Scripts/Simulation/ParliamentSystem.cs` (RNG use), `Assets/Scripts/Simulation/ApprovalLedgerRecorder.cs`, `Assets/Scripts/Simulation/UsBudgetProcedure.cs`, `Assets/Scripts/Simulation/Congress.cs` (`Applies`)
- `Assets/Scripts/UI/GameController.cs` (signing queue, calendar, `DeskOf`/`DeskLawDay`), `Assets/Scripts/UI/GameController.PresidentialStart.cs`, `Assets/Scripts/UI/GameController.Desk.cs`, `Assets/Scripts/UI/GameController.DocketV35.cs`, `Assets/Scripts/UI/GameController.ParliamentRows.cs`
- `Assets/Scripts/Testing/UiScreenshotDriver.cs` (the reflection sites and the SP-7b shots)
- `Assets/Editor/PolishPresidentialStartDiagnostic.cs`, `Assets/Editor/UsDocketDiagnostic.cs`, `Assets/Editor/UsVetoDiagnostic.cs`, `Assets/Editor/CabinetPressureDiagnostic.cs`, `Assets/Editor/PresidentialVetoDiagnostic.cs`, `Assets/Editor/WorldClockDiagnostic.cs`, `Assets/Editor/CommentClaimCheck.cs`, `Assets/Editor/ConstantProvenanceCheck.cs`

## Item-by-item verification

### 1. A Polish answer acts at once (`AnswerPolishDocket` then `ActOnDocketAnswers`)

**Does it reach the Polish slots?** Yes. Ordinary and amendment statutes ride `bill.Docket` in the eight non-budget sites, and each site's `SkipsForAnswers(bill.Docket)` guard comes before its `DaysRemaining--` (SimulationManager.cs:2331, 2401, 2460, 2558, 2637, 2945, 3024, 3125). Held budget acts ride `BudgetBill.ActsHeld`. `HasDocketAnswer` reads `ActsHeld` (SimulationManager.cs:1722-1727), so `AdvanceBudgetBillDay` passes its guard (1897), skips the decrement (`ActsHeld != null`, 1898), finds `DaysRemaining <= 0` (a held bill only exists after its countdown resolved), and dispatches to `FinishPolishBudget` (1906).

**Can a held budget apply twice or never?**
- **Twice: no.** `FinishPolishBudget` (PlayerPresident.cs:232-262) sends every held entry through `PolishDocketGate`. An answered entry always returns non-null (the `docket.Answer != 0` branch). Only the null ones are kept in `still`. When `still` is empty, the method clears `ActsHeld`, removes the slot (`_pendingBudgetBillByCountry.Remove`) and applies once. The next tick finds no slot. Two answers given on the same day each decide their own act, and only the last one applies the bill.
- **Never: no.** Unanswered acts are signed once their term has run. If the player has left office, the AI president answers by `PresidentialVetoDrawn` (PlayerPresident.cs:111-115). The budget act takes no veto (`MayVeto`), so it is always signed.
- **The mid-day case:** the day step for `CurrentDate` has already run, so an act whose term expired today was already signed by that step. The answers-only run sees the same `CurrentDate` and the same `PlayerHoldsPresidency`, so it cannot decide differently.

**Can the guard skip a Polish bill in the normal tick?** No. `_docketAnswersOnly` is set only inside `ActOnDocketAnswers` and is reset in a `finally` (SimulationManager.cs:1735-1750). The day loop never calls it.

**Does it re-run the Sejm's own countdowns or votes?** No. Only a bill whose docket holds an answer gets past the guard. That bill's site skips `Divide` and `PresidentialVetoGate` because `bill.Docket != null`, and goes straight to `UsVetoGate`, which hands off to `PolishDocketGate`. `FinishPolishBudget` votes nothing. The statute-act divisions were recorded at `HoldPolishBudget`, on the adoption day.

**RNG:**
- The player's veto draws nothing (`VetoedByThePlayer`).
- The AI fallback, `PresidentialVeto.Draw(MasterSeed, id, date, title)`, is a pure hash, not a stream draw.
- `ParliamentSystem.RandomSource` is declared but unused (ParliamentSystem.cs:51), and the `Apply*Result` methods draw nothing.

So the played world stays deterministic for a given answer timing. A no-player world never reaches `AnswerDocket`. The normal-tick Polish path is behaviour-identical to main:
- `PresidentialVetoGate` falls through to `PresidentialVetoDrawn` (main's body, verbatim) when the player is not President.
- `PolishDocketGate` with `docket == null` and no player-president returns `passed` and holds nothing, as main's `UsVetoGate` returned it for Poland.
- `RecordPresidentialVeto` has a new `Side > 0` reason branch for the backing party. An AI draw can never take it: a veto needs `AtRisk`, and `AtRisk` means the backing party did not vote for the statute.

### 2. `DocketLastDay`/`DocketLawDay` by `DocketEntry.Bill`

- **Ordinary bill:** presented 10 Aug, `LastDay` counts 11 Aug to 31 Aug (21 days, no Sunday exception for Poland), so the last day is 31 Aug and `LawDay` is 1 Sep.
- **Budget act:** presented 19 Aug, last day 26 Aug, law day 27 Aug.
- **The gate:** `PolishDocketGate` holds while `CurrentDate.Date <= DocketLastDay` (PlayerPresident.cs:116) and signs from the day after. That is the same boundary as main's US deadline form (`<=` to hold) and as `Expired`'s `today > last`.
- **The UI:** every surface now reads `DocketLawDay` (GameController.cs:3456, 9568; GameController.PresidentialStart.cs:70, 104-105). The pre-rebase direct `PresidentialDocket.LawDay(.., Bill)` calls in GameController.cs were correctly dropped in favour of main's helper.
- **US entries:** they never set `Bill`, so it stays 0, `Ordinary`, and the Bill overload falls through to the base period. A US `Deadline` takes precedence in both helpers, and the US expiry still calls the 3-arg `Expired`, which is equivalent when `Bill` is 0.

### 3. The `UsVetoGate` and `PresidentialVetoGate` overloads

- **The Poland hand-off** is the first line of the 7-parameter form (SimulationManager.cs:1647), before main's `docket != null` re-read. Every caller reaches it, including the 6-parameter forwarding form.
- **`UsVetoGate`:** the 6-parameter form keeps `(Country, DivisionRecord, BillConcern, bool, ref DocketEntry, bool)` (1640). UiScreenshotDriver.cs:2002 and UsVetoDiagnostic.cs:76 look it up by that exact type array, so they resolve it unambiguously; the 7-parameter form differs in arity. The source calls with 5 arguments bind to the 6-parameter form through its optional parameter.
- **`PresidentialVetoGate`:** there is only one method by that name (1580). The split-out body is named `PresidentialVetoDrawn`. So the name-only lookups at UiScreenshotDriver.cs:1937, CabinetPressureDiagnostic.cs:91 and PresidentialVetoDiagnostic.cs:321 cannot throw `AmbiguousMatchException`. None of those worlds sets `PlayerHoldsPresidency`, so they still draw.

### 4. Save format 50

- **Refusal:** `CurrentSaveVersion = 50`, and the loader refuses anything that is not equal (SaveGameService.cs:219, 405), so a v49 save is refused.
- **Saved fields:** `Country.PlayerCandidate`/`PlayerStart`/`PlayerHoldsPresidency`, `Contest.PlayerCandidate` and `DocketEntry.Bill` are all public, non-`[NonSerialized]` fields on objects serialized whole.
- **Clone hand-list:** `ClonePreviewCountry` keeps main's `UsCongressElections` line and SP-7b's three lines together (SimulationManager.cs:6918-6925).
- **`PassageYear` merge (6413):** Poland reads `desk.PresentedOn.Year`, which matches `FinishPolishBudget`'s `ActsHeld[0].PresentedOn.Year`. Every held act shares the adoption day, so `ActsHeld[0]` gives the right year even after the budget act leaves the list. Every other country keeps main's `DocketLawDay(countryId, desk).Year`, `Deadline` included.
- **No pinned version literal** (`== 49`) remains in any diagnostic.

### 5. Static guards

- **CommentClaimCheck:** all 27 distinct backticked `Type.Member` claims on added lines resolve to a declared type and member, including `PresidentialDocket.Bill` (a nested enum) and private `SimulationManager.NotePlayerPresidency`/`PolishDocketGate`.
- **ConstantProvenanceCheck:** this check only scans numeric constants under `Assets/Scripts/Simulation`, and no new one was added there. `PolishShortTermDays` (in Elections) is tagged SOURCED anyway.
- **Government-field writes:** no added line writes `Cabinet`, `Support` or `PmParty`.

## Findings (all Low)

### L1 - Main's L2 contract is now live and unmet: override divisions from an answer are not queued at once
**Where:** SimulationManager.cs:1731-1734 (the comment), GameController.PresidentialStart.cs:126-132 (`AnswerDocketItem`) and 184-197 (`AnswerSign`/`AnswerVeto`).

**What happens:** main's `ActOnDocketAnswers` doc says "nothing calls the verbs in play yet; when US-23 wires them, its caller queues the divisions right after the answer". SP-7b now wires the verbs into play for Poland, but neither caller runs the signing queue after a successful answer.
- A veto given on the plate, or on the Docket page while the clock is paused, appends the override division ("Vetoed by the President (...): ... the veto stands") dated today.
- Its ceremony only appears after the next play day tick (`QueueNewlyResolvedDivisions`, GameController.cs:781). If the player stays paused, the result of his own veto is not shown at all.
- The wall-clock timing matches the pre-rebase build, where the next tick acted. The new behaviour is only that the record is dated the answer's day while the ceremony lags. So this is the stated contract going unmet, not a new regression.
- The comment "nothing calls the verbs in play yet" is now false. CommentClaimCheck cannot catch it.

**Fix:** after a successful `SignDocketBill`/`VetoDocketBill` in `AnswerDocketItem`, `AnswerSign` and `AnswerVeto`, call `QueueNewlyResolvedDivisions()` (it is play, not a harness advance). Then update the LATENT note at SimulationManager.cs:1731-1734.

### L2 - The answered-card branch on the Docket page is dead, and its comment is wrong
**Where:** GameController.PresidentialStart.cs:84 (doc) and 98-102.

**What happens:** the doc says "a card stays answered only where its bill is still held for another act (a budget's)". In fact `FinishPolishBudget` drops every decided entry from `ActsHeld` (PlayerPresident.cs:240-248), and an answered entry is always decided. Every non-budget site sets `bill.Docket = null` once it decides. So after `ActOnDocketAnswers` no answered entry survives, and the "Signed"/"Vetoed" card never draws. The diagnostic agrees: after a signature, `ActsHeld.Count == 1` (PolishPresidentialStartDiagnostic.cs:200).

**Fix:** delete the branch, or keep it as a defensive fallback and correct the comment ("never drawn while answers act at once").

### L3 - Stale "next day step" wording in the diagnostic
**Where:** PolishPresidentialStartDiagnostic.cs:283-284.

**What happens:** the comment says "acted on at the next day step as on the day it was presented", but the check right below now asserts the veto is voted at once, on `CutOath`. This is a comment only; no CommentClaim pattern applies.

**Fix:** reword it to "acted on at once (J6 (i))".

### L4 - The new `PresidentialDocket.Expired(.., Bill)` overload is unused
**Where:** PresidentialDocket.cs:102-107.

**What happens:** the rebase moved `PolishDocketGate` onto `DocketLastDay` (PlayerPresident.cs:116). Nothing in game code, diagnostics or the driver calls the Bill overload of `Expired` any more. It is a second statement of the term rule that can drift from `DocketLastDay`.

**Fix:** remove it, or have one diagnostic pin it equal to `CurrentDate > DocketLastDay`.

### L5 - No check that an answer leaves the day's other countdowns alone in Poland
**Where:** PolishPresidentialStartDiagnostic.cs, section (c).

**What happens:** the diagnostic proves the answered bill acts at once and that the unanswered third statute stays held. It never puts a Polish bill that is still counting down (`Docket == null`) in a slot during an answer, and never answers an ordinary statute while a budget is held. So nothing checks that `ActOnDocketAnswers` leaves those alone in Poland. The code is right (the guards are as described under item 1); this is a missing regression check.

**Fix:** in (c), seat one pending Polish bill with `DaysRemaining = 2` before the signature, and assert it still reads 2 afterwards. Also answer one statute while the budget is held and assert `ActsHeld` is unchanged.

## Carried, not introduced by the rebase (for the record)

- **C1 (L) - the calendar shows the wrong day for a held Polish budget.** GameController.cs:3462 together with `DeskOf` (9571) labels `ActsHeld[0]`'s law day as the budget's: "Annual budget bill law unless the President answers". For Poland that is the budget act's 7-day day (27 Aug). The budget applies only when its last statute act is decided, which can be the 21-day day (10 Sep). Fix: for a Polish held budget, show the maximum `DocketLawDay` across `ActsHeld`.
- **C2 (nit) - the held-acts dispatch keys on `ActsHeld != null`, not the procedure.** `AdvanceBudgetBillDay` (SimulationManager.cs:1906) routes on `ActsHeld != null` rather than `StatutePartsAreActs`. A US world whose Senate stopped being sworn while acts were held (`Congress.Applies` turning false) would send US acts into `FinishPolishBudget`. This is unreachable in play. Fix: add `&& Elections.WorldClock.StatutePartsAreActs(countryId)`.
- **C3 (nit) - a stale doc line on the 6-parameter `UsVetoGate`.** Its doc still says "Every other country ... pass through untouched" (SimulationManager.cs:1617-1618). Poland now holds bills for a player-president.
