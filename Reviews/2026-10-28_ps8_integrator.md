# wave/ps8: the integrator's reading at the merge (2026-10-28)

**Why this exists.** PS-8 asks that every modelled country hold its elections on its own calendar as an AI country, with its count a pure function of its world's state and the record seated by date where a country's model does not exist. The feature-list row, `POLITICAL_SYSTEM_SPEC.md` decision 3 and the rulings K159-K163 set this out.

It was built on `wave/ps8` (f3797956) and reviewed three times:

| Review | Verdict | Main findings |
|---|---|---|
| `Reviews/2026-10-28_ps8_review.md` | NOT READY | H1: an AI Bundestag stopped at its first ballot and left a caretaker for four years. M1: the Riksdag's extra-election reason was false. |
| `_review2.md` | NOT READY | N1: the results-card film walked a game play cannot reach. N2: the hung-Bundestag plant could pass unexercised. |
| `_review3.md` | NOT READY on N3 alone | The film laid the player's budget as an AI bill. |

The fixes came in 7838ce15, 962ffd3e, aa9311f0 and a0a75dfd. Two more commits followed the integrator's measurements: cee08e89 (the purity check's chamber, Sweden 2022 seated by record, the US card's clauses, the Finance partner in every install line) and eb4a1913 (five older cheap checks updated for a world where AI countries vote). This file reads the merged state.

**Files reviewed:** `Assets/Scripts/Elections/WorldCalendar.cs`, `Assets/Scripts/Elections/WorldClock.cs`, `Assets/Scripts/Elections/GovernmentRecord.cs`, `Assets/Scripts/Simulation/SimulationManager.WorldCalendar.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/Persistence/SaveGameService.cs`, `Assets/Scripts/UI/GameController.WorldCalendar.cs`, `Assets/Scripts/UI/GameController.ForeignResultCard.cs`, `Assets/Scripts/UI/GameController.DocketV35.cs`, `Assets/Scripts/Testing/UiScreenshotDriver.cs`, `Assets/Editor/WorldCalendarDiagnostic.cs`, `Assets/Editor/PollingDayDiagnostic.cs`, `Assets/Editor/HistoryRolledForwardDiagnostic.cs`, `Assets/Editor/UsPresidentialElectionDiagnostic.cs`, `Assets/Editor/CongressOfRecordDiagnostic.cs`, `Assets/Editor/PresidencyDiagnostic.cs`, `Assets/Editor/FinancePartnerDiagnostic.cs`, `Assets/Editor/PartyInkDrawSiteCheck.cs`, `Assets/Editor/UiScreenshotCapture.cs`, `Assets/Editor/CheckSuite.cs`, `Assets/Editor/TrajectorySentinelCheck.cs`, `Tools/film_scope.tsv`, `docs/RULINGS_2026-10-23.md`, `ElectionsData/germany/raw/records/SHA256SUMS.txt`.

**The merge.**
- `git merge --squash wave/ps8` (a0a75dfd) on 626a911c (§937 landed) merged without conflict. The two later commits were taken onto main with `git checkout wave/ps8 -- <file>` for exactly the files they touched.
- The changed-line sets of all 19 code files on the branch (`git diff 06aa19e2 wave/ps8 -- f`) and on main (`git diff HEAD -- f`), sorted with carriage returns stripped, are IDENTICAL. The largest are `WorldCalendarDiagnostic.cs` 742, `WorldCalendar.cs` 341, `SimulationManager.WorldCalendar.cs` 319 and `UiScreenshotDriver.cs` 113.
- Save format 66 -> 67. A v66 save would never hold an AI election, so it is refused by the existing pattern.
- GATE OK.

**The integrator's changes.**
- Registered `WorldCalendarDiagnostic` in the simulation batch after ItalianMandatesDayDiagnostic, commented s938.
- Added the film-scope row `drysabr`.
- Re-pinned the sentinel to the family `ps8` (777 0ce1e056..., 424242 f450438a...).
- Appended K238-K244, K246 and K247 to the rulings. K245 was already taken by K210's (d1), so PS-8's BGB reckoning is K246. K247 records that an AI book follows the treaty rule whoever governs, reached only through a Finance partner.
- Re-staged the play saves and regenerated the three US model cards.

**What the measurements found, in order.**
1. **The first named run (nps8)** failed nine clauses in WorldCalendarDiagnostic.
   - Eight were the "purity" re-prediction disagreeing with the shares a count was handed. The economic vote's magnitudes read the SEATED chamber, and the count seats the new chamber straight after predicting, so the re-prediction read a different chamber. That is a legitimate state change, not an impure count. The check now puts the eve's chamber back for its call.
   - The other two came from Sweden's 11 Sep 2022 election in the Italy start, which the vote model cannot predict. The AI path now seats that election's chamber of record by date, as the row prescribes.
   - The US presidential card's (g) and (h) asserted the old world, "holds no US count", and were inverted by K243 and R-US5 (a).
2. **The dump `ps8` against `mf210` did not match the implementer's prediction.** Sweden moved only through trade from turn 7, Poland first at turn 6, and Germany not until turn 35.
   - The dump world does hold the elections: 652 CALENDAR lines, for example Sweden 2026 installing S alone, Italy 2027 FdI+Lega+FI+NM, Poland 2027 KO+TD, a DEM US president from January 2029, Germany 2029 CDU+SPD+CSU+BSW+FDP, and Sweden 2030 SD+M+KD+L.
   - An AI country's budget book follows the AI finance ministry's treaty rule whoever governs. A new government reaches it only through a Finance-holding partner, `FinancePartner`.
   - The install lines now name the partner and confirm it: Sweden 2026 and 2030, Poland 2027 and Germany 2029 have none (Finance stays with the head's party). Poland's 2031 cabinet gives Finance to TD, which is exactly the turn-6 first difference.
   - This is the design as built. It is recorded as K247, with the governing party's own fiscal preference reaching an AI book filed as a calibration item.
3. **The first cheap bar (cheapps8)** failed five older checks that asserted AI countries frozen: AverageCabinetDiagnostic (K59's install pattern), FinancePartnerDiagnostic (a no-player world now holds Germany's 2025 election inside the turn), CongressOfRecordDiagnostic (g), PresidencyDiagnostic (b) and (g), and PartyInkDrawSiteCheck (the results card drew party ink off-site; it now has its own allow-listed file). All five were updated by the rulings, and none was loosened beyond what PS-8 changes.

**The measurement (this tree, s938).**
- **nps8b:** WorldCalendarDiagnostic 9 of 9 clean, ending `every country's calendar holds`. Every first election falls on its statute's hand-coded day. PollingDayDiagnostic, Gate18Diagnostic, HistoryRolledForwardDiagnostic, ChamberVerdictCacheCheck, PreviewParityDiagnostic, MetaTextCheck, UiLeverVerbCheck and ConstantProvenanceCheck are clean.
- **slrtps8c:** the save round-trip, 12 of 12 clean.
- **Films:** drysabr (Sweden 1280) and realabr (Sweden 1280) exit 0. The walk holds Sweden's 13 Sep 2026 election as play holds it, installs S's government on 20 Oct, answers the budget by the player's own verb, and lists the AI US midterm of 3 Nov 2026 (House DEM 229, REP 206).
- **simps8:** 88 of 88. The simulation bar took 1778 s against K210's 1801 s with one check fewer, so the AI elections inside the no-policy centuries cost no measurable bar time.
- **cheapps8d:** clean except the ledger, whose rows are added at s938 with this file as evidence: `SimulationManager.WorldCalendar.cs` fc860170, `SimulationManager.cs` dd4f51a3, and both baseline digests.
- **The dump:** unchanged by the two later commits (ps8d byte-identical to ps8). Against `mf210`, 322 of 468 readings move by turn 100. GDP at turn 100: Germany -1.05 %, Poland -0.55 %, Italy -0.08 %, France and Sweden -0.01 %.

**Owed, not PS-8's:**
- The Design board for the results card (D-WORKLIST section 6; the card is a stand-in).
- COAL-1 for AI countries (K239).
- A governing party's fiscal preference reaching an AI book (K247).
- Gate18's record-factor refit, which reg5a already reported due.

**Verdict:** READY. The merged state equals the reviewed branch line for line, and both bars are clean. Each failure the measurements found had its cause identified and fixed at its cause, or was recorded as the design's own reading.
