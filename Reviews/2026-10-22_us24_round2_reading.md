# US-24, the second fix round, the rebase onto §896 and format 53: the integrator's reading (2026-10-22, s897)

**Reviewed:** branch `wave/us24` after its rebase onto main 1d42413c (§896, US-27): `f4814fdf` (the build), `09f6ae0a` (the first fix round), `df998671` (the second fix round), `794ca243` (save format 53 on §896's 52), squash-merged onto 1d42413c. The adversarial review (`Reviews/2026-10-22_us24_review.md`, NOT READY: H1, M1-M4, L1-L9) read the build; the second review (`Reviews/2026-10-22_us24_fix_review.md`, READY WITH FIXES: M1, L1-L4) read the first fix round. This reading covers `09f6ae0a..794ca243` and the rebase, so that the review ledger's rows for the money paths name a reading of the state being committed.

**Files reviewed:**
- `Assets/Scripts/Simulation/SimulationManager.cs`
- `Assets/Scripts/Simulation/SimulationManager.DebtLimit.cs`
- `Assets/Scripts/Simulation/UsDebtLimit.cs`
- `Assets/Scripts/Simulation/AiFinanceMinistry.cs`
- `Assets/Scripts/UI/FiscalRules.cs`
- `Assets/Scripts/Persistence/SaveGameService.cs`
- `Assets/Editor/UsDebtLimitCheck.cs`
- `Assets/Editor/UsDebtLimitLiveDiagnostic.cs`

## The money paths, line by line

- **`SimulationManager.cs`:** two comment changes ("format 52" to "format 53" on the pending raise's save and restore). No code. The rebase merged it with US-27's elections-region edits with no conflict: US-24's edits sit in the fiscal day (`ApplyRevenueAndSpending`'s `- debtLimitCut`, the cut's accrual, `Track` in `AdvanceDay`), the day tick, the docket answers, save/restore and the clone; US-27's in `HoldUsPresidentialCount` / `HoldUsMidtermCount`. Neither reads a field the other writes.
- **`SimulationManager.DebtLimit.cs`:** one comment ("format 53").
- **`UsDebtLimit.cs`:** the second review's L3. `BillTitle()` no longer calls `PoliSim.UI.UiFormat.Money`; it appends `RaiseWords`, "US$" + `(RaiseUsd / 1e12).ToString("0.00", CultureInfo.InvariantCulture)` + "T". It is a string of a constant, used as the raise's title (FORK-2's docket identity, saved in divisions), so a change to the UI's money tiers can no longer move a saved title. No figure the trajectory reads depends on it. The simulation now references no `PoliSim.UI` type (grep empty).
- **`AiFinanceMinistry.cs`:** unchanged since the first review read it (R-US21 (v): the US rule deleted; the EU rule untouched).

## The rest

- `FiscalRules.cs` (L1): the debt slip branches on `Binding`. While the measures run, the debt is held AT the limit, with what was borrowed through the measures and the days left. Once binding, the excess is named as borrowing through the measures to their end and, since, only for interest the cut does not cover. UI text only.
- `UsDebtLimitCheck.cs` (L2): the engine walk reinstates at the game's own debt subject to limit alone, R-US21 (ii)'s words, without the removed headroom. The record's headroom reproduction stays as a check. It changes a check's walk, not the game.
- `UsDebtLimitLiveDiagnostic.cs` (L4): the title held to the exact literal; (e2) votes two raises on different limits and requires the same title; the back pay carried through a raise on the day path and paid on the first unarmed day; (f) asserts format 53 and refuses `CurrentSaveVersion - 1`.
- `SaveGameService.cs`: `CurrentSaveVersion = 53` with US-24's history entry before US-27's 52; both load gates compare against `CurrentSaveVersion`, so a v52 save is refused (ruling A).

## The second review's M1

The asks the code's comments name (the raise's size and form, raise only, the book-only cut, reconciliation as the vehicle, who tables, withholding, the timing, the measures' accounting, interest always paid, the CRS window, the no-policy world's docket, the back pay, the 2022-2023 starts, the FRA cap, the no-headroom reinstatement, L9's three behaviours) are written into `docs/ASKS_FOR_ELIAS.md` in this commit (§897).

## Verdict

No defect. The delta after the second review is comments, a title formatted in the simulation in one fixed invariant form, UI words and diagnostic teeth. The ledger rows for the four money paths at the merged state rest on the two reviews for `f4814fdf..09f6ae0a` and on this reading for the rest. The family `us24dl` is proved by the dump diff and the sentinel's digests, which carry their own rows.

## The family `us24dl`: the dump diff (s897)

`dump897` (`TrajectoryBaselineDump.Run -trajlabel=us24dl`) on the merged tree, diffed against `aloopsal2` with `Tools/traj_diff.pl aloopsal2 us24dl <seed> 100 first`, both seeds:

| country | seed 777 | seed 424242 |
|---|---|---|
| France, Germany, Italy | identical at every turn | identical at every turn |
| Poland | first differs at turn 4 (CurrencyStrength, TradeBalance); t100 GDP 8508.716 -> 8508.724 | first differs at turn 4 (same fields); t100 GDP 8645.021 -> 8645.045 |
| Sweden | first differs at turn 4 (CurrencyStrength, TradeBalance); t100 GDP 1931.588 -> 1931.592 | first differs at turn 4 (same fields); t100 GDP 1917.648 -> 1917.662 |
| USA | first differs at turn 3 (11 fields: ApprovalRating, the confidences, ...); t100 debt 1041120 -> 1255819 (+20.6 %), GDP 123759 -> 133224 (+7.6 %) | first differs at turn 3; t100 debt 989185 -> 1261238 (+27.5 %), GDP 122380 -> 134591 (+10.0 %) |

**The explanation, per country.** The USA moves from turn 3, the boundary at which the retired ratio-trend trigger would first have fired (UsDebtLimitCheck's MINISTRY line), because R-US21 (v) removed the ministry's discretionary sequester and the FRA cap excess it carried: from then the USA's discretionary lines are not cut, so its deficit and debt are larger and its GDP higher. Poland and Sweden, with their own currencies, move a turn later through the dollar and trade only (their first changed fields are CurrencyStrength and TradeBalance; their t100 GDP moves under 0.01 %). France, Germany and Italy, on the euro, do not move on either seed within 100 turns. Turns 1 and 2 are byte-identical everywhere on both seeds, as predicted by reading: the live limit itself writes only its own fields in this dump (the AI USA reaches it and raises it by act; no cut arms without the player's day tick), and the no-cut path's `- debtLimitCut` is exact.

**The sentinel.** `sent897` (TrajectorySentinelCheck alone, on the old label) failed on the digests only, the energy bounds pass holding; the new digests are seed 777 `b7b901c25e3f349afff0891587e1694138590236e679c1e45910371d2f4d0259` and seed 424242 `78fca53f78f05de1980954612ebac0c187007eeef62f6dc84d2fdbbf5b0b0112`, set with the label `us24dl` in this commit.

**Note, not this family's:** the no-policy USA's debt at turn 100 is already about eight times its GDP on `aloopsal2` (1041120 against 123759 at 777) and about nine on `us24dl`. The no-policy century has no fiscal response for the USA once the sequester is retired, and the debt limit's cut arms only under a player's day tick. That is R-US21's form as built (the AI raises by act), stated here so the century's US debt path is read as what it is.
