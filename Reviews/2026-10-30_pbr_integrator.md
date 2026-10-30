# wave/pbr2 (K293's filing: the pension benefit ratio; K314-K319): the integrator's reading at the merge (2026-10-30)

**Files reviewed:** `Assets/Scripts/Data/Generated/PensionBenefitRatioPath.cs`, `Assets/Scripts/Data/PensionBenefitRatio.cs`, `Assets/Scripts/Data/PensionPayment.cs`, `Assets/Scripts/Data/SpendingDrivers.cs`, `Assets/Scripts/Data/SpendingLine.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, `Assets/Editor/PensionBenefitRatioDiagnostic.cs`, `Assets/Editor/DebtPathHoldDiagnostic.cs`, `Assets/Editor/AiFinanceMinistryDiagnostic.cs`, `Assets/Editor/TrajectorySentinelCheck.cs`, `Reviews/2026-10-30_pbr_prereg.md`, `Reviews/2026-10-30_pbr_review.md`, `docs/RULINGS_2026-10-23.md`.

**The path here.**
1. K293 filed each country's pension benefit ratio; it had to come before retiring the fiscal reaction from the player's book could be measured again under fisc2's (B).
2. Phase 1: the sources (the Ageing Report 2024 annex already on disk; CBO 62556's two workbooks fetched through the Internet Archive), then the prereg committed alone.
3. Phase 2, after fisc2 landed: amendments 1 and 2, each alone, before the build. The build stopped on two lines under the prereg: Italy 2070 (A1) and Sweden (h2).
4. Both were ruled (Code's recommendation, the standing instruction): registered with bounds after the measurement. Amendment 3 was committed alone, then the code. These are K318 and K319.
5. After Art. 155 landed, the implementer rebased into `wave/pbr2` (no conflicts), leaving `wave/pbr` untouched for the reviewer.
6. Independent review of 03a80d6c: READY on conditions (M1: record the rulings; M2: state what K318 and K319 relax). No High findings. Every bound fails when breached (planted mutations), neutrality is byte-identical, and the sources are exact (15 cells hand-checked).

**The merge.**
- `git diff 030d8d17 03a80d6c` and `git diff be2459e1 898eb8cc` have the same patch id (`98ba7d12`). The review applies to the rebased branch byte for byte.
- The prereg with amendments 1-3 was committed alone on main first (f6e7cfed), before any Unity run.
- K314-K319 were recorded on main alone before the merge (d05e0f2f). K318 and K319 carry M2's disclosures. M1 and M2 are answered.
- `git merge --squash wave/pbr2` (898eb8cc) applied cleanly on f6e7cfed.

**The measurement (this tree).**
- **npbr:** 15 of 15 clean. The prereg's Unity-only checks (PreviewParity, GeneratedCatalogCheck, UnemploymentFloorPhillips) pass here.
  - `PENSION BENEFIT RATIO: PASS`, with 2 registered misses: Italy 2050 (K315) and Italy 2070 (K318: (A1) +30.2 %, (A2) +6.0 %, coverage ×1.2288 the largest factor).
  - `DEBT HOLD: PASS`, with 2 registered misses: Germany (h2) (K295) and Sweden (h2) (K319: −13.0 against the DSM's −5.8 ±5.6). USA (h3): +13.4 against +19.6 over ten years, +56.7 against +74.5 over thirty.
  - LongRunAnchors, ParticipationCohort (K313 holding), SpendingIndexation, LineIndexation and PensionPayment are clean.
- **stagepbr:** the play saves re-staged at format 71. **slrtpbr:** 12 of 12 round-trip clean.
- **dumppbr** against dumpart155 (the `fisc2` family):
  - every country moves from turn 1 (ApprovalRating first; the USA from turn 2);
  - debt at turn 100 on seed 777: the USA −15.2 %, France −1.7 %, Sweden −1.1 %, Poland −0.75 %, Germany −0.38 %, Italy −0.24 %;
  - on seed 424242: the USA −15.1 %, Poland −2.6 %, France −1.4 %, Germany −1.0 %, Sweden −0.8 %, Italy −0.4 %.
- **The sentinel**, re-pinned to the family `pbr`: seed 777 `a7c54cbe9bf72b2cdd86d0d6bd92a8c68735bcc35e57f5375fba0aa4bd531730`, seed 424242 `7b70bc3eab2037df8aebd11a33f05039474d930e8830bc05da02b43b0d924bd8`.
- **Ledger rows `s956`** for the six money-path states, from the review. `SimulationManager.cs`'s merged state (98d360f5) differs from the reviewed 03a80d6c state (739d1fce) only by main's art155 hunks, which the patch id shows.

**FT-10, after the re-pin** (npbrl): four LEVEL cells were slack.
- France household (3.512 % at t12 against 5.5) is lowered by the check's rule to 4.5.
- France industry (0.917 %), Poland household (0.897 %) and Poland industry (0.914 %) now sit inside the 1.0 % clean band, so they CLOSE and leave the table, as Sweden's and Italy's did at §572.
- The breach ratchet `EnergyBreachCeiling` is lowered 4 → 1: 1 over the clean band.

**The simulation bar's one failure** (simpbr, 92 of 93): `AiFinanceMinistryDiagnostic` (`Assets/Editor/AiFinanceMinistryDiagnostic.cs`) found that France at 50 % debt with a deficit "at the 1 % limit" received a decision.
- **The cause is in the check's plant, not the rule.**
  - The plant wrote −0.01 × GDP. Through the rule's own division (flow / GDP × 100, single precision) that read −0.99999994 %, one unit in the last place inside the limit.
  - From there the rule correctly releases a microscopic share across all 24 lines.
  - The check had passed only where the rounding landed exactly, and pbr's French GDP after six turns moved it.
  - For this GDP, no single-precision flow within 64 units of the last place reads the limit exactly.
- **The integrator's fix:**
  - the check now asserts the rule's own resting point, `EuRequiredAdjustmentPercentOfGdp(−1, 50, 0) == 0`, independent of any plant;
  - it runs the end-to-end `Decide` plant only when a flow reads the limit exactly, and says so when none does.
- npbrf: clean. No rule or money path changes.

**The review's Lows (L1-L9) are filed, not fixed here:**
- K319's split compares the pension line alone with the DSM's whole cost of ageing, on different base years. Rebased, the verdict still holds: −5.1 against −2.3.
- The format-71 save note understates what a v70 save misses.
- The preview omits B's move before New Year within a turn.
- Poland's (A1) lines pass on offsetting errors ((A2) up to −14.6 %).
- Two helpers are dead code, and Germany (h2)'s reason text is stale.
- A few prose figures are slightly off.
- The year-five check doesn't cover the USA's pension line.
- Two promised instruments are not committed.

**Verdict:** READY. The merged state equals the reviewed branch (by patch id), plus the RULINGS lines and the sentinel's re-pin.
