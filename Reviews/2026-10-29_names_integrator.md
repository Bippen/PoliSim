# wave/names (display names for enums and party keys, K285, K312): the integrator's reading at the merge (2026-10-29)

**Files reviewed:** `Assets/Editor/EnumDisplayNameCheck.cs`, `Assets/Editor/CheckSuite.cs`, `Assets/Editor/CommentImmunityCheck.cs`, `Assets/Scripts/Data/EnumNames.cs`, `Assets/Scripts/Data/PartySystem.cs`, `Assets/Scripts/Simulation/SimulationManager.cs`, `Assets/Scripts/Simulation/SimulationManager.FormationProjection.cs`, `Assets/Scripts/Simulation/SimulationManager.PlayerElection.cs`, `Assets/Scripts/Simulation/StanceModel.cs`, `Assets/Scripts/UI/ElectionNightScreen.Italy.cs`, `Tools/film_scope.tsv`, `Tools/review_ledger.tsv`.

**The path here.**
1. §946's filed M4: raw enum names and party keys reach player text.
2. The branch: enum display names (`EnumNames`), K285's Italian short names, party keys by short name, and `EnumDisplayNameCheck`.
3. Review round 1: NOT READY (M1, and M2's three merge steps). Round 2: NOT READY (M3, the `ReasonShort` guard). L3 was fixed by K312. Round 3: READY at `c2e7c4ad`.

**The merge.**
- `git merge --squash wave/names` applied cleanly on 900c8643.
- M2's three steps taken here:
  - `EnumDisplayNameCheck` registered in the cheap group after `PortfolioDisplayNameCheck`;
  - added to `CommentImmunityCheck`'s MustStrip list;
  - ledger rows `s952` on the money path's final states: `SimulationManager.cs` b65ff65e, `.FormationProjection.cs` f42de7cb, `.PlayerElection.cs` 67d1a3f5.
- Film scope row `drynames`.

**The measurement (this tree).**
- **nnames:** 16 of 16 named checks clean. EnumDisplayNameCheck: 17 of 17 key members typed; K312's join 5 of 5; teeth 12 of 12 caught and 9 of 9 passed.
- **drynames:** 4 sessions, 0 failed, 0 overflow.
- **realnamesit** (Italy 1280, `-shotinterrupts`): 19 captured, 0 failed. The night's count reads "+E" and "Az-IV" (`realnamesit_e7c_election_night_takeover`).
- **realnamesde** (Germany 1280): 153 captured, 0 failed. The formation sheet reads "GRÜNE" (`realnamesde_07a_politics_parliament_formation_sheet`).
- **dumpnames** against dumpk311: byte-identical on seeds 777 and 424242. The change is text-only, so the sentinel stands at `k276`.
- **cheapnames:** 148 of 148, the new check included.
- **simnames:** 92 of 92.

**Verdict:** READY. The merged state equals the reviewed branch (`c2e7c4ad`) plus M2's three merge steps.
