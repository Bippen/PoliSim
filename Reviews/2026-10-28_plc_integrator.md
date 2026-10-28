# wave/plstage: the integrator's reading at the merge (2026-10-28)

**Why this exists.** PL-C stages a Polish campaign that serves both the Sejm's and the presidential elections. The rulings are K167 (the Kodeks wyborczy's limits cap the purse), K168 (the two finalists campaign through the 14 days between rounds) and K169 (the presidency on the 16 województwa, the Sejm on the 41 okręgi). The item also closes §936's owed line: the Polish count did not read the regional shift.

It was built on `wave/plstage` (5172cab2, b0adc430) and reviewed twice:

- `Reviews/2026-10-28_plc_review.md`: NOT READY.
  - H1: PollingDayDiagnostic still asserted that a Polish game stages no campaign.
  - H2: the Gate18 Poland refit was owed.
  - M1-M7 covered the remaining work: registration, films, the game's presidential day loop untested, the 2023 formation test on the old path, a false claim about voters abroad, the Polish no-policy books and the K-lines.
- `_review2.md`: READY.

The rework commits were fd6ebbd8 and e6ad1973. After the integrator's measurements came 5e6fe4ec (the cap caption), 10dddf88 (F7 registered under K267) and 9633bd97 (the films' order). This file reads the merged state.

**Files reviewed:** `Assets/Scripts/Elections/LiveCampaignSetup.Poland.cs`, `Assets/Scripts/Elections/PolishCampaign.cs`, `Assets/Scripts/Elections/Generated/PolishWojewodztwa.cs`, `Assets/Scripts/Elections/ParliamentaryCount.cs`, `Assets/Scripts/Elections/PolishSejmAllocation.cs`, `Assets/Scripts/Elections/PresidentialElection.cs`, `Assets/Scripts/Elections/RegionalCampaign.cs`, `Assets/Scripts/Elections/RecordFactors.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/Testing/UiScreenshotDriver.cs`, `Assets/Editor/PolishCampaignDiagnostic.cs`, `Assets/Editor/PollingDayDiagnostic.cs`, `Assets/Editor/PolishDeclarationsDiagnostic.cs`, `Assets/Editor/PolishPresidentialStartDiagnostic.cs`, `Assets/Editor/StartPointsDiagnostic.cs`, `Assets/Editor/StartBriefDiagnostic.cs`, `Assets/Editor/UsCampaignGeographyCheck.cs`, `Assets/Editor/CheckSuite.cs`, `Tools/wojewodztwa_prep.pl`, `Tools/film_scope.tsv`, `ElectionsData/poland/raw/spending_limits/SHA256SUMS.txt`, `ElectionsData/poland/raw/wojewodztwa_abroad_2020/SHA256SUMS.txt`, `docs/RULINGS_2026-10-23.md`.

**The merge.**
- `git merge --squash wave/plstage` was taken on 84fa8d08 (§940 and K252's prereg landed). K252's own merge was held aside in a stash ("k252-merge-hold") while this item was measured. The merge had no conflicts.
- Later commits were taken onto main by `git checkout wave/plstage -- <file>` for exactly the files they touched.
- The changed-line sets of all 30 code files on the branch (`git diff 3a275264 wave/plstage -- f`) and on main (`git diff HEAD -- f`), sorted with carriage returns stripped, are IDENTICAL.
- `RecordFactors.cs` differs from the branch by design: the refit was applied on main (below).
- Save format 67 -> 68. GATE OK.

**The integrator's changes.**
- Registered `PolishCampaignDiagnostic` in the simulation batch after RegionalCampaignDiagnostic, commented s941 (M1).
- **The Gate18 Poland refit (H2).** g18p1 failed only Poland2023, as K248 makes it, with a largest move of 1.5e-2 on MN. `perl Tools/record_factor_refit.pl g18p1.log --record s941` then moved 6 factors (37 already at the fit). In g18p2 the stored factors are the fit for all four elections (move 0.0e+0), and `--check` reports OK.
- Film-scope rows `dryplc`, `dryplc2` and `dryplc3`.
- K253-K267 appended to the rulings (M7). The implementer's K249-K261 drafts are renumbered, since K249-K252 were K218's.

**What the measurements found, in order.**
1. **nplc: F7 failed (the review's M4, the one result the implementer could not predict).** A Polish game's own 2023 election now seats KO+TD on NL's support, not the record's KO+TD+NL cabinet.
   - The implementer reproduced the formation out of Unity against the gate-built assembly, to the digit. Its base score (0.5 cohesion, 0.3 seat share, 0.2 negotiating power; support not scored) prefers the smaller cabinet even at the 2023 record's OWN seats (KO 157, TD 65, NL 26: KO+TD 67.450 against KO+TD+NL 65.471). KO+TD+NL wins only with NL at 30 or more and KO+TD about 200-210.
   - The last passing F7 log (cheapkf) confirms that the old pass came from a no-campaign count far from the record (KO 126, TD 77, NL 45). The campaign moves the count TOWARD the record: KO+TD 225 against the record's 222, NL 26 against 26.
   - The fix is a formation-scoring ruling, which would move every country's formation, so it is not PL-C's. F7 now prints as REGISTERED MISS (K267) with its condition unchanged and lifts itself when the condition holds. The R1-equals-the-game's-entry clause stays asserted.
2. **The films: an 8 px cap line overflowed** ("CAP ... PLN (KODEKS WYBORCZY ART. 199)" appended to the hero caption, about 100 px over in a 263.8 rect). The caption now reads "PLN · NN % OF THE ... CAP SPENT", with the article in the staging note.
3. **plc03 was MISSING.** The presidential HQ shots ran after the nights' walk had already passed 2025. They now film on that walk.
   - The film also logs a held clock after the Sejm night. The frame `realplci2_e7d_after_election_night.png` reads "Paused · 2 decisions waiting" (Docket and Budget marked): decisions a player answers, not a dead wall.
4. **The simulation bar (simplc) ran 31.5 minutes, past unity_run's 1800 s default.** It ended 89 of 89 clean. Future simulation bars take a longer `-TimeoutSec`.

**The measurement (this tree, s941).**

| Run | Result |
|---|---|
| nplc2 | PolishDeclarationsDiagnostic (F7 REGISTERED), PolishCampaignDiagnostic, Gate18Diagnostic: 3 of 3 clean |
| nplc (other checks) | PollingDayDiagnostic (H1), PolishPresidentialStartDiagnostic (M3, N1, N2), StartPoints, StartBrief, UsCampaignGeography, RegionalCampaign, GroundGameCount, PolishNight, PolishSejmAllocation, DebtPathHold, MetaText, UiLeverVerb, ConstantProvenance: clean |
| dumpplc | BYTE-IDENTICAL to `refit` (no player: the sentinel stands) |
| slrtplc | 12 of 12 |
| dryplc3, realplci3 (Poland 1280 `-shotinterrupts`) | exit 0, no OVERFLOW / CLIP / ESCAPE; 22 captured, 0 failed |
| realplc (Poland 1280, the presidential start and picker) | exit 0 |
| cheapplc | clean but the ledger |
| simplc | 89 of 89 |

- realplci3 includes plc01-plc04. `realplci3_plc04_runoff_campaign_hq.png` shows PiS and KO alone standing, "80 % OF THE 24,603,420 CAP SPENT", and campaign day 3 of 14.
- The ledger row for `SimulationManager.cs` at 498ad4b1 is added at s941 with this file as evidence. The baseline did not move.

**Seen on film, Lows left for later:**
- On the run-off HQ the top strip's polling-day chip reads the next Sejm date (17 Oct 2027), not the run-off's 1 Jun 2025.
- The footer announces a debate on 7 Jun, after the run-off's polling day, though K258 gives the run-off no debate.
- From the reviews: run-off debates, art. 136's sub-cap, older Polish Eurobarometer waves, and a check that rebuilds a loaded Sejm night from the saved okręg shift.

**Verdict:** READY. The merged state equals the reviewed branch line for line, and the refit is applied. F7's miss is the formation's own scoring at the record's seats, honestly registered under K267. Both bars are clean and the Polish films show the campaign.
