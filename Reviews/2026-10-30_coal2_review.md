# COAL-2 review: the coalition verbs - threat, dismissal, enforcement, re-declaration (K136, K137) (`git diff 68bf26af..9e7ceecf`, branch `wave/s9coal`)

**Files reviewed:** `Assets/Scripts/Simulation/SimulationManager.cs` (the diff; plus `AdvanceCountryDayTick`, `AdvanceConfidenceDay`, `TrackAgreements`, `LeaveGovernmentNow`, `ClonePreviewCountry`, `Answered`), `Assets/Scripts/Simulation/SimulationManager.CoalitionExit.cs` (the diff and the whole file), `Assets/Scripts/Simulation/SimulationManager.CoalitionVerbs.cs` (new, whole file), `Assets/Scripts/Simulation/SimulationManager.PlayerElection.cs` (the diff; `HoldPlayerElection`'s two count paths), `Assets/Scripts/Simulation/SimulationManager.PolishDesk.cs` (`CanLeaveGovernment`), `Assets/Scripts/Simulation/SimulationManager.AiVoteMemo.cs` (the key), `Assets/Scripts/Simulation/ShadowBaseline.cs` (the fork's day order), `Assets/Scripts/Simulation/StanceModel.cs` (the bloc pull), `Assets/Scripts/Elections/CoalitionVerbs.cs` (new, whole file), `Assets/Scripts/Elections/NationalElection.cs` (the diff; `BlocOf`, `SwedenSides`, `SwedenBlocsOn`, `SharesFromCampaign`), `Assets/Scripts/Elections/SupportAgreement.cs` (`Demand`, `Track`, `Copy`), `Assets/Scripts/Elections/GovernmentRecord.cs` (`LeaveCabinet`, `Discharge`, `Reappoint`, `AllocatePortfolios`), `Assets/Scripts/Data/Country.cs` (the diff), `Assets/Scripts/Data/World.cs` (`SupportAgreementTemplates`), `Assets/Scripts/Persistence/SaveGameService.cs` (the format line), `Assets/Scripts/UI/GameController.cs` (the day order), `Assets/Scripts/UI/GameController.CoalitionVerbs.cs` (grep: which readers the UI calls), `Assets/Scripts/Testing/UiScreenshotDriver.CoalitionVerbs.cs` and `UiScreenshotDriver.cs` (the diff), `Assets/Editor/CoalitionVerbsDiagnostic.cs` (whole file), `Assets/Editor/SwedenBlocsDiagnostic.cs` (check (d)), `Assets/Editor/CheckSuite.cs` (the registration lists), `Assets/Editor/AverageCabinetDiagnostic.cs` (K59's source scan), `Assets/Editor/ChamberVerdictCacheCheck.cs` (the who-governs guard), `Assets/Editor/PreviewParityDiagnostic.cs` (the value-field audit), `Assets/Editor/TrajectorySentinelCheck.cs` and `TrajectoryBaselineDump.cs` (no player seated), `Tools/headless_hold/*` (the build idiom), `docs/RULINGS_2026-10-18.md` (q1, q19, q21), `docs/RULINGS_2026-10-23.md` (K59, K130-K137, K197, K239, K270), `docs/specs/POLITICAL_SYSTEM_SPEC.md` §5.2, `POLISIM_FEATURE_LIST.md` (the COAL-2 row), `Reviews/2026-10-27_coal1_review.md`.

Independent adversarial review, 2026-10-30. HEAD 9e7ceecf, base 68bf26af, five commits. Read-only on the repo except this file. No Unity run. I did not use the implementer's outputs; the probe below is my own.

## What I ran

1. **A played-world probe, out of the engine** (.NET, not Mono; `Tools/headless_hold`'s build idiom: `Tools/cgate/make.pl`, the patched engine module, `mkproj.pl`). Scratch copies of both trees by `git archive` (Assets/Scripts, Assets/Editor, Tools). The runner is mine: `G:/Claude/tmp/claude/C--Users-elias/b52b1bc1-2842-4fff-8db1-9f3b7e407ddb/scratchpad/coal2rv_tool/CoalPlay.cs` and `build.sh` beside it. It steps a world in the controller's own day order, as `ShadowBaseline.RunDaysThrough` does:
   - the AI answers yesterday's waits (`AnswerWaitsAsTheAi`);
   - `AdvanceDay`;
   - `AdvanceCountryDayTick(player)`;
   - the turn on a boundary;
   - `HoldPlayerElection` on the polling day.
   It prints, at full precision (`R`): every election record (shares and seats), every change of who governs, and every country's GDP, approval, debt, inflation and unemployment at each boundary. At the end it prints the record of verbs (by reflection, so the same runner compiles on the base tree) and each Swedish party's bloc read two ways: through the country (`BlocOf(Country)`) and by date alone.
2. **No player** (the dump's world), seed 777, 2,200 days: byte-identical, base against branch, on every line both trees print. The branch holds no motion.
3. **Played Sweden**, seed 777, 3,300 days, players none, S, C and L. Seed 424242, 4,400 days, players none, S, C, L, M and V. **Played Poland** (TD) and **played Germany** (SPD), seed 777, 3,300 days.
   - Every boundary line is byte-identical in all twelve played runs.
   - Every election is byte-identical except the Swedish counts of two runs (H2).
   - The German run never completes its chancellor election in my harness: the record stays a caretaker from 2025. So the probe holds no German K137 evidence.
   - Poland re-declares at each count's formation:
     - 2023-12-13;
     - 2027-12-14, after the Art. 154 confidence vote closed;
     - 2031-12-16, the re-appointed record (the 9e7ceecf fix).
     Every Polish side is bloc -1, and nothing moves.
4. **A text test of `SwedenBlocsDiagnostic`'s check (d)** on both trees: `git show <rev>:Assets/Scripts/Elections/NationalElection.cs | grep -c -F <literal>` for the two literals the check requires (H1).
5. **Mutations, by reading** (no Unity): each named in hunt item 4, with whether `CoalitionVerbsDiagnostic` would fail.

## Hunt answers

### 1. Inertness: holds where no player is seated; a played Sweden moves after its first in-game count

- **The day loop is gated.** `AdvanceCoalitionVerbsDay` (`SimulationManager.CoalitionVerbs.cs:291-298`) returns unless the country is the player's and K133's.
  - Its one caller is `AdvanceCountryDayTick` (`SimulationManager.cs:392`).
  - That tick is called only for a player country:
    - `GameController.cs:785`;
    - `ShadowBaseline.cs:314`, with `_dayTickFor = realSim.PlayerCountryId`;
    - the harnesses.
  - The trajectory dump and the sentinel seat no player (`TrajectoryBaselineDump.cs:26`; the sentinel's own note), so nothing new runs there.
- **The other changed paths are gated or exact.**
  - `RecordLeaving` in `WeighPartnerExits` (`SimulationManager.CoalitionExit.cs:63`) sits behind that method's player gate (`:37`).
  - `LeaveGovernmentNow` is a player verb.
  - `BlocOf(Country)` (`NationalElection.cs:336-343`) computes the same day as before. It returns the dated side whenever `TryRedeclaredBloc` finds no motion, and an empty list returns at `CoalitionVerbs.cs:132`.
  - `SwedenBlocsOn(keys, on, played)` (`NationalElection.cs:444-450`) with null, or with no re-declaration, is the old array.
  - `SharesOnPublishedPolls` / `SharesFromCampaign` take `played` as an optional last argument, so every other caller passes null.
- **Random draws.** None. The new files draw no stream. (I grepped `SimulationRandom`, `UnityEngine.Random` and `System.Random`.)
- **Iteration order.**
  - `SettleCoalitionMotions` iterates a copy (`:310`).
  - `RedeclareAfterCount` iterates `PartySystems.For` read-only.
  - No shared collection is reordered.
- **Save and clone.**
  - The save gains a list. It is empty in a no-player world.
  - The clone (`SimulationManager.cs:7895`) takes a new list. `PreviewParityDiagnostic` audits value fields only, so the new reference field neither fails nor is checked.
  - `ChamberVerdictCacheCheck`'s guard and `AverageCabinetDiagnostic`'s K59 scan match nothing new: no `.Government =` write and no direct `Cabinet` or `Support` write.
- **Measured** (What I ran, 2 and 3).
  - The no-player world is byte-identical.
  - A played world is byte-identical until its first in-game count forms a government.
  - After that, in Sweden, K137 moves the next count (H2).
  - The claim "the no-player dump and the trajectory sentinel stay byte-identical" holds. A claim that nothing moves before a verb is used would not: K137 is not a verb.

### 2. Each verb against its ruling

**The threat** (the spec's §5.2, "threaten to leave"; Code's declared reading of the deadline):
- *Made.* The gate is `CanThreaten`, `SimulationManager.CoalitionVerbs.cs:43-54`. The motion is created at `:153-172`, with a demand from the threatener's own `SupportAgreement.Demand` list and a deadline of 30, 60 or 90 days.
- *Met.* `Delivered` (`CoalitionVerbs.cs:222-235`) is the agreement's own test: a law enacted, or a dial at or above its target. Dial items are only ever raises (`SupportAgreement.cs:122-140`), so `>=` is right.
- *Carried out.* On its deadline (`:317-322`), after `CanLeaveGovernment` re-reads the gates:
  - the player leaves (`LeaveAsThePlayer`, `:351-360`);
  - the leaving goes on the record;
  - the break line is written;
  - `ExitOpensConfidenceProcedure` runs.
  This is a copy of `LeaveGovernmentNow`'s body, not a call to it (L4).
- *Withdrawn.* By the verb (`:184-192`).
- *Carried before its day.* `LeaveGovernmentNow` settles a standing threat when the player leaves (`SimulationManager.cs:4358-4360`).
- *Lapsing with the government.* Only by party name: `g.PmParty != m.Target || !g.Cabinet.Contains(m.Actor)` (`:315`). A threat outlives a change of government whenever the same head's party re-forms with the threatener inside (M1).

**The dismissal** (§5.2, "the prime minister's party dismisses the partner"; q21 read as a partner's exit):
- `DismissNow` (`:206-223`):
  - reads the player's chairs before `LeaveCabinet` and seats them after (K59, as COAL-1 does at `SimulationManager.CoalitionExit.cs:60-62`);
  - writes `DismissedLine`;
  - records the motion;
  - runs `ExitOpensConfidenceProcedure(country, partner)`.
- The dismissed partner votes by its own lines through the existing vote (K132).
- `CanDismiss` refuses:
  - a caretaker;
  - the head itself;
  - a non-member;
  - a Fraktion member alone (§705).
- Correct against q21 and K59.

**Enforcement** (§5.2, "enforce the agreement"; Code's declared reading):
- Called on an owed item (`:239-257`).
- *Met* when the item reads `Delivered`.
- *Refused* if §654 broke it first.
- *Refused* on its deadline, still owed: the item is set `Broken` with `BrokenOn` today, and the break is written (`:325-337`).
- `AiVoteKey` already folds each agreement's broken count into the memo's key (`SimulationManager.AiVoteMemo.cs:64`), so the direct write cannot pass under that memo. §654's own breaks write the item in place the same way.
- One gap: met is read off the *tracked* state, not the live test the threat uses (L3).

**K137** ("a party re-declares its bloc after each count, from the formation payoff"):
- *When.* The first day `FormedAfter(g, LatestCount)` holds (`CoalitionVerbs.cs:252-262`): the record installed on or after the count, no caretaker, no stand-in, no open round or Sejm procedure. A re-appointed Sejm cabinet counts through its procedure's polling day (`:260`). The probe shows it firing on the formation day in Sweden and after the confidence vote in Poland.
- *Once per count.* `:389`. Held by the diagnostic.
- *Who.* Every seated party but the player's party in the chamber (`PartyInChamber`; a player-president's party is the AI's, as SP-7b has it).
- *The side.* `SideToward` (`CoalitionVerbs.cs:113-114`): FOR if in the cabinet or its recorded support, AGAINST otherwise.
- *Sweden's bloc.* `BlocOfSide` (`:118-122`): FOR maps to the head's standing bloc, AGAINST to the other one.
- *Read from its day.* `BlocOf(Country)` and the tactical layer read it from its own day (`TryRedeclaredBloc`, `:129-142`, `m.On.Date > on.Date` skipped).
- **The side is wrong for every party outside a minority government's recorded support.** That is the formation's usual 2026 outcome in played Sweden (H2).
- A re-declaration also outranks any later dated side of record (L1).

### 3. Edge cases

- **A caretaker.**
  - `CanDismiss` refuses one. `FormedAfter` excludes one.
  - `CanThreaten` and `CanEnforce` do not, because `CanLeaveGovernment` has no caretaker gate. So a threat can be made to a caretaker and carried out against it.
  - `ExitOpensConfidenceProcedure` then returns silently at its caretaker test (`SimulationManager.CoalitionExit.cs:83`). No procedure and no break line follow, which is right for a caretaker, though the docstring's "the exit opening the confidence procedure" is then not what happens.
  - While a caretaker serves, `TrackAgreements` does not run daily (`SimulationManager.cs:4471`), so enforcement's met test can be stale (L3).
- **A change of government mid-threat or mid-demand.** See M1.
- **A party in two roles.**
  - `SideToward` treats the cabinet and the support alike.
  - `RoleOf` decides the verbs.
  - A Fraktion pair cannot be dismissed apart.
  - A player-president's party is the AI's in K137 and refused every verb (`CoalitionVerbRefusal`, `:37`).
  - No defect found.
- **Save and load.**
  - `CoalitionMotion` is public fields only, so Newtonsoft serialises it with no contract change.
  - An older save leaves the initialiser's empty list, which is what it held. No motion could exist in it.
  - But a save cut after an in-game Swedish count's formation holds no K137 sides. Loaded here, it re-declares on its first day toward whatever government stands then. The continuous run of this build re-declared on the formation day toward the government formed (L2).
- **The preview's clone.**
  - It takes its own list (`SimulationManager.cs:7895`); the diagnostic's (6) holds that.
  - The `CoalitionMotion` objects themselves are shared. Nothing in the preview writes a motion today, so this is safe, but only by convention (L5).
- **Two motions on one day.**
  - The settle loop runs over a copy.
  - A threat carried out re-reads `g` after a successor is installed (`:322`).
  - K137 runs after the settle step, so a government installed by that step is the one re-declared toward, where the count's re-declaration has not yet run.
  - A withdrawal and a new threat on the same day leave two motions with one `On`. `LatestOf`'s `>=` picks the later one in list order.
  - No defect found.

### 4. Does `CoalitionVerbsDiagnostic` fail when the code is wrong?

**Caught:**
- the deadline read one day late (`<` to `<=` at `:317`), by the day-34 "still open" and day-35 "carried" pair;
- `RecordLeaving` dropped from `LeaveAsThePlayer` (`leaving != null`);
- `ExitOpensConfidenceProcedure` dropped from the carried threat or from the dismissal (`ProcedureOpened` reads the "after L left" break);
- the item not broken, or `BrokenOn` not set, on a refused demand;
- K137's once-per-count guard removed (`afterSecond == made`);
- `FormedAfter` removed (`beforeFormed`);
- the player's party not excluded (`made == 7`, `Actor != "S"`);
- `TryRedeclaredBloc`'s date test removed (`cBefore == 0`);
- `BlocOf(Country)` not reading the re-declaration (`cNow == 1`);
- `SwedenBlocsOn(..., played)` ignoring `played`;
- the no-player gate removed (`ai.CoalitionMotions.Count == 0`);
- the clone sharing the country's list.

**Not caught:**
- (a) **`AdvanceCoalitionVerbsDay` unplugged from `AdvanceCountryDayTick`** (delete `SimulationManager.cs:392`). The diagnostic (`:46`, `:76`) and the film (`UiScreenshotDriver.CoalitionVerbs.cs:41`, `:65`) both reach the step by reflection, so no threat would ever settle and no party would ever re-declare in the game, and every check would pass.
- (b) **`played: country` dropped at the count** (`SimulationManager.PlayerElection.cs:144`). The diagnostic calls `SwedenBlocsOn` directly and never the count's path. K137 would then never reach an election, and the one numeric consequence it has would vanish unseen.
- (c) **The threat's lapse deleted** (`:315`). No case changes who governs under an open threat.
- (d) **The enforcement's lapse deleted** (`:329`).
- (e) **K137's mapping for a minority government.** The only planted government is a majority with C inside (`CoalitionVerbsDiagnostic.cs:265`). The check asserts exactly the AGAINST-to-the-other-bloc mapping that H2 finds wrong. It would pass the defect and fail its fix.
- (f) **The dismissal's K59 seating removed.** Only `!g.Portfolios.ContainsKey("L")` is read.
- (g) **The seats filter in K137 removed.** Every planted party is seated.
- (h) **`RecordLeaving` removed from the K130 exit** (`SimulationManager.CoalitionExit.cs:63`).

And the diagnostic is registered in no bar (M2).

### 5. Simulated numbers outside the player's verbs

- **K137's AI re-declaration in played Sweden.** It feeds:
  - the count's tactical layer (`NationalElection.cs:479`, through `PlayerElection.cs:144`);
  - the government-bill bloc pull (`StanceModel.cs:341`, `:405`);
  - the chamber's drawing (`HemicycleRenderer`, `PoliSimTheme`, the party tile).
- **Measured:**
  - two of ten played Swedish runs moved their next counts: up to 0.033 points a party, no seat changed (H2);
  - every boundary figure stayed identical in all twelve runs.
- **Nothing else.** The K130 exit and the player's leaving only add record entries, which no number reads. Germany's and Poland's sides are recorded at bloc -1 and read by nothing numeric.

## Money paths

The first 16 hex of SHA-256 of each file's final state with carriage returns removed (`perl -0777 -pe 's/\r//g' <file> | sha256sum`). The 9e7ceecf blob and the worktree's file give the same value:

| file | digest |
|---|---|
| `Assets/Scripts/Simulation/SimulationManager.cs` | `93a7037639045cda` |
| `Assets/Scripts/Simulation/SimulationManager.CoalitionExit.cs` | `7a02cf81c2afdb09` |
| `Assets/Scripts/Simulation/SimulationManager.CoalitionVerbs.cs` | `9154b27cebe9368c` |
| `Assets/Scripts/Simulation/SimulationManager.PlayerElection.cs` | `71d6c571b4482ab5` |

## Findings

### High

**H1. The cheap bar fails on this branch: `SwedenBlocsDiagnostic` check (d) pins the two call forms COAL-2 changed.**
- Where: `Assets/Editor/SwedenBlocsDiagnostic.cs:113-115`. The check requires these literals in `NationalElection.cs`:
  - `"SwedenBlocsOn(keys, on.Value)"`;
  - `"SharesFromCampaign(country, keys, finalShares, belief, out tactical, on: pollingDay)"`.
- The branch changed them to `SwedenBlocsOn(keys, on.Value, played)` (`:479`) and `..., on: pollingDay, played: played)` (`:313`).
- `grep -c -F` of each literal: 1 and 1 at 68bf26af, 0 and 0 at 9e7ceecf.
- The diagnostic sits in `CheckSuite`'s `Suite` list (`CheckSuite.cs:197`; the simulation list starts at `:520`), so every commit from ab1c354b on fails the cheap bar.
- Scenario: the integrator merges on the branch's word. The next cheap bar on main is red on a check about K53/K80, and nothing names COAL-2 as the cause.
- Fix: update the two literals to the new forms. Make (d) also require `played: country` at the count's call (`SimulationManager.PlayerElection.cs:144`), which closes hole 4 (b) as well. Then run the cheap bar.

**H2. K137 puts every party outside a minority government's recorded support on the other bloc - V, MP and C join the Tidö side after an S minority forms.**
- Where:
  - `SimulationManager.CoalitionVerbs.cs:391`, `:397`, `:409`;
  - `CoalitionVerbs.cs:113-114` (`SideToward`: AGAINST for anyone not in the cabinet or its recorded support);
  - `CoalitionVerbs.cs:118-122` (`BlocOfSide`: AGAINST is `1 - governmentBloc`).
- Measured (What I ran, 3):
  - In eight of ten played Swedish runs, the 2026 count formed "S alone, no support recorded" (`MinorityGovernment`). The other two, with the player as S, formed no government until 2030.
  - After each such formation, K137 recorded V, MP and C (C unless it was the player) AGAINST, at bloc 1. Example, seed 424242 with player L, the last day: `BLOC V country=1 record=0`, `BLOC C country=1 record=0`, `BLOC MP country=1 record=0`.
  - The next counts read those sides. Seed 777 with player L, the 2034 count: C -0.032, MP -0.022, V -0.019, M +0.033, SD +0.028, KD +0.013 points. Seed 424242 with player V: the 2030 and 2034 counts move up to 0.006 points.
  - No seat moved in these runs, because no party sat near the 4 % line.
- Against the ruling: K137 says the party re-declares "from the formation payoff". V's payoff is not higher under SD+M than under S. The code reads no payoff for a non-member at all. Its docstring's "the payoff's fixed point, no member doing better elsewhere" covers members only.
  - It conflates "not in this government" with "in the other bloc".
  - The 2022 sides it cites (D-10 (a)) were a formal support agreement on both sides. An S minority governing on V, MP and C's tolerance has none recorded.
- Failure scenarios:
  - (i) The player sees V and MP drawn with M, SD, KD and L on the hemicycle from the S government's first day (`HemicycleRenderer` reads `BlocOf(Country)`).
  - (ii) At the next count with MP near 4 %, the tactical layer has right-bloc voters lend to MP and left-bloc voters abandon it. This is `SwedenBlocsDiagnostic` (d)'s own lending mechanism, pointed the wrong way, and it can move a seat and a government.
  - (iii) On the S government's bills, V and MP lose the government-bloc pull they had under the dated sides (`StanceModel.cs:405`).
  - (iv) A head whose party stands in no bloc maps every party to -1 (`BlocOfSide`'s guard), so all of Sweden's blocs vanish. This happens with a created party heading the government, or with a player head who re-declared NEITHER after the last count.
- The diagnostic cannot see any of this. Its one planted government is a majority with C inside, and it asserts the mapping as correct (hole 4 (e)).
- Fix: this is Elias's to rule, or Code's recommendation under the standing instruction of 2026-10-27, recorded as a K-line. Two candidate readings:
  - (a) AGAINST only for the declared opposition: the parties red-lined from the formed cabinet by their own lines (`GovernmentFormation.RedLinedFrom`, which COAL-1 already uses for K131). Every other non-member keeps its standing side, or takes NEITHER.
  - (b) Each non-member's side from the formation payoff: the government it does better in, of the formed one and the best one it could have joined.
- Then add a diagnostic case on an S minority with no recorded support, asserting V, MP and C stay on S's side.
- Until it is ruled, K137 should not write Sweden's blocs.

### Medium

**M1. An open threat or demand survives a change of government, and is carried out against a government it was never made to.**
- Where: `SimulationManager.CoalitionVerbs.cs:315` (the threat lapses only when the head's party changes or the threatener leaves the cabinet) and `:327-329` (the demand follows `g.AgreementOf(actor)` and the item by key into whatever agreement is current).
- `MotionState.Withdrawn`'s own doc (`CoalitionVerbs.cs:31`) says "lapsed with the government it was made to".
- Scenario, threat:
  - Player L in M+KD+L with SD's support threatens on 2026-08-01, 90 days, deadline 2026-10-30.
  - The 2026 count's round re-forms M+KD+L(+SD) on a new record (its own `FormedOn`, its own agreements).
  - On 2026-10-30 the old demand is undelivered. L leaves the government it formed a few weeks earlier, and the confidence procedure opens.
  - A threat whose deadline falls inside the round is carried out against the caretaker instead.
- Scenario, demand: player SD enforces a law item before the count. The re-formed government's new agreement (`FormAgreements`, scored again from SD's positions) carries the same key. On the old deadline the new agreement's item is broken.
- Fix: carry the government's identity on the motion (its `FormedOn` with the head's party, or a record reference rebuilt at load), and lapse the motion when `country.Government` is another record. Add the case to the diagnostic (holes 4 (c) and (d)).

**M2. `CoalitionVerbsDiagnostic` is registered in no bar, and it misses the two wirings that make COAL-2 reach the game.**
- Where: `Assets/Editor/CheckSuite.cs` names `CoalitionExitDiagnostic` (`:210`) but not `CoalitionVerbsDiagnostic`.
- The diagnostic reaches the day step by reflection (`CoalitionVerbsDiagnostic.cs:46`, `:76`), and so does the film. Unplugging `SimulationManager.cs:392`, or dropping `played: country` at `PlayerElection.cs:144`, passes everything (holes 4 (a) and (b)).
- COAL-1's review found the same registration gap (`Reviews/2026-10-27_coal1_review.md`, line 30), and the integrator registered it.
- Scenario: a later edit to `AdvanceCountryDayTick` drops the call. Threats never settle and K137 never runs in the game, and the cheap and simulation bars stay green.
- Fix:
  - register the diagnostic (cheap list);
  - add one case that steps the day through `AdvanceCountryDayTick`, not reflection, and sees a threat carried out;
  - let H1's repaired check (d) pin `played: country` at the count.

**M3. The played world's numbers move, and the branch neither measures nor declares it.**
- Where: K137 (H2) through the count's tactical layer and the bloc pull. The commit messages and the docstrings say "Sweden's blocs read a re-declared side". No figure says what that moves, and nothing in the branch exercises a played Swedish world across a count.
- Scenario: a play-protocol hold or a played-Sweden comparison run after the merge differs from main from the 2030 count on. Nobody can tell whether it is K137 or a regression.
- Fix: once H2 is ruled, the record names K137's played-world effect: the probe's form, played Sweden across 2026-2035 on both seeds, base against branch.

### Low

**L1. A K137 re-declaration outranks any later dated side of record.**
- Where: `NationalElection.cs:341` and `:448` take `TryRedeclaredBloc` first; `CoalitionVerbs.cs:129-142` returns the latest re-declaration on or before the day, whatever the record holds after it.
- §621 and q19 say a declaration stands until a later dated one replaces it.
- No dated Swedish side is later than 2026-06-09 today, so nothing reads wrong yet.
- Scenario: when the 2030 declarations are sourced, a re-declaration made after the 2026 count masks every one of them.
- Fix: compare the re-declaration's day with the standing dated side's `From`, and take the later.

**L2. "No format step" is not what the house's precedent allows.**
- Where: `Country.cs:1260-1264` ("additive under ruling A - a save from before holds none, which is what it held (no format step)").
- A v71 save cut after an in-game Swedish count's formation holds no K137 sides. Here it re-declares on the load's first day, toward the government standing then.
- If a partner left, a supporter withdrew or a successor formed between the formation and the save, its sides differ from the continuous run's, and so do its blocs.
- v60's step was taken CONSERVATIVE under ruling A for exactly this class: "the Swedish blocs are the dated sides" (`SaveGameService.cs:125`).
- Fix: the integrator's call (format numbering is shared with the other branches). Either take the step or state the divergence on the line.

**L3. Enforcement reads the tracked state, not the live test the threat uses.**
- Where: `:330` reads `item.State == Delivered`. `TrackAgreements` runs daily only outside a caretaker, a round and a declaration's week (`SimulationManager.cs:4471-4476`). Laws track at enactment; a dial moved in those windows does not.
- Scenario: the deadline falls in a round, and the dial is at its target but untracked. The item is set Broken, and `Track` never revisits a Broken item (`SupportAgreement.cs:161`).
- Fix: call `agreement.Track(country, CurrentDate)` before reading the item, or use `Delivered(country, m)` as the threat does.

**L4. The carried-out threat duplicates the leaving instead of calling it.**
- Where: `LeaveAsThePlayer` (`:351-360`) copies `LeaveGovernmentNow`'s body (`SimulationManager.cs:4351-4365`).
- They agree today. A later change to the player's leaving (a K59 hand-back, a gate) reaches one and not the other.
- `LeaveAsThePlayer`'s `Debug.Log` is not gated on `IsCounterfactual`, so every ticking fork logs the leaving again (`LeaveGovernmentNow`'s log has the same gap).
- Fix: call `LeaveGovernmentNow` from the day loop. Its threat settle is a no-op once the motion is settled, and calling the private `Now` body keeps FORK-2's replay out.

**L5. The preview's clone shares the motion objects.**
- Where: `SimulationManager.cs:7895` copies the list, not its elements.
- Nothing in the preview writes a motion today. A preview path that one day settles motions would settle the played world's.
- The diagnostic's (6) checks the list's identity only.
- Fix: copy the elements, or say on the line that the preview may only read them.

**L6. K136's done-condition is not evidenced on the branch.**
- K136 asks that each verb be "played once in a filmed Swedish and a filmed German game, the audit harness re-run".
- The film stages each verb: a planted count, the day's step by reflection, the clock set (`UiScreenshotDriver.CoalitionVerbs.cs:21`, `:41`, `:65`). No audit-harness run is on the branch.
- My probe could not finish a German chancellor election, so I could not check the German re-declaration in play either.
- Fix: the record says which of K136's three parts stand, and what is owed.

## Verdict

**Verdict:** NOT READY.

The four verbs are built on the paths their rulings name. The dismissal and the carried-out threat go through COAL-1's exit and its procedure, K59's seating holds, the verbs replay in every except-world through FORK-2, and a world with no player is byte-identical (measured). Two things stop the merge:
- the branch cannot pass the cheap bar (H1);
- K137, the one part that moves simulated numbers, re-declares the wrong side in the formation's most common Swedish outcome (H2). That is a model-semantics rework that needs a ruling and a second reader.

What makes it ready:
1. Repair `SwedenBlocsDiagnostic` (d) for the new call forms. Make it pin `played: country` at the count. Then run the cheap bar (H1).
2. A recorded K-line on K137's side for a non-member of the formed government (H2's reading (a) or (b)), built, with a diagnostic case on an S minority with no recorded support. A second reader reviews the rework.
3. Lapse open motions with the government they were made to, with the case in the diagnostic (M1).
4. Register `CoalitionVerbsDiagnostic` (cheap list), and add a case that settles a motion through `AdvanceCountryDayTick` (M2).
5. The record states K137's played-world effect with its measurement (M3), and K136's parts as they stand (L6).

L1-L5 can follow. L2 is the integrator's numbering call.
