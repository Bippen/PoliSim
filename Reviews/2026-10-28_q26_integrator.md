# wave/q26: the integrator's reading at the merge (2026-10-28)

**Why this exists.** MF q26/q27 was built on `wave/q26` (2dd28faf, 94626e22). Its rulings are q26/q27 of 2026-10-18 and K145. It answers the audit's GA-6-1 (a whole fleet retired in weeks, real GDP unmoved) and GA-6-2 (Sweden's chain unbalance printed, never priced). It was reviewed three times:
- `Reviews/2026-10-28_q26_review.md`: NOT READY on H1, M1-M3 and L1-L6.
- `_review2.md`: NOT READY on N1, the price cliff where one wind step sent Sweden's four zones to 0 EUR/MWh.
- `_review3.md`: READY, with two Lows.

There were four rework commits: 3168d94c, 2e08c4be, 4c12af62, and 41b9b800, the last after the integrator's first measured run. Main moved after the branch was cut: §932 (MF q28, 6b2fea27) touched none of q26's files. This file reads the merged state.

**Files reviewed:** `Assets/Scripts/Simulation/EnergyReliability.cs`, `Assets/Scripts/Simulation/EnergyMarket.cs`, `Assets/Scripts/Simulation/EnergyLedger.cs`, `Assets/Scripts/Simulation/AiEnergyMinistry.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/Data/Country.cs`, `Assets/Scripts/Data/Generated/ValueOfLostLoadData.cs`, `Assets/Scripts/UI/GameController.EnergyV35.cs`, `Assets/Editor/EnergyReliabilityDiagnostic.cs`, `Assets/Editor/AuditInvariants.cs`, `Assets/Editor/AuditDriver.Styles.cs`, `Assets/Editor/CheckSuite.cs`, `Tools/voll_prep.pl`, `Tools/film_scope.tsv`, `docs/RULINGS_2026-10-23.md`, `EnergyData/raw/voll/SHA256SUMS.txt`, `EnergyData/raw/reliability/SHA256SUMS.txt`.

**The merge.** `git merge --squash wave/q26` on 6b2fea27 merged without conflict. The changed-line sets of every code file on the branch (`git diff 5c38ce1c wave/q26 -- f`) and on main (`git diff HEAD -- f`), sorted with carriage returns stripped, are IDENTICAL:

| File | Changed lines |
|---|---|
| `AuditDriver.Styles.cs` | 16 |
| `AuditInvariants.cs` | 16 |
| `EnergyReliabilityDiagnostic.cs` | 338 (at 41b9b800) |
| `Country.cs` | 11 |
| `ValueOfLostLoadData.cs` | 42 |
| `AiEnergyMinistry.cs` | 2 |
| `EnergyLedger.cs` | 3 |
| `EnergyMarket.cs` | 83 |
| `EnergyReliability.cs` | 175 |
| `SimulationManager.cs` | 8 |
| `GameController.EnergyV35.cs` | 29 |

The save format stays 66: the two new Country fields load as exactly 0 from a v66 save, as review 2 confirmed. GATE OK.

**The integrator's changes.**
- Registered `EnergyReliabilityDiagnostic` in the simulation batch after DebtPathHoldDiagnostic, commented s933. This was H1, the integrator's by the sweep's rule.
- Added a film-scope row for the dry film `drysq26` (Sweden and Germany at 1280x720).
- Appended K223-K226 to the rulings:
  - K223: Sweden's surplus booked as displaced hydro at the water value.
  - K224: the clearing test is stricter than the ministry's.
  - K225: household VoLL kept out of GDP; the USA's small C&I left out.
  - K226: EnWG § 13b for Germany; the other five countries' statutes owed.
- Moved Elias's Design-return zip ("Missing design files and project link.zip") out of the gitignored `AssetPackArchive/` and into `PoliSim-captures/design/RETURN_2026-10-28/`. It is a Design return, not an asset pack, and DeliveredAssetCheck read its context screenshots as undelivered game assets. The zip's contents are filed unpacked beside it.

**The first measured run and its one failure.** nq26 passed every check except the diagnostic's new third surplus case (review 3's L2: three times the wind fleet more, links widened, which must spill past the hydro). The run printed the seed's balance exactly, with nothing curtailed and the control's price. The implementer traced the cause to the diagnostic's `Land` helper. It planted builds through `EnergyFleet.Place`, which refuses a build past Sweden's connection queue (`EnergyConnectionQueue.RefusalFor`) and returns null. The helper never checked the result, so the case cleared the record fleet. The model was not at fault. `Land` now writes the build into the queue as a landed order, and every plant asserts that the fleet moved by exactly the planted MW, so a silent no-op now fails by name. This is the same class of trap as an unchecked staging in a film: a plant that does nothing makes a check pass, or fail, vacuously.

**The measurement (this tree, s933).**
- **nq26, first run:** clean apart from the above:
  - EnergyMinistryDiagnostic, EnergyLedgerDiagnostic (its 1/3-NTC probe's assertions hold), PreviewParityDiagnostic, UiLeverVerbCheck, MetaTextCheck, ConstantProvenanceCheck and DeadStateCheck.
  - In EnergyReliabilityDiagnostic, every case before the third: the one-step case (+500 MW: block 0 balance 72.9 against the seed's -24.2, 97.1 MW curtailed, SE4 36.58 = the control's) and the 5 % case (+811 MW, 157.6 curtailed, price = control).
- **dumpq26 (`mf26`) against `mf28`:** BYTE-IDENTICAL on both seeds (cmp). Nothing is unserved at the default epoch, and the sentinel stays at `mf28`.
- **drysq26 (Sweden and Germany at 1280) and realq26 (Germany 1280):** exit 0 with no error logged.
- **cheapq26:** two failures, both answered here:
  - DeliveredAssetCheck: the misplaced zip, moved.
  - ReviewLedgerCheck: the four money paths UNREVIEWED: `AiEnergyMinistry.cs` 399c32aa, `EnergyLedger.cs` 5f07cbf3, `EnergyMarket.cs` 4fa8a418, `SimulationManager.cs` b59fd3fe. Their changed lines are the branch's, read in the three reviews, and their rows are added at s933 with this file as evidence.
- **The re-run after the fix:** recorded in the commit's Bars line (nq26b, cheap, sim).

**Verdict:** READY. The merged state equals the reviewed branch line for line on every file, and the default dump is byte-identical. The first run's one failure was a vacuous plant in the check, not the model, and it is fixed so the same plant can no longer pass or fail silently. Both declared consequences are on record as K-lines for Elias: a pressed retirement never leaves load unserved (K224), and Sweden's surplus is absorbed at the water value (K223).
