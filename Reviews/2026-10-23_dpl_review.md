# D-PL independent review - Poland's election nights (boards 28a-28d)

Branch `wave/dpl`, worktree `G:/UNITY/Projects/PoliSim-wt/dpl`, base `0899496d`, HEAD `78cf61d2` (five commits, 20 files, +2424 / -31).
Reviewer: an independent agent. No code edited, Unity never run, nothing pushed. Instruments: reading, the compile gate, and a dotnet 10 probe that compiles `PolishSejmNight.cs`, `PolishSejmAllocation.cs` and the generated `PolishSejmDistricts2023.cs` byte for byte against two shims (`CountryId`, `NationalElection.ExemptFromThreshold`, `ElectionNightFromModel.FinalMinute`). The probe lives in the session scratchpad (`dpl_review/probe/`) and is thrown away with it.

**Files reviewed:** `G:/UNITY/Projects/PoliSim/CLAUDE.md`; `COMPLETED.md` §707 only; the whole diff `0899496d..78cf61d2`; in full `Assets/Scripts/Elections/PolishSejmNight.cs`, `Assets/Scripts/Elections/PolishSejmAllocation.cs`, `Assets/Scripts/UI/ElectionNightScreen.Poland.cs`, `Assets/Scripts/UI/GameController.PolishNight.cs`, `Assets/Scripts/UI/OkregiCellView.cs`, `Assets/Editor/PolishNightDiagnostic.cs`; the head and okreg/voivodeship table of `Assets/Scripts/Elections/Generated/PolishOkregiCartogram.cs`; the head of `Assets/Scripts/Elections/Generated/PolishSejmDistricts2023.cs`; the head of `Tools/dpl_cartogram_prep.pl`; `Assets/Scripts/UI/ElectionNightScreen.cs` (the diff, `Shell`, `BuildFooter`'s sizes); `Assets/Scripts/UI/GameController.cs` (the diff, the day loop 760-810, the load path 1120-1175, the canvas phases 1920-2030, `CheckElection`/`ResolveElectionVerdict` 6385-6475, `ShowElectionNight` 6540-6700); `Assets/Scripts/UI/GameController.ParliamentRows.cs` (the diff); `Assets/Scripts/UI/PoliSimTheme.cs` (the diff, `HasPartyInk`, `PartyLaddered`, the nudge and bloc fence 340-560); `Assets/Scripts/Testing/UiScreenshotDriver.cs` (the diff; contexts 1485-1520, 4054, 4560-4640, 4935-4975, 5120-5165); `Assets/Scripts/Elections/ElectionNightFromModel.cs` (the diff and its callers); `Assets/Scripts/Elections/PresidentialElection.cs` (`Contest`, the constants 40-62, `PresidentAt`, `HoldRounds`, `HoldRunOff`); `Assets/Scripts/Simulation/SimulationManager.cs` (`HoldPresidentialRound`); `Assets/Scripts/Simulation/SimulationManager.PlayerPresident.cs` (`NotePlayerVerdict`); `Assets/Scripts/Elections/PresidentialVeto.cs` (the override constants); `Assets/Scripts/Data/PartySystem.cs` (`ShortName`, `ChamberSeats`, the Polish roster line); `Assets/Scripts/Elections/NationalElection.cs` (`ExemptFromThreshold`); `Assets/Scripts/UI/CanvasChrome.cs` (the scaler); `Assets/Scripts/UI/V35.cs` (the floor); `Assets/Editor/CheckSuite.cs` (the registrations); `Assets/Editor/CountryInkFenceCheck.cs`; `Assets/Editor/PartyInkHarness.cs`; `Assets/Editor/D16AcceptanceCheck.cs` (its scope); `Assets/Editor/ConstantProvenanceCheck.cs`; `Assets/Editor/DeadStateCheck.cs` (its regexes); `Assets/Editor/AuditDriver.cs` (the night handling 790-840); `Tools/textcheck/Runner.cs`; the board texts `PoliSim-captures/design/dpl/RETURN.md`, `NOTES.md`, `Election Night PL v3.5.dc.html` and `Election Night PL President v3.5.dc.html` (text scanned), and `night.json`'s digest.

## Verdict: NOT READY

The arithmetic holds. The night at 41 of 41 is the chamber's own count. A call never reverses and is never wrong. The certain seats are a true lower bound. The Swedish and German nights are unchanged. No simulation output moves. Three things stop it. First, the diagnostic that holds all this is in no bar (M1). Second, the Sejm night tells the player something false on the game's own played prediction (M2). Third, the President's night can be skipped silently after an in-session load (M3). Each is a small fix.

Gate: `bash Tools/cgate/gate.sh G:/UNITY/Projects/PoliSim-wt/dpl` -> `GATE OK`. `Tools/textcheck` on the worktree: 4 of 5 pass. `UpstreamCheck` fails only because `wave/dpl` has no upstream, which is the environment and not this code.

## The claims, measured

**1. The night at 41 of 41 against `Allocate`.** TRUE, and the residue reading is exact. Here is why. With Base = the 2023 committees' own sum, Σ_d size_d·(v_dc/size_d + Δ_c) = (national2023_c + Δ_c)·Base = national_c·Base. So a list's okregi carry exactly its national share times Base, plus the lift that the zero floor adds where the swing goes below nothing. `Residue` is that lift, and (Counted − Residue)/Base reaches the national share at 41 of 41. At the last okreg the call is the chamber's own test (`National >= Line`, the same `>=` as `Admitted`).
- **IN is never wrong:** running_k ≤ carried_k, so the bound can never pass the national share.
- **OUT is never wrong:** list k's final share is toward + remaining_k/Base, and remaining_k ≤ outstanding.
- Neither call can reverse: `At` never reconsiders a landed call.
- Rounding: the per-step sums differ by about 1e-16 of a share, far inside `LineMargin` 1e-9.
- Probe: 6,000 sweeps (a third with one list planted within ±0.1 pp of its line, a third with one or two new lists, shares down to 0.02 × 2023's). The 2023 record gives PiS 194 · KO 157 · TD 65 · NL 26 · Konf 18 · MN 0. Final calls or seats disagreeing with `Allocate`: **0**. Calls made at 5, 15, 25, 30, 35, 39 or 40 of 41 that disagree with the final national test: **0** of 42,000 instants.
- The residue matters: reading the line on the raw counted votes ÷ Base disagrees with the chamber in 477 list-verdicts. Board 28b's literal rule (counted ÷ all votes cast) disagrees in 555.

**2. Certain seats.** TRUE, and the subset enumeration is redundant. d'Hondt in one okreg is the top-M quotients under one strict total order: quotient, then votes, then list order, which is the tie rule `DHondt` already has. Removing a list only removes quotients from the pool, so no remaining list's seats can fall. The paradox the brief asks about cannot happen in d'Hondt with a fixed magnitude.
- So the minimum over subsets is: the all-open-admitted division for a list already called, and 0 for an open list.
- Probe: the subset minimum differs from that closed form **0** times over all declared okregi at the seven instants above.
- Cost: `At` is called once per board build, at the final minute live or at minute 220 on the film's staged night, never per frame. With 7 open lists it measured 0.22-1.37 ms. The 2^n loop costs nothing that matters but can be replaced (L1).

**3. Inertness.** TRUE.
- `Allocate`'s body is untouched. The diff adds `LineOf` and widens `DHondt` from `private` to `internal`.
- Nothing is saved: `_presidentialNightShown` is a controller field, and `SaveGame` is untouched.
- `ElectionNightFromModel.Available` is read only by `GameController.ShowElectionNight` and the new diagnostic. `ElectionNightFromModel.At` still returns null for Poland.
- `PoliSimTheme` is read by no file under `Simulation/`, `Elections/`, `Data/` or `Persistence/`.
- What the Polish inks repaint: the hemicycle (`HemicycleRenderer`), the start card's seat strip (`CountrySelectorScreen`, a Canvas surface no filmed width in this item claims), the laws board's bars (`GameController.LawsBoard`), and the party-creation flow. That flow now refuses a candidate hue within 0.06 oklab of a Polish ink (`NearestRealInk`), which is a behaviour change for Polish created parties.
- No check pins Poland at Neutral. `CountryInkFenceCheck` reads country inks. `D16AcceptanceCheck` is Sweden-only. `PartyInkHarness` (not in the suite) already returns 0 for every non-Swedish ink. No film or pixel pin reads Polish party colours.

**4. The partial split.** TRUE: Sweden and Germany behave the same.
- `Shell` is the old body, moved whole.
- The null-furniture check gives the same warning on the same conditions.
- `Masthead` gets the same two strings and the old sizes.
- `BuildFooter` gets `screen.Root.transform`, which is the old `root`.
- The phase machine's entry and exit are generic (`_electionNight != null`), so the new nights use the same seam.

**5. `CheckPresidentialRound`.**
- It fires once per played day. It runs only for Poland and only where a contest's `FirstVote` or held `RunOffOn` is today, and the loop then breaks on `_electionNight`.
- Headless runs: `AuditDriver` presses any night's `ContinueButton`, so audits do not stall. The audit's night numbering for a Polish run shifts by the President's nights (L9).
- The dry film's only Polish clock-running window is the interrupts film's held-clock watch. That is about eight days after 15 Oct 2023, with no round in it.
- FORK-2's worlds hold their own rounds but have no controller, so they open nothing.
- **Load is broken** (M3).

**6. The rules.**
- No runtime `{0}'s` and no `§` in player text.
- No `<see cref` in any added line. The added comment lines that carry a `"` name only identifiers the code really uses (`PolishSejmAllocation.Allocate`, `DHondt`, `FinalMinute`, `PolishSejmNight`), so no guard is satisfied falsely.
- CRLF is consistent in every touched file, the generator included.
- The six new `.meta` GUIDs are each unique in the tree.
- The cartogram's source digest matches `night.json` (`aa189e44…`), and its okreg-to-voivodeship table matches the PKW's (DS 1-3 … ZP 40-41).
- Provenance and the floor: see M4 and L4.

**7. The diagnostic and the film.** The diagnostic is real where it compares the night against `Allocate` and the cartogram against its invariants, but parts of (c) and (f) are true by construction (L3), and it is in no bar (M1). The film: see M5 and L8.

## Findings

**M1 - `PolishNightDiagnostic` is registered in no bar.** `Assets/Editor/CheckSuite.cs:214` registers `GermanNightDiagnostic` (§707: "the cheap bar's"). `PolishNightDiagnostic` appears nowhere in `CheckSuite.cs`. Failure scenario: a later edit to the uniform swing in `PolishSejmAllocation.Allocate` (say, keeping MN's rise in okreg 21; see M2) is not copied into `PolishSejmNight.DistrictVotes`, which "computes in the same expressions and the same order". The night and the chamber then part, and every bar stays green. The only runtime trace is `ShowPolishSejmNight`'s LogError, which fires only in a played Polish election. Three comments claim the diagnostic as a guard (`PolishSejmNight.cs:14`, `GameController.PolishNight.cs:110`, the cartogram's header), and none of those claims is enforced. Fix: one row in the cheap bar's table.

**M2 - the Sejm night says the minority "stands in one okręg" when the model has spread it to all 41.** `ElectionNightScreen.Poland.cs:180` (the legend) and `:199` (the slip: "OWED UNTIL ITS OKRĘG DECLARES"). For a committee with zero 2023 votes in an okreg, `DistrictVotes` and `Allocate` both compute local = max(0, 0 + Δ). So any rise in MN's national share above its 2023 0.124 % gives MN votes in every okreg. Probe on the diagnostic's own "§767 played prediction" (MN 0.2 %): **MN has votes in 41 of 41 okregi**. The legend is then false. `MinorityOkreg` returns 0, so the boxed 21 disappears. MN's seats stay owed until 41 of 41, not until okreg 21. The diagnostic's (c) skips exactly this case (`only21`). The night should print what the model holds. Whether a single-okreg committee's rise should be spread nationwide at all is an `Allocate` question (pre-existing, Elias's to rule), but this night must not state the opposite of it.

**M3 - an in-session load can silently skip a President's night.** `GameController.PolishNight.cs:17, 161, 170`. `_presidentialNightShown` is never reset; the load path (`GameController.cs:1120-1175`) and new-game paths do not touch it. Scenario: Elias watches the 18 May 2025 first-round night, loads an autosave from 10 May in the same session, and plays on. On 18 May the manager holds the first vote again, and `CheckPresidentialRound` returns at `_presidentialNightShown == today`, so there is no night. The guard has no other effect: the day loop visits each day once, and the night breaks the loop. Delete the guard or reset it on load.

**M4 - figures transcribed instead of read, and constants without provenance.**
- `ElectionNightScreen.Poland.cs:305` writes `const int seats = 460, majority = 231, threeFifths = 276`, and the slips repeat "460" and "276" (`:327`, `:338`). `PartySystems.ChamberSeats(CountryId.Poland)` is the SOURCED 460, and `PresidentialVeto.OverrideNumerator/OverrideDenominator` is the 3/5. Failure scenario: a chamber-size or override-base ruling moves the model while the night's ticks and slips keep the old numbers. Note also that 276 overrides only at full attendance: the override is 3/5 of those voting, which `PresidentialVeto` computes.
- `LaneTop = 0.15` (`:185`), `BoardPx = 1.5f` (`:30`), `LineMargin = 1e-9` (`PolishSejmNight.cs:107`) and the board pitches in `OkregiCellView` carry none of SOURCED, DERIVED, CONVENTION or [AUTHORED-DRAFT]. `ConstantProvenanceCheck` scans `Simulation/` only, so no bar catches this; the standing rule still does.

**M5 - the Poland `-shotstates` session now films the Sejm night, and nothing in the branch shows it was run.** `GameController.cs:6550` together with `UiScreenshotDriver.cs:4597-4614`. Before this branch, Poland's state pins logged "showed no election night" and filmed nothing at F. `Available(Poland)` is now true, so the same `CheckElection` builds the Polish night: frames `88n_election_night_takeover` and `88c_after_election_night` now exist for Poland, under the Canvas text guard, in a world where §770 has just planted and removed a presidential contest. Claim 7 ("does not break the Poland `-shotstates` path") is unproven: the commits carry no film evidence line, and no `§` record is on the branch. Run the Poland states session once.

**L1 - the subset loop can be the closed form.** `PolishSejmNight.cs:257`. See claim 2. Admit every list not called OUT and divide once; an open list's certain seats are 0. The probe shows the two agree everywhere.

**L2 - an OUT call cannot land before 41 of 41 for any realistic vote.** `PolishSejmNight.cs:239`. Okreg 19 (Warsaw), 8.02 % of the base, declares last, and the OUT bound counts every vote still out. So a 5 % party can never be called OUT early, and a coalition only below about 1 %. Probe: 13 of 7,677 OUT calls landed early, all of them KO near 1 %. That is sound but degenerate: "OUT AT n" on the calls line always reads 41. Also, board 28b asked for a bar of "counted ÷ all votes cast"; the build draws (counted − residue) ÷ base, which is right (claim 1) but a departure that has not been told to Design.

**L3 - three of the diagnostic's statements cannot fail.** `PolishNightDiagnostic.cs:105-118, 167`.
- "A landed call never changes" compares `At(minute)` with `At(final)`. Both re-run the same walk, and the walk never revisits a landed call, so they always agree. The real property, that an early call is the chamber's verdict, is covered only because (b) checks the 41-of-41 calls.
- "Certain and in play make its magnitude" is a definition: `InPlay = Magnitude − held`.
- (f) reads a constant `true`.

Replace these with an early-call-against-`Admitted` assertion, which is the one the probe ran.

**L4 - the 14 px floor at 1280 is 13.8 px.** The `ElectionNightScreen.Poland.cs:16` doc says 1.5 units a board pixel "holds the floor at 1280". That is true only for width matching. `CanvasChrome.ScalerMatch` is 0.5, so at 1280 × 699 the factor is √(0.667 × 0.647) = 0.657, and `Px(14)` = 21 units draws at 13.8 px. The shared masthead and foot draw at 12 and 11 units (7.9 and 7.2 px) on a night that claims v3.5. The okreg numbers' floor is 11 units (`OkregiCellView.cs:168`).

**L5 - a texture leak.** `OkregiCellView.cs:157` allocates a `Texture2D` on every `Rebuild` (each build, and each resize via `LateUpdate`) and never destroys it, neither on rebuild nor when the night is destroyed. That is about 0.8 MB at 1280 and 3.2 MB at 2560 per build. §648's 2560 film already died of out-of-memory once.

**L6 - "14 days on" is typed while the chip beside it is computed.** `ElectionNightScreen.Poland.cs:592`. A rule or calendar change would part the two lines.

**L7 - the run-off note has no honesty mark.** `ElectionNightScreen.Poland.cs:552`: "a share of the first round's voters stays home". That share is `AbstentionDraftMax` [AUTHORED-DRAFT], printed beside a [FITTED] τ with no mark. The words are the board's, and no figure is shown, so this is small.

**L8 - unreachable and lost states.**
- `PresidentStage.RunOff` and frame `e7r_presidential_election_night_runoff_undecided` (`UiScreenshotDriver.cs:5591`) are reached only by a tied run-off.
- A President's night falling on a Sejm night's day is dropped: `CheckPresidentialRound` returns on `_electionNight != null` and never retries.
- `PresidentAt`'s return is ignored (`GameController.PolishNight.cs:175`). `sitting` is a default struct with a null name, and the between-rounds line drops the name silently.

**L9 - departures not recorded.** The branch has no `§` record. These differ from the boards and are written nowhere:
- the † dense view is absent;
- the ABSENT dashed frame is a plain line (`:647`);
- the MN slip lacks 2023's "25 778 votes, 0 seats";
- 28b's Q3, that the lines are measured over the six committees and not over art. 196's total, is answered nowhere on the night (the legend says "of the national vote");
- the President's night ignores SP-7b's `PlayerCandidate`;
- the audit driver's `.nightN.txt` numbering for Polish runs now counts the President's nights.

Set against these: the LOW CONFIDENCE placements (the swing's head under lineage, and the cabinet's own line) match 28b's answer 6.
